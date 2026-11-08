using Avalonia;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;
using AnimatedCornerRadius = Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;

namespace Material3Expressive.Components;

// InteractiveListItem's one color Transition, independent animateDpAsState and keyed shape state.
internal sealed class ListItemMotion : IDisposable
{
    private readonly ListItem _owner;
    private readonly AnimationChildScope _scope;
    private readonly Transition<ColorState> _transition;
    private readonly Transition<ColorState>.TransitionAnimationState<Color>[] _colors;
    private readonly Animatable<float> _elevation;
    private double _paintedShapeProgress = double.NaN;
    private float _elevationTarget;
    private CancellationTokenSource? _elevationEffect;
    private Task? _elevationRun;
    private AnimatedCornerRadius? _shape;
    private ShapeTarget[] _shapes = [];
    private MaterialShape[] _observedShapes = [];
    private readonly CutCornerShape _cutOutline = new();
    private ShapeTarget? _staticShape;
    private sealed record ShapeTarget(MaterialShape? Path, CornerShape? Corners)
    {
        // Compose's corner shapes use value equality for key(shapes). Avalonia paths are
        // mutable objects; compare their immutable corner snapshots, not object identity.
        public bool Equals(ShapeTarget? other) => other is not null && (Corners is not null
            ? Corners == other.Corners : other.Corners is null && ReferenceEquals(Path, other.Path));
        public override int GetHashCode() => Corners?.GetHashCode() ?? Path?.GetHashCode() ?? 0;
    }
    private MaterialSpring _shapeSpec;
    private bool _pending;
    private bool _deferShape;
    private Task? _colorRun;
    private CancellationTokenSource? _colorEffect;
    private CancellationTokenSource? _shapeEffect;
    private Task? _shapeRun;
    private readonly record struct ColorState(bool Enabled, bool Selected, bool Dragged);
    private ColorState State => new(_owner.RowEnabled, _owner.Selected, _owner.Interactions.HasFlag(InteractionState.Dragged));

    public ListItemMotion(ListItem owner)
    {
        _owner = owner;
        _scope = AnimationScope.Get(owner).CreateChild("List item");
        _transition = new(State, "ListColor");
        _transition.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Transition.IsRunning) && !_transition.IsRunning) _colorEffect?.Cancel();
        };
        StyledProperty<Color>[] properties = [ListItem.AnimatedContainerColorProperty, ListItem.AnimatedContentColorProperty,
            ListItem.AnimatedLeadingContentColorProperty, ListItem.AnimatedTrailingContentColorProperty,
            ListItem.AnimatedOverlineContentColorProperty, ListItem.AnimatedSupportingContentColorProperty];
        _colors = properties.Select((property, index) =>
        {
            var animation = _transition.AnimateColor(state => Color(index, state), _ => ColorSpec());
            _scope.BindAnimatedValue(owner, property, animation);
            return animation;
        }).ToArray();
        _elevationTarget = TargetElevation;
        _elevation = new(new FloatConverter(), _elevationTarget, 0.1f, "List elevation");
        _scope.BindAnimatedValue(owner, ListItem.AnimatedElevationProperty, _elevation);
        ObserveShapes();
        UpdateShape();
    }
    private float TargetElevation => (float)(State.Dragged ? _owner.DraggedElevation : _owner.Elevation);
    internal void DeferShapeForInputCancellation() => _deferShape = true;
    private SpringSpec<float> ElevationSpec() => new((float)_owner.ElevationAnimation.DampingRatio, (float)_owner.ElevationAnimation.Stiffness, 0.1f);
    private SpringSpec<Color> ColorSpec() => new(_owner.ColorAnimation.DampingRatio, _owner.ColorAnimation.Stiffness);
    private Color Color(int index, ColorState state)
    {
        Color[] values = !state.Enabled
            ? [_owner.DisabledContainerColor, _owner.DisabledContentColor, _owner.DisabledLeadingContentColor, _owner.DisabledTrailingContentColor, _owner.DisabledOverlineContentColor, _owner.DisabledSupportingContentColor]
            : state.Dragged
                ? [_owner.DraggedContainerColor, _owner.DraggedContentColor, _owner.DraggedLeadingContentColor, _owner.DraggedTrailingContentColor, _owner.DraggedOverlineContentColor, _owner.DraggedSupportingContentColor]
                : state.Selected
                    ? [_owner.SelectedContainerColor, _owner.SelectedContentColor, _owner.SelectedLeadingContentColor, _owner.SelectedTrailingContentColor, _owner.SelectedOverlineContentColor, _owner.SelectedSupportingContentColor]
                    : [_owner.ContainerColor, _owner.ContentColor, _owner.LeadingContentColor, _owner.TrailingContentColor, _owner.OverlineContentColor, _owner.SupportingContentColor];
        return values[index];
    }
    internal void Refresh()
    {
        if (_scope.Lifetime.IsCancellationRequested) return;
        ObserveShapes();
        PaintShape();
        if (_pending) return;
        _pending = true;
        _ = _scope.Launch(async ct =>
        {
            if (!await _scope.WaitForNextFrameAsync(ct)) return;
            _pending = false;
            // animateDpAsState receives the collected drag state during composition.
            UpdateElevation();
            if (_deferShape)
            {
                _deferShape = false;
                // enabled changes dispose combinedClickable's input in composition. Its
                // interaction collector invalidates the following composition's shape.
                _ = _scope.Launch(async token => { if (!await _scope.WaitForNextFrameAsync(token)) return; UpdateShape(); }, "List disabled shape");
            }
            else UpdateShape();
            var state = State;
            var changedState = state != _transition.TargetState;
            for (var i = 0; i < _colors.Length; i++)
            {
                var index = i;
                _colors[i].Update(s => Color(index, s), _ => ColorSpec());
            }
            _transition.UpdateTarget(state);
            if (_colors.Any(c => !c.IsFinished) && (_colorRun is null || _colorRun.IsCompleted))
            {
                if (!changedState && !await _scope.WaitForNextFrameAsync(ct)) return;
                if (_colorRun is null || _colorRun.IsCompleted)
                {
                    _colorEffect?.Dispose();
                    _colorEffect = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    _colorRun = _scope.Launch(_ => _transition.RunAsync(_scope, _colorEffect.Token), "List color transition");
                }
            }
        }, "List composition");
    }
    private void UpdateElevation()
    {
        var target = TargetElevation;
        if (_elevationTarget == target) return;
        _elevationTarget = target;
        // Fixed AnimateAsState.restartAnimation cancels its previous job before
        // animateTo: cancellation resets velocity and last-frame time. AnimatedState's
        // continuous retargeting intentionally has different interruption semantics.
        _elevationEffect?.Cancel();
        var previous = _elevationRun;
        var spec = ElevationSpec();
        _elevationRun = _scope.Launch(async ct =>
        {
            if (previous is not null) { try { await previous; } catch (OperationCanceledException) { } }
            using var effect = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _elevationEffect = effect;
            try { await _elevation.AnimateToAsync(_scope, target, spec, cancellationToken: effect.Token); }
            finally { if (ReferenceEquals(_elevationEffect, effect)) _elevationEffect = null; }
        }, "List elevation");
    }
    private void UpdateShape()
    {
        var rounds = _owner.ResolveShapes();
        ShapeTarget Resolve(MaterialShape? path, CornerRadius corners) => new(path,
            path is null ? new CornerShape.Defined(corners) : CornerShape.FromMaterialShape(path));
        ShapeTarget[] shapes = [Resolve(_owner.Shape, rounds.Shape), Resolve(_owner.SelectedShape, rounds.SelectedShape),
            Resolve(_owner.PressedShape, rounds.PressedShape), Resolve(_owner.FocusedShape, rounds.FocusedShape),
            Resolve(_owner.HoveredShape, rounds.HoveredShape), Resolve(_owner.DraggedShape, rounds.DraggedShape)];
        var flags = _owner.Interactions;
        var target = shapes[flags.HasFlag(InteractionState.Pressed) ? 2
            : flags.HasFlag(InteractionState.Dragged) ? 5 : _owner.Selected ? 1
            : flags.HasFlag(InteractionState.Focused) ? 3 : flags.HasFlag(InteractionState.Hovered) ? 4 : 0];
        if (shapes.Any(shape => shape.Corners is null))
        {
            // ListItemShapes only animates when all six entries are CornerBasedShape.
            // In the generic branch ripple's shape identity follows the selected entry.
            if (_shape is not null || _staticShape is not null && _staticShape != target) _owner.ResetIndication();
            _shapeEffect?.Cancel(); _shape?.Dispose(); _shape = null;
            _shapes = shapes; _staticShape = target;
            PaintShape();
            return;
        }
        if (_shape is null || !_shapes.SequenceEqual(shapes) || _shapeSpec != _owner.ShapeAnimation)
        {
            if (_shape is not null || _staticShape is not null) _owner.ResetIndication();
            _shapeEffect?.Cancel();
            _shape?.Dispose();
            _shapeSpec = _owner.ShapeAnimation;
            _shapes = shapes;
            _staticShape = null;
            _shape = new(target.Corners!, FloatShapeSpec(), _scope, PaintShape);
        }
        else if (_shape.TargetShape != target.Corners)
        {
            _shapeEffect?.Cancel();
            var previous = _shapeRun;
            var shape = _shape;
            _shapeRun = _scope.Launch(async ct =>
            {
                if (previous is not null) { try { await previous; } catch (OperationCanceledException) { } }
                using var effect = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _shapeEffect = effect;
                try { await shape.AnimateToAsync(target.Corners!, FloatShapeSpec(), effect.Token); }
                finally { if (ReferenceEquals(_shapeEffect, effect)) _shapeEffect = null; }
            }, "List shape");
        }
        PaintShape();
    }

    private void ObserveShapes()
    {
        MaterialShape?[] candidates = [_owner.Shape, _owner.SelectedShape, _owner.PressedShape,
            _owner.FocusedShape, _owner.HoveredShape, _owner.DraggedShape];
        var shapes = candidates.OfType<MaterialShape>().Distinct().ToArray();
        if (_observedShapes.SequenceEqual(shapes)) return;
        foreach (var shape in _observedShapes) shape.PropertyChanged -= ShapeChanged;
        _observedShapes = shapes;
        foreach (var shape in shapes) shape.PropertyChanged += ShapeChanged;
    }
    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Refresh();
    // Compose's spring parameters are Float before the solver widens them to Double.
    private MaterialSpring FloatShapeSpec() => new((float)_shapeSpec.DampingRatio,
        (float)_shapeSpec.Stiffness, (float)_shapeSpec.VisibilityThreshold);
    internal void PaintShape()
    {
        if (_owner.Surface is not { } surface) return;
        if (_shape is null)
        {
            if (_staticShape is not { } target) return;
            surface.Shape = target.Path;
            surface.SetAnimatedCorners(target.Path is null ? target.Corners?.Resolve(surface.Bounds.Size) : null);
            return;
        }
        _shape.Size = surface.Bounds.Size;
        var progress = _shape.Progress;
        surface.Shape = _shape.CurrentIsCut ? _cutOutline : null;
        surface.SetAnimatedCorners(_shape.Current, shapeStateChanged: _paintedShapeProgress != progress);
        _paintedShapeProgress = progress;
    }
    public void Dispose()
    {
        foreach (var shape in _observedShapes) shape.PropertyChanged -= ShapeChanged;
        _observedShapes = [];
        _shapeEffect?.Cancel();
        _colorEffect?.Cancel();
        _shape?.Dispose();
        _elevationEffect?.Cancel();
        _elevation.Stop();
        _transition.Dispose();
        _scope.Dispose();
        _colorEffect?.Dispose();
        _owner.Surface?.SetAnimatedCorners(null);
        if (_owner.Surface is { } surface) surface.Shape = null;
    }
}

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

// The visual-only layers preserve the content's logical parent, bindings, transforms and opacity.
internal sealed class VisibilitySurface : Control
{
    internal bool OwnsLogicalChild { get; set; } = true;
    private Control? _child;
    public Control? Child
    {
        get => _child;
        set
        {
            if (_child == value) return;
            if (_child is not null)
            {
                VisualChildren.Remove(_child);
                if (OwnsLogicalChild) LogicalChildren.Remove(_child);
            }
            _child = value;
            if (value is not null)
            {
                VisualChildren.Add(value);
                if (OwnsLogicalChild) LogicalChildren.Add(value);
            }
            InvalidateMeasure();
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _child?.Measure(availableSize);
        return _child?.DesiredSize ?? default;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _child?.Arrange(new Rect(finalSize));
        return finalSize;
    }
}

internal sealed class VisibilityLayer : Control
{
    internal VisibilitySurface Surface { get; } = new();
    internal VisibilityMotion? Motion { get; set; }
    internal Action? Measured { get; set; }
    internal Func<AnimatedVisibilityScope?>? ResolveScope { get; set; }
    internal Func<EnterTransition>? Enter { get; set; }
    internal Func<ExitTransition>? Exit { get; set; }
    // Crossfade.animateFloat applies its own spec in every segment, including reversal.
    internal Func<IFiniteAnimationSpec<double>>? CrossfadeAlphaSpec { get; set; }
    internal Func<string?>? Label { get; set; }
    private Size _fullSize;
    /// <summary>Surface desired size, before expand/shrink LayoutSize.</summary>
    internal Size FullSize => _fullSize;

    public VisibilityLayer()
    {
        VisualChildren.Add(Surface);
        LogicalChildren.Add(Surface);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scope = ResolveScope?.Invoke();
        if (scope is not null && !scope.IsDisposed && (Motion is null || Motion.Scope != scope))
        {
            Motion?.Dispose();
            // Create channels only after the content has been measured below.
            Motion = null;
        }
        Surface.Measure(availableSize);
        _fullSize = Surface.DesiredSize;
        if (scope is not null && !scope.IsDisposed && Motion is null)
            Motion = new VisibilityMotion(this, scope, Enter!, Exit!, _fullSize);
        else
            Motion?.SetFullSize(_fullSize);
        Measured?.Invoke();
        return Motion?.LayoutSize(_fullSize) ?? _fullSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = Motion?.ChangesSize == true ? _fullSize : finalSize;
        var offset = Motion?.AlignmentOffset(finalSize, size, FlowDirection) ?? default;
        Surface.Arrange(new Rect(offset, size));
        Motion?.ApplyVisuals();
        return finalSize;
    }

    internal void ResetMotion()
    {
        Motion?.Dispose();
        Motion = null;
        Surface.Opacity = 1;
        Surface.RenderTransform = null;
        Surface.RenderTransformOrigin = RelativePoint.Center;
        ClipToBounds = false;
        InvalidateMeasure();
    }
}

internal sealed class VisibilityMotion : IDisposable
{
    private readonly VisibilityLayer _layer;
    private readonly Func<EnterTransition> _getEnter;
    private readonly Func<ExitTransition> _getExit;
    private VisibilityEffects _enter = new();
    private VisibilityEffects _exit = new();
    private EnterExitState _configuredTarget;
    private Size _fullSize;
    private bool _disposed;
    private bool _initialized;
    private readonly List<IDisposable> _animations = [];
    private readonly Transition<EnterExitState>.TransitionAnimationState<double> _alpha;
    private readonly Transition<EnterExitState>.TransitionAnimationState<double> _scale;
    private readonly Transition<EnterExitState>.TransitionAnimationState<Vector> _offset;
    private readonly Transition<EnterExitState>.TransitionAnimationState<Size> _size;
    private readonly Transition<EnterExitState>.TransitionAnimationState<Point> _origin;
    private readonly Transition<EnterExitState>.TransitionAnimationState<Point> _alignment;
    private readonly MatrixTransform _transform = new();

    internal AnimatedVisibilityScope Scope { get; }
    private Transition<EnterExitState> Transition => Scope.Transition;
    private VisibilityEffects Active => Transition.TargetState == EnterExitState.PostExit ? _exit : _enter;
    internal bool ChangesSize => _enter.ChangeSize is not null || _exit.ChangeSize is not null;

    internal VisibilityMotion(VisibilityLayer layer, AnimatedVisibilityScope scope,
        Func<EnterTransition> enter, Func<ExitTransition> exit, Size fullSize)
    {
        _layer = layer;
        Scope = scope;
        _getEnter = enter;
        _getExit = exit;
        _fullSize = fullSize;
        _configuredTarget = Transition.TargetState;
        enter().Apply(_enter);
        exit().Apply(_exit);
        Scope.Register(this);
        try
        {
            _alpha = Track(Transition.AnimateDouble(s => { Configure(); return s switch
            {
                EnterExitState.PreEnter => _enter.Fade?.Alpha ?? 1,
                EnterExitState.PostExit => _exit.Fade?.Alpha ?? 1,
                _ => 1
            }; }, segment => _layer.CrossfadeAlphaSpec?.Invoke() ?? (segment.InitialState == EnterExitState.PreEnter && segment.TargetState == EnterExitState.Visible
                ? _enter.Fade?.Spec ?? VisibilityEffects.Spring<double>()
                : segment.InitialState == EnterExitState.Visible && segment.TargetState == EnterExitState.PostExit
                    ? _exit.Fade?.Spec ?? VisibilityEffects.Spring<double>() : VisibilityEffects.Spring<double>()), Name("alpha")));
            _scale = Track(Transition.AnimateDouble(s => s switch
            {
                EnterExitState.PreEnter => _enter.Scale?.Scale ?? 1,
                EnterExitState.PostExit => _exit.Scale?.Scale ?? 1,
                _ => 1
            }, segment => segment.InitialState == EnterExitState.PreEnter && segment.TargetState == EnterExitState.Visible
                ? _enter.Scale?.Spec ?? DefaultScaleSpring()
                : segment.InitialState == EnterExitState.Visible && segment.TargetState == EnterExitState.PostExit
                    ? _exit.Scale?.Spec ?? DefaultScaleSpring() : DefaultScaleSpring(), Name("scale")));
            _offset = Track(Transition.AnimateVector(s => Checked(s switch
            {
                EnterExitState.PreEnter => _enter.Slide?.Offset(_fullSize) ?? default,
                EnterExitState.PostExit => _exit.Slide?.Offset(_fullSize) ?? default,
                _ => default
            }), _ => Active.Slide?.Spec ?? new SnapSpec<Vector>(), Name("offset")));
            _size = Track(Transition.AnimateSize(SizeByState,
                _ => Transition.CurrentState == EnterExitState.Visible && Transition.TargetState == EnterExitState.Visible
                    ? new SnapSpec<Size>() : Active.ChangeSize?.Spec ?? new SnapSpec<Size>(), Name("size")));
            _origin = Track(Transition.AnimatePoint(s => OriginFraction(s switch
            {
                EnterExitState.PreEnter => _enter.Scale?.Origin ?? _exit.Scale?.Origin,
                EnterExitState.PostExit => _exit.Scale?.Origin,
                _ => Transition.CurrentState == EnterExitState.PreEnter
                    ? _enter.Scale?.Origin ?? _exit.Scale?.Origin
                    : _exit.Scale?.Origin ?? _enter.Scale?.Origin
            } ?? RelativePoint.Center),
                // TransformOrigin.VectorConverter uses fractions and the default float
                // spring threshold, independently of the scale animation spec.
                _ => _enter.Scale is null && _exit.Scale is null
                    ? new SnapSpec<Point>() : new SpringSpec<Point>(visibilityThreshold: new Point(0.01, 0.01)), Name("origin")));
            _alignment = Track(Transition.AnimatePoint(s => AlignmentFraction((s switch
            {
                EnterExitState.PreEnter => _enter.ChangeSize?.Alignment,
                EnterExitState.PostExit => _exit.ChangeSize?.Alignment,
                _ => Active.ChangeSize?.Alignment
            }) ?? _enter.ChangeSize?.Alignment ?? _exit.ChangeSize?.Alignment ?? VisibilityAlignment.Center),
                _ => VisibilityEffects.Spring<Point>(), Name("alignment")));
            _initialized = true;
            ApplyVisuals();
        }
        catch { Dispose(); throw; }
    }

    private string Name(string channel) => $"{_layer.Label?.Invoke() ?? "Visibility"} {channel}";

    // EnterExitTransition.kt: the absent effect still uses its default spring when an
    // interrupted channel has residual velocity, even if both endpoints have value 1.
    private static SpringSpec<double> DefaultScaleSpring()
        => new(stiffness: Spring.StiffnessMediumLow, visibilityThreshold: 0.002);

    private Transition<EnterExitState>.TransitionAnimationState<T> Track<T>(Transition<EnterExitState>.TransitionAnimationState<T> animation)
    {
        _animations.Add(animation);
        animation.PropertyChanged += Changed;
        _animations.Add(new ActionDisposable(() => animation.PropertyChanged -= Changed));
        return animation;
    }

    // Target selectors run synchronously during the Transition's state mutation, before sampling.
    private void Configure()
    {
        if (_configuredTarget == Transition.TargetState) return;
        _configuredTarget = Transition.TargetState;
        if (_configuredTarget == EnterExitState.PostExit)
        {
            _exit = new();
            _getExit().Apply(_exit);
        }
        else
        {
            _enter = new();
            _getEnter().Apply(_enter);
        }
    }

    private Size SizeByState(EnterExitState state)
    {
        var size = state switch
        {
            EnterExitState.PreEnter => _enter.ChangeSize?.Size(_fullSize) ?? _fullSize,
            EnterExitState.PostExit => _exit.ChangeSize?.Size(_fullSize) ?? _fullSize,
            _ => _fullSize
        };
        if (!double.IsFinite(size.Width) || !double.IsFinite(size.Height) || size.Width < 0 || size.Height < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Visibility sizes must be finite and non-negative.");
        return size;
    }

    private static Vector Checked(Vector offset)
    {
        VisibilityEffects.Finite(offset.X);
        VisibilityEffects.Finite(offset.Y);
        return offset;
    }

    private Point OriginFraction(RelativePoint origin)
        => origin.Unit == RelativeUnit.Relative ? origin.Point : new Point(
            _fullSize.Width == 0 ? 0 : origin.Point.X / _fullSize.Width,
            _fullSize.Height == 0 ? 0 : origin.Point.Y / _fullSize.Height);

    internal void SetFullSize(Size size)
    {
        if (_disposed || size == _fullSize) return;
        _fullSize = size;
        Transition.RefreshAnimations();
    }

    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (!_initialized || _disposed) return;
        if (e.PropertyName == "Value")
        {
            ApplyVisuals();
            if (ReferenceEquals(sender, _size)) _layer.InvalidateMeasure();
            if (ReferenceEquals(sender, _alignment)) _layer.InvalidateArrange();
        }
    }

    internal Size LayoutSize(Size fullSize)
        => ChangesSize && !(Transition.CurrentState == EnterExitState.Visible && Transition.TargetState == EnterExitState.Visible && !Transition.IsRunning)
            ? new Size(Math.Max(0, _size.Value.Width), Math.Max(0, _size.Value.Height)) : fullSize;

    internal Point AlignmentOffset(Size available, Size full, FlowDirection flowDirection)
    {
        if (!ChangesSize) return default;
        var x = flowDirection == FlowDirection.RightToLeft ? 1 - _alignment.Value.X : _alignment.Value.X;
        return new Point((available.Width - full.Width) * x, (available.Height - full.Height) * _alignment.Value.Y);
    }

    internal static Point Align(Size child, Size available, VisibilityAlignment alignment, FlowDirection flowDirection)
    {
        var fraction = AlignmentFraction(alignment);
        var x = flowDirection == FlowDirection.RightToLeft ? 1 - fraction.X : fraction.X;
        return new Point((available.Width - child.Width) * x, (available.Height - child.Height) * fraction.Y);
    }

    private static Point AlignmentFraction(VisibilityAlignment alignment)
    {
        var x = alignment switch
        {
            VisibilityAlignment.TopStart or VisibilityAlignment.Start or VisibilityAlignment.BottomStart => 0d,
            VisibilityAlignment.TopEnd or VisibilityAlignment.End or VisibilityAlignment.BottomEnd => 1d,
            _ => 0.5
        };
        var y = alignment switch
        {
            VisibilityAlignment.TopStart or VisibilityAlignment.Top or VisibilityAlignment.TopEnd => 0d,
            VisibilityAlignment.BottomStart or VisibilityAlignment.Bottom or VisibilityAlignment.BottomEnd => 1d,
            _ => 0.5
        };
        return new Point(x, y);
    }

    internal void ApplyVisuals()
    {
        if (!_initialized || _disposed) return;
        _layer.Surface.Opacity = Math.Clamp(_alpha.Value, 0, 1);
        var origin = new Point(_origin.Value.X * _fullSize.Width, _origin.Value.Y * _fullSize.Height);
        _transform.Matrix = Matrix.CreateTranslation(-origin.X, -origin.Y)
            * Matrix.CreateScale(_scale.Value, _scale.Value)
            * Matrix.CreateTranslation(origin.X + _offset.Value.X, origin.Y + _offset.Value.Y);
        _layer.Surface.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        _layer.Surface.RenderTransform = _transform;
        _layer.ClipToBounds = Active.ChangeSize?.Clip ?? false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var animation in _animations) animation.Dispose();
        _animations.Clear();
        Scope.Unregister(this);
        if (ReferenceEquals(_layer.Motion, this)) _layer.Motion = null;
    }
}

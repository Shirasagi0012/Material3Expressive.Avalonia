using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum DrawerValue { Closed, Open }

/// <summary>Hoisted two-anchor drawer state. CurrentValue is the last settled value, including during a drag.</summary>
public sealed class DrawerState : AvaloniaObject
{
    public static readonly DirectProperty<DrawerState, DrawerValue> CurrentValueProperty = AvaloniaProperty.RegisterDirect<DrawerState, DrawerValue>(nameof(CurrentValue), s => s.CurrentValue);
    public static readonly DirectProperty<DrawerState, DrawerValue> TargetValueProperty = AvaloniaProperty.RegisterDirect<DrawerState, DrawerValue>(nameof(TargetValue), s => s.TargetValue);
    public static readonly DirectProperty<DrawerState, double> CurrentOffsetProperty = AvaloniaProperty.RegisterDirect<DrawerState, double>(nameof(CurrentOffset), s => s.CurrentOffset);
    public static readonly DirectProperty<DrawerState, bool> IsAnimationRunningProperty = AvaloniaProperty.RegisterDirect<DrawerState, bool>(nameof(IsAnimationRunning), s => s.IsAnimationRunning);
    public static readonly DirectProperty<DrawerState, bool> IsOpenProperty = AvaloniaProperty.RegisterDirect<DrawerState, bool>(nameof(IsOpen), s => s.IsOpen);
    public static readonly DirectProperty<DrawerState, bool> IsClosedProperty = AvaloniaProperty.RegisterDirect<DrawerState, bool>(nameof(IsClosed), s => s.IsClosed);

    private DrawerValue _current, _target, _anchorValue;
    private double _currentOffset = double.NaN;
    private bool _running;
    private Control? _owner;
    private readonly HashSet<Control> _owners = [];
    private AnimationChildScope? _scope;
    private CancellationTokenSource? _mutation, _restart;
    private DrawerValue? _dragTarget;
    private float _offset = float.NaN, _closed = float.NaN, _velocity;
    private double _density = 1;
    private bool _dragging;
    private readonly Func<DrawerValue, bool> _confirm;

    public DrawerState() : this(DrawerValue.Closed) { }
    public DrawerState(DrawerValue initialValue, Func<DrawerValue, bool>? confirmStateChange = null)
    {
        _current = _target = _anchorValue = initialValue;
        _confirm = confirmStateChange ?? (_ => true);
    }
    public DrawerValue CurrentValue => _current;
    public DrawerValue TargetValue => _target;
    /// <summary>Logical start-axis offset in DIP; NaN before the first layout.</summary>
    public double CurrentOffset => _currentOffset;
    public bool IsAnimationRunning => _running;
    public bool IsOpen => CurrentValue == DrawerValue.Open;
    public bool IsClosed => !IsOpen;
    public Task OpenAsync(CancellationToken cancellationToken = default) => Animate(DrawerValue.Open, false, _velocity, cancellationToken);
    public Task CloseAsync(CancellationToken cancellationToken = default) => Animate(DrawerValue.Closed, false, _velocity, cancellationToken);

    public Task SnapToAsync(DrawerValue value, CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        CancelMutation();
        _dragging = false;
        if (!float.IsNaN(_closed)) DragTo(Position(value));
        // anchoredDrag runs the body before confirmValueChange; veto does not undo its offset.
        if (Confirm(value)) SetSettled(value);
        Refresh();
        return Task.CompletedTask;
    }

    internal bool Confirm(DrawerValue value) => _confirm(value);
    internal float OffsetPx => _offset;
    internal float ClosedPx => _closed;
    internal void Attach(Control owner)
    {
        if (!_owners.Add(owner)) return;
        _owner = owner;
        _scope ??= AnimationScope.GetRoot(TopLevel.GetTopLevel(owner)!).CreateChild("Drawer state");
    }
    internal void Detach(Control owner)
    {
        if (!_owners.Remove(owner)) return;
        if (_owners.Count != 0) { _owner = _owners.First(); return; }
        CancelMutation();
        _scope?.Dispose(); _scope = null; _owner = null; _dragging = false;
        Refresh();
    }
    internal void UpdateAnchors(double width, double density)
    {
        var closed = -(float)(width * density);
        if (_closed == closed && _density == density) return;
        _closed = closed; _density = density;
        var target = float.IsNaN(_offset) ? TargetValue : Closest(_offset);
        if (_mutation is not null || _dragging)
        {
            _dragTarget = target;
            _restart?.Cancel();
        }
        else { DragTo(Position(target)); SetSettled(target); }
        Refresh();
    }
    internal void BeginDrag()
    {
        CancelMutation(); _dragging = true;
        Refresh();
    }
    internal void DragBy(float delta)
    {
        DragTo(Math.Clamp(_offset + delta, _closed, 0));
        Refresh();
    }
    internal Task EndDrag(float velocity)
    {
        _dragging = false;
        var target = ComputeTarget(velocity);
        if (!Confirm(target)) target = _anchorValue;
        return Animate(target, true, velocity, default);
    }
    private float Position(DrawerValue value) => value == DrawerValue.Open ? 0 : _closed;
    private DrawerValue Closest(float offset) => Math.Abs(offset - _closed) < Math.Abs(offset) ? DrawerValue.Closed : DrawerValue.Open;
    private DrawerValue ComputeTarget(float velocity)
    {
        if (velocity == 0) return Closest(_offset);
        if (_offset <= _closed) return DrawerValue.Closed;
        if (_offset >= 0) return DrawerValue.Open;
        if (Math.Abs(velocity) >= 400 * _density) return velocity > 0 ? DrawerValue.Open : DrawerValue.Closed;
        return velocity > 0 ? (_offset - _closed >= -_closed * .5f ? DrawerValue.Open : DrawerValue.Closed)
            : (-_offset >= -_closed * .5f ? DrawerValue.Closed : DrawerValue.Open);
    }
    private Task Animate(DrawerValue value, bool settle, float velocity, CancellationToken cancellationToken)
    {
        Dispatcher.UIThread.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        if (_scope is not { } scope || scope.Lifetime.IsCancellationRequested)
            throw new InvalidOperationException("Mount a drawer before animating. Use the constructor or SnapToAsync to seed its state.");
        CancelMutation(); _dragging = false;
        var mutation = CancellationTokenSource.CreateLinkedTokenSource(scope.Lifetime, cancellationToken);
        _mutation = mutation; _dragTarget = value; Refresh();
        return scope.Launch(async _ =>
        {
            try
            {
                if (float.IsNaN(_closed)) { if (Confirm(value)) SetSettled(value); return; }
                while (true)
                {
                    using var restart = CancellationTokenSource.CreateLinkedTokenSource(mutation.Token);
                    _restart = restart;
                    var latest = _dragTarget!.Value;
                    var target = Position(latest);
                    var initial = float.IsNaN(_offset) ? 0 : _offset;
                    var animation = new AnimationState<float>(new FloatConverter(), initial, AnimationVector.From([velocity]));
                    try
                    {
                        var decay = DecayAnimationSpec.ExponentialDecay<float>();
                        var projected = decay.CalculateTargetValue(initial, velocity);
                        if (settle && initial != target && velocity * (target - initial) >= 0 && velocity != 0
                            && (velocity > 0 ? projected >= target : projected <= target))
                        {
                            var previous = initial;
                            await SuspendAnimation.AnimateDecayAsync(scope, animation, decay, block: frame =>
                            {
                                var crossed = frame.Value < target && previous > target || frame.Value > target && previous < target;
                                DragTo(crossed ? target : frame.Value, frame.Velocity);
                                previous = _offset; Refresh();
                                if (crossed) frame.CancelAnimation();
                            }, cancellationToken: restart.Token);
                        }
                        else if (!settle || initial != target)
                        {
                            // NavigationDrawer.kt captures this tween in the anchored state constructor.
                            IAnimationSpec<float> spec = settle ? new TweenSpec<float>(TimeSpan.FromMilliseconds(256)) : Spring(latest);
                            await SuspendAnimation.AnimateToAsync(scope, animation, target, spec, block: frame => { DragTo(frame.Value, frame.Velocity); Refresh(); }, cancellationToken: restart.Token);
                        }
                        if (Confirm(value)) { DragTo(Position(value), _velocity); SetSettled(value); }
                        break;
                    }
                    catch (OperationCanceledException) when (!mutation.IsCancellationRequested && restart.IsCancellationRequested)
                    { velocity = _velocity; }
                    finally { if (_restart == restart) _restart = null; }
                }
            }
            finally
            {
                if (_mutation == mutation) { _mutation = null; _dragTarget = null; Refresh(); }
                mutation.Dispose();
            }
        }, "Drawer anchored animation");
    }
    private IAnimationSpec<float> Spring(DrawerValue value)
    {
        var owner = (NavigationDrawer)_owner!;
        var spec = value == DrawerValue.Open ? owner.OpenAnimation : owner.CloseAnimation;
        if (spec.Stiffness <= 0)
            throw new InvalidOperationException("NavigationDrawer motion configuration is not ready. Provide the requested open/close animation before changing its state.");
        return new SpringSpec<float>((float)spec.DampingRatio, (float)spec.Stiffness, .01f);
    }
    private void DragTo(float offset, float velocity = 0)
    {
        var previous = _offset; _offset = offset; _velocity = velocity;
        if (float.IsNaN(previous)) return;
        var next = offset >= previous ? DrawerValue.Open : DrawerValue.Closed;
        if (Math.Abs(offset - Position(_anchorValue)) >= -_closed / 2 && Confirm(next)) _anchorValue = next;
    }
    private void SetSettled(DrawerValue value)
    {
        var wasOpen = IsOpen;
        SetAndRaise(CurrentValueProperty, ref _current, value); _anchorValue = value;
        if (wasOpen != IsOpen) { RaisePropertyChanged(IsOpenProperty, wasOpen, IsOpen); RaisePropertyChanged(IsClosedProperty, !wasOpen, IsClosed); }
    }
    private void CancelMutation()
    {
        _mutation?.Cancel(); _mutation = null; _dragTarget = null;
        Refresh();
    }
    private void Refresh()
    {
        SetAndRaise(TargetValueProperty, ref _target, _dragTarget ?? (float.IsNaN(_offset) ? _anchorValue : Closest(_offset)));
        SetAndRaise(CurrentOffsetProperty, ref _currentOffset, _offset / _density);
        SetAndRaise(IsAnimationRunningProperty, ref _running, _dragTarget is not null);
    }
}

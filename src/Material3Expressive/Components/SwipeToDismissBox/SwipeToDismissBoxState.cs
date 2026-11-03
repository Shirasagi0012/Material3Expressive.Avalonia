/* SwipeToDismissBox.kt / AnchoredDraggable.kt: anchored state and settling.
 * Copyright 2023-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum SwipeToDismissBoxValue { StartToEnd, EndToStart, Settled }

/// <summary>Application-owned state for <see cref="SwipeToDismissBox"/>. Distances are in DIP.</summary>
public sealed class SwipeToDismissBoxState : AvaloniaObject
{
    public static readonly DirectProperty<SwipeToDismissBoxState, SwipeToDismissBoxValue> CurrentValueProperty = AvaloniaProperty.RegisterDirect<SwipeToDismissBoxState, SwipeToDismissBoxValue>(nameof(CurrentValue), s => s.CurrentValue);
    public static readonly DirectProperty<SwipeToDismissBoxState, SwipeToDismissBoxValue> TargetValueProperty = AvaloniaProperty.RegisterDirect<SwipeToDismissBoxState, SwipeToDismissBoxValue>(nameof(TargetValue), s => s.TargetValue);
    public static readonly DirectProperty<SwipeToDismissBoxState, SwipeToDismissBoxValue> SettledValueProperty = AvaloniaProperty.RegisterDirect<SwipeToDismissBoxState, SwipeToDismissBoxValue>(nameof(SettledValue), s => s.SettledValue);
    public static readonly DirectProperty<SwipeToDismissBoxState, SwipeToDismissBoxValue> DismissDirectionProperty = AvaloniaProperty.RegisterDirect<SwipeToDismissBoxState, SwipeToDismissBoxValue>(nameof(DismissDirection), s => s.DismissDirection);
    public static readonly DirectProperty<SwipeToDismissBoxState, double> CurrentOffsetProperty = AvaloniaProperty.RegisterDirect<SwipeToDismissBoxState, double>(nameof(CurrentOffset), s => s.CurrentOffset);
    public static readonly DirectProperty<SwipeToDismissBoxState, double> ProgressProperty = AvaloniaProperty.RegisterDirect<SwipeToDismissBoxState, double>(nameof(Progress), s => s.Progress);

    private SwipeToDismissBoxValue _current, _settled, _target, _direction;
    private double _currentOffset = double.NaN, _progress = 1, _density = 1;
    private float _offset = float.NaN, _velocity;
    private readonly Func<double, double> _positionalThreshold;
    // Insertion order is significant for ties in DraggableAnchors.closestAnchor.
    private (SwipeToDismissBoxValue Value, float Position)[] _anchors = [];
    private readonly HashSet<Control> _owners = [];
    private AnimationChildScope? _scope;
    private CancellationTokenSource? _mutation, _restart;
    private SwipeToDismissBoxValue? _dragTarget;
    private bool _dragging;
    private (double Width, double Density, bool Start, bool End)? _pendingAnchors;

    public SwipeToDismissBoxState() : this(SwipeToDismissBoxValue.Settled) { }
    public SwipeToDismissBoxState(SwipeToDismissBoxValue initialValue, Func<double, double>? positionalThreshold = null)
    {
        _current = _settled = _target = initialValue;
        _direction = SwipeToDismissBoxValue.Settled;
        _positionalThreshold = positionalThreshold ?? (_ => 56);
    }

    /// <summary>The closest anchor passed during the current movement, including mid-drag.</summary>
    public SwipeToDismissBoxValue CurrentValue => _current;
    public SwipeToDismissBoxValue TargetValue => _target;
    /// <summary>The last completed anchor; unlike CurrentValue this does not change mid-drag.</summary>
    public SwipeToDismissBoxValue SettledValue => _settled;
    public SwipeToDismissBoxValue DismissDirection => _direction;
    /// <summary>Logical start-axis offset in DIP; NaN until the first layout.</summary>
    public double CurrentOffset => _currentOffset;
    /// <summary>Progress from SettledValue to TargetValue; 1 when both anchors coincide.</summary>
    public double Progress => _progress;
    public double RequireOffset() => double.IsNaN(CurrentOffset)
        ? throw new InvalidOperationException("The swipe offset is not initialized. Wait for the first layout.") : CurrentOffset;
    public Task ResetAsync(CancellationToken cancellationToken = default) => Animate(SwipeToDismissBoxValue.Settled, false, _velocity, cancellationToken);
    public Task DismissAsync(SwipeToDismissBoxValue direction, CancellationToken cancellationToken = default) => Animate(direction, false, _velocity, cancellationToken);

    public Task SnapToAsync(SwipeToDismissBoxValue value, CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        CancelMutation();
        _dragging = false;
        if (!float.IsNaN(Position(value))) DragTo(Position(value));
        SetSettled(value);
        Refresh();
        return Task.CompletedTask;
    }

    internal float OffsetPx => _offset;
    internal bool IsAnimationRunning => _dragTarget is not null;
    internal bool IsSettlingAnimation => _mutation is not null;
    internal void Attach(Control owner)
    {
        if (!_owners.Add(owner)) return;
        _scope ??= AnimationScope.GetRoot(TopLevel.GetTopLevel(owner)!).CreateChild("Swipe to dismiss state");
    }
    internal void Detach(Control owner)
    {
        if (!_owners.Remove(owner) || _owners.Count != 0) return;
        CancelMutation();
        _dragging = false;
        _scope?.Dispose();
        _scope = null;
        _pendingAnchors = null;
    }

    internal void UpdateAnchors(double width, double density, bool startToEnd, bool endToStart)
    {
        var same = _density == density && _anchors.Length == 1 + (startToEnd ? 1 : 0) + (endToStart ? 1 : 0)
            && (!startToEnd || Position(SwipeToDismissBoxValue.StartToEnd) == (float)(width * density))
            && (!endToStart || Position(SwipeToDismissBoxValue.EndToStart) == -(float)(width * density));
        if (_mutation is null || _scope is not { } scope)
        {
            _pendingAnchors = null;
            ApplyAnchors(width, density, startToEnd, endToStart);
            return;
        }
        if (same) { _pendingAnchors = null; return; }
        var pending = _pendingAnchors is not null;
        _pendingAnchors = (width, density, startToEnd, endToStart);
        if (pending) return;
        _ = scope.Launch(async ct =>
        {
            // Compose applies size/configuration changes after animation writes in the next
            // recomposition frame; snapshotFlow then restarts from that frame's latest offset.
            await scope.NextFrameAsync(ct);
            if (_pendingAnchors is not { } next) return;
            _pendingAnchors = null;
            ApplyAnchors(next.Width, next.Density, next.Start, next.End);
        }, "Swipe anchors update");
    }

    private void ApplyAnchors(double width, double density, bool startToEnd, bool endToStart)
    {
        var anchors = new List<(SwipeToDismissBoxValue, float)> { (SwipeToDismissBoxValue.Settled, 0) };
        if (startToEnd) anchors.Add((SwipeToDismissBoxValue.StartToEnd, (float)(width * density)));
        if (endToStart) anchors.Add((SwipeToDismissBoxValue.EndToStart, -(float)(width * density)));
        if (_anchors.SequenceEqual(anchors) && _density == density) return;
        var previous = _anchors.Length == 0 ? CurrentValue : TargetValue;
        _anchors = anchors.ToArray();
        _density = density;
        var target = float.IsNaN(Position(previous)) ? SwipeToDismissBoxValue.Settled : previous;
        if (_mutation is not null || _dragging)
        {
            _dragTarget = target;
            _restart?.Cancel();
        }
        else
        {
            DragTo(Position(target));
            _dragTarget = null;
            SetSettled(target);
        }
        Refresh();
    }

    internal void BeginDrag()
    {
        CancelMutation();
        _dragging = true;
    }
    internal void DragBy(float delta)
    {
        DragTo(ClampOffset((float.IsNaN(_offset) ? 0 : _offset) + delta));
        Refresh();
    }
    internal Task EndDrag(float velocity)
    {
        _dragging = false;
        // The no-target anchoredDrag completes before fling takes the mutex.
        CompleteAtClosest();
        return Animate(ComputeTarget(velocity), true, velocity, default);
    }
    internal void CancelDrag()
    {
        _dragging = false;
        CompleteAtClosest();
        _dragTarget = null;
        Refresh();
    }

    private float Position(SwipeToDismissBoxValue value) => _anchors.FirstOrDefault(a => a.Value == value, (Value: value, Position: float.NaN)).Position;
    private float ClampOffset(float offset) => Math.Clamp(offset, _anchors.Min(a => a.Position), _anchors.Max(a => a.Position));
    private SwipeToDismissBoxValue Closest(float offset, bool? upwards = null)
    {
        var closest = CurrentValue;
        var minimum = float.PositiveInfinity;
        foreach (var anchor in _anchors)
        {
            var delta = upwards is { } up ? (up ? anchor.Position - offset : offset - anchor.Position) : Math.Abs(anchor.Position - offset);
            var distance = delta < 0 ? float.PositiveInfinity : delta;
            if (distance <= minimum) { minimum = distance; closest = anchor.Value; }
        }
        return closest;
    }
    private SwipeToDismissBoxValue ComputeTarget(float velocity)
    {
        if (velocity == 0) return Closest(_offset);
        var forward = velocity > 0;
        if (Math.Abs(velocity) >= 125 * _density) return Closest(_offset, forward);
        var left = Closest(_offset, false);
        var right = Closest(_offset, true);
        var distance = Math.Abs(Position(left) - Position(right));
        var threshold = Math.Abs((float)(_positionalThreshold(distance / _density) * _density));
        var origin = forward ? Position(left) : Position(right);
        return Math.Abs(origin - _offset) >= threshold ? (forward ? right : left) : (forward ? left : right);
    }

    private Task Animate(SwipeToDismissBoxValue value, bool fling, float velocity, CancellationToken cancellationToken)
    {
        Dispatcher.UIThread.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        // AnchoredDraggable allows a programmatic target with no anchor (also before layout).
        if (!fling && float.IsNaN(Position(value)))
        {
            SetSettled(value);
            Refresh();
            return Task.CompletedTask;
        }
        if (_scope is not { } scope || scope.Lifetime.IsCancellationRequested)
            throw new InvalidOperationException("Mount a SwipeToDismissBox before animating its measured state.");
        CancelMutation();
        _dragging = false;
        var mutation = CancellationTokenSource.CreateLinkedTokenSource(scope.Lifetime, cancellationToken);
        _mutation = mutation;
        // A fling uses the no-target anchoredDrag overload. Its target stays the closest anchor.
        _dragTarget = fling ? null : value;
        Refresh();
        return scope.Launch(async _ =>
        {
            try
            {
                while (true)
                {
                    using var restart = CancellationTokenSource.CreateLinkedTokenSource(mutation.Token);
                    _restart = restart;
                    var target = Position(fling ? ComputeTarget(velocity) : _dragTarget!.Value);
                    var initial = float.IsNaN(_offset) ? 0 : _offset;
                    var animation = new AnimationState<float>(new FloatConverter(), fling ? 0 : initial, AnimationVector.From([velocity]));
                    try
                    {
                        if (fling || initial != target)
                        {
                            var consumed = 0f;
                            await SuspendAnimation.AnimateToAsync(scope, animation, fling ? target - initial : target,
                                new TweenSpec<float>(TimeSpan.FromMilliseconds(300)), block: frame =>
                                {
                                    if (fling)
                                    {
                                        var delta = frame.Value - consumed;
                                        var next = ClampOffset(_offset + delta);
                                        var actual = next - _offset;
                                        DragTo(next);
                                        consumed += actual;
                                        if (Math.Abs(delta - actual) > .5f) frame.CancelAnimation();
                                    }
                                    else DragTo(frame.Value, frame.Velocity);
                                    Refresh();
                                }, cancellationToken: restart.Token);
                        }
                        if (fling) CompleteAtClosest();
                        else
                        {
                            // The target-accepting upstream overload commits its original target,
                            // even if the anchor update restarted the animation toward a new one.
                            DragTo(Position(value), _velocity);
                            SetSettled(value);
                        }
                        break;
                    }
                    catch (OperationCanceledException) when (!mutation.IsCancellationRequested && restart.IsCancellationRequested)
                    { if (!fling) velocity = _velocity; }
                    finally { if (_restart == restart) _restart = null; }
                }
            }
            finally
            {
                if (_mutation == mutation) { _mutation = null; _dragTarget = null; Refresh(); }
                mutation.Dispose();
            }
        }, "Swipe to dismiss anchored animation");
    }

    private void DragTo(float offset, float velocity = 0)
    {
        var previous = _offset;
        _offset = offset;
        _velocity = velocity;
        if (float.IsNaN(previous)) return;
        var forward = offset >= previous;
        SwipeToDismissBoxValue left, right;
        if (offset == Position(CurrentValue))
        {
            var other = Closest(offset + (forward ? 1 : -1), forward);
            left = forward ? CurrentValue : other;
            right = forward ? other : CurrentValue;
        }
        else { left = Closest(offset, false); right = Closest(offset, true); }
        if (Math.Abs(offset - Position(CurrentValue)) >= Math.Abs(Position(left) - Position(right)) / 2)
            SetAndRaise(CurrentValueProperty, ref _current, forward ? right : left);
    }
    private void CompleteAtClosest()
    {
        var closest = Closest(_offset);
        if (Math.Abs(_offset - Position(closest)) < .5f) SetSettled(closest);
    }
    private void SetSettled(SwipeToDismissBoxValue value)
    {
        SetAndRaise(CurrentValueProperty, ref _current, value);
        SetAndRaise(SettledValueProperty, ref _settled, value);
    }
    private void CancelMutation()
    {
        _mutation?.Cancel();
        _mutation = null;
        _dragTarget = null;
        Refresh();
    }
    private void Refresh()
    {
        var currentPosition = Position(CurrentValue);
        var target = _dragTarget ?? (float.IsNaN(_offset) || float.IsNaN(currentPosition) || _offset == currentPosition ? CurrentValue : Closest(_offset));
        SetAndRaise(TargetValueProperty, ref _target, target);
        SetAndRaise(CurrentOffsetProperty, ref _currentOffset, _offset / _density);
        SetAndRaise(DismissDirectionProperty, ref _direction, float.IsNaN(_offset) || _offset == 0 ? SwipeToDismissBoxValue.Settled : _offset > 0 ? SwipeToDismissBoxValue.StartToEnd : SwipeToDismissBoxValue.EndToStart);
        var start = Position(SettledValue);
        var end = Position(TargetValue);
        var progress = Math.Abs(end - start) > 1e-6f ? (_offset - start) / (end - start) : 1;
        SetAndRaise(ProgressProperty, ref _progress, progress < 1e-6f ? 0 : progress > 1 - 1e-6f ? 1 : progress);
    }
}

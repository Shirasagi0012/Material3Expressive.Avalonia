// SheetDefaults.kt, BottomSheet.kt and Foundation AnchoredDraggable.kt, fixed upstream baseline.
// Copyright 2023-2026 The Android Open Source Project. Licensed under Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum SheetValue { Hidden, Expanded, PartiallyExpanded }

/// <summary>Application-owned bottom sheet state. Distances and threshold callbacks use DIP.</summary>
public sealed class SheetState : AvaloniaObject
{
    public static readonly DirectProperty<SheetState, SheetValue> CurrentValueProperty = AvaloniaProperty.RegisterDirect<SheetState, SheetValue>(nameof(CurrentValue), s => s.CurrentValue);
    public static readonly DirectProperty<SheetState, SheetValue> TargetValueProperty = AvaloniaProperty.RegisterDirect<SheetState, SheetValue>(nameof(TargetValue), s => s.TargetValue);
    public static readonly DirectProperty<SheetState, double> CurrentOffsetProperty = AvaloniaProperty.RegisterDirect<SheetState, double>(nameof(CurrentOffset), s => s.CurrentOffset);
    public static readonly DirectProperty<SheetState, bool> IsVisibleProperty = AvaloniaProperty.RegisterDirect<SheetState, bool>(nameof(IsVisible), s => s.IsVisible);
    public static readonly DirectProperty<SheetState, bool> IsAnimationRunningProperty = AvaloniaProperty.RegisterDirect<SheetState, bool>(nameof(IsAnimationRunning), s => s.IsAnimationRunning);
    public static readonly DirectProperty<SheetState, bool> HasExpandedStateProperty = AvaloniaProperty.RegisterDirect<SheetState, bool>(nameof(HasExpandedState), s => s.HasExpandedState);
    public static readonly DirectProperty<SheetState, bool> HasPartiallyExpandedStateProperty = AvaloniaProperty.RegisterDirect<SheetState, bool>(nameof(HasPartiallyExpandedState), s => s.HasPartiallyExpandedState);

    private SheetValue _current, _target, _anchorValue;
    private double _currentOffset = double.NaN, _density = 1;
    private bool _visible, _running, _expanded, _partial, _dragging;
    private float _offset = float.NaN, _velocity;
    private readonly HashSet<SheetValue> _enabled;
    private readonly Func<SheetValue, bool> _confirm;
    private readonly Func<double> _positionalThreshold;
    private (SheetValue Value, float Position)[] _anchors = [];
    private BottomSheet? _owner;
    private AnimationChildScope? _scope;
    private CancellationTokenSource? _mutation, _restart;
    private SheetValue? _dragTarget;
    private (double Full, double Sheet, double Density, double? Peek)? _pendingAnchors;
    private (double Full, double Sheet, double Density, double? Peek)? _appliedLayout;

    public SheetState() : this(SheetValue.Hidden) { }
    public SheetState(SheetValue initialValue, IEnumerable<SheetValue>? enabledValues = null,
        Func<SheetValue, bool>? confirmValueChange = null, Func<double>? positionalThreshold = null)
    {
        _enabled = enabledValues?.ToHashSet() ?? [SheetValue.Hidden, SheetValue.PartiallyExpanded, SheetValue.Expanded];
        if (!_enabled.Contains(SheetValue.Expanded)) throw new ArgumentException("Expanded must be enabled.", nameof(enabledValues));
        if (!_enabled.Contains(initialValue)) throw new ArgumentException("The initial value must be enabled.", nameof(initialValue));
        _current = _target = _anchorValue = initialValue;
        _visible = initialValue != SheetValue.Hidden;
        _confirm = confirmValueChange ?? (_ => true);
        _positionalThreshold = positionalThreshold ?? (() => 56);
    }

    /// <summary>The last settled value; this stays unchanged while dragging or animating.</summary>
    public SheetValue CurrentValue => _current;
    public SheetValue TargetValue => _target;
    public double CurrentOffset => _currentOffset;
    /// <summary>Uses the current crossed anchor, so visibility can change before settling.</summary>
    public bool IsVisible => _visible;
    public bool IsAnimationRunning => _running;
    public bool HasExpandedState => _expanded;
    public bool HasPartiallyExpandedState => _partial;
    public double RequireOffset() => double.IsNaN(CurrentOffset)
        ? throw new InvalidOperationException("The sheet offset is not initialized. Wait for its first layout.") : CurrentOffset;
    public Task ExpandAsync(CancellationToken cancellationToken = default) => Request(SheetValue.Expanded, true, cancellationToken);
    public Task ShowAsync(CancellationToken cancellationToken = default) => Request(HasPartiallyExpandedState ? SheetValue.PartiallyExpanded : SheetValue.Expanded, true, cancellationToken);
    public Task PartialExpandAsync(CancellationToken cancellationToken = default)
    {
        if (!_enabled.Contains(SheetValue.PartiallyExpanded)) throw new InvalidOperationException("PartiallyExpanded is not enabled.");
        return Request(SheetValue.PartiallyExpanded, false, cancellationToken);
    }
    public Task HideAsync(CancellationToken cancellationToken = default)
    {
        if (!_enabled.Contains(SheetValue.Hidden)) throw new InvalidOperationException("Hidden is not enabled.");
        return Request(SheetValue.Hidden, false, cancellationToken);
    }
    private Task Request(SheetValue value, bool show, CancellationToken cancellationToken)
    {
        VerifyAccess(); cancellationToken.ThrowIfCancellationRequested();
        if (!Confirm(value)) return Task.CompletedTask;
        return Animate(value, false, _velocity, show, cancellationToken);
    }

    internal bool Confirm(SheetValue value) => _confirm(value);
    internal bool HiddenEnabled => _enabled.Contains(SheetValue.Hidden);
    internal float OffsetPx => _offset;
    internal float MinimumAnchor => _anchors.Length == 0 ? float.NaN : _anchors.Min(a => a.Position);
    internal void Attach(BottomSheet owner)
    {
        if (_owner == owner) return;
        if (_owner is not null) throw new InvalidOperationException("A SheetState can be attached to only one BottomSheet at a time.");
        _owner = owner;
        _scope = AnimationScope.Get(owner).CreateChild("Bottom sheet state");
    }
    internal void Detach(BottomSheet owner)
    {
        if (_owner != owner) return;
        CancelMutation(); _dragging = false; _pendingAnchors = null;
        _scope?.Dispose(); _scope = null; _owner = null;
    }
    internal void UpdateAnchors(double fullHeight, double sheetHeight, double density, double? peekHeight = null)
    {
        if (_mutation is null || _scope is not { } scope) { ApplyAnchors(fullHeight, sheetHeight, density, peekHeight); return; }
        var next = (fullHeight, sheetHeight, density, peekHeight);
        if (_appliedLayout == next && _pendingAnchors is null) return;
        if (_pendingAnchors == next) return;
        var alreadyPending = _pendingAnchors is not null;
        _pendingAnchors = next;
        if (alreadyPending) return;
        _ = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct);
            if (_pendingAnchors is not { } pending) return;
            _pendingAnchors = null; ApplyAnchors(pending.Full, pending.Sheet, pending.Density, pending.Peek);
        }, "Bottom sheet anchor update");
    }
    private void ApplyAnchors(double fullHeight, double sheetHeight, double density, double? peekHeight)
    {
        _appliedLayout = (fullHeight, sheetHeight, density, peekHeight);
        var full = (float)(fullHeight * density); var sheet = (float)(sheetHeight * density);
        var anchors = new List<(SheetValue Value, float Position)>();
        if (peekHeight is { } peekDip)
        {
            var peek = (float)(peekDip * density);
            var ambiguousPartial = peek == 0 && (_anchors.Length == 0 || CurrentValue == SheetValue.PartiallyExpanded && !IsAnimationRunning);
            if (_enabled.Contains(SheetValue.PartiallyExpanded) && (peek > 0 || ambiguousPartial) && peek != sheet)
                anchors.Add((SheetValue.PartiallyExpanded, full - peek));
            if (sheet == 0 || peek == 0 || HiddenEnabled) anchors.Add((SheetValue.Hidden, full));
            if (sheet > 0) anchors.Add((SheetValue.Expanded, full - sheet));
        }
        else
        {
            anchors.Add((SheetValue.Hidden, full));
            // Deterministic partially-expanded flag is true in the fixed baseline.
            if (_enabled.Contains(SheetValue.PartiallyExpanded)) anchors.Add((SheetValue.PartiallyExpanded, full - Math.Min(full / 2, sheet)));
            if (sheet != 0) anchors.Add((SheetValue.Expanded, Math.Max(0, full - sheet)));
        }
        if (_anchors.SequenceEqual(anchors) && density == _density) return;
        var wasConverged = HasPartiallyExpandedState && HasExpandedState && Position(SheetValue.PartiallyExpanded) == Position(SheetValue.Expanded);
        var target = TargetValue;
        _anchors = anchors.ToArray(); _density = density;
        if (target == SheetValue.PartiallyExpanded)
            target = peekHeight is null && wasConverged && HasPosition(SheetValue.Expanded) ? SheetValue.Expanded
                : HasPosition(SheetValue.PartiallyExpanded) ? SheetValue.PartiallyExpanded
                : HasPosition(SheetValue.Expanded) ? SheetValue.Expanded : SheetValue.Hidden;
        else if (target == SheetValue.Expanded && !HasPosition(SheetValue.Expanded)) target = SheetValue.Hidden;
        if (_mutation is not null || _dragging) { _dragTarget = target; _restart?.Cancel(); }
        else
        {
            // Foundation trySnapTo preserves the previous offset when the reconciled value
            // no longer has an anchor (notably standard sheets leaving a zero peek height).
            var targetOffset = Position(target);
            if (!float.IsNaN(targetOffset)) { DragTo(targetOffset); _dragTarget = null; }
            SetSettled(target);
        }
        Refresh();
    }
    internal void BeginDrag() { CancelMutation(); _dragging = true; }
    internal void CancelDrag() { _dragging = false; _dragTarget = null; Refresh(); }
    internal void DragBy(float delta) { DragTo(Clamp(_offset + delta)); Refresh(); }
    internal float DispatchRawDelta(float delta)
    {
        var previous = float.IsNaN(_offset) ? 0 : _offset;
        _offset = Clamp(previous + delta); // dispatchRawDelta does not update currentValue or velocity.
        Refresh(); return _offset - previous;
    }
    internal Task<float> EndDrag(float velocity, CancellationToken cancellationToken = default)
    {
        _dragging = false; CompleteAtClosest();
        return FlingAsync(velocity, cancellationToken);
    }
    internal async Task<float> FlingAsync(float velocity, CancellationToken cancellationToken = default)
    {
        var safe = velocity;
        if (_owner?.StandardPeekHeight is null && safe > 0 && HasPosition(SheetValue.Hidden))
        {
            var distance = Math.Max(0, Position(SheetValue.Hidden) - _offset);
            var zone = 125f * (float)_density;
            if (distance < zone)
            {
                var factor = distance / zone; safe *= factor * factor;
                if (velocity >= zone) safe = Math.Max(safe, zone);
            }
        }
        return await Animate(CurrentValue, true, safe, true, cancellationToken);
    }
    private bool HasPosition(SheetValue value) => _anchors.Any(a => a.Value == value);
    private float Position(SheetValue value) => _anchors.FirstOrDefault(a => a.Value == value, (Value: value, Position: float.NaN)).Position;
    private float Clamp(float value) => Math.Clamp(value, _anchors.Min(a => a.Position), _anchors.Max(a => a.Position));
    private SheetValue Closest(float offset, bool? forward = null)
    {
        var result = _anchorValue; var distance = float.PositiveInfinity;
        foreach (var anchor in _anchors)
        {
            var delta = forward is { } f ? f ? anchor.Position - offset : offset - anchor.Position : Math.Abs(anchor.Position - offset);
            var candidate = delta < 0 ? float.PositiveInfinity : delta;
            if (candidate <= distance) { distance = candidate; result = anchor.Value; }
        }
        return result;
    }
    private SheetValue ComputeTarget(float velocity)
    {
        SheetValue target;
        if (velocity == 0) target = Closest(_offset);
        else if (Math.Abs(velocity) >= 125 * _density) target = Closest(_offset, velocity > 0);
        else
        {
            var left = Closest(_offset, false); var right = Closest(_offset, true);
            var from = velocity > 0 ? Position(left) : Position(right);
            var crossed = Math.Abs(from - _offset) >= Math.Abs((float)(_positionalThreshold() * _density));
            target = crossed ? velocity > 0 ? right : left : velocity > 0 ? left : right;
        }
        return Confirm(target) ? target : CurrentValue;
    }
    private Task<float> Animate(SheetValue value, bool fling, float velocity, bool show, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!fling && !HasPosition(value)) { if (Confirm(value)) SetSettled(value); Refresh(); return Task.FromResult(0f); }
        var scope = _scope ?? throw new InvalidOperationException("Mount a BottomSheet before animating its measured state.");
        var owner = _owner!;
        var configured = show ? owner.ShowAnimation : owner.HideAnimation;
        if (configured is not { Stiffness: > 0 } motion) throw new InvalidOperationException("The bottom sheet motion theme is not ready.");
        var spec = new SpringSpec<float>(motion.DampingRatio, motion.Stiffness);
        CancelMutation(); _dragging = false;
        var mutation = CancellationTokenSource.CreateLinkedTokenSource(scope.Lifetime, cancellationToken);
        _mutation = mutation; _dragTarget = fling ? null : value; Refresh();
        return Run();
        async Task<float> Run()
        {
            var remaining = 0f;
            try
            {
                await scope.Launch(async _ =>
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
                                await SuspendAnimation.AnimateToAsync(scope, animation, fling ? target - initial : target, spec, block: frame =>
                                {
                                    if (fling)
                                    {
                                        var targetOffset = target - initial;
                                        var real = targetOffset == 0 ? 0 : targetOffset > 0 ? Math.Min(frame.Value, targetOffset) : Math.Max(frame.Value, targetOffset);
                                        var delta = real - consumed;
                                        var next = Clamp(_offset + delta); var actual = next - _offset;
                                        DragTo(next); consumed += actual;
                                        remaining = velocity == 0 ? 0 : velocity > 0 ? Math.Min(frame.Velocity, velocity) : Math.Max(frame.Velocity, velocity);
                                        if (Math.Abs(delta - actual) > .5f || real != frame.Value) frame.CancelAnimation();
                                    }
                                    else DragTo(frame.Value, frame.Velocity);
                                    Refresh();
                                }, cancellationToken: restart.Token);
                                if (fling && Math.Abs(target - _offset) < .5f) remaining = 0;
                            }
                            if (fling) CompleteAtClosest();
                            else if (Confirm(value)) { DragTo(Position(value), _velocity); SetSettled(value); }
                            break;
                        }
                        catch (OperationCanceledException) when (!mutation.IsCancellationRequested && restart.IsCancellationRequested)
                        { if (!fling) velocity = _velocity; }
                        finally { if (_restart == restart) _restart = null; }
                    }
                }, "Bottom sheet anchored animation");
                return remaining;
            }
            finally
            {
                if (_mutation == mutation) { _mutation = null; _dragTarget = null; Refresh(); }
                mutation.Dispose();
            }
        }
    }
    private void DragTo(float offset, float velocity = 0)
    {
        var previous = _offset; _offset = offset; _velocity = velocity;
        if (float.IsNaN(previous)) return;
        var forward = offset >= previous;
        SheetValue left, right;
        if (offset == Position(_anchorValue))
        {
            var other = Closest(offset + (forward ? 1 : -1), forward);
            left = forward ? _anchorValue : other; right = forward ? other : _anchorValue;
        }
        else { left = Closest(offset, false); right = Closest(offset, true); }
        if (Math.Abs(offset - Position(_anchorValue)) >= Math.Abs(Position(left) - Position(right)) / 2)
        { var next = forward ? right : left; if (Confirm(next)) _anchorValue = next; }
    }
    private void CompleteAtClosest()
    {
        var closest = Closest(_offset);
        if (Math.Abs(_offset - Position(closest)) < .5f && Confirm(closest)) SetSettled(closest);
    }
    private void SetSettled(SheetValue value) { SetAndRaise(CurrentValueProperty, ref _current, value); _anchorValue = value; }
    private void CancelMutation() { _mutation?.Cancel(); _mutation = null; _dragTarget = null; Refresh(); }
    private void Refresh()
    {
        // The fixed Foundation target calculation retains a current value whose anchor
        // is absent, allowing a subsequent layout to restore that anticipated state.
        var currentAnchorOffset = Position(_anchorValue);
        SetAndRaise(TargetValueProperty, ref _target, _dragTarget ?? (float.IsNaN(_offset) || float.IsNaN(currentAnchorOffset) || _offset == currentAnchorOffset ? _anchorValue : Closest(_offset)));
        SetAndRaise(CurrentOffsetProperty, ref _currentOffset, _offset / _density);
        SetAndRaise(IsVisibleProperty, ref _visible, _anchorValue != SheetValue.Hidden);
        SetAndRaise(IsAnimationRunningProperty, ref _running, _dragTarget is not null);
        SetAndRaise(HasExpandedStateProperty, ref _expanded, HasPosition(SheetValue.Expanded));
        SetAndRaise(HasPartiallyExpandedStateProperty, ref _partial, HasPosition(SheetValue.PartiallyExpanded));
    }
}

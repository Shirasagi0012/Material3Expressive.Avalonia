using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal sealed class ContentSlotPanel : Panel
{
    internal AnimatedContent? Host { get; set; }
    private Transition<object?>.DeferredAnimation<Size>? _sizeAnimation;
    private Transition<object?>.TransitionAnimationState<Size>? _sizeNode;
    private Size? _lastSize;
    private readonly Dictionary<ContentSlot, Size> _slotSizes = [];

    internal Size? LastSize => _lastSize;
    internal void ClearLastSize() => _lastSize = null;
    internal Size MeasuredMax { get; private set; }
    internal Size TargetSlotSize { get; private set; }

    internal void ResetSize()
    {
        if (_sizeNode is not null) _sizeNode.PropertyChanged -= OnSizeSampled;
        _sizeNode = null;
        _sizeAnimation?.Dispose();
        _sizeAnimation = null;
        _lastSize = null;
        _slotSizes.Clear();
        if (Parent is Border border) border.ClipToBounds = false;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var host = Host ?? throw new InvalidOperationException("ContentSlotPanel requires a host.");
        _ = host.ContentAlignment;
        ContentSlot? target = null;
        foreach (var child in Children)
        {
            if (child is ContentSlot slot && slot.IsTarget) target = slot;
        }
        if (target is not null) target.Measure(availableSize);
        foreach (var child in Children)
        {
            if (child is ContentSlot slot && !ReferenceEquals(slot, target))
                slot.Measure(availableSize);
        }

        MeasuredMax = default;
        TargetSlotSize = default;
        _slotSizes.Clear();
        foreach (var child in Children)
        {
            if (child is not ContentSlot slot) continue;
            _slotSizes[slot] = slot.DesiredSize;
            MeasuredMax = new Size(Math.Max(MeasuredMax.Width, slot.DesiredSize.Width),
                Math.Max(MeasuredMax.Height, slot.DesiredSize.Height));
            if (slot.IsTarget) TargetSlotSize = slot.DesiredSize;
        }

        var previousSize = host.CurrentSize;
        host.CurrentSize = _lastSize ?? MeasuredMax;
        host.TargetContentSize = TargetSlotSize;
        if (previousSize != host.CurrentSize && previousSize != default)
        {
            foreach (var child in Children)
            {
                if (child is ContentSlot { Scope.Transition: { IsDisposed: false } transition })
                    transition.RefreshAnimations();
            }
        }
        var parent = host.Connection?.Transition;
        var transform = host.ResolvedTransform;
        var sizeTransform = transform?.SizeTransform;
        var animating = sizeTransform is not null && parent is not null &&
            !parent.IsSeeking &&
            !EqualityComparer<object?>.Default.Equals(parent.CurrentState, parent.TargetState);

        if (animating)
        {
            _sizeAnimation ??= parent!.CreateDeferredAnimation(new SizeConverter(),
                $"{host.Label ?? "AnimatedContent"} size");
            _sizeNode = _sizeAnimation.Animate(segment =>
            {
                var initial = SizeFor(segment.InitialState);
                var targetSize = SizeFor(segment.TargetState);
                return sizeTransform!.CreateAnimationSpec(initial, targetSize);
            }, SizeFor);
            _sizeNode.PropertyChanged -= OnSizeSampled;
            _sizeNode.PropertyChanged += OnSizeSampled;
            _lastSize = _sizeNode.Value;
            if (Parent is Border border) border.ClipToBounds = sizeTransform!.Clip;
            return new Size(Math.Max(0, _sizeNode.Value.Width), Math.Max(0, _sizeNode.Value.Height));
        }

        if (parent is not null &&
            !EqualityComparer<object?>.Default.Equals(parent.CurrentState, parent.TargetState))
        {
            if (Parent is Border borderBusy) borderBusy.ClipToBounds = false;
            return MeasuredMax;
        }
        if (_sizeNode is { IsFinished: false } && _sizeAnimation is not null && parent is not null)
        {
            _sizeNode = _sizeAnimation.Animate(_ => new SnapSpec<Size>(), _ => TargetSlotSize);
            _lastSize = _sizeNode.Value;
        }
        else
            _lastSize = TargetSlotSize == default ? MeasuredMax : TargetSlotSize;
        if (Parent is Border borderIdle) borderIdle.ClipToBounds = false;
        return _lastSize ?? default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var host = Host;
        foreach (var child in Children)
        {
            if (child is not ContentSlot slot) continue;
            var childSize = slot.DesiredSize;
            var offset = host is null ? default
                : EnterExitMotion.Align(childSize, finalSize, host.ContentAlignment, FlowDirection);
            slot.Arrange(new Rect(offset, childSize));
        }
        return finalSize;
    }

    private Size SizeFor(object? state)
    {
        var host = Host!;
        var parent = host.Connection!.Transition;
        if (host.KeysEqual(state, parent.Segment.InitialState))
            return _lastSize ?? MeasuredMax;
        var slot = host.SlotFor(state);
        return slot is not null && _slotSizes.TryGetValue(slot, out var size) ? size : default;
    }

    private void OnSizeSampled(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Value")
            InvalidateMeasure();
    }
}

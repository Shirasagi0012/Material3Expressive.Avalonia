using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

internal sealed partial class CarouselPanel : VirtualizingPanel
{
    private HorizontalCarousel? _owner;
    private float _density = 1;
    private float _pageSize, _spacing;
    private float? _layoutScroll;
    private int _layoutCurrent;
    private float _layoutFraction;
    private Size _pageConstraint;
    private double _measuredCrossAxis;
    internal CarouselStrategy? Strategy { get; private set; }
    internal float PageStride => _pageSize + _spacing;
    internal bool LastScrollRemeasured { get; private set; }
    protected override bool BypassFlowDirectionPolicies => true;

    protected override Size MeasureOverride(Size availableSize)
    {
        _owner = this.GetVisualAncestors().OfType<HorizontalCarousel>().FirstOrDefault();
        if (_owner is null) return default;
        _owner.SetPanel(this);
        if (!double.IsFinite(availableSize.Width))
            throw new InvalidOperationException("HorizontalCarousel needs a finite width along its scrolling axis.");
        var owner = _owner;
        var previousWidth = Strategy?.AvailableSpace / _density;
        var previousStride = PageStride;
        var previousDensity = _density;
        var previousState = Strategy?.IsValid == true ? LayoutStateAt((float)owner.ScrollOffset * _density) : ((int Current, float Fraction)?)null;
        var previousSnap = previousState is { } state ? Strategy!.SnapPosition(state.Current, Items.Count) : 0;
        _density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var space = (float)CarouselStrategy.Round((float)availableSize.Width * _density);
        _spacing = CarouselStrategy.Round((float)owner.ItemSpacing * _density);
        var preferred = double.IsNaN(owner.ItemWidth) ? space : (float)owner.ItemWidth * _density;
        var keylines = CarouselKeylines.Create(owner.Variant, space, preferred, _spacing, Items.Count,
            (float)owner.MinSmallItemWidth * _density, (float)owner.MaxSmallItemWidth * _density, 10 * _density);
        var padding = owner.ContentPadding;
        Strategy = new(keylines, space, _spacing, (float)padding.Left * _density, (float)padding.Right * _density);
        _layoutScroll = null;
        _pageSize = CarouselStrategy.Round(Strategy.ItemSize);
        if (previousState is { } old && (previousWidth != space / _density || previousStride != PageStride || previousDensity != _density ||
            previousSnap != Strategy.SnapPosition(old.Current, Items.Count)))
        {
            var scroll = Math.Clamp(old.Current * PageStride - CarouselStrategy.Round(
                Strategy.SnapPosition(old.Current, Items.Count) - old.Fraction * PageStride), 0, MaxOffset);
            owner.RebaseAfterMeasure(scroll / _density, _density);
            var (current, fraction) = LayoutStateAt(scroll);
            owner.Publish(MaxOffset / _density, current, fraction);
        }
        var cross = Math.Max(0, availableSize.Height - padding.Top - padding.Bottom);
        _pageConstraint = new Size(_pageSize / _density, cross);
        RealizePages((float)owner.ScrollOffset * _density, _pageConstraint);
        if (previousWidth is { } oldWidth) owner.TrackFocusOnResize(oldWidth, space / _density);
        _measuredCrossAxis = _realized.Values.Select(child => child.DesiredSize.Height).DefaultIfEmpty(0).Max();
        return new(availableSize.Width, _measuredCrossAxis + padding.Top + padding.Bottom);
    }

    internal double OffsetForItem(int index)
    {
        if (Strategy is not { IsValid: true } strategy || Items.Count == 0) return 0;
        index = Math.Clamp(index, 0, Items.Count - 1);
        return Math.Clamp(index * (_pageSize + _spacing) - strategy.SnapPosition(index, Items.Count), 0, MaxOffset) / _density;
    }
    internal int? PreJumpPosition(int target, double offset)
    {
        if (Strategy is not { IsValid: true } strategy || Items.Count == 0) return null;
        var scroll = Math.Clamp(CarouselStrategy.Round((float)offset * _density), 0, MaxOffset);
        var stride = _pageSize + _spacing;
        var first = Math.Clamp((int)(scroll / stride), 0, Items.Count - 1);
        var last = Math.Clamp((int)MathF.Ceiling((scroll + strategy.AvailableSpace) / stride) - 1, first, Items.Count - 1);
        var forward = target > first;
        if (((forward && target > last) || (!forward && target < first)) && Math.Abs(target - first) >= 3)
            return forward ? Math.Max(target - (last - first + 1), first) : Math.Min(target + (last - first + 1), first);
        return null;
    }
    internal float ScrollDistanceToItem(int target, float scroll, bool usePagerDistance)
    {
        if (Strategy is not { IsValid: true } strategy || Items.Count == 0) return 0;
        var (current, fraction) = LayoutStateAt(scroll);
        if (usePagerDistance)
        {
            // Pager semantics invokes PagerState.animateScrollToPage, not CarouselState's
            // specialized navigation. Preserve its fraction subtraction and extent clamp.
            var distance = CarouselStrategy.Round((target - current) * (_pageSize + _spacing) - fraction * (_pageSize + _spacing));
            return Math.Clamp(scroll + distance, 0, MaxOffset) - scroll;
        }
        // Fixed CarouselState.calculateScrollDistanceTo does not subtract the current offset
        // fraction. Preserve that upstream behavior when an animation is redirected mid-page.
        return (target - current) * (_pageSize + _spacing) + strategy.SnapPosition(current, Items.Count)
            - strategy.SnapPosition(target, Items.Count);
    }
    // PagerState.performScroll uses absolute page coordinates for bounds. When the
    // incremental layout cannot be reused, PagerMeasure reconstructs placement from
    // currentPage/currentPageOffsetFraction, including its half-page fraction clamp.
    internal (float Position, float Consumed, bool Coerced) ApplyScroll(float scroll, float delta)
    {
        if (Strategy is not { IsValid: true } strategy || Items.Count == 0) return (scroll, 0, true);
        var (current, fraction) = LayoutStateAt(scroll);
        var stride = PageStride;
        var absolute = current * stride + CarouselStrategy.Round(fraction * stride);
        var maximum = MaxOffset + Math.Clamp(strategy.SnapPosition(Items.Count - 1, Items.Count), 0, strategy.AvailableSpace);
        var minimum = Math.Min(maximum, Math.Clamp(strategy.SnapPosition(0, Items.Count), 0, strategy.AvailableSpace));
        var updated = absolute + delta;
        var coerced = Math.Clamp(updated, minimum, maximum);
        var consumed = coerced - absolute;
        var incremental = CanApplyScroll(scroll, consumed, fraction);
        LastScrollRemeasured = !incremental;
        var position = incremental ? scroll + consumed
            : current * stride - CarouselStrategy.Round(strategy.SnapPosition(current, Items.Count) - (fraction + consumed / stride) * stride);
        position = Math.Clamp(position, 0, MaxOffset);
        if (!incremental) _layoutScroll = null;
        LayoutStateAt(position);
        return (position, consumed, updated != coerced);
    }

    private bool CanApplyScroll(float scroll, float amount, float fraction)
    {
        var stride = PageStride;
        var space = Strategy!.AvailableSpace;
        var first = (int)(scroll / stride);
        var last = Math.Min(Items.Count - 1, (int)MathF.Ceiling((scroll + space) / stride) - 1);
        var firstOffset = first * stride - scroll;
        var lastOffset = last * stride - scroll;
        var firstScroll = scroll - first * stride + amount;
        var nextFraction = fraction + amount / stride;
        var canApply = amount > 0
            ? MathF.Min(firstOffset + stride, lastOffset + stride - space) > amount
            : MathF.Min(-firstOffset, space - lastOffset) > -amount;
        return firstScroll >= 0 && firstScroll < stride && nextFraction > -.5f && nextFraction < .5f && canApply;
    }

    private (int Current, float Fraction) LayoutStateAt(float scroll)
    {
        if (Items.Count == 0 || Strategy is not { IsValid: true } strategy) return (0, 0);
        var stride = _pageSize + _spacing;
        if (_layoutScroll is { } previous)
        {
            if (previous == scroll) return (_layoutCurrent, _layoutFraction);
            // PagerMeasureResult.copyWithScrollDeltaWithoutRemeasure preserves the current
            // page and accumulates Float fractions until its visible-page boundaries change.
            var delta = previous - scroll;
            var fraction = _layoutFraction - delta / stride;
            if (CanApplyScroll(previous, -delta, _layoutFraction))
            {
                _layoutScroll = scroll;
                _layoutFraction = fraction;
                return (_layoutCurrent, fraction);
            }
        }
        _layoutCurrent = VisibleIndices(scroll)
            .MinBy(i => MathF.Abs(i * stride - strategy.SnapPosition(i, Items.Count) - scroll));
        _layoutFraction = Math.Clamp((scroll - (_layoutCurrent * stride - strategy.SnapPosition(_layoutCurrent, Items.Count))) / stride, -.5f, .5f);
        _layoutScroll = scroll;
        return (_layoutCurrent, _layoutFraction);
    }
    internal float ApproachOffset(float scroll, float velocity, float decayOffset, CarouselFlingMode mode)
    {
        var stride = _pageSize + _spacing;
        if (stride == 0) return 0;
        var first = Math.Clamp((int)(scroll / stride), 0, Math.Max(0, Items.Count - 1));
        var start = velocity < 0 ? first + 1 : first;
        var target = Math.Clamp(start + (int)(decayOffset / stride), 0, Items.Count);
        var corrected = mode == CarouselFlingMode.SingleAdvance ? Math.Clamp(target, Math.Max(0, start - 1), start + 1) : target;
        corrected = Math.Clamp(corrected, 0, Items.Count);
        var approach = MathF.Max(0, MathF.Abs((corrected - start) * stride) - stride);
        return approach == 0 ? 0 : approach * Math.Sign(velocity);
    }
    internal float SnapOffset(float scroll, float velocity, float gestureDelta, float density, FlowDirection flow)
    {
        if (Strategy is not { IsValid: true } strategy || Items.Count == 0) return 0;
        var stride = _pageSize + _spacing;
        var lower = float.NegativeInfinity;
        var upper = float.PositiveInfinity;
        foreach (var i in VisibleIndices(scroll))
        {
            var position = i * stride - scroll;
            // Pager visiblePagesInfo includes the trailing spacing of a page.
            if (position + stride <= 0 || position >= strategy.AvailableSpace) continue;
            var distance = position - strategy.SnapPosition(i, Items.Count);
            if (distance <= 0 && distance > lower) lower = distance;
            if (distance >= 0 && distance < upper) upper = distance;
        }
        if (float.IsNegativeInfinity(lower)) lower = upper;
        if (float.IsPositiveInfinity(upper)) upper = lower;
        var scrollingForward = (int)gestureDelta == 0 ? velocity >= 0 : gestureDelta <= 0;
        if (scroll >= MaxOffset) { upper = 0; if (scrollingForward) lower = 0; }
        if (scroll <= 0) { lower = 0; if (!scrollingForward) upper = 0; }
        float result;
        if (MathF.Abs(velocity) >= 400 * density) result = velocity > 0 ? upper : lower;
        else
        {
            var forward = flow == FlowDirection.LeftToRight ? scrollingForward : !scrollingForward;
            var fromSnap = _pageSize == 0 ? 0 : gestureDelta / _pageSize;
            var overflow = fromSnap - (int)fromSnap;
            if (MathF.Abs(overflow) > .5f) result = forward ? upper : lower;
            else if (MathF.Abs(fromSnap) >= MathF.Abs(MathF.Min(56 * density, _pageSize / 2) / _pageSize))
                result = forward ? lower : upper;
            else result = MathF.Abs(lower) < MathF.Abs(upper) ? lower : upper;
        }
        return float.IsFinite(result) ? result : 0;
    }
    private float MaxOffset => Strategy is { } strategy
        ? MathF.Max(0, _pageSize * Items.Count + _spacing * (Items.Count - 1) - strategy.AvailableSpace) : 0;

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_owner is not { } owner || Strategy is not { IsValid: true } strategy)
        {
            foreach (var child in Children) child.Arrange(default);
            _owner?.Publish(0, 0, 0);
            return finalSize;
        }
        var scroll = Math.Clamp((float)CarouselStrategy.Round((float)owner.ScrollOffset * _density), 0, MaxOffset);
        var stride = _pageSize + _spacing;
        var (current, fraction) = LayoutStateAt(scroll);
        owner.Publish(MaxOffset / _density, current, fraction);
        var floatStride = strategy.ItemSize + _spacing;
        var strategyScroll = current * floatStride + fraction * floatStride - strategy.SnapPosition(current, Items.Count);
        var keylines = strategy.GetKeylines(strategyScroll, strategy.MaxScrollOffset(Items.Count));
        var padding = owner.ContentPadding;
        var height = Math.Max(0, finalSize.Height - padding.Top - padding.Bottom);
        RealizePages(scroll, _pageConstraint);
        if (_realized.Values.Select(child => child.DesiredSize.Height).DefaultIfEmpty(0).Max() != _measuredCrossAxis)
            InvalidateMeasure();
        // PagerMeasure.calculatePagesOffsets places pinned extra pages consecutively
        // beside visible pages, even when their item indices are far away. Keyline
        // translation still uses the item's original index below.
        var firstVisible = VisibleIndices(scroll).FirstOrDefault();
        var placementIndex = firstVisible - _realized.Keys.Count(index => index < firstVisible);
        foreach (var (i, child) in _realized)
        {
            var center = i * floatStride + strategy.ItemSize / 2 - strategyScroll;
            var before = keylines.Before(center);
            var after = keylines.After(center);
            var progress = before == after ? 1 : (center - before.UnadjustedOffset) / (after.UnadjustedOffset - before.UnadjustedOffset);
            var keyline = CarouselKeyline.Lerp(before, after, progress);
            var translation = keyline.Offset - center;
            if (before == after) translation += (center - keyline.UnadjustedOffset) / keyline.Size;
            var pageOffset = CarouselStrategy.Round(placementIndex++ * stride - scroll);
            // Pager places the integer page (including RTL) before carouselItem applies
            // its Float layer translation. Reassociation changes edge visibility in BIV.
            var x = owner.FlowDirection == FlowDirection.RightToLeft
                ? (float)finalSize.Width * _density - pageOffset - _pageSize - translation
                : pageOffset + translation;
            // Avalonia ZIndex is integer; this preserves the order/ties of 1f / (1 + distance).
            child.ZIndex = Items.Count - Math.Abs(i - current);
            child.Arrange(new Rect(x / _density, padding.Top, _pageSize / _density, height));
            if (child is CarouselItem item)
            {
                var left = (strategy.ItemSize / 2 - keyline.Size / 2) / _density;
                item.UpdateDrawInfo(new(keyline.Size / _density, strategy.MinItemSize / _density,
                    strategy.MaxItemSize / _density, new Rect(left, 0, keyline.Size / _density, height)));
            }
        }
        return finalSize;
    }
}

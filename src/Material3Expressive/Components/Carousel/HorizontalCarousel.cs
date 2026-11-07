using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

/// <summary>A controlled horizontal keyline carousel with multi-browse, uncontained and centered hero arrangements.</summary>
public partial class HorizontalCarousel : ItemsControl
{
    public static readonly StyledProperty<CarouselVariant> VariantProperty = AvaloniaProperty.Register<HorizontalCarousel, CarouselVariant>(nameof(Variant));
    public static readonly StyledProperty<double> ItemWidthProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(ItemWidth), double.NaN,
        validate: value => double.IsNaN(value) || double.IsFinite(value) && value > 0);
    public static readonly StyledProperty<double> ItemSpacingProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(ItemSpacing), validate: NonNegative);
    public static readonly StyledProperty<double> MinSmallItemWidthProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(MinSmallItemWidth), 40, validate: Positive);
    public static readonly StyledProperty<double> MaxSmallItemWidthProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(MaxSmallItemWidth), 56, validate: Positive);
    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<HorizontalCarousel, Thickness>(nameof(ContentPadding));
    public static readonly StyledProperty<double> ScrollOffsetProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(ScrollOffset), validate: double.IsFinite);
    public static readonly StyledProperty<CarouselFlingMode> FlingModeProperty = AvaloniaProperty.Register<HorizontalCarousel, CarouselFlingMode>(nameof(FlingMode));
    public static readonly StyledProperty<IAnimationSpec<float>?> SnapAnimationSpecProperty = AvaloniaProperty.Register<HorizontalCarousel, IAnimationSpec<float>?>(nameof(SnapAnimationSpec));
    public static readonly StyledProperty<IDecayAnimationSpec<float>?> DecayAnimationSpecProperty = AvaloniaProperty.Register<HorizontalCarousel, IDecayAnimationSpec<float>?>(nameof(DecayAnimationSpec));
    public static readonly DirectProperty<HorizontalCarousel, double> MaximumScrollOffsetProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, double>(nameof(MaximumScrollOffset), o => o.MaximumScrollOffset);
    public static readonly DirectProperty<HorizontalCarousel, int> CurrentItemProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, int>(nameof(CurrentItem), o => o.CurrentItem);
    public static readonly DirectProperty<HorizontalCarousel, double> CurrentItemOffsetFractionProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, double>(nameof(CurrentItemOffsetFraction), o => o.CurrentItemOffsetFraction);
    public static readonly DirectProperty<HorizontalCarousel, bool> IsScrollInProgressProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, bool>(nameof(IsScrollInProgress), o => o.IsScrollInProgress);
    private static bool NonNegative(double value) => double.IsFinite(value) && value >= 0;
    private static bool Positive(double value) => double.IsFinite(value) && value > 0;

    public CarouselVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    /// <summary>Preferred width for MultiBrowse, item width for Uncontained, maximum width for CenteredHero. NaN fills available space.</summary>
    public double ItemWidth { get => GetValue(ItemWidthProperty); set => SetValue(ItemWidthProperty, value); }
    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }
    public double MinSmallItemWidth { get => GetValue(MinSmallItemWidthProperty); set => SetValue(MinSmallItemWidthProperty, value); }
    public double MaxSmallItemWidth { get => GetValue(MaxSmallItemWidthProperty); set => SetValue(MaxSmallItemWidthProperty, value); }
    /// <summary>Logical start/top/end/bottom padding. Main-axis padding is applied after masking.</summary>
    public Thickness ContentPadding { get => GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    /// <summary>Requested logical scroll distance in DIP. Layout clamps to the current content extent.</summary>
    public double ScrollOffset { get => GetValue(ScrollOffsetProperty); set => SetValue(ScrollOffsetProperty, value); }
    public CarouselFlingMode FlingMode { get => GetValue(FlingModeProperty); set => SetValue(FlingModeProperty, value); }
    /// <summary>Optional fling snap spec; values and velocities use physical pixels as in Compose.</summary>
    public IAnimationSpec<float>? SnapAnimationSpec { get => GetValue(SnapAnimationSpecProperty); set => SetValue(SnapAnimationSpecProperty, value); }
    public IDecayAnimationSpec<float>? DecayAnimationSpec { get => GetValue(DecayAnimationSpecProperty); set => SetValue(DecayAnimationSpecProperty, value); }
    public double MaximumScrollOffset => _maximum;
    public int CurrentItem => _current;
    public double CurrentItemOffsetFraction => _fraction;
    public bool IsScrollInProgress => _scrolling;
    private double _maximum, _fraction;
    private int _current;
    private bool _scrolling;
    private CarouselPanel? _panel;
    private AnimationChildScope? _animationScope;
    private CancellationTokenSource? _scrollCancellation;
    private readonly CarouselScrollPosition _scrollPosition = new();

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new CarouselItem();
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<CarouselItem>(item, out recycleKey);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelPointer(false);
        CancelScroll();
        _panel = null;
        base.OnApplyTemplate(e);
    }
    internal void SetPanel(CarouselPanel panel) => _panel = panel;
    internal void RebaseAfterMeasure(double offset, float density)
    {
        if (_scrollCancellation is not null) _scrollPosition.Rebase(offset, density);
        else SetCurrentValue(ScrollOffsetProperty, offset);
    }
    internal void Publish(double maximum, int current, double fraction)
    {
        SetAndRaise(MaximumScrollOffsetProperty, ref _maximum, maximum);
        SetAndRaise(CurrentItemProperty, ref _current, current);
        SetAndRaise(CurrentItemOffsetFractionProperty, ref _fraction, fraction);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ScrollOffsetProperty) _panel?.InvalidateArrange();
        else if (change.Property == VariantProperty || change.Property == ItemWidthProperty || change.Property == ItemSpacingProperty ||
            change.Property == MinSmallItemWidthProperty || change.Property == MaxSmallItemWidthProperty ||
            change.Property == ContentPaddingProperty || change.Property == FlowDirectionProperty)
            _panel?.InvalidateMeasure();
        if (change.Property == IsEffectivelyEnabledProperty || change.Property == UserScrollEnabledProperty || change.Property == FlowDirectionProperty)
            CancelPointer(true, afterConfigurationCommit: true);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _focusedChild = null;
        CancelPointer(false);
        CancelScroll();
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>Immediately brings an item to its focal position. Indices are clamped to the current collection.</summary>
    public void ScrollToItem(int index)
    {
        VerifyAccess();
        CancelPointer(false);
        CancelScroll();
        var panel = RequirePanel();
        SetCurrentValue(ScrollOffsetProperty, panel.OffsetForItem(index));
    }

    /// <summary>Animates to the item's focal position using Compose's default float spring unless a spec is supplied.</summary>
    public Task AnimateScrollToItemAsync(int index, IAnimationSpec<float>? animationSpec = null, CancellationToken cancellationToken = default)
        => AnimateScroll(index, animationSpec, cancellationToken, usePagerDistance: false);

    private Task AnimateScroll(int index, IAnimationSpec<float>? animationSpec, CancellationToken cancellationToken, bool usePagerDistance)
    {
        VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        var panel = RequirePanel();
        CancelPointer(false);
        CancelScroll();
        index = Math.Clamp(index, 0, Math.Max(0, ItemCount - 1));
        if (panel.PreJumpPosition(index, ScrollOffset) is { } preJump)
        {
            SetCurrentValue(ScrollOffsetProperty, panel.OffsetForItem(preJump));
            panel.InvalidateArrange();
        }
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var initial = (float)Math.Clamp(ScrollOffset, 0, MaximumScrollOffset) * density;
        var displacement = panel.ScrollDistanceToItem(index, initial, usePagerDistance);
        if (displacement == 0) return Task.CompletedTask;
        _scrollPosition.Rebase(initial / density, density);
        var scope = AnimationScope.Get(this).CreateChild("Carousel programmatic scroll");
        _animationScope = scope;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scrollCancellation = cancellation;
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true);
        return Run();

        async Task Run()
        {
            try
            {
                // A presentation contribution preserves the underlying binding while the scroll runs.
                using var binding = scope.BindAnimatedValue(this, ScrollOffsetProperty, _scrollPosition);
                var previous = 0f;
                await SuspendAnimation.AnimateAsync(scope, new FloatConverter(), 0, displacement,
                    animationSpec: animationSpec ?? new SpringSpec<float>(), block: (value, _) =>
                    {
                        var consumed = _scrollPosition.Consume(value - previous, (float)MaximumScrollOffset * density, panel);
                        previous += consumed;
                    }, cancellationToken: cancellation.Token);
            }
            finally
            {
                if (ReferenceEquals(_scrollCancellation, cancellation))
                {
                    SetCurrentValue(ScrollOffsetProperty, _scrollPosition.Value);
                    _scrollCancellation = null;
                    _animationScope = null;
                    SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
                }
                cancellation.Dispose();
                scope.Dispose();
            }
        }
    }

    private CarouselPanel RequirePanel() => _panel is { Strategy.IsValid: true } panel ? panel
        : throw new InvalidOperationException("Lay out the carousel at a finite width before scrolling to an item.");
    private void CancelScroll()
    {
        _bringRequests.Clear();
        _bringingIntoView = false;
        _trackingFocusedChild = false;
        _pendingSettle?.Dispose();
        _pendingSettle = null;
        var offset = ScrollOffset;
        var cancellation = _scrollCancellation;
        _scrollCancellation = null;
        cancellation?.Cancel();
        _animationScope?.Dispose();
        _animationScope = null;
        if (cancellation is not null) SetCurrentValue(ScrollOffsetProperty, offset);
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
    }

    /// <summary>Runs the configured fling with a logical velocity in DIP/s and returns unconsumed velocity.</summary>
    public Task<double> FlingAsync(double velocity, CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        CancelPointer(false);
        return Fling(velocity, 0, cancellationToken);
    }

    private Task<double> Fling(double velocity, float gestureDelta, CancellationToken cancellationToken)
    {
        VerifyAccess();
        ArgumentOutOfRangeException.ThrowIfNotEqual(double.IsFinite(velocity), true, nameof(velocity));
        cancellationToken.ThrowIfCancellationRequested();
        var panel = RequirePanel();
        CancelScroll();
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        _scrollPosition.Rebase(Math.Clamp(ScrollOffset, 0, MaximumScrollOffset), density);
        var mode = FlingMode == CarouselFlingMode.Default
            ? Variant == CarouselVariant.Uncontained ? CarouselFlingMode.NoSnap : CarouselFlingMode.SingleAdvance : FlingMode;
        var decay = DecayAnimationSpec ?? ComposeAnimation.Runtime.DecayAnimationSpec.SplineBasedDecay<float>(density);
        var snap = SnapAnimationSpec ?? new SpringSpec<float>(stiffness: 400);
        var scope = AnimationScope.Get(this).CreateChild("Carousel fling");
        _animationScope = scope;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scrollCancellation = cancellation;
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true);
        return Run();

        async Task<double> Run()
        {
            try
            {
                using var binding = scope.BindAnimatedValue(this, ScrollOffsetProperty, _scrollPosition);
                var remaining = await SnapFling.RunAsync(scope, (float)velocity * density, decay, snap,
                    delta => _scrollPosition.Consume(delta, (float)MaximumScrollOffset * density, panel),
                    (speed, decayOffset) => mode == CarouselFlingMode.NoSnap ? decayOffset
                        : panel.ApproachOffset(_scrollPosition.Position, speed, decayOffset, mode),
                    speed => mode == CarouselFlingMode.NoSnap ? 0
                        : panel.SnapOffset(_scrollPosition.Position, speed, gestureDelta, density, FlowDirection), cancellation.Token);
                return remaining / density;
            }
            finally
            {
                if (ReferenceEquals(_scrollCancellation, cancellation))
                {
                    SetCurrentValue(ScrollOffsetProperty, _scrollPosition.Value);
                    _scrollCancellation = null; _animationScope = null;
                    SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
                }
                cancellation.Dispose(); scope.Dispose();
            }
        }
    }
}

internal sealed class CarouselPanel : Panel
{
    private HorizontalCarousel? _owner;
    private float _density = 1;
    private float _pageSize, _spacing;
    private float? _layoutScroll;
    private int _layoutCurrent;
    private float _layoutFraction;
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
        var previousSnap = previousState is { } state ? Strategy!.SnapPosition(state.Current, Children.Count) : 0;
        _density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var space = (float)CarouselStrategy.Round((float)availableSize.Width * _density);
        _spacing = CarouselStrategy.Round((float)owner.ItemSpacing * _density);
        var preferred = double.IsNaN(owner.ItemWidth) ? space : (float)owner.ItemWidth * _density;
        var keylines = CarouselKeylines.Create(owner.Variant, space, preferred, _spacing, Children.Count,
            (float)owner.MinSmallItemWidth * _density, (float)owner.MaxSmallItemWidth * _density, 10 * _density);
        var padding = owner.ContentPadding;
        Strategy = new(keylines, space, _spacing, (float)padding.Left * _density, (float)padding.Right * _density);
        _layoutScroll = null;
        _pageSize = CarouselStrategy.Round(Strategy.ItemSize);
        if (previousState is { } old && (previousWidth != space / _density || previousStride != PageStride || previousDensity != _density ||
            previousSnap != Strategy.SnapPosition(old.Current, Children.Count)))
        {
            var scroll = Math.Clamp(old.Current * PageStride - CarouselStrategy.Round(
                Strategy.SnapPosition(old.Current, Children.Count) - old.Fraction * PageStride), 0, MaxOffset);
            owner.RebaseAfterMeasure(scroll / _density, _density);
            var (current, fraction) = LayoutStateAt(scroll);
            owner.Publish(MaxOffset / _density, current, fraction);
        }
        var cross = Math.Max(0, availableSize.Height - padding.Top - padding.Bottom);
        foreach (var child in Children) child.Measure(new Size(_pageSize / _density, cross));
        if (previousWidth is { } oldWidth) owner.TrackFocusOnResize(oldWidth, space / _density);
        var height = Children.Select(child => child.DesiredSize.Height).DefaultIfEmpty(0).Max() + padding.Top + padding.Bottom;
        return new(availableSize.Width, height);
    }

    internal double OffsetForItem(int index)
    {
        if (Strategy is not { IsValid: true } strategy || Children.Count == 0) return 0;
        index = Math.Clamp(index, 0, Children.Count - 1);
        return Math.Clamp(index * (_pageSize + _spacing) - strategy.SnapPosition(index, Children.Count), 0, MaxOffset) / _density;
    }
    internal int? PreJumpPosition(int target, double offset)
    {
        if (Strategy is not { IsValid: true } strategy || Children.Count == 0) return null;
        var scroll = Math.Clamp(CarouselStrategy.Round((float)offset * _density), 0, MaxOffset);
        var stride = _pageSize + _spacing;
        var first = Math.Clamp((int)(scroll / stride), 0, Children.Count - 1);
        var last = Math.Clamp((int)MathF.Ceiling((scroll + strategy.AvailableSpace) / stride) - 1, first, Children.Count - 1);
        var forward = target > first;
        if (((forward && target > last) || (!forward && target < first)) && Math.Abs(target - first) >= 3)
            return forward ? Math.Max(target - (last - first + 1), first) : Math.Min(target + (last - first + 1), first);
        return null;
    }
    internal float ScrollDistanceToItem(int target, float scroll, bool usePagerDistance)
    {
        if (Strategy is not { IsValid: true } strategy || Children.Count == 0) return 0;
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
        return (target - current) * (_pageSize + _spacing) + strategy.SnapPosition(current, Children.Count)
            - strategy.SnapPosition(target, Children.Count);
    }
    // PagerState.performScroll uses absolute page coordinates for bounds. When the
    // incremental layout cannot be reused, PagerMeasure reconstructs placement from
    // currentPage/currentPageOffsetFraction, including its half-page fraction clamp.
    internal (float Position, float Consumed, bool Coerced) ApplyScroll(float scroll, float delta)
    {
        if (Strategy is not { IsValid: true } strategy || Children.Count == 0) return (scroll, 0, true);
        var (current, fraction) = LayoutStateAt(scroll);
        var stride = PageStride;
        var absolute = current * stride + CarouselStrategy.Round(fraction * stride);
        var maximum = MaxOffset + Math.Clamp(strategy.SnapPosition(Children.Count - 1, Children.Count), 0, strategy.AvailableSpace);
        var minimum = Math.Min(maximum, Math.Clamp(strategy.SnapPosition(0, Children.Count), 0, strategy.AvailableSpace));
        var updated = absolute + delta;
        var coerced = Math.Clamp(updated, minimum, maximum);
        var consumed = coerced - absolute;
        var incremental = CanApplyScroll(scroll, consumed, fraction);
        LastScrollRemeasured = !incremental;
        var position = incremental ? scroll + consumed
            : current * stride - CarouselStrategy.Round(strategy.SnapPosition(current, Children.Count) - (fraction + consumed / stride) * stride);
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
        var last = Math.Min(Children.Count - 1, (int)MathF.Ceiling((scroll + space) / stride) - 1);
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
        if (Children.Count == 0 || Strategy is not { IsValid: true } strategy) return (0, 0);
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
        _layoutCurrent = Enumerable.Range(0, Children.Count)
            .Where(i => i * stride - scroll + stride > 0 && i * stride - scroll < strategy.AvailableSpace)
            .MinBy(i => MathF.Abs(i * stride - strategy.SnapPosition(i, Children.Count) - scroll));
        _layoutFraction = Math.Clamp((scroll - (_layoutCurrent * stride - strategy.SnapPosition(_layoutCurrent, Children.Count))) / stride, -.5f, .5f);
        _layoutScroll = scroll;
        return (_layoutCurrent, _layoutFraction);
    }
    internal float ApproachOffset(float scroll, float velocity, float decayOffset, CarouselFlingMode mode)
    {
        var stride = _pageSize + _spacing;
        if (stride == 0) return 0;
        var first = Math.Clamp((int)(scroll / stride), 0, Math.Max(0, Children.Count - 1));
        var start = velocity < 0 ? first + 1 : first;
        var target = Math.Clamp(start + (int)(decayOffset / stride), 0, Children.Count);
        var corrected = mode == CarouselFlingMode.SingleAdvance ? Math.Clamp(target, Math.Max(0, start - 1), start + 1) : target;
        corrected = Math.Clamp(corrected, 0, Children.Count);
        var approach = MathF.Max(0, MathF.Abs((corrected - start) * stride) - stride);
        return approach == 0 ? 0 : approach * Math.Sign(velocity);
    }
    internal float SnapOffset(float scroll, float velocity, float gestureDelta, float density, FlowDirection flow)
    {
        if (Strategy is not { IsValid: true } strategy || Children.Count == 0) return 0;
        var stride = _pageSize + _spacing;
        var lower = float.NegativeInfinity;
        var upper = float.PositiveInfinity;
        for (var i = 0; i < Children.Count; i++)
        {
            var position = i * stride - scroll;
            // Pager visiblePagesInfo includes the trailing spacing of a page.
            if (position + stride <= 0 || position >= strategy.AvailableSpace) continue;
            var distance = position - strategy.SnapPosition(i, Children.Count);
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
        ? MathF.Max(0, _pageSize * Children.Count + _spacing * (Children.Count - 1) - strategy.AvailableSpace) : 0;

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
        var strategyScroll = current * floatStride + fraction * floatStride - strategy.SnapPosition(current, Children.Count);
        var keylines = strategy.GetKeylines(strategyScroll, strategy.MaxScrollOffset(Children.Count));
        var padding = owner.ContentPadding;
        var height = Math.Max(0, finalSize.Height - padding.Top - padding.Bottom);
        for (var i = 0; i < Children.Count; i++)
        {
            var center = i * floatStride + strategy.ItemSize / 2 - strategyScroll;
            var before = keylines.Before(center);
            var after = keylines.After(center);
            var progress = before == after ? 1 : (center - before.UnadjustedOffset) / (after.UnadjustedOffset - before.UnadjustedOffset);
            var keyline = CarouselKeyline.Lerp(before, after, progress);
            var translation = keyline.Offset - center;
            if (before == after) translation += (center - keyline.UnadjustedOffset) / keyline.Size;
            var pageOffset = CarouselStrategy.Round(i * stride - scroll);
            // Pager places the integer page (including RTL) before carouselItem applies
            // its Float layer translation. Reassociation changes edge visibility in BIV.
            var x = owner.FlowDirection == FlowDirection.RightToLeft
                ? (float)finalSize.Width * _density - pageOffset - _pageSize - translation
                : pageOffset + translation;
            var child = Children[i];
            // Avalonia ZIndex is integer; this preserves the order/ties of 1f / (1 + distance).
            child.ZIndex = Children.Count - Math.Abs(i - current);
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

/// <summary>Current keyline mask, in DIP. The item's content retains its full layout size.</summary>
public readonly record struct CarouselItemDrawInfo(double Size, double MinSize, double MaxSize, Rect MaskRect);

/// <summary>A carousel item with an optional rounded mask and border.</summary>
public class CarouselItem : ContentControl
{
    public static readonly DirectProperty<CarouselItem, CarouselItemDrawInfo> DrawInfoProperty =
        AvaloniaProperty.RegisterDirect<CarouselItem, CarouselItemDrawInfo>(nameof(DrawInfo), o => o.DrawInfo);
    private CarouselItemDrawInfo _drawInfo;
    public CarouselItemDrawInfo DrawInfo => _drawInfo;
    private CarouselMask? _mask, _clip;
    private SurfaceOutline? _outline;
    private CornerRadius? _borderCorners, _maskCorners;
    private double _shapeDensity;
    internal void UpdateDrawInfo(CarouselItemDrawInfo info)
    {
        SetAndRaise(DrawInfoProperty, ref _drawInfo, info);
        UpdateMask();
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _mask = e.NameScope.Find<CarouselMask>("PART_Mask");
        _clip = e.NameScope.Find<CarouselMask>("PART_Clip");
        _outline = e.NameScope.Find<SurfaceOutline>("PART_Outline");
        _borderCorners = null;
        _maskCorners = null;
        UpdateMask();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CornerRadiusProperty || change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty)
            UpdateMask();
    }
    private void UpdateMask()
    {
        if (_mask is null || _outline is null) return;
        // carouselItem's outer graphics layer clips both user content and maskBorder
        // to the rectangular keyline bounds. maskClip is the separate rounded inner shape.
        if (_clip is not null) _clip.Clip = DrawInfo.MaskRect == new Rect(Bounds.Size) ? null : new RectangleGeometry(DrawInfo.MaskRect);
        var rect = DrawInfo.MaskRect.Intersect(new Rect(Bounds.Size));
        if (rect.Width < 0 || rect.Height < 0) return;
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        if (_shapeDensity != density) { _shapeDensity = density; _maskCorners = null; _borderCorners = null; }
        // CarouselItemScope.rememberMaskShape keys on draw-info identity and density, not shape.
        _maskCorners ??= CornerRadius;
        _mask.Clip = Foundation.Rendering.RoundedRectangleGeometry.Create(rect, _maskCorners.Value);
        _outline.Width = rect.Width;
        _outline.Height = rect.Height;
        _outline.PathLayoutSize = Bounds.Size;
        _outline.PathBounds = rect;
        _outline.RenderTransform = new TranslateTransform(rect.X, rect.Y);
        if (BorderBrush is null || BorderThickness == default) _borderCorners = null;
        else _borderCorners ??= CornerRadius;
        _outline.CornerRadius = _borderCorners ?? CornerRadius;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _maskCorners = null; _borderCorners = null;
        base.OnDetachedFromVisualTree(e);
    }
}

/* Portions translated from AndroidX TabRow.kt, Copyright 2022 The Android Open Source Project.
 * Apache License 2.0. Fixed source: docs/upstream-baseline.json. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class TabRowChrome : Panel
{
    public TabRowChrome()
    {
        AddHandler(PointerWheelChangedEvent, (_, e) =>
        {
            if (Scrollable && (e.Delta.X != 0 || e.Delta.Y != 0 && e.KeyModifiers.HasFlag(KeyModifiers.Shift))) Content.InterruptScroll();
        }, RoutingStrategies.Tunnel);
        AddHandler(ScrollGestureEvent, (_, e) =>
        {
            if (Scrollable && e.Delta.X != 0) Content.InterruptScroll();
        }, RoutingStrategies.Bubble, handledEventsToo: true);
    }
    public static readonly StyledProperty<Control?> OwnerProperty = AvaloniaProperty.Register<TabRowChrome, Control?>(nameof(Owner));
    public static readonly StyledProperty<IBrush?> ContentColorProperty = AvaloniaProperty.Register<TabRowChrome, IBrush?>(nameof(ContentColor));
    public Control? Owner { get => GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }
    public IBrush? ContentColor { get => GetValue(ContentColorProperty); set => SetValue(ContentColorProperty, value); }
    private Control? _observedOwner;
    internal bool OwnerAttached => _observedOwner is not null;
    internal bool Scrollable => Owner is { } owner && TabRowAppearance.GetIsScrollable(owner);
    internal double EdgePadding => Owner is { } owner ? TabRowAppearance.GetEdgePadding(owner) : 0;
    internal double MinTabWidth => Owner is { } owner ? TabRowAppearance.GetMinTabWidth(owner) : 0;
    internal bool Primary => Owner is not { } owner || TabRowAppearance.GetVariant(owner) == TabRowVariant.Primary;
    internal int SelectedIndex => Owner switch { TabRow row => row.SelectedIndex, TabControl native => native.SelectedIndex, _ => 0 };
    internal ScrollViewer Scroller => (ScrollViewer)Children[1];
    internal TabRowContent Content => (TabRowContent)Scroller.Content!;
    internal Control Indicator => Children[2];
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _observedOwner = Owner;
        if (_observedOwner is { } owner) owner.PropertyChanged += OwnerChanged;
        Scroller.ScrollChanged += ScrollChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // TemplateBinding can clear Owner before the visual detach notification.
        if (_observedOwner is { } owner) { owner.PropertyChanged -= OwnerChanged; owner.ClearValue(TabRowAppearance.TabPositionsProperty); }
        _observedOwner = null;
        Scroller.ScrollChanged -= ScrollChanged;
        base.OnDetachedFromVisualTree(e);
    }
    private void ScrollChanged(object? sender, ScrollChangedEventArgs e) => InvalidateArrange();
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TabRow.SelectedIndexProperty || e.Property == SelectingItemsControl.SelectedIndexProperty ||
            e.Property == TabRowAppearance.VariantProperty || e.Property == TabRowAppearance.IsScrollableProperty ||
            e.Property == TabRowAppearance.EdgePaddingProperty || e.Property == TabRowAppearance.MinTabWidthProperty)
        {
            Content.InvalidateMeasure();
            Content.GetVisualDescendants().OfType<TabRowItemsPanel>().FirstOrDefault()?.InvalidateMeasure();
            InvalidateMeasure();
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return default;
        ScrollViewer.SetHorizontalScrollBarVisibility(Scroller, Scrollable ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled);
        Children[1].Measure(availableSize);
        Children[0].Measure(new Size(availableSize.Width, Children[1].DesiredSize.Height));
        // Empty fixed rows have zero height even though the divider has an independent height.
        return Children[1].DesiredSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var height = Content.LayoutHeight;
        var dy = (MathUtils.DipToPx(finalSize.Height, scale) - MathUtils.DipToPx(height, scale)) / 2 / scale;
        Children[0].ZIndex = Scrollable ? -1 : 1;
        Children[2].ZIndex = 2;
        Children[0].Arrange(new Rect(0, (Scrollable ? finalSize.Height : dy + height) - Children[0].DesiredSize.Height, finalSize.Width, Children[0].DesiredSize.Height));
        Children[1].Arrange(new Rect(finalSize));
        Content.ArrangeIndicator(finalSize);
        Content.UpdateScroll();
        return finalSize;
    }
}

internal sealed class TabRowItemsPanel : StackPanel
{
    public TabRowItemsPanel() => Orientation = Orientation.Horizontal;
    internal TabPosition[] Positions { get; private set; } = [];
    internal double LayoutHeight => _height / LayoutHelper.GetLayoutScale(this);
    private int[] _widths = [];
    private int _height, _padding;
    private bool _measuringHeaders;
    internal bool IntrinsicTargetsPending { get; set; }
    internal void HeaderMeasureInvalidated()
    {
        if (!_measuringHeaders) { IntrinsicTargetsPending = true; InvalidateMeasure(); }
    }
    protected override void OnMeasureInvalidated()
    {
        base.OnMeasureInvalidated();
        // Content can change an intrinsic/indicator width without changing the row's
        // allocated size. Keep the enclosing offset node in the same measure pass.
        foreach (var ancestor in this.GetVisualAncestors().OfType<Control>())
        {
            ancestor.InvalidateMeasure();
            if (ancestor is TabRowContent) break;
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _measuringHeaders = true;
        try
        {
            var chrome = this.GetVisualAncestors().OfType<TabRowChrome>().First();
            var scale = LayoutHelper.GetLayoutScale(this);
            int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
            foreach (var presenter in Children.OfType<ContentPresenter>()) presenter.UpdateChild();
            foreach (var child in Children.OfType<TemplatedControl>()) child.ApplyTemplate();
            // Native TabItem's template content becomes visual during its first measure.
            // Realize it before asking the Material header for intrinsic dimensions.
            foreach (var child in Children) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var headers = Children.Select(child => child.GetVisualDescendants().OfType<TabHeader>().FirstOrDefault()).ToArray();
            var rowWidth = double.IsFinite(availableSize.Width) ? Px(availableSize.Width) : 0;
            var width = chrome.Scrollable ? int.MaxValue : Children.Count == 0 ? 0 : rowWidth / Children.Count;
            _height = 0;
            for (var index = 0; index < Children.Count; index++)
            {
                if (headers[index] is { } header) _height = Math.Max(_height, header.IntrinsicHeight(width, scale));
                else { Children[index].Measure(new Size(width == int.MaxValue ? double.PositiveInfinity : width / scale, double.PositiveInfinity)); _height = Math.Max(_height, Px(Children[index].DesiredSize.Height)); }
            }
            _padding = chrome.Scrollable ? Px(chrome.EdgePadding) : 0;
            _widths = new int[Children.Count]; Positions = new TabPosition[Children.Count];
            var left = chrome.Scrollable ? (float)chrome.EdgePadding : 0f;
            var extent = _padding * 2;
            for (var index = 0; index < Children.Count; index++)
            {
                var intrinsic = headers[index]?.IntrinsicWidth(chrome.Scrollable ? int.MaxValue : _height, scale) ?? Px(Children[index].DesiredSize.Width);
                var itemWidth = chrome.Scrollable ? Math.Max(Px(chrome.MinTabWidth), intrinsic) : width;
                var contentWidth = Math.Max((chrome.Scrollable ? intrinsic : Math.Min(intrinsic, width)) / (float)scale - 32f, 24f);
                _widths[index] = itemWidth;
                // Intrinsic probes measure the presenters with different constraints. Replay
                // the real measure through every template wrapper, including Surface's Border,
                // even when the allocated tab size itself did not change.
                if (headers[index] is { } measuredHeader)
                {
                    measuredHeader.InvalidateMeasure();
                    foreach (var ancestor in measuredHeader.GetVisualAncestors().OfType<Control>().TakeWhile(control => control != Children[index]))
                        ancestor.InvalidateMeasure();
                }
                Children[index].InvalidateMeasure();
                Children[index].Measure(new Size(itemWidth / scale, _height / scale));
                Positions[index] = new TabPosition(left, itemWidth / (float)scale, contentWidth);
                left += itemWidth / (float)scale; extent += itemWidth;
            }
            if (chrome.OwnerAttached && chrome.Owner is { } owner && !TabRowAppearance.GetTabPositions(owner).SequenceEqual(Positions))
                owner.SetValue(TabRowAppearance.TabPositionsProperty, Array.AsReadOnly(Positions));
            return new Size((chrome.Scrollable ? extent : rowWidth) / scale, _height / scale);
        }
        finally { _measuringHeaders = false; }
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var left = _padding;
        for (var index = 0; index < Children.Count; index++)
        {
            Children[index].Arrange(new Rect(left / scale, 0, _widths[index] / scale, _height / scale));
            left += _widths[index];
        }
        return finalSize;
    }
}

// The native ScrollViewer carries wheel/drag input. The Material offset/width and automatic
// centering tracks remain here, with DefaultSpatial and upstream pixel rounding.
internal sealed class TabRowContent : Panel
{
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> IndicatorAnimationProperty = AvaloniaProperty.Register<TabRowContent, Foundation.Motion.SpringSpec>(nameof(IndicatorAnimation));
    public Foundation.Motion.SpringSpec IndicatorAnimation { get => GetValue(IndicatorAnimationProperty); set => SetValue(IndicatorAnimationProperty, value); }
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> ScrollAnimationProperty = AvaloniaProperty.Register<TabRowContent, Foundation.Motion.SpringSpec>(nameof(ScrollAnimation));
    public Foundation.Motion.SpringSpec ScrollAnimation { get => GetValue(ScrollAnimationProperty); set => SetValue(ScrollAnimationProperty, value); }

    private AnimationScope? _motion;
    private Animatable<float>? _offset, _width, _scroll;
    private int? _selectedForScroll;
    private float _scrollPrevious, _scrollFraction;
    private bool _resetScroll;
    private int _scrollGeneration;
    private CancellationTokenSource? _scrollRun;
    private readonly Dictionary<Animatable<float>, float> _requested = [];
    private TabRowChrome Chrome => this.GetVisualAncestors().OfType<TabRowChrome>().First();
    internal float IndicatorOffset => _offset?.Value ?? 0;
    internal float IndicatorWidth => _width?.Value ?? 0;
    internal double LayoutHeight { get; private set; }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _motion = AnimationScope.Get(this);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IndicatorAnimationProperty || change.Property == ScrollAnimationProperty)
        {
            InvalidateMeasure();
            InvalidateArrange();
        }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _motion = null; _offset = _width = _scroll = null; _selectedForScroll = null; _requested.Clear();
        _scrollPrevious = _scrollFraction = 0; _scrollGeneration++;
        _scrollRun?.Cancel(); _scrollRun?.Dispose(); _scrollRun = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 1) return default;
        Children[0].Measure(availableSize);
        var desired = Children[0].DesiredSize;
        var chrome = Chrome;
        var items = this.GetVisualDescendants().OfType<TabRowItemsPanel>().FirstOrDefault();
        var positions = items?.Positions ?? [];
        var intrinsicChange = items?.IntrinsicTargetsPending == true;
        if (items is not null) items.IntrinsicTargetsPending = false;
        LayoutHeight = items?.LayoutHeight ?? desired.Height;
        if (positions.Length == 0 || chrome.SelectedIndex < 0 || chrome.SelectedIndex >= positions.Length)
        {
            chrome.Indicator.IsVisible = false;
            return desired;
        }
        chrome.Indicator.IsVisible = true;
        var position = positions[chrome.SelectedIndex];
        var targetWidth = (float)(chrome.Primary ? position.ContentWidth : position.Width);
        if (this.IsAttachedToVisualTree() && _motion is not null)
        {
            var motion = _motion = AnimationScope.Get(this);
            if (_offset is null)
            {
                _offset = new Animatable<float>(new FloatConverter(), (float)position.Left, VisibilityThresholds.Float);
                _width = new Animatable<float>(new FloatConverter(), targetWidth, VisibilityThresholds.Float);
                motion.SubscribeValue(_offset, _ => InvalidateMeasure());
                motion.SubscribeValue(_width, _ => InvalidateMeasure());
            }
            Animate(_width!, targetWidth, "Tab indicator width", intrinsicChange);
            Animate(_offset, (float)position.Left, "Tab indicator offset", intrinsicChange);
        }
        var scale = LayoutHelper.GetLayoutScale(this);
        chrome.Indicator.Measure(new Size(Math.Max(0, MathUtils.DipToPx(_width?.Value ?? targetWidth, (float)scale)) / scale, LayoutHeight));
        return desired;
    }
    private void Animate(Animatable<float> track, float target, string label, bool intrinsicChange)
    {
        if (!this.IsAttachedToVisualTree() || _motion is null || (_requested.TryGetValue(track, out var requested) ? requested : track.TargetValue) == target) return;
        var motion = _motion = AnimationScope.Get(this);
        var spec = IndicatorAnimation;
        if (spec.Stiffness <= 0) return;
        _requested[track] = target;
        // TabIndicatorOffsetNode launches animateTo directly; interruption retains velocity.
        _ = motion.Launch(async ct =>
        {
            // Apply the measure-triggered coroutine after the corresponding composition
            // frame. Its first animation frame is the next one, as in the Desktop node.
            await motion.NextFrameAsync(ct);
            // FixedTabRow queries intrinsics before measuring tabs; its offset node
            // observes the TabPositions write on the next layout frame. ScrollableTabRow
            // measures the tabs before querying intrinsics and measuring the indicator.
            if (intrinsicChange && !Chrome.Scrollable) await motion.NextFrameAsync(ct);
            if (_requested[track] != target) return;
            await track.AnimateToAsync(motion, target, new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.1f), cancellationToken: ct);
        }, label);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var dy = (MathUtils.DipToPx(finalSize.Height, scale) - MathUtils.DipToPx(LayoutHeight, scale)) / 2 / scale;
        Children[0].Arrange(new Rect(0, dy, finalSize.Width, LayoutHeight));
        if (Chrome.Bounds.Width > 0) ArrangeIndicator(Chrome.Bounds.Size);
        return finalSize;
    }
    internal void ArrangeIndicator(Size finalSize)
    {
        var chrome = Chrome;
        var positions = this.GetVisualDescendants().OfType<TabRowItemsPanel>().FirstOrDefault()?.Positions ?? [];
        if (positions.Length == 0 || chrome.SelectedIndex < 0 || chrome.SelectedIndex >= positions.Length) return;
        var scale = LayoutHelper.GetLayoutScale(this);
        var position = positions[chrome.SelectedIndex];
        var width = MathUtils.DipToPx(IndicatorWidth, (float)scale) / scale;
        // Fixed TabRow places the offset node at start zero; scrollable rows center the
        // node in the selected tab before its animated start offset is applied.
        // Compose coerces the offset node's reported width to the outer constraints.
        // Its apparent-to-real offset centers the actual width inside that allocation.
        var tabPixels = MathUtils.DipToPx(position.Width, scale);
        var indicatorPixels = MathUtils.DipToPx(width, scale);
        // ScrollableTabRow constrains the offset node to ContentWidth, including the
        // secondary indicator. Its reported width and its actual animated width differ
        // during overshoot. Keep the two integer centering operations separate.
        var reportedPixels = chrome.Scrollable ? Math.Min(indicatorPixels, MathUtils.DipToPx(position.ContentWidth, scale)) : tabPixels;
        var outerCenter = chrome.Scrollable ? Math.Max(0, (tabPixels - reportedPixels) / 2) : 0;
        var coercion = (reportedPixels - indicatorPixels) / 2;
        var center = (outerCenter + (FlowDirection == FlowDirection.RightToLeft ? reportedPixels - indicatorPixels - coercion : coercion)) / scale;
        var dy = (MathUtils.DipToPx(finalSize.Height, scale) - MathUtils.DipToPx(LayoutHeight, scale)) / 2 / scale;
        chrome.Indicator.Arrange(new Rect(center + MathUtils.DipToPx(IndicatorOffset, (float)scale) / scale - chrome.Scroller.Offset.X,
            dy + LayoutHeight - chrome.Indicator.DesiredSize.Height, width, chrome.Indicator.DesiredSize.Height));
    }
    internal void UpdateScroll()
    {
        var chrome = Chrome;
        var positions = this.GetVisualDescendants().OfType<TabRowItemsPanel>().FirstOrDefault()?.Positions ?? [];
        if (positions.Length == 0 || chrome.SelectedIndex < 0 || chrome.SelectedIndex >= positions.Length) return;
        var scale = LayoutHelper.GetLayoutScale(this);
        var position = positions[chrome.SelectedIndex];
        var spec = ScrollAnimation;
        if (spec.Stiffness <= 0) return;
        if (chrome.Scrollable && _selectedForScroll != chrome.SelectedIndex && this.IsAttachedToVisualTree() && _motion is not null && chrome.Scroller.Viewport.Width > 0)
        {
            var motion = _motion = AnimationScope.Get(this);
            var firstLayout = _selectedForScroll is null;
            _selectedForScroll = chrome.SelectedIndex;
            var generation = ++_scrollGeneration;
            var scroller = chrome.Scroller;
            var total = MathUtils.DipToPx(positions[^1].Right, scale) + MathUtils.DipToPx(chrome.EdgePadding, scale);
            var visible = MathUtils.DipToPx(scroller.Viewport.Width, scale);
            var target = Math.Clamp(MathUtils.DipToPx(position.Left, scale) - (visible / 2 - MathUtils.DipToPx(position.Width, scale) / 2), 0, Math.Max(0, total - visible));
            if (_scroll is null)
            {
                _scroll = new Animatable<float>(new FloatConverter(), 0f, VisibilityThresholds.Float);
                motion.SubscribeValue(_scroll, current =>
                {
                    if (_resetScroll) return;
                    // ScrollExtensions.animateScrollBy + ScrollState's Float accumulator.
                    // Keep the integer physical scroll position and residual independently.
                    var delta = current - _scrollPrevious;
                    var value = MathUtils.DipToPx(scroller.Offset.X, scale);
                    var absolute = value + delta + _scrollFraction;
                    var maximum = Math.Max(0, MathUtils.DipToPx(scroller.Extent.Width - scroller.Viewport.Width, scale));
                    var next = Math.Clamp(absolute, 0f, maximum);
                    var consumed = next - value;
                    var integer = MathUtils.RoundToInt(consumed);
                    _scrollFraction = consumed - integer;
                    _scrollPrevious += absolute != next ? consumed : delta;
                    scroller.Offset = new Vector((value + integer) / scale, 0);
                });
            }
            _ = motion.Launch(async ct =>
            {
                // The mount's onLaidOut coroutine is already scheduled before the first
                // frame. Later selection changes run after their composition frame; let
                // the outgoing animation consume that frame before MutatorMutex cancellation.
                if (!firstLayout) await motion.NextFrameAsync(ct);
                if (generation != _scrollGeneration) return;
                _resetScroll = true;
                try
                {
                    await _scroll.StopAsync();
                    if (_scroll is null) return;
                    await _scroll.SnapToAsync(0f);
                }
                finally { _resetScroll = false; }
                if (_scroll is null) return;
                _scrollPrevious = 0;
                ct.ThrowIfCancellationRequested();
                _scrollRun?.Dispose(); _scrollRun = CancellationTokenSource.CreateLinkedTokenSource(ct);
                await _scroll.AnimateToAsync(motion, target - MathUtils.DipToPx(scroller.Offset.X, scale),
                    new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f), cancellationToken: _scrollRun.Token);
            }, "Tab scroll target");
        }
    }
    internal void InterruptScroll()
    {
        // ScrollState's UserInput mutation cancels automatic centering, which uses the
        // default priority. A later selected-index change is allowed to center again.
        _scrollGeneration++;
        _scrollRun?.Cancel();
    }
}

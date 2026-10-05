using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// WideNavigationRailLayout: four animateDpAsState tracks and its measure policy.
// Width decisions retain Compose's integer pixels. Vertical motion is continuous,
// with the original integer geometry retained at its endpoints.
internal sealed class WideNavigationRailLayout : Panel
{
    public static readonly StyledProperty<double> CollapsedContainerWidthProperty = AvaloniaProperty.Register<WideNavigationRailLayout, double>(nameof(CollapsedContainerWidth));
    public double CollapsedContainerWidth { get => GetValue(CollapsedContainerWidthProperty); set => SetValue(CollapsedContainerWidthProperty, value); }
    public static readonly StyledProperty<double> ExpandedContainerMinWidthProperty = AvaloniaProperty.Register<WideNavigationRailLayout, double>(nameof(ExpandedContainerMinWidth));
    public double ExpandedContainerMinWidth { get => GetValue(ExpandedContainerMinWidthProperty); set => SetValue(ExpandedContainerMinWidthProperty, value); }
    public static readonly StyledProperty<double> ExpandedContainerMaxWidthProperty = AvaloniaProperty.Register<WideNavigationRailLayout, double>(nameof(ExpandedContainerMaxWidth));
    public double ExpandedContainerMaxWidth { get => GetValue(ExpandedContainerMaxWidthProperty); set => SetValue(ExpandedContainerMaxWidthProperty, value); }
    public static readonly StyledProperty<double> CollapsedItemVerticalSpaceProperty = AvaloniaProperty.Register<WideNavigationRailLayout, double>(nameof(CollapsedItemVerticalSpace));
    public double CollapsedItemVerticalSpace { get => GetValue(CollapsedItemVerticalSpaceProperty); set => SetValue(CollapsedItemVerticalSpaceProperty, value); }
    public static readonly StyledProperty<double> CollapsedItemMinHeightProperty = AvaloniaProperty.Register<WideNavigationRailLayout, double>(nameof(CollapsedItemMinHeight));
    public double CollapsedItemMinHeight { get => GetValue(CollapsedItemMinHeightProperty); set => SetValue(CollapsedItemMinHeightProperty, value); }
    public static readonly StyledProperty<double> HeaderSpaceProperty = AvaloniaProperty.Register<WideNavigationRailLayout, double>(nameof(HeaderSpace));
    public double HeaderSpace { get => GetValue(HeaderSpaceProperty); set => SetValue(HeaderSpaceProperty, value); }
    public static readonly StyledProperty<double> ItemHorizontalPaddingProperty = AvaloniaProperty.Register<WideNavigationRailLayout, double>(nameof(ItemHorizontalPadding));
    public double ItemHorizontalPadding { get => GetValue(ItemHorizontalPaddingProperty); set => SetValue(ItemHorizontalPaddingProperty, value); }
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> LayoutAnimationProperty = AvaloniaProperty.Register<WideNavigationRailLayout, Foundation.Motion.SpringSpec>(nameof(LayoutAnimation));
    public Foundation.Motion.SpringSpec LayoutAnimation { get => GetValue(LayoutAnimationProperty); set => SetValue(LayoutAnimationProperty, value); }
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> ModalWidthAnimationProperty = AvaloniaProperty.Register<WideNavigationRailLayout, Foundation.Motion.SpringSpec>(nameof(ModalWidthAnimation));
    public Foundation.Motion.SpringSpec ModalWidthAnimation { get => GetValue(ModalWidthAnimationProperty); set => SetValue(ModalWidthAnimationProperty, value); }
    internal static readonly AttachedProperty<double> MinimumItemHeightProperty =
        AvaloniaProperty.RegisterAttached<WideNavigationRailLayout, Control, double>("MinimumItemHeight", 48, inherits: true);
    internal static readonly AttachedProperty<WideNavigationRailVerticalValue?> ItemHeightMotionProperty =
        AvaloniaProperty.RegisterAttached<WideNavigationRailLayout, Control, WideNavigationRailVerticalValue?>("ItemHeightMotion", inherits: true);
    public static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<WideNavigationRailLayout, bool>(nameof(Expanded));
    public static readonly StyledProperty<bool> IsModalProperty = AvaloniaProperty.Register<WideNavigationRailLayout, bool>(nameof(IsModal));
    public static readonly StyledProperty<NavigationRailArrangement> ArrangementProperty =
        AvaloniaProperty.Register<WideNavigationRailLayout, NavigationRailArrangement>(nameof(Arrangement), NavigationRailArrangement.Top);
    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<WideNavigationRailLayout, Thickness>(nameof(ContentPadding));

    public bool Expanded { get => GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }
    public bool IsModal { get => GetValue(IsModalProperty); set => SetValue(IsModalProperty, value); }
    public NavigationRailArrangement Arrangement { get => GetValue(ArrangementProperty); set => SetValue(ArrangementProperty, value); }
    public Thickness ContentPadding { get => GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }

    private VisualAnimationScope? _scope;
    private Animatable<float>? _minWidth, _widthFullRange, _itemSpacing, _itemHeight;
    private bool _pending;
    private float _minimumWidth, _fullWidth, _spacing, _minimumHeight;
    private int _currentWidth, _actualMaxExpandedWidth;
    private WideNavigationRailItemsPanel? _items;

    static WideNavigationRailLayout() => AffectsMeasure<WideNavigationRailLayout>(ExpandedProperty, IsModalProperty, ArrangementProperty, ContentPaddingProperty, CollapsedContainerWidthProperty, ExpandedContainerMinWidthProperty, ExpandedContainerMaxWidthProperty, CollapsedItemVerticalSpaceProperty, CollapsedItemMinHeightProperty, HeaderSpaceProperty, ItemHorizontalPaddingProperty);
    public WideNavigationRailLayout()
    {
        UseLayoutRounding = false;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_scope is not null) return;
        _scope = VisualAnimationScope.Create(this);
        _minimumWidth = (float)(Expanded ? ExpandedContainerMinWidth : CollapsedContainerWidth);
        _fullWidth = (float)(Expanded ? ExpandedContainerMaxWidth : CollapsedContainerWidth);
        _spacing = (float)(Expanded ? 0 : CollapsedItemVerticalSpace);
        _minimumHeight = (float)(Expanded ? 48 : CollapsedItemMinHeight);
        _minWidth = _scope.CreateAnimatable(_minimumWidth);
        _widthFullRange = _scope.CreateAnimatable(_fullWidth);
        _itemSpacing = _scope.CreateAnimatable(_spacing);
        _itemHeight = _scope.CreateAnimatable(_minimumHeight);
        _scope.SubscribeValue(_minWidth, value => { _minimumWidth = value; InvalidateMeasure(); });
        _scope.SubscribeValue(_widthFullRange, value => { _fullWidth = value; InvalidateMeasure(); });
        _scope.SubscribeValue(_itemSpacing, value => { _spacing = value; InvalidateMeasure(); });
        _scope.SubscribeValue(_itemHeight, value => { _minimumHeight = value; InvalidateMeasure(); });
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _scope?.Dispose();
        _scope = null;
        _minWidth = _widthFullRange = _itemSpacing = _itemHeight = null;
        _pending = false;
        _currentWidth = _actualMaxExpandedWidth = 0;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LayoutAnimationProperty || change.Property == ModalWidthAnimationProperty
            || change.Property == ExpandedProperty || change.Property == IsModalProperty
            || change.Property == CollapsedContainerWidthProperty || change.Property == ExpandedContainerMinWidthProperty || change.Property == ExpandedContainerMaxWidthProperty || change.Property == CollapsedItemVerticalSpaceProperty || change.Property == CollapsedItemMinHeightProperty || change.Property == HeaderSpaceProperty || change.Property == ItemHorizontalPaddingProperty) UpdateTargets();
    }
    private void UpdateTargets()
    {
        if (_scope is not { IsEnding: false } scope || _pending) return;
        _pending = true;
        _ = scope.Launch(async ct =>
        {
            // Modal's target is derived from a position animation in the current
            // frame. Its animateDpAsState effects start in that recomposition,
            // so a second frame gate here would delay all four tracks.
            if (!IsModal) await scope.Clock.NextFrameAsync(ct);
            _pending = false;
            var spatial = LayoutAnimation;
            var widthSpec = IsModal ? ModalWidthAnimation : spatial;
            if (spatial.Stiffness <= 0 || widthSpec.Stiffness <= 0) return;
            Restart(_minWidth!, (float)(Expanded ? ExpandedContainerMinWidth : CollapsedContainerWidth), widthSpec);
            Restart(_widthFullRange!, (float)(Expanded ? ExpandedContainerMaxWidth : CollapsedContainerWidth), widthSpec);
            Restart(_itemSpacing!, (float)(Expanded ? 0 : CollapsedItemVerticalSpace), spatial);
            Restart(_itemHeight!, (float)(Expanded ? 48 : CollapsedItemMinHeight), spatial);

            void Restart(Animatable<float> track, float target, Foundation.Motion.SpringSpec spec)
            {
                if (track.TargetValue == target) return;
                _ = scope.Launch(async lifetime =>
                {
                    await track.StopAsync();
                    lifetime.ThrowIfCancellationRequested();
                    await track.AnimateToAsync(target, new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.1f), cancellationToken: lifetime);
                }, "Wide rail layout track");
            }
        }, "Wide rail layout targets");
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var layoutScale = LayoutHelper.GetLayoutScale(this);
        var density = (float)layoutScale;
        int Px(double value) => double.IsFinite(value) ? MathUtils.DipToPx(value, density) : int.MaxValue / 4;
        var maxWidth = Math.Min(Px(availableSize.Width), Px(ExpandedContainerMaxWidth - ContentPadding.Left - ContentPadding.Right));
        var owner = TemplatedParent as WideNavigationRail ?? this.FindAncestorOfType<WideNavigationRail>();
        // ItemsControl owns the item logical parents; inheritance from ItemsPanel
        // would skip these controls even though they are its visual children.
        var itemHeight = WideNavigationRailVerticalValue.FromDp(_minimumHeight,
            CollapsedItemMinHeight, 48, layoutScale);
        if (Children[1] is ItemsPresenter && owner is not null)
        {
            owner.SetValue(MinimumItemHeightProperty, itemHeight.Value / layoutScale);
            owner.SetValue(ItemHeightMotionProperty, itemHeight);
        }
        var externalMinWidth = owner is null ? 0 : double.IsNaN(owner.Width) ? owner.MinWidth : owner.Width;
        var actualMin = externalMinWidth <= 0 ? Math.Min(Px(_minimumWidth), maxWidth)
            : Math.Min(Px(Math.Max(0, externalMinWidth - owner!.WindowInsets.Left - owner.WindowInsets.Right - ContentPadding.Left - ContentPadding.Right)), maxWidth);
        var expandedMin = externalMinWidth <= 0 ? Math.Min(Px(ExpandedContainerMinWidth), maxWidth) : actualMin;
        var header = (ContentPresenter)Children[0];
        header.Measure(new Size(maxWidth / layoutScale, availableSize.Height));
        var hasHeader = header.Content is not null || header.ContentTemplate is not null;
        Children[1].Measure(new Size(maxWidth / layoutScale, availableSize.Height)); // Materialize the ItemsPanel.
        _items = Children[1].GetVisualDescendants().OfType<WideNavigationRailItemsPanel>().FirstOrDefault();
        if (_items is null) return new Size(actualMin / layoutScale, double.IsFinite(availableSize.Height) ? availableSize.Height : header.DesiredSize.Height);
        _items.Expanded = Expanded;
        _items.HeaderHeight = hasHeader ? Px(header.DesiredSize.Height) : 0;
        _items.HeaderOffset = hasHeader && header.DesiredSize.Height > 0 ? _items.HeaderHeight + Px(HeaderSpace) : 0;
        _items.GapMotion = WideNavigationRailVerticalValue.FromDp(_spacing,
            CollapsedItemVerticalSpace, 0, layoutScale);
        _items.HeightMotion = itemHeight;
        _items.ItemMaxWidth = Math.Max(Px(48), Expanded ? maxWidth - Px(ItemHorizontalPadding) : actualMin);
        _items.Arrangement = Arrangement;
        _items.TopPadding = Px(ContentPadding.Top);
        _items.BottomPadding = Px(ContentPadding.Bottom);
        _items.MeasureItems(new Size(maxWidth / layoutScale, availableSize.Height), layoutScale);
        // The upstream empty-content branch returns before remembering expanded width.
        if (_items.Children.Count == 0 && !hasHeader) return new Size(actualMin / layoutScale, double.IsFinite(availableSize.Height) ? availableSize.Height : 0);
        var width = actualMin;
        if (Expanded)
        {
            var widest = Math.Max(_items.ExpandedItemMaxWidth, Px(header.DesiredSize.Width));
            if (widest > actualMin && widest > expandedMin)
            {
                width = Math.Min(Px(_fullWidth), Math.Min(Math.Max(widest, expandedMin), maxWidth));
                _actualMaxExpandedWidth = width;
            }
        }
        else if (_actualMaxExpandedWidth > 0)
            width = Math.Clamp(Px(_fullWidth), actualMin, Math.Max(_currentWidth, actualMin));
        _currentWidth = width;
        return new Size(width / layoutScale, double.IsFinite(availableSize.Height) ? availableSize.Height : (_items.ItemsHeight + _items.HeaderOffset) / layoutScale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var header = Children[0];
        header.Arrange(new Rect(0, 0, header.DesiredSize.Width, header.DesiredSize.Height));
        Children[1].Arrange(new Rect(finalSize));
        return finalSize;
    }
}

internal sealed class WideNavigationRailItemsPanel : Panel
{
    internal bool Expanded;
    internal int HeaderHeight, HeaderOffset, ItemMaxWidth, TopPadding, BottomPadding;
    internal int ExpandedItemMaxWidth;
    internal double ItemsHeight;
    internal WideNavigationRailVerticalValue GapMotion, HeightMotion;
    internal double Spacing => GapMotion.Value;
    internal double ItemMinHeight => HeightMotion.Value;
    internal NavigationRailArrangement Arrangement = NavigationRailArrangement.Top;
    private Size _measureSize;
    private bool _prepared;

    public WideNavigationRailItemsPanel() => UseLayoutRounding = false;

    protected override void OnMeasureInvalidated()
    {
        base.OnMeasureInvalidated();
        // This panel reports the available area to the ItemsPresenter, while the
        // outer measure policy consumes each item's measured size. Forward child
        // size changes even when this panel's own DesiredSize stays constant.
        if (_prepared) this.FindAncestorOfType<WideNavigationRailLayout>()?.InvalidateMeasure();
    }

    internal void MeasureItems(Size available, double layoutScale)
    {
        var density = (float)layoutScale;
        _measureSize = available;
        _prepared = true;
        SetValue(WideNavigationRailLayout.MinimumItemHeightProperty, ItemMinHeight / layoutScale);
        SetValue(WideNavigationRailLayout.ItemHeightMotionProperty, HeightMotion);
        ExpandedItemMaxWidth = 0;
        ItemsHeight = 0;
        var horizontalPadding = this.FindAncestorOfType<WideNavigationRailLayout>()!.ItemHorizontalPadding;
        double offset = HeaderHeight;
        foreach (var child in Children)
        {
            child.Measure(new Size(Math.Min(available.Width, ItemMaxWidth / layoutScale), Math.Max(0, available.Height - offset / layoutScale)));
            var measuredWidth = MathUtils.DipToPx(child.DesiredSize.Width, density);
            // Preserve the comparison/storage order in WideNavigationRailLayout:
            // compare bare measured width, then store it plus horizontal padding.
            if (Expanded && ExpandedItemMaxWidth < measuredWidth)
                ExpandedItemMaxWidth = measuredWidth + MathUtils.DipToPx(horizontalPadding, density);
            offset = child.DesiredSize.Height * layoutScale;
            ItemsHeight += offset;
        }
        InvalidateArrange();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!_prepared)
        {
            foreach (var child in Children) child.Measure(availableSize);
            return new Size(Children.Select(child => child.DesiredSize.Width).DefaultIfEmpty().Max(),
                Children.Sum(child => child.DesiredSize.Height));
        }
        return _measureSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var layoutScale = LayoutHelper.GetLayoutScale(this);
        var density = (float)layoutScale;
        var height = finalSize.Height * layoutScale;
        var sizes = Children.Select(child => child.DesiredSize.Height * layoutScale).ToArray();
        for (var i = 0; i < sizes.Length - 1; i++) sizes[i] += Spacing;
        var center = Arrangement == NavigationRailArrangement.Center;
        var total = center ? height + TopPadding + BottomPadding : height - HeaderOffset;
        var positions = Arrangement.ArrangeContinuous(total, sizes, density);
        // Each item can override Top/Start and owns an independent position track.
        // Use the nearest endpoint combination, fading its rounding residual to zero
        // whenever an axis crosses .5. This keeps all static combinations exact without
        // enumerating 2^item-count corners or introducing another animation.
        var anchors = Children.Select(child => child.GetVisualDescendants().OfType<WideNavigationRailItemLayout>().FirstOrDefault()
            ?? child as WideNavigationRailItemLayout).ToArray();
        var endpointSizes = anchors.Select((item, i) => item?.ArrangementAnchorHeight
            ?? MathUtils.RoundToInt(sizes[i] - (i < sizes.Length - 1 ? Spacing : 0))).ToArray();
        for (var i = 0; i < endpointSizes.Length - 1; i++) endpointSizes[i] += GapMotion.Anchor;
        var weight = GapMotion.EndpointWeight * HeightMotion.EndpointWeight
            * anchors.Aggregate(1d, (value, item) => value * (item?.ArrangementAnchorWeight ?? 1));
        var endpointTotal = MathUtils.RoundToInt(total);
        var exact = Arrangement.Arrange(endpointTotal, endpointSizes, density);
        var continuous = Arrangement.ArrangeContinuous(endpointTotal, endpointSizes.Select(value => (double)value).ToArray(), density);
        var offset = center ? -TopPadding : HeaderOffset;
        for (var i = 0; i < Children.Count; i++)
            Children[i].Arrange(new Rect(0, (positions[i] + weight * (exact[i] - continuous[i]) + offset) / layoutScale,
                Children[i].DesiredSize.Width, Children[i].DesiredSize.Height));
        return finalSize;
    }
}

// A layout value derived from an existing Dp animation, not a second animation track.
// Interpolating rounded endpoints removes intermediate roundToPx steps while retaining
// the original endpoint dimensions at every render scale.
internal readonly record struct WideNavigationRailVerticalValue(double Value, int Collapsed, int Expanded, double Progress)
{
    internal int Anchor => Progress < .5 ? Collapsed : Expanded;
    internal double EndpointWeight => CornerWeight(Progress);
    internal static double CornerWeight(double progress)
    {
        var distance = 2 * Math.Abs(Math.Clamp(progress, 0, 1) - .5);
        return distance * distance * (3 - 2 * distance);
    }
    internal static WideNavigationRailVerticalValue FromDp(double value, double collapsed, double expanded, double scale)
    {
        var start = MathUtils.DipToPx((float)collapsed, (float)scale);
        var end = MathUtils.DipToPx((float)expanded, (float)scale);
        var progress = collapsed == expanded ? 0 : (value - collapsed) / (expanded - collapsed);
        return new(MathUtils.Lerp(start, end, progress), start, end, progress);
    }
}


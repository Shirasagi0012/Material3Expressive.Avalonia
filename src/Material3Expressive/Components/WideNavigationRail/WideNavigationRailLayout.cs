using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;

namespace Material3Expressive.Components;

// WideNavigationRailLayout: four animateDpAsState tracks and its measure policy.
// Width/height decisions and placements use the same physical integer pixels as Compose.
internal sealed class WideNavigationRailLayout : Panel
{
    internal static readonly AttachedProperty<double> MinimumItemHeightProperty =
        AvaloniaProperty.RegisterAttached<WideNavigationRailLayout, Control, double>("MinimumItemHeight", 48, inherits: true);
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
    private float _minimumWidth = 96, _fullWidth = 96, _spacing = 4, _minimumHeight = 64;
    private int _currentWidth, _actualMaxExpandedWidth;
    private WideNavigationRailItemsPanel? _items;

    static WideNavigationRailLayout() => AffectsMeasure<WideNavigationRailLayout>(ExpandedProperty, IsModalProperty, ArrangementProperty, ContentPaddingProperty);
    public WideNavigationRailLayout()
    {
        ResourcesChanged += (_, _) => { InvalidateMeasure(); UpdateTargets(); };
        ActualThemeVariantChanged += (_, _) => { InvalidateMeasure(); UpdateTargets(); };
    }
    internal double Metric(MaterialTokenKey<double> key, double fallback) =>
        TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is double number ? number : fallback;
    private Foundation.Motion.SpringSpec Spec(MaterialTokenKey<Foundation.Motion.SpringSpec> key, Foundation.Motion.SpringSpec fallback) =>
        TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is Foundation.Motion.SpringSpec spec ? spec : fallback;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_scope is not null) return;
        _scope = VisualAnimationScope.Create(this);
        _minimumWidth = (float)(Expanded ? Metric(MaterialTokens.Comp.WideNavigationRail.ExpandedContainerMinWidth, 220) : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedContainerWidth, 96));
        _fullWidth = (float)(Expanded ? Metric(MaterialTokens.Comp.WideNavigationRail.ExpandedContainerMaxWidth, 360) : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedContainerWidth, 96));
        _spacing = (float)(Expanded ? 0 : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedItemVerticalSpace, 4));
        _minimumHeight = (float)(Expanded ? 48 : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedItemMinHeight, 64));
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
        if (change.Property == ExpandedProperty || change.Property == IsModalProperty) UpdateTargets();
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
            var spatial = Spec(MaterialTokens.Sys.Motion.Spring.SpatialDefault, MotionScheme.Expressive.DefaultSpatial);
            var widthSpec = IsModal ? Spec(MaterialTokens.Sys.Motion.Spring.SpatialFast, MotionScheme.Expressive.FastSpatial) : spatial;
            Restart(_minWidth!, (float)(Expanded ? Metric(MaterialTokens.Comp.WideNavigationRail.ExpandedContainerMinWidth, 220) : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedContainerWidth, 96)), widthSpec);
            Restart(_widthFullRange!, (float)(Expanded ? Metric(MaterialTokens.Comp.WideNavigationRail.ExpandedContainerMaxWidth, 360) : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedContainerWidth, 96)), widthSpec);
            Restart(_itemSpacing!, (float)(Expanded ? 0 : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedItemVerticalSpace, 4)), spatial);
            Restart(_itemHeight!, (float)(Expanded ? 48 : Metric(MaterialTokens.Comp.WideNavigationRail.CollapsedItemMinHeight, 64)), spatial);

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
        var density = (float)LayoutHelper.GetLayoutScale(this);
        int Px(double value) => double.IsFinite(value) ? (int)Math.Floor(value * density + 0.5) : int.MaxValue / 4;
        var maxWidth = Math.Min(Px(availableSize.Width), Px(Metric(MaterialTokens.Comp.WideNavigationRail.ExpandedContainerMaxWidth, 360) - ContentPadding.Left - ContentPadding.Right));
        var owner = TemplatedParent as WideNavigationRail ?? this.FindAncestorOfType<WideNavigationRail>();
        // ItemsControl owns the item logical parents; inheritance from ItemsPanel
        // would skip these controls even though they are its visual children.
        if (Children[1] is ItemsPresenter) owner?.SetValue(MinimumItemHeightProperty, Px(_minimumHeight) / (double)density);
        var externalMinWidth = owner is null ? 0 : double.IsNaN(owner.Width) ? owner.MinWidth : owner.Width;
        var actualMin = externalMinWidth <= 0 ? Math.Min(Px(_minimumWidth), maxWidth)
            : Math.Min(Px(Math.Max(0, externalMinWidth - owner!.WindowInsets.Left - owner.WindowInsets.Right - ContentPadding.Left - ContentPadding.Right)), maxWidth);
        var expandedMin = externalMinWidth <= 0 ? Math.Min(Px(Metric(MaterialTokens.Comp.WideNavigationRail.ExpandedContainerMinWidth, 220)), maxWidth) : actualMin;
        var header = (ContentPresenter)Children[0];
        header.Measure(new Size(maxWidth / density, availableSize.Height));
        var hasHeader = header.Content is not null || header.ContentTemplate is not null;
        Children[1].Measure(new Size(maxWidth / density, availableSize.Height)); // Materialize the ItemsPanel.
        _items = Children[1].GetVisualDescendants().OfType<WideNavigationRailItemsPanel>().FirstOrDefault();
        if (_items is null) return new Size(actualMin / density, double.IsFinite(availableSize.Height) ? availableSize.Height : header.DesiredSize.Height);
        _items.Expanded = Expanded;
        _items.HeaderHeight = hasHeader ? Px(header.DesiredSize.Height) : 0;
        _items.HeaderOffset = hasHeader && header.DesiredSize.Height > 0 ? _items.HeaderHeight + Px(Metric(MaterialTokens.Comp.WideNavigationRail.HeaderSpace, 40)) : 0;
        _items.Spacing = Px(_spacing);
        _items.ItemMinHeight = Px(_minimumHeight);
        _items.ItemMaxWidth = Math.Max(Px(48), Expanded ? maxWidth - Px(Metric(MaterialTokens.Comp.WideNavigationRail.ItemHorizontalPadding, 20)) : actualMin);
        _items.Arrangement = Arrangement;
        _items.TopPadding = Px(ContentPadding.Top);
        _items.BottomPadding = Px(ContentPadding.Bottom);
        _items.MeasureItems(new Size(maxWidth / density, availableSize.Height), density);
        // The upstream empty-content branch returns before remembering expanded width.
        if (_items.Children.Count == 0 && !hasHeader) return new Size(actualMin / density, double.IsFinite(availableSize.Height) ? availableSize.Height : 0);
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
        return new Size(width / density, double.IsFinite(availableSize.Height) ? availableSize.Height : _items.ItemsHeight / density + _items.HeaderOffset / density);
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
    internal int HeaderHeight, HeaderOffset, Spacing, ItemMinHeight, ItemMaxWidth, TopPadding, BottomPadding;
    internal int ExpandedItemMaxWidth, ItemsHeight;
    internal NavigationRailArrangement Arrangement = NavigationRailArrangement.Top;
    private Size _measureSize;
    private bool _prepared;

    protected override void OnMeasureInvalidated()
    {
        base.OnMeasureInvalidated();
        // This panel reports the available area to the ItemsPresenter, while the
        // outer measure policy consumes each item's measured size. Forward child
        // size changes even when this panel's own DesiredSize stays constant.
        if (_prepared) this.FindAncestorOfType<WideNavigationRailLayout>()?.InvalidateMeasure();
    }

    internal void MeasureItems(Size available, float density)
    {
        _measureSize = available;
        _prepared = true;
        SetValue(WideNavigationRailLayout.MinimumItemHeightProperty, ItemMinHeight / (double)density);
        ExpandedItemMaxWidth = ItemsHeight = 0;
        var horizontalPadding = this.FindAncestorOfType<WideNavigationRailLayout>()?.Metric(MaterialTokens.Comp.WideNavigationRail.ItemHorizontalPadding, 20) ?? 20;
        var offset = HeaderHeight;
        foreach (var child in Children)
        {
            child.Measure(new Size(Math.Min(available.Width, ItemMaxWidth / density), Math.Max(0, available.Height - offset / density)));
            var measuredWidth = (int)Math.Floor(child.DesiredSize.Width * density + 0.5);
            // Preserve the comparison/storage order in WideNavigationRailLayout:
            // compare bare measured width, then store it plus horizontal padding.
            if (Expanded && ExpandedItemMaxWidth < measuredWidth)
                ExpandedItemMaxWidth = measuredWidth + (int)Math.Floor(horizontalPadding * density + 0.5);
            offset = (int)Math.Floor(child.DesiredSize.Height * density + 0.5);
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
        var density = (float)LayoutHelper.GetLayoutScale(this);
        var height = (int)Math.Floor(finalSize.Height * density + 0.5);
        var sizes = Children.Select(child => (int)Math.Floor(child.DesiredSize.Height * density + 0.5)).ToArray();
        for (var i = 0; i < sizes.Length - 1; i++) sizes[i] += Spacing;
        var center = Arrangement == NavigationRailArrangement.Center;
        var positions = Arrangement.Arrange(center ? height + TopPadding + BottomPadding : height - HeaderOffset, sizes, density);
        var offset = center ? -TopPadding : HeaderOffset;
        for (var i = 0; i < Children.Count; i++)
            Children[i].Arrange(new Rect(0, (positions[i] + offset) / density, Children[i].DesiredSize.Width, Children[i].DesiredSize.Height));
        return finalSize;
    }
}


using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// NavigationItem.kt: TopIconOrIconOnlyMeasurePolicy / StartIconMeasurePolicy and
// placeIcon / placeLabelAndTopIcon / placeLabelAndStartIcon (not AnimatedMeasurePolicy).
internal sealed class ShortNavigationBarItemLayout : Panel
{
    public static readonly StyledProperty<bool> HasLabelProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, bool>(nameof(HasLabel));
    public static readonly StyledProperty<NavigationItemIconPosition> IconPositionProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, NavigationItemIconPosition>(nameof(IconPosition));
    public static readonly StyledProperty<float> SelectionProgressProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, float>(nameof(SelectionProgress));
    public static readonly StyledProperty<double> IndicatorWidthProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, double>(nameof(IndicatorWidth), 56);
    public static readonly StyledProperty<double> IndicatorHeightProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, double>(nameof(IndicatorHeight), 32);
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, double>(nameof(IconSize), 24);
    public static readonly StyledProperty<double> StartIndicatorHeightProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, double>(nameof(StartIndicatorHeight), 40);
    public static readonly StyledProperty<double> StartIndicatorHorizontalPaddingProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, double>(nameof(StartIndicatorHorizontalPadding), 16);
    public static readonly StyledProperty<double> TopItemVerticalPaddingProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, double>(nameof(TopItemVerticalPadding), 6);
    public static readonly StyledProperty<double> IconLabelSpaceProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, double>(nameof(IconLabelSpace), 4);
    public static readonly StyledProperty<CornerRadius> IndicatorShapeProperty = AvaloniaProperty.Register<ShortNavigationBarItemLayout, CornerRadius>(nameof(IndicatorShape), new CornerRadius(double.PositiveInfinity));
    public bool HasLabel { get => GetValue(HasLabelProperty); set => SetValue(HasLabelProperty, value); }
    public NavigationItemIconPosition IconPosition { get => GetValue(IconPositionProperty); set => SetValue(IconPositionProperty, value); }
    public float SelectionProgress { get => GetValue(SelectionProgressProperty); set => SetValue(SelectionProgressProperty, value); }
    public double IndicatorWidth { get => GetValue(IndicatorWidthProperty); set => SetValue(IndicatorWidthProperty, value); }
    public double IndicatorHeight { get => GetValue(IndicatorHeightProperty); set => SetValue(IndicatorHeightProperty, value); }
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public double StartIndicatorHeight { get => GetValue(StartIndicatorHeightProperty); set => SetValue(StartIndicatorHeightProperty, value); }
    public double StartIndicatorHorizontalPadding { get => GetValue(StartIndicatorHorizontalPaddingProperty); set => SetValue(StartIndicatorHorizontalPaddingProperty, value); }
    public double TopItemVerticalPadding { get => GetValue(TopItemVerticalPaddingProperty); set => SetValue(TopItemVerticalPaddingProperty, value); }
    public double IconLabelSpace { get => GetValue(IconLabelSpaceProperty); set => SetValue(IconLabelSpaceProperty, value); }
    public CornerRadius IndicatorShape { get => GetValue(IndicatorShapeProperty); set => SetValue(IndicatorShapeProperty, value); }
    internal Size MinimumSize { get; set; }
    private Rect _icon, _label, _indicator, _ripple;
    private Size _inner;
    private double HorizontalPadding => IconPosition == NavigationItemIconPosition.Top ? (IndicatorWidth - IconSize) / 2 : StartIndicatorHorizontalPadding;
    private double VerticalPadding => ((IconPosition == NavigationItemIconPosition.Top ? IndicatorHeight : StartIndicatorHeight) - IconSize) / 2;

    static ShortNavigationBarItemLayout()
    {
        AffectsMeasure<ShortNavigationBarItemLayout>(HasLabelProperty, IconPositionProperty, SelectionProgressProperty, IndicatorWidthProperty,
            IndicatorHeightProperty, IconSizeProperty, StartIndicatorHeightProperty, StartIndicatorHorizontalPaddingProperty, TopItemVerticalPaddingProperty, IconLabelSpaceProperty);
        AffectsArrange<ShortNavigationBarItemLayout>(IndicatorShapeProperty);
    }

    protected override void OnMeasureInvalidated()
    {
        base.OnMeasureInvalidated();
        // Allocation fixes DesiredSize, so ordinary upward size-change propagation
        // cannot notify the bar when a content slot's intrinsic dimensions change.
        this.GetVisualAncestors().OfType<ShortNavigationBarItemsPanel>().FirstOrDefault()?.InvalidateMeasure();
    }

    internal int IntrinsicHeight(int width, double scale)
    {
        Children[2].IsVisible = HasLabel;
        Children[1].Measure(new Size(width / scale, double.PositiveInfinity));
        Children[2].Measure(new Size(width / scale, double.PositiveInfinity));
        var iconHeight = MathUtils.DipToPx(Children[1].DesiredSize.Height, scale);
        var labelHeight = HasLabel ? MathUtils.DipToPx(Children[2].DesiredSize.Height, scale) : 0;
        // The top policy includes these paddings even without a label.
        var content = IconPosition == NavigationItemIconPosition.Start && HasLabel
            ? Math.Max(iconHeight, labelHeight) + MathUtils.DipToPx((float)(VerticalPadding * 2), (float)scale)
            : iconHeight + labelHeight + MathUtils.DipToPx((float)(TopItemVerticalPadding * 2 + VerticalPadding * 2 + 4), (float)scale);
        return Math.Max(MathUtils.DipToPx(48f, (float)scale), content);
    }

    internal int IntrinsicWidth(int height, double scale)
    {
        Children[2].IsVisible = HasLabel;
        Children[1].Measure(new Size(double.PositiveInfinity, height / scale));
        Children[2].Measure(new Size(double.PositiveInfinity, height / scale));
        var iconWidth = MathUtils.DipToPx(Children[1].DesiredSize.Width, scale);
        var labelWidth = HasLabel ? MathUtils.DipToPx(Children[2].DesiredSize.Width, scale) : 0;
        // Only StartIconMeasurePolicy overrides maxIntrinsicWidth. The top/icon-only
        // default replays measure with DefaultIntrinsicMeasurable: the empty ripple
        // has intrinsic width zero even when measured with fixed width constraints.
        var content = IconPosition == NavigationItemIconPosition.Start && HasLabel
            ? iconWidth + labelWidth + MathUtils.DipToPx((float)(HorizontalPadding * 2 + IconLabelSpace), (float)scale)
            : labelWidth;
        return Math.Max(MathUtils.DipToPx(48f, (float)scale), content);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Children[2].IsVisible = HasLabel;
        var scale = LayoutHelper.GetLayoutScale(this);
        var maximumWidth = double.IsFinite(availableSize.Width) ? MathUtils.DipToPx(availableSize.Width, scale) : int.MaxValue;
        var maximumHeight = double.IsFinite(availableSize.Height) ? MathUtils.DipToPx(availableSize.Height, scale) : int.MaxValue;
        var minimumWidth = Math.Min(maximumWidth, MathUtils.DipToPx(!double.IsNaN(Width) ? availableSize.Width : Math.Max(48, MinimumSize.Width), scale));
        var minimumHeight = Math.Min(maximumHeight, MathUtils.DipToPx(!double.IsNaN(Height) ? availableSize.Height : Math.Max(48, MinimumSize.Height), scale));
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        var horizontal = Px(HorizontalPadding * 2); var vertical = Px(VerticalPadding * 2);
        var start = HasLabel && IconPosition == NavigationItemIconPosition.Start;
        Children[1].Measure(new Size((start ? maximumWidth : Math.Max(0, maximumWidth - horizontal)) / scale,
            (start ? maximumHeight : Math.Max(0, maximumHeight - vertical)) / scale));
        var iw = MathUtils.DipToPx(Children[1].DesiredSize.Width, scale);
        var ih = MathUtils.DipToPx(Children[1].DesiredSize.Height, scale);
        if (HasLabel)
            Children[2].Measure(new Size((start ? Math.Max(0, maximumWidth - iw - Px(IconLabelSpace)) : maximumWidth) / scale,
                (start ? maximumHeight : Math.Max(0, maximumHeight - Math.Min(ih + vertical, maximumHeight) - Px(4))) / scale));
        var lw = HasLabel ? MathUtils.DipToPx(Children[2].DesiredSize.Width, scale) : 0;
        var lh = HasLabel ? MathUtils.DipToPx(Children[2].DesiredSize.Height, scale) : 0;
        var totalWidth = start ? iw + lw + Px(IconLabelSpace + HorizontalPadding * 2) : iw + horizontal;
        var totalHeight = (start ? Math.Max(ih, lh) : ih) + vertical;
        var rw = Math.Min(totalWidth, maximumWidth); var rh = Math.Min(totalHeight, maximumHeight);
        var aw = Math.Min(MathUtils.RoundToInt(totalWidth * Math.Max(0, SelectionProgress)), maximumWidth);
        var width = Math.Clamp(HasLabel && !start ? Math.Max(lw, rw) : rw, minimumWidth, maximumWidth);
        var height = Math.Clamp(HasLabel && !start ? MathUtils.RoundToInt(rh + 4f * (float)scale + lh + (float)TopItemVerticalPadding * (float)scale * 2) : rh, minimumHeight, maximumHeight);
        if (HasLabel && !start)
        {
            var iconY = Px(TopItemVerticalPadding + VerticalPadding);
            _icon = new Rect((width - iw) / 2, iconY, iw, ih);
            _label = new Rect((width - lw) / 2, iconY + ih + Px(VerticalPadding + 4), lw, lh);
            _indicator = new Rect((width - aw) / 2, iconY - Px(VerticalPadding), aw, rh);
            _ripple = new Rect((width - rw) / 2, _indicator.Y, rw, rh);
        }
        else
        {
            var contentWidth = start ? iw + Px(IconLabelSpace) + lw : iw;
            _icon = new Rect((width - contentWidth) / 2, (height - ih) / 2, iw, ih);
            _label = new Rect(_icon.X + iw + Px(IconLabelSpace), (height - lh) / 2, lw, lh);
            _indicator = new Rect((width - aw) / 2, (height - rh) / 2, aw, rh);
            _ripple = new Rect((width - rw) / 2, (height - rh) / 2, rw, rh);
        }
        Children[0].Measure(new Size(aw / scale, rh / scale));
        Children[3].Measure(new Size(rw / scale, rh / scale));
        _inner = new Size(width, height);
        return new Size(width / scale, height / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var dx = (MathUtils.DipToPx(finalSize.Width, scale) - (int)_inner.Width) / 2;
        var dy = (MathUtils.DipToPx(finalSize.Height, scale) - (int)_inner.Height) / 2;
        void Place(Control child, Rect box) => child.Arrange(new Rect((box.X + dx) / scale, (box.Y + dy) / scale, box.Width / scale, box.Height / scale));
        Place(Children[0], _indicator); Place(Children[1], _icon);
        Children[2].IsVisible = HasLabel;
        if (HasLabel) Place(Children[2], _label);
        Place(Children[3], _ripple);
        if (Children[0] is Border { Child: Border indicator })
            indicator.CornerRadius = double.IsPositiveInfinity(IndicatorShape.TopLeft) ? new CornerRadius(Math.Min(_indicator.Width, _indicator.Height) / scale / 2) : IndicatorShape;
        if (Children[3] is MaterialSurface ripple)
            ripple.CornerRadius = double.IsPositiveInfinity(IndicatorShape.TopLeft) ? new CornerRadius(Math.Min(_ripple.Width, _ripple.Height) / scale / 2) : IndicatorShape;
        return finalSize;
    }
}

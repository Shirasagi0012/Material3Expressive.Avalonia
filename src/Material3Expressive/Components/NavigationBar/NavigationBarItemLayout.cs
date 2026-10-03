using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// NavigationBar.kt: NavigationBarItemLayout, placeIcon and placeLabelAndIcon.
internal sealed class NavigationBarItemLayout : Panel
{
    public static readonly StyledProperty<bool> HasLabelProperty = AvaloniaProperty.Register<NavigationBarItemLayout, bool>(nameof(HasLabel));
    public static readonly StyledProperty<bool> AlwaysShowLabelProperty = AvaloniaProperty.Register<NavigationBarItemLayout, bool>(nameof(AlwaysShowLabel), true);
    public static readonly StyledProperty<float> AlphaProgressProperty = AvaloniaProperty.Register<NavigationBarItemLayout, float>(nameof(AlphaProgress));
    public static readonly StyledProperty<float> SizeProgressProperty = AvaloniaProperty.Register<NavigationBarItemLayout, float>(nameof(SizeProgress));
    public static readonly StyledProperty<double> ItemMinHeightProperty = AvaloniaProperty.Register<NavigationBarItemLayout, double>(nameof(ItemMinHeight), 80);
    public static readonly StyledProperty<double> IndicatorWidthProperty = AvaloniaProperty.Register<NavigationBarItemLayout, double>(nameof(IndicatorWidth), 56);
    public static readonly StyledProperty<double> IndicatorHeightProperty = AvaloniaProperty.Register<NavigationBarItemLayout, double>(nameof(IndicatorHeight), 32);
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<NavigationBarItemLayout, double>(nameof(IconSize), 24);
    public static readonly StyledProperty<CornerRadius> IndicatorShapeProperty = AvaloniaProperty.Register<NavigationBarItemLayout, CornerRadius>(nameof(IndicatorShape), new CornerRadius(double.PositiveInfinity));
    private int _layoutHeight;

    public bool HasLabel { get => GetValue(HasLabelProperty); set => SetValue(HasLabelProperty, value); }
    public bool AlwaysShowLabel { get => GetValue(AlwaysShowLabelProperty); set => SetValue(AlwaysShowLabelProperty, value); }
    public float AlphaProgress { get => GetValue(AlphaProgressProperty); set => SetValue(AlphaProgressProperty, value); }
    public float SizeProgress { get => GetValue(SizeProgressProperty); set => SetValue(SizeProgressProperty, value); }
    public double ItemMinHeight { get => GetValue(ItemMinHeightProperty); set => SetValue(ItemMinHeightProperty, value); }
    public double IndicatorWidth { get => GetValue(IndicatorWidthProperty); set => SetValue(IndicatorWidthProperty, value); }
    public double IndicatorHeight { get => GetValue(IndicatorHeightProperty); set => SetValue(IndicatorHeightProperty, value); }
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public CornerRadius IndicatorShape { get => GetValue(IndicatorShapeProperty); set => SetValue(IndicatorShapeProperty, value); }

    static NavigationBarItemLayout()
    {
        AffectsMeasure<NavigationBarItemLayout>(HasLabelProperty, ItemMinHeightProperty, IndicatorWidthProperty, IndicatorHeightProperty, IconSizeProperty, SizeProgressProperty);
        AffectsArrange<NavigationBarItemLayout>(AlwaysShowLabelProperty, AlphaProgressProperty, IndicatorShapeProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var icon = Children[1];
        var label = Children[2];
        icon.Measure(availableSize);
        label.Measure(availableSize);
        var iconWidth = MathUtils.DipToPx(icon.DesiredSize.Width, scale);
        var iconHeight = MathUtils.DipToPx(icon.DesiredSize.Height, scale);
        var totalWidth = iconWidth + MathUtils.DipToPx((float)(IndicatorWidth - IconSize), scale);
        var indicatorHeight = iconHeight + MathUtils.DipToPx((float)(IndicatorHeight - IconSize), scale);
        var animatedWidth = MathUtils.RoundToInt(totalWidth * Math.Max(0, SizeProgress));
        Children[3].Measure(new Size(totalWidth / (double)scale, indicatorHeight / (double)scale));
        Children[0].Measure(new Size(animatedWidth / (double)scale, indicatorHeight / (double)scale));
        var maximumHeight = double.IsFinite(availableSize.Height) ? MathUtils.DipToPx(availableSize.Height, scale) : int.MaxValue;
        var minimumHeight = !double.IsNaN(Height) ? maximumHeight
            : Math.Min(MathUtils.DipToPx(MinHeight > 0 ? MinHeight : ItemMinHeight, scale), maximumHeight);
        if (HasLabel)
        {
            var padding = (float)(IndicatorHeight - IconSize) / 2 * scale;
            var contentHeight = iconHeight + padding + 4f * scale + MathUtils.DipToPx(label.DesiredSize.Height, scale);
            _layoutHeight = MathUtils.RoundToInt(contentHeight + Math.Max((minimumHeight - contentHeight) / 2, padding) * 2);
        }
        else _layoutHeight = Math.Clamp(MathUtils.DipToPx((float)ItemMinHeight, scale), minimumHeight, maximumHeight);
        var width = double.IsFinite(availableSize.Width) ? MathUtils.DipToPx(availableSize.Width, scale) : iconWidth + MathUtils.DipToPx(44f, scale) * 2;
        return new Size(width / (double)scale, _layoutHeight / (double)scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var width = MathUtils.DipToPx(finalSize.Width, scale);
        var height = Math.Max(MathUtils.DipToPx(finalSize.Height, scale), _layoutHeight);
        var apparentOffsetY = (MathUtils.DipToPx(finalSize.Height, scale) - height) / 2;
        var icon = Children[1];
        var label = Children[2];
        var iconWidth = MathUtils.DipToPx(icon.DesiredSize.Width, scale);
        var iconHeight = MathUtils.DipToPx(icon.DesiredSize.Height, scale);
        var labelWidth = MathUtils.DipToPx(label.DesiredSize.Width, scale);
        var labelHeight = MathUtils.DipToPx(label.DesiredSize.Height, scale);
        var progress = Math.Max(0, SizeProgress);
        var totalWidth = iconWidth + MathUtils.DipToPx((float)(IndicatorWidth - IconSize), scale);
        var indicatorWidth = MathUtils.RoundToInt(totalWidth * progress);
        var indicatorHeight = iconHeight + MathUtils.DipToPx((float)(IndicatorHeight - IconSize), scale);
        int iconY, indicatorY, rippleY, labelY = 0;
        if (!HasLabel)
        {
            iconY = (height - iconHeight) / 2;
            indicatorY = rippleY = (height - indicatorHeight) / 2;
        }
        else
        {
            var padding = (float)(IndicatorHeight - IconSize) / 2 * scale;
            var contentHeight = iconHeight + padding + 4f * scale + labelHeight;
            var selectedIconY = Math.Max((height - contentHeight) / 2, padding);
            var unselectedIconY = AlwaysShowLabel ? selectedIconY : (height - iconHeight) / 2f;
            var offset = (unselectedIconY - selectedIconY) * (1 - progress);
            iconY = MathUtils.RoundToInt(selectedIconY + offset);
            indicatorY = MathUtils.RoundToInt(selectedIconY - MathUtils.DipToPx((float)(IndicatorHeight - IconSize) / 2, scale) + offset);
            rippleY = MathUtils.RoundToInt(selectedIconY - padding + offset);
            labelY = MathUtils.RoundToInt(selectedIconY + iconHeight + padding + 4f * scale + offset);
        }
        void Place(Control child, int childWidth, int childHeight, int y) =>
            child.Arrange(new Rect((width - childWidth) / 2 / (double)scale, (y + apparentOffsetY) / (double)scale, childWidth / (double)scale, childHeight / (double)scale));
        Place(Children[0], indicatorWidth, indicatorHeight, indicatorY);
        if (Children[0] is Border { Child: Border indicator })
            indicator.CornerRadius = double.IsPositiveInfinity(IndicatorShape.TopLeft)
                ? new CornerRadius(Math.Min(indicatorWidth, indicatorHeight) / (double)scale / 2) : IndicatorShape;
        Place(icon, iconWidth, iconHeight, iconY);
        if (label is Border { Child: { } labelContent })
            labelContent.Opacity = AlwaysShowLabel ? 1 : progress == 0 ? 0 : Math.Clamp(AlphaProgress, 0, 1);
        label.IsVisible = HasLabel;
        label.IsHitTestVisible = HasLabel && (AlwaysShowLabel || progress != 0);
        if (HasLabel) Place(label, labelWidth, labelHeight, labelY);
        Place(Children[3], totalWidth, indicatorHeight, rippleY);
        if (Children[3] is MaterialSurface ripple)
            ripple.CornerRadius = double.IsPositiveInfinity(IndicatorShape.TopLeft)
                ? new CornerRadius(Math.Min(totalWidth, indicatorHeight) / (double)scale / 2) : IndicatorShape;
        return finalSize;
    }
}

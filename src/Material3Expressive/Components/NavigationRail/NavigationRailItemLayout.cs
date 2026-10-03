using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Direct translation of NavigationRailItemLayout / placeIcon / placeLabelAndIcon.
// Work in physical integer pixels like Compose; only convert at the Avalonia layout boundary.
internal sealed class NavigationRailItemLayout : Panel
{
    public static readonly StyledProperty<bool> HasLabelProperty = AvaloniaProperty.Register<NavigationRailItemLayout, bool>(nameof(HasLabel));
    public static readonly StyledProperty<bool> AlwaysShowLabelProperty = AvaloniaProperty.Register<NavigationRailItemLayout, bool>(nameof(AlwaysShowLabel), true);
    public static readonly StyledProperty<float> AlphaProgressProperty = AvaloniaProperty.Register<NavigationRailItemLayout, float>(nameof(AlphaProgress));
    public static readonly StyledProperty<float> SizeProgressProperty = AvaloniaProperty.Register<NavigationRailItemLayout, float>(nameof(SizeProgress));
    public static readonly StyledProperty<double> ItemMinWidthProperty = AvaloniaProperty.Register<NavigationRailItemLayout, double>(nameof(ItemMinWidth), 80);
    public static readonly StyledProperty<double> IndicatorWidthProperty = AvaloniaProperty.Register<NavigationRailItemLayout, double>(nameof(IndicatorWidth), 56);
    public static readonly StyledProperty<double> IndicatorHeightProperty = AvaloniaProperty.Register<NavigationRailItemLayout, double>(nameof(IndicatorHeight), 32);
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<NavigationRailItemLayout, double>(nameof(IconSize), 24);
    public static readonly StyledProperty<CornerRadius> IndicatorShapeProperty = AvaloniaProperty.Register<NavigationRailItemLayout, CornerRadius>(nameof(IndicatorShape), new CornerRadius(double.PositiveInfinity));
    private double _layoutHeight;

    public bool HasLabel { get => GetValue(HasLabelProperty); set => SetValue(HasLabelProperty, value); }
    public bool AlwaysShowLabel { get => GetValue(AlwaysShowLabelProperty); set => SetValue(AlwaysShowLabelProperty, value); }
    public float AlphaProgress { get => GetValue(AlphaProgressProperty); set => SetValue(AlphaProgressProperty, value); }
    public float SizeProgress { get => GetValue(SizeProgressProperty); set => SetValue(SizeProgressProperty, value); }
    public double ItemMinWidth { get => GetValue(ItemMinWidthProperty); set => SetValue(ItemMinWidthProperty, value); }
    public double IndicatorWidth { get => GetValue(IndicatorWidthProperty); set => SetValue(IndicatorWidthProperty, value); }
    public double IndicatorHeight { get => GetValue(IndicatorHeightProperty); set => SetValue(IndicatorHeightProperty, value); }
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public CornerRadius IndicatorShape { get => GetValue(IndicatorShapeProperty); set => SetValue(IndicatorShapeProperty, value); }

    static NavigationRailItemLayout()
    {
        AffectsMeasure<NavigationRailItemLayout>(HasLabelProperty, ItemMinWidthProperty, IndicatorWidthProperty, IndicatorHeightProperty, IconSizeProperty, SizeProgressProperty);
        AffectsArrange<NavigationRailItemLayout>(AlwaysShowLabelProperty, AlphaProgressProperty, IndicatorShapeProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        double Px(double value)
        {
            var px = value * scale;
            return px >= int.MinValue && px <= int.MaxValue ? MathUtils.RoundToInt(px) : Math.Floor(px + 0.5);
        }
        var icon = Children[1];
        var label = Children[2];
        icon.Measure(availableSize);
        label.Measure(availableSize);
        var iconWidth = Px(icon.DesiredSize.Width);
        var iconHeight = Px(icon.DesiredSize.Height);
        var progress = Math.Max(0, SizeProgress);
        var totalWidth = iconWidth + Px(IndicatorWidth - IconSize);
        var indicatorHeight = iconHeight + Px((HasLabel ? IndicatorHeight : IndicatorWidth) - IconSize);
        Children[3].Measure(new Size(totalWidth / scale, indicatorHeight / scale));
        Children[0].Measure(new Size(MathUtils.PxToDip(MathUtils.RoundToInt(totalWidth * progress), scale), indicatorHeight / scale));
        var width = Math.Max(Math.Min(Px(ItemMinWidth), Px(availableSize.Width)),
            Math.Max(iconWidth, HasLabel ? Math.Max(Px(label.DesiredSize.Width), Px(Children[0].DesiredSize.Width)) : totalWidth));
        var minimumHeight = double.IsNaN(Height) ? Math.Min(Px(MinHeight > 0 ? MinHeight : IndicatorWidth), Px(availableSize.Height)) : Px(availableSize.Height);
        if (HasLabel)
        {
            var padding = (float)((IndicatorHeight - IconSize) * scale / 2);
            var contentHeight = (float)iconHeight + padding + (float)(4 * scale) + (float)Px(label.DesiredSize.Height);
            _layoutHeight = MathUtils.RoundToInt(contentHeight + Math.Max((minimumHeight - contentHeight) / 2, padding) * 2);
        }
        else
            _layoutHeight = Math.Min(Px(availableSize.Height), Math.Max(minimumHeight, Px(IndicatorWidth)));
        return new Size(width / scale, _layoutHeight / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        double Px(double value)
        {
            var px = value * scale;
            return px >= int.MinValue && px <= int.MaxValue ? MathUtils.RoundToInt(px) : Math.Floor(px + 0.5);
        }
        var width = Px(finalSize.Width);
        var height = Math.Max(Px(finalSize.Height), _layoutHeight);
        var apparentOffsetY = Math.Truncate((Px(finalSize.Height) - height) / 2);
        var icon = Children[1];
        var label = Children[2];
        var iconWidth = Px(icon.DesiredSize.Width);
        var iconHeight = Px(icon.DesiredSize.Height);
        var labelWidth = Px(label.DesiredSize.Width);
        var labelHeight = Px(label.DesiredSize.Height);
        var progress = Math.Max(0, SizeProgress);
        var totalWidth = iconWidth + Px(IndicatorWidth - IconSize);
        var indicatorWidth = MathUtils.RoundToInt(totalWidth * progress);
        var indicatorHeight = iconHeight + Px((HasLabel ? IndicatorHeight : IndicatorWidth) - IconSize);
        double iconY, rippleY, labelY = 0;
        if (!HasLabel)
        {
            iconY = Math.Truncate((height - iconHeight) / 2);
            rippleY = Math.Truncate((height - indicatorHeight) / 2);
        }
        else
        {
            var padding = (float)((IndicatorHeight - IconSize) * scale / 2);
            var contentHeight = (float)iconHeight + padding + (float)(4 * scale) + (float)labelHeight;
            var contentPadding = Math.Max(((float)height - contentHeight) / 2, padding);
            var selectedIconY = contentPadding;
            var unselectedIconY = AlwaysShowLabel ? selectedIconY : ((float)height - (float)iconHeight) / 2;
            var offset = MathUtils.Lerp(0f, unselectedIconY - selectedIconY, 1 - progress);
            iconY = MathUtils.RoundToInt(selectedIconY + offset);
            rippleY = MathUtils.RoundToInt(selectedIconY - padding + offset);
            labelY = MathUtils.RoundToInt(selectedIconY + (float)iconHeight + padding + (float)(4 * scale) + offset);
        }
        void Place(Control child, double childWidth, double childHeight, double y) =>
            child.Arrange(new Rect(Math.Truncate((width - childWidth) / 2) / scale, (y + apparentOffsetY) / scale, childWidth / scale, childHeight / scale));
        Place(Children[0], indicatorWidth, indicatorHeight, rippleY);
        if (Children[0] is Border { Child: Border indicator })
            indicator.CornerRadius = !HasLabel || double.IsPositiveInfinity(IndicatorShape.TopLeft)
                ? new CornerRadius(Math.Min(indicatorWidth, indicatorHeight) / scale / 2) : IndicatorShape;
        Place(icon, iconWidth, iconHeight, iconY);
        if (label is Border { Child: { } labelContent })
            labelContent.Opacity = AlwaysShowLabel ? 1 : progress == 0 ? 0 : Math.Clamp(AlphaProgress, 0, 1);
        // The unselected label is measured, but upstream does not place it at progress=0.
        label.IsVisible = HasLabel;
        label.IsHitTestVisible = HasLabel && (AlwaysShowLabel || progress != 0);
        if (HasLabel) Place(label, labelWidth, labelHeight, labelY);
        Place(Children[3], totalWidth, indicatorHeight, rippleY);
        if (Children[3] is MaterialSurface ripple)
            ripple.CornerRadius = !HasLabel || double.IsPositiveInfinity(IndicatorShape.TopLeft)
                ? new CornerRadius(Math.Min(totalWidth, indicatorHeight) / scale / 2) : IndicatorShape;
        return finalSize;
    }
}

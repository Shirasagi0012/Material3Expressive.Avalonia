using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Tokens;

namespace Material3Expressive.Components;

// NavigationItem.kt: AnimatedMeasurePolicy/placeAnimatedLabelAndIcon and the
// icon-only branch of TopIconOrIconOnlyMeasurePolicy. Coordinates stay in px.
internal sealed class WideNavigationRailItemLayout : Panel
{
    public static readonly StyledProperty<bool> HasLabelProperty = AvaloniaProperty.Register<WideNavigationRailItemLayout, bool>(nameof(HasLabel));
    public static readonly StyledProperty<NavigationItemIconPosition> IconPositionProperty =
        AvaloniaProperty.Register<WideNavigationRailItemLayout, NavigationItemIconPosition>(nameof(IconPosition));
    public static readonly StyledProperty<float> SelectionProgressProperty = AvaloniaProperty.Register<WideNavigationRailItemLayout, float>(nameof(SelectionProgress));
    public static readonly StyledProperty<float> IconPositionProgressProperty = AvaloniaProperty.Register<WideNavigationRailItemLayout, float>(nameof(IconPositionProgress));
    public static readonly StyledProperty<Thickness?> IndicatorPaddingProperty = AvaloniaProperty.Register<WideNavigationRailItemLayout, Thickness?>(nameof(IndicatorPadding));
    public static readonly StyledProperty<Thickness> CollapsedIndicatorPaddingProperty = AvaloniaProperty.Register<WideNavigationRailItemLayout, Thickness>(nameof(CollapsedIndicatorPadding), new Thickness(16, 4));
    public static readonly StyledProperty<Thickness> ExpandedIndicatorPaddingProperty = AvaloniaProperty.Register<WideNavigationRailItemLayout, Thickness>(nameof(ExpandedIndicatorPadding), new Thickness(16, 16));
    public static readonly StyledProperty<CornerRadius> IndicatorShapeProperty =
        AvaloniaProperty.Register<WideNavigationRailItemLayout, CornerRadius>(nameof(IndicatorShape), new CornerRadius(double.PositiveInfinity));
    public bool HasLabel { get => GetValue(HasLabelProperty); set => SetValue(HasLabelProperty, value); }
    public NavigationItemIconPosition IconPosition { get => GetValue(IconPositionProperty); set => SetValue(IconPositionProperty, value); }
    public float SelectionProgress { get => GetValue(SelectionProgressProperty); set => SetValue(SelectionProgressProperty, value); }
    public float IconPositionProgress { get => GetValue(IconPositionProgressProperty); set => SetValue(IconPositionProgressProperty, value); }
    public Thickness? IndicatorPadding { get => GetValue(IndicatorPaddingProperty); set => SetValue(IndicatorPaddingProperty, value); }
    public Thickness CollapsedIndicatorPadding { get => GetValue(CollapsedIndicatorPaddingProperty); set => SetValue(CollapsedIndicatorPaddingProperty, value); }
    public Thickness ExpandedIndicatorPadding { get => GetValue(ExpandedIndicatorPaddingProperty); set => SetValue(ExpandedIndicatorPaddingProperty, value); }
    public CornerRadius IndicatorShape { get => GetValue(IndicatorShapeProperty); set => SetValue(IndicatorShapeProperty, value); }
    private Rect _indicatorBox, _iconBox, _labelBox, _rippleBox;
    private Size _inner;
    static WideNavigationRailItemLayout()
    {
        AffectsMeasure<WideNavigationRailItemLayout>(HasLabelProperty, IconPositionProperty, SelectionProgressProperty, IconPositionProgressProperty,
            IndicatorPaddingProperty, CollapsedIndicatorPaddingProperty, ExpandedIndicatorPaddingProperty, WideNavigationRailLayout.MinimumItemHeightProperty);
        AffectsArrange<WideNavigationRailItemLayout>(IndicatorShapeProperty);
    }
    public WideNavigationRailItemLayout()
    {
        ResourcesChanged += (_, _) => InvalidateMeasure();
        ActualThemeVariantChanged += (_, _) => InvalidateMeasure();
    }
    private double Metric(Material3Expressive.Foundation.Tokens.MaterialTokenKey<double> key, double fallback) =>
        Material3Expressive.Foundation.Tokens.TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is double number ? number : fallback;

    protected override Size MeasureOverride(Size availableSize)
    {
        var density = (float)LayoutHelper.GetLayoutScale(this);
        int Px(double value) => double.IsFinite(value) ? (int)Math.Floor((float)value * density + 0.5f) : int.MaxValue / 4;
        static int Round(float value) => (int)MathF.Floor(value + 0.5f);
        static int Lerp(int a, int b, float progress) => a + (int)Math.Floor((b - a) * (double)progress + 0.5);
        var maxWidth = Px(availableSize.Width);
        var maxHeight = Px(availableSize.Height);
        var position = Math.Max(0, IconPositionProgress);
        var selection = Math.Max(0, SelectionProgress);
        var horizontalPadding = Px(Metric(MaterialTokens.Comp.WideNavigationRail.ItemHorizontalPadding, 20));
        var icon = Children[1];
        var label = Children[2];
        label.IsVisible = HasLabel;
        icon.Measure(availableSize);
        label.Measure(availableSize);
        var iconWidth = Px(icon.DesiredSize.Width);
        var iconHeight = Px(icon.DesiredSize.Height);
        var labelWidth = Px(label.DesiredSize.Width);
        var labelHeight = Px(label.DesiredSize.Height);
        int innerWidth, innerHeight;
        if (!HasLabel)
        {
            var padding = (Metric(MaterialTokens.Comp.WideNavigationRail.ActiveIndicatorWidth, 56) - Metric(MaterialTokens.Comp.WideNavigationRail.IconSize, 24)) / 2;
            var loose = new Size(Math.Max(0, availableSize.Width - padding * 2), Math.Max(0, availableSize.Height - padding * 2));
            icon.Measure(loose);
            iconWidth = Px(icon.DesiredSize.Width);
            iconHeight = Px(icon.DesiredSize.Height);
            var totalWidth = iconWidth + Px(padding * 2);
            var totalHeight = iconHeight + Px(padding * 2);
            var rippleWidth = Math.Min(totalWidth, maxWidth);
            var rippleHeight = Math.Min(totalHeight, maxHeight);
            var indicatorWidth = Math.Min(Round(totalWidth * selection), maxWidth);
            innerWidth = Math.Min(rippleWidth + horizontalPadding * 2, maxWidth);
            innerHeight = Math.Min(Math.Max(rippleHeight, Math.Min(Px(48), maxHeight)), maxHeight);
            _indicatorBox = new Rect((innerWidth - indicatorWidth) / 2, (innerHeight - rippleHeight) / 2, indicatorWidth, rippleHeight);
            _rippleBox = new Rect((innerWidth - rippleWidth) / 2, (innerHeight - rippleHeight) / 2, rippleWidth, rippleHeight);
            _iconBox = new Rect((innerWidth - iconWidth) / 2, (innerHeight - iconHeight) / 2, iconWidth, iconHeight);
            _labelBox = default;
        }
        else
        {
            var paddingProgress = Math.Clamp(position, 0, 1);
            var padding = IndicatorPadding ?? new Thickness(
                (1 - paddingProgress) * (float)CollapsedIndicatorPadding.Left + paddingProgress * (float)ExpandedIndicatorPadding.Left,
                (1 - paddingProgress) * (float)CollapsedIndicatorPadding.Top + paddingProgress * (float)ExpandedIndicatorPadding.Top,
                (1 - paddingProgress) * (float)CollapsedIndicatorPadding.Right + paddingProgress * (float)ExpandedIndicatorPadding.Right,
                (1 - paddingProgress) * (float)CollapsedIndicatorPadding.Bottom + paddingProgress * (float)ExpandedIndicatorPadding.Bottom);
            var padHorizontal = Px(padding.Left + padding.Right);
            var padVertical = Px(padding.Top + padding.Bottom);
            var startSpace = Px(Metric(MaterialTokens.Comp.WideNavigationRail.StartIconLabelSpace, 8));
            var topSpace = Px(Metric(MaterialTokens.Comp.WideNavigationRail.TopIconLabelSpace, 4));
            var indicatorWidthProgress = Lerp(iconWidth, iconWidth + labelWidth + startSpace, position) + padHorizontal;
            var indicatorHeightProgress = Lerp(iconHeight, Math.Max(iconHeight, labelHeight), position) + padVertical;
            var rippleWidth = Math.Min(indicatorWidthProgress, maxWidth);
            var rippleHeight = Math.Min(indicatorHeightProgress, maxHeight);
            var indicatorWidth = Math.Min(Round(indicatorWidthProgress * selection), maxWidth);
            var indicatorHeight = Math.Min(indicatorHeightProgress, maxHeight);
            var widthTop = Math.Min(Math.Max(labelWidth, iconWidth + horizontalPadding * 2 + padHorizontal), maxWidth);
            var widthStart = Math.Min(rippleWidth + horizontalPadding, maxWidth);
            var width = widthTop + (widthStart - widthTop) * position;
            innerWidth = Round(width);
            var heightTop = Math.Min(rippleHeight + topSpace + labelHeight, maxHeight);
            var heightStart = Math.Min(rippleHeight, maxHeight);
            innerHeight = Lerp(heightTop, heightStart, position);
            var rippleXTop = horizontalPadding;
            var rippleXStart = Round((horizontalPadding + width - rippleWidth) / 2);
            var rippleX = Lerp(rippleXTop, rippleXStart, position);
            var iconX = horizontalPadding + Px(padding.Left);
            var iconYTop = Px(padding.Top);
            var iconYStart = (innerHeight - iconHeight) / 2 - iconYTop;
            var iconY = Lerp(0, iconYStart, position) + iconYTop;
            var labelXTop = (iconWidth + padHorizontal + horizontalPadding * 2 - labelWidth) / 2;
            var labelYTop = iconY + iconHeight + Px(padding.Bottom + Metric(MaterialTokens.Comp.WideNavigationRail.TopIconLabelSpace, 4));
            var labelStartOffset = IconPosition == NavigationItemIconPosition.Top && position > 0 ? 0 : horizontalPadding * (1 - position);
            var labelXStart = iconX + iconWidth + startSpace - labelStartOffset;
            var labelX = position < 0.5f ? labelXTop : labelXStart * position;
            var labelY = position < 0.5f ? labelYTop : (innerHeight - labelHeight) / 2;
            _indicatorBox = new Rect(horizontalPadding, 0, Math.Max(0, indicatorWidth), Math.Max(0, indicatorHeight));
            _rippleBox = new Rect(rippleX, 0, Math.Max(0, rippleWidth), Math.Max(0, rippleHeight));
            _iconBox = new Rect(iconX, iconY, iconWidth, iconHeight);
            _labelBox = new Rect((int)labelX, labelY, labelWidth, labelHeight);
            if (label is Border { Child: { } content }) content.Opacity = Math.Clamp(4 * (position - 0.5f) * (position - 0.5f), 0, 1);
        }
        Children[0].Measure(new Size(_indicatorBox.Width / density, _indicatorBox.Height / density));
        Children[3].Measure(new Size(_rippleBox.Width / density, _rippleBox.Height / density));
        _inner = new Size(innerWidth, innerHeight);
        // Outer selectable Box defaultMinSize and propagated parent minimums;
        // AnimatedMeasurePolicy itself uses loose constraints.
        var minimumHeight = GetValue(WideNavigationRailLayout.MinimumItemHeightProperty);
        return new Size(Math.Max(Math.Min(48, availableSize.Width), innerWidth / density),
            Math.Max(Math.Min(minimumHeight, availableSize.Height), innerHeight / density));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var density = (float)LayoutHelper.GetLayoutScale(this);
        // Placeable.apparentToRealOffset centers a measure result coerced by the
        // parent's minimum constraints with integer division, not Alignment.Center.
        var offsetX = Math.Truncate((Math.Floor(finalSize.Width * density + 0.5) - _inner.Width) / 2);
        var offsetY = Math.Truncate((Math.Floor(finalSize.Height * density + 0.5) - _inner.Height) / 2);
        void Place(Control control, Rect box) => control.Arrange(new Rect((box.X + offsetX) / density, (box.Y + offsetY) / density, box.Width / density, box.Height / density));
        Place(Children[0], _indicatorBox);
        Place(Children[1], _iconBox);
        if (HasLabel) Place(Children[2], _labelBox);
        Place(Children[3], _rippleBox);
        if (Children[0] is Border { Child: Border indicator })
            indicator.CornerRadius = !HasLabel || double.IsPositiveInfinity(IndicatorShape.TopLeft)
                ? new CornerRadius(Math.Min(_indicatorBox.Width, _indicatorBox.Height) / density / 2) : IndicatorShape;
        if (Children[3] is MaterialSurface ripple)
            ripple.CornerRadius = !HasLabel || double.IsPositiveInfinity(IndicatorShape.TopLeft)
                ? new CornerRadius(Math.Min(_rippleBox.Width, _rippleBox.Height) / density / 2) : IndicatorShape;
        return finalSize;
    }
}


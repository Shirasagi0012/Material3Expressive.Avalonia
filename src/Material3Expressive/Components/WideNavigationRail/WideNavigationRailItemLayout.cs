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
    private WideNavigationRailVerticalValue _heightMotion;
    private int _collapsedInnerHeight, _expandedInnerHeight, _maximumHeight;
    internal int ArrangementAnchorHeight => Math.Max(Math.Min(_heightMotion.Anchor, _maximumHeight),
        !HasLabel || IconPositionProgress < .5f ? _collapsedInnerHeight : _expandedInnerHeight);
    internal double ArrangementAnchorWeight => HasLabel ? WideNavigationRailVerticalValue.CornerWeight(IconPositionProgress) : 1;
    // Temporary, read-only observations for Catalog's rail jitter trace.
    // Keep the continuous values before the static endpoint correction.
    private Thickness _tracePadding;
    private double _traceInnerHeightRaw, _traceIconYRaw, _traceCenterOffsetYRaw, _traceCenterOffsetY;
    static WideNavigationRailItemLayout()
    {
        AffectsMeasure<WideNavigationRailItemLayout>(HasLabelProperty, IconPositionProperty, SelectionProgressProperty, IconPositionProgressProperty,
            IndicatorPaddingProperty, CollapsedIndicatorPaddingProperty, ExpandedIndicatorPaddingProperty,
            WideNavigationRailLayout.MinimumItemHeightProperty, WideNavigationRailLayout.ItemHeightMotionProperty);
        AffectsArrange<WideNavigationRailItemLayout>(IndicatorShapeProperty);
    }
    public WideNavigationRailItemLayout()
    {
        UseLayoutRounding = false;
        ResourcesChanged += (_, _) => InvalidateMeasure();
        ActualThemeVariantChanged += (_, _) => InvalidateMeasure();
    }
    private double Metric(Material3Expressive.Foundation.Tokens.MaterialTokenKey<double> key, double fallback) =>
        Material3Expressive.Foundation.Tokens.TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is double number ? number : fallback;

    protected override Size MeasureOverride(Size availableSize)
    {
        var layoutScale = LayoutHelper.GetLayoutScale(this);
        var density = (float)layoutScale;
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
        int innerWidth;
        double innerHeight;
        _maximumHeight = maxHeight;
        var minimumHeight = GetValue(WideNavigationRailLayout.MinimumItemHeightProperty);
        _heightMotion = GetValue(WideNavigationRailLayout.ItemHeightMotionProperty)
            ?? WideNavigationRailVerticalValue.FromDp(minimumHeight, minimumHeight, minimumHeight, layoutScale);
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
            // This branch has no internal position animation; its original half-pixel
            // truncation remains constant while the outer minimum height animates.
            _indicatorBox = new Rect((innerWidth - indicatorWidth) / 2, Math.Truncate((innerHeight - rippleHeight) / 2), indicatorWidth, rippleHeight);
            _rippleBox = new Rect((innerWidth - rippleWidth) / 2, Math.Truncate((innerHeight - rippleHeight) / 2), rippleWidth, rippleHeight);
            _iconBox = new Rect((innerWidth - iconWidth) / 2, Math.Truncate((innerHeight - iconHeight) / 2), iconWidth, iconHeight);
            _collapsedInnerHeight = _expandedInnerHeight = (int)innerHeight;
            _tracePadding = new Thickness(padding);
            _traceInnerHeightRaw = innerHeight;
            _traceIconYRaw = (innerHeight - iconHeight) / 2d;
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
            var collapsedPadding = IndicatorPadding ?? CollapsedIndicatorPadding;
            var expandedPadding = IndicatorPadding ?? ExpandedIndicatorPadding;
            double VerticalPadding(double start, double end) => Px(start) + (Px(end) - Px(start)) * (double)paddingProgress;
            // Rounded static endpoints, continuous intermediate padding. Total and
            // individual padding retain their separate upstream rounding definitions.
            var padVertical = VerticalPadding(collapsedPadding.Top + collapsedPadding.Bottom, expandedPadding.Top + expandedPadding.Bottom);
            var startSpace = Px(Metric(MaterialTokens.Comp.WideNavigationRail.StartIconLabelSpace, 8));
            var topSpace = Px(Metric(MaterialTokens.Comp.WideNavigationRail.TopIconLabelSpace, 4));
            var indicatorWidthProgress = Lerp(iconWidth, iconWidth + labelWidth + startSpace, position) + padHorizontal;
            var indicatorHeightProgress = iconHeight + (Math.Max(iconHeight, labelHeight) - iconHeight) * (double)position + padVertical;
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
            innerHeight = heightTop + (heightStart - heightTop) * (double)position;
            var collapsedRippleHeight = Math.Min(iconHeight + Px(collapsedPadding.Top + collapsedPadding.Bottom), maxHeight);
            var expandedRippleHeight = Math.Min(Math.Max(iconHeight, labelHeight) + Px(expandedPadding.Top + expandedPadding.Bottom), maxHeight);
            _collapsedInnerHeight = Math.Min(collapsedRippleHeight + topSpace + labelHeight, maxHeight);
            _expandedInnerHeight = expandedRippleHeight;
            _tracePadding = padding;
            _traceInnerHeightRaw = heightTop + (heightStart - heightTop) * (double)position;
            var rippleXTop = horizontalPadding;
            var rippleXStart = Round((horizontalPadding + width - rippleWidth) / 2);
            var rippleX = Lerp(rippleXTop, rippleXStart, position);
            var iconX = horizontalPadding + Px(padding.Left);
            var iconYTop = VerticalPadding(collapsedPadding.Top, expandedPadding.Top);
            var iconYStart = (innerHeight - iconHeight) / 2 - iconYTop;
            var rawIconY = iconYStart * position + iconYTop;
            var expandedIconCenter = (_expandedInnerHeight - iconHeight) / 2d;
            var iconY = rawIconY + paddingProgress * (Math.Truncate(expandedIconCenter) - expandedIconCenter);
            _traceIconYRaw = iconYStart * (double)position + iconYTop;
            var labelXTop = (iconWidth + padHorizontal + horizontalPadding * 2 - labelWidth) / 2;
            var topGap = Metric(MaterialTokens.Comp.WideNavigationRail.TopIconLabelSpace, 4);
            var labelYTop = iconY + iconHeight + VerticalPadding(collapsedPadding.Bottom + topGap, expandedPadding.Bottom + topGap);
            var labelStartOffset = IconPosition == NavigationItemIconPosition.Top && position > 0 ? 0 : horizontalPadding * (1 - position);
            var labelXStart = iconX + iconWidth + startSpace - labelStartOffset;
            var labelX = position < 0.5f ? labelXTop : labelXStart * position;
            var expandedLabelCenter = (_expandedInnerHeight - labelHeight) / 2d;
            var labelY = position < 0.5f ? labelYTop : (innerHeight - labelHeight) / 2
                + paddingProgress * (Math.Truncate(expandedLabelCenter) - expandedLabelCenter);
            _indicatorBox = new Rect(horizontalPadding, 0, Math.Max(0, indicatorWidth), Math.Max(0, indicatorHeight));
            _rippleBox = new Rect(rippleX, 0, Math.Max(0, rippleWidth), Math.Max(0, rippleHeight));
            _iconBox = new Rect(iconX, iconY, iconWidth, iconHeight);
            _labelBox = new Rect((int)labelX, labelY, labelWidth, labelHeight);
            if (label is Border { Child: { } content }) content.Opacity = Math.Clamp(4 * (position - 0.5f) * (position - 0.5f), 0, 1);
        }
        Children[0].Measure(new Size(_indicatorBox.Width / layoutScale, _indicatorBox.Height / layoutScale));
        Children[3].Measure(new Size(_rippleBox.Width / layoutScale, _rippleBox.Height / layoutScale));
        _inner = new Size(innerWidth, innerHeight);
        // Outer selectable Box defaultMinSize and propagated parent minimums;
        // AnimatedMeasurePolicy itself uses loose constraints.
        // Convert physical pixels back to Avalonia's double DIP without a float
        // division: at 125%, 63px / 1.25f becomes 50.4000015 DIP and the default
        // layout rounding then grows it to 64px, disturbing animated centering.
        return new Size(Math.Max(Math.Min(48, availableSize.Width), innerWidth / layoutScale),
            Math.Max(Math.Min(_heightMotion.Value / layoutScale, availableSize.Height), innerHeight / layoutScale));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var density = LayoutHelper.GetLayoutScale(this);
        // Continuous centering plus the endpoint residual of Placeable's integer
        // division. Item position and the rail's minimum height are independent axes
        // (including explicit Top/Start overrides), so blend all four combinations.
        var offsetX = Math.Truncate((Math.Floor(finalSize.Width * density + 0.5) - _inner.Width) / 2);
        var rawOffsetY = (finalSize.Height * density - _inner.Height) / 2;
        double Residual(int minimum, int content)
        {
            var half = (Math.Max(Math.Min(minimum, _maximumHeight), content) - content) / 2d;
            return Math.Truncate(half) - half;
        }
        var heightProgress = Math.Clamp(_heightMotion.Progress, 0, 1);
        double HeightResidual(int content) => (1 - heightProgress) * Residual(_heightMotion.Collapsed, content)
            + heightProgress * Residual(_heightMotion.Expanded, content);
        var positionProgress = HasLabel ? Math.Clamp(IconPositionProgress, 0, 1) : 0;
        var residual = (1 - positionProgress) * HeightResidual(_collapsedInnerHeight)
            + positionProgress * HeightResidual(_expandedInnerHeight);
        var offsetY = rawOffsetY + residual;
        _traceCenterOffsetYRaw = rawOffsetY;
        _traceCenterOffsetY = offsetY;
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


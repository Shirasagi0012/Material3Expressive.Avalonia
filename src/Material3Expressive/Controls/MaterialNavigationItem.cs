using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// One destination in a <see cref="MaterialNavigationBar" />.
/// </summary>
/// <remarks>
/// The item is a toggle, and the bar keeps exactly one of them checked. The visible selection is
/// the indicator: a pill that grows behind the icon rather than a colour change alone, which is
/// what Material 3 replaced the old highlighted-tab look with.
/// </remarks>
public class MaterialNavigationItem : ToggleButton
{
    /// <inheritdoc cref="Icon" />
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<MaterialNavigationItem, object?>(nameof(Icon));

    // Read-only outputs of token resolution; the template binds to them.
    public static readonly DirectProperty<MaterialNavigationItem, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialNavigationItem, double> IconLabelSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(IconLabelSpace), o => o.IconLabelSpace);

    public static readonly DirectProperty<MaterialNavigationItem, IImmutableBrush> IconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, IImmutableBrush>(nameof(IconBrush), o => o.IconBrush);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private SpringAnimation? _indicator;
    private NavigationTokens _tokens = NavigationTokens.Bar;
    private NavigationColors _colors = NavigationTokens.Bar.Resolve(null, false);

    public MaterialNavigationItem()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => InvalidateVisual();

        ApplyTokens();
    }

    /// <summary>The destination's icon.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The size the icon should be drawn at.</summary>
    public double IconSize => _tokens.IconSize;

    /// <summary>The gap between the indicator and the label under it.</summary>
    public double IconLabelSpace => _tokens.IconLabelSpace;

    /// <summary>The colour the icon should take right now.</summary>
    public IImmutableBrush IconBrush =>
        IsChecked == true ? _colors.ActiveIconColor : _colors.InactiveIconColor;

    /// <summary>Which navigation surface this item belongs to; set by the parent.</summary>
    internal NavigationVariant Surface { get; set; } = NavigationVariant.Bar;

    /// <summary>The pill behind the icon, in control coordinates.</summary>
    protected Rect IndicatorArea
    {
        get
        {
            var width = _tokens.ActiveIndicatorWidth;
            var height = _tokens.ActiveIndicatorHeight;
            var scale = LayoutHelper.GetLayoutScale(this);
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
                scale = 1;

            // NavigationBarItemLayout places the indicator with integral physical-pixel
            // arithmetic: (containerWidthPx - indicatorWidthPx) / 2.  Keeping this in logical
            // coordinates leaves the middle item of an uneven weighted row at a half pixel and
            // changes its rounded-rectangle antialiasing, even though its text placement is
            // already correct.
            var containerWidth = ToComposePixels(Bounds.Width, scale);
            var indicatorWidth = ToComposePixels(width, scale);
            var left = Math.Max(0, (containerWidth - indicatorWidth) / 2) / scale;
            return new Rect(left, IndicatorTop, indicatorWidth / scale, height);
        }
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private double IndicatorTop =>
        // The label sits under the indicator, so the pair is centred rather than the pill alone.
        (Bounds.Height - _tokens.ActiveIndicatorHeight - _tokens.IconLabelSpace - LabelHeight) / 2;

    private double LabelHeight =>
        Content is null ? 0 : _theme.Theme.Typography.GetTypeScale(_tokens.LabelTextStyle).LineHeight;

    private SpringAnimation Indicator =>
        _indicator ??= _interaction.Motion.AddSpring(
            IsChecked == true ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var area = IndicatorArea;
        if (area.Width <= 0 || area.Height <= 0)
            return;

        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ActiveIndicatorShape, area.Size);

        var scale = Indicator.Value;
        if (scale > 0.001)
        {
            // The pill grows horizontally out of the icon rather than fading in.
            var width = area.Width * scale;
            var grown = new Rect(area.Center.X - width / 2, area.Y, width, area.Height);

            using (context.PushTransform(Matrix.CreateTranslation(grown.X, grown.Y)))
                MaterialContainer.Draw(context, grown.Size, corners, _colors.ActiveIndicatorColor);
        }

        _interaction.InteractionArea = area;
        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);

        base.Render(context);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            Indicator.AnimateTo(IsChecked == true ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));
            _interaction.Motion.Start();
            ApplyColors();
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            ApplyTokens();
        }
    }

    internal void ApplyTokens()
    {
        var previousIconSize = IconSize;
        var previousSpace = IconLabelSpace;

        _tokens = _theme.Theme.GetNavigationTokens(Surface);
        _colors = _theme.Theme.GetNavigationColors(Surface, _theme.IsDark);

        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previousSpace, IconLabelSpace);

        var typography = _theme.Theme.Typography;
        FontSize = typography.GetTypeScale(_tokens.LabelTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.LabelTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.LabelTextStyle);

        // NavigationBarItem and NavigationRailItem both wrap their label in
        // ProvideContentColorTextStyle.  FontSize alone is not sufficient for a templated
        // Avalonia control: the supplied TextBlock otherwise retains the inherited BodyMedium
        // line height, tracking, and weight.  Carry the complete local text-style contract to
        // the content subtree, matching the Compose composition local.
        TypographyScheme.SetTextStyle(this, _tokens.LabelTextStyle);

        ApplyColors();
        InvalidateMeasure();
    }

    private void ApplyColors()
    {
        var previousIconBrush = IconBrush;

        Foreground = IsChecked == true ? _colors.ActiveLabelTextColor : _colors.InactiveLabelTextColor;
        RaisePropertyChanged(IconBrushProperty, previousIconBrush, IconBrush);

        InvalidateVisual();
    }
}

/// <summary>
/// Arranges a navigation item's icon and label using Compose's physical-pixel placement rules.
/// </summary>
/// <remarks>
/// <para>
/// NavigationBar.kt and NavigationRail.kt both calculate <c>labelX</c> as
/// <c>(containerWidth - labelPlaceable.width) / 2</c>, where both operands are integral physical
/// pixels. The integer division deliberately rounds a half pixel down. Avalonia's general-purpose
/// centre alignment preserves the fractional coordinate until rendering, which rounds the same
/// label up on a 2x surface.
/// </para>
/// <para>
/// The panel keeps the shared template but performs that Compose-specific calculation for each
/// child slot. Measured text extents are rounded up to the integral placeable size Compose uses;
/// this is a layout rule, not a label- or index-specific correction.
/// </para>
/// </remarks>
public sealed class MaterialNavigationItemContentPanel : Panel
{
    /// <summary>The fixed gap between the icon slot and the label slot.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, double>(nameof(Spacing));

    static MaterialNavigationItemContentPanel()
    {
        AffectsMeasure<MaterialNavigationItemContentPanel>(SpacingProperty);
    }

    /// <inheritdoc cref="SpacingProperty" />
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return default;

        var availableWidth = availableSize.Width;
        var desiredWidth = 0d;
        var desiredHeight = 0d;

        foreach (var child in visibleChildren)
        {
            child.Measure(new Size(availableWidth, double.PositiveInfinity));
            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
            desiredHeight += child.DesiredSize.Height;
        }

        desiredHeight += Spacing * (visibleChildren.Length - 1);
        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return finalSize;

        var scale = GetScale();
        var finalWidth = ToComposePixels(finalSize.Width, scale);
        var finalHeight = ToComposePixels(finalSize.Height, scale);
        var spacing = ToComposePixels(Spacing, scale);
        var childWidths = new int[visibleChildren.Length];
        var childHeights = new int[visibleChildren.Length];
        var contentHeight = spacing * (visibleChildren.Length - 1);

        for (var index = 0; index < visibleChildren.Length; index++)
        {
            childWidths[index] = ToPlaceablePixels(visibleChildren[index].DesiredSize.Width, scale);
            childHeights[index] = ToPlaceablePixels(visibleChildren[index].DesiredSize.Height, scale);
            contentHeight += childHeights[index];
        }

        // NavigationBar.kt / NavigationRail.kt use roundToInt only when emitting the vertical
        // placeable position. This is equivalent to rounding the shared pair offset once here.
        var top = ToComposePixels((finalHeight - contentHeight) / 2d, 1);

        for (var index = 0; index < visibleChildren.Length; index++)
        {
            var width = childWidths[index];
            var height = childHeights[index];
            var left = Math.Max(0, (finalWidth - width) / 2);

            visibleChildren[index].Arrange(new Rect(
                ToLogicalPixels(left, scale),
                ToLogicalPixels(top, scale),
                ToLogicalPixels(width, scale),
                ToLogicalPixels(height, scale)));

            top += height + spacing;
        }

        return finalSize;
    }

    private double GetScale()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return scale > 0 && !double.IsNaN(scale) && !double.IsInfinity(scale) ? scale : 1;
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private static int ToPlaceablePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Ceiling(value * scale - 0.000001));

    private static double ToLogicalPixels(int value, double scale) => value / scale;
}

using Avalonia;
using Avalonia.Controls.Primitives;
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
            return new Rect((Bounds.Width - width) / 2, IndicatorTop, width, height);
        }
    }

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

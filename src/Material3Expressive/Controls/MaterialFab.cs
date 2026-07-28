using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 floating action button, extended when it is given a <see cref="Label" />.
/// </summary>
/// <remarks>
/// Extended and unextended are one control rather than two, because the difference is entirely in
/// whether a label is present: same container, same elevation, same shape, and Material animates
/// between them. Setting <see cref="Label" /> extends it.
/// <para>
/// <see cref="IsLowered" /> is the one FAB-specific piece of state: on a surface that scrolls
/// underneath it, Material drops the FAB from level 3 to level 1 so the shadow does not fight the
/// content sliding past.
/// </para>
/// </remarks>
public class MaterialFab : Button
{
    public static readonly StyledProperty<FabVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialFab, FabVariant>(nameof(Variant));

    public static readonly StyledProperty<FabSize> SizeProperty =
        AvaloniaProperty.Register<MaterialFab, FabSize>(nameof(Size), FabSize.Small);

    /// <inheritdoc cref="Label" />
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<MaterialFab, object?>(nameof(Label));

    /// <inheritdoc cref="IsLowered" />
    public static readonly StyledProperty<bool> IsLoweredProperty =
        AvaloniaProperty.Register<MaterialFab, bool>(nameof(IsLowered));

    // Read-only outputs of token resolution. Direct properties rather than plain CLR ones because
    // the template binds to them, and a TemplateBinding needs an AvaloniaProperty.
    public static readonly DirectProperty<MaterialFab, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialFab, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialFab, double> IconLabelSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialFab, double>(nameof(IconLabelSpace), o => o.IconLabelSpace);

    public static readonly DirectProperty<MaterialFab, bool> IsExtendedProperty =
        AvaloniaProperty.RegisterDirect<MaterialFab, bool>(nameof(IsExtended), o => o.IsExtended);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private AnimatedShape? _shape;
    private FabColors _colors = FabColorTokens.Primary.Resolve(null, false);
    private FabColorTokens _tokens = FabColorTokens.Primary;
    private FabSizeTokens _metrics = FabSizeTokens.Small;

    public MaterialFab()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateShape();

        ApplyTokens();
    }

    static MaterialFab()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialFab>(false);
        AffectsRender<MaterialFab>(VariantProperty, IsLoweredProperty);
    }

    public FabVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public FabSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// The extended FAB's text. Leaving it null keeps the FAB square and icon-only.
    /// </summary>
    public object? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Drops the FAB to the lowered elevation, for a surface that scrolls beneath it.
    /// </summary>
    public bool IsLowered
    {
        get => GetValue(IsLoweredProperty);
        set => SetValue(IsLoweredProperty, value);
    }

    /// <summary>True when a label has been set, which is what "extended" means.</summary>
    public bool IsExtended => Label is { };

    /// <summary>The size the icon in the content slot should be drawn at.</summary>
    public double IconSize => _metrics.IconSize;

    /// <summary>The gap between the icon and the label when extended.</summary>
    public double IconLabelSpace => _metrics.IconLabelSpace;

    /// <summary>Tells the template that the values it binds to have been recomputed.</summary>
    private void RaiseTokenOutputs(FabSizeTokens previous, bool wasExtended)
    {
        RaisePropertyChanged(IconSizeProperty, previous.IconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previous.IconLabelSpace, IconLabelSpace);
        RaisePropertyChanged(IsExtendedProperty, wasExtended, IsExtended);
    }

    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    private AnimatedShape Shape =>
        _shape ??= _interaction.CreateAnimatedShape(RestingShape(), _theme.Spring(MotionRole.DefaultEffects));

    public override void Render(DrawingContext context)
    {
        var corners = ContainerCornerRadius;

        MaterialContainer.Draw(
            context,
            Bounds.Size,
            corners,
            _colors.ContainerColor,
            shadows: CurrentShadows());

        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);

        base.Render(context);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty
            || change.Property == SizeProperty
            || change.Property == LabelProperty)
            ApplyTokens();
    }

    private void ApplyTokens()
    {
        var previousMetrics = _metrics;
        var wasExtended = IsExtended;

        _tokens = _theme.Theme.GetFabColorTokens(Variant);
        _colors = _theme.Theme.GetFabColors(Variant, _theme.IsDark);
        _metrics = _theme.Theme.GetFabSizeTokens(Size);
        RaiseTokenOutputs(previousMetrics, wasExtended);

        var typography = _theme.Theme.Typography;

        Foreground = _colors.IconColor;
        MinHeight = _metrics.ContainerHeight;
        MinWidth = IsExtended ? _metrics.ContainerHeight : _metrics.ContainerWidth;
        Padding = IsExtended
            ? new Thickness(_metrics.LabelSpace, 0, _metrics.LabelSpace, 0)
            : default;
        FontSize = typography.GetTypeScale(_metrics.LabelTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_metrics.LabelTextStyle);
        FontWeight = typography.GetFontWeight(_metrics.LabelTextStyle);

        UpdateShape(snap: true);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? _theme.Theme.Shapes.Get(_metrics.PressedContainerShape)
            : RestingShape();

        if (snap && !Shape.IsRunning)
        {
            Shape.SnapTo(target);
            InvalidateVisual();
            return;
        }

        Shape.AnimateTo(target, _theme.Spring(MotionRole.DefaultEffects));
        _interaction.Motion.Start();
    }

    private RoundedCornerShape RestingShape()
    {
        return _theme.Theme.Shapes.Get(_metrics.ContainerShape);
    }

    private BoxShadows CurrentShadows()
    {
        var interaction = _interaction.Interaction;

        var elevation = IsLowered ? _tokens.LoweredContainerElevation
            : interaction.HasFlag(InteractionState.Pressed) ? _tokens.PressedContainerElevation
            : interaction.HasFlag(InteractionState.Hovered) ? _tokens.HoveredContainerElevation
            : interaction.HasFlag(InteractionState.Focused) ? _tokens.FocusedContainerElevation
            : _tokens.ContainerElevation;

        var shadow = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;
        return Elevation.ToBoxShadows(elevation, shadow);
    }
}

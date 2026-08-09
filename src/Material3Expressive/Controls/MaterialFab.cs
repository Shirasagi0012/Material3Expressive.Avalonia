using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>The container-colour variants available to a Material floating action button.</summary>
/// <remarks>
/// This component API is intentionally hand-authored. <c>FabDefaults</c> maps the selection to
/// raw upstream token tables and to the system roles used by the spec-only tertiary and surface
/// variants.
/// </remarks>
public enum FabVariant
{
    /// <summary>The primary-container FAB.</summary>
    Primary,

    /// <summary>The secondary-container FAB.</summary>
    Secondary,

    /// <summary>The tertiary-container FAB.</summary>
    Tertiary,

    /// <summary>A neutral FAB for a surface where a coloured one would compete.</summary>
    Surface
}

/// <summary>The three standard Material floating action button sizes.</summary>
/// <remarks>
/// Raw token selection and the expressive extended-FAB layout policy belong to
/// <c>FabDefaults</c>, not to a generated defaults record.
/// </remarks>
public enum FabSize
{
    /// <summary>The 56dp baseline FAB.</summary>
    Small,

    /// <summary>The 80dp FAB.</summary>
    Medium,

    /// <summary>The 96dp FAB.</summary>
    Large
}

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
public class MaterialFab : Button, IMaterialThemeConsumer, IMaterialInteractionConsumer
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

    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;

    private AnimatedShape? _shape;
    private FabColors _colors = FabDefaults.Colors(ResolvedTheme.Default, FabVariant.Primary, isDark: false);
    private FabElevation _elevation = FabDefaults.Elevation(FabVariant.Primary);
    private FabMetrics _metrics = FabDefaults.Metrics(FabSize.Small);

    public MaterialFab()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;

        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        UpdateShape();

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
    private void RaiseTokenOutputs(FabMetrics previous, bool wasExtended)
    {
        RaisePropertyChanged(IconSizeProperty, previous.IconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previous.IconLabelSpace, IconLabelSpace);
        RaisePropertyChanged(IsExtendedProperty, wasExtended, IsExtended);
    }

    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    private AnimatedShape Shape => _shape ??= CreateShape();

    private AnimatedShape CreateShape()
    {
        var shape = new AnimatedShape(
            RestingShape(),
            _theme.Current.Theme.Motion.Get(MotionRole.DefaultEffects));
        _interaction.Motion.Add(shape.Progress);
        return shape;
    }

    public override void Render(DrawingContext context)
    {
        var corners = ContainerCornerRadius;
        var shadow = _theme.Current.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.Current.IsDark) ?? Colors.Black;

        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            _colors.ContainerColor,
            null,
            0,
            CurrentElevation(),
            shadow);

        base.Render(context);
        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);
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

        _colors = FabDefaults.Colors(_theme.Current.Theme, Variant, _theme.Current.IsDark);
        _elevation = FabDefaults.Elevation(Variant);
        _metrics = FabDefaults.Metrics(Size);
        RaiseTokenOutputs(previousMetrics, wasExtended);

        var typography = _theme.Current.Theme.Typography;

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
            ? _theme.Current.Theme.Shapes.Get(_metrics.PressedContainerShape)
            : RestingShape();

        if (snap && !Shape.IsRunning)
        {
            Shape.SnapTo(target);
            InvalidateVisual();
            return;
        }

        Shape.AnimateTo(target, _theme.Current.Theme.Motion.Get(MotionRole.DefaultEffects));
        _interaction.Motion.RequestFrame();
    }

    private RoundedCornerShape RestingShape()
    {
        return _theme.Current.Theme.Shapes.Get(_metrics.ContainerShape);
    }

    private double CurrentElevation()
    {
        var interaction = _interaction.Interaction;

        return _elevation.For(IsLowered, interaction);
    }
}

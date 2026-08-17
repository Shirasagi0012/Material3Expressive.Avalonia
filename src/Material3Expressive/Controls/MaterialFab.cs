using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>The container-colour variants available to a Material floating action button.</summary>
/// <remarks>
/// This component API is intentionally hand-authored. The <c>FabValueKeys</c> catalog maps the
/// selector's choice to raw upstream token tables and to the system roles used by the spec-only
/// tertiary and surface variants.
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
/// Raw token selection belongs to <c>FabValueKeys</c>; the expressive extended-FAB layout policy
/// remains in the control and its theme, not in a generated defaults record.
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
public class MaterialFab : Button, IMaterialInteractionConsumer
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

    private static readonly FabVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        Colors.Black);

    private static readonly FabElevationProfile DefaultElevation = new(
        ElevationTokens.Level3,
        ElevationTokens.Level4,
        ElevationTokens.Level3,
        ElevationTokens.Level3,
        ElevationTokens.Level1);

    private static readonly FabGeometryProfile DefaultGeometry = new(
        ContainerHeight: 56,
        ContainerWidth: 56,
        ContainerShape: ShapeRole.CornerLarge,
        PressedContainerShape: ShapeRole.CornerMedium,
        IconSize: 24,
        LabelSpace: 16,
        IconLabelSpace: 12,
        LabelTextStyle: TypographyRole.LabelLarge);

    // These hidden semantic properties carry the typed MaterialValue results selected by the
    // ControlTheme. Public Button properties remain ordinary setters so application styles and
    // local values retain normal Avalonia precedence.
    internal static readonly StyledProperty<FabVisualColors> ResolvedVisualColorsProperty =
        AvaloniaProperty.Register<MaterialFab, FabVisualColors>(
            nameof(ResolvedVisualColors),
            DefaultVisualColors);

    internal static readonly StyledProperty<FabElevationProfile> ResolvedElevationProperty =
        AvaloniaProperty.Register<MaterialFab, FabElevationProfile>(
            nameof(ResolvedElevation),
            DefaultElevation);

    internal static readonly StyledProperty<FabGeometryProfile> ResolvedGeometryProperty =
        AvaloniaProperty.Register<MaterialFab, FabGeometryProfile>(
            nameof(ResolvedGeometry),
            DefaultGeometry);

    internal static readonly StyledProperty<Thickness> ResolvedContentPaddingProperty =
        AvaloniaProperty.Register<MaterialFab, Thickness>(nameof(ResolvedContentPadding));

    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;

    private AnimatedShape? _shape;
    private bool _hasMaterialValues;

    public MaterialFab()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;

        UpdateDerivedProperties();
    }

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        UpdateShape();

    static MaterialFab()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialFab>(false);
        AffectsRender<MaterialFab>(
            VariantProperty,
            IsLoweredProperty,
            ResolvedVisualColorsProperty,
            ResolvedElevationProperty,
            ResolvedGeometryProperty);
        AffectsMeasure<MaterialFab>(ResolvedGeometryProperty, ResolvedContentPaddingProperty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _hasMaterialValues = this.TryFindResource(
            FabValueKeys.SmallGeometry,
            ActualThemeVariant,
            out _);
        if (!_hasMaterialValues)
            ApplyUnscopedFallback();
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
    public double IconSize => EffectiveGeometry.IconSize;

    /// <summary>The gap between the icon and the label when extended.</summary>
    public double IconLabelSpace => EffectiveGeometry.IconLabelSpace;

    /// <summary>Gets the hidden variant-selected visual profile.</summary>
    internal FabVisualColors ResolvedVisualColors
    {
        get => GetValue(ResolvedVisualColorsProperty);
        set => SetValue(ResolvedVisualColorsProperty, value);
    }

    /// <summary>Gets the hidden variant-selected interaction-elevation profile.</summary>
    internal FabElevationProfile ResolvedElevation
    {
        get => GetValue(ResolvedElevationProperty);
        set => SetValue(ResolvedElevationProperty, value);
    }

    /// <summary>Gets the hidden size-selected geometry profile.</summary>
    internal FabGeometryProfile ResolvedGeometry
    {
        get => GetValue(ResolvedGeometryProperty);
        set => SetValue(ResolvedGeometryProperty, value);
    }

    /// <summary>Gets the hidden extended-FAB padding policy.</summary>
    internal Thickness ResolvedContentPadding
    {
        get => GetValue(ResolvedContentPaddingProperty);
        set => SetValue(ResolvedContentPaddingProperty, value);
    }

    /// <summary>Tells the template that the values it binds to have been recomputed.</summary>
    private void RaiseTokenOutputs(FabGeometryProfile previous, bool wasExtended)
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
        var colors = ResolvedVisualColors;

        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            Background ?? colors.ContainerColor,
            null,
            0,
            CurrentElevation(),
            colors.ShadowColor);

        base.Render(context);
        _interaction.DrawLayers(context, colors.StateLayerColor, corners);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LabelProperty)
        {
            var wasExtended = change.GetOldValue<object?>() is not null;
            RaiseTokenOutputs(ResolvedGeometry, wasExtended);
            UpdateDerivedProperties();
        }
        else if (change.Property == ResolvedGeometryProperty)
        {
            var previous = change.GetOldValue<FabGeometryProfile>();
            RaiseTokenOutputs(previous, IsExtended);
            UpdateDerivedProperties();
            UpdateShape(snap: true);
        }
        else if (change.Property == SizeProperty && !_hasMaterialValues && VisualRoot is not null)
        {
            var previous = FallbackGeometry(change.GetOldValue<FabSize>());
            RaiseTokenOutputs(previous, IsExtended);
            ApplyUnscopedFallback();
            UpdateShape(snap: true);
        }
        else if (change.Property == ResolvedVisualColorsProperty
            || change.Property == ResolvedElevationProperty)
        {
            InvalidateVisual();
        }
    }

    private void UpdateDerivedProperties()
    {
        ResolvedContentPadding = IsExtended
            ? new Thickness(EffectiveGeometry.LabelSpace, 0, EffectiveGeometry.LabelSpace, 0)
            : default;
        // Do not install the unscoped compatibility values before attachment.  A control can be
        // constructed before it is placed under a MaterialTheme; an early Style-priority value
        // would then compete with the ControlTheme binding once the control is attached.
        if (!_hasMaterialValues && VisualRoot is not null)
            ApplyUnscopedFallback();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? _theme.Current.Theme.Shapes.Get(EffectiveGeometry.PressedContainerShape)
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
        return _theme.Current.Theme.Shapes.Get(EffectiveGeometry.ContainerShape);
    }

    private double CurrentElevation()
    {
        var interaction = _interaction.Interaction;

        return ResolvedElevation.For(IsLowered, interaction);
    }

    private FabGeometryProfile EffectiveGeometry =>
        _hasMaterialValues ? ResolvedGeometry : FallbackGeometry(Size);

    private void ApplyUnscopedFallback()
    {
        var geometry = EffectiveGeometry;
        SetValue(MinHeightProperty, geometry.ContainerHeight, BindingPriority.Style);
        SetValue(
            MinWidthProperty,
            IsExtended ? geometry.ContainerHeight : geometry.ContainerWidth,
            BindingPriority.Style);
        SetValue(
            PaddingProperty,
            IsExtended
                ? new Thickness(geometry.LabelSpace, 0, geometry.LabelSpace, 0)
                : default(Thickness),
            BindingPriority.Style);
    }

    private static FabGeometryProfile FallbackGeometry(FabSize size) => size switch
    {
        FabSize.Small => DefaultGeometry,
        FabSize.Medium => new FabGeometryProfile(
            80,
            80,
            ShapeRole.CornerLargeIncreased,
            ShapeRole.CornerLarge,
            28,
            24,
            16,
            TypographyRole.TitleMedium),
        FabSize.Large => new FabGeometryProfile(
            96,
            96,
            ShapeRole.CornerExtraLarge,
            ShapeRole.CornerLarge,
            32,
            28,
            20,
            TypographyRole.HeadlineSmall),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}

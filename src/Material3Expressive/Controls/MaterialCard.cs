using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>The three visual variants available to a Material card.</summary>
/// <remarks>
/// This is component API rather than generated token metadata. The raw card token tables retain
/// the upstream facts; <see cref="CardValueKeys" /> owns the consumer-ready mapping while this
/// axis remains a ControlTheme selection policy.
/// </remarks>
public enum CardVariant
{
    /// <summary>A low surface lifted from its background by elevation.</summary>
    Elevated,

    /// <summary>A flat surface using the highest surface-container role.</summary>
    Filled,

    /// <summary>A plain surface behind an outline.</summary>
    Outlined
}

/// <summary>
/// A Material 3 card: elevated, filled or outlined.
/// </summary>
/// <remarks>
/// A container first and a control second, so it derives from <see cref="ContentControl" /> and is
/// inert until <see cref="IsClickable" /> is set. That flag is not decoration: Material says a card
/// only shows a state layer and raises on hover when it is actually actionable, and a card that
/// looks pressable but is not is worse than one that looks flat.
/// </remarks>
public class MaterialCard : ContentControl, IMaterialInteractionConsumer
{
    public static readonly StyledProperty<CardVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialCard, CardVariant>(nameof(Variant), CardVariant.Filled);

    /// <inheritdoc cref="IsClickable" />
    public static readonly StyledProperty<bool> IsClickableProperty =
        AvaloniaProperty.Register<MaterialCard, bool>(nameof(IsClickable));

    // These hidden semantic properties carry the typed MaterialValue results selected by the
    // ControlTheme. Public Background/Border/BorderThickness remain ordinary setters so an
    // application Style or a local value retains normal Avalonia precedence.
    internal static readonly StyledProperty<CardVisualColors?> ResolvedVisualColorsProperty =
        AvaloniaProperty.Register<MaterialCard, CardVisualColors?>(nameof(ResolvedVisualColors));
    internal static readonly StyledProperty<CardGeometryProfile?> ResolvedGeometryProperty =
        AvaloniaProperty.Register<MaterialCard, CardGeometryProfile?>(nameof(ResolvedGeometry));
    internal static readonly StyledProperty<CardElevationProfile?> ResolvedElevationProperty =
        AvaloniaProperty.Register<MaterialCard, CardElevationProfile?>(nameof(ResolvedElevation));

    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;

    private static readonly CardVisualColors EmptyVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        Colors.Black);
    private static readonly CardGeometryProfile EmptyGeometry = new(
        ShapeRole.CornerMedium,
        new Thickness());
    private static readonly CardElevationProfile EmptyElevation = new(0, 0, 0, 0, 0, 0);

    public MaterialCard()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;
    }

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    static MaterialCard()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialCard>(false);
        AffectsRender<MaterialCard>(
            VariantProperty,
            IsClickableProperty,
            IsEffectivelyEnabledProperty,
            BackgroundProperty,
            BorderBrushProperty,
            BorderThicknessProperty,
            ResolvedVisualColorsProperty,
            ResolvedGeometryProperty,
            ResolvedElevationProperty);
    }

    /// <summary>Elevated, filled or outlined.</summary>
    public CardVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// Whether the card responds to interaction. Off by default: Material only gives a card a
    /// state layer and a hover lift when it is genuinely actionable.
    /// </summary>
    public bool IsClickable
    {
        get => GetValue(IsClickableProperty);
        set => SetValue(IsClickableProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected colour profile.</summary>
    internal CardVisualColors? ResolvedVisualColors
    {
        get => GetValue(ResolvedVisualColorsProperty);
        set => SetValue(ResolvedVisualColorsProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected geometry profile.</summary>
    internal CardGeometryProfile? ResolvedGeometry
    {
        get => GetValue(ResolvedGeometryProperty);
        set => SetValue(ResolvedGeometryProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected elevation profile.</summary>
    internal CardElevationProfile? ResolvedElevation
    {
        get => GetValue(ResolvedElevationProperty);
        set => SetValue(ResolvedElevationProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var enabled = IsEffectivelyEnabled;
        var colors = ResolvedVisualColors ?? EmptyVisualColors;
        var geometry = ResolvedGeometry ?? EmptyGeometry;
        var corners = _theme.Current.Theme.Shapes.GetCornerRadius(geometry.ContainerShape, Bounds.Size);
        var fill = Background ?? (enabled ? colors.ContainerColor : colors.DisabledContainerColor);
        var outline = BorderBrush ?? (enabled ? colors.OutlineColor : colors.DisabledOutlineColor);
        var outlineWidth = Uniform(BorderThickness);
        if (outlineWidth <= 0)
            outlineWidth = Uniform(geometry.BorderThickness);

        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            fill,
            outline,
            outlineWidth,
            CurrentElevation(enabled),
            colors.ShadowColor);

        base.Render(context);

        if (IsClickable)
            _interaction.DrawLayers(context, colors.StateLayerColor, corners);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsClickableProperty)
            Focusable = IsClickable;
    }

    private double CurrentElevation(bool enabled)
    {
        var interaction = IsClickable ? _interaction.Interaction : InteractionState.None;
        return (ResolvedElevation ?? EmptyElevation).For(enabled, interaction);
    }

    private static double Uniform(Thickness thickness) =>
        Math.Max(Math.Max(thickness.Left, thickness.Top), Math.Max(thickness.Right, thickness.Bottom));

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>The five colour variants available to a Material button.</summary>
/// <remarks>
/// This is component API, not a generated token-set enum. The raw token tables describe the
/// upstream facts; <see cref="ButtonDefaults" /> owns the mapping from this axis to those facts.
/// </remarks>
public enum ButtonVariant
{
    /// <summary>High emphasis: a primary-coloured container.</summary>
    Filled,

    /// <summary>Medium emphasis: a secondary-container fill.</summary>
    Tonal,

    /// <summary>A low surface fill separated from the background by elevation.</summary>
    Elevated,

    /// <summary>A transparent container behind an outline.</summary>
    Outlined,

    /// <summary>Lowest emphasis: label and state layer only.</summary>
    Text
}

/// <summary>The five expressive size presets available to Material buttons.</summary>
/// <remarks>
/// This is component API, not a generated defaults-table enum. <see cref="ButtonDefaults" />
/// owns size-to-raw-token mapping, Compose corrections, and Avalonia adaptations.
/// </remarks>
public enum ButtonSize
{
    /// <summary>A compact 32dp button.</summary>
    ExtraSmall,

    /// <summary>The default 40dp button.</summary>
    Small,

    /// <summary>A 56dp button.</summary>
    Medium,

    /// <summary>A 96dp button.</summary>
    Large,

    /// <summary>A 136dp button.</summary>
    ExtraLarge
}

/// <summary>
/// Material button options shared by native <see cref="Button" /> and
/// <see cref="Avalonia.Controls.Primitives.ToggleButton" /> controls.
/// </summary>
/// <remarks>
/// These attached properties are the public Button-family API. Named control themes only provide
/// defaults for them; no Material Button CLR facade is required.
/// </remarks>
public static class M3Button
{
    /// <summary>
    /// Internal activation marker installed by the library's Material Button control theme.
    /// </summary>
    /// <remarks>
    /// The marker deliberately has no visual meaning. It gives native <see cref="Button" />
    /// controls an owner-local lifecycle boundary without making every Button in an application
    /// allocate Material defaults state.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter activation. New themes must use MaterialValueKey<T>.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<bool> UseMaterialDefaultsProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>(
            "UseMaterialDefaults", typeof(M3Button), defaultValue: false);

    public static readonly AttachedProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonVariant>(
            "Variant", typeof(M3Button), ButtonVariant.Filled, inherits: true);

    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonSize>(
            "Size", typeof(M3Button), ButtonSize.Small, inherits: true);

    public static readonly AttachedProperty<bool> IsSquareProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsSquare", typeof(M3Button));

    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.RegisterAttached<Button, RoundedCornerShape?>(
            "ContainerShapeOverride", typeof(M3Button));

    // These are resolved fallback profiles. They are intentionally separate from ordinary styled
    // properties so their local values never compete with application Background/Padding/etc.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<ButtonColors?> DefaultColorsProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonColors?>(
            "DefaultColors", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<ButtonMetrics?> DefaultMetricsProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonMetrics?>(
            "DefaultMetrics", typeof(M3Button));

    // These semantic properties carry the new MaterialValue results inside the ControlTheme.
    // Variant/size selectors choose a typed endpoint, while the ordinary public properties keep a
    // single base-theme setter so application styles and local values retain Avalonia precedence.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<IBrush?> ResolvedContainerBrushProperty =
        AvaloniaProperty.RegisterAttached<Button, IBrush?>(
            "ResolvedContainerBrush", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<double> ResolvedContainerHeightProperty =
        AvaloniaProperty.RegisterAttached<Button, double>(
            "ResolvedContainerHeight", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<Thickness> ResolvedContentPaddingProperty =
        AvaloniaProperty.RegisterAttached<Button, Thickness>(
            "ResolvedContentPadding", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<TypographyRole> ResolvedTextStyleProperty =
        AvaloniaProperty.RegisterAttached<Button, TypographyRole>(
            "ResolvedTextStyle", typeof(M3Button), TypographyRole.LabelLarge);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ButtonVisualColors?> ResolvedVisualColorsProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonVisualColors?>(
            "ResolvedVisualColors", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ButtonElevationProfile?> ResolvedElevationProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonElevationProfile?>(
            "ResolvedElevation", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<SpringSpec> ResolvedShapeMotionProperty =
        AvaloniaProperty.RegisterAttached<Button, SpringSpec>(
            "ResolvedShapeMotion", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<Thickness> ResolvedOutlineThicknessProperty =
        AvaloniaProperty.RegisterAttached<Button, Thickness>(
            "ResolvedOutlineThickness", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy Button shape-profile transport. Shape roles are now supplied through " +
        "ButtonValueKeys and MaterialButtonContainerPresenter.ShapeProfile.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<ButtonShapes?> DefaultShapesProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonShapes?>(
            "DefaultShapes", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<ButtonElevation?> DefaultElevationProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonElevation?>(
            "DefaultElevation", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<SpringSpec> DefaultShapeMotionProperty =
        AvaloniaProperty.RegisterAttached<Button, SpringSpec>(
            "DefaultShapeMotion", typeof(M3Button));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Legacy component-defaults adapter output. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    public static readonly AttachedProperty<Thickness> DefaultOutlineThicknessProperty =
        AvaloniaProperty.RegisterAttached<Button, Thickness>(
            "DefaultOutlineThickness", typeof(M3Button));

    /// <summary>
    /// Overrides the state-layer brush projected by the Material Button control theme.
    /// </summary>
    /// <remarks>
    /// This is deliberately a normal styled property: applications can override it with a Style
    /// or a local value without reaching into a generated token table.
    /// </remarks>
    public static readonly AttachedProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<Button, IBrush?>(
            "StateLayerBrush", typeof(M3Button));

    static M3Button()
    {
        UseMaterialDefaultsProperty.Changed.AddClassHandler<Button>(static (button, _) =>
        {
            if (button.GetValue(UseMaterialDefaultsProperty))
                ButtonDefaultsAdapter.GetOrCreate(button).Activate();
            else if (ButtonDefaultsAdapter.TryGet(button, out var adapter))
                adapter?.Deactivate();
        });
    }

    public static ButtonVariant GetVariant(Button button) => button.GetValue(VariantProperty);

    public static void SetVariant(Button button, ButtonVariant value) =>
        button.SetValue(VariantProperty, value);

    public static ButtonSize GetSize(Button button) => button.GetValue(SizeProperty);

    public static void SetSize(Button button, ButtonSize value) => button.SetValue(SizeProperty, value);

    public static bool GetIsSquare(Button button) => button.GetValue(IsSquareProperty);

    public static void SetIsSquare(Button button, bool value) => button.SetValue(IsSquareProperty, value);

    public static RoundedCornerShape? GetContainerShapeOverride(Button button) =>
        button.GetValue(ContainerShapeOverrideProperty);

    public static void SetContainerShapeOverride(Button button, RoundedCornerShape? value) =>
        button.SetValue(ContainerShapeOverrideProperty, value);

    internal static ButtonColors? GetDefaultColors(Button button) =>
        button.GetValue(DefaultColorsProperty);

    internal static void SetDefaultColors(Button button, ButtonColors? value) =>
        button.SetValue(DefaultColorsProperty, value);

    internal static ButtonMetrics? GetDefaultMetrics(Button button) =>
        button.GetValue(DefaultMetricsProperty);

    internal static void SetDefaultMetrics(Button button, ButtonMetrics? value) =>
        button.SetValue(DefaultMetricsProperty, value);

    [Obsolete(
        "Legacy Button shape-profile transport. Shape roles are now supplied through " +
        "ButtonValueKeys and MaterialButtonContainerPresenter.ShapeProfile.",
        DiagnosticId = "M3E1004")]
    internal static ButtonShapes? GetDefaultShapes(Button button) =>
        button.GetValue(DefaultShapesProperty);

    [Obsolete(
        "Legacy Button shape-profile transport. Shape roles are now supplied through " +
        "ButtonValueKeys and MaterialButtonContainerPresenter.ShapeProfile.",
        DiagnosticId = "M3E1004")]
    internal static void SetDefaultShapes(Button button, ButtonShapes? value) =>
        button.SetValue(DefaultShapesProperty, value);

    internal static ButtonElevation? GetDefaultElevation(Button button) =>
        button.GetValue(DefaultElevationProperty);

    internal static void SetDefaultElevation(Button button, ButtonElevation? value) =>
        button.SetValue(DefaultElevationProperty, value);

    internal static SpringSpec GetDefaultShapeMotion(Button button) =>
        button.GetValue(DefaultShapeMotionProperty);

    internal static void SetDefaultShapeMotion(Button button, SpringSpec value) =>
        button.SetValue(DefaultShapeMotionProperty, value);

    internal static Thickness GetDefaultOutlineThickness(Button button) =>
        button.GetValue(DefaultOutlineThicknessProperty);

    internal static void SetDefaultOutlineThickness(Button button, Thickness value) =>
        button.SetValue(DefaultOutlineThicknessProperty, value);

    internal static ButtonVisualColors? GetResolvedVisualColors(Button button) =>
        button.GetValue(ResolvedVisualColorsProperty);

    internal static ButtonElevationProfile? GetResolvedElevation(Button button) =>
        button.GetValue(ResolvedElevationProperty);

    internal static SpringSpec GetResolvedShapeMotion(Button button) =>
        button.GetValue(ResolvedShapeMotionProperty);

    public static IBrush? GetStateLayerBrush(Button button) =>
        button.GetValue(StateLayerBrushProperty);

    public static void SetStateLayerBrush(Button button, IBrush? value) =>
        button.SetValue(StateLayerBrushProperty, value);

    internal static bool GetUseMaterialDefaults(Button button) =>
        button.GetValue(UseMaterialDefaultsProperty);

    internal static void SetUseMaterialDefaults(Button button, bool value) =>
        button.SetValue(UseMaterialDefaultsProperty, value);
}

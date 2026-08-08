using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Controls;

namespace Material3Expressive.Foundation;

/// <summary>
/// Material button options shared by native <see cref="Button" /> controls and CLR facades.
/// </summary>
/// <remarks>
/// These attached properties are the canonical state for the button implementation. Named
/// control themes only provide defaults for them, and facade properties add themselves as owners
/// of these same property instances rather than copying values into a second state store.
/// </remarks>
public static class ButtonAssist
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
    public static readonly AttachedProperty<bool> UseMaterialDefaultsProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>(
            "UseMaterialDefaults", typeof(ButtonAssist), defaultValue: false);

    public static readonly AttachedProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonVariant>(
            "Variant", typeof(ButtonAssist), ButtonVariant.Filled, inherits: true);

    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonSize>(
            "Size", typeof(ButtonAssist), ButtonSize.Small, inherits: true);

    public static readonly AttachedProperty<bool> IsSquareProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsSquare", typeof(ButtonAssist));

    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.RegisterAttached<Button, RoundedCornerShape?>(
            "ContainerShapeOverride", typeof(ButtonAssist));

    // These are resolved fallback profiles. They are intentionally separate from ordinary styled
    // properties so their local values never compete with application Background/Padding/etc.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ButtonColors?> DefaultColorsProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonColors?>(
            "DefaultColors", typeof(ButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ButtonMetrics?> DefaultMetricsProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonMetrics?>(
            "DefaultMetrics", typeof(ButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ButtonShapes?> DefaultShapesProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonShapes?>(
            "DefaultShapes", typeof(ButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<ButtonElevation?> DefaultElevationProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonElevation?>(
            "DefaultElevation", typeof(ButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<SpringSpec> DefaultShapeMotionProperty =
        AvaloniaProperty.RegisterAttached<Button, SpringSpec>(
            "DefaultShapeMotion", typeof(ButtonAssist));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<Thickness> DefaultOutlineThicknessProperty =
        AvaloniaProperty.RegisterAttached<Button, Thickness>(
            "DefaultOutlineThickness", typeof(ButtonAssist));

    /// <summary>
    /// Overrides the state-layer brush projected by the Material Button control theme.
    /// </summary>
    /// <remarks>
    /// This is deliberately a normal styled property: applications can override it with a Style
    /// or a local value without reaching into a generated token table.
    /// </remarks>
    public static readonly AttachedProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<Button, IBrush?>(
            "StateLayerBrush", typeof(ButtonAssist));

    static ButtonAssist()
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

    internal static ButtonShapes? GetDefaultShapes(Button button) =>
        button.GetValue(DefaultShapesProperty);

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

    public static IBrush? GetStateLayerBrush(Button button) =>
        button.GetValue(StateLayerBrushProperty);

    public static void SetStateLayerBrush(Button button, IBrush? value) =>
        button.SetValue(StateLayerBrushProperty, value);

    internal static bool GetUseMaterialDefaults(Button button) =>
        button.GetValue(UseMaterialDefaultsProperty);

    internal static void SetUseMaterialDefaults(Button button, bool value) =>
        button.SetValue(UseMaterialDefaultsProperty, value);
}

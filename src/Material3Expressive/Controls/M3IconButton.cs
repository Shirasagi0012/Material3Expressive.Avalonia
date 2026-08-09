using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>The four colour variants available to an icon button.</summary>
/// <remarks>
/// This is component API rather than generated token metadata. The raw <c>.comp.jsonc</c>
/// tables record upstream facts; <see cref="IconButtonDefaults" /> owns the mapping from this axis
/// to those tables and to the active system theme.
/// </remarks>
public enum IconButtonVariant
{
    /// <summary>An icon without a container.</summary>
    Standard,

    /// <summary>High emphasis: a primary-coloured container.</summary>
    Filled,

    /// <summary>Medium emphasis: a secondary-container fill.</summary>
    Tonal,

    /// <summary>A transparent container behind an outline.</summary>
    Outlined
}

/// <summary>The five expressive size presets available to icon buttons.</summary>
/// <remarks>
/// The enum is deliberately hand-authored. Size-to-token mapping and any Avalonia shaping are
/// policy owned by <see cref="IconButtonDefaults" />, not by a generated defaults table.
/// </remarks>
public enum IconButtonSize
{
    /// <summary>A compact 32dp icon button.</summary>
    ExtraSmall,

    /// <summary>The default 40dp icon button.</summary>
    Small,

    /// <summary>A 56dp icon button.</summary>
    Medium,

    /// <summary>A 96dp icon button.</summary>
    Large,

    /// <summary>A 136dp icon button.</summary>
    ExtraLarge
}

/// <summary>How much horizontal room an icon button leaves around its icon.</summary>
public enum IconButtonWidth
{
    Narrow,
    Default,
    Wide
}

/// <summary>
/// Material icon-button options shared by native <see cref="Button" /> and
/// <see cref="ToggleButton" /> controls.
/// </summary>
/// <remarks>
/// These attached properties are the public icon-button family API. Named control themes only
/// provide defaults for them; no Material IconButton CLR facade is required.
/// </remarks>
public static class M3IconButton
{
    /// <summary>
    /// Internal activation marker installed by the Material icon-button ControlTheme.
    /// </summary>
    /// <remarks>
    /// A single marker is used for both native Button and ToggleButton owners; the class handler
    /// dispatches to the corresponding owner-local adapter. Ordinary Buttons that do not use a
    /// Material icon-button theme never allocate a defaults adapter.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<bool> UseMaterialDefaultsProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>(
            "UseMaterialDefaults", typeof(M3IconButton), defaultValue: false);

    public static readonly AttachedProperty<IconButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonVariant>(
            "Variant", typeof(M3IconButton), IconButtonVariant.Standard, inherits: true);

    public static readonly AttachedProperty<IconButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonSize>(
            "Size", typeof(M3IconButton), IconButtonSize.Small, inherits: true);

    public static readonly AttachedProperty<IconButtonWidth> ButtonWidthProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonWidth>(
            "ButtonWidth", typeof(M3IconButton), IconButtonWidth.Default, inherits: true);

    public static readonly AttachedProperty<bool> IsSquareProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsSquare", typeof(M3IconButton));

    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.RegisterAttached<Button, RoundedCornerShape?>(
            "ContainerShapeOverride", typeof(M3IconButton));

    public static readonly AttachedProperty<double> IconSizeProperty =
        AvaloniaProperty.RegisterAttached<Button, double>(
            "IconSize", typeof(M3IconButton), 24);

    /// <summary>Marks a Button/ToggleButton as using the icon-button semantic visual contract.</summary>
    public static readonly AttachedProperty<bool> IsIconButtonProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>(
            "IsIconButton", typeof(M3IconButton));

    // These are resolved fallback profiles. They intentionally remain separate from ordinary
    // presentation properties so adapter-owned values never compete with app Background/Padding
    // or local values.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<IconButtonColors?> DefaultColorsProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonColors?>(
            "DefaultColors", typeof(M3IconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<IconButtonMetrics?> DefaultMetricsProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonMetrics?>(
            "DefaultMetrics", typeof(M3IconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<IconButtonShapes?> DefaultShapesProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonShapes?>(
            "DefaultShapes", typeof(M3IconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<SpringSpec> DefaultShapeMotionProperty =
        AvaloniaProperty.RegisterAttached<Button, SpringSpec>(
            "DefaultShapeMotion", typeof(M3IconButton));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly AttachedProperty<Thickness> DefaultOutlineThicknessProperty =
        AvaloniaProperty.RegisterAttached<Button, Thickness>(
            "DefaultOutlineThickness", typeof(M3IconButton));

    /// <summary>Overrides the state-layer brush projected by the Material icon-button theme.</summary>
    public static readonly AttachedProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.RegisterAttached<Button, IBrush?>(
            "StateLayerBrush", typeof(M3IconButton));

    static M3IconButton()
    {
        UseMaterialDefaultsProperty.Changed.AddClassHandler<Button>(static (button, _) =>
        {
            if (button.GetValue(UseMaterialDefaultsProperty))
            {
                if (button is ToggleButton toggle)
                    ToggleIconButtonDefaultsAdapter.GetOrCreate(toggle).Activate();
                else
                    IconButtonDefaultsAdapter.GetOrCreate(button).Activate();
            }
            else if (button is ToggleButton toggle)
            {
                if (ToggleIconButtonDefaultsAdapter.TryGet(toggle, out var adapter))
                    adapter?.Deactivate();
            }
            else if (IconButtonDefaultsAdapter.TryGet(button, out var adapter))
            {
                adapter?.Deactivate();
            }
        });
    }

    public static IconButtonVariant GetVariant(Button button) => button.GetValue(VariantProperty);

    public static void SetVariant(Button button, IconButtonVariant value) =>
        button.SetValue(VariantProperty, value);

    public static IconButtonSize GetSize(Button button) => button.GetValue(SizeProperty);

    public static void SetSize(Button button, IconButtonSize value) => button.SetValue(SizeProperty, value);

    public static IconButtonWidth GetButtonWidth(Button button) => button.GetValue(ButtonWidthProperty);

    public static void SetButtonWidth(Button button, IconButtonWidth value) =>
        button.SetValue(ButtonWidthProperty, value);

    public static bool GetIsSquare(Button button) => button.GetValue(IsSquareProperty);

    public static void SetIsSquare(Button button, bool value) => button.SetValue(IsSquareProperty, value);

    public static RoundedCornerShape? GetContainerShapeOverride(Button button) =>
        button.GetValue(ContainerShapeOverrideProperty);

    public static void SetContainerShapeOverride(Button button, RoundedCornerShape? value) =>
        button.SetValue(ContainerShapeOverrideProperty, value);

    public static double GetIconSize(Button button) => button.GetValue(IconSizeProperty);

    public static bool GetIsIconButton(Button button) => button.GetValue(IsIconButtonProperty);

    public static void SetIsIconButton(Button button, bool value) =>
        button.SetValue(IsIconButtonProperty, value);

    internal static IconButtonColors? GetDefaultColors(Button button) =>
        button.GetValue(DefaultColorsProperty);

    internal static void SetDefaultColors(Button button, IconButtonColors? value) =>
        button.SetValue(DefaultColorsProperty, value);

    internal static IconButtonMetrics? GetDefaultMetrics(Button button) =>
        button.GetValue(DefaultMetricsProperty);

    internal static void SetDefaultMetrics(Button button, IconButtonMetrics? value) =>
        button.SetValue(DefaultMetricsProperty, value);

    internal static IconButtonShapes? GetDefaultShapes(Button button) =>
        button.GetValue(DefaultShapesProperty);

    internal static void SetDefaultShapes(Button button, IconButtonShapes? value) =>
        button.SetValue(DefaultShapesProperty, value);

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

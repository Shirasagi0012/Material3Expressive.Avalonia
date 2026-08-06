using System;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls.ButtonRuntime;

/// <summary>
/// The runtime colour contract owned by ToggleButton. A normal button's enabled colour is not a
/// valid model for a toggle: checked and unchecked content/container pairs are independent states,
/// and the state layer follows the currently selected content colour.
/// </summary>
internal sealed record ToggleButtonColors(
    IImmutableBrush UncheckedContainerColor,
    IImmutableBrush UncheckedContentColor,
    IImmutableBrush CheckedContainerColor,
    IImmutableBrush CheckedContentColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush DisabledContentColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush DisabledOutlineColor,
    bool HasOutline)
{
    public IImmutableBrush ContainerColor(bool enabled, bool checkedState) =>
        !enabled ? DisabledContainerColor
        : checkedState ? CheckedContainerColor
        : UncheckedContainerColor;

    public IImmutableBrush ContentColor(bool enabled, bool checkedState) =>
        !enabled ? DisabledContentColor
        : checkedState ? CheckedContentColor
        : UncheckedContentColor;

    public IImmutableBrush StateLayerColor(bool checkedState) =>
        checkedState ? CheckedContentColor : UncheckedContentColor;
}

/// <summary>Three geometry states used by a ToggleButton's interruptible shape morph.</summary>
internal sealed record ToggleButtonShapes(
    ShapeRole UncheckedShape,
    ShapeRole PressedShape,
    ShapeRole CheckedShape);

/// <summary>
/// Adapts the generated ToggleButton defaults into a runtime record. The table owns the static
/// state values; conditional checked/disabled selection and Avalonia property binding are control
/// policy, and belong in C# beside the control.
/// </summary>
internal static class ToggleButtonDefaults
{
    /// <summary>Returns the generated toggle colour tokens for a button-facing variant.</summary>
    public static ToggleButtonColorTokens ColorTokens(ResolvedTheme theme, ButtonVariant variant) =>
        theme.GetToggleButtonColorTokens(ToToggleVariant(variant));

    public static ToggleButtonColors Colors(ResolvedTheme theme, ButtonVariant variant, bool isDark)
    {
        var toggleVariant = ToToggleVariant(variant);
        var colors = theme.GetToggleButtonColors(toggleVariant, isDark);
        var tokens = ColorTokens(theme, variant);
        return new ToggleButtonColors(
            colors.UncheckedContainerColor,
            colors.UncheckedContentColor,
            colors.CheckedContainerColor,
            colors.CheckedContentColor,
            colors.DisabledContainerColor,
            colors.DisabledContentColor,
            colors.OutlineColor,
            colors.DisabledOutlineColor,
            tokens.HasOutline);
    }

    public static ToggleButtonShapes Shapes(ResolvedTheme theme, ButtonSize size)
    {
        var metrics = theme.GetButtonSizeTokens(size);
        return new ToggleButtonShapes(
            metrics.ContainerShape,
            metrics.PressedContainerShape,
            metrics.SquareContainerShape);
    }

    private static ToggleButtonVariant ToToggleVariant(ButtonVariant variant) => variant switch
    {
        ButtonVariant.Filled => ToggleButtonVariant.Filled,
        ButtonVariant.Tonal => ToggleButtonVariant.Tonal,
        ButtonVariant.Elevated => ToggleButtonVariant.Elevated,
        ButtonVariant.Outlined => ToggleButtonVariant.Outlined,
        ButtonVariant.Text => ToggleButtonVariant.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };
}

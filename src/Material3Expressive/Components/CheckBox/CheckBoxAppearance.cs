using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation.Interaction;
using SkiaSharp;

namespace Material3Expressive.Components;

/// <summary>Material paint options for the native Avalonia CheckBox. State and input remain native.</summary>
public static class CheckBoxAppearance
{
    public static readonly AttachedProperty<MaterialInteractionSource?> InteractionSourceProperty =
        AvaloniaProperty.RegisterAttached<CheckBox, MaterialInteractionSource?>("InteractionSource", typeof(CheckBoxAppearance));
    public static MaterialInteractionSource? GetInteractionSource(CheckBox owner) => owner.GetValue(InteractionSourceProperty);
    public static void SetInteractionSource(CheckBox owner, MaterialInteractionSource? value) => owner.SetValue(InteractionSourceProperty, value);

    public static readonly AttachedProperty<CheckBoxColors?> ColorsProperty =
        AvaloniaProperty.RegisterAttached<CheckBox, CheckBoxColors?>("Colors", typeof(CheckBoxAppearance));
    public static CheckBoxColors? GetColors(CheckBox owner) => owner.GetValue(ColorsProperty);
    public static void SetColors(CheckBox owner, CheckBoxColors? value) => owner.SetValue(ColorsProperty, value);

    public static readonly AttachedProperty<CheckBoxStroke?> CheckmarkStrokeProperty =
        AvaloniaProperty.RegisterAttached<CheckBox, CheckBoxStroke?>("CheckmarkStroke", typeof(CheckBoxAppearance));
    public static CheckBoxStroke? GetCheckmarkStroke(CheckBox owner) => owner.GetValue(CheckmarkStrokeProperty);
    public static void SetCheckmarkStroke(CheckBox owner, CheckBoxStroke? value) => owner.SetValue(CheckmarkStrokeProperty, value);

    public static readonly AttachedProperty<CheckBoxStroke?> OutlineStrokeProperty =
        AvaloniaProperty.RegisterAttached<CheckBox, CheckBoxStroke?>("OutlineStroke", typeof(CheckBoxAppearance));
    public static CheckBoxStroke? GetOutlineStroke(CheckBox owner) => owner.GetValue(OutlineStrokeProperty);
    public static void SetOutlineStroke(CheckBox owner, CheckBoxStroke? value) => owner.SetValue(OutlineStrokeProperty, value);
}

/// <summary>The thirteen slots of fixed Compose CheckboxColors; null Colors uses dynamic theme tokens.</summary>
public sealed record CheckBoxColors(Color CheckedCheckmarkColor, Color UncheckedCheckmarkColor,
    Color CheckedBoxColor, Color UncheckedBoxColor, Color DisabledCheckedBoxColor,
    Color DisabledUncheckedBoxColor, Color DisabledIndeterminateBoxColor, Color CheckedBorderColor,
    Color UncheckedBorderColor, Color DisabledBorderColor, Color DisabledUncheckedBorderColor,
    Color DisabledIndeterminateBorderColor, Color DisabledCheckmarkColor);

/// <summary>Compose Stroke with Width, Dashes and DashOffset in absolute DIP (not pen-width multiples).
/// PathEffect is a caller-owned Skia effect expressed in physical pixels.</summary>
public sealed record CheckBoxStroke
{
    public double Width { get; init; } = 2;
    public PenLineCap Cap { get; init; } = PenLineCap.Square;
    public PenLineJoin Join { get; init; } = PenLineJoin.Miter;
    public double MiterLimit { get; init; } = 4;
    public double[]? Dashes { get; init; }
    public double DashOffset { get; init; }
    /// <summary>Optional Compose/Skia path effect in physical pixels. Takes precedence over
    /// Dashes and DashOffset; the caller retains ownership and must keep it alive while in use.</summary>
    public SKPathEffect? PathEffect { get; init; }
}

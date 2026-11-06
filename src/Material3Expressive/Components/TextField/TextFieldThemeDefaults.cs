using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material3Expressive.Components;

// State triggers select theme defaults here. The native properties receive them
// through ordinary theme bindings, so application styles and local values win.
internal static class TextFieldThemeDefaults
{
    // Template-owned source, including the private fallback. Consumers observe the same
    // stream as the decoration without installing a second native input controller.
    internal static readonly AttachedProperty<Foundation.Interaction.MaterialInteractionSource?> EffectiveInteractionSourceProperty =
        AvaloniaProperty.RegisterAttached<TextBox, Foundation.Interaction.MaterialInteractionSource?>("EffectiveInteractionSource", typeof(TextFieldThemeDefaults));
    // Core popup scenes preserve the parent's focused editor while their own focus owner
    // navigates the menu. The Avalonia overlay shares one focus manager instead.
    internal static readonly AttachedProperty<bool> RetainPopupFocusProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("RetainPopupFocus", typeof(TextFieldThemeDefaults));

    public static readonly AttachedProperty<bool> IsFocusedProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsFocused", typeof(TextFieldThemeDefaults));
    public static bool GetIsFocused(TextBox owner) => owner.GetValue(IsFocusedProperty);
    public static void SetIsFocused(TextBox owner, bool value) => owner.SetValue(IsFocusedProperty, value);

    public static readonly AttachedProperty<bool> IndicatorFocusedProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IndicatorFocused", typeof(TextFieldThemeDefaults));
    public static bool GetIndicatorFocused(TextBox owner) => owner.GetValue(IndicatorFocusedProperty);
    public static void SetIndicatorFocused(TextBox owner, bool value) => owner.SetValue(IndicatorFocusedProperty, value);

    public static readonly AttachedProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IBrush?>("Foreground", typeof(TextFieldThemeDefaults));
    public static IBrush? GetForeground(TextBox owner) => owner.GetValue(ForegroundProperty);
    public static void SetForeground(TextBox owner, IBrush? value) => owner.SetValue(ForegroundProperty, value);

    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IBrush?>("Background", typeof(TextFieldThemeDefaults));
    public static IBrush? GetBackground(TextBox owner) => owner.GetValue(BackgroundProperty);
    public static void SetBackground(TextBox owner, IBrush? value) => owner.SetValue(BackgroundProperty, value);

    public static readonly AttachedProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IBrush?>("BorderBrush", typeof(TextFieldThemeDefaults));
    public static IBrush? GetBorderBrush(TextBox owner) => owner.GetValue(BorderBrushProperty);
    public static void SetBorderBrush(TextBox owner, IBrush? value) => owner.SetValue(BorderBrushProperty, value);

    public static readonly AttachedProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IBrush?>("PlaceholderForeground", typeof(TextFieldThemeDefaults));
    public static IBrush? GetPlaceholderForeground(TextBox owner) => owner.GetValue(PlaceholderForegroundProperty);
    public static void SetPlaceholderForeground(TextBox owner, IBrush? value) => owner.SetValue(PlaceholderForegroundProperty, value);

    public static readonly AttachedProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IBrush?>("CaretBrush", typeof(TextFieldThemeDefaults));
    public static IBrush? GetCaretBrush(TextBox owner) => owner.GetValue(CaretBrushProperty);
    public static void SetCaretBrush(TextBox owner, IBrush? value) => owner.SetValue(CaretBrushProperty, value);

}

// Mirrors the decoration's label-presence rule without embedding baseline values
// or consulting property precedence outside Avalonia.
internal sealed class TextFieldPaddingDefaultConverter : IMultiValueConverter
{
    public static TextFieldPaddingDefaultConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count < 4 ? AvaloniaProperty.UnsetValue :
        values[0] is not null && values[0] != AvaloniaProperty.UnsetValue &&
        values[1] is TextFieldLabelPosition.Inside ? values[3] : values[2];
}

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

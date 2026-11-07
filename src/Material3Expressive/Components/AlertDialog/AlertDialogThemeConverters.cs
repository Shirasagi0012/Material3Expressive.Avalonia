using Avalonia;
using Avalonia.Data.Converters;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

internal static class AlertDialogThemeConverters
{
    public static readonly IMultiValueConverter ContentPadding = new FuncMultiValueConverter<object?, object?>(values =>
        values.ToArray() is [bool precision, Thickness normal, Thickness compact] ? precision ? compact : normal : AvaloniaProperty.UnsetValue);

    public static readonly IMultiValueConverter TextBottomSpacing = new FuncMultiValueConverter<object?, object?>(values =>
        values.ToArray() is [bool precision, double normal, double compact]
            ? new Thickness(0, 0, 0, precision ? compact : normal) : AvaloniaProperty.UnsetValue);

    public static readonly IMultiValueConverter HeadlineStyle = new FuncMultiValueConverter<object?, object?>(values =>
        values.ToArray() is [bool precision, TextStyle normal, TextStyle compact]
            ? precision ? normal.Merge(compact) : normal : AvaloniaProperty.UnsetValue);

    public static readonly IMultiValueConverter ActionVerticalSpacing = new FuncMultiValueConverter<object?, object?>(values =>
    {
        if (values.ToArray() is not [double spacing, var minimum]) return AvaloniaProperty.UnsetValue;
        if (!double.IsNaN(spacing)) return spacing;
        // AlertDialog.kt: (8.dp - (LocalMinimumInteractiveComponentSize - 40.dp)).coerceIn(0.dp, 8.dp).
        // Null/NaN in the local minimum-size API preserves the component default (48 DIP).
        var size = minimum is double value && !double.IsNaN(value) ? Math.Max(0, value) : 48;
        return Math.Clamp(8 - (size - 40), 0, 8);
    });

    public static readonly IMultiValueConverter BottomSpacing = new FuncMultiValueConverter<object?, object?>(values =>
        values.ToArray() is [double spacing] ? new Thickness(0, 0, 0, spacing) : AvaloniaProperty.UnsetValue);
}

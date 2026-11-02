using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

internal static class AlertDialogThemeConverters
{
    public static readonly IMultiValueConverter BottomSpacing = new FuncMultiValueConverter<object?, object?>(values =>
        values.ToArray() is [double spacing] ? new Thickness(0, 0, 0, spacing) : AvaloniaProperty.UnsetValue);
}

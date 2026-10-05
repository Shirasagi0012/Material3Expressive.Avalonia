using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

// Select the overload's default without promoting it above an ordinary user Style.
internal sealed class ProgressIndicatorModeDefaultConverter : IMultiValueConverter
{
    public static ProgressIndicatorModeDefaultConverter Instance { get; } = new();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count < 3 ? AvaloniaProperty.UnsetValue : values[0] is true ? values[2] : values[1];
}

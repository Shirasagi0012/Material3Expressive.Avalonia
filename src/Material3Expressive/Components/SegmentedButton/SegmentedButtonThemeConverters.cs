using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

internal sealed class SegmentedButtonThemeConverters : IMultiValueConverter
{
    public static IMultiValueConverter OutlineThickness { get; } = new SegmentedButtonThemeConverters();
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [double width] ? new Thickness(width) : AvaloniaProperty.UnsetValue;
}

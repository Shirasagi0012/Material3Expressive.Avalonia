using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

internal sealed class ChipThemeConverters(int kind) : IMultiValueConverter
{
    public static IMultiValueConverter Elevation { get; } = new ChipThemeConverters(0);
    public static IMultiValueConverter Spacing { get; } = new ChipThemeConverters(1);
    public static IMultiValueConverter Shapes { get; } = new ChipThemeConverters(3);
    public static IMultiValueConverter OutlineThickness { get; } = new ChipThemeConverters(2);
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) => kind switch
    {
        0 when values is [double rest, double pressed, double focused, double hovered, double dragged, double disabled] => new ChipElevation(rest, pressed, focused, hovered, dragged, disabled),
        1 when values is [double leading, double trailing] => new ChipSpacing(leading, trailing),
        2 when values is [double width] => new Thickness(width),
        3 when values is [CornerRadius rest, CornerRadius selected, CornerRadius pressed] => new ChipShapes(rest, selected, pressed),
        _ => AvaloniaProperty.UnsetValue,
    };
}

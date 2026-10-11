using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material3Expressive.Components;

internal sealed class MenuItemThemeConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count == 2 ? values[1] as Avalonia.Styling.ControlTheme ?? values[0] : Avalonia.AvaloniaProperty.UnsetValue;
}


// All values are live Token bindings. Native IsSelected is navigation/hover, not selection.
internal sealed class MenuColorConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        object? result;
        if (values.Count == 3) result = values[values[0] is MenuColorStyle.Vibrant ? 2 : 1];
        else if (values.Count == 9)
        {
            var offset = values[0] is MenuColorStyle.Vibrant ? 6 : 3;
            result = values[offset + (values[1] is false ? 2 : values[2] is true ? 1 : 0)];
        }
        else return Avalonia.AvaloniaProperty.UnsetValue;
        return result is Color color && typeof(IBrush).IsAssignableFrom(targetType) ? new SolidColorBrush(color) : result;
    }
}

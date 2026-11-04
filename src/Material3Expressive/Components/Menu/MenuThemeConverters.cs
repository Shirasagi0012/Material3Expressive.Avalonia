using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material3Expressive.Components;


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

internal static class MenuSpacingConverters
{
    public static IMultiValueConverter SupportingMargin { get; } = new FuncMultiValueConverter<object?, object>(values =>
    {
        var inputs = values.ToArray();
        if (inputs is not [var content, var template, Thickness normal, Thickness supporting]) return AvaloniaProperty.UnsetValue;
        return content is not null || template is not null ? supporting : normal;
    });
    public static IMultiValueConverter LeadingMargin { get; } = new FuncMultiValueConverter<object?, Thickness>(values =>
        values.ToArray() is [double spacing] ? new Thickness(0, 0, spacing, 0) : default);

    public static IMultiValueConverter TrailingMargin { get; } = new FuncMultiValueConverter<object?, Thickness>(values =>
        values.ToArray() is [double spacing] ? new Thickness(spacing, 0, 0, 0) : default);
}

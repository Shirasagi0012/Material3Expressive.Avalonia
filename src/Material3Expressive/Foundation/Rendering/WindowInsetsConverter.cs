using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material3Expressive.Foundation.Rendering;

// Physical host insets must survive Avalonia's logical RTL mirror unchanged.
internal sealed class WindowInsetsConverter : IMultiValueConverter
{
    public static IMultiValueConverter Instance { get; } = new WindowInsetsConverter();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count == 2 && values[0] is Thickness insets && values[1] is FlowDirection direction
            ? direction == FlowDirection.RightToLeft
                ? new Thickness(insets.Right, insets.Top, insets.Left, insets.Bottom) : insets
            : AvaloniaProperty.UnsetValue;
}

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace Material3Expressive.Components;

// Conditional defaults stay at theme priority, so an ordinary user Style can override them.
internal sealed class SliderVariantDefaultConverter : IMultiValueConverter
{
    public static SliderVariantDefaultConverter Instance { get; } = new();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count < 3 ? AvaloniaProperty.UnsetValue : values[0] is Orientation.Vertical ? values[2] : values[1];
}

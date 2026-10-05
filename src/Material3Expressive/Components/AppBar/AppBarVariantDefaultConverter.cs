using System.Globalization;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

// A conditional default binding keeps the setter at theme priority. A pseudoclass
// setter on these public properties would outrank ordinary application styles.
internal sealed class AppBarVariantDefaultConverter(bool bottom) : IMultiValueConverter
{
    public static AppBarVariantDefaultConverter FlexibleBottom { get; } = new(true);

    public static AppBarVariantDefaultConverter CenterAligned { get; } = new(false);

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count < 3
            ? Avalonia.AvaloniaProperty.UnsetValue
            : (bottom
                ? values[0] is FlexibleBottomAppBar
                : values[0] is CenterAlignedTopAppBar)
                ? values[2]
                : values[1];
}

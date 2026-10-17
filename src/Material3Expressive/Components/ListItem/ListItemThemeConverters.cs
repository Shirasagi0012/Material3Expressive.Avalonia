using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Components;

internal sealed class ListItemThemeConverters(bool padding) : IMultiValueConverter
{
    public static IValueConverter Brush { get; } = new FuncValueConverter<Color, IBrush>(color => new ImmutableSolidColorBrush(color));
    public static IValueConverter Interactive { get; } = new FuncValueConverter<ListItemInteractionMode, bool>(mode => mode != ListItemInteractionMode.None);
    public static IMultiValueConverter Padding { get; } = new ListItemThemeConverters(true);
    public static IMultiValueConverter Breakpoint { get; } = new ListItemThemeConverters(false);
    public static IValueConverter ElevationZIndex { get; } = new FuncValueConverter<float, int>(height => height > 0 ? 1 : 0);
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [double a, double b, double c, double d]
            ? padding ? new Thickness(a, b, c, d) : (a + b) / 2 - c - d
            : AvaloniaProperty.UnsetValue;
}

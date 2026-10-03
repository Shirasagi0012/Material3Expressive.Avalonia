using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

internal sealed class WideNavigationRailPaddingConverter : IMultiValueConverter
{
    private enum PaddingKind { Top, Collapsed, Expanded }
    private readonly PaddingKind _kind;
    private WideNavigationRailPaddingConverter(PaddingKind kind) => _kind = kind;
    public static IMultiValueConverter Top { get; } = new WideNavigationRailPaddingConverter(PaddingKind.Top);
    public static IMultiValueConverter Collapsed { get; } = new WideNavigationRailPaddingConverter(PaddingKind.Collapsed);
    public static IMultiValueConverter Expanded { get; } = new WideNavigationRailPaddingConverter(PaddingKind.Expanded);
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_kind == PaddingKind.Top)
            return values.Count == 1 && values[0] is double top ? new Thickness(0, top, 0, 0) : AvaloniaProperty.UnsetValue;
        if (values.Count != 3 || values[0] is not double horizontal || values[1] is not double height || values[2] is not double icon)
            return AvaloniaProperty.UnsetValue;
        // WideNavigationRailItemDefaults: collapsed width minus token icon size;
        // expanded horizontal spacing comes directly from FullWidthLeadingSpace.
        return new Thickness(_kind == PaddingKind.Collapsed ? (horizontal - icon) / 2 : horizontal, (height - icon) / 2);
    }
}

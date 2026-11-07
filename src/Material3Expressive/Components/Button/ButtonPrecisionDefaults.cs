using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

internal static class ButtonPrecisionDefaults
{
    public static IMultiValueConverter Height { get; } = new HeightConverter();
    public static IMultiValueConverter Padding { get; } = new PaddingConverter();

    private sealed class HeightConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [bool enabled, double normal, double precise] ? enabled ? precise : normal : AvaloniaProperty.UnsetValue;
    }

    private sealed class PaddingConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [bool enabled, Thickness normal, Thickness precise, double iconSide, var icon, var template])
                return AvaloniaProperty.UnsetValue;
            if (!enabled) return normal;
            var hasIcon = icon is not null && icon != AvaloniaProperty.UnsetValue || template is not null && template != AvaloniaProperty.UnsetValue;
            // The shared icon slot is a logical start icon. Avalonia mirrors the content
            // row in RTL; preserve explicit owner Padding and other size-class defaults.
            return hasIcon ? new Thickness(iconSide, precise.Top, precise.Right, precise.Bottom) : precise;
        }
    }
}

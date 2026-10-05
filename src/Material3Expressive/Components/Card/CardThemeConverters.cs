using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// Only default-value composition lives here. Effective configuration is selected by Avalonia;
// these converters neither resolve tokens nor write back into the owner.
internal sealed class CardThemeConverters(int kind) : IMultiValueConverter
{
    public static IMultiValueConverter State { get; } = new CardThemeConverters(0);
    public static IMultiValueConverter DisabledContainer { get; } = new CardThemeConverters(1);
    public static IMultiValueConverter DisabledContent { get; } = new CardThemeConverters(2);
    public static IMultiValueConverter Outline { get; } = new CardThemeConverters(3);
    public static IMultiValueConverter Thickness { get; } = new CardThemeConverters(4);

    private static Color Alpha(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(opacity * 255, MidpointRounding.AwayFromZero), color.R, color.G, color.B);

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (kind == 0 && values is [bool clickable, bool enabled, var normal, var disabled])
        {
            var value = !clickable || enabled ? normal : disabled;
            return value is Color color ? new ImmutableSolidColorBrush(color) : value;
        }
        if (kind == 1 && values is [Color disabledBase, double opacity, Color background])
            return ColorComposite.Over(Alpha(disabledBase, opacity), 1, background);
        if (kind == 2 && values is [var ink, double inkOpacity])
            return ink is ISolidColorBrush solid ? new ImmutableSolidColorBrush(Alpha(solid.Color, inkOpacity)) : ink;
        if (kind == 3 && values is [bool outlineClickable, bool outlineEnabled, Color outline,
            Color disabledOutline, double outlineOpacity, Color backdrop])
            return new ImmutableSolidColorBrush(!outlineClickable || outlineEnabled ? outline
                : ColorComposite.Over(Alpha(disabledOutline, outlineOpacity), 1, backdrop));
        if (kind == 4 && values is [double width]) return new Thickness(width);
        return AvaloniaProperty.UnsetValue;
    }
}

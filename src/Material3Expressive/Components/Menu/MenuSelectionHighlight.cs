using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// Persistent highlight for a MenuItem that is :selected (keyboard navigation, or the parent of an
// open submenu) while its interaction state layer is not showing: the item's content color at the
// focus state-layer opacity, composited over its container. M3 menus define no such state, so this
// keeps it in the state-layer vocabulary instead of borrowing the "selected" (checked) colors.
internal sealed class MenuSelectionHighlight : IMultiValueConverter
{
    public static IMultiValueConverter Converter { get; } = new MenuSelectionHighlight();
    private static readonly MenuColorConverter Colors = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Vertical items: MenuColorConverter's nine container inputs, then content brush and opacity.
        // Menu bar items: content brush and opacity over a transparent container.
        var container = values.Count == 11
            ? Colors.Convert(values.Take(9).ToList(), typeof(object), null, culture)
            : Avalonia.Media.Colors.Transparent;
        if (values.Count is not (11 or 2) || container is not Color background
            || values[^2] is not ISolidColorBrush content || values[^1] is not double opacity)
            return AvaloniaProperty.UnsetValue;
        return new ImmutableSolidColorBrush(ColorComposite.Over(content.Color, opacity * content.Opacity, background));
    }
}

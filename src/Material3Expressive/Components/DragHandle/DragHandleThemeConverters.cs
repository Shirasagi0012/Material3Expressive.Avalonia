using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material3Expressive.Components;

internal sealed class DragHandleThemeConverters : IMultiValueConverter
{
    public static IMultiValueConverter Instance { get; } = new DragHandleThemeConverters();
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) => values switch
    {
        [Size rest, Size pressed, Size dragged] => new DragHandleSizes(rest, pressed, dragged),
        [Color rest, Color pressed, Color dragged] => new DragHandleColors(rest, pressed, dragged),
        [CornerRadius rest, CornerRadius pressed, CornerRadius dragged] => new DragHandleShapes(rest, pressed, dragged),
        _ => AvaloniaProperty.UnsetValue,
    };
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MaterialShapes.Gallery.Converters;

public sealed class BorderOpacityConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [int index, int selectedIndex, int currentIndex, double progress, ..])
            return new SolidColorBrush(Colors.Transparent);

        var alpha =
            (index == selectedIndex ? progress : 0) +
            (index == currentIndex ? 1 - progress : 0);

        alpha = Math.Clamp(alpha, 0, 1);

        return new SolidColorBrush(Colors.Red, alpha);
    }
}

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

// Maps the native ProgressBar range onto LinearProgressIndicator.Progress (0..1).
// An empty range draws full, like Avalonia's ProgressBar.UpdateIndicator (Percentage 100).
// NaN draws empty: RangeBase rejects NaN, and Avalonia's NaN width would collapse to 0 anyway.
internal sealed class ProgressBarProgressConverter : IMultiValueConverter
{
    public static ProgressBarProgressConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [double value, double minimum, double maximum] ? Map(value, minimum, maximum) : AvaloniaProperty.UnsetValue;

    internal static double Map(double value, double minimum, double maximum)
    {
        var range = maximum - minimum;
        if (double.IsNaN(value) || double.IsNaN(range)) return 0;
        if (Math.Abs(range) < double.Epsilon) return 1;
        var progress = (value - minimum) / range;
        return double.IsNaN(progress) ? 0 : Math.Clamp(progress, 0, 1);
    }
}

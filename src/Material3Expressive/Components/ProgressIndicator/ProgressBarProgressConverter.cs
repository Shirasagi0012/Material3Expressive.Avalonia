using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

// Maps the native ProgressBar range onto LinearProgressIndicator.Progress (0..1).
// An empty range has no meaningful fraction, so it draws as empty rather than full.
internal sealed class ProgressBarProgressConverter : IMultiValueConverter
{
    public static ProgressBarProgressConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [double value, double minimum, double maximum] ? Map(value, minimum, maximum) : AvaloniaProperty.UnsetValue;

    internal static double Map(double value, double minimum, double maximum)
    {
        var range = maximum - minimum;
        if (double.IsNaN(value) || double.IsNaN(range) || range == 0) return 0;
        var progress = (value - minimum) / range;
        return double.IsNaN(progress) ? 0 : Math.Clamp(progress, 0, 1);
    }
}

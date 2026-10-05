using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Selecting values inside the theme setter keeps public inputs at theme priority,
// unlike a conditional style setter which would override ordinary application styles.
internal sealed class FabSizeDefaultConverter : IMultiValueConverter
{
    public static FabSizeDefaultConverter Instance { get; } = new();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count == 5 && values[0] is FloatingActionButtonSize size
            ? values[size switch { FloatingActionButtonSize.Small => 2, FloatingActionButtonSize.Medium => 3, FloatingActionButtonSize.Large => 4, _ => 1 }]
            : AvaloniaProperty.UnsetValue;
}

internal sealed class FabProgressFunctionConverter(bool color = false, bool radius = false) : IMultiValueConverter
{
    public static FabProgressFunctionConverter Scalar { get; } = new();
    public static FabProgressFunctionConverter Color { get; } = new(color: true);
    public static FabProgressFunctionConverter CornerRadius { get; } = new(radius: true);
    private static readonly ColorConverter ColorConverter = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (color && values is [Color initialColor, Color finalColor])
            return (Func<float, Color>)(progress => LerpColor(initialColor, finalColor, progress));
        if (!color && values is [double initial, double final])
        {
            // FloatingActionButtonMenu.kt derives the closed corner from half its size.
            var end = radius ? (float)final / 2 : (float)final;
            return (Func<float, double>)(progress => MathUtils.Lerp((float)initial, end, progress));
        }
        return AvaloniaProperty.UnsetValue;
    }

    private static Color LerpColor(Color initial, Color final, float progress)
    {
        Span<double> a = stackalloc double[4]; Span<double> b = stackalloc double[4];
        ColorConverter.To(initial, a); ColorConverter.To(final, b);
        for (var i = 0; i < 4; i++) a[i] = MathUtils.Lerp((float)a[i], (float)b[i], Math.Clamp(progress, 0, 1));
        return ColorConverter.From(a);
    }
}

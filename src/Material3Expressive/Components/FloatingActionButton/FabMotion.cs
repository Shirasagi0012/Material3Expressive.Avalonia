using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using ComposeAnimation.Runtime;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// Conversion only: configuration is supplied by the theme, never resolved at runtime.
internal static class FabMotion
{
    internal static SpringSpec<float> Spring(MotionSpring spec, float threshold = 0.01f) =>
        // Compose stores both parameters as Float before simulation.
        new((float)spec.DampingRatio, (float)spec.Stiffness, threshold);

    public static IMultiValueConverter FromToken { get; } = new SpringConverter();
    private sealed class SpringConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [MotionSpring spec] ? Spring(spec) : AvaloniaProperty.UnsetValue;
    }
}

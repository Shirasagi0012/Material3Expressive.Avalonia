using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using ComposeAnimation.Runtime;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// Conversion only: configuration is supplied by the theme, never resolved at runtime.
internal static class FabMotion
{
    internal static bool IsReady(MotionSpring? spec) => spec is { } value &&
        float.IsFinite((float)value.DampingRatio) && value.DampingRatio >= 0 &&
        float.IsFinite((float)value.Stiffness) && (float)value.Stiffness > 0;

    internal static bool TrySpring(MotionSpring? spec, out SpringSpec<float> animation, float threshold = 0.01f)
    {
        if (!IsReady(spec))
        {
            animation = null!;
            return false;
        }
        // Compose stores both parameters as Float before simulation.
        var value = spec!.Value;
        animation = new((float)value.DampingRatio, (float)value.Stiffness, threshold);
        return true;
    }

    public static IMultiValueConverter FromToken { get; } = new SpringConverter();
    private sealed class SpringConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [MotionSpring spec] && TrySpring(spec, out var animation)
                ? animation : AvaloniaProperty.UnsetValue;
    }
}

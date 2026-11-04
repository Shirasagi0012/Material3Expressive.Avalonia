using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

internal sealed class SearchBarMotion(bool entering) : IMultiValueConverter
{
    public static IMultiValueConverter Enter { get; } = new SearchBarMotion(true);
    public static IMultiValueConverter Exit { get; } = new SearchBarMotion(false);
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2 || values[0] is not TimeSpan duration || values[1] is not TimeSpan delay ||
            duration < TimeSpan.Zero || delay < TimeSpan.Zero) return BindingOperations.DoNothing;
        var easing = entering ? new CubicBezierEasing(.05, .7, .1, 1) : new CubicBezierEasing(0, 1, 0, 1);
        // expandVertically / shrinkVertically use the default Bottom alignment upstream.
        // Reuse the same transition engine so reversal retains its active effects and velocity.
        return entering
            ? new FadeIn { Spec = new TweenSpec<double>(duration, delay, easing) }
                + new ExpandVertically { Spec = new TweenSpec<Size>(duration, delay, easing) }
            : (object)(new FadeOut { Spec = new TweenSpec<double>(duration, delay, easing) }
                + new ShrinkVertically { Spec = new TweenSpec<Size>(duration, delay, easing) });
    }
}

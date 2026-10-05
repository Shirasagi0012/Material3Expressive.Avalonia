using Avalonia.Animation.Easings;

namespace ComposeAnimation.Runtime;

public static class ComposeEasings
{
    public static readonly CubicBezierEasing FastOutSlowIn = new(0.4, 0.0, 0.2, 1.0);

    public static readonly CubicBezierEasing LinearOutSlowIn = new(0.0, 0.0, 0.2, 1.0);

    public static readonly CubicBezierEasing FastOutLinearIn = new(0.4, 0.0, 1.0, 1.0);

    public static readonly LinearEasing Linear = new LinearEasing();
}

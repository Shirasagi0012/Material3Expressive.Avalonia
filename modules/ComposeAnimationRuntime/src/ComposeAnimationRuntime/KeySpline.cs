namespace ComposeAnimationRuntime;

public readonly record struct KeySpline(double X1, double Y1, double X2, double Y2)
{
    public Func<double, double> Easing => Easings.CubicBezier(X1, Y1, X2, Y2);
    public double Transform(double progress) => Easing(progress);
}

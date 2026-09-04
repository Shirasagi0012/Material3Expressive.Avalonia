namespace ComposeAnimationRuntime;

public class SpringAnimationSpec<T> : IAnimationSpec<T>
{
    public SpringAnimationSpec(IAnimationVectorConverter<T> converter, double dampingRatio = 1, double stiffness = 1500, double visibilityThreshold = 0.001)
    {
        ArgumentNullException.ThrowIfNull(converter);
        if (!double.IsFinite(dampingRatio) || dampingRatio < 0) throw new ArgumentOutOfRangeException(nameof(dampingRatio));
        if (!double.IsFinite(stiffness) || stiffness <= 0) throw new ArgumentOutOfRangeException(nameof(stiffness));
        if (!double.IsFinite(visibilityThreshold) || visibilityThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(visibilityThreshold));
        Converter = converter; DampingRatio = dampingRatio; Stiffness = stiffness; VisibilityThreshold = visibilityThreshold;
    }
    public IAnimationVectorConverter<T> Converter { get; }
    public double DampingRatio { get; }
    public double Stiffness { get; }
    public double VisibilityThreshold { get; }
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        var start = initialValue.Copy(); var velocity = initialVelocity.Copy(); var target = targetValue.Copy();
        var duration = EstimateDuration(start, velocity, target);
        return new SampledAnimation(duration, seconds => Sample(start, velocity, target, seconds), seconds => SampleVelocity(start, velocity, target, seconds));
    }
    private double EstimateDuration(AnimationVector start, AnimationVector velocity, AnimationVector target)
    {
        if (DampingRatio == 0) return double.PositiveInfinity;
        var time = 0.0;
        for (var index = 0; index < 10000; index++)
        {
            var sample = Sample(start, velocity, target, time);
            var speed = SampleVelocity(start, velocity, target, time);
            var settled = true;
            for (var component = 0; component < sample.Dimension; component++)
                settled &= Math.Abs(sample[component] - target[component]) <= VisibilityThreshold && Math.Abs(speed[component]) <= VisibilityThreshold;
            if (settled) return time;
            time += 0.016;
        }
        return time;
    }
    private AnimationVector Sample(AnimationVector start, AnimationVector velocity, AnimationVector target, double t)
    {
        var result = start.Copy();
        for (var index = 0; index < result.Dimension; index++) result[index] = (float)(target[index] + Scalar(start[index] - target[index], velocity[index], t));
        return result;
    }
    private AnimationVector SampleVelocity(AnimationVector start, AnimationVector velocity, AnimationVector target, double t)
    {
        var result = start.Copy(); result.Reset();
        for (var index = 0; index < result.Dimension; index++) result[index] = (float)ScalarVelocity(start[index] - target[index], velocity[index], t);
        return result;
    }
    private double Scalar(double displacement, double initialVelocity, double time)
    {
        var w = Math.Sqrt(Stiffness); var z = DampingRatio;
        if (z < 1) { var wd = w * Math.Sqrt(1 - z * z); var e = Math.Exp(-z * w * time); var c = (initialVelocity + z * w * displacement) / wd; return e * (displacement * Math.Cos(wd * time) + c * Math.Sin(wd * time)); }
        if (z == 1) { var e = Math.Exp(-w * time); return (displacement + (initialVelocity + w * displacement) * time) * e; }
        var s = w * Math.Sqrt(z * z - 1); var r1 = -w * (z - Math.Sqrt(z * z - 1)); var r2 = -w * (z + Math.Sqrt(z * z - 1)); var a = (initialVelocity - r2 * displacement) / (r1 - r2); var b = displacement - a; return a * Math.Exp(r1 * time) + b * Math.Exp(r2 * time);
    }
    private double ScalarVelocity(double displacement, double initialVelocity, double time)
    {
        var w = Math.Sqrt(Stiffness); var z = DampingRatio;
        if (z < 1) { var wd = w * Math.Sqrt(1 - z * z); var e = Math.Exp(-z * w * time); var c = (initialVelocity + z * w * displacement) / wd; return e * (-z * w * (displacement * Math.Cos(wd * time) + c * Math.Sin(wd * time)) + (-displacement * wd * Math.Sin(wd * time) + (initialVelocity + z * w * displacement) * Math.Cos(wd * time))); }
        if (z == 1) { var e = Math.Exp(-w * time); var q = displacement + (initialVelocity + w * displacement) * time; return ((initialVelocity + w * displacement) - w * q) * e; }
        var s = w * Math.Sqrt(z * z - 1); var r1 = -w * (z - Math.Sqrt(z * z - 1)); var r2 = -w * (z + Math.Sqrt(z * z - 1)); var a = (initialVelocity - r2 * displacement) / (r1 - r2); var b = displacement - a; return a * r1 * Math.Exp(r1 * time) + b * r2 * Math.Exp(r2 * time);
    }
}

public sealed class SpringSpec<T> : SpringAnimationSpec<T>
{
    public SpringSpec(IAnimationVectorConverter<T> converter, double dampingRatio = 1, double stiffness = 1500, double visibilityThreshold = 0.001)
        : base(converter, dampingRatio, stiffness, visibilityThreshold) { }
}

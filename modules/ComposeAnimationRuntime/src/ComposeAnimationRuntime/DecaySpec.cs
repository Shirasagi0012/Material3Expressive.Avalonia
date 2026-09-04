namespace ComposeAnimationRuntime;

public sealed class DecaySpec<T> : IAnimationSpec<T>
{
    public DecaySpec(IAnimationVectorConverter<T> converter, double friction = 4)
    { ArgumentNullException.ThrowIfNull(converter); if (!double.IsFinite(friction) || friction <= 0) throw new ArgumentOutOfRangeException(nameof(friction)); Converter = converter; Friction = friction; }
    public IAnimationVectorConverter<T> Converter { get; }
    public double Friction { get; }
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        var start = initialValue.Copy(); var velocity = initialVelocity.Copy();
        return new SampledAnimation(double.PositiveInfinity, seconds =>
        {
            var result = start.Copy(); var decay = Math.Exp(-Friction * seconds);
            for (var index = 0; index < result.Dimension; index++) result[index] = (float)(start[index] + velocity[index] * (1 - decay) / Friction);
            return result;
        }, seconds =>
        {
            var result = velocity.Copy(); var decay = Math.Exp(-Friction * seconds);
            for (var index = 0; index < result.Dimension; index++) result[index] = (float)(velocity[index] * decay);
            return result;
        });
    }
}

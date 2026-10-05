namespace ComposeAnimation.Runtime;

/// <summary>
/// Stores the specification of a decay animation, including 1) the data type to be animated, and
/// 2) the configuration that will be used once the data (of type <typeparamref name="T"/>) has
/// been converted to an <see cref="AnimationVector"/>.
/// </summary>
public interface IDecayAnimationSpec<T>
{
    IVectorizedDecayAnimationSpec Vectorize(ITwoWayConverter<T> converter);
}

/// <summary>Factory methods for <see cref="IDecayAnimationSpec{T}"/>.</summary>
public static class DecayAnimationSpec
{
    /// <summary>
    /// Android <c>ViewConfiguration.getScrollFriction()</c>. Used by
    /// <see cref="SplineBasedDecay{T}"/>.
    /// </summary>
    public const double ScrollFriction = 0.015;

    /// <summary>
    /// Creates a decay animation spec where the friction/deceleration is always proportional to
    /// the velocity, so the velocity goes under an exponential decay. The higher the
    /// <paramref name="frictionMultiplier"/>, the sooner the animation will stop.
    /// </summary>
    /// <param name="frictionMultiplier">The decay friction multiplier. Must be greater than 0.</param>
    /// <param name="absVelocityThreshold">
    /// The minimum speed, below which the animation is considered finished. Must be greater than 0.
    /// </param>
    public static IDecayAnimationSpec<T> ExponentialDecay<T>(
        double frictionMultiplier = 1,
        double absVelocityThreshold = 0.1)
        => new FloatExponentialDecaySpec(frictionMultiplier, absVelocityThreshold)
            .GenerateDecayAnimationSpec<T>();

    /// <summary>
    /// Native Android fling curve (OverScroller spline). <paramref name="density"/> is Compose
    /// <c>Density.density</c> (dpi / 160). Pass Avalonia <c>TopLevel.RenderScaling</c> for the
    /// same mapping; rebuild the spec when scaling changes.
    /// </summary>
    public static IDecayAnimationSpec<T> SplineBasedDecay<T>(double density)
        => new SplineBasedFloatDecayAnimationSpec(density).GenerateDecayAnimationSpec<T>();

    /// <summary>
    /// Target value of a decay animation from <paramref name="initialValue"/> and
    /// <paramref name="initialVelocity"/>, without running the animation.
    /// </summary>
    public static T CalculateTargetValue<T>(
        this IDecayAnimationSpec<T> spec,
        ITwoWayConverter<T> converter,
        T initialValue,
        T initialVelocity)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(converter);

        var vectorized = spec.Vectorize(converter);
        var initial = new AnimationVector(converter.Dimension);
        var velocity = new AnimationVector(converter.Dimension);
        var target = new AnimationVector(converter.Dimension);
        converter.To(initialValue, initial.AsSpan());
        converter.To(initialVelocity, velocity.AsSpan());
        vectorized.GetTargetValue(initial.AsReadOnlySpan(), velocity.AsReadOnlySpan(), target.AsSpan());
        return converter.From(target.AsReadOnlySpan());
    }

    /// <summary>Float convenience overload matching Compose's <c>DecayAnimationSpec&lt;Float&gt;</c> helper.</summary>
    public static float CalculateTargetValue(
        this IDecayAnimationSpec<float> spec,
        float initialValue,
        float initialVelocity)
        => spec.CalculateTargetValue(new FloatConverter(), initialValue, initialVelocity);

    /// <summary>Double convenience overload of <see cref="CalculateTargetValue{T}"/>.</summary>
    public static double CalculateTargetValue(
        this IDecayAnimationSpec<double> spec,
        double initialValue,
        double initialVelocity)
        => spec.CalculateTargetValue(new DoubleConverter(), initialValue, initialVelocity);

    /// <summary>
    /// Creates a <see cref="IDecayAnimationSpec{T}"/> from a <see cref="IFloatDecayAnimationSpec"/>
    /// by applying it on every dimension of the vector that <typeparamref name="T"/> converts to.
    /// </summary>
    public static IDecayAnimationSpec<T> GenerateDecayAnimationSpec<T>(this IFloatDecayAnimationSpec floatDecaySpec)
    {
        ArgumentNullException.ThrowIfNull(floatDecaySpec);
        return new GeneratedDecayAnimationSpec<T>(floatDecaySpec);
    }
}

internal sealed class GeneratedDecayAnimationSpec<T>(IFloatDecayAnimationSpec floatDecaySpec)
    : IDecayAnimationSpec<T>
{
    public IVectorizedDecayAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        return new VectorizedFloatDecaySpec(floatDecaySpec, converter.Dimension);
    }
}

internal sealed class VectorizedFloatDecaySpec : IVectorizedDecayAnimationSpec
{
    private readonly IFloatDecayAnimationSpec _floatDecaySpec;
    private readonly int _dimension;

    public VectorizedFloatDecaySpec(IFloatDecayAnimationSpec floatDecaySpec, int dimension)
    {
        ArgumentNullException.ThrowIfNull(floatDecaySpec);
        ArgumentOutOfRangeException.ThrowIfLessThan(dimension, 1);

        _floatDecaySpec = floatDecaySpec;
        _dimension = dimension;
    }

    public double AbsVelocityThreshold => _floatDecaySpec.AbsVelocityThreshold;

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initialValue,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination)
    {
        VectorValidation.Validate(initialValue, initialVelocity, destination.Length);
        for (var i = 0; i < _dimension; i++)
            destination[i] = _floatDecaySpec.GetValue(playTime, initialValue[i], initialVelocity[i]);
    }

    public void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initialValue,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination)
    {
        VectorValidation.Validate(initialValue, initialVelocity, destination.Length);
        for (var i = 0; i < _dimension; i++)
            destination[i] = _floatDecaySpec.GetVelocity(playTime, initialValue[i], initialVelocity[i]);
    }

    public void GetTargetValue(
        ReadOnlySpan<double> initialValue,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination)
    {
        VectorValidation.Validate(initialValue, initialVelocity, destination.Length);
        for (var i = 0; i < _dimension; i++)
            destination[i] = _floatDecaySpec.GetTargetValue(initialValue[i], initialVelocity[i]);
    }

    public TimeSpan GetDuration(ReadOnlySpan<double> initialValue, ReadOnlySpan<double> initialVelocity)
    {
        VectorValidation.Validate(initialValue, initialVelocity);
        var maxDuration = TimeSpan.Zero;
        for (var i = 0; i < _dimension; i++)
        {
            var duration = _floatDecaySpec.GetDuration(initialValue[i], initialVelocity[i]);
            if (duration > maxDuration)
                maxDuration = duration;
        }
        return maxDuration;
    }
}

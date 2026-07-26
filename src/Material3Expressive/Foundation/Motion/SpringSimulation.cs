using System;

namespace Material3Expressive.Foundation;

/// <summary>A spring's state at one instant.</summary>
public readonly record struct SpringMotion(double Value, double Velocity);

/// <summary>
/// The closed-form solution of a damped harmonic oscillator.
/// </summary>
/// <remarks>
/// Ported from androidx <c>animation-core/SpringSimulation.kt</c>. The important property is that
/// this is <i>analytic</i>, not integrated: the state at any elapsed time is computed directly from
/// the starting value and velocity, so the result does not depend on how often it is sampled. A
/// dropped frame changes nothing, and the same animation looks identical at 60Hz and 144Hz.
/// <para>
/// The spring is expressed as a natural frequency and a damping ratio rather than as mass, spring
/// constant and damping coefficient — mass is normalised to 1, which is why stiffness alone
/// determines the frequency.
/// </para>
/// </remarks>
public readonly struct SpringSimulation
{
    private readonly double _naturalFrequency;

    public SpringSimulation(double dampingRatio, double stiffness)
    {
        if (stiffness <= 0)
            throw new ArgumentOutOfRangeException(nameof(stiffness), stiffness, "Spring stiffness must be positive.");

        if (dampingRatio < 0)
            throw new ArgumentOutOfRangeException(
                nameof(dampingRatio), dampingRatio, "Damping ratio must be non-negative.");

        DampingRatio = dampingRatio;
        _naturalFrequency = Math.Sqrt(stiffness);
    }

    public SpringSimulation(SpringSpec spec) : this(spec.DampingRatio, spec.Stiffness)
    {
    }

    public double DampingRatio { get; }

    public double Stiffness => _naturalFrequency * _naturalFrequency;

    /// <summary>
    /// The value and velocity <paramref name="elapsedSeconds" /> after starting from
    /// <paramref name="startValue" /> and <paramref name="startVelocity" />, heading for
    /// <paramref name="targetValue" />.
    /// </summary>
    public SpringMotion Update(double startValue, double startVelocity, double targetValue, double elapsedSeconds)
    {
        var displacement0 = startValue - targetValue;
        var dampingRatioSquared = DampingRatio * DampingRatio;
        var r = -DampingRatio * _naturalFrequency;

        double displacement;
        double velocity;

        if (DampingRatio > 1)
        {
            // Over-damped: two real roots, no overshoot.
            var s = _naturalFrequency * Math.Sqrt(dampingRatioSquared - 1);
            var gammaPlus = r + s;
            var gammaMinus = r - s;

            var coefficientB = (gammaMinus * displacement0 - startVelocity) / (gammaMinus - gammaPlus);
            var coefficientA = displacement0 - coefficientB;

            displacement = coefficientA * Math.Exp(gammaMinus * elapsedSeconds)
                           + coefficientB * Math.Exp(gammaPlus * elapsedSeconds);
            velocity = coefficientA * gammaMinus * Math.Exp(gammaMinus * elapsedSeconds)
                       + coefficientB * gammaPlus * Math.Exp(gammaPlus * elapsedSeconds);
        }
        else if (DampingRatio == 1.0)
        {
            // Critically damped: a repeated real root, the fastest non-overshooting return.
            var coefficientA = displacement0;
            var coefficientB = startVelocity + _naturalFrequency * displacement0;
            var decay = Math.Exp(-_naturalFrequency * elapsedSeconds);

            displacement = (coefficientA + coefficientB * elapsedSeconds) * decay;
            velocity = (coefficientA + coefficientB * elapsedSeconds) * decay * -_naturalFrequency
                       + coefficientB * decay;
        }
        else
        {
            // Under-damped: complex roots, so the motion oscillates inside a decaying envelope.
            var dampedFrequency = _naturalFrequency * Math.Sqrt(1 - dampingRatioSquared);
            var cosCoefficient = displacement0;
            var sinCoefficient = (-r * displacement0 + startVelocity) / dampedFrequency;
            var phase = dampedFrequency * elapsedSeconds;
            var envelope = Math.Exp(r * elapsedSeconds);

            displacement = envelope * (cosCoefficient * Math.Cos(phase) + sinCoefficient * Math.Sin(phase));
            velocity = displacement * r
                       + envelope * (-dampedFrequency * cosCoefficient * Math.Sin(phase)
                                     + dampedFrequency * sinCoefficient * Math.Cos(phase));
        }

        return new SpringMotion(displacement + targetValue, velocity);
    }
}

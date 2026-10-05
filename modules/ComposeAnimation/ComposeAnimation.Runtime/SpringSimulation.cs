namespace ComposeAnimation.Runtime;

/// <summary>Motion state: value and velocity at a point in time.</summary>
internal readonly record struct Motion(double Value, double Velocity);

/// <summary>
/// Simulates spring physics, and allows querying the motion (i.e. value and velocity) at a certain
/// time in the future based on the starting velocity and value, using an analytical approach.
/// </summary>
internal sealed class SpringSimulation
{
    private double _naturalFreq = Math.Sqrt(Spring.StiffnessVeryLow);
    private double _dampingRatio = Spring.DampingRatioNoBouncy;

    public double FinalPosition { get; set; }

    /// <summary>Stiffness of the spring. Must be positive.</summary>
    public double Stiffness
    {
        get => _naturalFreq * _naturalFreq;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _naturalFreq = Math.Sqrt(value);
        }
    }

    /// <summary>Damping ratio of the spring. Must be non-negative.</summary>
    public double DampingRatio
    {
        get => _dampingRatio;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _dampingRatio = value;
        }
    }

    /// <summary>Returns the spring position and velocity after <paramref name="deltaT"/>.</summary>
    public Motion UpdateValues(double lastDisplacement, double lastVelocity, double deltaT)
    {
        var adjustedDisplacement = lastDisplacement - FinalPosition;
        var dampingRatioSquared = DampingRatio * DampingRatio;
        var r = -DampingRatio * _naturalFreq;

        double displacement;
        double currentVelocity;

        if (DampingRatio > 1)
        {
            // Over-damped.
            var s = _naturalFreq * Math.Sqrt(dampingRatioSquared - 1);
            var gammaPlus = r + s;
            var gammaMinus = r - s;

            var coeffB = (gammaMinus * adjustedDisplacement - lastVelocity) / (gammaMinus - gammaPlus);
            var coeffA = adjustedDisplacement - coeffB;

            displacement = coeffA * Math.Exp(gammaMinus * deltaT) + coeffB * Math.Exp(gammaPlus * deltaT);
            currentVelocity = coeffA * gammaMinus * Math.Exp(gammaMinus * deltaT) +
                coeffB * gammaPlus * Math.Exp(gammaPlus * deltaT);
        }
        else if (DampingRatio == 1.0)
        {
            // Critically damped.
            var coeffA = adjustedDisplacement;
            var coeffB = lastVelocity + _naturalFreq * adjustedDisplacement;
            var nFdT = -_naturalFreq * deltaT;

            displacement = (coeffA + coeffB * deltaT) * Math.Exp(nFdT);
            currentVelocity = (coeffA + coeffB * deltaT) * Math.Exp(nFdT) * -_naturalFreq +
                coeffB * Math.Exp(nFdT);
        }
        else
        {
            // Under-damped.
            var dampedFreq = _naturalFreq * Math.Sqrt(1 - dampingRatioSquared);
            var cosCoeff = adjustedDisplacement;
            var sinCoeff = 1 / dampedFreq * (-r * adjustedDisplacement + lastVelocity);
            var dFdT = dampedFreq * deltaT;

            displacement = Math.Exp(r * deltaT) * (cosCoeff * Math.Cos(dFdT) + sinCoeff * Math.Sin(dFdT));
            currentVelocity = displacement * r +
                Math.Exp(r * deltaT) * (-dampedFreq * cosCoeff * Math.Sin(dFdT) + dampedFreq * sinCoeff * Math.Cos(dFdT));
        }

        return new Motion(displacement + FinalPosition, currentVelocity);
    }
}

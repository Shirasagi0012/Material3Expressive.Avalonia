namespace ComposeAnimation.Runtime;

/// <summary>Recommended configurations for spring animations.</summary>
public static class Spring
{
    /// <summary>Stiffness constant for an extremely stiff spring.</summary>
    public const double StiffnessHigh = 10_000;

    /// <summary>Stiffness constant for a medium stiff spring. This is the default stiffness.</summary>
    public const double StiffnessMedium = 1_500;

    /// <summary>Stiffness constant for a medium-low stiff spring.</summary>
    public const double StiffnessMediumLow = 400;

    /// <summary>Stiffness constant for a spring with low stiffness.</summary>
    public const double StiffnessLow = 200;

    /// <summary>Stiffness constant for a spring with very low stiffness.</summary>
    public const double StiffnessVeryLow = 50;

    /// <summary>Damping ratio for a very bouncy spring.</summary>
    public const double DampingRatioHighBouncy = 0.2;

    /// <summary>Damping ratio for a medium bouncy spring. This is the default damping ratio.</summary>
    public const double DampingRatioMediumBouncy = 0.5;

    /// <summary>Damping ratio for a spring with low bounciness.</summary>
    public const double DampingRatioLowBouncy = 0.75;

    /// <summary>Damping ratio for a spring with no bounciness (critically damped).</summary>
    public const double DampingRatioNoBouncy = 1;

    /// <summary>Default cutoff for rounding off physics-based animations.</summary>
    public const double DefaultDisplacementThreshold = 0.01;
}

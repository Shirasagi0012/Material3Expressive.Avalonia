using System;

namespace Material3Expressive.Foundation;

/// <summary>
/// A value being animated over time, whatever drives it.
/// </summary>
/// <remarks>
/// Material 3 Expressive is not uniformly spring-driven. Spatial motion — anything that moves or
/// changes shape — uses springs, because physics is what makes it feel expressive. State layer
/// alpha does not: upstream fades it with 15–150ms linear tweens, deliberately short enough to read
/// as an instant response. Both kinds ride the same frame loop, so they share this.
/// </remarks>
public interface IAnimation
{
    /// <summary>The current value.</summary>
    double Value { get; }

    /// <summary>True while the value is still changing.</summary>
    bool IsRunning { get; }

    /// <summary>Advances by <paramref name="delta" />; returns true while still running.</summary>
    bool Tick(TimeSpan delta);

    /// <summary>Jumps to the end of the animation.</summary>
    void Settle();
}

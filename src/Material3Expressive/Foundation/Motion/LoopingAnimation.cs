using System;

namespace Material3Expressive.Foundation;

/// <summary>
/// A phase that runs from 0 to 1 and starts again, forever.
/// </summary>
/// <remarks>
/// What an indeterminate indicator needs, and the one animation here with no destination — so it
/// is the one that keeps a control's frame loop alive indefinitely. That is intended: a spinner
/// that stops spinning is a bug. <see cref="Settle" /> is how it ends, and a control detaching
/// from the tree settles everything it owns.
/// </remarks>
public sealed class LoopingAnimation : IAnimation
{
    private double _elapsedSeconds;

    public LoopingAnimation(TimeSpan period, double initialValue = 0)
    {
        if (period <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period), period, "A loop needs a positive period.");

        Period = period;
        Value = initialValue - Math.Floor(initialValue);
        _elapsedSeconds = Value * Period.TotalSeconds;
        IsRunning = true;
    }

    public TimeSpan Period { get; }

    /// <summary>The current phase, in [0, 1).</summary>
    public double Value { get; private set; }

    public bool IsRunning { get; private set; }

    public bool Tick(TimeSpan delta)
    {
        if (!IsRunning)
            return false;

        _elapsedSeconds = (_elapsedSeconds + delta.TotalSeconds) % Period.TotalSeconds;
        Value = _elapsedSeconds / Period.TotalSeconds;
        return true;
    }

    /// <summary>Stops the loop where it stands.</summary>
    public void Settle()
    {
        IsRunning = false;
    }

    /// <summary>Restarts a settled loop from its current phase.</summary>
    public void Resume()
    {
        IsRunning = true;
    }
}

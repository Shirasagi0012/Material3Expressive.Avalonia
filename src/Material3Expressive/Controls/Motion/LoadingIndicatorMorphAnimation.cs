using System;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls.Motion;

/// <summary>
/// The discrete, spring-driven morph cycle used by Material 3's indeterminate loading indicator.
/// </summary>
/// <remarks>
/// This deliberately is not a uniform loop. AndroidX <c>LoadingIndicator.kt</c> starts a
/// <c>spring(dampingRatio = .6, stiffness = 200, visibilityThreshold = .1)</c> every 650 ms,
/// then advances to the next polygon and adds a quarter turn. The global rotation is a separate
/// linear loop. Keeping the scheduler and the spring together also makes a large deterministic
/// test-clock tick cross interval boundaries exactly as the coroutine implementation does.
/// </remarks>
internal sealed class LoadingIndicatorMorphAnimation : IAnimation
{
    internal static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(650);

    private static readonly SpringSpec MorphSpring = new(0.6, 200, 0.1);

    private readonly SpringAnimation _progress = new(0, MorphSpring);
    private TimeSpan _intervalElapsed;

    public LoadingIndicatorMorphAnimation(int morphCount)
    {
        if (morphCount < 2)
            throw new ArgumentOutOfRangeException(nameof(morphCount), morphCount, "A loading indicator needs at least two morphs.");

        MorphCount = morphCount;
        // AndroidX initializes this target to a quarter turn before beginning the first morph.
        MorphRotationDegrees = 90;
        StartMorph();
    }

    public int MorphCount { get; }

    /// <summary>Current sequence index, including the final shape-to-first-shape morph.</summary>
    public int MorphIndex { get; private set; }

    /// <summary>The stable quarter-turn target for the active morph.</summary>
    public double MorphRotationDegrees { get; private set; }

    /// <summary>Spring-driven morph progress, safely bounded for geometry interpolation.</summary>
    public double Value => Math.Clamp(_progress.Value, 0, 1);

    public bool IsRunning { get; private set; } = true;

    public bool Tick(TimeSpan delta)
    {
        if (!IsRunning)
            return false;
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta), "Animation time cannot move backwards.");

        var remaining = delta;
        while (remaining > TimeSpan.Zero)
        {
            var untilNextMorph = Interval - _intervalElapsed;
            var step = remaining < untilNextMorph ? remaining : untilNextMorph;

            _progress.Tick(step);
            _intervalElapsed += step;
            remaining -= step;

            if (_intervalElapsed < Interval)
                continue;

            MorphIndex = (MorphIndex + 1) % MorphCount;
            MorphRotationDegrees = (MorphRotationDegrees + 90) % 360;
            _intervalElapsed = TimeSpan.Zero;
            _progress.SnapTo(0);
            StartMorph();
        }

        return true;
    }

    public void Settle()
    {
        _progress.Settle();
        IsRunning = false;
    }

    /// <summary>Restarts a detached indicator at the start of a fresh upstream interval.</summary>
    public void Resume()
    {
        if (IsRunning)
            return;

        _intervalElapsed = TimeSpan.Zero;
        _progress.SnapTo(0);
        StartMorph();
        IsRunning = true;
    }

    private void StartMorph()
    {
        _progress.AnimateTo(1, MorphSpring);
    }
}

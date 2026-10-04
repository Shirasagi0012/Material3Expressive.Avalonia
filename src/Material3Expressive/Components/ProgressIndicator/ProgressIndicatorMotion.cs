/*
 * Translated from AndroidX ProgressIndicator.kt / WavyProgressIndicator.kt.
 * Copyright 2022, 2024 The Android Open Source Project; Apache-2.0.
 * Fixed baseline: docs/upstream-baseline.json.
 */
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

internal static class ProgressIndicatorMotion
{
    internal static readonly CubicBezierEasing Accelerate = new(.3, 0, .8, .15);
    internal static readonly CubicBezierEasing Decelerate = new(.05, .7, .1, 1);
    internal static readonly CubicBezierEasing Standard = new(.2, 0, 0, 1);

    internal static InfiniteRepeatableSpec<float> Linear(int delay, int duration)
    {
        var config = new KeyframesSpecConfig<float> { Duration = TimeSpan.FromMilliseconds(1750) };
        config.At(delay, 0).Using(Accelerate);
        config.At(delay + duration, 1);
        return new(new KeyframesSpec<float>(config));
    }

    internal static InfiniteRepeatableSpec<float> GlobalRotation =>
        new(new TweenSpec<float>(TimeSpan.FromMilliseconds(6000), easing: ComposeEasings.Linear));

    internal static InfiniteRepeatableSpec<float> Rotation
    {
        get
        {
            var config = new KeyframesSpecConfig<float> { Duration = TimeSpan.FromMilliseconds(6000) };
            // In upstream keyframes, `using` belongs to the interval starting at this keyframe.
            config.At(300, 90).Using(Decelerate);
            config.At(1500, 90); config.At(1800, 180);
            config.At(3000, 180); config.At(3300, 270);
            config.At(4500, 270); config.At(4800, 360); config.At(6000, 360);
            return new(new KeyframesSpec<float>(config));
        }
    }

    internal static InfiniteRepeatableSpec<float> Sweep
    {
        get
        {
            var config = new KeyframesSpecConfig<float> { Duration = TimeSpan.FromMilliseconds(6000) };
            config.At(3000, .87f).Using(Standard); config.At(6000, .1f);
            return new(new KeyframesSpec<float>(config));
        }
    }
}

public static class ProgressIndicatorDefaults
{
    public static SpringSpec<float> ProgressAnimationSpec => new(1, 50, .001f);
}

public static class WavyProgressIndicatorDefaults
{
    public static TweenSpec<float> ProgressAnimationSpec =>
        new(TimeSpan.FromMilliseconds(500), easing: ComposeEasings.Linear);
    public static double IndicatorAmplitude(double progress) => (float)progress <= .1f || (float)progress >= .95f ? 0 : 1;
}

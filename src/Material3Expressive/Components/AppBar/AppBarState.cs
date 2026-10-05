/* Translated from AndroidX AppBar.kt, Copyright The Android Open Source Project.
 * Apache License 2.0. Fixed source: docs/upstream-baseline.json. */
using Avalonia;

namespace Material3Expressive.Components;

internal interface IHidingAppBarState
{
    float HeightOffsetLimit { get; }

    float HeightOffset { get; set; }

    float CollapsedFraction { get; }
}

/// <summary>Top bar offsets and velocities use physical pixels, as in Compose.</summary>
public sealed class TopAppBarState : AvaloniaObject, IHidingAppBarState
{
    public static readonly StyledProperty<float> HeightOffsetLimitProperty =
        AvaloniaProperty.Register<TopAppBarState, float>(nameof(HeightOffsetLimit), -float.MaxValue);
    public static readonly StyledProperty<float> HeightOffsetProperty =
        AvaloniaProperty.Register<TopAppBarState, float>(nameof(HeightOffset), coerce: (owner, value) =>
            CoerceOffset(owner, value));
    public static readonly StyledProperty<float> ContentOffsetProperty =
        AvaloniaProperty.Register<TopAppBarState, float>(nameof(ContentOffset));

    public TopAppBarState(
        float initialHeightOffsetLimit = -float.MaxValue,
        float initialHeightOffset = 0,
        float initialContentOffset = 0)
    {
        HeightOffsetLimit = initialHeightOffsetLimit;
        // The upstream constructor retains the initial offset without coercing it.
        SetValue(HeightOffsetProperty, initialHeightOffset);
        ContentOffset = initialContentOffset;
        _initializing = false;
    }

    private bool _initializing = true;

    private static float CoerceOffset(AvaloniaObject owner, float value)
    {
        var state = (TopAppBarState)owner;
        return state._initializing ? value : Math.Clamp(value, state.HeightOffsetLimit, 0);
    }

    public float HeightOffsetLimit
    {
        get => GetValue(HeightOffsetLimitProperty);
        set => SetValue(HeightOffsetLimitProperty, value);
    }

    public float HeightOffset
    {
        get => GetValue(HeightOffsetProperty);
        set => SetValue(HeightOffsetProperty, value);
    }

    public float ContentOffset
    {
        get => GetValue(ContentOffsetProperty);
        set => SetValue(ContentOffsetProperty, value);
    }

    public float CollapsedFraction => HeightOffsetLimit != 0 ? HeightOffset / HeightOffsetLimit : 0;

    internal Func<bool> IsScrollingContentAtStart { get; set; } = () => true;
    public float OverlappedFraction =>
        !IsScrollingContentAtStart() && ContentOffset == 0
            ? 1
            : HeightOffsetLimit != 0
                ? 1 - Math.Clamp(
                    HeightOffsetLimit + MathF.Abs(ContentOffset),
                    HeightOffsetLimit,
                    0) / HeightOffsetLimit
                : 0;
}

/// <summary>Bottom bar offsets and velocities use physical pixels, as in Compose.</summary>
public sealed class BottomAppBarState : AvaloniaObject, IHidingAppBarState
{
    public static readonly StyledProperty<float> HeightOffsetLimitProperty =
        AvaloniaProperty.Register<BottomAppBarState, float>(nameof(HeightOffsetLimit), -float.MaxValue);
    public static readonly StyledProperty<float> HeightOffsetProperty =
        AvaloniaProperty.Register<BottomAppBarState, float>(nameof(HeightOffset), coerce: (owner, value) =>
            CoerceOffset(owner, value));
    public static readonly StyledProperty<float> ContentOffsetProperty =
        AvaloniaProperty.Register<BottomAppBarState, float>(nameof(ContentOffset));

    public BottomAppBarState(
        float initialHeightOffsetLimit = -float.MaxValue,
        float initialHeightOffset = 0,
        float initialContentOffset = 0)
    {
        HeightOffsetLimit = initialHeightOffsetLimit;
        SetValue(HeightOffsetProperty, initialHeightOffset);
        ContentOffset = initialContentOffset;
        _initializing = false;
    }

    private bool _initializing = true;

    private static float CoerceOffset(AvaloniaObject owner, float value)
    {
        var state = (BottomAppBarState)owner;
        return state._initializing ? value : Math.Clamp(value, state.HeightOffsetLimit, 0);
    }

    public float HeightOffsetLimit
    {
        get => GetValue(HeightOffsetLimitProperty);
        set => SetValue(HeightOffsetLimitProperty, value);
    }

    public float HeightOffset
    {
        get => GetValue(HeightOffsetProperty);
        set => SetValue(HeightOffsetProperty, value);
    }

    public float ContentOffset
    {
        get => GetValue(ContentOffsetProperty);
        set => SetValue(ContentOffsetProperty, value);
    }

    public float CollapsedFraction => HeightOffsetLimit != 0 ? HeightOffset / HeightOffsetLimit : 0;
}

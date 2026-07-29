using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.VisualTesting.Rendering;

namespace Material3Expressive.VisualTesting.Model;

/// <summary>
/// One deterministic scene that mirrors a pinned Compose reference case.
/// </summary>
public sealed record VisualCase(
    string Id,
    PixelSize Viewport,
    VisualTheme Theme,
    Func<VisualCaseContext, Control> CreateContent,
    double RenderScaling = 1.0,
    Action<VisualContext>? Prepare = null,
    string Culture = "en-US",
    FlowDirection FlowDirection = FlowDirection.LeftToRight,
    VisualMotionSpec? Motion = null)
{
}

/// <summary>One source-authored motion scenario/checkpoint implemented by the Avalonia scene.</summary>
public sealed record VisualMotionSpec(
    string ScenarioId,
    string CheckpointId,
    string TriggerAction,
    Action<VisualContext> Trigger,
    VisualMotionClockCoverage ClockCoverage);

/// <summary>The time domains which a motion case either controls or deliberately excludes.</summary>
public sealed record VisualMotionClockCoverage(
    string ComposeFrameClock,
    string ComposeCoroutineClock,
    string AvaloniaMotionGroup,
    string AvaloniaTransition,
    string AvaloniaComposition,
    string InputTimestamp)
{
    public static VisualMotionClockCoverage ComponentOwned(bool usesComposeCoroutineClock) => new(
        ComposeFrameClock: "controlled",
        ComposeCoroutineClock: usesComposeCoroutineClock ? "controlled" : "unused",
        AvaloniaMotionGroup: "controlled",
        AvaloniaTransition: "forbidden",
        AvaloniaComposition: "forbidden",
        InputTimestamp: "insensitive");
}

/// <summary>Compose-produced logical frame history consumed by the Avalonia renderer.</summary>
public sealed record VisualMotionReplay(
    string Origin,
    string TimeUnit,
    float DurationScale,
    IReadOnlyList<long> DeliveryFrameTicks,
    int CaptureFrameIndex,
    string ScheduleSha256);

/// <summary>Data available while a visual scene is being constructed.</summary>
public sealed record VisualCaseContext(VisualTheme Theme, FontFamily Roboto);

/// <summary>The mounted scene available to input/setup callbacks before capture.</summary>
public sealed record VisualContext(Window Window, Control Content, Control Root)
{
    internal Action<TimeSpan>? AdvanceManualMotion { get; init; }
    internal Action? SettleManualMotion { get; init; }
    internal IReadOnlyList<long>? DeliveredMotionFrameTicks { get; set; }
    internal int? MotionCaptureFrameIndex { get; set; }

    public T Require<T>(string name) where T : Control =>
        Content.GetLogicalDescendants()
            .Concat(Content.GetVisualDescendants())
            .Prepend(Content)
            .Distinct()
            .OfType<T>()
            .Single(control => string.Equals(control.Name, name, StringComparison.Ordinal));

    /// <summary>Moves all component-owned animation values to their current targets.</summary>
    public void SettleAnimations()
    {
        if (SettleManualMotion is { } settle)
            settle();
        else
            VisualMotionSettler.Settle(Content);
    }

    /// <summary>
    /// Disconnects component motion from the host render clock so exact keyframes can be captured.
    /// Call this before sending the input that starts an animation.
    /// </summary>
    public void UseManualMotionClock()
    {
        if (AdvanceManualMotion is null)
            VisualMotionSettler.UseManualClock(Content);
    }

    /// <summary>Advances every component-owned animation by an exact test-controlled duration.</summary>
    public void AdvanceAnimations(TimeSpan delta)
    {
        if (AdvanceManualMotion is { } advance)
            advance(delta);
        else
            VisualMotionSettler.AdvanceBy(Content, delta);
    }
}

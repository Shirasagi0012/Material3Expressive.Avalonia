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
    FlowDirection FlowDirection = FlowDirection.LeftToRight)
{
}

/// <summary>Data available while a visual scene is being constructed.</summary>
public sealed record VisualCaseContext(VisualTheme Theme, FontFamily Roboto);

/// <summary>The mounted scene available to input/setup callbacks before capture.</summary>
public sealed record VisualContext(Window Window, Control Content, Control Root)
{
    public T Require<T>(string name) where T : Control =>
        Content.GetLogicalDescendants()
            .Concat(Content.GetVisualDescendants())
            .Prepend(Content)
            .Distinct()
            .OfType<T>()
            .Single(control => string.Equals(control.Name, name, StringComparison.Ordinal));

    /// <summary>Moves all component-owned animation values to their current targets.</summary>
    public void SettleAnimations() => VisualMotionSettler.Settle(Content);

    /// <summary>
    /// Disconnects component motion from the host render clock so exact keyframes can be captured.
    /// Call this before sending the input that starts an animation.
    /// </summary>
    public void UseManualMotionClock() => VisualMotionSettler.UseManualClock(Content);

    /// <summary>Advances every component-owned animation by an exact test-controlled duration.</summary>
    public void AdvanceAnimations(TimeSpan delta) => VisualMotionSettler.AdvanceBy(Content, delta);
}

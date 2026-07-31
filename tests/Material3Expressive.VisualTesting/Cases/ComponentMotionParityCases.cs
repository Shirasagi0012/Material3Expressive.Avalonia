using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Material component checkpoints driven by the Compose-produced replay schedule.</summary>
internal static class ComponentMotionParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        CircularWavy("start"),
        CircularWavy("half-wave"),
        CircularWavy("one-wave"),
        CircularWavyIndeterminate(dark: false),
        CircularWavyIndeterminate(dark: true),
        LoadingIndicator("start"),
        LoadingIndicator("spring-mid"),
        LoadingIndicator("next-morph")
    ];

    private static VisualCase CircularWavy(string checkpointId) => new(
        $"progress/circular-wavy/motion/{checkpointId}/light",
        new PixelSize(112, 112),
        VisualTheme.Light,
        _ => new Grid
        {
            Children =
            {
                new MaterialCircularProgressIndicator
                {
                    Name = "component",
                    Value = .5,
                    IsWavy = true,
                    WaveSpeed = 0,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        },
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: "progress/circular-wavy/determinate-motion/light",
            CheckpointId: checkpointId,
            TriggerAction: "start-wave-travel",
            Trigger: context => context.Require<MaterialCircularProgressIndicator>("component").WaveSpeed = 15,
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(usesComposeCoroutineClock: false)));

    private static VisualCase CircularWavyIndeterminate(bool dark) => new(
        $"progress/circular-wavy/indeterminate/start/{(dark ? "dark" : "light")}",
        new PixelSize(112, 112),
        dark ? VisualTheme.Dark : VisualTheme.Light,
        _ => new Grid
        {
            Children =
            {
                new MaterialCircularProgressIndicator
                {
                    Name = "component",
                    IsWavy = true,
                    WaveSpeed = 0,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        },
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId:
                $"progress/circular-wavy/indeterminate/{(dark ? "dark" : "light")}",
            CheckpointId: "start",
            TriggerAction: "start-indeterminate",
            Trigger: context =>
                context.Require<MaterialCircularProgressIndicator>("component").IsIndeterminate = true,
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(usesComposeCoroutineClock: true)));

    private static VisualCase LoadingIndicator(string checkpointId) => new(
        $"progress/loading-indicator/indeterminate/{checkpointId}/light",
        new PixelSize(112, 112),
        VisualTheme.Light,
        _ => new Grid
        {
            Children =
            {
                // Keep the control's natural measurement: both upstream and Avalonia own the same
                // 48dp/DIP public slot and scale the active polygon within it.
                new MaterialLoadingIndicator
                {
                    Name = "component",
                    Value = 0,
                    IsIndeterminate = false,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        },
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: "progress/loading-indicator/indeterminate/light",
            CheckpointId: checkpointId,
            TriggerAction: "start-indeterminate",
            Trigger: context => context.Require<MaterialLoadingIndicator>("component").IsIndeterminate = true,
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(usesComposeCoroutineClock: true)));
}

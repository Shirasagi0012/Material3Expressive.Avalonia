// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// SnackbarHost.kt and Snackbar.kt. The matching static Compose scene uses alpha22's equivalent
// default current-item, action, and layout contracts.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class SnackbarParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        new(
            "snackbar/host/message/light",
            new PixelSize(360, 96),
            VisualTheme.Light,
            _ => Scene(withAction: false),
            RenderScaling: 1,
            Prepare: context => context.SettleAnimations()),
        new(
            "snackbar/host/action/light",
            new PixelSize(360, 96),
            VisualTheme.Light,
            _ => Scene(withAction: true),
            RenderScaling: 1,
            Prepare: context => context.SettleAnimations()),
        Motion("start"),
        Motion("enter-mid"),
        Motion("shown"),
        Motion("timeout"),
        Motion("exit-mid"),
        Motion("dismissed")
    ];

    private static VisualCase Motion(string checkpointId) => new(
        $"snackbar/host/motion/{checkpointId}/light",
        new PixelSize(360, 96),
        VisualTheme.Light,
        _ => Scene(withAction: false, showImmediately: false),
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: "snackbar/host/short-timeout/light",
            CheckpointId: checkpointId,
            TriggerAction: "show-short-snackbar",
            Trigger: context => _ = context
                .Require<MaterialSnackbarHost>("host")
                .State
                .ShowSnackbarAsync(
                    "Message archived",
                    duration: SnackbarDuration.Short),
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(
                usesComposeCoroutineClock: true)));

    private static Grid Scene(
        bool withAction,
        bool showImmediately = true)
    {
        var state = new SnackbarHostState();
        var host = new MaterialSnackbarHost
        {
            Name = "host",
            Width = 344,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            State = state
        };
        if (showImmediately)
        {
            _ = state.ShowSnackbarAsync(
                "Message archived",
                actionLabel: withAction ? "Undo" : null,
                duration: SnackbarDuration.Indefinite);
        }
        return new Grid
        {
            Name = "component",
            Width = 360,
            Height = 96,
            Children = { host }
        };
    }
}

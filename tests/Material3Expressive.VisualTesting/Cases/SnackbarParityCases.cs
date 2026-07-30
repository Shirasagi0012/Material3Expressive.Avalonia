// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// SnackbarHost.kt and Snackbar.kt. The matching static Compose scene uses alpha22's equivalent
// default current-item, action, and layout contracts.

using Avalonia;
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
            Prepare: context => context.SettleAnimations())
    ];

    private static MaterialSnackbarHost Scene(bool withAction)
    {
        var state = new SnackbarHostState();
        var host = new MaterialSnackbarHost
        {
            Name = "component",
            Width = 360,
            Height = 96,
            State = state
        };
        _ = state.ShowSnackbarAsync(
            "Message archived",
            actionLabel: withAction ? "Undo" : null,
            duration: SnackbarDuration.Indefinite);
        return host;
    }
}

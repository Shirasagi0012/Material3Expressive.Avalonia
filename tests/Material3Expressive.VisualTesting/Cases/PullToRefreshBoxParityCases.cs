// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 /
// Jetpack Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 PullToRefresh.kt.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class PullToRefreshBoxParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Create("start"),
        Create("mid"),
        Create("settled")
    ];

    private static VisualCase Create(string checkpointId) => new(
        $"pull-to-refresh/retract/{checkpointId}/light",
        new PixelSize(360, 180),
        VisualTheme.Light,
        _ => new Border
        {
            Width = 360,
            Height = 180,
            Background = new SolidColorBrush(Color.Parse("#FDF7FF")),
            Child = CreateComponent()
        },
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: "pull-to-refresh/retract/light",
            CheckpointId: checkpointId,
            TriggerAction: "animate-to-hidden",
            Trigger: context =>
            {
                var state = context
                    .Require<MaterialPullToRefreshBox>("component")
                    .State;
                state.SnapTo(1.25);
                state.AnimateToHidden();
            },
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(
                usesComposeCoroutineClock: true)));

    private static MaterialPullToRefreshBox CreateComponent()
    {
        var state = new PullToRefreshState();
        var indicator = new MaterialPullToRefreshIndicator
        {
            Name = "indicator",
            ShowGlyph = false,
            Elevation = 0
        };
        indicator.Update(state.DistanceFraction, refreshing: false);
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PullToRefreshState.DistanceFraction))
                indicator.Update(state.DistanceFraction, refreshing: false);
        };

        return new MaterialPullToRefreshBox
        {
            Name = "component",
            State = state,
            IndicatorContent = indicator,
            Width = 280,
            Height = 120,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = new Border
            {
                Name = "content",
                Background = new SolidColorBrush(Color.Parse("#F2ECF4")),
                Child = new TextBlock
                {
                    Name = "message",
                    Text = "Release to refresh",
                    Classes = { "body-large" },
                    Foreground = new SolidColorBrush(Color.Parse("#1D1B20")),
                    LineHeight = 24,
                    UseLayoutRounding = true,
                    RenderTransform = new TranslateTransform(.5, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, .03125, 0, 0)
                }
            }
        };
    }
}

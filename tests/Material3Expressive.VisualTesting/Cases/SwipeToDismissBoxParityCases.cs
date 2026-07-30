// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 /
// Jetpack Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// SwipeToDismissBox.kt and foundation AnchoredDraggableDefaults.SnapAnimationSpec.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class SwipeToDismissBoxParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Create("start"),
        Create("mid"),
        Create("settled")
    ];

    private static VisualCase Create(string checkpointId) => new(
        $"swipe-to-dismiss/end-to-start/{checkpointId}/light",
        new PixelSize(360, 120),
        VisualTheme.Light,
        _ => new Border
        {
            Width = 360,
            Height = 120,
            Background = new SolidColorBrush(Color.Parse("#FDF7FF")),
            Child = new MaterialSwipeToDismissBox
            {
                Name = "component",
                Width = 280,
                Height = 64,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                BackgroundContent = new Border
                {
                    Name = "background",
                    Background = new SolidColorBrush(Color.Parse("#FFDAD6")),
                    Padding = new Thickness(16, 0),
                    Child = new TextBlock
                    {
                        Name = "delete",
                        Text = "Delete",
                        Classes = { "body-large" },
                        Foreground = new SolidColorBrush(Color.Parse("#93000A")),
                        LineHeight = 24,
                        LetterSpacing = 0.5,
                        UseLayoutRounding = true,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0.03125, 0, 0)
                    }
                },
                Content = new Border
                {
                    Name = "content",
                    Background = new SolidColorBrush(Color.Parse("#F2ECF4")),
                    Padding = new Thickness(16, 0),
                    Child = new TextBlock
                    {
                        Name = "message",
                        Text = "Message",
                        Classes = { "body-large" },
                        Foreground = new SolidColorBrush(Color.Parse("#1D1B20")),
                        LineHeight = 24,
                        UseLayoutRounding = true,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0.03125, 0, 0)
                    }
                }
            }
        },
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: "swipe-to-dismiss/end-to-start/light",
            CheckpointId: checkpointId,
            TriggerAction: "dismiss-end-to-start",
            Trigger: context => context
                .Require<MaterialSwipeToDismissBox>("component")
                .State
                .Dismiss(SwipeToDismissBoxValue.EndToStart),
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(
                usesComposeCoroutineClock: true)));
}

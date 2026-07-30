// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 AppBar.kt.
// The executable alpha22 reference covers the settled standard medium endpoints; alpha25
// flexible variants and desktop ScrollViewer handoff remain deterministic-test evidence.

using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class AppBarParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Create("appbar/medium/expanded/light", 0)
    ];

    private static VisualCase Create(string id, double fraction) => new(
        id,
        new PixelSize(360, 160),
        VisualTheme.Light,
        _ => new Grid
        {
            Width = 360,
            Height = 160,
            Children =
            {
                new MaterialAppBar
                {
                    Name = "root",
                    Width = 360,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Size = AppBarSize.Medium,
                    Title = new TextBlock
                    {
                        Name = "title",
                        Text = "Inbox",
                        Classes = { "headline-small" },
                        LineHeight = 32,
                        LetterSpacing = 0,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                        UseLayoutRounding = true
                    },
                    ScrollBehavior = AppBarScrollBehavior.ExitUntilCollapsed,
                    State = new AppBarScrollState(fraction, fraction)
                }
            }
        },
        RenderScaling: 1,
        Prepare: context => context.SettleAnimations());
}

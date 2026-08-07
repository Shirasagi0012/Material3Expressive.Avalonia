// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack Material3
// 1.5.0-alpha22. Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// FloatingToolbar.kt and FloatingToolbarDefaultsTokens.kt VERSION 12_0_0.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Cases;

internal static class FloatingToolbarParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Create(FloatingToolbarOrientation.Horizontal),
        Create(FloatingToolbarOrientation.Vertical)
    ];

    private static VisualCase Create(FloatingToolbarOrientation orientation)
    {
        var horizontal = orientation == FloatingToolbarOrientation.Horizontal;
        var name = horizontal ? "horizontal" : "vertical";
        return new VisualCase(
            $"floating-toolbar/{name}/standard-expanded/light",
            horizontal ? new PixelSize(320, 112) : new PixelSize(112, 320),
            VisualTheme.Light,
            context => new Border
            {
                Name = "root",
                Width = horizontal ? 320 : 112,
                Height = horizontal ? 112 : 320,
                Background = CaseLayout.Brush(context, SysColorToken.Surface),
                Child = Toolbar(context, orientation),
            },
            RenderScaling: 1,
            Prepare: context => context.SettleAnimations());
    }

    private static MaterialFloatingToolbar Toolbar(
        VisualCaseContext context,
        FloatingToolbarOrientation orientation)
    {
        var toolbar = new MaterialFloatingToolbar
        {
            Name = "toolbar",
            Orientation = orientation,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        toolbar.Items.Add(Action("action-primary", CaseLayout.Brush(context, SysColorToken.Primary)));
        toolbar.Items.Add(Action("action-secondary", CaseLayout.Brush(context, SysColorToken.Secondary)));
        toolbar.Items.Add(Action("action-tertiary", CaseLayout.Brush(context, SysColorToken.Tertiary)));
        return toolbar;
    }

    private static Border Action(string name, Avalonia.Media.IBrush color) => new()
    {
        Name = name,
        Width = 48,
        Height = 48,
        Child = new Border
        {
            Width = 16,
            Height = 16,
            Background = color,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }
    };
}

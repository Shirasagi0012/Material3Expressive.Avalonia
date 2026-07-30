// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 /
// Jetpack Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// ExposedDropdownMenu.kt and Menu.kt.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class ExposedDropdownMenuParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Create("start"),
        Create("mid"),
        Create("settled")
    ];

    private static VisualCase Create(string checkpointId) => new(
        $"exposed-dropdown/filled/open/{checkpointId}/light",
        new PixelSize(360, 320),
        VisualTheme.Light,
        _ => new Grid
        {
            Width = 360,
            Height = 320,
            Children =
            {
                new MaterialExposedDropdownMenu
                {
                    Name = "component",
                    Width = 240,
                    Margin = new Thickness(0, 32, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    ItemsSource = new[] { "Red", "Green", "Blue" },
                    ItemTemplate = new FuncDataTemplate<string>((value, _) => new TextBlock
                    {
                        Name = value?.ToLowerInvariant(),
                        Text = value,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    })
                }
            }
        },
        RenderScaling: 1,
        Prepare: checkpointId == "mid"
            ? context =>
            {
                var menu = context.Require<MaterialExposedDropdownMenu>("component");
                var popup = menu.GetTemplateDescendants().OfType<Popup>().Single();
                var gutter = (Border)popup.Child!;
                ((MaterialSurface)gutter.Child!).ShadowElevation = 0;
            }
            : null,
        Motion: new VisualMotionSpec(
            ScenarioId: "exposed-dropdown/filled/open/light",
            CheckpointId: checkpointId,
            TriggerAction: "expand-menu",
            Trigger: context =>
                context.Require<MaterialExposedDropdownMenu>("component").IsDropDownOpen = true,
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(
                usesComposeCoroutineClock: false)));
}

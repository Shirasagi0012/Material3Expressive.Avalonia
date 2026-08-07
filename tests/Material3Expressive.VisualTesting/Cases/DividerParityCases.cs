// Implementation-source basis: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/{Divider.kt,
// tokens/DividerDefaultsTokens.kt} (VERSION v0_117), DividerTest.kt, and DividerScreenshotTest.kt.
// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack alpha22. The
// Canvas-drawn stroke has no independent Compose layout node, so this parity contract compares
// the component's real layout bounds and same-coordinate raster. Focused deterministic tests
// separately prove the one-physical-pixel pen width at 1x and 2x.

using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Static HorizontalDivider and VerticalDivider scenes mirrored by the Compose producer.</summary>
internal static class DividerParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Horizontal("divider/horizontal/default/light", VisualTheme.Light),
        Horizontal("divider/horizontal/default/dark", VisualTheme.Dark),
        Vertical("divider/vertical/default/light"),
        Horizontal("divider/horizontal/custom-thickness/light", VisualTheme.Light, thickness: 3),
        Horizontal("divider/horizontal/hairline/light", VisualTheme.Light, isHairline: true),
        Vertical("divider/vertical/hairline/light", isHairline: true)
    ];

    private static VisualCase Horizontal(
        string id,
        VisualTheme theme,
        double thickness = 1,
        bool isHairline = false) =>
        new(
            id,
            new PixelSize(320, 48),
            theme,
            _ =>
            {
                var grid = new Grid { RowDefinitions = new RowDefinitions("20,Auto,*") };
                var divider = new MaterialDivider
                {
                    Name = "component",
                    Thickness = thickness,
                    IsHairline = isHairline
                };
                Grid.SetRow(divider, 1);
                grid.Children.Add(divider);
                return grid;
            },
            RenderScaling: 1.0);

    private static VisualCase Vertical(string id, bool isHairline = false) =>
        new(
            id,
            new PixelSize(48, 320),
            VisualTheme.Light,
            _ =>
            {
                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("20,Auto,*") };
                var divider = new MaterialDivider
                {
                    Name = "component",
                    Orientation = DividerOrientation.Vertical,
                    IsHairline = isHairline
                };
                Grid.SetColumn(divider, 1);
                grid.Children.Add(divider);
                return grid;
            },
            RenderScaling: 1.0);
}

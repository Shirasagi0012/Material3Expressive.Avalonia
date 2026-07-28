using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Single-component scenes mirrored by the Android Compose capture project.</summary>
internal static class ComponentParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        FilledCard(),
        PrimaryFab(),
        SelectedCheckbox()
    ];

    private static VisualCase FilledCard() =>
        Scene(
            "card/filled/fixed/enabled/light",
            new PixelSize(240, 128),
            _ => new MaterialCard
            {
                Name = "component",
                Variant = CardVariant.Filled,
                Width = 160,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

    private static VisualCase PrimaryFab() =>
        Scene(
            "fab/primary/small/enabled/light",
            new PixelSize(160, 120),
            _ => new MaterialFab
            {
                Name = "component",
                Variant = FabVariant.Primary,
                Size = FabSize.Small,
                Content = new Border { Width = 24, Height = 24 },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

    private static VisualCase SelectedCheckbox() =>
        Scene(
            "selection/checkbox/selected/light",
            new PixelSize(96, 96),
            _ => new MaterialCheckbox
            {
                Name = "component",
                IsChecked = true,
                Content = null,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

    private static VisualCase Scene(
        string id,
        PixelSize viewport,
        Func<VisualCaseContext, Control> createControl) =>
        new(
            id,
            viewport,
            VisualTheme.Light,
            context => new Grid { Children = { createControl(context) } },
            RenderScaling: 1.0);
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Rendering;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Single-component scenes mirrored by the Android Compose capture project.</summary>
internal static class ComponentParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        FilledCard(),
        ElevatedCard(),
        PrimaryFab(),
        SelectedCheckbox(),
        CheckboxWithLabel(),
        IndeterminateCheckbox(),
        VerticalSlider(),
        RangeSlider(),
        CircularWavyProgress(),
        SelectedTonalToggleButton()
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

    private static VisualCase ElevatedCard() =>
        Scene(
            "card/elevated/fixed/enabled/light",
            new PixelSize(240, 128),
            _ => new MaterialCard
            {
                Name = "component",
                Variant = CardVariant.Elevated,
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

    private static VisualCase IndeterminateCheckbox() =>
        Scene(
            "selection/checkbox/indeterminate/light",
            new PixelSize(96, 96),
            _ => new MaterialCheckbox
            {
                Name = "component",
                IsThreeState = true,
                IsChecked = null,
                Content = null,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

    private static VisualCase CheckboxWithLabel() =>
        Scene(
            "selection/checkbox/label/unchecked/light",
            new PixelSize(160, 96),
            _ => new MaterialCheckbox
            {
                Name = "component",
                Content = new TextBlock
                {
                    Name = "label",
                    Text = "Unchecked"
                },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

    private static VisualCase VerticalSlider() =>
        Scene(
            "selection/slider/vertical/default/light",
            new PixelSize(112, 240),
            _ => new MaterialSlider
            {
                Name = "component",
                Orientation = Orientation.Vertical,
                Width = 48,
                Height = 160,
                Value = 0.65,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

    private static VisualCase RangeSlider() =>
        Scene(
            "selection/range-slider/default/light",
            new PixelSize(288, 96),
            _ => new MaterialRangeSlider
            {
                Name = "component",
                Width = 240,
                Height = 48,
                Minimum = 0,
                Maximum = 1,
                StartValue = .25,
                EndValue = .75,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

    private static VisualCase CircularWavyProgress() =>
        Scene(
            "progress/circular-wavy/determinate/light",
            new PixelSize(112, 112),
            _ => new MaterialCircularProgressIndicator
            {
                    Name = "component",
                    Value = .5,
                    IsWavy = true,
                    WaveSpeed = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            context => VisualMotionSettler.UseManualClock(context.Content));

    private static VisualCase SelectedTonalToggleButton() =>
        Scene(
            "togglebutton/tonal/selected/light",
            new PixelSize(160, 96),
            _ => new MaterialToggleButton
            {
                Name = "component",
                Variant = ButtonVariant.Tonal,
                IsChecked = true,
                Content = new TextBlock
                {
                    Name = "label",
                    Text = "Tonal"
                },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            context => context.SettleAnimations());

    private static VisualCase Scene(
        string id,
        PixelSize viewport,
        Func<VisualCaseContext, Control> createControl,
        Action<VisualContext>? prepare = null) =>
        new(
            id,
            viewport,
            VisualTheme.Light,
            context => new Grid { Children = { createControl(context) } },
            RenderScaling: 1.0,
            Prepare: prepare);
}

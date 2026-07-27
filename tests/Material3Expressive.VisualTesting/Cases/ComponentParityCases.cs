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
        SelectedCheckbox(),
        OutlinedTextField(),
        NavigationBar()
    ];

    private static VisualCase FilledCard() =>
        Scene(
            "card/filled/fixed/enabled/light",
            new PixelSize(240, 128),
            new MaterialCard
            {
                Variant = CardVariant.Filled,
                Width = 160,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            "card");

    private static VisualCase PrimaryFab() =>
        Scene(
            "fab/primary/small/enabled/light",
            new PixelSize(160, 120),
            new MaterialFab
            {
                Variant = FabVariant.Primary,
                Size = FabSize.Small,
                Content = new Border { Width = 24, Height = 24 },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            "fab");

    private static VisualCase SelectedCheckbox() =>
        Scene(
            "selection/checkbox/selected/light",
            new PixelSize(96, 96),
            new MaterialCheckbox
            {
                IsChecked = true,
                Content = null,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            "selection");

    private static VisualCase OutlinedTextField() =>
        Scene(
            "textfield/outlined/populated/light",
            new PixelSize(280, 96),
            new MaterialTextField
            {
                Variant = TextFieldVariant.Outlined,
                Label = "Label",
                Text = "Text",
                Width = 240,
                Height = 56,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            "textfield");

    private static VisualCase NavigationBar()
    {
        var navigation = new MaterialNavigationBar
        {
            Variant = NavigationVariant.Bar,
            SelectedIndex = 0,
            Width = 360,
            Height = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        navigation.Items.Add(Item("One"));
        navigation.Items.Add(Item("Two"));
        navigation.Items.Add(Item("Three"));
        return Scene(
            "navigation/bar/three-items/selected/light",
            new PixelSize(360, 112),
            navigation,
            "navigation");

        static MaterialNavigationItem Item(string label) =>
            new()
            {
                Icon = new Border { Width = 24, Height = 24 },
                Content = label
            };
    }

    private static VisualCase Scene(
        string id,
        PixelSize viewport,
        Control control,
        string component) =>
        new(
            id,
            viewport,
            VisualTheme.Light,
            _ => new Grid { Children = { control } },
            ComparisonProfile.Exact,
            ["full", "parity", component]);
}

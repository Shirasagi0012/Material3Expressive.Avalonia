using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>
/// Static parity scenes for the alpha22-compatible segmented-button row contract.
/// </summary>
internal static class SegmentedButtonParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        SingleChoice(VisualTheme.Light, enabled: true),
        SingleChoice(VisualTheme.Dark, enabled: true),
        SingleChoice(VisualTheme.Light, enabled: false),
        MultipleChoice()
    ];

    private static VisualCase SingleChoice(VisualTheme theme, bool enabled)
    {
        var state = enabled ? "selected-middle" : "selected-middle/disabled";
        var themeName = theme.IsDark ? "dark" : "light";
        return SegmentedButtonScene(
            $"segmentedbutton/single/{state}/{themeName}",
            theme,
            SegmentedButtonSelectionMode.Single,
            enabled,
            selectedIndices: [1]);
    }

    private static VisualCase MultipleChoice() =>
        SegmentedButtonScene(
            "segmentedbutton/multiple/first-last-selected/light",
            VisualTheme.Light,
            SegmentedButtonSelectionMode.Multiple,
            enabled: true,
            selectedIndices: [0, 2]);

    private static VisualCase SegmentedButtonScene(
        string id,
        VisualTheme theme,
        SegmentedButtonSelectionMode selectionMode,
        bool enabled,
        int[] selectedIndices) =>
        new(
            id,
            new PixelSize(360, 96),
            theme,
            _ =>
            {
                var row = new MaterialSegmentedButtonRow
                {
                    Name = "component",
                    Width = 300,
                    Height = 40,
                    SelectionMode = selectionMode,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var labels = new[] { "Day", "Month", "Week" };
                for (var index = 0; index < labels.Length; index++)
                {
                    row.Children.Add(new MaterialSegmentedButton
                    {
                        Name = $"item-{index}",
                        Content = new TextBlock
                        {
                            Name = $"label-{index}",
                            Text = labels[index]
                        },
                        IsChecked = selectedIndices.Contains(index),
                        IsEnabled = enabled
                    });
                }

                return new Grid { Children = { row } };
            },
            RenderScaling: 1.0,
            Prepare: context => context.SettleAnimations());
}

// Implementation-source basis: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// BottomSheetScaffold.kt and SheetDefaults.kt (VERSION 29.0.0). The matching Compose cases use
// alpha22's standard partial/expanded anchors and therefore do not claim alpha25-only behavior.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class BottomSheetParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Create("bottomsheet/scaffold/partial/light", VisualTheme.Light, BottomSheetValue.PartiallyExpanded),
        Create("bottomsheet/scaffold/expanded/light", VisualTheme.Light, BottomSheetValue.Expanded),
        Create("bottomsheet/scaffold/partial/dark", VisualTheme.Dark, BottomSheetValue.PartiallyExpanded),
        Motion("start"),
        Motion("spring-mid"),
        Motion("settled"),
    ];

    private static VisualCase Create(string id, VisualTheme theme, BottomSheetValue value) => new(
        id,
        new PixelSize(360, 320),
        theme,
        _ => Scene(theme, value),
        RenderScaling: 1,
        Prepare: context => context.SettleAnimations());

    private static VisualCase Motion(string checkpointId) => new(
        $"bottomsheet/scaffold/motion/{checkpointId}/light",
        new PixelSize(360, 320),
        VisualTheme.Light,
        _ => Scene(VisualTheme.Light, BottomSheetValue.PartiallyExpanded),
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: "bottomsheet/scaffold/partial-to-expanded/light",
            CheckpointId: checkpointId,
            TriggerAction: "expand-sheet",
            Trigger: context => context
                .Require<MaterialBottomSheetScaffold>("root")
                .EffectiveState
                .Expand(),
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(
                usesComposeCoroutineClock: true)));

    private static MaterialBottomSheetScaffold Scene(
        VisualTheme theme,
        BottomSheetValue value) =>
        new()
        {
            Name = "root",
            Width = 360,
            Height = 320,
            Foreground = new Avalonia.Media.SolidColorBrush(
                theme == VisualTheme.Dark
                    ? Avalonia.Media.Color.FromRgb(230, 224, 233)
                    : Avalonia.Media.Color.FromRgb(29, 27, 32)),
            State = new BottomSheetState(value),
            TopBar = new Border
            {
                MinHeight = 56,
                Padding = new Thickness(16),
                Child = new TextBlock
                {
                    Name = "title",
                    Classes = { "title-medium" },
                    Text = "Inbox",
                    LineHeight = 24,
                    UseLayoutRounding = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0.03125, 0, 0)
                }
            },
            Content = new Border
            {
                Name = "body",
                Background = Avalonia.Media.Brushes.Transparent,
                Child = new TextBlock
                {
                    Name = "body-text",
                    Text = "Body content",
                    Classes = { "body-large" },
                    LineHeight = 24,
                    UseLayoutRounding = true,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(16, 16.03125, 16, 16)
                }
            },
            SheetContent = new Border
            {
                Name = "sheet",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 88,
                Child = new StackPanel
                {
                    Spacing = 0,
                    Margin = new Thickness(16, 22, 16, 22),
                    Children =
                    {
                        new TextBlock
                        {
                            Name = "sheet-title",
                            Text = "Filters",
                            Classes = { "title-medium" },
                            LineHeight = 24,
                            UseLayoutRounding = true,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(0, 0.03125, 0, 0)
                        },
                        new TextBlock
                        {
                            Name = "sheet-support",
                            Text = "Persistent sheet",
                            Classes = { "body-medium" },
                            LineHeight = 20,
                            UseLayoutRounding = true,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(0, 0.1162, 0, 0)
                        }
                    }
                }
            }
        };
}

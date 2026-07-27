using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Representative risks that should not be multiplied across every component.</summary>
internal static class CrossCuttingCases
{
    private static readonly Color AlternateSeed = Color.FromRgb(0x00, 0x67, 0x7A);

    public static IReadOnlyList<VisualCase> All { get; } =
    [
        DynamicSeed(isDark: false),
        DynamicSeed(isDark: true),
        Rtl(VisualTheme.Light),
        Rtl(VisualTheme.Dark),
        Scaling(1.25),
        Scaling(2.0),
        LongText(VisualTheme.Light),
        LongText(VisualTheme.Dark),
        SubtreeTheme(VisualTheme.Light),
        SubtreeTheme(VisualTheme.Dark)
    ];

    private static VisualCase DynamicSeed(bool isDark)
    {
        var theme = new VisualTheme(
            AlternateSeed,
            isDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light);
        return new VisualCase(
            $"cross-cutting/dynamic-seed/components/{ThemeName(theme)}",
            new PixelSize(900, 240),
            theme,
            _ => CaseLayout.Horizontal(
                new MaterialButton
                {
                    Content = "Dynamic",
                    Variant = ButtonVariant.Filled
                },
                new MaterialCard
                {
                    Width = 180,
                    Height = 120,
                    Padding = new Thickness(18),
                    Variant = CardVariant.Filled,
                    Content = "Seeded card"
                },
                new MaterialTextField
                {
                    Width = 240,
                    Label = "Seed",
                    Text = "#00677A",
                    Variant = TextFieldVariant.Outlined
                }),
            ComparisonProfile.Exact,
            ["full", "cross-cutting", "dynamic-color"]);
    }

    private static VisualCase Rtl(VisualTheme theme) =>
        new(
            $"cross-cutting/rtl/input-navigation/{ThemeName(theme)}",
            new PixelSize(920, 300),
            theme,
            _ =>
            {
                var navigation = new MaterialNavigationBar
                {
                    Width = 420,
                    Height = 80,
                    SelectedIndex = 0
                };
                navigation.Items.Add(Destination("Home"));
                navigation.Items.Add(Destination("Saved"));
                navigation.Items.Add(Destination("Inbox"));

                return new StackPanel
                {
                    Spacing = 22,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        CaseLayout.Horizontal(
                            new MaterialButton
                            {
                                Content = "Continue",
                                Variant = ButtonVariant.Filled
                            },
                            new MaterialTextField
                            {
                                Width = 280,
                                Label = "Account",
                                Text = "sample@example.com",
                                Variant = TextFieldVariant.Outlined
                            }),
                        navigation
                    }
                };
            },
            ComparisonProfile.Exact,
            ["full", "cross-cutting", "rtl"],
            FlowDirection: FlowDirection.RightToLeft);

    private static VisualCase Scaling(double scaling)
    {
        var logicalSize = new PixelSize(520, 180);
        return new VisualCase(
            $"cross-cutting/dpi/button-typography/{scaling:0.##}x/light",
            new PixelSize(
                (int)Math.Round(logicalSize.Width * scaling),
                (int)Math.Round(logicalSize.Height * scaling)),
            VisualTheme.Light,
            context => CaseLayout.Horizontal(
                new MaterialButton
                {
                    Content = "Scaled",
                    Variant = ButtonVariant.Filled
                },
                new MaterialIconButton
                {
                    Variant = IconButtonVariant.Tonal,
                    Content = new Border { Width = 24, Height = 24 }
                },
                CaseLayout.Label("Roboto at scaled DPI", context, 18, FontWeight.Medium)),
            ComparisonProfile.Exact,
            ["full", "cross-cutting", "dpi"],
            RenderScaling: scaling);
    }

    private static VisualCase LongText(VisualTheme theme) =>
        new(
            $"cross-cutting/long-text/components/{ThemeName(theme)}",
            new PixelSize(980, 310),
            theme,
            context => CaseLayout.Horizontal(
                new StackPanel
                {
                    Width = 290,
                    Spacing = 14,
                    Children =
                    {
                        new MaterialButton
                        {
                            Content = "A deliberately long button label",
                            Variant = ButtonVariant.Filled
                        },
                        new MaterialChip
                        {
                            Content = "A much longer filter selection",
                            Variant = ChipVariant.Filter,
                            IsChecked = true
                        }
                    }
                },
                new MaterialCard
                {
                    Width = 300,
                    Height = 180,
                    Padding = new Thickness(20),
                    Variant = CardVariant.Outlined,
                    Content = new TextBlock
                    {
                        Text =
                            "Long supporting content wraps at a fixed width so line breaking " +
                            "becomes part of the reviewed visual contract.",
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = context.Roboto
                    }
                },
                new MaterialSnackbar
                {
                    Width = 300,
                    IsOpen = true,
                    Duration = TimeSpan.Zero,
                    Message = "A longer operation completed successfully",
                    Action = "Review"
                }),
            ComparisonProfile.Exact,
            ["full", "cross-cutting", "long-text"]);

    private static VisualCase SubtreeTheme(VisualTheme theme) =>
        new(
            $"cross-cutting/subtree-theme/buttons/{ThemeName(theme)}",
            new PixelSize(760, 240),
            theme,
            _ =>
            {
                var alternateTheme = new VisualTheme(
                    Color.FromRgb(0x9C, 0x41, 0x46),
                    theme.Variant,
                    theme.SpecVersion,
                    theme.Platform);
                var scoped = new Border
                {
                    Padding = new Thickness(28),
                    Child = new MaterialButton
                    {
                        Content = "Scoped seed",
                        Variant = ButtonVariant.Filled
                    }
                };
                MaterialThemeScope.SetTheme(scoped, alternateTheme.CreateMaterialTheme());

                return CaseLayout.Horizontal(
                    new Border
                    {
                        Padding = new Thickness(28),
                        Child = new MaterialButton
                        {
                            Content = "Outer seed",
                            Variant = ButtonVariant.Filled
                        }
                    },
                    scoped);
            },
            ComparisonProfile.Exact,
            ["full", "cross-cutting", "subtree-theme", "dynamic-color"]);

    private static MaterialNavigationItem Destination(string label) =>
        new()
        {
            Icon = new Border { Width = 24, Height = 24 },
            Content = label
        };

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}

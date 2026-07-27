using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class ButtonCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Variants(VisualTheme.Light),
        Variants(VisualTheme.Dark),
        Sizes(VisualTheme.Light),
        Disabled(VisualTheme.Light),
        Disabled(VisualTheme.Dark)
    ];

    private static VisualCase Variants(VisualTheme theme)
    {
        return new VisualCase(
            $"button/all-variants/medium/{ThemeName(theme)}",
            new PixelSize(760, 160),
            theme,
            _ => CaseLayout.Horizontal(
                Button("Filled", ButtonVariant.Filled, ButtonSize.Medium),
                Button("Tonal", ButtonVariant.Tonal, ButtonSize.Medium),
                Button("Elevated", ButtonVariant.Elevated, ButtonSize.Medium),
                Button("Outlined", ButtonVariant.Outlined, ButtonSize.Medium),
                Button("Text", ButtonVariant.Text, ButtonSize.Medium)),
            ComparisonProfile.Exact,
            ["smoke", "full", "button"]);
    }

    private static VisualCase Sizes(VisualTheme theme)
    {
        return new VisualCase(
            $"button/filled/all-sizes/{ThemeName(theme)}",
            new PixelSize(900, 190),
            theme,
            _ => CaseLayout.Horizontal(
                Button("XS", ButtonVariant.Filled, ButtonSize.ExtraSmall),
                Button("Small", ButtonVariant.Filled, ButtonSize.Small),
                Button("Medium", ButtonVariant.Filled, ButtonSize.Medium),
                Button("Large", ButtonVariant.Filled, ButtonSize.Large),
                Button("Extra large", ButtonVariant.Filled, ButtonSize.ExtraLarge)),
            ComparisonProfile.Exact,
            ["smoke", "full", "button"]);
    }

    private static VisualCase Disabled(VisualTheme theme)
    {
        return new VisualCase(
            $"button/all-variants/disabled/{ThemeName(theme)}",
            new PixelSize(760, 160),
            theme,
            _ =>
            {
                var panel = CaseLayout.Horizontal(
                    Button("Filled", ButtonVariant.Filled, ButtonSize.Medium),
                    Button("Tonal", ButtonVariant.Tonal, ButtonSize.Medium),
                    Button("Elevated", ButtonVariant.Elevated, ButtonSize.Medium),
                    Button("Outlined", ButtonVariant.Outlined, ButtonSize.Medium),
                    Button("Text", ButtonVariant.Text, ButtonSize.Medium));
                foreach (var child in panel.Children)
                    child.IsEnabled = false;
                return panel;
            },
            ComparisonProfile.Exact,
            ["full", "button"]);
    }

    private static MaterialButton Button(string label, ButtonVariant variant, ButtonSize size)
    {
        return new MaterialButton
        {
            Content = label,
            Variant = variant,
            Size = size,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Cases;

internal static class FirstWaveCases
{
    private const string StarPath =
        "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 " +
        "9.19 8.63 2 9.24l5.46 4.73L5.82 21z";

    private const string EditPath =
        "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25z" +
        "M20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 " +
        "0l-1.83 1.83 3.75 3.75 1.83-1.83z";

    public static IReadOnlyList<VisualCase> All { get; } =
    [
        IconVariants(VisualTheme.Light),
        IconVariants(VisualTheme.Dark),
        IconWidths(VisualTheme.Light),
        ToggleIcons(VisualTheme.Light),
        ToggleButtons(VisualTheme.Light),
        ToggleButtons(VisualTheme.Dark),
        Fabs(VisualTheme.Light),
        Fabs(VisualTheme.Dark),
        Cards(VisualTheme.Light),
        Cards(VisualTheme.Dark)
    ];

    private static VisualCase IconVariants(VisualTheme theme)
    {
        return new VisualCase(
            $"icon-button/all-variants/medium/{ThemeName(theme)}",
            new PixelSize(520, 170),
            theme,
            _ => CaseLayout.Horizontal(
                IconButton(IconButtonVariant.Standard),
                IconButton(IconButtonVariant.Filled),
                IconButton(IconButtonVariant.Tonal),
                IconButton(IconButtonVariant.Outlined)),
            ComparisonProfile.Exact,
            ["smoke", "full", "first-wave", "icon-button"]);
    }

    private static VisualCase IconWidths(VisualTheme theme)
    {
        return new VisualCase(
            $"icon-button/filled/all-widths/{ThemeName(theme)}",
            new PixelSize(460, 170),
            theme,
            _ => CaseLayout.Horizontal(
                IconButton(IconButtonVariant.Filled, IconButtonWidth.Narrow),
                IconButton(IconButtonVariant.Filled, IconButtonWidth.Default),
                IconButton(IconButtonVariant.Filled, IconButtonWidth.Wide)),
            ComparisonProfile.Exact,
            ["full", "first-wave", "icon-button"]);
    }

    private static VisualCase ToggleIcons(VisualTheme theme)
    {
        return new VisualCase(
            $"icon-button/toggle-selected/{ThemeName(theme)}",
            new PixelSize(440, 170),
            theme,
            _ => CaseLayout.Horizontal(
                ToggleIcon(IconButtonVariant.Standard, false),
                ToggleIcon(IconButtonVariant.Standard, true),
                ToggleIcon(IconButtonVariant.Filled, false),
                ToggleIcon(IconButtonVariant.Filled, true)),
            ComparisonProfile.Exact,
            ["full", "first-wave", "icon-button", "toggle"]);
    }

    private static VisualCase ToggleButtons(VisualTheme theme)
    {
        return new VisualCase(
            $"button/toggle-selected/{ThemeName(theme)}",
            new PixelSize(560, 170),
            theme,
            _ => CaseLayout.Horizontal(
                ToggleButton("Unselected", ButtonVariant.Filled, false),
                ToggleButton("Selected", ButtonVariant.Filled, true),
                ToggleButton("Unselected", ButtonVariant.Outlined, false),
                ToggleButton("Selected", ButtonVariant.Outlined, true)),
            ComparisonProfile.Exact,
            ["full", "first-wave", "button", "toggle"]);
    }

    private static VisualCase Fabs(VisualTheme theme)
    {
        return new VisualCase(
            $"fab/variants-and-sizes/{ThemeName(theme)}",
            new PixelSize(760, 210),
            theme,
            _ => CaseLayout.Horizontal(
                Fab(FabVariant.Primary, FabSize.Small),
                Fab(FabVariant.Secondary, FabSize.Medium),
                Fab(FabVariant.Tertiary, FabSize.Large),
                Fab(FabVariant.Surface, FabSize.Small, "Compose"),
                Fab(FabVariant.Primary, FabSize.Small, "Lowered", lowered: true)),
            ComparisonProfile.Exact,
            ["smoke", "full", "first-wave", "fab"]);
    }

    private static VisualCase Cards(VisualTheme theme)
    {
        return new VisualCase(
            $"card/all-variants/{ThemeName(theme)}",
            new PixelSize(720, 240),
            theme,
            context => CaseLayout.Horizontal(
                Card(CardVariant.Elevated, context),
                Card(CardVariant.Filled, context),
                Card(CardVariant.Outlined, context)),
            ComparisonProfile.Exact,
            ["smoke", "full", "first-wave", "card"]);
    }

    private static MaterialIconButton IconButton(
        IconButtonVariant variant,
        IconButtonWidth width = IconButtonWidth.Default)
    {
        return new MaterialIconButton
        {
            Variant = variant,
            Size = IconButtonSize.Medium,
            ButtonWidth = width,
            Content = Icon(StarPath)
        };
    }

    private static MaterialToggleIconButton ToggleIcon(
        IconButtonVariant variant,
        bool selected)
    {
        return new MaterialToggleIconButton
        {
            Variant = variant,
            Size = IconButtonSize.Medium,
            IsChecked = selected,
            Content = Icon(StarPath)
        };
    }

    private static MaterialToggleButton ToggleButton(
        string label,
        ButtonVariant variant,
        bool selected)
    {
        return new MaterialToggleButton
        {
            Content = label,
            Variant = variant,
            Size = ButtonSize.Medium,
            IsChecked = selected
        };
    }

    private static MaterialFab Fab(
        FabVariant variant,
        FabSize size,
        string? label = null,
        bool lowered = false)
    {
        return new MaterialFab
        {
            Variant = variant,
            Size = size,
            Label = label,
            IsLowered = lowered,
            Content = Icon(EditPath)
        };
    }

    private static MaterialCard Card(CardVariant variant, VisualCaseContext context)
    {
        return new MaterialCard
        {
            Variant = variant,
            IsClickable = true,
            Width = 190,
            Height = 144,
            Padding = new Thickness(18),
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = variant.ToString(),
                        FontFamily = context.Roboto,
                        FontSize = 18,
                        FontWeight = FontWeight.Medium,
                        Foreground = CaseLayout.Brush(context, SysColorToken.OnSurface)
                    },
                    new TextBlock
                    {
                        Text = "Interactive card",
                        FontFamily = context.Roboto,
                        FontSize = 14,
                        Foreground = CaseLayout.Brush(context, SysColorToken.OnSurfaceVariant)
                    }
                }
            }
        };
    }

    private static PathIcon Icon(string path)
    {
        return new PathIcon
        {
            Data = StreamGeometry.Parse(path)
        };
    }

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}

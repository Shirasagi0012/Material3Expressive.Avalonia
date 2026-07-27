using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialColorUtilities.DynamicColors;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>
/// One-component scenes shared verbatim with the Compose capture app.
/// </summary>
internal static class ParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        PrimaryColorRoles(VisualTheme.Light),
        PrimaryColorRoles(VisualTheme.Dark),
        Button(ButtonVariant.Filled, enabled: true, VisualTheme.Light),
        Button(ButtonVariant.Filled, enabled: true, VisualTheme.Dark),
        Button(ButtonVariant.Filled, enabled: false, VisualTheme.Light),
        Button(ButtonVariant.Filled, enabled: false, VisualTheme.Dark),
        Button(ButtonVariant.Tonal, enabled: true, VisualTheme.Light),
        Button(ButtonVariant.Elevated, enabled: true, VisualTheme.Light),
        Button(ButtonVariant.Outlined, enabled: true, VisualTheme.Light),
        Button(ButtonVariant.Text, enabled: true, VisualTheme.Light)
    ];

    private static VisualCase PrimaryColorRoles(VisualTheme theme)
    {
        return new VisualCase(
            $"foundation/color/primary-roles/{(theme.IsDark ? "dark" : "light")}",
            new PixelSize(240, 96),
            theme,
            _ =>
            {
                var source = new TonalSpotScheme(theme.SeedColor)
                {
                    SpecVersion = ColorSpec.SpecVersion.Spec2021,
                    Platform = DynamicScheme.Platform.Phone
                };
                var scheme = new ResolvedColorScheme(source);
                var roles = new[]
                {
                    SysColorToken.Primary,
                    SysColorToken.OnPrimary,
                    SysColorToken.PrimaryContainer,
                    SysColorToken.OnPrimaryContainer
                };
                var grid = new Grid
                {
                    ColumnDefinitions =
                    [
                        new ColumnDefinition(60, GridUnitType.Pixel),
                        new ColumnDefinition(60, GridUnitType.Pixel),
                        new ColumnDefinition(60, GridUnitType.Pixel),
                        new ColumnDefinition(60, GridUnitType.Pixel)
                    ]
                };
                for (var index = 0; index < roles.Length; index++)
                {
                    var swatch = new Border
                    {
                        Background = new SolidColorBrush(
                            scheme.GetColor(roles[index], theme.IsDark))
                    };
                    Grid.SetColumn(swatch, index);
                    grid.Children.Add(swatch);
                }

                return grid;
            },
            RenderScaling: 1.0);
    }

    private static VisualCase Button(ButtonVariant variant, bool enabled, VisualTheme theme)
    {
        var variantName = variant switch
        {
            ButtonVariant.Filled => "filled",
            ButtonVariant.Tonal => "tonal",
            ButtonVariant.Elevated => "elevated",
            ButtonVariant.Outlined => "outlined",
            ButtonVariant.Text => "text",
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };
        var state = enabled ? "enabled" : "disabled";
        var themeName = theme.IsDark ? "dark" : "light";

        return new VisualCase(
            $"button/{variantName}/medium/{state}/{themeName}",
            new PixelSize(240, 96),
            theme,
            _ => new Grid
            {
                Children =
                {
                    new MaterialButton
                    {
                        Content = "Button",
                        Variant = variant,
                        Size = ButtonSize.Medium,
                        IsEnabled = enabled,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center
                    }
                }
            },
            RenderScaling: 1.0);
    }
}

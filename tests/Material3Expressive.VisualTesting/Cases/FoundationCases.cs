using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Cases;

internal static class FoundationCases
{
    private static readonly (SysColorToken Background, SysColorToken Foreground, string Label)[] Colors =
    [
        (SysColorToken.Primary, SysColorToken.OnPrimary, "Primary"),
        (SysColorToken.PrimaryContainer, SysColorToken.OnPrimaryContainer, "Primary container"),
        (SysColorToken.Secondary, SysColorToken.OnSecondary, "Secondary"),
        (SysColorToken.SecondaryContainer, SysColorToken.OnSecondaryContainer, "Secondary container"),
        (SysColorToken.Tertiary, SysColorToken.OnTertiary, "Tertiary"),
        (SysColorToken.Error, SysColorToken.OnError, "Error"),
        (SysColorToken.SurfaceContainer, SysColorToken.OnSurface, "Surface container"),
        (SysColorToken.InverseSurface, SysColorToken.InverseOnSurface, "Inverse surface")
    ];

    public static IReadOnlyList<VisualCase> All { get; } =
    [
        ColorRoles(VisualTheme.Light),
        ColorRoles(VisualTheme.Dark),
        Typography(VisualTheme.Light),
        Typography(VisualTheme.Dark),
        Shapes(VisualTheme.Light)
    ];

    private static VisualCase ColorRoles(VisualTheme theme)
    {
        return new VisualCase(
            $"foundation/color/core-roles/{ThemeName(theme)}",
            new PixelSize(720, 300),
            theme,
            context =>
            {
                var scheme = new ResolvedColorScheme(new TonalSpotScheme(theme.SeedColor));
                var panel = new WrapPanel
                {
                    Width = 672,
                    ItemWidth = 156,
                    ItemHeight = 112,
                    ItemSpacing = 12,
                    LineSpacing = 12,
                    Margin = new Thickness(24),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                foreach (var color in Colors)
                {
                    panel.Children.Add(new Border
                    {
                        Width = 156,
                        Height = 112,
                        Padding = new Thickness(12),
                        Background = new SolidColorBrush(
                            scheme.GetColor(color.Background, theme.IsDark)),
                        Child = new TextBlock
                        {
                            Text = color.Label,
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = context.Roboto,
                            FontSize = 14,
                            FontWeight = FontWeight.Medium,
                            Foreground = new SolidColorBrush(
                                scheme.GetColor(color.Foreground, theme.IsDark)),
                            VerticalAlignment = VerticalAlignment.Bottom
                        }
                    });
                }

                return panel;
            },
            ComparisonProfile.Exact,
            ["smoke", "full", "foundation"]);
    }

    private static VisualCase Typography(VisualTheme theme)
    {
        return new VisualCase(
            $"foundation/typography/representative/{ThemeName(theme)}",
            new PixelSize(760, 470),
            theme,
            context =>
            {
                var panel = new StackPanel
                {
                    Spacing = 8,
                    Margin = new Thickness(32),
                    VerticalAlignment = VerticalAlignment.Center
                };
                panel.Children.Add(TypeLine("Display small · Roboto Regular", TypographyRole.DisplaySmall, context));
                panel.Children.Add(TypeLine("Headline medium · Roboto Regular", TypographyRole.HeadlineMedium, context));
                panel.Children.Add(TypeLine("Title large · Roboto Regular", TypographyRole.TitleLarge, context));
                panel.Children.Add(TypeLine("Body large · Roboto Regular", TypographyRole.BodyLarge, context));
                panel.Children.Add(TypeLine("Label large · Roboto Medium", TypographyRole.LabelLarge, context));
                panel.Children.Add(TypeLine(
                    "Title medium emphasized · Roboto Bold",
                    TypographyRole.TitleMediumEmphasized,
                    context));
                return panel;
            },
            ComparisonProfile.Exact,
            ["smoke", "full", "foundation", "typography"]);
    }

    private static VisualCase Shapes(VisualTheme theme)
    {
        return new VisualCase(
            $"foundation/shape/core-scale/{ThemeName(theme)}",
            new PixelSize(760, 220),
            theme,
            context =>
            {
                var materialTheme = theme.CreateMaterialTheme();
                var shapeScheme = materialTheme.Resolved.Shapes;
                var roles = new[]
                {
                    ShapeRole.CornerExtraSmall,
                    ShapeRole.CornerSmall,
                    ShapeRole.CornerMedium,
                    ShapeRole.CornerLarge,
                    ShapeRole.CornerExtraLarge,
                    ShapeRole.CornerFull
                };
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 14,
                    Margin = new Thickness(24),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                foreach (var role in roles)
                {
                    var size = new Size(96, 96);
                    panel.Children.Add(new StackPanel
                    {
                        Spacing = 8,
                        Children =
                        {
                            new Border
                            {
                                Width = size.Width,
                                Height = size.Height,
                                CornerRadius = shapeScheme.GetCornerRadius(role, size),
                                Background = CaseLayout.Brush(context, SysColorToken.PrimaryContainer)
                            },
                            CaseLayout.Label(role.ToString().Replace("Corner", ""), context)
                        }
                    });
                }

                return panel;
            },
            ComparisonProfile.Exact,
            ["smoke", "full", "foundation", "shape"]);
    }

    private static TextBlock TypeLine(
        string text,
        TypographyRole role,
        VisualCaseContext context)
    {
        var block = new TextBlock
        {
            Text = text,
            Foreground = CaseLayout.Brush(context, SysColorToken.OnSurface)
        };
        TypographyScheme.SetTextStyle(block, role);
        return block;
    }

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}

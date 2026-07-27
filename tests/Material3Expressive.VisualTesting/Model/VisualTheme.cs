using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.DynamicColors;

namespace Material3Expressive.VisualTesting.Model;

/// <summary>A fully explicit theme used by one visual test case.</summary>
public sealed record VisualTheme(
    Color SeedColor,
    ThemeVariant Variant,
    MaterialColorUtilities.DynamicColors.ColorSpec.SpecVersion SpecVersion =
        MaterialColorUtilities.DynamicColors.ColorSpec.SpecVersion.Spec2021,
    DynamicScheme.Platform Platform = DynamicScheme.Platform.Phone)
{
    public static VisualTheme Light { get; } =
        new(Color.FromRgb(0x67, 0x50, 0xA4), ThemeVariant.Light);

    public static VisualTheme Dark { get; } =
        new(Color.FromRgb(0x67, 0x50, 0xA4), ThemeVariant.Dark);

    public bool IsDark => Variant == ThemeVariant.Dark;

    public string SchemeVariant => "TonalSpot";

    public MaterialTheme CreateMaterialTheme()
    {
        var roboto = FontEnvironment.Roboto;
        return new MaterialTheme
        {
            ColorScheme = new TonalSpotScheme(SeedColor)
            {
                SpecVersion = SpecVersion,
                Platform = Platform
            },
            TypographyScheme = new TypographyScheme
            {
                BrandFontFamily = roboto,
                PlainFontFamily = roboto,
                EmphasizedBrandFontFamily = roboto,
                EmphasizedPlainFontFamily = roboto
            }
        };
    }
}

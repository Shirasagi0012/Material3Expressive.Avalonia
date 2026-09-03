using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Baseline;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;
using MaterialColorUtilities.DynamicColors;
using MaterialColorUtilities.HCT;
using MaterialColorUtilities.Scheme;
using MaterialColorUtilities.Utils;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// Pins the ColorScheme → MCU pipeline: every value the source serves must equal what a directly
/// constructed MCU scheme produces for the same inputs (seed #6750A4, TonalSpot, Spec2026 — which
/// MCU falls back to Spec2025 for this variant).
/// </summary>
public class ColorSchemeTests
{
    private static readonly ArgbColor SeedArgb = new(0xFF6750A4u);

    private static DynamicScheme ReferenceScheme(bool isDark, double contrastLevel = 0.0) =>
        new SchemeTonalSpot(
            Hct.From(SeedArgb),
            isDark,
            contrastLevel,
            ColorSpec.SpecVersion.Spec2026,
            DynamicScheme.DefaultPlatform);

    private static Color ToColor(ArgbColor argb) => Color.FromArgb(argb.Alpha, argb.Red, argb.Green, argb.Blue);

    [AvaloniaFact]
    public void SysColor_Light_MatchesMcuDirectly()
    {
        var scheme = new ColorScheme();

        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.Primary, ThemeVariant.Light, out var value));
        Assert.Equal(ToColor(ReferenceScheme(isDark: false).Primary), Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void SysColor_Dark_MatchesMcuDirectly()
    {
        var scheme = new ColorScheme();

        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.Primary, ThemeVariant.Dark, out var value));
        Assert.Equal(ToColor(ReferenceScheme(isDark: true).Primary), Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void SysColor_DimRole_HasValueUnderDefaultSpec()
    {
        var scheme = new ColorScheme();

        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.PrimaryDim, ThemeVariant.Light, out var value));
        Assert.Equal(ToColor(ReferenceScheme(isDark: false).PrimaryDim), Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void SysColor_CustomVariantInheritingDark_ResolvesDark()
    {
        var scheme = new ColorScheme();
        var highContrastDark = new ThemeVariant("HighContrastDark", ThemeVariant.Dark);

        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.Surface, highContrastDark, out var value));
        Assert.Equal(ToColor(ReferenceScheme(isDark: true).Surface), Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void SysColor_NullTheme_TreatedAsLight()
    {
        var scheme = new ColorScheme();

        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.Surface, null, out var value));
        Assert.Equal(ToColor(ReferenceScheme(isDark: false).Surface), Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void RefPalette_Tone_ComesFromPalette()
    {
        var scheme = new ColorScheme();

        Assert.True(scheme.TryGetToken(MaterialTokens.Ref.Palette.Primary40, ThemeVariant.Light, out var value));
        Assert.Equal(ToColor(ReferenceScheme(isDark: false).PrimaryPalette.Get(40)), Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void UnknownKey_ReturnsFalse()
    {
        var scheme = new ColorScheme();
        var unknown = new MaterialTokenKey<Color>("md.sys.color.not-a-role");

        Assert.False(scheme.TryGetToken(unknown, ThemeVariant.Light, out _));
    }

    [AvaloniaFact]
    public void SeedChange_RaisesChanged_AndValuesFollow()
    {
        var scheme = new ColorScheme();
        var before = scheme.TryGetToken(MaterialTokens.Sys.Color.Primary, ThemeVariant.Light, out var v)
            ? Assert.IsType<Color>(v)
            : throw new InvalidOperationException();

        var raised = 0;
        scheme.Changed += (_, _) => raised++;
        scheme.Seed = Color.FromRgb(0x66, 0xCC, 0xFF);

        Assert.Equal(1, raised);
        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.Primary, ThemeVariant.Light, out var after));
        var expected = new SchemeTonalSpot(
            Hct.From(new ArgbColor(0xFF66CCFFu)),
            false, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform).Primary;
        Assert.Equal(ToColor(expected), Assert.IsType<Color>(after));
        Assert.NotEqual(before, Assert.IsType<Color>(after));
    }

    [AvaloniaFact]
    public void ContrastLevel_FlowsIntoScheme()
    {
        var scheme = new ColorScheme { ContrastLevel = 1.0 };

        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.Primary, ThemeVariant.Light, out var value));
        Assert.Equal(
            ToColor(ReferenceScheme(isDark: false, contrastLevel: 1.0).Primary),
            Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void Variant_Expressive_MatchesMcuDirectly()
    {
        var scheme = new ColorScheme { Variant = Variant.Expressive };
        var reference = new SchemeExpressive(
            Hct.From(SeedArgb), false, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform);

        Assert.True(scheme.TryGetToken(MaterialTokens.Sys.Color.Tertiary, ThemeVariant.Light, out var value));
        Assert.Equal(ToColor(reference.Tertiary), Assert.IsType<Color>(value));
    }
}

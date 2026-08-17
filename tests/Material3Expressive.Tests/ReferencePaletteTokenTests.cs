using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Markups;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;
using SharedTokens = MaterialDesignTokens.Avalonia;
using Xunit;
using McuReferencePaletteTokens = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens;

namespace Material3Expressive.Tests;

public sealed class ReferencePaletteTokenTests
{
    [Fact]
    public void KeysAreStableReferenceLayerDescriptors()
    {
        var primary = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);

        Assert.Same(primary, McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40));
        Assert.Same(
            primary,
            McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40, "IgnoredForStandardPalette"));
        Assert.Equal(SharedTokens.TokenLayer.Reference, primary.Layer);
        Assert.Equal("Reference.Palette.Primary.40", primary.Path);
        Assert.Equal(typeof(Color), primary.ValueType);

        var custom = McuReferencePaletteTokens.Get(RefPaletteToken.Custom, 60, "Brand");
        Assert.Same(custom, McuReferencePaletteTokens.Get(RefPaletteToken.Custom, 60, "brAND"));
        Assert.Equal("Reference.Palette.Custom.u004200520041004E0044.60", custom.Path);
        Assert.True(McuReferencePaletteTokens.TryGetMetadata(
            custom, out var palette, out var tone, out var customKey));
        Assert.Equal(RefPaletteToken.Custom, palette);
        Assert.Equal(60d, tone);
        Assert.Equal("BRAND", customKey);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 101));
    }

    [AvaloniaFact]
    public void MaterialThemeProvidesStandardAndCustomPaletteValues()
    {
        var scheme = new TonalSpotScheme(Colors.DarkOrange);
        scheme.CustomColors.Add(new CustomColor("Brand", Color.FromRgb(0x2A, 0x7F, 0xD1))
        {
            Harmonize = false
        });
        var theme = new MaterialTheme { ColorScheme = scheme };
        var consumer = new Border();
        var root = Show(new Border { Child = consumer });
        root.Resources.MergedDictionaries.Add(theme);

        var primary = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 35);
        var custom = McuReferencePaletteTokens.Get(RefPaletteToken.Custom, 70, "brand");

        Assert.Equal(
            new ResolvedColorScheme(theme.ColorScheme!).GetPaletteColor(RefPaletteToken.Primary, 35),
            SharedTokenTestHelper.Resolve(consumer, primary));
        Assert.Equal(
            new ResolvedColorScheme(theme.ColorScheme!).GetPaletteColor(RefPaletteToken.Custom, 70, "Brand"),
            SharedTokenTestHelper.Resolve(consumer, custom));
    }

    [AvaloniaFact]
    public void SystemAndComponentAliasesRebindReferenceTokensFromRequesterScope()
    {
        var referencePrimary = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var referenceSecondary = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var referenceTertiary = McuReferencePaletteTokens.Get(RefPaletteToken.Tertiary, 70);
        var system = new SharedTokens.MaterialToken<Color>(
            "System.Test.AliasToReference",
            SharedTokens.TokenLayer.System);
        var component = new SharedTokens.MaterialToken<Color>(
            "Component.Test.AliasToSystem",
            SharedTokens.TokenLayer.Component);

        var systemConsumer = new Border();
        var componentConsumer = new Border();
        var siblingConsumer = new Border();
        var systemScope = new Border { Child = systemConsumer };
        var componentScope = new Border { Child = componentConsumer };
        var root = Show(new StackPanel
        {
            Children = { systemScope, componentScope, siblingConsumer }
        });
        var theme = new MaterialTheme();
        root.Resources.MergedDictionaries.Add(theme);
        var defaults = new MaterialTokenOverrides();
        defaults.Alias(system, referencePrimary);
        defaults.Alias(component, system);
        root.Resources.MergedDictionaries.Add(defaults);
        var baseline = new ResolvedColorScheme(new TonalSpotScheme(Color.Parse("#6750A4")));

        var systemOverrides = AddOverrides(systemScope);
        systemOverrides.Alias(system, referenceSecondary);
        systemOverrides.Set(referenceSecondary, Colors.LimeGreen);
        systemScope.Resources.MergedDictionaries.Add(new MaterialTheme());

        var componentOverrides = AddOverrides(componentScope);
        componentOverrides.Alias(component, referenceTertiary);
        componentOverrides.Set(referenceTertiary, Colors.HotPink);
        componentScope.Resources.MergedDictionaries.Add(new MaterialTheme());

        Assert.Equal(Colors.LimeGreen, SharedTokenTestHelper.Resolve(systemConsumer, component));
        Assert.Equal(Colors.HotPink, SharedTokenTestHelper.Resolve(componentConsumer, component));
        Assert.Equal(
            baseline.GetPaletteColor(RefPaletteToken.Primary, 40),
            SharedTokenTestHelper.Resolve(siblingConsumer, component));
    }

    [Fact]
    public void XamlExtensionReturnsCatalogKeyAndValidatesTone()
    {
        var extension = new ReferencePaletteTokenExtension(RefPaletteToken.NeutralVariant, 90);

        Assert.Same(
            McuReferencePaletteTokens.Get(RefPaletteToken.NeutralVariant, 90),
            extension.ProvideValue());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReferencePaletteTokenExtension(RefPaletteToken.Primary, 101));
    }

    private static MaterialTokenOverrides AddOverrides(Control owner)
    {
        var overrides = new MaterialTokenOverrides();
        owner.Resources.MergedDictionaries.Add(overrides);
        return overrides;
    }

    private static T Show<T>(T control)
        where T : Control
    {
        ThemeTestHelper.ShowWindow(control);
        return control;
    }
}

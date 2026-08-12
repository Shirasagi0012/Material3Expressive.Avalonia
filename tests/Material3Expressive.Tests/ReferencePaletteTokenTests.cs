using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Markups;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ReferencePaletteTokenTests
{
    private readonly MaterialTokenResolver _resolver = new();

    [Fact]
    public void KeysAreStableReferenceLayerDescriptors()
    {
        var primary = ReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);

        Assert.Same(primary, ReferencePaletteTokens.Get(RefPaletteToken.Primary, 40));
        Assert.Same(
            primary,
            ReferencePaletteTokens.Get(RefPaletteToken.Primary, 40, "IgnoredForStandardPalette"));
        Assert.Equal(TokenLayer.Reference, primary.Layer);
        Assert.Equal("Reference.Palette.Primary.40", primary.Path);
        Assert.Equal(typeof(Color), primary.ValueType);

        var custom = ReferencePaletteTokens.Get(RefPaletteToken.Custom, 60, "Brand");
        Assert.Same(custom, ReferencePaletteTokens.Get(RefPaletteToken.Custom, 60, "brAND"));
        Assert.Equal("Reference.Palette.Custom.Brand.60", custom.Path);
        Assert.True(ReferencePaletteTokens.TryGetMetadata(
            custom, out var palette, out var tone, out var customKey));
        Assert.Equal(RefPaletteToken.Custom, palette);
        Assert.Equal((byte)60, tone);
        Assert.Equal("Brand", customKey);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReferencePaletteTokens.Get(RefPaletteToken.Primary, 101));
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

        var primary = ReferencePaletteTokens.Get(RefPaletteToken.Primary, 35);
        var custom = ReferencePaletteTokens.Get(RefPaletteToken.Custom, 70, "brand");

        Assert.Equal(
            theme.Resolved.ColorScheme!.GetPaletteColor(RefPaletteToken.Primary, 35),
            _resolver.Resolve(consumer, primary));
        Assert.Equal(
            theme.Resolved.ColorScheme.GetPaletteColor(RefPaletteToken.Custom, 70, "Brand"),
            _resolver.Resolve(consumer, custom));
    }

    [AvaloniaFact]
    public void SystemAndComponentAliasesRebindReferenceTokensFromRequesterScope()
    {
        var referencePrimary = ReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var referenceSecondary = ReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var referenceTertiary = ReferencePaletteTokens.Get(RefPaletteToken.Tertiary, 70);
        var system = new MaterialToken<Color>(
            "System.Test.AliasToReference",
            TokenLayer.System,
            Token.Alias(referencePrimary));
        var component = new MaterialToken<Color>(
            "Component.Test.AliasToSystem",
            TokenLayer.Component,
            Token.Alias(system));

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

        var systemOverrides = AddOverrides(systemScope);
        systemOverrides.Alias(system, referenceSecondary);
        systemOverrides.Set(referenceSecondary, Colors.LimeGreen);

        var componentOverrides = AddOverrides(componentScope);
        componentOverrides.Alias(component, referenceTertiary);
        componentOverrides.Set(referenceTertiary, Colors.HotPink);

        Assert.Equal(Colors.LimeGreen, _resolver.Resolve(systemConsumer, component));
        Assert.Equal(Colors.HotPink, _resolver.Resolve(componentConsumer, component));
        Assert.Equal(
            theme.Resolved.ColorScheme!.GetPaletteColor(RefPaletteToken.Primary, 40),
            _resolver.Resolve(siblingConsumer, component));
    }

    [Fact]
    public void XamlExtensionReturnsCatalogKeyAndValidatesTone()
    {
        var extension = new ReferencePaletteTokenExtension(RefPaletteToken.NeutralVariant, 90);

        Assert.Same(
            ReferencePaletteTokens.Get(RefPaletteToken.NeutralVariant, 90),
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

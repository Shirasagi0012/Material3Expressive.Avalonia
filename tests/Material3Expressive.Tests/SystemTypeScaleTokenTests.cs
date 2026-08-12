using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Markups;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class SystemTypeScaleTokenTests
{
    private readonly MaterialTokenResolver _resolver = new();

    [AvaloniaFact]
    public void EveryRoleHasStableSystemKeysWithGeneratedFallbacks()
    {
        var consumer = Show(new Border());

        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            var keys = SystemTypeScaleTokens.For(role);
            var defaults = TypeScale.For(role);

            Assert.Same(keys, SystemTypeScaleTokens.For(role));
            Assert.Equal(TokenLayer.System, keys.FontSize.Layer);
            Assert.Equal($"System.TypeScale.{role}.FontSize", keys.FontSize.Path);
            Assert.Equal(defaults.FontSize, _resolver.Resolve(consumer, keys.FontSize));
            Assert.Equal(defaults.LineHeight, _resolver.Resolve(consumer, keys.LineHeight));
            Assert.Equal(defaults.LetterSpacing, _resolver.Resolve(consumer, keys.LetterSpacing));
            Assert.Equal(defaults.Font, _resolver.Resolve(consumer, keys.Font));
            Assert.Equal(defaults.Weight, _resolver.Resolve(consumer, keys.Weight));
        }
    }

    [AvaloniaFact]
    public void SparseOverrideChangesOnlyOneFieldAndCanMoveToAnotherRole()
    {
        var consumer = new Border();
        var root = Show(new Border { Child = consumer });
        var overrides = new SystemTypeScaleOverrides(TypographyRole.LabelLarge)
        {
            FontSize = Token.Literal(18d)
        };
        root.Resources.MergedDictionaries.Add(overrides);

        var labelLarge = SystemTypeScaleTokens.For(TypographyRole.LabelLarge);
        var titleLarge = SystemTypeScaleTokens.For(TypographyRole.TitleLarge);
        Assert.Equal(18d, _resolver.Resolve(consumer, labelLarge.FontSize));
        Assert.Equal(TypeScale.LabelLarge.LineHeight, _resolver.Resolve(consumer, labelLarge.LineHeight));

        overrides.Role = TypographyRole.TitleLarge;

        Assert.Equal(TypeScale.LabelLarge.FontSize, _resolver.Resolve(consumer, labelLarge.FontSize));
        Assert.Equal(18d, _resolver.Resolve(consumer, titleLarge.FontSize));
        Assert.Equal(TypeScale.TitleLarge.Weight, _resolver.Resolve(consumer, titleLarge.Weight));
    }

    [AvaloniaFact]
    public void MaterialThemePublishesLegacyTypographySchemeOverridesIntoTokenGraph()
    {
        var theme = new MaterialTheme();
        theme.TypographyScheme.TypeScaleOverrides.Add(new TypeScaleOverrides
        {
            Variant = TypographyRole.BodyMedium,
            FontSize = 19,
            Weight = FontWeightToken.Bold
        });

        var consumer = new Border();
        var root = Show(new Border { Child = consumer });
        root.Resources.MergedDictionaries.Add(theme);

        var keys = SystemTypeScaleTokens.For(TypographyRole.BodyMedium);
        Assert.Equal(19d, _resolver.Resolve(consumer, keys.FontSize));
        Assert.Equal(FontWeightToken.Bold, _resolver.Resolve(consumer, keys.Weight));
        Assert.Equal(TypeScale.BodyMedium.LineHeight, _resolver.Resolve(consumer, keys.LineHeight));
    }

    [Fact]
    public void OverrideLiteralPropertiesExposeClosedConvertersForXaml()
    {
        var properties = TypeDescriptor.GetProperties(typeof(SystemTypeScaleOverrides));
        var size = Assert.IsType<LiteralTokenDefinition<double>>(
            properties[nameof(SystemTypeScaleOverrides.FontSize)]!.Converter
                .ConvertFromInvariantString("17.5"));
        var font = Assert.IsType<LiteralTokenDefinition<FontFamilyToken>>(
            properties[nameof(SystemTypeScaleOverrides.Font)]!.Converter
                .ConvertFromInvariantString(nameof(FontFamilyToken.EmphasizedPlain)));

        Assert.Equal(17.5, size.Value);
        Assert.Equal(FontFamilyToken.EmphasizedPlain, font.Value);

        var key = new TypeScaleTokenExtension(TypographyRole.BodyLarge)
        {
            Field = SystemTypeScaleField.LineHeight
        }.ProvideValue();
        Assert.Same(SystemTypeScaleTokens.For(TypographyRole.BodyLarge).LineHeight, key);
    }

    private static T Show<T>(T control)
        where T : Control
    {
        ThemeTestHelper.ShowWindow(control);
        return control;
    }
}

using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// The type scale moved into a token table, which closed a real gap: the emphasized slots now
/// carry their own face and weight instead of silently rendering at the base weight.
/// </summary>
public class TypeScaleTests
{
    [Theory]
    [InlineData(TypographyRole.DisplayLarge, 57, 64, -0.2)]
    [InlineData(TypographyRole.BodyMedium, 14, 20, 0.2)]
    [InlineData(TypographyRole.LabelLarge, 14, 20, 0.1)]
    [InlineData(TypographyRole.TitleMediumEmphasized, 16, 24, 0.15)]
    public void Metrics_MatchTheUpstreamTypeScale(
        TypographyRole role, double size, double lineHeight, double tracking)
    {
        var scale = TypeScale.For(role);

        Assert.Equal(size, scale.FontSize);
        Assert.Equal(lineHeight, scale.LineHeight);
        Assert.Equal(tracking, scale.LetterSpacing);
    }

    [Theory]
    [InlineData(TypographyRole.DisplayLarge, FontFamilyToken.Brand, FontWeightToken.Regular)]
    [InlineData(TypographyRole.TitleLarge, FontFamilyToken.Brand, FontWeightToken.Regular)]
    [InlineData(TypographyRole.LabelSmall, FontFamilyToken.Plain, FontWeightToken.Medium)]
    [InlineData(TypographyRole.DisplayLargeEmphasized, FontFamilyToken.EmphasizedBrand, FontWeightToken.Medium)]
    [InlineData(TypographyRole.LabelLargeEmphasized, FontFamilyToken.EmphasizedPlain, FontWeightToken.Bold)]
    [InlineData(TypographyRole.BodyMediumEmphasized, FontFamilyToken.EmphasizedPlain, FontWeightToken.Medium)]
    public void EachSlot_NamesItsOwnFaceAndWeight(
        TypographyRole role, FontFamilyToken font, FontWeightToken weight)
    {
        var scale = TypeScale.For(role);

        Assert.Equal(font, scale.Font);
        Assert.Equal(weight, scale.Weight);
    }

    [Fact]
    public void EmphasizedSlots_ResolveHeavierThanTheirBase()
    {
        var typography = new MaterialTheme().Resolved.Typography;

        Assert.Equal(FontWeight.Normal, typography.GetFontWeight(TypographyRole.BodyLarge));
        Assert.Equal(FontWeight.Medium, typography.GetFontWeight(TypographyRole.BodyLargeEmphasized));
        Assert.Equal(FontWeight.Bold, typography.GetFontWeight(TypographyRole.LabelLargeEmphasized));
    }

    [Fact]
    public void TheSchemesFaces_AnswerForEverySlotThatNamesThem()
    {
        var theme = new MaterialTheme();
        theme.TypographyScheme.EmphasizedPlainFontFamily = new FontFamily("Fixture Emphasized");
        theme.TypographyScheme.PlainFontFamily = new FontFamily("Fixture Plain");

        var typography = theme.Resolved.Typography;

        Assert.Equal("Fixture Plain", typography.GetFontFamily(TypographyRole.BodyLarge).Name);
        Assert.Equal("Fixture Emphasized", typography.GetFontFamily(TypographyRole.BodyLargeEmphasized).Name);
    }

    [Fact]
    public void ASlot_CanBeRetunedOnTheTheme()
    {
        var theme = new MaterialTheme();
        theme.Components.Add(new TypeScaleOverrides { Variant = TypographyRole.LabelLarge, FontSize = 15 });

        var typography = theme.Resolved.Typography;

        Assert.Equal(15, typography.GetTypeScale(TypographyRole.LabelLarge).FontSize);

        // Everything it did not set still comes from the table.
        Assert.Equal(TypeScale.LabelLarge.LineHeight, typography.GetTypeScale(TypographyRole.LabelLarge).LineHeight);
        Assert.Equal(TypeScale.LabelMedium.FontSize, typography.GetTypeScale(TypographyRole.LabelMedium).FontSize);
    }
}

using Material3Expressive.VisualTesting.Parity;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class ParityContractTests
{
    [Theory]
    [InlineData("foundation/color/primary-roles/light", "color-swatch")]
    [InlineData("card/filled/fixed/enabled/light", "container")]
    [InlineData("button/filled/medium/enabled/light", "component-with-text")]
    [InlineData("textfield/filled/empty/light", "text-field")]
    public void EveryCaseFamilySelectsAnExplicitComparisonProfile(string caseId, string profileId)
    {
        Assert.Equal(profileId, ParityComparisonProfiles.ForCase(caseId).Id);
    }

    [Fact]
    public void ComparerRejectsAnyUnapprovedTranslation()
    {
        var expected = Layout("button", new ParityRect(10, 10, 30, 20));
        var actual = Layout("button", new ParityRect(11, 10, 31, 20));
        var image = SolidImage(40, 30, 255);

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            image,
            image,
            ParityComparisonProfiles.ComponentWithText);

        Assert.False(comparison.IsMatch);
        var failure = Assert.Single(comparison.Failures);
        Assert.Equal(
            "Node 'button' bounds differs (LTRB px): expected (10.000, 10.000, 30.000, 20.000), " +
            "actual (11.000, 10.000, 31.000, 20.000), tolerance (0.500, 0.500, 0.500, 0.500).",
            failure);
    }

    [Fact]
    public void CharacterBoundingDifferenceUsesOneLtrbMessage()
    {
        var expected = TextLayout(new ParityRect(249.728f, 76, 265.896f, 116));
        var actual = TextLayout(new ParityRect(250.528f, 72, 266.896f, 112));
        var image = SolidImage(300, 140, 255);

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            image,
            image,
            ParityComparisonProfiles.ComponentWithText);

        Assert.False(comparison.IsMatch);
        var failure = Assert.Single(comparison.Failures);
        Assert.Equal(
            "Text 'label' character 4 bounding differs (LTRB px): " +
            "expected (249.728, 76.000, 265.896, 116.000), " +
            "actual (250.528, 72.000, 266.896, 112.000), " +
            "tolerance (0.500, 0.500, 0.500, 0.500).",
            failure);
    }

    [Fact]
    public void ComparerRejectsFocusedStateMismatch()
    {
        var expected = Layout("field", new ParityRect(10, 10, 30, 20), focused: true);
        var actual = Layout("field", new ParityRect(10, 10, 30, 20), focused: false);
        var image = SolidImage(40, 30, 255);

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            image,
            image,
            ParityComparisonProfiles.TextField);

        Assert.False(comparison.IsMatch);
        Assert.Equal(
            "Node 'field' focused state differs: expected true, actual false.",
            Assert.Single(comparison.Failures));
    }

    private static ParityCaptureResult Layout(string id, ParityRect bounds, bool? focused = null) => new(
        ParityCaptureResult.CurrentSchemaVersion,
        "test",
        new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", 2),
        new ParityViewport(40, 30),
        [new ParityNode(id, "container", null, bounds, true, Focused: focused)]);

    private static ParityCaptureResult TextLayout(ParityRect characterBounds)
    {
        var textLayout = new ParityTextLayout(
            "label",
            "UTF-16",
            100,
            100,
            1,
            false,
            [new ParityTextLine(0, 0, 5, 5, 198, 267, 76, 116, 100, false)],
            [
                new ParityCharacterBox(0, "006C", 0, new ParityRect(198, 76, 216, 116)),
                new ParityCharacterBox(1, "0061", 0, new ParityRect(216, 76, 231, 116)),
                new ParityCharacterBox(2, "0062", 0, new ParityRect(231, 76, 241, 116)),
                new ParityCharacterBox(3, "0065", 0, new ParityRect(241, 76, 249.728f, 116)),
                new ParityCharacterBox(4, "006C", 0, characterBounds),
            ]);
        return new ParityCaptureResult(
            ParityCaptureResult.CurrentSchemaVersion,
            "test",
            new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", 2),
            new ParityViewport(300, 140),
            [new ParityNode("label", "text", null, new ParityRect(249, 76, 267, 116), true, textLayout)]);
    }

    private static Material3Expressive.VisualTesting.Model.RenderedImage SolidImage(int width, int height, byte value)
    {
        var pixels = Enumerable.Repeat(value, width * height * 4).ToArray();
        return new Material3Expressive.VisualTesting.Model.RenderedImage(width, height, pixels);
    }
}

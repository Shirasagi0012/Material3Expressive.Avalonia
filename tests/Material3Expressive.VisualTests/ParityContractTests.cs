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
        Assert.Contains(comparison.Failures, item => item.Contains("bounds left", StringComparison.Ordinal));
    }

    private static ParityCaptureResult Layout(string id, ParityRect bounds) => new(
        1,
        "test",
        new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", 2),
        new ParityViewport(40, 30),
        [new ParityNode(id, "container", null, bounds, true)]);

    private static Material3Expressive.VisualTesting.Model.RenderedImage SolidImage(int width, int height, byte value)
    {
        var pixels = Enumerable.Repeat(value, width * height * 4).ToArray();
        return new Material3Expressive.VisualTesting.Model.RenderedImage(width, height, pixels);
    }
}

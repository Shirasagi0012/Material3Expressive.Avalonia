using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [Fact]
    public void StructuredIssuesListAllActualDescriptorConflicts()
    {
        var e = Measurement.Numeric("x", 1);
        var a = e with { Unit = "px", Space = "text" };
        var issue = ParityComparison.Field("x", e, a).Issue!;
        Assert.Equal("descriptorMismatch", issue.Code); Assert.Equal(new[] { "unit", "space" }, issue.Members);
        Assert.Equal("measurementAbsent", ParityComparison.Field("x", e, null).Issue!.Code);
        var c = Capture("avalonia", Point(e));
        var result = Compare(Capture("compose", Point(e)), c with { Samples = [c.Samples[0] with { FrameTimeUs = 12 }] });
        Assert.Equal(new[] { "frameTimeUs" }, result.Samples.Single().Issue!.Members);
        Assert.Equal("sampleMismatch", result.Samples.Single().Points.Single().Fields.Single().Issue!.Code);
    }

    [Fact]
    public void NoToleranceCanHideFractionalGeometryDifference()
    {
        var e = Capture("compose", Point(Measurement.Numeric("x", 0)));
        var a = Capture("avalonia", Point(Measurement.Numeric("x", .0000001)));
        var result = Compare(e, a); Assert.Equal(1, result.Coverage.Different); Assert.Equal(.0000001, result.Samples[0].Points[0].Fields[0].Delta![0]);
    }

    [Fact]
    public void UnavailableBothSidesRemainsNotCompared()
    {
        var field = Measurement.Unavailable("font", "text", "string", "none", "none", "No reader");
        Assert.Equal("notCompared", Compare(Capture("compose", Point(field)), Capture("avalonia", Point(field))).Status);
    }

    [Theory]
    [InlineData("unit")]
    [InlineData("space")]
    [InlineData("definition")]
    [InlineData("kind")]
    public void SemanticMismatchIsNotANumericDifference(string part)
    {
        var e = Measurement.Numeric("width", 10); var a = part switch
        {
            "unit" => e with { Unit = "px" },
            "space" => e with { Space = "text" },
            "definition" => e with { Definition = "glyph-ink-width" },
            _ => e with { Kind = "point", Numbers = [10, 10] }
        };
        Assert.Equal("notCompared", ParityComparison.Field("width", e, a).Status);
    }

    [Fact]
    public void MissingFieldAndMissingPointAreVisibleSeparately()
    {
        var e = Capture("compose", Point(Measurement.Numeric("width", 10)));
        var a = Capture("avalonia", Point()); Assert.Equal(1, Compare(e, a).Coverage.NotCompared);
        Assert.Equal("notCompared", Compare(e, Capture("avalonia")).Samples[0].Points.Single().Status);
    }

    [Fact]
    public void SampleMatchingUsesIdsAndChecksTimeAndStage()
    {
        var e = Capture("compose", Point()); var a = Capture("avalonia", Point());
        Assert.Equal("notCompared", Compare(e, a with { Samples = [a.Samples[0] with { TimeUs = 1 }] }).Samples.Single().Status);
        Assert.Equal("notCompared", Compare(e, a with { Samples = [a.Samples[0] with { FrameTimeUs = 1 }] }).Samples.Single().Status);
        Assert.Equal("notCompared", Compare(e, a with { Samples = [a.Samples[0] with { Stage = "pressed" }] }).Samples.Single().Status);
        Assert.Equal(2, Compare(e, a with { Samples = [a.Samples[0] with { Id = "other" }] }).Coverage.NotCompared);
    }

    [Fact]
    public void BooleanFactsUseNativeBooleanValues()
    {
        var e = Measurement.Flag("enabled", true); var a = Measurement.Flag("enabled", false);
        e.Validate(); a.Validate();
        Assert.Equal("different", ParityComparison.Field("enabled", e, a).Status);
        Assert.Null(ParityComparison.Field("enabled", e, a).Delta);
    }

    [Fact]
    public void MismatchedParentRetainsBothFactsWithoutPairingThem()
    {
        var e = Capture("compose", new("root", "container", null, []), new("target", "container", "root", [Measurement.Numeric("x", 1)]));
        var a = Capture("avalonia", Point(Measurement.Numeric("x", 2)));
        var point = Compare(e, a).Samples.Single().Points.Single(p => p.Id == "target");
        Assert.Equal("notCompared", point.Status); Assert.Equal("root", point.ExpectedParentId); Assert.Null(point.ActualParentId);
        Assert.Equal(1, point.Fields.Single().Expected!.Numbers![0]); Assert.Equal(2, point.Fields.Single().Actual!.Numbers![0]);
        Assert.Null(point.Fields.Single().Delta);
    }

    [Fact]
    public void NumericVectorsKeepSignedDelta()
    {
        var e = Measurement.Vector("box", "rect", [10, 20, 30, 40], "dip", "scene", "geometry");
        var a = e with { Numbers = [9, 20, 31, 39] };
        Assert.Equal(new double[] { -1, 0, 1, -1 }, ParityComparison.Field("box", e, a).Delta);
    }

    [Fact]
    public void ActualResolvedFontCannotBeSubstitutedForMissingFacts()
    {
        var e = Measurement.Unavailable("resolvedFonts", "text", "strings", "none", "none", "No API", "text.resolved-font-families");
        var a = new Measurement("resolvedFonts", "text", "strings", "none", "none", "text.resolved-font-families", "runtime", Strings: ["Roboto"]);
        Assert.Equal("notCompared", ParityComparison.Field("resolvedFonts", e, a).Status);
    }
}

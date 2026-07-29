using Material3Expressive.VisualTesting.Parity;
using Material3Expressive.VisualTesting.Model;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class ParityContractTests
{
    [Fact]
    public void MotionTimelineUsesAStableCanonicalScheduleHash()
    {
        Assert.Equal(
            "5FECEB66FFC86F38D952786C6D696C79C2DBC239DD4E91B46729D73A27FB57E9",
            ParityMotionTimeline.ScheduleSha256([0]));
    }

    [Fact]
    public void MotionTimelineRejectsAFrameMovingBackwards()
    {
        var motion = Motion([0, 166667, 160000], elapsedTicks: 160000) with
        {
            ScheduleSha256 = ParityMotionTimeline.ScheduleSha256([0, 166667, 160000])
        };

        Assert.Throws<InvalidDataException>(() => ParityMotionTimeline.Validate(motion, "motion/test"));
    }

    [Fact]
    public void ComparerRejectsMotionTimeEvenWhenPixelsAndGeometryMatch()
    {
        var image = SolidImage(40, 30, 255);
        var expected = Layout("component", new ParityRect(10, 10, 30, 20)) with
        {
            Motion = Motion([0, 166667], 166667)
        };
        var actual = expected with
        {
            Motion = Motion([0, 166667, 333333], 333333)
        };

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            image,
            image,
            ParityComparisonProfiles.Container);

        Assert.False(comparison.IsMatch);
        Assert.Contains("Motion timeline contract differs.", comparison.Failures);
    }

    [Theory]
    [InlineData("foundation/color/primary-roles/light", "color-swatch")]
    [InlineData("foundation/elevation/rounded-rectangle/level6/light", "elevation")]
    [InlineData("foundation/typography/display-headline/light", "component-with-text")]
    [InlineData("card/filled/fixed/enabled/light", "container")]
    [InlineData("progress/circular-wavy/determinate/light", "complex-shape")]
    [InlineData("progress/circular-wavy/motion/half-wave/light", "complex-shape")]
    [InlineData("progress/loading-indicator/indeterminate/spring-mid/light", "container")]
    [InlineData("foundation/motion/linear-tween/mid/light", "container")]
    [InlineData("button/filled/medium/enabled/light", "component-with-text")]
    [InlineData("listitem/click/enabled/light", "component-with-text")]
    [InlineData("listitem/click/dragged/light", "component-with-text-and-elevation")]
    [InlineData("listitem/click/disabled-dragged/light", "component-with-text-and-elevation")]
    [InlineData("textfield/filled/empty/light", "text-field")]
    public void EveryCaseFamilySelectsAnExplicitComparisonProfile(string caseId, string profileId)
    {
        var selection = ParityComparisonProfiles.ResolveForCase(caseId);
        Assert.Equal(profileId, selection.Profile.Id);
        Assert.StartsWith(selection.RulePrefix, caseId, StringComparison.Ordinal);
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

    [Fact]
    public void ComparerIgnoresSmallRasterDifferencesInsideMatchingTextLayout()
    {
        var bounds = new ParityRect(249.728f, 76, 265.896f, 116);
        var expected = TextLayout(bounds);
        var actual = TextLayout(bounds);
        var expectedImage = WithVerticalLine(SolidImage(300, 140, 255), 201, 80, 110, 0);
        var actualImage = WithPixel(expectedImage, 200, 80, 243);

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            expectedImage,
            actualImage,
            ParityComparisonProfiles.ComponentWithText);

        Assert.True(comparison.IsMatch);
        Assert.Equal(0, comparison.Pixels.DifferentPixels);
    }

    [Fact]
    public void ComparerDoesNotIgnoreSmallRasterDifferencesOutsideMatchingTextLayout()
    {
        var bounds = new ParityRect(249.728f, 76, 265.896f, 116);
        var layout = TextLayout(bounds);
        var expectedImage = SolidImage(300, 140, 255);
        var actualImage = WithPixel(expectedImage, 20, 20, 243);

        var comparison = new ParityComparer().Compare(
            layout,
            layout,
            expectedImage,
            actualImage,
            ParityComparisonProfiles.ComponentWithText);

        Assert.False(comparison.IsMatch);
        Assert.Equal(
            "Raster differs in 1/42000 pixels (max channel delta 12).",
            Assert.Single(comparison.Failures));
        Assert.Equal(ParityComparison.CurrentSchemaVersion, comparison.SchemaVersion);
        Assert.Equal(10, comparison.SchemaVersion);
        Assert.Equal(ParityComparisonProfiles.CurrentSchemaVersion, comparison.ProfileSchemaVersion);
        Assert.Equal(1, comparison.Raster!.RawDifferentPixels);
        Assert.Equal(0, comparison.Raster.AllowedPixels);
        Assert.Equal([ParityRasterRejectionReason.OutsideSemanticMask], comparison.Raster.RejectionReasons);
        Assert.Equal(new ParityPixelBounds(20, 20, 20, 20), comparison.Raster.RejectedBounds);
        var strict = Assert.Single(comparison.Raster.Regions, region => region.Kind == "strict-unowned");
        Assert.Equal(1, strict.ObservedPixels);
        Assert.Equal(12, strict.ObservedEnergy);
        var sample = Assert.Single(strict.RejectedSamples);
        Assert.Equal((20, 20, (byte)12), (sample.X, sample.Y, sample.ChannelDelta));
        Assert.Contains("outsideSemanticMask", comparison.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void ComparerDoesNotApplyTextRasterToleranceWhenTextLayoutDiffers()
    {
        var expected = TextLayout(new ParityRect(249.728f, 76, 265.896f, 116));
        var actual = TextLayout(new ParityRect(250.528f, 76, 266.896f, 116));
        var expectedImage = SolidImage(300, 140, 255);
        var actualImage = WithPixel(expectedImage, 200, 80, 243);

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            expectedImage,
            actualImage,
            ParityComparisonProfiles.ComponentWithText);

        Assert.False(comparison.IsMatch);
        Assert.Contains(comparison.Failures, failure => failure.StartsWith("Text 'label' character 4 bounding differs", StringComparison.Ordinal));
        Assert.Contains("Raster differs in 1/42000 pixels (max channel delta 12).", comparison.Failures);
    }

    [Fact]
    public void ComparerAllowsOnePixelLineBoxRepresentationDifferenceWithoutRelaxingCharacterBounds()
    {
        var expected = WithLineLocalBounds(
            TextLayout(new ParityRect(249.728f, 76, 265.896f, 116)),
            new ParityRect(0, -1, 69, 39));
        var actual = WithLineLocalBounds(
            TextLayout(new ParityRect(249.728f, 76, 265.896f, 116)),
            new ParityRect(0, 0, 69, 40));
        var image = SolidImage(300, 140, 255);

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            image,
            image,
            ParityComparisonProfiles.ComponentWithText);

        Assert.True(comparison.IsMatch);
    }

    [Fact]
    public void ElevationProfileAllowsChannelDeltaOneButRejectsDeltaTwo()
    {
        var shadow = new ParityShadow(
            "m150",
            "rectangle",
            new ParityRect(0, 0, 20, 10),
            new ParityRect(10, 10, 30, 20),
            new ParityCornerRadii(0, 0, 0, 0),
            new ParityAffineTransform(2, 0, 0, 2, 10, 10),
            4,
            "#FF000000",
            "#FF000000",
            0.039f,
            0.19f,
            1,
            false,
            new ParityPoint3(40, 0, 80),
            160,
            [],
            OccluderDrawn: false);
        var layout = Layout("component", new ParityRect(10, 10, 30, 20), shadow: shadow);
        var expected = WithRectangle(SolidImage(40, 30, 255), 10, 10, 30, 20, 200);
        var deltaOne = WithPixel(expected, 20, 15, 199);
        var deltaTwo = WithPixel(expected, 20, 15, 198);
        var profile = ParityComparisonProfiles.ForCase("foundation/elevation/rectangle/level12/light");

        Assert.True(new ParityComparer().Compare(layout, layout, expected, deltaOne, profile).IsMatch);
        var rejected = new ParityComparer().Compare(layout, layout, expected, deltaTwo, profile);
        Assert.False(rejected.IsMatch);
        Assert.Equal(1, rejected.Pixels.DifferentPixels);
        Assert.Equal(2, rejected.Pixels.MaximumChannelDelta);
    }

    [Fact]
    public void ComparerRejectsShadowParameterMismatchEvenWhenRasterMatches()
    {
        var shadow = new ParityShadow(
            "m150",
            "roundedRectangle",
            new ParityRect(0, 0, 20, 10),
            new ParityRect(10, 10, 50, 30),
            new ParityCornerRadii(4, 4, 4, 4),
            new ParityAffineTransform(2, 0, 0, 2, 10, 10),
            12,
            "#FF000000",
            "#FF000000",
            0.039f,
            0.19f,
            1,
            false,
            new ParityPoint3(40, 0, 80),
            160,
            []);
        var expected = Layout("component", new ParityRect(10, 10, 50, 30), shadow: shadow);
        var actual = Layout("component", new ParityRect(10, 10, 50, 30), shadow: shadow with { ElevationPx = 13 });
        var image = SolidImage(60, 40, 255);

        var comparison = new ParityComparer().Compare(
            expected,
            actual,
            image,
            image,
            ParityComparisonProfiles.ForCase("foundation/elevation/rounded-rectangle/level6/light"));

        Assert.False(comparison.IsMatch);
        Assert.Contains(comparison.Failures, failure => failure.StartsWith("Shadow 'component' elevation differs", StringComparison.Ordinal));
    }

    private static ParityCaptureResult Layout(
        string id,
        ParityRect bounds,
        bool? focused = null,
        ParityShadow? shadow = null) => new(
        ParityCaptureResult.CurrentSchemaVersion,
        "test",
        new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", 2),
        new ParityViewport(40, 30),
        [new ParityNode(id, "container", null, bounds, true, Focused: focused, Shadow: shadow)]);

    private static ParityMotionCapture Motion(IReadOnlyList<long> frames, long elapsedTicks) => new(
        ParityMotionCapture.CurrentSchemaVersion,
        "motion/test",
        "checkpoint",
        "first-controlled-motion-frame",
        "100ns-ticks",
        1,
        elapsedTicks,
        [new ParityMotionEvent(0, "mount")],
        frames,
        frames.Count - 1,
        ParityMotionTimeline.ScheduleSha256(frames),
        VisualMotionClockCoverage.ComponentOwned(usesComposeCoroutineClock: false));

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

    private static Material3Expressive.VisualTesting.Model.RenderedImage WithPixel(
        Material3Expressive.VisualTesting.Model.RenderedImage source,
        int x,
        int y,
        byte value)
    {
        var pixels = (byte[])source.Pixels.Clone();
        var offset = (y * source.Width + x) * 4;
        pixels[offset] = value;
        pixels[offset + 1] = value;
        pixels[offset + 2] = value;
        return source with { Pixels = pixels };
    }

    private static Material3Expressive.VisualTesting.Model.RenderedImage WithVerticalLine(
        Material3Expressive.VisualTesting.Model.RenderedImage source,
        int x,
        int top,
        int bottom,
        byte value)
    {
        var result = source;
        for (var y = top; y < bottom; y++)
            result = WithPixel(result, x, y, value);
        return result;
    }

    private static Material3Expressive.VisualTesting.Model.RenderedImage WithRectangle(
        Material3Expressive.VisualTesting.Model.RenderedImage source,
        int left,
        int top,
        int right,
        int bottom,
        byte value)
    {
        var result = source;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
                result = WithPixel(result, x, y, value);
        }
        return result;
    }

    private static ParityCaptureResult WithLineLocalBounds(ParityCaptureResult source, ParityRect bounds)
    {
        var node = Assert.Single(source.Nodes);
        var layout = Assert.IsType<ParityTextLayout>(node.TextLayout);
        var line = Assert.Single(layout.Lines) with { BoundsInTextPx = bounds };
        return source with { Nodes = [node with { TextLayout = layout with { Lines = [line] } }] };
    }
}

using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Parity;
using Xunit;

namespace Material3Expressive.VisualTests;

/// <summary>Mutation tests for the expected-only semantic raster budget engine.</summary>
public sealed class SemanticRasterBudgetTests
{
    [Fact]
    public void TextInkBudgetAllowsLowDeltaAtExpectedGlyphEdgeOnly()
    {
        var layout = TextScene();
        var expected = TextImage();
        var edgeNoise = WithPixel(expected, 7, 8, 243); // Expected glyph at x=8; this is its 1px AA band.
        var blankLineBoxNoise = WithPixel(expected, 14, 5, 243);

        var edge = new ParityComparer().Compare(layout, layout, expected, edgeNoise, TextProfile());
        var blank = new ParityComparer().Compare(layout, layout, expected, blankLineBoxNoise, TextProfile());

        Assert.True(edge.IsMatch);
        Assert.Equal(ParityComparisonKind.SemanticRasterEquivalent, edge.Kind);
        Assert.Equal("text-ink", Assert.Single(edge.Raster!.Regions).Kind);
        Assert.False(blank.IsMatch);
        Assert.Equal(1, blank.Pixels.DifferentPixels);
    }

    [Fact]
    public void TextInkBudgetRequiresMatchingTextLayout()
    {
        var expected = TextScene();
        var changedLayout = TextScene(characterOffset: 1);
        var image = TextImage();
        var actual = WithPixel(image, 7, 8, 243);

        var comparison = new ParityComparer().Compare(expected, changedLayout, image, actual, TextProfile());

        Assert.False(comparison.IsMatch);
        Assert.Contains(comparison.Failures, item => item.StartsWith("Text 'label' character 0 bounding differs", StringComparison.Ordinal));
        Assert.Equal(1, comparison.Pixels.DifferentPixels);
    }

    [Fact]
    public void ShadowSupportBudgetScalesWithExpectedSupportAndRejectsOutsideNoise()
    {
        var layout = ShadowScene();
        var expected = ShadowImage();
        var supportNoise = WithPixel(expected, 7, 15, 199); // Flat expected shadow support is 200.
        var outsideNoise = WithPixel(expected, 1, 1, 254);

        var within = new ParityComparer().Compare(layout, layout, expected, supportNoise, ShadowProfile(edgeBudget: 2));
        var outside = new ParityComparer().Compare(layout, layout, expected, outsideNoise, ShadowProfile(edgeBudget: 2));

        Assert.True(within.IsMatch);
        var support = Assert.Single(within.Raster!.Regions, region => region.Kind == "shadow-support");
        Assert.True(support.EligiblePixels > 0);
        Assert.False(outside.IsMatch);
        Assert.Equal(1, outside.Pixels.DifferentPixels);
    }

    [Fact]
    public void OpaqueOccluderDoesNotReceiveTheShadowSupportAllowance()
    {
        var layout = ShadowScene(occluderDrawn: true);
        var expected = ShadowImage();
        var occluderNoise = WithPixel(expected, 20, 15, 254);

        var comparison = new ParityComparer().Compare(layout, layout, expected, occluderNoise, ShadowProfile(edgeBudget: 2));

        Assert.False(comparison.IsMatch);
        Assert.Equal(1, comparison.Pixels.DifferentPixels);
    }

    [Fact]
    public void ShadowHighDeltaIsOnlyAllowedAtExpectedBoundaryAndWithinItsBudget()
    {
        var layout = ShadowScene();
        var expected = ShadowImage();
        var boundaryA = WithPixel(expected, 7, 10, 152);
        var boundaryB = WithPixel(boundaryA, 7, 14, 152);
        var boundaryC = WithPixel(boundaryB, 7, 18, 152);
        var interior = WithPixel(expected, 20, 15, 152);
        var profile = ShadowProfile(edgeBudget: 2);

        Assert.True(new ParityComparer().Compare(layout, layout, expected, boundaryB, profile).IsMatch);

        var tooMany = new ParityComparer().Compare(layout, layout, expected, boundaryC, profile);
        Assert.False(tooMany.IsMatch);
        Assert.True(tooMany.Pixels.DifferentPixels >= 1);

        var interiorMismatch = new ParityComparer().Compare(layout, layout, expected, interior, profile);
        Assert.False(interiorMismatch.IsMatch);
        Assert.Equal(1, interiorMismatch.Pixels.DifferentPixels);
    }

    [Fact]
    public void ShadowBoundaryRejectsAThickBlockEvenWhenTheCountBudgetWouldAllowIt()
    {
        var layout = ShadowScene();
        var expected = ShadowImage();
        var actual = expected;
        for (var y = 6; y <= 8; y++)
        {
            for (var x = 6; x <= 8; x++)
                actual = WithPixel(actual, x, y, 152);
        }

        var comparison = new ParityComparer().Compare(layout, layout, expected, actual, ShadowProfile(edgeBudget: 12));

        Assert.False(comparison.IsMatch);
        var edge = Assert.Single(comparison.Raster!.Regions, region => region.Kind == "shadow-edge");
        Assert.True(Assert.Single(edge.Budgets).LargestConnectedComponentThicknessPx > 2);
    }

    [Fact]
    public void ShadowBoundaryChecksEveryComponentInsteadOfOnlyTheLargestOne()
    {
        var layout = ShadowScene();
        var expected = ShadowImage();
        var actual = expected;
        for (var y = 10; y < 20; y++)
            actual = WithPixel(actual, 6, y, 152); // A larger, thin component.
        for (var y = 6; y <= 8; y++)
        {
            for (var x = 6; x <= 8; x++)
                actual = WithPixel(actual, x, y, 152); // A smaller 3px-thick component.
        }

        var comparison = new ParityComparer().Compare(layout, layout, expected, actual, ShadowProfile(edgeBudget: 64));

        Assert.False(comparison.IsMatch);
        var edge = Assert.Single(comparison.Raster!.Regions, region => region.Kind == "shadow-edge");
        var budget = Assert.Single(edge.Budgets);
        Assert.Equal(10, budget.LargestConnectedComponentPixels);
        Assert.Equal(3, budget.LargestConnectedComponentThicknessPx);
    }

    [Fact]
    public void OpaqueOnePixelBorderShiftDoesNotUseTheEdgeBudget()
    {
        var expected = Solid(24, 24, 255);
        for (var y = 5; y < 19; y++)
            expected = WithPixel(expected, 8, y, 0);
        var actual = Solid(24, 24, 255);
        for (var y = 5; y < 19; y++)
            actual = WithPixel(actual, 9, y, 0);
        var profile = Profile("shape", ShapeEdgePolicy());
        var layout = Layout("component", new ParityRect(4, 4, 20, 20));

        var comparison = new ParityComparer().Compare(layout, layout, expected, actual, profile);

        Assert.False(comparison.IsMatch);
        Assert.True(comparison.Pixels.MaximumChannelDelta >= 255);
    }

    [Fact]
    public void LowContrastExpectedBoundaryCanAbsorbOneChannelOfAntialiasing()
    {
        var expected = Solid(24, 24, 255);
        for (var y = 6; y < 18; y++)
        {
            for (var x = 6; x < 18; x++)
                expected = WithPixel(expected, x, y, 247);
        }
        var actual = WithPixel(expected, 6, 12, 246);
        var outside = WithPixel(expected, 3, 12, 254);
        var layout = Layout("component", new ParityRect(6, 6, 18, 18));
        var profile = Profile("low-contrast-shape", ShapeEdgePolicy(edgeDetectionDelta: 4));

        var comparison = new ParityComparer().Compare(
            layout,
            layout,
            expected,
            actual,
            profile);

        Assert.True(comparison.IsMatch);
        Assert.False(new ParityComparer().Compare(layout, layout, expected, outside, profile).IsMatch);
    }

    private static ParityComparisonProfile TextProfile() => Profile(
        "text",
        new ParityRasterPolicy(
            "semantic-raster-v1", 0, 16, 1, 4, 8,
            TextInk: Region(1, Budget(17, 10, 170, 10, 10)),
            ShapeEdge: null,
            ShadowSupport: null,
            ShadowEdge: null),
        compareTextLayout: true);

    private static ParityComparisonProfile ShadowProfile(int edgeBudget) => Profile(
        "shadow",
        new ParityRasterPolicy(
            "semantic-raster-v1", 0, 16, 1, 4, 8,
            TextInk: null,
            ShapeEdge: null,
            ShadowSupport: Region(1, Budget(1, 64, 64, 4096, 4096)),
            ShadowEdge: Region(1, Budget(48, edgeBudget, edgeBudget * 48, edgeBudget, 2))));

    private static ParityRasterPolicy ShapeEdgePolicy(byte edgeDetectionDelta = 16) => new(
        "semantic-raster-v1", 0, edgeDetectionDelta, 1, 4, 8,
        TextInk: null,
        ShapeEdge: Region(1, Budget(8, 64, 512, 64, 2)),
        ShadowSupport: null,
        ShadowEdge: null);

    private static ParityRasterRegionPolicy Region(int dilationPx, params ParityRasterDeltaBudget[] budgets) =>
        new(dilationPx, budgets);

    private static ParityRasterDeltaBudget Budget(
        byte maximumDelta,
        int pixels,
        int energy,
        int maximumComponentPixels,
        int maximumComponentThickness) => new(
        maximumDelta,
        0,
        pixels,
        0,
        pixels,
        0,
        energy,
        0,
        energy,
        maximumComponentPixels,
        maximumComponentThickness);

    private static ParityComparisonProfile Profile(
        string id,
        ParityRasterPolicy policy,
        bool compareTextLayout = false) => new(
        id,
        new ParityLayoutTolerance(0, 0, 0, 0),
        CompareGeometry: true,
        CompareTextLayout: compareTextLayout,
        CompareRaster: true,
        RasterPolicy: policy);

    private static ParityCaptureResult Layout(string id, ParityRect bounds) => new(
        ParityCaptureResult.CurrentSchemaVersion,
        "test",
        new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", 2),
        new ParityViewport(24, 24),
        [new ParityNode(id, "container", null, bounds, true)]);

    private static ParityCaptureResult TextScene(float characterOffset = 0)
    {
        var text = new ParityTextLayout(
            "A",
            "utf-16",
            12,
            12,
            1,
            false,
            [new ParityTextLine(0, 0, 1, 1, 5, 15, 4, 16, 12, false)],
            [new ParityCharacterBox(0, "0041", 0, new ParityRect(7 + characterOffset, 4, 10 + characterOffset, 16))]);
        return new ParityCaptureResult(
            ParityCaptureResult.CurrentSchemaVersion,
            "test",
            new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", 2),
            new ParityViewport(24, 24),
            [new ParityNode("label", "text", null, new ParityRect(5, 4, 15, 16), true, text)]);
    }

    private static ParityCaptureResult ShadowScene(bool occluderDrawn = false)
    {
        var shadow = new ParityShadow(
            "m150",
            "rectangle",
            new ParityRect(0, 0, 20, 10),
            new ParityRect(8, 8, 32, 22),
            new ParityCornerRadii(0, 0, 0, 0),
            new ParityAffineTransform(2, 0, 0, 2, 8, 8),
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
            OccluderDrawn: occluderDrawn);
        return new ParityCaptureResult(
            ParityCaptureResult.CurrentSchemaVersion,
            "test",
            new ParityCoordinateSpace("physical-pixel", "root-top-left", "right", "down", 2),
            new ParityViewport(40, 30),
            [new ParityNode("component", "shadow", null, new ParityRect(8, 8, 32, 22), true, Shadow: shadow)]);
    }

    private static RenderedImage TextImage()
    {
        var result = Solid(24, 24, 255);
        for (var y = 5; y < 15; y++)
            result = WithPixel(result, 8, y, 0);
        return result;
    }

    private static RenderedImage ShadowImage()
    {
        var result = Solid(40, 30, 255);
        for (var y = 6; y < 24; y++)
        {
            for (var x = 6; x < 34; x++)
            {
                if (x < 8 || x >= 32 || y < 8 || y >= 22)
                    result = WithPixel(result, x, y, 200);
            }
        }
        return result;
    }

    private static RenderedImage Solid(int width, int height, byte value)
    {
        var pixels = new byte[width * height * 4];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var offset = pixel * 4;
            pixels[offset] = value;
            pixels[offset + 1] = value;
            pixels[offset + 2] = value;
            pixels[offset + 3] = 255;
        }
        return new RenderedImage(width, height, pixels);
    }

    private static RenderedImage WithPixel(RenderedImage source, int x, int y, byte value)
    {
        var pixels = (byte[])source.Pixels.Clone();
        var offset = (y * source.Width + x) * 4;
        pixels[offset] = value;
        pixels[offset + 1] = value;
        pixels[offset + 2] = value;
        return source with { Pixels = pixels };
    }
}

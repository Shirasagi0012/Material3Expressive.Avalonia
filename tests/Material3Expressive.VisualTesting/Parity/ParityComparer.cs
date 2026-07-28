using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>Direct comparison only: no translation, registration, cropping, or Avalonia baseline.</summary>
public sealed class ParityComparer
{
    public ParityComparison Compare(
        ParityCaptureResult expected,
        ParityCaptureResult actual,
        RenderedImage expectedImage,
        RenderedImage actualImage,
        ParityComparisonProfile? profile = null)
    {
        profile ??= ParityComparisonProfiles.ComponentWithText;
        var tolerance = profile.Tolerance;
        var failures = new List<string>();
        if (profile.CompareGeometry)
        {
            CompareViewport(expected, actual, failures);
            CompareNodes(expected.Nodes, actual.Nodes, tolerance, profile.CompareTextLayout, failures);
        }
        var pixels = ComparePixels(expectedImage, actualImage);
        if (profile.CompareRaster
            && (pixels.DifferentPixels > tolerance.MaxDifferentPixels || pixels.MaximumChannelDelta > tolerance.MaxChannelDelta))
        {
            failures.Add($"Raster differs in {pixels.DifferentPixels}/{pixels.TotalPixels} pixels " +
                         $"(max channel delta {pixels.MaximumChannelDelta}).");
        }
        return new ParityComparison(profile.Id, failures.Count == 0, failures, pixels);
    }

    private static void CompareViewport(ParityCaptureResult expected, ParityCaptureResult actual, ICollection<string> failures)
    {
        if (expected.CoordinateSpace != actual.CoordinateSpace)
            failures.Add("Coordinate-space contract differs.");
        if (expected.ViewportPx != actual.ViewportPx)
            failures.Add($"Viewport differs: expected {expected.ViewportPx.Width}x{expected.ViewportPx.Height}, " +
                         $"actual {actual.ViewportPx.Width}x{actual.ViewportPx.Height}.");
    }

    private static void CompareNodes(
        IReadOnlyList<ParityNode> expected,
        IReadOnlyList<ParityNode> actual,
        ParityTolerance tolerance,
        bool compareTextLayout,
        ICollection<string> failures)
    {
        var actualById = actual.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var node in expected)
        {
            if (!actualById.TryGetValue(node.Id, out var counterpart))
            {
                failures.Add($"Required node '{node.Id}' is missing from Avalonia capture.");
                continue;
            }
            if (!counterpart.Visible || counterpart.BoundsInRootPx is null)
            {
                failures.Add($"Required node '{node.Id}' is not visible in Avalonia capture.");
                continue;
            }
            if (node.Kind != counterpart.Kind || node.ParentId != counterpart.ParentId)
                failures.Add($"Node '{node.Id}' identity differs (kind or parent).");
            CompareRect($"Node '{node.Id}' bounds", node.BoundsInRootPx, counterpart.BoundsInRootPx, tolerance.GeometryPx, failures);
            if (compareTextLayout)
                CompareText(node, counterpart, tolerance, failures);
        }
    }

    private static void CompareText(ParityNode expected, ParityNode actual, ParityTolerance tolerance, ICollection<string> failures)
    {
        if (expected.TextLayout is null)
            return;
        if (actual.TextLayout is null)
        {
            failures.Add($"Text layout for '{expected.Id}' is missing from Avalonia capture.");
            return;
        }
        var left = expected.TextLayout;
        var right = actual.TextLayout;
        if (left.Text != right.Text || left.Encoding != right.Encoding || left.LineCount != right.LineCount)
            failures.Add($"Text contract for '{expected.Id}' differs.");
        CompareNumber($"Text '{expected.Id}' first baseline", left.FirstBaselineInRootPx, right.FirstBaselineInRootPx, tolerance.TextPx, failures);
        CompareNumber($"Text '{expected.Id}' last baseline", left.LastBaselineInRootPx, right.LastBaselineInRootPx, tolerance.TextPx, failures);
        if (left.Lines.Count != right.Lines.Count)
            failures.Add($"Text '{expected.Id}' line count differs.");
        foreach (var line in left.Lines.Zip(right.Lines))
        {
            if (line.First.Index != line.Second.Index || line.First.StartUtf16 != line.Second.StartUtf16
                || line.First.EndUtf16 != line.Second.EndUtf16 || line.First.VisibleEndUtf16 != line.Second.VisibleEndUtf16)
                failures.Add($"Text '{expected.Id}' line {line.First.Index} UTF-16 range differs.");
            CompareNumber($"Text '{expected.Id}' line {line.First.Index} baseline", line.First.BaselinePx, line.Second.BaselinePx, tolerance.TextPx, failures);
        }
        if (left.CharacterBoxes.Count != right.CharacterBoxes.Count)
            failures.Add($"Text '{expected.Id}' UTF-16 box count differs.");
        foreach (var pair in left.CharacterBoxes.Zip(right.CharacterBoxes))
        {
            if (pair.First.Utf16Offset != pair.Second.Utf16Offset || pair.First.CodeUnit != pair.Second.CodeUnit)
                failures.Add($"Text '{expected.Id}' character contract differs at offset {pair.First.Utf16Offset}.");
            CompareRect($"Text '{expected.Id}' character {pair.First.Utf16Offset}", pair.First.BoundsInRootPx, pair.Second.BoundsInRootPx, tolerance.TextPx, failures);
        }
    }

    private static void CompareRect(string label, ParityRect? expected, ParityRect? actual, float tolerance, ICollection<string> failures)
    {
        if (expected is null || actual is null)
        {
            if (expected != actual) failures.Add($"{label} is missing.");
            return;
        }
        CompareNumber(label + " left", expected.Left, actual.Left, tolerance, failures);
        CompareNumber(label + " top", expected.Top, actual.Top, tolerance, failures);
        CompareNumber(label + " right", expected.Right, actual.Right, tolerance, failures);
        CompareNumber(label + " bottom", expected.Bottom, actual.Bottom, tolerance, failures);
    }

    private static void CompareNumber(string label, float expected, float actual, float tolerance, ICollection<string> failures)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            failures.Add($"{label} differs: expected {expected:F3}px, actual {actual:F3}px, tolerance {tolerance:F3}px.");
    }

    private static ParityPixelDifference ComparePixels(RenderedImage expected, RenderedImage actual)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return new ParityPixelDifference(Math.Max(expected.PixelCount, actual.PixelCount), Math.Max(expected.PixelCount, actual.PixelCount), byte.MaxValue);
        var different = 0;
        byte maximum = 0;
        for (var pixel = 0; pixel < expected.PixelCount; pixel++)
        {
            var differentPixel = false;
            var offset = pixel * 4;
            for (var channel = 0; channel < 4; channel++)
            {
                var delta = (byte)Math.Abs(expected.Pixels[offset + channel] - actual.Pixels[offset + channel]);
                maximum = Math.Max(maximum, delta);
                differentPixel |= delta != 0;
            }
            if (differentPixel)
                different++;
        }
        return new ParityPixelDifference(different, expected.PixelCount, maximum);
    }
}

public sealed record ParityTolerance(float GeometryPx, float TextPx, int MaxDifferentPixels, byte MaxChannelDelta);

public sealed record ParityPixelDifference(int DifferentPixels, int TotalPixels, byte MaximumChannelDelta);

public sealed record ParityComparison(string ProfileId, bool IsMatch, IReadOnlyList<string> Failures, ParityPixelDifference Pixels)
{
    public string Summary => IsMatch ? "Parity match." : string.Join(Environment.NewLine, Failures);
}

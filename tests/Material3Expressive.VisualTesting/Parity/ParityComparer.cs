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
        var matchedTextRasterRegions = profile.CompareGeometry
            ? CompareGeometryAndGetMatchedTextRegions(expected, actual, tolerance, profile.CompareTextLayout, failures)
            : [];
        var pixels = ComparePixels(expectedImage, actualImage, matchedTextRasterRegions, tolerance.TextRasterChannelDelta);
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

    private static IReadOnlyList<ParityRect> CompareGeometryAndGetMatchedTextRegions(
        ParityCaptureResult expected,
        ParityCaptureResult actual,
        ParityTolerance tolerance,
        bool compareTextLayout,
        ICollection<string> failures)
    {
        CompareViewport(expected, actual, failures);
        var matchedTextRasterRegions = new List<ParityRect>();
        var actualById = actual.Nodes.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var node in expected.Nodes)
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
            CompareOptionalBoolean($"Node '{node.Id}' focused state", node.Focused, counterpart.Focused, failures);
            CompareRect($"Node '{node.Id}' bounds", node.BoundsInRootPx, counterpart.BoundsInRootPx, tolerance.GeometryPx, failures);
            if (compareTextLayout && CompareText(node, counterpart, tolerance, failures) && node.TextLayout is not null)
            {
                matchedTextRasterRegions.AddRange(node.TextLayout.Lines.Select(line => new ParityRect(
                    line.LeftPx,
                    line.TopPx,
                    line.RightPx,
                    line.BottomPx)));
            }
        }
        return matchedTextRasterRegions;
    }

    private static bool CompareText(ParityNode expected, ParityNode actual, ParityTolerance tolerance, ICollection<string> failures)
    {
        if (expected.TextLayout is null)
            return true;
        if (actual.TextLayout is null)
        {
            failures.Add($"Text layout for '{expected.Id}' is missing from Avalonia capture.");
            return false;
        }
        var failureCount = failures.Count;
        var left = expected.TextLayout;
        var right = actual.TextLayout;
        if (left.Text != right.Text || left.Encoding != right.Encoding || left.LineCount != right.LineCount)
            failures.Add($"Text contract for '{expected.Id}' differs.");
        CompareOptionalPoint(
            $"Text '{expected.Id}' draw origin",
            left.DrawOriginInRootPx,
            right.DrawOriginInRootPx,
            tolerance.TextPx,
            failures);
        CompareOptionalNumber(
            $"Text '{expected.Id}' first baseline in text",
            left.FirstBaselineInTextPx,
            right.FirstBaselineInTextPx,
            tolerance.TextPx,
            failures);
        CompareOptionalNumber(
            $"Text '{expected.Id}' last baseline in text",
            left.LastBaselineInTextPx,
            right.LastBaselineInTextPx,
            tolerance.TextPx,
            failures);
        CompareTextStyleSignature(expected.Id, left.StyleSignature, right.StyleSignature, failures);
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
            CompareOptionalRect(
                $"Text '{expected.Id}' line {line.First.Index} local bounds",
                line.First.BoundsInTextPx,
                line.Second.BoundsInTextPx,
                tolerance.TextPx,
                failures);
            CompareOptionalNumber(
                $"Text '{expected.Id}' line {line.First.Index} local baseline",
                line.First.BaselineInTextPx,
                line.Second.BaselineInTextPx,
                tolerance.TextPx,
                failures);
        }
        if (left.CharacterBoxes.Count != right.CharacterBoxes.Count)
            failures.Add($"Text '{expected.Id}' UTF-16 box count differs.");
        foreach (var pair in left.CharacterBoxes.Zip(right.CharacterBoxes))
        {
            if (pair.First.Utf16Offset != pair.Second.Utf16Offset || pair.First.CodeUnit != pair.Second.CodeUnit)
                failures.Add($"Text '{expected.Id}' character contract differs at offset {pair.First.Utf16Offset}.");
            CompareRect($"Text '{expected.Id}' character {pair.First.Utf16Offset} bounding", pair.First.BoundsInRootPx, pair.Second.BoundsInRootPx, tolerance.TextPx, failures);
            CompareOptionalRect(
                $"Text '{expected.Id}' character {pair.First.Utf16Offset} local bounding",
                pair.First.BoundsInTextPx,
                pair.Second.BoundsInTextPx,
                tolerance.TextPx,
                failures);
        }
        return failures.Count == failureCount;
    }

    private static void CompareTextStyleSignature(
        string id,
        ParityTextStyleSignature? expected,
        ParityTextStyleSignature? actual,
        ICollection<string> failures)
    {
        if (expected is null && actual is null)
            return;
        if (expected is null || actual is null)
        {
            failures.Add($"Text '{id}' style signature is missing from one capture.");
            return;
        }
        if (expected != actual)
        {
            failures.Add(
                $"Text '{id}' style signature differs: expected {FormatStyleSignature(expected)}, " +
                $"actual {FormatStyleSignature(actual)}.");
        }
    }

    private static void CompareRect(string label, ParityRect? expected, ParityRect? actual, float tolerance, ICollection<string> failures)
    {
        if (expected is null || actual is null)
        {
            if (expected != actual) failures.Add($"{label} is missing.");
            return;
        }
        if (MathF.Abs(expected.Left - actual.Left) <= tolerance
            && MathF.Abs(expected.Top - actual.Top) <= tolerance
            && MathF.Abs(expected.Right - actual.Right) <= tolerance
            && MathF.Abs(expected.Bottom - actual.Bottom) <= tolerance)
            return;
        failures.Add(
            $"{label} differs (LTRB px): expected {FormatRect(expected)}, actual {FormatRect(actual)}, " +
            $"tolerance {FormatTolerance(tolerance)}.");
    }

    private static void CompareOptionalRect(
        string label,
        ParityRect? expected,
        ParityRect? actual,
        float tolerance,
        ICollection<string> failures)
    {
        if (expected is null && actual is null)
            return;
        CompareRect(label, expected, actual, tolerance, failures);
    }

    private static void CompareOptionalPoint(
        string label,
        ParityPoint? expected,
        ParityPoint? actual,
        float tolerance,
        ICollection<string> failures)
    {
        if (expected is null && actual is null)
            return;
        if (expected is null || actual is null)
        {
            failures.Add($"{label} is missing.");
            return;
        }
        if (MathF.Abs(expected.X - actual.X) > tolerance || MathF.Abs(expected.Y - actual.Y) > tolerance)
        {
            failures.Add(
                $"{label} differs (XY px): expected ({expected.X:F3}, {expected.Y:F3}), " +
                $"actual ({actual.X:F3}, {actual.Y:F3}), tolerance ({tolerance:F3}, {tolerance:F3}).");
        }
    }

    private static void CompareOptionalNumber(
        string label,
        float? expected,
        float? actual,
        float tolerance,
        ICollection<string> failures)
    {
        if (expected is null && actual is null)
            return;
        if (expected is null || actual is null)
        {
            failures.Add($"{label} is missing.");
            return;
        }
        CompareNumber(label, expected.Value, actual.Value, tolerance, failures);
    }

    private static void CompareOptionalBoolean(
        string label,
        bool? expected,
        bool? actual,
        ICollection<string> failures)
    {
        if (expected is null && actual is null)
            return;
        if (expected is null || actual is null)
        {
            failures.Add($"{label} is missing.");
            return;
        }
        if (expected != actual)
            failures.Add($"{label} differs: expected {expected.Value.ToString().ToLowerInvariant()}, actual {actual.Value.ToString().ToLowerInvariant()}.");
    }

    private static string FormatRect(ParityRect rect) =>
        FormattableString.Invariant($"({rect.Left:F3}, {rect.Top:F3}, {rect.Right:F3}, {rect.Bottom:F3})");

    private static string FormatTolerance(float tolerance) =>
        FormattableString.Invariant($"({tolerance:F3}, {tolerance:F3}, {tolerance:F3}, {tolerance:F3})");

    private static string FormatStyleSignature(ParityTextStyleSignature signature) =>
        FormattableString.Invariant(
            $"family={signature.RequestedFontFamily}/{signature.ResolvedFontFamily}, fontHash={signature.PinnedFontFileSha256 ?? "none"}, weight={signature.FontWeight}, size={FormatOptionalPixels(signature.FontSizePx)}, lineHeight={FormatOptionalPixels(signature.LineHeightPx)}, letterSpacing={FormatOptionalPixels(signature.LetterSpacingPx)}");

    private static string FormatOptionalPixels(float? value) => value is null
        ? "unspecified"
        : FormattableString.Invariant($"{value.Value:F3}px");

    private static void CompareNumber(string label, float expected, float actual, float tolerance, ICollection<string> failures)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            failures.Add($"{label} differs: expected {expected:F3}px, actual {actual:F3}px, tolerance {tolerance:F3}px.");
    }

    private static ParityPixelDifference ComparePixels(
        RenderedImage expected,
        RenderedImage actual,
        IReadOnlyList<ParityRect> matchedTextRasterRegions,
        byte textRasterChannelDelta)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return new ParityPixelDifference(Math.Max(expected.PixelCount, actual.PixelCount), Math.Max(expected.PixelCount, actual.PixelCount), byte.MaxValue);
        var different = 0;
        byte maximum = 0;
        for (var pixel = 0; pixel < expected.PixelCount; pixel++)
        {
            var offset = pixel * 4;
            byte pixelMaximum = 0;
            for (var channel = 0; channel < 4; channel++)
            {
                var delta = (byte)Math.Abs(expected.Pixels[offset + channel] - actual.Pixels[offset + channel]);
                pixelMaximum = Math.Max(pixelMaximum, delta);
            }
            if (pixelMaximum == 0)
                continue;
            var x = pixel % expected.Width;
            var y = pixel / expected.Width;
            if (pixelMaximum <= textRasterChannelDelta && IsInsideMatchedTextRasterRegion(x, y, matchedTextRasterRegions))
                continue;
            maximum = Math.Max(maximum, pixelMaximum);
            different++;
        }
        return new ParityPixelDifference(different, expected.PixelCount, maximum);
    }

    private static bool IsInsideMatchedTextRasterRegion(int x, int y, IReadOnlyList<ParityRect> regions) =>
        regions.Any(region => x + 0.5f >= region.Left && x + 0.5f < region.Right
                              && y + 0.5f >= region.Top && y + 0.5f < region.Bottom);
}

/// <summary>
/// <see cref="TextRasterChannelDelta"/> is a local gray-antialiasing noise floor. It applies
/// only inside a text line whose layout and resolved text style both compared successfully.
/// </summary>
public sealed record ParityTolerance(
    float GeometryPx,
    float TextPx,
    int MaxDifferentPixels,
    byte MaxChannelDelta,
    byte TextRasterChannelDelta);

public sealed record ParityPixelDifference(int DifferentPixels, int TotalPixels, byte MaximumChannelDelta);

public sealed record ParityComparison(string ProfileId, bool IsMatch, IReadOnlyList<string> Failures, ParityPixelDifference Pixels)
{
    public string Summary => IsMatch ? "Parity match." : string.Join(Environment.NewLine, Failures);
}

using Material3Expressive.VisualTesting.Model;
using System.Text.Json.Serialization;

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
        var tolerance = profile.LayoutTolerance;
        var failures = new List<string>();
        CompareMotion(expected.Motion, actual.Motion, failures);
        var semantic = profile.CompareGeometry
            ? CompareGeometryAndGetMatchedTextRegions(expected, actual, tolerance, profile.CompareTextLayout, failures)
            : ParityRasterSemanticContext.Empty;
        var rasterPolicy = profile.RasterPolicy
            ?? throw new InvalidOperationException($"Visual-parity profile '{profile.Id}' has no semantic raster policy.");
        var raster = SemanticRasterComparer.Compare(expectedImage, actualImage, rasterPolicy, semantic);
        var pixels = new ParityPixelDifference(
            raster.RejectedPixels,
            Math.Max(expectedImage.PixelCount, actualImage.PixelCount),
            raster.MaximumRejectedChannelDelta);
        ParityRasterComparison? warningRaster = null;
        IReadOnlyList<string> warnings = [];
        if (profile.CompareRaster && !raster.IsMatch)
        {
            var rasterFailure = $"Raster differs in {pixels.DifferentPixels}/{pixels.TotalPixels} pixels " +
                                $"(max channel delta {pixels.MaximumChannelDelta}).";
            if (failures.Count == 0 && profile.WarningRasterPolicy is not null)
            {
                warningRaster = SemanticRasterComparer.Compare(
                    expectedImage,
                    actualImage,
                    profile.WarningRasterPolicy,
                    semantic);
                if (warningRaster.IsMatch)
                {
                    warnings =
                    [
                        $"{rasterFailure} Warning accepted by profile '{profile.Id}': {profile.WarningReason}"
                    ];
                }
                else
                {
                    failures.Add(rasterFailure);
                }
            }
            else
            {
                failures.Add(rasterFailure);
            }
        }
        return new ParityComparison(profile.Id, failures.Count == 0, failures, pixels, raster, warningRaster, warnings);
    }

    private static void CompareMotion(
        ParityMotionCapture? expected,
        ParityMotionCapture? actual,
        ICollection<string> failures)
    {
        if (expected is null && actual is null)
            return;
        if (expected is null || actual is null)
        {
            failures.Add("Motion timeline contract is missing from one capture.");
            return;
        }

        if (expected.SchemaVersion != actual.SchemaVersion
            || expected.ScenarioId != actual.ScenarioId
            || expected.CheckpointId != actual.CheckpointId
            || expected.Origin != actual.Origin
            || expected.TimeUnit != actual.TimeUnit
            || expected.DurationScale != actual.DurationScale
            || expected.ElapsedTicks != actual.ElapsedTicks
            || expected.CaptureFrameIndex != actual.CaptureFrameIndex
            || !string.Equals(expected.ScheduleSha256, actual.ScheduleSha256, StringComparison.OrdinalIgnoreCase)
            || expected.ClockCoverage != actual.ClockCoverage
            || !expected.Events.SequenceEqual(actual.Events)
            || !expected.DeliveryFrameTicks.SequenceEqual(actual.DeliveryFrameTicks))
        {
            failures.Add("Motion timeline contract differs.");
        }
    }

    private static void CompareViewport(ParityCaptureResult expected, ParityCaptureResult actual, ICollection<string> failures)
    {
        if (expected.CoordinateSpace != actual.CoordinateSpace)
            failures.Add("Coordinate-space contract differs.");
        if (expected.ViewportPx != actual.ViewportPx)
            failures.Add($"Viewport differs: expected {expected.ViewportPx.Width}x{expected.ViewportPx.Height}, " +
                         $"actual {actual.ViewportPx.Width}x{actual.ViewportPx.Height}.");
    }

    private static ParityRasterSemanticContext CompareGeometryAndGetMatchedTextRegions(
        ParityCaptureResult expected,
        ParityCaptureResult actual,
        ParityLayoutTolerance tolerance,
        bool compareTextLayout,
        ICollection<string> failures)
    {
        CompareViewport(expected, actual, failures);
        var matchedTextNodes = new List<ParityNode>();
        var matchedShadowNodes = new List<ParityNode>();
        var actualById = actual.Nodes.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var node in expected.Nodes)
        {
            var nodeFailureCount = failures.Count;
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
            CompareShadow(node.Id, node.Shadow, counterpart.Shadow, tolerance.GeometryPx, failures);
            var textMatched = compareTextLayout && CompareText(node, counterpart, tolerance, failures);
            if (failures.Count == nodeFailureCount && textMatched && node.TextLayout is not null)
            {
                matchedTextNodes.Add(node);
            }
            if (failures.Count == nodeFailureCount && node.Shadow is not null)
                matchedShadowNodes.Add(node);
        }
        return new ParityRasterSemanticContext(matchedTextNodes, matchedShadowNodes);
    }

    private static void CompareShadow(
        string id,
        ParityShadow? expected,
        ParityShadow? actual,
        float geometryTolerance,
        ICollection<string> failures)
    {
        if (expected is null && actual is null)
            return;
        if (expected is null || actual is null)
        {
            failures.Add($"Shadow contract for '{id}' is missing from one capture.");
            return;
        }

        if (expected.SkiaBehaviorVersion != actual.SkiaBehaviorVersion
            || expected.ShapeKind != actual.ShapeKind
            || expected.AmbientColorArgb != actual.AmbientColorArgb
            || expected.SpotColorArgb != actual.SpotColorArgb
            || expected.TransparentOccluder != actual.TransparentOccluder
            || expected.OccluderDrawn != actual.OccluderDrawn
            || !expected.Flags.SequenceEqual(actual.Flags, StringComparer.Ordinal))
        {
            failures.Add($"Shadow identity for '{id}' differs.");
        }

        CompareRect($"Shadow '{id}' local shape bounds", expected.ShapeBoundsLocalDp, actual.ShapeBoundsLocalDp, geometryTolerance, failures);
        CompareRect($"Shadow '{id}' device occluder bounds", expected.OccluderBoundsDevicePx, actual.OccluderBoundsDevicePx, geometryTolerance, failures);
        CompareOptionalRect($"Shadow '{id}' parent clip bounds", expected.ClipBoundsInRootPx, actual.ClipBoundsInRootPx, geometryTolerance, failures);
        CompareCornerRadii(id, expected.CornerRadiiDp, actual.CornerRadiiDp, geometryTolerance, failures);
        CompareTransform(id, expected.LocalToDevice, actual.LocalToDevice, geometryTolerance, failures);
        CompareNumber($"Shadow '{id}' elevation", expected.ElevationPx, actual.ElevationPx, geometryTolerance, failures);
        CompareNumber($"Shadow '{id}' light X", expected.LightPositionPx.X, actual.LightPositionPx.X, geometryTolerance, failures);
        CompareNumber($"Shadow '{id}' light Y", expected.LightPositionPx.Y, actual.LightPositionPx.Y, geometryTolerance, failures);
        CompareNumber($"Shadow '{id}' light Z", expected.LightPositionPx.Z, actual.LightPositionPx.Z, geometryTolerance, failures);
        CompareNumber($"Shadow '{id}' light radius", expected.LightRadiusPx, actual.LightRadiusPx, geometryTolerance, failures);
        CompareScalar($"Shadow '{id}' ambient alpha", expected.AmbientAlpha, actual.AmbientAlpha, failures);
        CompareScalar($"Shadow '{id}' spot alpha", expected.SpotAlpha, actual.SpotAlpha, failures);
        CompareScalar($"Shadow '{id}' opacity", expected.Opacity, actual.Opacity, failures);
    }

    private static void CompareCornerRadii(
        string id,
        ParityCornerRadii expected,
        ParityCornerRadii actual,
        float tolerance,
        ICollection<string> failures)
    {
        if (MathF.Abs(expected.TopLeft - actual.TopLeft) <= tolerance
            && MathF.Abs(expected.TopRight - actual.TopRight) <= tolerance
            && MathF.Abs(expected.BottomRight - actual.BottomRight) <= tolerance
            && MathF.Abs(expected.BottomLeft - actual.BottomLeft) <= tolerance)
            return;
        failures.Add($"Shadow '{id}' corner radii differ.");
    }

    private static void CompareTransform(
        string id,
        ParityAffineTransform expected,
        ParityAffineTransform actual,
        float tolerance,
        ICollection<string> failures)
    {
        if (MathF.Abs(expected.M11 - actual.M11) <= tolerance
            && MathF.Abs(expected.M12 - actual.M12) <= tolerance
            && MathF.Abs(expected.M21 - actual.M21) <= tolerance
            && MathF.Abs(expected.M22 - actual.M22) <= tolerance
            && MathF.Abs(expected.M31 - actual.M31) <= tolerance
            && MathF.Abs(expected.M32 - actual.M32) <= tolerance)
            return;
        failures.Add($"Shadow '{id}' local-to-device transform differs.");
    }

    private static void CompareScalar(string label, float expected, float actual, ICollection<string> failures)
    {
        const float tolerance = 0.00001f;
        if (MathF.Abs(expected - actual) > tolerance)
            failures.Add($"{label} differs: expected {expected:F6}, actual {actual:F6}.");
    }

    private static bool CompareText(ParityNode expected, ParityNode actual, ParityLayoutTolerance tolerance, ICollection<string> failures)
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
            tolerance.TextBaselinePx,
            failures);
        CompareOptionalNumber(
            $"Text '{expected.Id}' last baseline in text",
            left.LastBaselineInTextPx,
            right.LastBaselineInTextPx,
            tolerance.TextBaselinePx,
            failures);
        CompareTextStyleSignature(expected.Id, left.StyleSignature, right.StyleSignature, failures);
        CompareNumber($"Text '{expected.Id}' first baseline", left.FirstBaselineInRootPx, right.FirstBaselineInRootPx, tolerance.TextBaselinePx, failures);
        CompareNumber($"Text '{expected.Id}' last baseline", left.LastBaselineInRootPx, right.LastBaselineInRootPx, tolerance.TextBaselinePx, failures);
        if (left.Lines.Count != right.Lines.Count)
            failures.Add($"Text '{expected.Id}' line count differs.");
        foreach (var line in left.Lines.Zip(right.Lines))
        {
            if (line.First.Index != line.Second.Index || line.First.StartUtf16 != line.Second.StartUtf16
                || line.First.EndUtf16 != line.Second.EndUtf16 || line.First.VisibleEndUtf16 != line.Second.VisibleEndUtf16)
                failures.Add($"Text '{expected.Id}' line {line.First.Index} UTF-16 range differs.");
            CompareNumber($"Text '{expected.Id}' line {line.First.Index} baseline", line.First.BaselinePx, line.Second.BaselinePx, tolerance.TextBaselinePx, failures);
            CompareOptionalRect(
                $"Text '{expected.Id}' line {line.First.Index} local bounds",
                line.First.BoundsInTextPx,
                line.Second.BoundsInTextPx,
                tolerance.TextLineBoundsPx,
                failures);
            CompareOptionalNumber(
                $"Text '{expected.Id}' line {line.First.Index} local baseline",
                line.First.BaselineInTextPx,
                line.Second.BaselineInTextPx,
                tolerance.TextBaselinePx,
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

}

/// <summary>
/// Layout-only physical-pixel tolerances. Raster acceptance is deliberately expressed by
/// <see cref="ParityRasterPolicy"/> so a global pixel allowance cannot accidentally mask a
/// semantic or geometry defect.
/// </summary>
public sealed record ParityLayoutTolerance(
    float GeometryPx,
    float TextPx,
    float TextBaselinePx,
    float TextLineBoundsPx);

public sealed record ParityPixelDifference(int DifferentPixels, int TotalPixels, byte MaximumChannelDelta);

/// <summary>Whether a comparison was exact, semantically equivalent, warning-accepted, or failed.</summary>
public enum ParityComparisonKind
{
    ExactMatch,
    SemanticRasterEquivalent,
    RasterWarning,
    Mismatch
}

public sealed record ParityComparison(
    string ProfileId,
    bool IsMatch,
    IReadOnlyList<string> Failures,
    ParityPixelDifference Pixels,
    ParityRasterComparison? Raster = null,
    ParityRasterComparison? WarningRaster = null,
    IReadOnlyList<string>? Warnings = null)
{
    [JsonIgnore]
    public IReadOnlyList<string> WarningsOrEmpty => Warnings ?? [];

    public ParityComparisonKind Kind => !IsMatch
        ? ParityComparisonKind.Mismatch
        : WarningsOrEmpty.Count > 0
            ? ParityComparisonKind.RasterWarning
            : Raster is { AllowedPixels: > 0 }
                ? ParityComparisonKind.SemanticRasterEquivalent
                : ParityComparisonKind.ExactMatch;

    public string Summary => !IsMatch
        ? string.Join(Environment.NewLine, Failures)
        : WarningsOrEmpty.Count == 0
            ? "Parity match."
            : "Parity match with warning." + Environment.NewLine + string.Join(Environment.NewLine, WarningsOrEmpty);
}

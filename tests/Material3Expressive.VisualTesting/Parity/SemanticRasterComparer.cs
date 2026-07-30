using Material3Expressive.VisualTesting.Model;
using System.Text.Json.Serialization;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>
/// Expected-only raster comparison for scenes whose semantic and layout contracts have already
/// matched. Pixels outside a declared semantic region remain strict; a region's allowance is
/// derived from its own expected-mask size rather than from the screenshot dimensions.
/// </summary>
internal static class SemanticRasterComparer
{
    public static ParityRasterComparison Compare(
        RenderedImage expected,
        RenderedImage actual,
        ParityRasterPolicy policy,
        ParityRasterSemanticContext semantic)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            var pixels = Math.Max(expected.PixelCount, actual.PixelCount);
            var rejected = Enumerable.Range(0, expected.PixelCount).ToArray();
            return new ParityRasterComparison(
                pixels,
                byte.MaxValue,
                0,
                0,
                pixels,
                byte.MaxValue,
                [new ParityRasterRegionResult(
                    "image-size",
                    "image-size-mismatch",
                    0,
                    0,
                    pixels,
                    pixels * byte.MaxValue,
                    0,
                    0,
                    [],
                    pixels,
                    byte.MaxValue,
                    [ParityRasterRejectionReason.ImageSizeMismatch],
                    expected.PixelCount == 0 ? null : new ParityPixelBounds(0, 0, expected.Width - 1, expected.Height - 1),
                    [])],
                [ParityRasterRejectionReason.ImageSizeMismatch],
                expected.PixelCount == 0 ? null : new ParityPixelBounds(0, 0, expected.Width - 1, expected.Height - 1),
                [],
                rejected);
        }

        var regions = BuildRegions(expected, policy, semantic);
        var owners = AssignOwners(expected.PixelCount, regions);
        var accumulators = regions
            .Select((region, priority) => new RegionAccumulator(region, priority, expected.Width, expected.Height))
            .ToArray();
        var ignoredFloor = 0;
        var rawDifferent = 0;
        byte rawMaximum = 0;
        var outside = new List<ObservedPixel>();

        for (var pixel = 0; pixel < expected.PixelCount; pixel++)
        {
            var delta = VisualChannelDelta(expected, actual, pixel);
            if (delta == 0)
                continue;
            rawDifferent++;
            rawMaximum = Math.Max(rawMaximum, delta);
            if (delta <= policy.StrictChannelDelta)
            {
                ignoredFloor++;
                continue;
            }

            var owner = owners[pixel];
            if (owner < 0)
            {
                outside.Add(new ObservedPixel(pixel, delta));
                continue;
            }

            accumulators[owner].Record(delta, pixel);
        }

        var reports = accumulators.Select(item => item.BuildReport(expected, actual)).ToList();
        if (outside.Count != 0)
        {
            var outsidePixels = outside.Select(item => item.Pixel).ToArray();
            reports.Add(new ParityRasterRegionResult(
                "strict-outside-semantic-mask",
                "strict-unowned",
                regions.Count,
                owners.Count(owner => owner < 0),
                outside.Count,
                outside.Sum(item => item.Delta),
                0,
                0,
                [],
                outside.Count,
                outside.Max(item => item.Delta),
                [ParityRasterRejectionReason.OutsideSemanticMask],
                BoundsOf(outsidePixels, expected.Width),
                SamplesOf(outsidePixels, expected, actual)));
        }

        var rejectedPixels = accumulators
            .SelectMany(item => item.RejectedPixelIndices)
            .Concat(outside.Select(item => item.Pixel))
            .Distinct()
            .Order()
            .ToArray();
        var reasons = reports
            .SelectMany(report => report.RejectionReasons)
            .Distinct()
            .ToArray();
        var allowed = rawDifferent - ignoredFloor - rejectedPixels.Length;
        return new ParityRasterComparison(
            rawDifferent,
            rawMaximum,
            ignoredFloor,
            allowed,
            rejectedPixels.Length,
            MaximumDelta(rejectedPixels, expected, actual),
            reports,
            reasons,
            BoundsOf(rejectedPixels, expected.Width),
            SamplesOf(rejectedPixels, expected, actual),
            rejectedPixels);
    }

    private static IReadOnlyList<RasterRegion> BuildRegions(
        RenderedImage expected,
        ParityRasterPolicy policy,
        ParityRasterSemanticContext semantic)
    {
        var regions = new List<RasterRegion>();

        // A text allocation is only valid after all text and owner-node facts matched. The mask
        // is derived from the expected glyph edges, not from the entire line rectangle.
        if (policy.TextInk is not null)
        {
            foreach (var node in semantic.MatchedTextNodes)
            {
                var mask = BuildTextInkMask(expected, node.TextLayout!, policy);
                if (HasPixels(mask))
                    regions.Add(new RasterRegion($"text:{node.Id}", "text-ink", mask, policy.TextInk));
            }
        }

        // Shadow-edge comes before generic shape-edge so the rare high-delta quantization
        // allowance can only be spent on a matched shadow's expected boundary.
        var shadowSupportRegions = new List<RasterRegion>();
        var shadowOccluderRegions = new List<RasterRegion>();
        if (policy.ShadowEdge is not null
            || policy.ShadowSupport is not null
            || policy.ShadowOccluder is not null)
        {
            var background = FindDominantPixel(expected);
            foreach (var node in semantic.MatchedShadowNodes)
            {
                var envelope = BuildShadowEnvelopeMask(expected, node.Shadow!, policy);
                var occluder = BuildOccluderMask(expected, node.Shadow!);
                var support = BuildShadowSupportMask(expected, background, envelope, occluder, node.Shadow!, policy);
                if (!HasPixels(support))
                    continue;

                if (policy.ShadowEdge is not null)
                {
                    var edge = BuildBinaryBoundary(
                        support,
                        expected.Width,
                        expected.Height,
                        policy.ShadowEdge.DilationPx);
                    Intersect(edge, envelope);
                    if (node.Shadow!.OccluderDrawn && !node.Shadow.TransparentOccluder)
                    {
                        if (policy.ShadowOccluder is null)
                        {
                            Except(edge, occluder);
                        }
                        else
                        {
                            var occluderEdge = BuildBinaryBoundary(
                                occluder,
                                expected.Width,
                                expected.Height,
                                policy.ShadowEdge.DilationPx);
                            Intersect(occluderEdge, envelope);
                            Union(edge, occluderEdge);
                        }
                    }
                    if (HasPixels(edge))
                        regions.Add(new RasterRegion($"shadow-edge:{node.Id}", "shadow-edge", edge, policy.ShadowEdge));
                }
                if (policy.ShadowSupport is not null)
                    shadowSupportRegions.Add(new RasterRegion($"shadow:{node.Id}", "shadow-support", support, policy.ShadowSupport));
                if (policy.ShadowOccluder is not null
                    && node.Shadow!.OccluderDrawn
                    && !node.Shadow.TransparentOccluder)
                {
                    var ownedOccluder = Dilate(
                        occluder,
                        expected.Width,
                        expected.Height,
                        policy.ShadowOccluder.DilationPx);
                    Intersect(ownedOccluder, envelope);
                    shadowOccluderRegions.Add(new RasterRegion(
                        $"shadow-occluder:{node.Id}",
                        "shadow-occluder",
                        ownedOccluder,
                        policy.ShadowOccluder));
                }
            }
        }

        if (policy.ShapeEdge is not null)
        {
            var edge = BuildImageEdgeMask(
                expected,
                policy.EdgeDetectionChannelDelta,
                policy.ShapeEdge.DilationPx,
                null);
            if (HasPixels(edge))
                regions.Add(new RasterRegion("shape-edge", "shape-edge", edge, policy.ShapeEdge));
        }

        // The opaque occluder and diffuse support are deliberately less specific than text and
        // edges. The occluder owner is only present in motion profiles that explicitly model
        // whole-layer alpha composition; ordinary component fills remain strict.
        regions.AddRange(shadowOccluderRegions);
        regions.AddRange(shadowSupportRegions);

        return regions;
    }

    private static int[] AssignOwners(int pixelCount, IReadOnlyList<RasterRegion> regions)
    {
        var result = Enumerable.Repeat(-1, pixelCount).ToArray();
        for (var index = 0; index < regions.Count; index++)
        {
            var mask = regions[index].Mask;
            for (var pixel = 0; pixel < mask.Length; pixel++)
            {
                // Earlier regions are deliberately more specific: text, shadow boundary,
                // generic edge, then diffuse shadow support.
                if (mask[pixel] && result[pixel] < 0)
                    result[pixel] = index;
            }
        }
        return result;
    }

    private static bool[] BuildTextInkMask(
        RenderedImage image,
        ParityTextLayout layout,
        ParityRasterPolicy policy)
    {
        var clip = new bool[image.PixelCount];
        var padding = policy.TextInk!.DilationPx;
        foreach (var line in layout.Lines)
        {
            var left = Math.Max(0, (int)MathF.Floor(line.LeftPx) - padding);
            var top = Math.Max(0, (int)MathF.Floor(line.TopPx) - padding);
            var right = Math.Min(image.Width, (int)MathF.Ceiling(line.RightPx) + padding);
            var bottom = Math.Min(image.Height, (int)MathF.Ceiling(line.BottomPx) + padding);
            for (var y = top; y < bottom; y++)
            {
                for (var x = left; x < right; x++)
                    clip[y * image.Width + x] = true;
            }
        }

        return BuildImageEdgeMask(
            image,
            policy.EdgeDetectionChannelDelta,
            policy.TextInk.DilationPx,
            clip);
    }

    private static bool[] BuildShadowEnvelopeMask(
        RenderedImage image,
        ParityShadow shadow,
        ParityRasterPolicy policy)
    {
        var result = new bool[image.PixelCount];
        var padding = Math.Max(
            policy.MinimumShadowPaddingPx,
            (int)MathF.Ceiling(shadow.ElevationPx * policy.ShadowPaddingPerElevationPx));
        var bounds = shadow.OccluderBoundsDevicePx;
        var clip = shadow.ClipBoundsInRootPx;
        var left = Math.Max(0, (int)MathF.Floor(bounds.Left) - padding);
        var top = Math.Max(0, (int)MathF.Floor(bounds.Top) - padding);
        var right = Math.Min(image.Width, (int)MathF.Ceiling(bounds.Right) + padding);
        var bottom = Math.Min(image.Height, (int)MathF.Ceiling(bounds.Bottom) + padding);
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (clip is not null && !Contains(clip, x + 0.5f, y + 0.5f))
                    continue;
                var pixel = y * image.Width + x;
                result[pixel] = true;
            }
        }

        return result;
    }

    private static bool[] BuildShadowSupportMask(
        RenderedImage image,
        uint background,
        bool[] envelope,
        bool[] occluder,
        ParityShadow shadow,
        ParityRasterPolicy policy)
    {
        var result = new bool[image.PixelCount];
        for (var pixel = 0; pixel < result.Length; pixel++)
        {
            if (envelope[pixel]
                && (!shadow.OccluderDrawn || shadow.TransparentOccluder || !occluder[pixel])
                && ExpectedPixelDelta(image, pixel, background) > policy.ShadowSignalChannelDelta)
                result[pixel] = true;
        }

        var dilated = Dilate(result, image.Width, image.Height, policy.ShadowSupport?.DilationPx ?? 0);
        for (var pixel = 0; pixel < dilated.Length; pixel++)
            dilated[pixel] &= envelope[pixel]
                              && (!shadow.OccluderDrawn || shadow.TransparentOccluder || !occluder[pixel]);
        return dilated;
    }

    private static bool[] BuildOccluderMask(RenderedImage image, ParityShadow shadow)
    {
        var result = new bool[image.PixelCount];
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
                result[y * image.Width + x] = IsInsideOccluder(shadow, x + 0.5f, y + 0.5f);
        }
        return result;
    }

    private static bool IsInsideOccluder(ParityShadow shadow, float x, float y)
    {
        var bounds = shadow.OccluderBoundsDevicePx;
        if (!Contains(bounds, x, y))
            return false;
        if (string.Equals(shadow.ShapeKind, "rectangle", StringComparison.Ordinal))
            return true;

        var width = bounds.Width;
        var height = bounds.Height;
        if (width <= 0 || height <= 0)
            return false;
        var centerX = (bounds.Left + bounds.Right) / 2;
        var centerY = (bounds.Top + bounds.Bottom) / 2;
        if (string.Equals(shadow.ShapeKind, "circle", StringComparison.Ordinal))
        {
            var normalizedX = (x - centerX) / (width / 2);
            var normalizedY = (y - centerY) / (height / 2);
            return normalizedX * normalizedX + normalizedY * normalizedY <= 1;
        }

        // The contract carries local dp radii and a physical affine transform. The scene
        // catalog currently uses axis-aligned shadows; using the larger physical scale is a
        // conservative conversion for anisotropic scale too.
        var local = shadow.ShapeBoundsLocalDp;
        var scaleX = local.Width == 0 ? 1 : width / local.Width;
        var scaleY = local.Height == 0 ? 1 : height / local.Height;
        var radii = shadow.CornerRadiiDp;
        var topLeft = Math.Max(0, radii.TopLeft * Math.Max(scaleX, scaleY));
        var topRight = Math.Max(0, radii.TopRight * Math.Max(scaleX, scaleY));
        var bottomRight = Math.Max(0, radii.BottomRight * Math.Max(scaleX, scaleY));
        var bottomLeft = Math.Max(0, radii.BottomLeft * Math.Max(scaleX, scaleY));
        if (x < bounds.Left + topLeft && y < bounds.Top + topLeft)
            return InCorner(x, y, bounds.Left + topLeft, bounds.Top + topLeft, topLeft);
        if (x > bounds.Right - topRight && y < bounds.Top + topRight)
            return InCorner(x, y, bounds.Right - topRight, bounds.Top + topRight, topRight);
        if (x > bounds.Right - bottomRight && y > bounds.Bottom - bottomRight)
            return InCorner(x, y, bounds.Right - bottomRight, bounds.Bottom - bottomRight, bottomRight);
        if (x < bounds.Left + bottomLeft && y > bounds.Bottom - bottomLeft)
            return InCorner(x, y, bounds.Left + bottomLeft, bounds.Bottom - bottomLeft, bottomLeft);
        return true;
    }

    private static bool InCorner(float x, float y, float centerX, float centerY, float radius)
    {
        if (radius <= 0)
            return true;
        var dx = x - centerX;
        var dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool[] BuildImageEdgeMask(
        RenderedImage image,
        byte threshold,
        int dilationPx,
        bool[]? clip)
    {
        var seed = new bool[image.PixelCount];
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = y * image.Width + x;
                if (clip is not null && !clip[pixel])
                    continue;
                foreach (var (offsetX, offsetY) in Neighbors)
                {
                    var neighborX = x + offsetX;
                    var neighborY = y + offsetY;
                    if ((uint)neighborX >= (uint)image.Width || (uint)neighborY >= (uint)image.Height)
                        continue;
                    var neighbor = neighborY * image.Width + neighborX;
                    if (clip is not null && !clip[neighbor])
                        continue;
                    if (ExpectedPixelDelta(image, pixel, image, neighbor) > threshold)
                    {
                        seed[pixel] = true;
                        break;
                    }
                }
            }
        }

        var dilated = Dilate(seed, image.Width, image.Height, dilationPx);
        if (clip is null)
            return dilated;
        for (var pixel = 0; pixel < dilated.Length; pixel++)
            dilated[pixel] &= clip[pixel];
        return dilated;
    }

    private static bool[] BuildBinaryBoundary(bool[] source, int width, int height, int dilationPx)
    {
        var boundary = new bool[source.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = y * width + x;
                if (!source[pixel])
                    continue;
                foreach (var (offsetX, offsetY) in Neighbors)
                {
                    var neighborX = x + offsetX;
                    var neighborY = y + offsetY;
                    if ((uint)neighborX >= (uint)width || (uint)neighborY >= (uint)height
                        || !source[neighborY * width + neighborX])
                    {
                        boundary[pixel] = true;
                        break;
                    }
                }
            }
        }
        return Dilate(boundary, width, height, dilationPx);
    }

    private static bool[] Dilate(bool[] source, int width, int height, int radius)
    {
        if (radius <= 0)
            return source;
        var result = new bool[source.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!source[y * width + x])
                    continue;
                var top = Math.Max(0, y - radius);
                var bottom = Math.Min(height - 1, y + radius);
                var left = Math.Max(0, x - radius);
                var right = Math.Min(width - 1, x + radius);
                for (var neighborY = top; neighborY <= bottom; neighborY++)
                {
                    for (var neighborX = left; neighborX <= right; neighborX++)
                        result[neighborY * width + neighborX] = true;
                }
            }
        }
        return result;
    }

    private static void Intersect(bool[] target, bool[] clip)
    {
        for (var pixel = 0; pixel < target.Length; pixel++)
            target[pixel] &= clip[pixel];
    }

    private static void Except(bool[] target, bool[] excluded)
    {
        for (var pixel = 0; pixel < target.Length; pixel++)
            target[pixel] &= !excluded[pixel];
    }

    private static void Union(bool[] target, bool[] source)
    {
        for (var pixel = 0; pixel < target.Length; pixel++)
            target[pixel] |= source[pixel];
    }

    private static bool HasPixels(bool[] mask) => mask.Any(value => value);

    private static uint FindDominantPixel(RenderedImage image)
    {
        var counts = new Dictionary<uint, int>();
        var dominant = 0u;
        var maximum = 0;
        for (var pixel = 0; pixel < image.PixelCount; pixel++)
        {
            var offset = pixel * 4;
            var value = (uint)(image.Pixels[offset]
                               | image.Pixels[offset + 1] << 8
                               | image.Pixels[offset + 2] << 16
                               | image.Pixels[offset + 3] << 24);
            var count = counts.TryGetValue(value, out var existing) ? existing + 1 : 1;
            counts[value] = count;
            if (count > maximum)
            {
                dominant = value;
                maximum = count;
            }
        }
        return dominant;
    }

    private static byte VisualChannelDelta(RenderedImage expected, RenderedImage actual, int pixel) =>
        ExpectedPixelDelta(expected, pixel, actual, pixel);

    private static byte MaximumDelta(
        IReadOnlyList<int> pixels,
        RenderedImage expected,
        RenderedImage actual)
    {
        byte maximum = 0;
        foreach (var pixel in pixels)
            maximum = Math.Max(maximum, VisualChannelDelta(expected, actual, pixel));
        return maximum;
    }

    private static ParityPixelBounds? BoundsOf(IReadOnlyList<int> pixels, int width)
    {
        if (pixels.Count == 0)
            return null;
        var left = width;
        var top = int.MaxValue;
        var right = -1;
        var bottom = -1;
        foreach (var pixel in pixels)
        {
            var x = pixel % width;
            var y = pixel / width;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }
        return new ParityPixelBounds(left, top, right, bottom);
    }

    private static IReadOnlyList<ParityRasterPixelSample> SamplesOf(
        IReadOnlyList<int> pixels,
        RenderedImage expected,
        RenderedImage actual) => pixels
        .Take(8)
        .Select(pixel => new ParityRasterPixelSample(
            pixel % expected.Width,
            pixel / expected.Width,
            VisualChannelDelta(expected, actual, pixel),
            PixelRgba(expected, pixel),
            PixelRgba(actual, pixel)))
        .ToArray();

    private static string PixelRgba(RenderedImage image, int pixel)
    {
        var offset = pixel * 4;
        return $"#{image.Pixels[offset]:X2}{image.Pixels[offset + 1]:X2}{image.Pixels[offset + 2]:X2}{image.Pixels[offset + 3]:X2}";
    }

    private static byte ExpectedPixelDelta(RenderedImage left, int leftPixel, RenderedImage right, int rightPixel)
    {
        var leftOffset = leftPixel * 4;
        var rightOffset = rightPixel * 4;
        byte maximum = 0;
        for (var channel = 0; channel < 4; channel++)
        {
            var delta = (byte)Math.Abs(left.Pixels[leftOffset + channel] - right.Pixels[rightOffset + channel]);
            maximum = Math.Max(maximum, delta);
        }
        return maximum;
    }

    private static byte ExpectedPixelDelta(RenderedImage image, int pixel, uint packed)
    {
        var offset = pixel * 4;
        byte maximum = 0;
        for (var channel = 0; channel < 4; channel++)
        {
            var value = (byte)(packed >> (8 * channel));
            var delta = (byte)Math.Abs(image.Pixels[offset + channel] - value);
            maximum = Math.Max(maximum, delta);
        }
        return maximum;
    }

    private static bool Contains(ParityRect rect, float x, float y) =>
        x >= rect.Left && x < rect.Right && y >= rect.Top && y < rect.Bottom;

    private static readonly (int X, int Y)[] Neighbors =
    [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),            (1, 0),
        (-1, 1),  (0, 1),  (1, 1)
    ];

    private readonly record struct ObservedPixel(int Pixel, byte Delta);

    private sealed record RasterRegion(string Id, string Kind, bool[] Mask, ParityRasterRegionPolicy Policy)
    {
        public int EligiblePixels => Mask.Count(value => value);
    }

    private sealed class RegionAccumulator
    {
        private readonly RasterRegion _region;
        private readonly int _priority;
        private readonly BucketAccumulator[] _buckets;
        private readonly List<ObservedPixel> _unbudgeted = [];
        private IReadOnlyList<int> _rejectedPixelIndices = [];

        public RegionAccumulator(RasterRegion region, int priority, int width, int height)
        {
            _region = region;
            _priority = priority;
            _buckets = region.Policy.Budgets.Select(budget => new BucketAccumulator(budget, region.EligiblePixels, width, height)).ToArray();
        }

        public IReadOnlyList<int> RejectedPixelIndices => _rejectedPixelIndices;

        public void Record(byte delta, int pixel)
        {
            foreach (var bucket in _buckets)
            {
                if (delta <= bucket.Policy.MaxChannelDelta)
                {
                    bucket.Record(delta, pixel);
                    return;
                }
            }
            _unbudgeted.Add(new ObservedPixel(pixel, delta));
        }

        public ParityRasterRegionResult BuildReport(RenderedImage expected, RenderedImage actual)
        {
            var reports = _buckets.Select(bucket => bucket.BuildReport()).ToArray();
            _rejectedPixelIndices = _buckets
                .SelectMany(bucket => bucket.RejectedPixelIndices)
                .Concat(_unbudgeted.Select(item => item.Pixel))
                .Distinct()
                .Order()
                .ToArray();
            var reasons = reports
                .SelectMany(report => report.RejectionReasons)
                .Concat(_unbudgeted.Count == 0
                    ? []
                    : [ParityRasterRejectionReason.OutsideDeltaBudget])
                .Distinct()
                .ToArray();
            return new ParityRasterRegionResult(
                _region.Id,
                _region.Kind,
                _priority,
                _region.EligiblePixels,
                _buckets.Sum(bucket => bucket.ObservedPixels) + _unbudgeted.Count,
                _buckets.Sum(bucket => bucket.ObservedEnergy) + _unbudgeted.Sum(item => item.Delta),
                _unbudgeted.Count,
                _unbudgeted.Sum(item => item.Delta),
                reports,
                _rejectedPixelIndices.Count,
                MaximumDelta(_rejectedPixelIndices, expected, actual),
                reasons,
                BoundsOf(_rejectedPixelIndices, expected.Width),
                SamplesOf(_rejectedPixelIndices, expected, actual));
        }
    }

    private sealed class BucketAccumulator
    {
        private readonly int _width;
        private readonly int _height;

        public BucketAccumulator(ParityRasterDeltaBudget policy, int eligiblePixels, int width, int height)
        {
            Policy = policy;
            _width = width;
            _height = height;
            AllowedPixels = CalculateBudget(
                policy.MinimumPixelsPerOwner,
                policy.BasePixelsPerOwner,
                policy.PixelsPer10kEligiblePixels,
                eligiblePixels,
                policy.MaxPixelsPerOwner);
            AllowedEnergy = CalculateBudget(
                policy.MinimumEnergyPerOwner,
                policy.BaseEnergyPerOwner,
                policy.EnergyPer10kEligiblePixels,
                eligiblePixels,
                policy.MaxEnergyPerOwner);
        }

        public ParityRasterDeltaBudget Policy { get; }
        public int AllowedPixels { get; }
        public int AllowedEnergy { get; }
        public int ObservedPixels { get; private set; }
        public int ObservedEnergy { get; private set; }
        public byte MaximumObservedChannelDelta { get; private set; }
        private readonly List<ObservedPixel> _pixels = [];
        public IReadOnlyList<int> RejectedPixelIndices { get; private set; } = [];

        public void Record(byte delta, int pixel)
        {
            ObservedPixels++;
            ObservedEnergy += delta;
            MaximumObservedChannelDelta = Math.Max(MaximumObservedChannelDelta, delta);
            _pixels.Add(new ObservedPixel(pixel, delta));
        }

        public ParityRasterBudgetResult BuildReport()
        {
            var components = FindComponentSummary(_pixels, _width, _height, Policy);
            var quotaRejected = FindQuotaRejectedPixels(_pixels, AllowedPixels, AllowedEnergy);
            RejectedPixelIndices = quotaRejected
                .Concat(components.RejectedPixelIndices)
                .Distinct()
                .Order()
                .ToArray();
            var reasons = new List<ParityRasterRejectionReason>();
            if (ObservedPixels > AllowedPixels)
                reasons.Add(ParityRasterRejectionReason.PixelCountBudget);
            if (ObservedEnergy > AllowedEnergy)
                reasons.Add(ParityRasterRejectionReason.EnergyBudget);
            if (components.ExceededPixelArea)
                reasons.Add(ParityRasterRejectionReason.ConnectedComponentPixels);
            if (components.ExceededThickness)
                reasons.Add(ParityRasterRejectionReason.ConnectedComponentThickness);
            return new ParityRasterBudgetResult(
                Policy.MaxChannelDelta,
                ObservedPixels,
                AllowedPixels,
                ObservedEnergy,
                MaximumObservedChannelDelta,
                AllowedEnergy,
                RejectedPixelIndices.Count,
                RejectedPixelIndices.Count == 0
                    ? (byte)0
                    : _pixels.Where(item => RejectedPixelIndices.Contains(item.Pixel)).Max(item => item.Delta),
                components.LargestPixels,
                components.LargestThicknessPx,
                reasons,
                BoundsOf(RejectedPixelIndices, _width));
        }

        private static IReadOnlyList<int> FindQuotaRejectedPixels(
            IReadOnlyList<ObservedPixel> pixels,
            int allowedPixels,
            int allowedEnergy)
        {
            var remainingPixels = pixels.Count;
            var remainingEnergy = pixels.Sum(item => item.Delta);
            if (remainingPixels <= allowedPixels && remainingEnergy <= allowedEnergy)
                return [];

            var rejected = new List<int>();
            foreach (var item in pixels.OrderByDescending(item => item.Delta).ThenBy(item => item.Pixel))
            {
                if (remainingPixels <= allowedPixels && remainingEnergy <= allowedEnergy)
                    break;
                rejected.Add(item.Pixel);
                remainingPixels--;
                remainingEnergy -= item.Delta;
            }
            return rejected;
        }

        private static ComponentSummary FindComponentSummary(
            IReadOnlyList<ObservedPixel> pixels,
            int width,
            int height,
            ParityRasterDeltaBudget policy)
        {
            if (pixels.Count == 0)
                return new ComponentSummary(0, 0, [], false, false);

            var remaining = pixels.Select(item => item.Pixel).ToHashSet();
            var largestPixels = 0;
            var largestThickness = 0;
            var rejectedPixels = new List<int>();
            var exceededPixelArea = false;
            var exceededThickness = false;
            var queue = new Queue<int>();
            while (remaining.Count != 0)
            {
                var first = remaining.First();
                remaining.Remove(first);
                queue.Enqueue(first);
                var count = 0;
                var left = width;
                var right = -1;
                var top = height;
                var bottom = -1;
                var componentPixels = new List<int>();
                while (queue.Count != 0)
                {
                    var pixel = queue.Dequeue();
                    componentPixels.Add(pixel);
                    count++;
                    var x = pixel % width;
                    var y = pixel / width;
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                    foreach (var (offsetX, offsetY) in Neighbors)
                    {
                        var neighborX = x + offsetX;
                        var neighborY = y + offsetY;
                        if ((uint)neighborX >= (uint)width || (uint)neighborY >= (uint)height)
                            continue;
                        var neighbor = neighborY * width + neighborX;
                        if (remaining.Remove(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }

                var thickness = Math.Min(right - left + 1, bottom - top + 1);
                largestPixels = Math.Max(largestPixels, count);
                largestThickness = Math.Max(largestThickness, thickness);
                var areaExceeded = count > policy.MaxConnectedComponentPixels;
                var thicknessExceeded = thickness > policy.MaxConnectedComponentThicknessPx;
                if (areaExceeded || thicknessExceeded)
                    rejectedPixels.AddRange(componentPixels);
                exceededPixelArea |= areaExceeded;
                exceededThickness |= thicknessExceeded;
            }
            return new ComponentSummary(
                largestPixels,
                largestThickness,
                rejectedPixels,
                exceededPixelArea,
                exceededThickness);
        }

        private readonly record struct ComponentSummary(
            int LargestPixels,
            int LargestThicknessPx,
            IReadOnlyList<int> RejectedPixelIndices,
            bool ExceededPixelArea,
            bool ExceededThickness);

        private static int CalculateBudget(
            int minimum,
            int baseBudget,
            int per10kEligiblePixels,
            int eligiblePixels,
            int maximum)
        {
            var scaled = (int)((((long)eligiblePixels * per10kEligiblePixels) + 9_999) / 10_000);
            var calculated = checked(baseBudget + scaled);
            return Math.Min(maximum, Math.Max(minimum, calculated));
        }
    }
}

/// <summary>Semantic inputs eligible to receive raster allowances after structural comparison.</summary>
internal sealed record ParityRasterSemanticContext(
    IReadOnlyList<ParityNode> MatchedTextNodes,
    IReadOnlyList<ParityNode> MatchedShadowNodes)
{
    public static ParityRasterSemanticContext Empty { get; } = new([], []);
}

/// <summary>
/// A central, auditable policy. The coefficients are fixed in the profile; only the resulting
/// quota is dynamic, calculated from the matching expected region's mask size.
/// </summary>
public sealed record ParityRasterPolicy(
    string Algorithm,
    byte StrictChannelDelta,
    byte EdgeDetectionChannelDelta,
    byte ShadowSignalChannelDelta,
    float ShadowPaddingPerElevationPx,
    int MinimumShadowPaddingPx,
    ParityRasterRegionPolicy? TextInk = null,
    ParityRasterRegionPolicy? ShapeEdge = null,
    ParityRasterRegionPolicy? ShadowSupport = null,
    ParityRasterRegionPolicy? ShadowEdge = null,
    ParityRasterRegionPolicy? ShadowOccluder = null);

public sealed record ParityRasterRegionPolicy(
    int DilationPx,
    IReadOnlyList<ParityRasterDeltaBudget> Budgets);

public sealed record ParityRasterDeltaBudget(
    byte MaxChannelDelta,
    int MinimumPixelsPerOwner,
    int BasePixelsPerOwner,
    int PixelsPer10kEligiblePixels,
    int MaxPixelsPerOwner,
    int MinimumEnergyPerOwner,
    int BaseEnergyPerOwner,
    int EnergyPer10kEligiblePixels,
    int MaxEnergyPerOwner,
    int MaxConnectedComponentPixels,
    int MaxConnectedComponentThicknessPx);

public sealed record ParityRasterComparison(
    int RawDifferentPixels,
    byte MaximumRawChannelDelta,
    int IgnoredFloorPixels,
    int AllowedPixels,
    int RejectedPixels,
    byte MaximumRejectedChannelDelta,
    IReadOnlyList<ParityRasterRegionResult> Regions,
    IReadOnlyList<ParityRasterRejectionReason> RejectionReasons,
    ParityPixelBounds? RejectedBounds,
    IReadOnlyList<ParityRasterPixelSample> RejectedSamples,
    [property: JsonIgnore] IReadOnlyList<int> RejectedPixelIndices)
{
    public bool IsMatch => RejectedPixels == 0;
}

public sealed record ParityRasterRegionResult(
    string Id,
    string Kind,
    int Priority,
    int EligiblePixels,
    int ObservedPixels,
    int ObservedEnergy,
    int UnbudgetedPixels,
    int UnbudgetedEnergy,
    IReadOnlyList<ParityRasterBudgetResult> Budgets,
    int RejectedPixels,
    byte MaximumRejectedChannelDelta,
    IReadOnlyList<ParityRasterRejectionReason> RejectionReasons,
    ParityPixelBounds? RejectedBounds,
    IReadOnlyList<ParityRasterPixelSample> RejectedSamples);

public sealed record ParityRasterBudgetResult(
    byte MaxChannelDelta,
    int ObservedPixels,
    int AllowedPixels,
    int ObservedEnergy,
    byte MaximumObservedChannelDelta,
    int AllowedEnergy,
    int RejectedPixels,
    byte MaximumRejectedChannelDelta,
    int LargestConnectedComponentPixels,
    int LargestConnectedComponentThicknessPx,
    IReadOnlyList<ParityRasterRejectionReason> RejectionReasons,
    ParityPixelBounds? RejectedBounds);

public enum ParityRasterRejectionReason
{
    ImageSizeMismatch,
    OutsideSemanticMask,
    OutsideDeltaBudget,
    PixelCountBudget,
    EnergyBudget,
    ConnectedComponentPixels,
    ConnectedComponentThickness
}

/// <summary>Inclusive physical-pixel bounds of one rejected raster set.</summary>
public sealed record ParityPixelBounds(int Left, int Top, int Right, int Bottom);

/// <summary>A bounded same-coordinate sample from the official rejected-pixel set.</summary>
public sealed record ParityRasterPixelSample(
    int X,
    int Y,
    byte ChannelDelta,
    string ExpectedPremultipliedRgba,
    string ActualPremultipliedRgba);

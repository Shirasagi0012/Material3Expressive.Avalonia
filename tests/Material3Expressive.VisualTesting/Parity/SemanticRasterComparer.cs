using Material3Expressive.VisualTesting.Model;

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
            return new ParityRasterComparison(
                0,
                0,
                pixels,
                byte.MaxValue,
                [new ParityRasterRegionResult(
                    "image-size",
                    "strict",
                    0,
                    [],
                    pixels,
                    byte.MaxValue)]);
        }

        var regions = BuildRegions(expected, policy, semantic);
        var owners = AssignOwners(expected.PixelCount, regions);
        var accumulators = regions.Select(region => new RegionAccumulator(region, expected.Width, expected.Height)).ToArray();
        var ignoredFloor = 0;
        var outside = 0;
        byte outsideMaximum = 0;

        for (var pixel = 0; pixel < expected.PixelCount; pixel++)
        {
            var delta = VisualChannelDelta(expected, actual, pixel);
            if (delta == 0)
                continue;
            if (delta <= policy.StrictChannelDelta)
            {
                ignoredFloor++;
                continue;
            }

            var owner = owners[pixel];
            if (owner < 0)
            {
                outside++;
                outsideMaximum = Math.Max(outsideMaximum, delta);
                continue;
            }

            accumulators[owner].Record(delta, pixel);
        }

        var reports = accumulators.Select(item => item.BuildReport()).ToArray();
        var rejected = outside;
        var maximum = outsideMaximum;
        foreach (var report in reports)
        {
            rejected += report.RejectedPixels;
            maximum = Math.Max(maximum, report.MaximumRejectedChannelDelta);
        }

        var allowed = accumulators.Sum(item => item.AllowedPixels);
        return new ParityRasterComparison(ignoredFloor, allowed, rejected, maximum, reports);
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
        if (policy.ShadowEdge is not null || policy.ShadowSupport is not null)
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
                        Except(edge, occluder);
                    if (HasPixels(edge))
                        regions.Add(new RasterRegion($"shadow-edge:{node.Id}", "shadow-edge", edge, policy.ShadowEdge));
                }
                if (policy.ShadowSupport is not null)
                    shadowSupportRegions.Add(new RasterRegion($"shadow:{node.Id}", "shadow-support", support, policy.ShadowSupport));
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

        // Diffuse shadow support is deliberately the least-specific region. A pixel that also
        // belongs to a text, shadow-boundary, or generic shape edge consumes that stricter
        // region's budget instead.
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

    private sealed record RasterRegion(string Id, string Kind, bool[] Mask, ParityRasterRegionPolicy Policy)
    {
        public int EligiblePixels => Mask.Count(value => value);
    }

    private sealed class RegionAccumulator
    {
        private readonly RasterRegion _region;
        private readonly BucketAccumulator[] _buckets;
        private int _unbudgetedPixels;
        private byte _unbudgetedMaximum;

        public RegionAccumulator(RasterRegion region, int width, int height)
        {
            _region = region;
            _buckets = region.Policy.Budgets.Select(budget => new BucketAccumulator(budget, region.EligiblePixels, width, height)).ToArray();
        }

        public int AllowedPixels => _buckets.Sum(bucket => Math.Min(bucket.ObservedPixels, bucket.AllowedPixels));

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
            _unbudgetedPixels++;
            _unbudgetedMaximum = Math.Max(_unbudgetedMaximum, delta);
        }

        public ParityRasterRegionResult BuildReport()
        {
            var reports = _buckets.Select(bucket => bucket.BuildReport()).ToArray();
            var rejected = _unbudgetedPixels;
            var maximum = _unbudgetedMaximum;
            foreach (var bucket in reports)
            {
                rejected += bucket.RejectedPixels;
                maximum = Math.Max(maximum, bucket.MaximumRejectedChannelDelta);
            }
            return new ParityRasterRegionResult(_region.Id, _region.Kind, _region.EligiblePixels, reports, rejected, maximum);
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
        private readonly List<int> _pixels = [];

        public void Record(byte delta, int pixel)
        {
            ObservedPixels++;
            ObservedEnergy += delta;
            MaximumObservedChannelDelta = Math.Max(MaximumObservedChannelDelta, delta);
            _pixels.Add(pixel);
        }

        public ParityRasterBudgetResult BuildReport()
        {
            var countOverage = Math.Max(0, ObservedPixels - AllowedPixels);
            var energyOverage = Math.Max(0, ObservedEnergy - AllowedEnergy);
            // Energy is measured in channel-delta units. Convert an energy overage into a
            // conservative pixel count for the legacy summary while retaining the exact energy
            // figures in the report.
            var energyPixels = energyOverage == 0
                ? 0
                : Math.Max(1, (int)Math.Ceiling(energyOverage / (double)Math.Max(1, (int)Policy.MaxChannelDelta)));
            var components = FindComponentSummary(_pixels, _width, _height, Policy);
            var rejected = Math.Max(Math.Max(countOverage, energyPixels), components.RejectedPixels);
            return new ParityRasterBudgetResult(
                Policy.MaxChannelDelta,
                ObservedPixels,
                AllowedPixels,
                ObservedEnergy,
                AllowedEnergy,
                rejected,
                rejected == 0 ? (byte)0 : MaximumObservedChannelDelta,
                components.LargestPixels,
                components.LargestThicknessPx);
        }

        private static ComponentSummary FindComponentSummary(
            IReadOnlyList<int> pixels,
            int width,
            int height,
            ParityRasterDeltaBudget policy)
        {
            if (pixels.Count == 0)
                return default;

            var remaining = pixels.ToHashSet();
            var largestPixels = 0;
            var largestThickness = 0;
            var rejectedPixels = 0;
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
                while (queue.Count != 0)
                {
                    var pixel = queue.Dequeue();
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
                if (count > policy.MaxConnectedComponentPixels || thickness > policy.MaxConnectedComponentThicknessPx)
                    rejectedPixels += count;
            }
            return new ComponentSummary(largestPixels, largestThickness, rejectedPixels);
        }

        private readonly record struct ComponentSummary(
            int LargestPixels,
            int LargestThicknessPx,
            int RejectedPixels);

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
    ParityRasterRegionPolicy? ShadowEdge = null);

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
    int IgnoredFloorPixels,
    int AllowedPixels,
    int RejectedPixels,
    byte MaximumRejectedChannelDelta,
    IReadOnlyList<ParityRasterRegionResult> Regions)
{
    public bool IsMatch => RejectedPixels == 0;
}

public sealed record ParityRasterRegionResult(
    string Id,
    string Kind,
    int EligiblePixels,
    IReadOnlyList<ParityRasterBudgetResult> Budgets,
    int RejectedPixels,
    byte MaximumRejectedChannelDelta);

public sealed record ParityRasterBudgetResult(
    byte MaxChannelDelta,
    int ObservedPixels,
    int AllowedPixels,
    int ObservedEnergy,
    int AllowedEnergy,
    int RejectedPixels,
    byte MaximumRejectedChannelDelta,
    int LargestConnectedComponentPixels,
    int LargestConnectedComponentThicknessPx);

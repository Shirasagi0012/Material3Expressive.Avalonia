using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Compose;

/// <summary>
/// Diagnostic cross-renderer comparison. It deliberately has no pass threshold: the first parity
/// phase records geometry, perceptual colour, edge and text-region evidence for human review.
/// </summary>
public sealed class ComposeParityComparer
{
    private const int AlignmentRadius = 4;
    private const int EdgeTolerance = 1;
    private const double EdgeThreshold = 72;
    private const double ColorEdgeThreshold = 4;
    private const double DominantForegroundThreshold = 0.25;

    public ComposeParityMetrics Compare(
        RenderedImage reference,
        RenderedImage avalonia,
        IReadOnlyList<ComposeParityRegion>? regions = null)
    {
        Validate(reference);
        Validate(avalonia);
        if (reference.Width != avalonia.Width || reference.Height != avalonia.Height)
        {
            return new ComposeParityMetrics(
                false,
                reference.Width,
                reference.Height,
                avalonia.Width,
                avalonia.Height,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                null,
                null,
                [],
                $"Dimensions differ: Compose {reference.Width}x{reference.Height}, " +
                $"Avalonia {avalonia.Width}x{avalonia.Height}.");
        }

        var referenceBackground = EstimateCornerColor(reference);
        var avaloniaBackground = EstimateCornerColor(avalonia);
        var referenceForeground = FindForegroundBounds(reference, referenceBackground);
        var avaloniaForeground = FindForegroundBounds(avalonia, avaloniaBackground);
        var referenceEdges = DetectEdges(reference);
        var avaloniaEdges = DetectEdges(avalonia);
        var alignment = FindAlignment(referenceForeground, avaloniaForeground);
        var rawRmse = RootMeanSquareError(reference, avalonia, 0, 0);
        var alignedRmse = RootMeanSquareError(
            reference,
            avalonia,
            alignment.X,
            alignment.Y);
        // A shadow or asymmetric size difference can move the foreground bounding-box centre
        // without translating the component. Never report that as alignment when applying the
        // offset makes the actual pixel correspondence worse.
        if ((alignment.X != 0 || alignment.Y != 0) && alignedRmse >= rawRmse)
        {
            alignment = (0, 0);
            alignedRmse = rawRmse;
        }
        var color = CompareColor(reference, avalonia, alignment.X, alignment.Y, null);
        var edge = CompareEdges(
            referenceEdges,
            avaloniaEdges,
            reference.Width,
            reference.Height,
            alignment.X,
            alignment.Y,
            null);

        var foregroundColor = CompareForegroundDominant(
            reference,
            avalonia,
            referenceBackground,
            avaloniaBackground);

        var regionMetrics = (regions ?? [])
            .Select(region => CompareRegion(
                reference,
                avalonia,
                referenceEdges,
                avaloniaEdges,
                alignment.X,
                alignment.Y,
                region))
            .ToArray();

        var summary =
            $"alignment ({alignment.X:+0;-0;0},{alignment.Y:+0;-0;0})px; " +
            $"edge F1 {edge.F1:F4}; mean ΔE76 {color.MeanDeltaE:F3}; " +
            $"p95 ΔE76 {color.Percentile95DeltaE:F3}; " +
            $"dominant foreground ΔE76 {foregroundColor:F3}.";

        return new ComposeParityMetrics(
            true,
            reference.Width,
            reference.Height,
            avalonia.Width,
            avalonia.Height,
            alignment.X,
            alignment.Y,
            rawRmse,
            alignedRmse,
            color.MeanAbsoluteChannelError,
            color.MeanDeltaE,
            color.Percentile95DeltaE,
            color.MaximumDeltaE,
            DeltaE76(referenceBackground, avaloniaBackground),
            foregroundColor,
            edge.Precision,
            edge.Recall,
            edge.F1,
            referenceForeground,
            avaloniaForeground,
            regionMetrics,
            summary);
    }

    public RenderedImage CreateDeltaEHeatmap(
        RenderedImage reference,
        RenderedImage avalonia,
        ComposeParityMetrics metrics)
    {
        if (!metrics.DimensionsMatch)
            return avalonia;

        var pixels = new byte[reference.Pixels.Length];
        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                var offset = (y * reference.Width + x) * 4;
                if (!TryPixel(avalonia, x + metrics.AlignmentX, y + metrics.AlignmentY, out var a))
                {
                    pixels[offset] = 255;
                    pixels[offset + 3] = 255;
                    continue;
                }

                var delta = DeltaE76(ReadLab(reference, x, y), a.Lab);
                var value = Math.Clamp(delta / 20d, 0, 1);
                pixels[offset] = ToByte(255 * value);
                pixels[offset + 1] = ToByte(220 * (1 - Math.Abs(value - 0.5) * 2));
                pixels[offset + 2] = ToByte(160 * (1 - value));
                pixels[offset + 3] = 255;
            }
        }

        return new RenderedImage(reference.Width, reference.Height, pixels);
    }

    public RenderedImage CreateAlignedOverlay(
        RenderedImage reference,
        RenderedImage avalonia,
        ComposeParityMetrics metrics)
    {
        if (!metrics.DimensionsMatch)
            return avalonia;

        var pixels = new byte[reference.Pixels.Length];
        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                var offset = (y * reference.Width + x) * 4;
                if (!TryPixel(avalonia, x + metrics.AlignmentX, y + metrics.AlignmentY, out var a))
                {
                    Array.Copy(reference.Pixels, offset, pixels, offset, 4);
                    continue;
                }

                pixels[offset] = (byte)((reference.Pixels[offset] + a.Red) / 2);
                pixels[offset + 1] = (byte)((reference.Pixels[offset + 1] + a.Green) / 2);
                pixels[offset + 2] = (byte)((reference.Pixels[offset + 2] + a.Blue) / 2);
                pixels[offset + 3] = 255;
            }
        }

        return new RenderedImage(reference.Width, reference.Height, pixels);
    }

    public RenderedImage CreateEdgeOverlay(
        RenderedImage reference,
        RenderedImage avalonia,
        ComposeParityMetrics metrics)
    {
        if (!metrics.DimensionsMatch)
            return avalonia;

        var referenceEdges = DetectEdges(reference);
        var avaloniaEdges = DetectEdges(avalonia);
        var pixels = new byte[reference.Pixels.Length];
        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                var pixel = y * reference.Width + x;
                var offset = pixel * 4;
                var referenceEdge = referenceEdges[pixel];
                var avaloniaEdge = TryMask(
                    avaloniaEdges,
                    reference.Width,
                    reference.Height,
                    x + metrics.AlignmentX,
                    y + metrics.AlignmentY);

                if (referenceEdge && avaloniaEdge)
                {
                    pixels[offset] = 255;
                    pixels[offset + 1] = 255;
                    pixels[offset + 2] = 255;
                }
                else if (referenceEdge)
                {
                    pixels[offset] = 255;
                    pixels[offset + 2] = 200;
                }
                else if (avaloniaEdge)
                {
                    pixels[offset + 1] = 230;
                    pixels[offset + 2] = 255;
                }
                else
                {
                    var luminance = (byte)(
                        (reference.Pixels[offset] * 54
                         + reference.Pixels[offset + 1] * 183
                         + reference.Pixels[offset + 2] * 19) >> 8);
                    var faded = (byte)(luminance / 5);
                    pixels[offset] = faded;
                    pixels[offset + 1] = faded;
                    pixels[offset + 2] = faded;
                }

                pixels[offset + 3] = 255;
            }
        }

        return new RenderedImage(reference.Width, reference.Height, pixels);
    }

    private static ComposeParityRegionMetrics CompareRegion(
        RenderedImage reference,
        RenderedImage avalonia,
        bool[] referenceEdges,
        bool[] avaloniaEdges,
        int alignmentX,
        int alignmentY,
        ComposeParityRegion region)
    {
        var bounds = new PixelBounds(region.X, region.Y, region.Width, region.Height);
        var color = CompareColor(reference, avalonia, alignmentX, alignmentY, bounds);
        var edge = CompareEdges(
            referenceEdges,
            avaloniaEdges,
            reference.Width,
            reference.Height,
            alignmentX,
            alignmentY,
            bounds);
        var referenceInk = FindInkBounds(reference, bounds, region.Kind);
        var avaloniaInk = FindInkBounds(avalonia, bounds, region.Kind);
        int? inkBottomDelta = referenceInk is not null && avaloniaInk is not null
            ? avaloniaInk.Value.Bottom - referenceInk.Value.Bottom
            : null;

        return new ComposeParityRegionMetrics(
            region.Id,
            region.Kind,
            bounds,
            color.MeanDeltaE,
            color.Percentile95DeltaE,
            color.AverageColorDeltaE,
            edge.F1,
            referenceInk,
            avaloniaInk,
            inkBottomDelta);
    }

    private static (int X, int Y) FindAlignment(
        PixelBounds? referenceForeground,
        PixelBounds? avaloniaForeground)
    {
        if (referenceForeground is null || avaloniaForeground is null)
            return (0, 0);

        // These parity scenes contain one centered component. Aligning by foreground centre
        // identifies a true translation while preserving size, shadow and glyph-shape differences.
        // Maximising edge correlation instead can mistake a narrower shape or a text baseline
        // difference for an arbitrary whole-component shift.
        var x = (int)Math.Round(
            CenterX(avaloniaForeground.Value) - CenterX(referenceForeground.Value),
            MidpointRounding.ToEven);
        var y = (int)Math.Round(
            CenterY(avaloniaForeground.Value) - CenterY(referenceForeground.Value),
            MidpointRounding.ToEven);
        return (
            Math.Clamp(x, -AlignmentRadius, AlignmentRadius),
            Math.Clamp(y, -AlignmentRadius, AlignmentRadius));

        static double CenterX(PixelBounds bounds) => bounds.X + (bounds.Width - 1) / 2d;

        static double CenterY(PixelBounds bounds) => bounds.Y + (bounds.Height - 1) / 2d;
    }

    private static EdgeMetrics CompareEdges(
        bool[] referenceEdges,
        bool[] avaloniaEdges,
        int width,
        int height,
        int alignmentX,
        int alignmentY,
        PixelBounds? region,
        int tolerance = EdgeTolerance)
    {
        var bounds = region ?? new PixelBounds(0, 0, width, height);
        var referenceCount = 0;
        var referenceMatched = 0;
        var avaloniaCount = 0;
        var avaloniaMatched = 0;

        for (var y = bounds.Y; y < bounds.Bottom; y++)
        {
            for (var x = bounds.X; x < bounds.Right; x++)
            {
                if (TryMask(referenceEdges, width, height, x, y))
                {
                    referenceCount++;
                    if (HasEdgeNear(
                            avaloniaEdges,
                            width,
                            height,
                            x + alignmentX,
                            y + alignmentY,
                            tolerance))
                    {
                        referenceMatched++;
                    }
                }

                var actualX = x + alignmentX;
                var actualY = y + alignmentY;
                if (TryMask(avaloniaEdges, width, height, actualX, actualY))
                {
                    avaloniaCount++;
                    if (HasEdgeNear(
                            referenceEdges,
                            width,
                            height,
                            x,
                            y,
                            tolerance))
                    {
                        avaloniaMatched++;
                    }
                }
            }
        }

        var precision = avaloniaCount == 0
            ? referenceCount == 0 ? 1 : 0
            : avaloniaMatched / (double)avaloniaCount;
        var recall = referenceCount == 0
            ? avaloniaCount == 0 ? 1 : 0
            : referenceMatched / (double)referenceCount;
        var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);
        return new EdgeMetrics(precision, recall, f1);
    }

    private static ColorMetrics CompareColor(
        RenderedImage reference,
        RenderedImage avalonia,
        int alignmentX,
        int alignmentY,
        PixelBounds? region)
    {
        var bounds = region ?? new PixelBounds(0, 0, reference.Width, reference.Height);
        var deltaE = new List<double>(bounds.Width * bounds.Height);
        double absoluteChannelError = 0;
        var channels = 0;
        var referenceAverage = new LabAccumulator();
        var avaloniaAverage = new LabAccumulator();

        for (var y = bounds.Y; y < bounds.Bottom; y++)
        {
            for (var x = bounds.X; x < bounds.Right; x++)
            {
                if (!TryPixel(avalonia, x + alignmentX, y + alignmentY, out var actual))
                    continue;

                var expected = ReadPixel(reference, x, y);
                absoluteChannelError += Math.Abs(expected.Red - actual.Red);
                absoluteChannelError += Math.Abs(expected.Green - actual.Green);
                absoluteChannelError += Math.Abs(expected.Blue - actual.Blue);
                absoluteChannelError += Math.Abs(expected.Alpha - actual.Alpha);
                channels += 4;
                deltaE.Add(DeltaE76(expected.Lab, actual.Lab));
                referenceAverage.Add(expected.Lab);
                avaloniaAverage.Add(actual.Lab);
            }
        }

        if (deltaE.Count == 0)
            return new ColorMetrics(0, 0, 0, 0, 0);

        deltaE.Sort();
        return new ColorMetrics(
            absoluteChannelError / channels,
            deltaE.Average(),
            Percentile(deltaE, 0.95),
            deltaE[^1],
            DeltaE76(referenceAverage.Average, avaloniaAverage.Average));
    }

    private static double RootMeanSquareError(
        RenderedImage reference,
        RenderedImage avalonia,
        int alignmentX,
        int alignmentY)
    {
        double squared = 0;
        long channels = 0;
        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                if (!TryPixel(avalonia, x + alignmentX, y + alignmentY, out var actual))
                    continue;

                var offset = (y * reference.Width + x) * 4;
                Add(reference.Pixels[offset], actual.Red);
                Add(reference.Pixels[offset + 1], actual.Green);
                Add(reference.Pixels[offset + 2], actual.Blue);
                Add(reference.Pixels[offset + 3], actual.Alpha);
            }
        }

        return channels == 0 ? 0 : Math.Sqrt(squared / channels);

        void Add(byte expected, byte actual)
        {
            var delta = expected - actual;
            squared += delta * delta;
            channels++;
        }
    }

    private static bool[] DetectEdges(RenderedImage image)
    {
        var luma = new double[image.PixelCount];
        var lab = new Lab[image.PixelCount];
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = ReadPixel(image, x, y);
                var index = y * image.Width + x;
                luma[index] =
                    pixel.Red * 0.2126 + pixel.Green * 0.7152 + pixel.Blue * 0.0722;
                lab[index] = pixel.Lab;
            }
        }

        var edges = new bool[image.PixelCount];
        for (var y = 1; y < image.Height - 1; y++)
        {
            for (var x = 1; x < image.Width - 1; x++)
            {
                var top = (y - 1) * image.Width;
                var middle = y * image.Width;
                var bottom = (y + 1) * image.Width;
                var gx =
                    -luma[top + x - 1] + luma[top + x + 1]
                    - 2 * luma[middle + x - 1] + 2 * luma[middle + x + 1]
                    - luma[bottom + x - 1] + luma[bottom + x + 1];
                var gy =
                    -luma[top + x - 1] - 2 * luma[top + x] - luma[top + x + 1]
                    + luma[bottom + x - 1] + 2 * luma[bottom + x] + luma[bottom + x + 1];
                var horizontalColor = DeltaE76(lab[middle + x - 1], lab[middle + x + 1]);
                var verticalColor = DeltaE76(lab[top + x], lab[bottom + x]);
                edges[middle + x] =
                    Math.Sqrt(gx * gx + gy * gy) >= EdgeThreshold
                    || Math.Max(horizontalColor, verticalColor) >= ColorEdgeThreshold;
            }
        }

        return edges;
    }

    private static PixelBounds? FindForegroundBounds(RenderedImage image, Lab background)
    {
        var mask = new bool[image.PixelCount];
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
                mask[y * image.Width + x] = DeltaE76(ReadLab(image, x, y), background) >= 2.5;
        }

        return BoundsOf(mask, image.Width, image.Height, new PixelBounds(0, 0, image.Width, image.Height));
    }

    private static PixelBounds? FindInkBounds(
        RenderedImage image,
        PixelBounds region,
        ComposeParityRegionKind kind)
    {
        var backgrounds = kind is ComposeParityRegionKind.Text or ComposeParityRegionKind.Icon
            ? EstimateDominantBackgrounds(image, region)
            : [EstimateCornerColor(image, region)];
        var mask = new bool[region.Width * region.Height];
        for (var y = 0; y < region.Height; y++)
        {
            for (var x = 0; x < region.Width; x++)
            {
                var imageX = region.X + x;
                var imageY = region.Y + y;
                if (imageX >= 0 && imageY >= 0 && imageX < image.Width && imageY < image.Height)
                {
                    var color = ReadLab(image, imageX, imageY);
                    mask[y * region.Width + x] =
                        backgrounds.All(background => DeltaE76(color, background) >= 5);
                }
            }
        }

        var local = BoundsOf(mask, region.Width, region.Height, new PixelBounds(0, 0, region.Width, region.Height));
        return local is null
            ? null
            : new PixelBounds(
                region.X + local.Value.X,
                region.Y + local.Value.Y,
                local.Value.Width,
                local.Value.Height);
    }

    private static PixelBounds? BoundsOf(
        bool[] mask,
        int width,
        int height,
        PixelBounds search)
    {
        var left = search.Right;
        var top = search.Bottom;
        var right = -1;
        var bottom = -1;
        for (var y = search.Y; y < search.Bottom && y < height; y++)
        {
            for (var x = search.X; x < search.Right && x < width; x++)
            {
                if (!mask[y * width + x])
                    continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left
            ? null
            : new PixelBounds(left, top, right - left + 1, bottom - top + 1);
    }

    private static Lab EstimateCornerColor(RenderedImage image)
    {
        var colors = new List<Lab>(36);
        const int sample = 3;
        for (var y = 0; y < sample; y++)
        {
            for (var x = 0; x < sample; x++)
            {
                colors.Add(ReadLab(image, x, y));
                colors.Add(ReadLab(image, image.Width - 1 - x, y));
                colors.Add(ReadLab(image, x, image.Height - 1 - y));
                colors.Add(ReadLab(image, image.Width - 1 - x, image.Height - 1 - y));
            }
        }

        return Average(colors);
    }

    private static Lab EstimateCornerColor(RenderedImage image, PixelBounds region)
    {
        var colors = new List<Lab>(4);
        Add(region.X, region.Y);
        Add(region.Right - 1, region.Y);
        Add(region.X, region.Bottom - 1);
        Add(region.Right - 1, region.Bottom - 1);
        return Average(colors);

        void Add(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < image.Width && y < image.Height)
                colors.Add(ReadLab(image, x, y));
        }
    }

    private static IReadOnlyList<Lab> EstimateDominantBackgrounds(
        RenderedImage image,
        PixelBounds region)
    {
        var counts = new Dictionary<int, (int Count, Lab Lab)>();
        for (var x = region.X; x < region.Right; x++)
        {
            Add(x, region.Y);
            Add(x, region.Bottom - 1);
        }
        for (var y = region.Y + 1; y < region.Bottom - 1; y++)
        {
            Add(region.X, y);
            Add(region.Right - 1, y);
        }
        var dominant = counts.Values
            .Where(item => item.Count >= 2)
            .OrderByDescending(item => item.Count)
            .Take(3)
            .Select(item => item.Lab)
            .ToArray();
        return dominant.Length == 0
            ? [counts.Values.MaxBy(item => item.Count).Lab]
            : dominant;

        void Add(int x, int y)
        {
            var pixel = ReadPixel(image, x, y);
            var key = pixel.Red << 16 | pixel.Green << 8 | pixel.Blue;
            if (counts.TryGetValue(key, out var current))
                counts[key] = (current.Count + 1, current.Lab);
            else
                counts[key] = (1, pixel.Lab);
        }
    }

    private static double CompareForegroundDominant(
        RenderedImage reference,
        RenderedImage avalonia,
        Lab referenceBackground,
        Lab avaloniaBackground)
    {
        var expected = FindDominantForeground(reference, referenceBackground);
        var actual = FindDominantForeground(avalonia, avaloniaBackground);
        return expected is null || actual is null ? 0 : DeltaE76(expected.Value, actual.Value);
    }

    private static Lab? FindDominantForeground(RenderedImage image, Lab background)
    {
        var counts = new Dictionary<int, (int Count, Lab Lab)>();
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = ReadPixel(image, x, y);
                if (DeltaE76(pixel.Lab, background) < DominantForegroundThreshold)
                    continue;

                var key = pixel.Red << 16 | pixel.Green << 8 | pixel.Blue;
                if (counts.TryGetValue(key, out var current))
                    counts[key] = (current.Count + 1, current.Lab);
                else
                    counts[key] = (1, pixel.Lab);
            }
        }

        return counts.Count == 0 ? null : counts.Values.MaxBy(item => item.Count).Lab;
    }

    private static bool HasEdgeNear(
        bool[] mask,
        int width,
        int height,
        int x,
        int y,
        int radius)
    {
        for (var offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (var offsetX = -radius; offsetX <= radius; offsetX++)
            {
                if (TryMask(mask, width, height, x + offsetX, y + offsetY))
                    return true;
            }
        }

        return false;
    }

    private static bool TryMask(bool[] mask, int width, int height, int x, int y) =>
        x >= 0 && y >= 0 && x < width && y < height && mask[y * width + x];

    private static bool TryPixel(RenderedImage image, int x, int y, out Pixel pixel)
    {
        if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
        {
            pixel = default;
            return false;
        }

        pixel = ReadPixel(image, x, y);
        return true;
    }

    private static Pixel ReadPixel(RenderedImage image, int x, int y)
    {
        var offset = (y * image.Width + x) * 4;
        var alpha = image.Pixels[offset + 3];
        var red = Unpremultiply(image.Pixels[offset], alpha);
        var green = Unpremultiply(image.Pixels[offset + 1], alpha);
        var blue = Unpremultiply(image.Pixels[offset + 2], alpha);
        return new Pixel(red, green, blue, alpha, ToLab(red, green, blue));
    }

    private static Lab ReadLab(RenderedImage image, int x, int y) => ReadPixel(image, x, y).Lab;

    private static byte Unpremultiply(byte value, byte alpha) =>
        alpha is 0 or 255 ? value : ToByte(value * 255d / alpha);

    private static Lab ToLab(byte red, byte green, byte blue)
    {
        var r = Linear(red / 255d);
        var g = Linear(green / 255d);
        var b = Linear(blue / 255d);
        var x = (r * 0.4124564 + g * 0.3575761 + b * 0.1804375) / 0.95047;
        var y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
        var z = (r * 0.0193339 + g * 0.1191920 + b * 0.9503041) / 1.08883;
        var fx = Pivot(x);
        var fy = Pivot(y);
        var fz = Pivot(z);
        return new Lab(116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz));

        static double Linear(double value) =>
            value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);

        static double Pivot(double value) =>
            value > 216d / 24389d
                ? Math.Cbrt(value)
                : 841d / 108d * value + 4d / 29d;
    }

    private static Lab Average(IEnumerable<Lab> colors)
    {
        var accumulator = new LabAccumulator();
        foreach (var color in colors)
            accumulator.Add(color);
        return accumulator.Average;
    }

    private static double DeltaE76(Lab left, Lab right)
    {
        var dl = left.L - right.L;
        var da = left.A - right.A;
        var db = left.B - right.B;
        return Math.Sqrt(dl * dl + da * da + db * db);
    }

    private static double Percentile(IReadOnlyList<double> sorted, double percentile)
    {
        var index = (sorted.Count - 1) * percentile;
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
            return sorted[lower];
        var fraction = index - lower;
        return sorted[lower] * (1 - fraction) + sorted[upper] * fraction;
    }

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), byte.MinValue, byte.MaxValue);

    private static void Validate(RenderedImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width <= 0
            || image.Height <= 0
            || image.Pixels.Length != image.PixelCount * 4)
        {
            throw new InvalidDataException("Parity images must be non-empty packed RGBA.");
        }
    }

    private readonly record struct Pixel(byte Red, byte Green, byte Blue, byte Alpha, Lab Lab);

    private readonly record struct Lab(double L, double A, double B);

    private readonly record struct EdgeMetrics(double Precision, double Recall, double F1);

    private readonly record struct ColorMetrics(
        double MeanAbsoluteChannelError,
        double MeanDeltaE,
        double Percentile95DeltaE,
        double MaximumDeltaE,
        double AverageColorDeltaE);

    private sealed class LabAccumulator
    {
        private double _l;
        private double _a;
        private double _b;

        public int Count { get; private set; }

        public Lab Average => Count == 0 ? default : new Lab(_l / Count, _a / Count, _b / Count);

        public void Add(Lab color)
        {
            _l += color.L;
            _a += color.A;
            _b += color.B;
            Count++;
        }
    }
}

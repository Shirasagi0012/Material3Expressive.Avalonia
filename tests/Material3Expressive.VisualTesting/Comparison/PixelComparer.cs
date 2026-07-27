using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Comparison;

/// <summary>Compares tightly packed premultiplied RGBA images without platform image APIs.</summary>
public sealed class PixelComparer
{
    public PixelComparisonResult Compare(
        RenderedImage expected,
        RenderedImage actual,
        ComparisonProfile profile)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(profile);

        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return PixelComparisonResult.DimensionMismatch(expected, actual);

        if (expected.Pixels.Length != expected.PixelCount * 4
            || actual.Pixels.Length != actual.PixelCount * 4)
        {
            throw new InvalidDataException("RGBA images must contain exactly four bytes per pixel.");
        }

        var mask = new bool[expected.PixelCount];
        long changedPixels = 0;
        long squaredError = 0;
        byte maximumDelta = 0;

        for (var pixel = 0; pixel < expected.PixelCount; pixel++)
        {
            var offset = pixel * 4;
            byte pixelDelta = 0;

            for (var channel = 0; channel < 4; channel++)
            {
                var delta = Math.Abs(expected.Pixels[offset + channel] - actual.Pixels[offset + channel]);
                squaredError += (long)delta * delta;
                if (delta > pixelDelta)
                    pixelDelta = (byte)delta;
                if (delta > maximumDelta)
                    maximumDelta = (byte)delta;
            }

            if (pixelDelta <= profile.IgnoredChannelDelta)
                continue;

            mask[pixel] = true;
            changedPixels++;
        }

        var changedRatio = changedPixels / (double)expected.PixelCount;
        var rmse = Math.Sqrt(squaredError / (double)(expected.PixelCount * 4));
        var largestComponent = FindLargestComponent(mask, expected.Width, expected.Height);
        var passed = changedRatio <= profile.MaximumChangedPixelRatio
                     && rmse <= profile.MaximumRootMeanSquareError
                     && largestComponent <= profile.MaximumChangedComponentSize;

        var summary = passed
            ? $"{profile.Name}: images match."
            : $"{profile.Name}: {changedPixels} pixels changed ({changedRatio:P4}), " +
              $"max channel delta {maximumDelta}, RMSE {rmse:F4}, " +
              $"largest component {largestComponent}px.";

        return new PixelComparisonResult(
            true,
            expected.Width,
            expected.Height,
            actual.Width,
            actual.Height,
            changedPixels,
            changedRatio,
            maximumDelta,
            rmse,
            largestComponent,
            passed,
            summary,
            mask);
    }

    public RenderedImage CreateDiff(
        RenderedImage expected,
        RenderedImage actual,
        PixelComparisonResult result)
    {
        if (!result.DimensionsMatch)
            return actual;

        var pixels = new byte[expected.Pixels.Length];
        for (var pixel = 0; pixel < expected.PixelCount; pixel++)
        {
            var offset = pixel * 4;
            if (result.ChangedMask[pixel])
            {
                var delta = 0;
                for (var channel = 0; channel < 4; channel++)
                {
                    delta = Math.Max(
                        delta,
                        Math.Abs(expected.Pixels[offset + channel] - actual.Pixels[offset + channel]));
                }

                pixels[offset] = byte.MaxValue;
                pixels[offset + 1] = (byte)Math.Max(0, 96 - delta);
                pixels[offset + 2] = (byte)Math.Max(0, 96 - delta);
                pixels[offset + 3] = byte.MaxValue;
            }
            else
            {
                var luminance = (byte)(
                    (expected.Pixels[offset] * 54
                     + expected.Pixels[offset + 1] * 183
                     + expected.Pixels[offset + 2] * 19) >> 8);
                var faded = (byte)((luminance + 255 * 3) / 4);
                pixels[offset] = faded;
                pixels[offset + 1] = faded;
                pixels[offset + 2] = faded;
                pixels[offset + 3] = byte.MaxValue;
            }
        }

        return new RenderedImage(expected.Width, expected.Height, pixels);
    }

    public RenderedImage CreateOverlay(RenderedImage expected, RenderedImage actual)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return actual;

        var pixels = new byte[expected.Pixels.Length];
        for (var index = 0; index < pixels.Length; index++)
            pixels[index] = (byte)((expected.Pixels[index] + actual.Pixels[index]) / 2);

        return new RenderedImage(expected.Width, expected.Height, pixels);
    }

    private static int FindLargestComponent(bool[] mask, int width, int height)
    {
        if (mask.Length == 0)
            return 0;

        var visited = new bool[mask.Length];
        var queue = new Queue<int>();
        var largest = 0;

        for (var start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || visited[start])
                continue;

            visited[start] = true;
            queue.Enqueue(start);
            var size = 0;

            while (queue.TryDequeue(out var current))
            {
                size++;
                var x = current % width;
                var y = current / width;

                Visit(current - 1, x > 0);
                Visit(current + 1, x + 1 < width);
                Visit(current - width, y > 0);
                Visit(current + width, y + 1 < height);
            }

            largest = Math.Max(largest, size);
        }

        return largest;

        void Visit(int index, bool inBounds)
        {
            if (!inBounds || !mask[index] || visited[index])
                return;

            visited[index] = true;
            queue.Enqueue(index);
        }
    }
}

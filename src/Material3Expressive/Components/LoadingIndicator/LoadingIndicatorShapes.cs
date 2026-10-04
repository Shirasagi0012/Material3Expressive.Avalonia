/*
 * Translated from AndroidX LoadingIndicator.kt (Copyright 2024 The Android Open Source
 * Project; Apache-2.0). Fixed baseline: docs/upstream-baseline.json.
 */
using Avalonia;
using MaterialShapes;
using SkiaSharp;

namespace Material3Expressive.Components;

internal sealed class LoadingIndicatorShapes
{
    internal Morph[] Sequence { get; }
    internal float ScaleFactor { get; }

    internal LoadingIndicatorShapes(IReadOnlyList<RoundedPolygon> polygons, bool circularSequence)
    {
        if (polygons.Count < 2)
            throw new ArgumentException("Polygons should have, at least, two RoundedPolygons", nameof(polygons));
        Sequence = Enumerable.Range(0, circularSequence ? polygons.Count : polygons.Count - 1)
            .Select(i => new Morph(polygons[i].Normalized(), polygons[(i + 1) % polygons.Count].Normalized()))
            .ToArray();
        var scaleFactor = 1f;
        foreach (var polygon in polygons)
        {
            var bounds = polygon.CalculateBounds();
            var maxBounds = polygon.CalculateMaxBounds();
            var scaleX = (float)bounds.Width / (float)maxBounds.Width;
            var scaleY = (float)bounds.Height / (float)maxBounds.Height;
            scaleFactor = MathF.Min(scaleFactor, MathF.Max(scaleX, scaleY));
        }
        ScaleFactor = scaleFactor;
    }

    internal (int Index, float Progress) DeterminateMorph(float progress)
    {
        // Kotlin Float.toInt maps NaN to zero; the visual progress still remains NaN.
        var index = float.IsNaN(progress) ? 0 : Math.Min((int)(Sequence.Length * progress), Sequence.Length - 1);
        var adjusted = progress == 1 && index == Sequence.Length - 1 ? 1 : progress * Sequence.Length % 1;
        return (index, adjusted);
    }

    // ShapeUtil.toPath(startAngle = 0), followed by LoadingIndicator.processPath. Use the
    // Skia control-point bounds (Path.getBounds), rather than the curve's tight bounds.
    internal SKPath ProcessPath(int index, float progress, float side, float activeScale)
    {
        var cubics = Sequence[index].AsCubics(progress);
        using var builder = new SKPathBuilder();
        var first = cubics[0].Anchor0;
        builder.MoveTo((float)first.X, (float)first.Y);
        foreach (var cubic in cubics)
            builder.CubicTo((float)cubic.Control0.X, (float)cubic.Control0.Y,
                (float)cubic.Control1.X, (float)cubic.Control1.Y, (float)cubic.Anchor1.X, (float)cubic.Anchor1.Y);
        builder.Close();
        var path = builder.Detach();
        var scale = ScaleFactor * activeScale;
        path.Transform(SKMatrix.CreateScale(side * scale, side * scale));
        var bounds = path.Bounds;
        path.Transform(SKMatrix.CreateTranslation(side / 2 - bounds.MidX, side / 2 - bounds.MidY));
        return path;
    }
}

/*
 * Translated from AndroidX CircularWavyProgressModifiers.kt / ShapeUtil.kt.
 * Copyright 2024 The Android Open Source Project; Apache-2.0.
 * RoundedPolygon and Morph are provided by the pinned MaterialShapes submodule.
 */
using Avalonia;
using Avalonia.Media;
using MaterialShapes;
using SkiaSharp;

namespace Material3Expressive.Components;

internal sealed class CircularProgressShapes
{
    RoundedPolygon? track, active;
    Morph? morph;
    float currentWidth = -1, currentHeight = -1, currentWavelength = -1;
    internal int VertexCount { get; private set; } = -1;

    internal void Update(float width, float height, float wavelength, float strokeWidth, bool requiresMorph)
    {
        if (!(wavelength > 0)) throw new ArgumentOutOfRangeException(nameof(wavelength), "Wavelength must be positive.");
        if (width == currentWidth && height == currentHeight && wavelength == currentWavelength)
        {
            if (requiresMorph && morph is null) morph = new(track!, active!);
            return;
        }
        var radius = Math.Min(width, height) / 2 - strokeWidth / 2;
        var count = Math.Max(5, (int)Math.Round(2 * Math.PI * radius / wavelength, MidpointRounding.AwayFromZero));
        if (count != VertexCount)
        {
            track = RoundedPolygon.CreateCircle(count).Normalized();
            active = RoundedPolygon.CreateStar(count, innerRadius: .75,
                rounding: new CornerRounding(.35, .4), innerRounding: new CornerRounding(.5)).Normalized();
            if (requiresMorph) morph = new(track, active);
        }
        currentWidth = width; currentHeight = height; currentWavelength = wavelength; VertexCount = count;
    }

    internal SKPath TrackPath() => WritePath(track!.Cubics, false, track.Center);
    internal SKPath ProgressPath(float amplitude, bool repeat)
    {
        if (morph is not null) return WritePath(morph.AsCubics(amplitude), repeat, new Point(.5, .5));
        else
        {
            var polygon = amplitude == 1 ? active! : track!;
            return WritePath(polygon.Cubics, repeat, polygon.Center);
        }
    }

    // ShapeUtil.pathFromCubics writes Float points, repeats the same contour, closes it, then
    // rotates about the origin. The pivot only determines the first anchor's angle.
    static SKPath WritePath(IReadOnlyList<CubicBezier> cubics, bool repeat, Point pivot)
    {
        using var builder = new SKPathBuilder();
        if (cubics.Count == 0) return builder.Detach();
        builder.MoveTo((float)cubics[0].Anchor0.X, (float)cubics[0].Anchor0.Y);
        for (var pass = 0; pass < (repeat ? 2 : 1); pass++)
        {
            if (pass > 0) builder.LineTo((float)cubics[0].Anchor0.X, (float)cubics[0].Anchor0.Y);
            foreach (var cubic in cubics)
                builder.CubicTo((float)cubic.Control0.X, (float)cubic.Control0.Y,
                    (float)cubic.Control1.X, (float)cubic.Control1.Y, (float)cubic.Anchor1.X, (float)cubic.Anchor1.Y);
        }
        builder.Close();
        var path = builder.Detach();
        var first = cubics[0].Anchor0;
        var radians = MathF.Atan2((float)first.Y - (float)pivot.Y, (float)first.X - (float)pivot.X);
        var angle = (float)(radians * 180.0 / Math.PI);
        path.Transform(SKMatrix.CreateRotationDegrees(-angle + 270));
        return path;
    }
}

internal sealed class CircularProgressDrawingCache : IDisposable
{
    SKPath fullProgress = new(), fullTrack = new();
    readonly SKPathBuilder segmentBuilder = new();
    readonly SKPathMeasure progressMeasure = new(), trackMeasure = new();
    internal SKPath Progress { get; private set; } = new();
    internal SKPath Track { get; private set; } = new();
    float currentWidth = -1, currentHeight = -1, currentAmplitude = -1, currentWavelength = -1;
    float currentGap = -1, currentScale = -1, progressLength, trackLength, capWidth;
    ProgressIndicatorStroke? currentStroke, currentTrackStroke;
    bool currentMotion;

    internal void Update(float width, float height, float end, float amplitude, float offset, float wavelength,
        float gap, ProgressIndicatorStroke stroke, ProgressIndicatorStroke trackStroke, float scale,
        bool enableMotion, CircularProgressShapes shapes)
    {
        var strokeWidth = (float)stroke.Width * scale; var trackWidth = (float)trackStroke.Width * scale;
        if (width != currentWidth || height != currentHeight || amplitude != currentAmplitude ||
            wavelength != currentWavelength || gap != currentGap || stroke != currentStroke ||
            trackStroke != currentTrackStroke || scale != currentScale || enableMotion != currentMotion)
        {
            capWidth = (stroke.Cap == PenLineCap.Flat && trackStroke.Cap == PenLineCap.Flat) || height > width
                ? 0 : Math.Max(strokeWidth / 2, trackWidth / 2);
            fullProgress.Dispose(); fullProgress = shapes.ProgressPath(amplitude, enableMotion);
            Process(fullProgress);
            progressMeasure.SetPath(fullProgress, true);
            progressLength = enableMotion ? progressMeasure.Length / 2 : progressMeasure.Length;
            fullTrack.Dispose(); fullTrack = shapes.TrackPath();
            Process(fullTrack);
            trackMeasure.SetPath(fullTrack, true); trackLength = trackMeasure.Length;
            void Process(SKPath path)
            {
                path.Transform(SKMatrix.CreateScale(width - strokeWidth, height - strokeWidth));
                var bounds = path.Bounds;
                path.Offset(width / 2 - bounds.MidX, height / 2 - bounds.MidY);
            }
            currentWidth = width; currentHeight = height; currentAmplitude = amplitude;
            currentWavelength = wavelength; currentGap = gap; currentStroke = stroke;
            currentTrackStroke = trackStroke; currentScale = scale; currentMotion = enableMotion;
        }
        segmentBuilder.Reset();
        var stop = end * progressLength;
        var trackGap = Math.Min(stop, gap); var insets = Math.Min(stop, capWidth);
        var spacing = insets * 2 + trackGap;
        if (enableMotion)
        {
            var wave = Math.Clamp(offset, 0, 1);
            var shift = wave * progressLength;
            progressMeasure.GetSegment(shift, stop + shift, segmentBuilder, true);
            Progress.Dispose(); Progress = segmentBuilder.Detach();
            var angle = wave * 360 % 360;
            if (angle != 0)
            {
                var bounds = fullProgress.Bounds;
                Progress.Offset(-bounds.MidX, -bounds.MidY);
                Progress.Transform(SKMatrix.CreateRotationDegrees(-angle));
                Progress.Offset(bounds.MidX, bounds.MidY);
            }
        }
        else
        {
            progressMeasure.GetSegment(0, stop, segmentBuilder, true);
            Progress.Dispose(); Progress = segmentBuilder.Detach();
        }
        if (trackLength > 0) trackMeasure.GetSegment(end * trackLength + spacing, trackLength - spacing, segmentBuilder, true);
        Track.Dispose(); Track = segmentBuilder.Detach();
    }

    public void Dispose()
    {
        fullProgress.Dispose(); fullTrack.Dispose(); progressMeasure.Dispose(); trackMeasure.Dispose();
        Progress.Dispose(); Track.Dispose(); segmentBuilder.Dispose();
    }
}

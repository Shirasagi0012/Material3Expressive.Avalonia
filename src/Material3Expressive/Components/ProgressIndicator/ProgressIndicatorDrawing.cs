/*
 * Translated from AndroidX ProgressIndicator.kt / LinearWavyProgressModifiers.kt.
 * Copyright 2022, 2024 The Android Open Source Project; Apache-2.0.
 */
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Material3Expressive.Components;

internal static class ProgressPaint
{
    internal static SKPaint Create(Color color, ProgressIndicatorStroke stroke, float scale, double opacity) => new()
    {
        IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (float)stroke.Width * scale,
        StrokeCap = Cap(stroke.Cap), StrokeJoin = stroke.Join switch
        { PenLineJoin.Round => SKStrokeJoin.Round, PenLineJoin.Bevel => SKStrokeJoin.Bevel, _ => SKStrokeJoin.Miter },
        StrokeMiter = (float)stroke.Miter, PathEffect = stroke.PathEffect,
        Color = new(color.R, color.G, color.B, (byte)Math.Clamp(Math.Round(color.A * opacity, MidpointRounding.AwayFromZero), 0, 255))
    };
    internal static SKStrokeCap Cap(PenLineCap cap) => cap switch
    { PenLineCap.Round => SKStrokeCap.Round, PenLineCap.Square => SKStrokeCap.Square, _ => SKStrokeCap.Butt };
    internal static void Stop(SKCanvas canvas, SKPaint paint, float x, float y, float size, bool round)
    {
        paint.Style = SKPaintStyle.Fill;
        if (round) canvas.DrawCircle(x + size / 2, y, size / 2, paint);
        else canvas.DrawRect(x, y - size / 2, size, size, paint);
    }
}

internal sealed class PlainProgressDrawOperation(Size size, float scale, bool circular, bool indeterminate,
    float progress, Color color, Color trackColor, ProgressIndicatorStroke stroke, float gapSize, bool rtl,
    float[] fractions, float sweepProgress, float rotation, float stopSize) : ICustomDrawOperation
{
    public Rect Bounds { get; } = new Rect(size).Inflate(Math.Max(stroke.Width / 2, 1 / scale));
    public bool HitTest(Point point) => false;
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() { }
    public void Render(ImmediateDrawingContext context)
    {
        using var lease = (context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
            ?? throw new NotSupportedException("Material Progress Indicator requires Avalonia.Skia.")).Lease();
        var canvas = lease.SkCanvas;
        using var active = ProgressPaint.Create(color, stroke, scale, lease.CurrentOpacity);
        using var track = ProgressPaint.Create(trackColor, stroke, scale, lease.CurrentOpacity);
        var width = (float)size.Width * scale; var height = (float)size.Height * scale;
        var thickness = active.StrokeWidth;
        var adjustedGap = stroke.Cap == PenLineCap.Flat || height > width ? gapSize : gapSize + thickness;
        canvas.Save();
        try
        {
            canvas.Scale(1 / scale);
            if (circular)
            {
                if (indeterminate) canvas.RotateDegrees(rotation, width / 2, height / 2);
                var sweep = (indeterminate ? sweepProgress : progress) * 360;
                // Keep the upstream Dp circumference arithmetic, including Float conversion.
                var gapSweep = (adjustedGap / scale / (float)(Math.PI * (width / scale))) * 360;
                var start = indeterminate ? 0 : 270;
                var gap = Math.Min(sweep, gapSweep);
                var rect = new SKRect(thickness / 2, thickness / 2, width - thickness / 2, width - thickness / 2);
                canvas.DrawArc(rect, start + sweep + gap, 360 - sweep - gap * 2, false, track);
                canvas.DrawArc(rect, start, sweep, false, active);
            }
            else
            {
                var gap = adjustedGap / width;
                if (!indeterminate)
                {
                    var trackStart = progress + Math.Min(progress, gap);
                    if (trackStart <= 1) Line(trackStart, 1, track);
                    Line(0, progress, active);
                    if (stopSize > 0)
                    {
                        var stop = Math.Min(stopSize, height);
                        var offset = Math.Min((height - stop) / 2, 6 * scale);
                        if (rtl) { canvas.Save(); canvas.Scale(-1, 1, width / 2, height / 2); }
                        ProgressPaint.Stop(canvas, active, width - stop - offset, height / 2, stop, stroke.Cap == PenLineCap.Round);
                        if (rtl) canvas.Restore();
                    }
                }
                else
                {
                    var tail = fractions[0]; var head = fractions[1]; var tail2 = fractions[2]; var head2 = fractions[3];
                    if (head < 1 - gap) Line(head > 0 ? head + gap : 0, 1, track);
                    if (head - tail > 0) Line(head, tail, active);
                    if (tail > gap) Line(head2 > 0 ? head2 + gap : 0, tail < 1 ? tail - gap : 1, track);
                    if (head2 - tail2 > 0) Line(head2, tail2, active);
                    if (tail2 > gap) Line(0, tail2 < 1 ? tail2 - gap : 1, track);
                }
                void Line(float start, float end, SKPaint paint)
                {
                    var x0 = (rtl ? 1 - end : start) * width;
                    var x1 = (rtl ? 1 - start : end) * width;
                    if (stroke.Cap == PenLineCap.Flat || height > width)
                    {
                        paint.StrokeCap = SKStrokeCap.Butt;
                        canvas.DrawLine(x0, height / 2, x1, height / 2, paint);
                    }
                    else if (Math.Abs(end - start) > 0)
                    {
                        var inset = thickness / 2;
                        canvas.DrawLine(Math.Clamp(x0, inset, width - inset), height / 2,
                            Math.Clamp(x1, inset, width - inset), height / 2, paint);
                    }
                }
            }
        }
        finally { canvas.Restore(); }
    }
}

internal sealed class ProgressIndicatorDrawOperation(Size size, float scale, Color color, Color trackColor,
    ProgressIndicatorStroke stroke, ProgressIndicatorStroke trackStroke, SKPath[] progress, SKPath track,
    float rotation, float stopSize = 0, float progressEnd = 0, float horizontalInsets = 0) : ICustomDrawOperation
{
    public Rect Bounds { get; } = new Rect(size).Inflate(Math.Max(stroke.Width, trackStroke.Width) / 2 + 1 / scale);
    public bool HitTest(Point point) => false;
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() { foreach (var path in progress) path.Dispose(); track.Dispose(); }
    public void Render(ImmediateDrawingContext context)
    {
        using var lease = (context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
            ?? throw new NotSupportedException("Material Wavy Progress Indicator requires Avalonia.Skia.")).Lease();
        var canvas = lease.SkCanvas;
        using var activePaint = ProgressPaint.Create(color, stroke, scale, lease.CurrentOpacity);
        using var trackPaint = ProgressPaint.Create(trackColor, trackStroke, scale, lease.CurrentOpacity);
        var width = (float)size.Width * scale; var height = (float)size.Height * scale;
        canvas.Save();
        try
        {
            canvas.Scale(1 / scale);
            if (rotation != 0) canvas.RotateDegrees(rotation, width / 2, height / 2);
            canvas.DrawPath(track, trackPaint);
            foreach (var path in progress) canvas.DrawPath(path, activePaint);
            var stop = Math.Min(trackPaint.StrokeWidth, stopSize);
            var offset = stop == trackPaint.StrokeWidth ? 0 : trackPaint.StrokeWidth / 4;
            var x = width - stop - offset;
            var progressX = width * progressEnd + horizontalInsets;
            if (x <= progressX) { stop = Math.Max(0, stop - (progressX - x)); x = progressX; }
            if (stop > 0) ProgressPaint.Stop(canvas, activePaint, x, height / 2, stop, trackStroke.Cap == PenLineCap.Round);
        }
        finally { canvas.Restore(); }
    }
}

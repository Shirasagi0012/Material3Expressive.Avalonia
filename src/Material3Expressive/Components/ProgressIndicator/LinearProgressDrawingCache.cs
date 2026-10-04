/* Translated from AndroidX LinearWavyProgressModifiers.kt, Apache-2.0, fixed baseline. */
using Avalonia.Media;
using SkiaSharp;

namespace Material3Expressive.Components;

internal sealed class LinearProgressDrawingCache : IDisposable
{
    readonly SKPathBuilder full = new(), trackBuilder = new(), segmentBuilder = new();
    readonly SKPathMeasure measure = new();
    internal SKPath Track { get; private set; } = new();
    internal SKPath[] Progress { get; private set; } = [];
    internal float CapWidth { get; private set; }
    float currentWidth = -1, currentHeight = -1, currentWavelength = -1, currentAmplitude = -1;
    float pathScale;
    ProgressIndicatorStroke? currentStroke, currentTrackStroke;
    float currentScale;

    internal void Update(float width, float height, float wavelength, float[] fractions, float amplitude,
        float offset, float gap, ProgressIndicatorStroke stroke, ProgressIndicatorStroke trackStroke, float scale)
    {
        if (Progress.Length != fractions.Length / 2)
        {
            foreach (var path in Progress) path.Dispose();
            Progress = Enumerable.Range(0, fractions.Length / 2).Select(_ => new SKPath()).ToArray();
        }
        var strokeWidth = (float)stroke.Width * scale; var trackWidth = (float)trackStroke.Width * scale;
        CapWidth = (stroke.Cap == PenLineCap.Flat && trackStroke.Cap == PenLineCap.Flat) || height > width
            ? 0 : Math.Max(strokeWidth / 2, trackWidth / 2);
        if (width != currentWidth || height != currentHeight || wavelength != currentWavelength ||
            stroke != currentStroke || trackStroke != currentTrackStroke || scale != currentScale ||
            (amplitude == 0) != (currentAmplitude == 0))
        {
            full.Reset(); full.MoveTo(0, 0);
            if (amplitude == 0) full.LineTo(width, 0);
            else
            {
                if (wavelength <= 0) throw new ArgumentOutOfRangeException(nameof(wavelength), "Wavelength must be positive when drawing a wave.");
                var half = wavelength / 2; var x = half; var controlX = half / 2; var controlY = height - strokeWidth;
                while (x <= width + wavelength * 2)
                {
                    full.QuadTo(controlX, controlY, x, 0);
                    x += half; controlX += half; controlY *= -1;
                }
            }
            using var fullPath = full.Snapshot();
            fullPath.Offset(0, height / 2);
            measure.SetPath(fullPath, false);
            pathScale = measure.Length / (fullPath.Bounds.Width + .00000001f);
            currentWidth = width; currentHeight = height; currentWavelength = wavelength;
            currentStroke = stroke; currentTrackStroke = trackStroke; currentScale = scale;
        }
        var halfHeight = height / 2;
        var adjustedGap = gap; var visible = false;
        var nextTrackEnd = width - CapWidth;
        trackBuilder.Reset(); trackBuilder.MoveTo(nextTrackEnd, halfHeight);
        for (var i = 0; i < Progress.Length; i++)
        {
            segmentBuilder.Reset();
            var start = fractions[i * 2]; var end = fractions[i * 2 + 1];
            var tail = start * width; var head = end * width;
            if (i == 0)
            {
                adjustedGap = head < CapWidth ? 0 : Math.Min(head - CapWidth, gap);
                visible = head >= CapWidth;
            }
            // Kotlin fastCoerceIn preserves its min/max comparison order even for a tiny width.
            float Coerce(float value) => value < CapWidth ? CapWidth : value > width - CapWidth ? width - CapWidth : value;
            var adjustedHead = Coerce(head); var adjustedTail = Coerce(tail);
            if (Math.Abs(end - start) > 0)
            {
                var shift = amplitude != 0 ? offset * wavelength : 0;
                measure.GetSegment((adjustedTail + shift) * pathScale, (adjustedHead + shift) * pathScale, segmentBuilder, true);
                var path = segmentBuilder.Detach();
                path.Transform(SKMatrix.CreateScale(1, amplitude));
                path.Offset(shift > 0 ? -shift : 0, (1 - amplitude) * halfHeight);
                Progress[i].Dispose(); Progress[i] = path;
            }
            else { Progress[i].Dispose(); Progress[i] = segmentBuilder.Detach(); }
            var spacing = visible ? adjustedGap + CapWidth * 2 : adjustedGap;
            if (nextTrackEnd > adjustedHead + spacing)
                trackBuilder.LineTo(Math.Max(CapWidth, adjustedHead + spacing), halfHeight);
            if (head > tail)
            {
                nextTrackEnd = Math.Max(CapWidth, adjustedTail - spacing);
                trackBuilder.MoveTo(nextTrackEnd, halfHeight);
            }
        }
        if (nextTrackEnd > CapWidth) trackBuilder.LineTo(CapWidth, halfHeight);
        Track.Dispose(); Track = trackBuilder.Detach();
        currentAmplitude = amplitude;
    }

    public void Dispose()
    {
        full.Dispose(); trackBuilder.Dispose(); segmentBuilder.Dispose(); measure.Dispose(); Track.Dispose();
        foreach (var path in Progress) path.Dispose();
    }
}

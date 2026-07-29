using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using MaterialShapes;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 Expressive circular progress indicator, determinate or not.
/// </summary>
/// <remarks>
/// As with the linear one, Expressive's change is the gap: the active arc and the track arc are
/// separated by <c>TrackActiveSpace</c>, converted here from a length along the circumference into
/// the angle it subtends, so the gap stays the specified size at any diameter.
/// </remarks>
public class MaterialCircularProgressIndicator : MaterialControl
{
    /// <inheritdoc cref="Value" />
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MaterialCircularProgressIndicator, double>(nameof(Value));

    /// <inheritdoc cref="IsIndeterminate" />
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MaterialCircularProgressIndicator, bool>(nameof(IsIndeterminate));

    /// <summary>Draws the Expressive circular wave rather than the flat circular arc.</summary>
    public static readonly StyledProperty<bool> IsWavyProperty =
        AvaloniaProperty.Register<MaterialCircularProgressIndicator, bool>(nameof(IsWavy));

    /// <summary>
    /// The wavy indicator's motion in device-independent pixels per second. A value of zero keeps
    /// the wave stationary. The alpha25 Compose default is one wavelength per second.
    /// </summary>
    public static readonly StyledProperty<double> WaveSpeedProperty =
        AvaloniaProperty.Register<MaterialCircularProgressIndicator, double>(nameof(WaveSpeed), 15);

    private static readonly TimeSpan RotationPeriod = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan SweepPeriod = TimeSpan.FromSeconds(2.5);
    // Used only until the first arrange resolves the actual vertex count. The default 48dp wave
    // size resolves to nine vertices, so this preserves the alpha25 default before then.
    private static readonly TimeSpan InitialWavePeriod = TimeSpan.FromSeconds(9);

    /// <summary>Twelve o'clock, which is where a Material arc starts.</summary>
    private const double StartAngle = -90;

    private LoopingAnimation? _rotation;
    private LoopingAnimation? _sweep;
    private LoopingAnimation? _wave;
    private int _waveVertices;
    private double _waveSpeed;
    private SpringAnimation? _progress;

    private ProgressTokens _tokens = ProgressTokens.Circular;
    private ProgressColors _colors = ProgressTokens.Circular.Resolve(null, false);

    static MaterialCircularProgressIndicator()
    {
        AffectsRender<MaterialCircularProgressIndicator>(IsIndeterminateProperty, IsWavyProperty);
    }

    /// <inheritdoc cref="MaterialLinearProgressIndicator.Value" />
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <inheritdoc cref="MaterialLinearProgressIndicator.IsIndeterminate" />
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the indicator uses the alpha25 <c>CircularWavyProgressIndicator</c>
    /// geometry. The flat circular indicator remains the default for compatibility.
    /// </summary>
    public bool IsWavy
    {
        get => GetValue(IsWavyProperty);
        set => SetValue(IsWavyProperty, value);
    }

    /// <inheritdoc cref="WaveSpeedProperty" />
    public double WaveSpeed
    {
        get => GetValue(WaveSpeedProperty);
        set => SetValue(WaveSpeedProperty, value);
    }

    private LoopingAnimation Rotation => _rotation ??= Motion.Add(new LoopingAnimation(RotationPeriod));

    private LoopingAnimation Sweep => _sweep ??= Motion.Add(new LoopingAnimation(SweepPeriod));

    private LoopingAnimation Wave => _wave ??= Motion.Add(new LoopingAnimation(InitialWavePeriod));

    private SpringAnimation Progress =>
        _progress ??= Motion.AddSpring(Clamp(Value), Spring(MotionRole.DefaultSpatial));

    public override void Render(DrawingContext context)
    {
        var thickness = Math.Max(_tokens.ActiveThickness, _tokens.TrackThickness);
        var diameter = Math.Min(Bounds.Width, Bounds.Height);
        if (diameter <= thickness)
            return;

        var radius = (diameter - thickness) / 2;
        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);

        if (IsWavy)
        {
            DrawWavy(context, centre, radius, thickness);
            return;
        }

        // The gap is specified as a length, so convert it to the angle it subtends at this radius.
        var gap = radius > 0 ? _tokens.TrackActiveSpace / radius * (180 / Math.PI) : 0;

        var (start, sweep) = ActiveArc();

        DrawArc(context, centre, radius, start, sweep, _colors.ActiveIndicatorColor, _tokens.ActiveThickness);

        // The track fills what the active arc leaves, minus a gap at each end.
        var trackSweep = 360 - sweep - gap * 2;
        if (trackSweep > 0)
            DrawArc(context, centre, radius, start + sweep + gap, trackSweep, _colors.TrackColor, _tokens.TrackThickness);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = IsWavy ? _tokens.WaveSize : _tokens.Size;
        return new Size(size, size);
    }

    protected override void OnThemeChanged(ResolvedTheme theme, bool isDark)
    {
        _tokens = theme.GetProgressTokens(ProgressIndicatorShape.Circular);
        _colors = theme.GetProgressColors(ProgressIndicatorShape.Circular, isDark);
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            Progress.AnimateTo(Clamp(Value), Spring(MotionRole.DefaultSpatial));
            UpdateLoops();
            Motion.Start();
        }
        else if (change.Property == IsIndeterminateProperty || change.Property == IsWavyProperty ||
                 change.Property == WaveSpeedProperty)
        {
            UpdateLoops();
            if (change.Property == IsWavyProperty)
                InvalidateMeasure();
        }
    }

    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateLoops();
    }

    /// <summary>The active arc's start angle and sweep, in degrees.</summary>
    private (double Start, double Sweep) ActiveArc()
    {
        if (!IsIndeterminate)
            return (StartAngle, 360 * Progress.Value);

        // Two loops at different periods: one spins the arc, the other grows and shrinks it. Their
        // periods are deliberately not multiples of each other, so the pattern does not visibly
        // repeat.
        var spin = Rotation.Value * 360;
        var breath = Math.Sin(Sweep.Value * Math.PI * 2) * 0.5 + 0.5;
        var sweep = 20 + breath * 250;

        return (StartAngle + spin, sweep);
    }

    private void DrawWavy(DrawingContext context, Point centre, double radius, double thickness)
    {
        // CircularWavyProgressIndicator reads its progress lambda directly. Preserve the existing
        // spring for the legacy flat control, but do not introduce an animation Compose does not
        // apply to the wavy API.
        var progress = IsIndeterminate ? IndeterminateProgress() : IsWavy ? Clamp(Value) : Progress.Value;
        var amplitude = IsIndeterminate ? 1d : IndicatorAmplitude(progress);
        var vertices = Math.Max(5, (int)Math.Round(2 * Math.PI * radius / _tokens.WaveWavelength));
        var waveCubics = BuildWaveCubics(centre, radius * 2, vertices, amplitude, verticalOffset: -.425);
        var wavePath = SampleCubics(waveCubics);
        var trackPath = BuildWavePath(centre, diameter: radius * 2, vertices, 0);
        CentrePathBounds(trackPath, centre);

        // CircularWavyProgressModifiers trims the circle track by its rounded-cap clearance plus
        // the token gap. The active shape is intentionally left untrimmed at its own endpoints.
        var trackLength = ClosedPathLength(trackPath);
        var activeLength = progress * trackLength;
        var capClearance = Math.Min(activeLength, thickness / 2) * 2;
        var gapClearance = Math.Min(activeLength, _tokens.TrackActiveSpace);
        var trackInset = trackLength <= 0 ? 0 : (capClearance + gapClearance) / trackLength;
        var trackStart = progress + trackInset;
        var trackEnd = 1 - trackInset;
        if (trackEnd > trackStart)
        {
            var track = TrimClosedPath(trackPath, trackStart, trackEnd - trackStart, centre, 0);
            if (track is not null)
                context.DrawGeometry(null, new ImmutablePen(_colors.TrackColor, _tokens.TrackThickness,
                    lineCap: PenLineCap.Round), track);
        }

        if (wavePath.Count < 2 || progress <= 0)
            return;

        // The upstream modifier offsets the repeated wave then rotates it back, keeping the
        // progress endpoint fixed while the star lobes travel beneath it.
        var phase = amplitude > 0 && HasWaveMotion ? ConfigureWave(vertices).Value : 0;
        var active = phase == 0
            ? TrimCubicPath(waveCubics, progress)
            : TrimClosedPath(wavePath, phase, progress, centre, -phase * Math.PI * 2);
        if (active is not null)
            context.DrawGeometry(null, new ImmutablePen(_colors.ActiveIndicatorColor, thickness,
                lineCap: PenLineCap.Round), active);
    }

    private static List<Point> BuildWavePath(Point centre, double diameter, int vertices, double amplitude)
    {
        var cubics = BuildWaveCubics(centre, diameter, vertices, amplitude);
        return SampleCubics(cubics);
    }

    private static List<CubicBezier> BuildWaveCubics(
        Point centre, double diameter, int vertices, double amplitude, double verticalOffset = 0)
    {
        var circle = RoundedPolygon.CreateCircle(vertices).Normalized();
        var star = RoundedPolygon.CreateStar(
            vertices,
            innerRadius: .75,
            rounding: new CornerRounding(.35, .4),
            innerRounding: new CornerRounding(.5)).Normalized();
        var clampedAmplitude = Math.Clamp(amplitude, 0, 1);
        // CircularShapes avoids a Morph for its stable endpoints: it draws the circle at zero
        // amplitude and the rounded-star polygon at full amplitude. Apart from matching upstream,
        // this avoids an unnecessary curve re-segmentation for the common determinate state.
        IReadOnlyList<CubicBezier> sourceCubics = clampedAmplitude switch
        {
            <= 0 => circle.Cubics,
            >= 1 => star.Cubics,
            _ => new Morph(circle, star).AsCubics(clampedAmplitude),
        };
        var first = sourceCubics[0].Anchor0;
        var rotateToTwelveOClock = StartAngle * Math.PI / 180 - Math.Atan2(first.Y - .5, first.X - .5);
        var transformed = sourceCubics
            .Select(cubic => TransformCubic(cubic, point =>
                Scale(RotateAroundOrigin(point, rotateToTwelveOClock), centre, diameter)))
            .ToList();
        var points = SampleCubics(transformed);
        var boundsCentre = BoundsCentre(points);
        var shift = new Vector(centre.X - boundsCentre.X, centre.Y + verticalOffset - boundsCentre.Y);
        return transformed.Select(cubic => TransformCubic(cubic, point => point + shift)).ToList();
    }

    private static List<Point> SampleCubics(IReadOnlyList<CubicBezier> cubics)
    {
        const int samplesPerCubic = 64;
        var points = new List<Point>(cubics.Count * samplesPerCubic);
        foreach (var cubic in cubics)
            for (var sample = 0; sample <= samplesPerCubic; sample++)
            {
                if (points.Count > 0 && sample == 0)
                    continue;
                points.Add(PointOnCubic(cubic, sample / (double)samplesPerCubic));
            }
        if (points.Count > 1 && Distance(points[0], points[^1]) < .001)
            points.RemoveAt(points.Count - 1);
        return points;
    }

    private static StreamGeometry? TrimClosedPath(
        IReadOnlyList<Point> path,
        double startFraction,
        double lengthFraction,
        Point centre,
        double rotation)
    {
        if (path.Count < 2 || lengthFraction <= 0)
            return null;

        var lengths = new double[path.Count + 1];
        for (var index = 0; index < path.Count; index++)
            lengths[index + 1] = lengths[index] + Distance(path[index], path[(index + 1) % path.Count]);
        var total = lengths[^1];
        if (total <= 0)
            return null;

        var start = (startFraction - Math.Floor(startFraction)) * total;
        var end = start + Math.Min(1, lengthFraction) * total;
        var points = new List<Point>();
        for (var distance = start; distance <= end;)
        {
            points.Add(Rotate(PointAt(path, lengths, distance % total), centre, rotation));
            var nextIndex = SegmentIndex(lengths, distance % total);
            var nextBoundary = distance - (distance % total) + lengths[nextIndex + 1];
            if (nextBoundary <= distance + .0001)
                nextBoundary += total;
            distance = Math.Min(end, nextBoundary);
            if (distance >= end)
            {
                points.Add(Rotate(PointAt(path, lengths, end % total), centre, rotation));
                break;
            }
        }

        if (points.Count < 2)
            return null;
        var geometry = new StreamGeometry();
        using var sink = geometry.Open();
        sink.BeginFigure(points[0], false);
        for (var index = 1; index < points.Count; index++)
            sink.LineTo(points[index]);
        sink.EndFigure(false);
        return geometry;
    }

    private static double ClosedPathLength(IReadOnlyList<Point> path)
    {
        var length = 0d;
        for (var index = 0; index < path.Count; index++)
            length += Distance(path[index], path[(index + 1) % path.Count]);
        return length;
    }

    private static Geometry? TrimCubicPath(IReadOnlyList<CubicBezier> cubics, double lengthFraction)
    {
        if (cubics.Count == 0 || lengthFraction <= 0)
            return null;

        var geometry = new StreamGeometry();
        using var sink = geometry.Open();
        sink.BeginFigure(cubics[0].Anchor0, false);
        foreach (var cubic in cubics)
            sink.CubicBezierTo(cubic.Control0, cubic.Control1, cubic.Anchor1);
        sink.EndFigure(true);

        return geometry.TryGetSegment(0, Math.Min(1, lengthFraction) * geometry.ContourLength,
            startOnBeginFigure: true, out var segment)
            ? segment
            : null;
    }

    // Compose scales a normalized Shapes path and then translates its measured bounds to the
    // draw scope centre. This matters for an odd-sided star, whose geometric bounds are not
    // centred on its nominal polygon origin.
    private static void CentrePathBounds(IList<Point> path, Point centre)
    {
        if (path.Count == 0)
            return;

        var minX = path[0].X;
        var maxX = minX;
        var minY = path[0].Y;
        var maxY = minY;
        for (var index = 1; index < path.Count; index++)
        {
            minX = Math.Min(minX, path[index].X);
            maxX = Math.Max(maxX, path[index].X);
            minY = Math.Min(minY, path[index].Y);
            maxY = Math.Max(maxY, path[index].Y);
        }

        var shift = new Vector(centre.X - (minX + maxX) / 2, centre.Y - (minY + maxY) / 2);
        for (var index = 0; index < path.Count; index++)
            path[index] += shift;
    }

    private static Point BoundsCentre(IReadOnlyList<Point> path)
    {
        var minX = path.Min(point => point.X);
        var maxX = path.Max(point => point.X);
        var minY = path.Min(point => point.Y);
        var maxY = path.Max(point => point.Y);
        return new Point((minX + maxX) / 2, (minY + maxY) / 2);
    }

    private static CubicBezier TransformCubic(CubicBezier cubic, Func<Point, Point> transform) =>
        new(transform(cubic.Anchor0), transform(cubic.Control0), transform(cubic.Control1), transform(cubic.Anchor1));

    private static int SegmentIndex(IReadOnlyList<double> lengths, double distance)
    {
        for (var index = 0; index < lengths.Count - 1; index++)
            // A point exactly at a segment boundary belongs to the following segment. Otherwise
            // TrimClosedPath would see the same boundary twice and jump straight to the end.
            if (distance < lengths[index + 1] - .0001 || index == lengths.Count - 2)
                return index;
        return lengths.Count - 2;
    }

    private static Point PointAt(IReadOnlyList<Point> path, IReadOnlyList<double> lengths, double distance)
    {
        var index = SegmentIndex(lengths, distance);
        var segmentLength = lengths[index + 1] - lengths[index];
        var fraction = segmentLength <= 0 ? 0 : (distance - lengths[index]) / segmentLength;
        var from = path[index];
        var to = path[(index + 1) % path.Count];
        return new Point(from.X + (to.X - from.X) * fraction, from.Y + (to.Y - from.Y) * fraction);
    }

    private static Point Rotate(Point point, Point centre, double radians)
    {
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        var x = point.X - centre.X;
        var y = point.Y - centre.Y;
        return new Point(centre.X + x * cosine - y * sine, centre.Y + x * sine + y * cosine);
    }

    private static double Distance(Point first, Point second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private double IndeterminateProgress()
    {
        var breath = Math.Sin(Sweep.Value * Math.PI * 2) * .5 + .5;
        return .08 + breath * .62;
    }

    private static double IndicatorAmplitude(double progress) => progress is > .1 and < .95 ? 1 : 0;

    private LoopingAnimation ConfigureWave(int vertices)
    {
        var speed = HasWaveMotion ? WaveSpeed : 0;
        if (speed <= 0 || _tokens.WaveWavelength <= 0)
            return Wave;

        // Compose repeats after every resolved polygon vertex: one full circuit therefore takes
        // vertexCount * wavelength / waveSpeed. Recreate only if those inputs changed, preserving
        // phase so an API update does not visibly jump backwards.
        if (_wave is not null && _waveVertices == vertices && Math.Abs(_waveSpeed - speed) < .0001)
            return _wave;

        var phase = _wave?.Value ?? 0;
        if (_wave is not null)
            Motion.Remove(_wave);

        var seconds = vertices * _tokens.WaveWavelength / speed;
        _wave = Motion.Add(new LoopingAnimation(TimeSpan.FromSeconds(Math.Max(.001, seconds)), phase));
        _waveVertices = vertices;
        _waveSpeed = speed;
        Motion.Start();
        return _wave;
    }

    private static void DrawArc(
        DrawingContext context, Point centre, double radius, double startDegrees, double sweepDegrees,
        IImmutableBrush brush, double thickness)
    {
        if (sweepDegrees <= 0.01 || thickness <= 0)
            return;

        var pen = new ImmutablePen(brush, thickness, lineCap: PenLineCap.Round);

        // A full circle cannot be drawn as one arc — its start and end points coincide, so the
        // renderer cannot tell which way round to go.
        if (sweepDegrees >= 359.99)
        {
            context.DrawEllipse(null, pen, centre, radius, radius);
            return;
        }

        var start = PointOnCircle(centre, radius, startDegrees);
        var end = PointOnCircle(centre, radius, startDegrees + sweepDegrees);

        var geometry = new StreamGeometry();
        using (var sink = geometry.Open())
        {
            sink.BeginFigure(start, false);
            sink.ArcTo(
                end,
                new Size(radius, radius),
                0,
                sweepDegrees > 180,
                SweepDirection.Clockwise);
            sink.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private bool HasWaveMotion => double.IsFinite(WaveSpeed) && WaveSpeed > 0;

    private static Point PointOnCircle(Point centre, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(centre.X + Math.Cos(radians) * radius, centre.Y + Math.Sin(radians) * radius);
    }

    private void UpdateLoops()
    {
        if (IsIndeterminate)
        {
            Rotation.Resume();
            Sweep.Resume();
        }
        else
        {
            Rotation.Settle();
            Sweep.Settle();
        }

        if (IsWavy && HasWaveMotion && (IsIndeterminate || IndicatorAmplitude(Clamp(Value)) > 0))
            Wave.Resume();
        else
            Wave.Settle();

        Motion.Start();
        InvalidateVisual();
    }

    private static double Clamp(double value)
    {
        return double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
    }

    private static Point PointOnCubic(CubicBezier cubic, double t)
    {
        var inverse = 1 - t;
        return new Point(
            cubic.Anchor0.X * inverse * inverse * inverse +
            cubic.Control0.X * 3 * t * inverse * inverse +
            cubic.Control1.X * 3 * t * t * inverse + cubic.Anchor1.X * t * t * t,
            cubic.Anchor0.Y * inverse * inverse * inverse +
            cubic.Control0.Y * 3 * t * inverse * inverse +
            cubic.Control1.Y * 3 * t * t * inverse + cubic.Anchor1.Y * t * t * t);
    }

    private static Point Scale(Point point, Point centre, double diameter) =>
        new(centre.X + (point.X - .5) * diameter, centre.Y + (point.Y - .5) * diameter);

    private static Point RotateAroundOrigin(Point point, double radians)
    {
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return new Point(point.X * cosine - point.Y * sine, point.X * sine + point.Y * cosine);
    }
}

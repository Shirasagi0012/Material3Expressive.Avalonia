using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Controls.Automation;
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

    private static readonly TimeSpan RotationPeriod = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan SweepPeriod = TimeSpan.FromSeconds(6);
    // Used only until the first arrange resolves the actual vertex count. The default 48dp wave
    // size resolves to nine vertices, so this preserves the alpha25 default before then.
    private static readonly TimeSpan InitialWavePeriod = TimeSpan.FromSeconds(9);

    /// <summary>Twelve o'clock, which is where a Material arc starts.</summary>
    private const double StartAngle = -90;
    private const double IndeterminateMinimumProgress = .1;
    private const double IndeterminateMaximumProgress = .87;
    private static readonly Easing IndeterminateProgressEasing =
        Easings.CubicBezier(.2, 0, 0, 1);
    private static readonly Easing IndeterminateRotationEasing =
        Easings.CubicBezier(.05, .7, .1, 1);

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

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialProgressIndicatorAutomationPeer(this);

    /// <summary>The active arc's start angle and sweep, in degrees.</summary>
    private (double Start, double Sweep) ActiveArc()
    {
        if (!IsIndeterminate)
            return (StartAngle, 360 * Progress.Value);

        // Alpha25 drives both the arc rotation and its grow/shrink progress over one 6000ms cycle.
        return (
            StartAngle + IndeterminateRotationDegrees(),
            IndeterminateProgress() * 360);
    }

    private void DrawWavy(DrawingContext context, Point centre, double radius, double thickness)
    {
        // CircularWavyProgressIndicator reads its progress lambda directly. Preserve the existing
        // spring for the legacy flat control, but do not introduce an animation Compose does not
        // apply to the wavy API.
        var progress = IsIndeterminate ? IndeterminateProgress() : IsWavy ? Clamp(Value) : Progress.Value;
        var amplitude = IsIndeterminate ? 1d : IndicatorAmplitude(progress);
        var vertices = Math.Max(5, (int)Math.Round(2 * Math.PI * radius / _tokens.WaveWavelength));
        var waveCubics = BuildWaveCubics(centre, radius * 2, vertices, amplitude);
        var trackCubics = BuildWaveCubics(centre, diameter: radius * 2, vertices, 0);

        // CircularWavyProgressModifiers trims the circle track by its rounded-cap clearance plus
        // the token gap. The active shape is intentionally left untrimmed at its own endpoints.
        var trackLength = CubicPathLength(trackCubics);
        var activeLength = progress * trackLength;
        var capClearance = Math.Min(activeLength, thickness / 2) * 2;
        var gapClearance = Math.Min(activeLength, _tokens.TrackActiveSpace);
        var trackInset = trackLength <= 0 ? 0 : (capClearance + gapClearance) / trackLength;
        var trackStart = progress + trackInset;
        var trackEnd = 1 - trackInset;
        using (context.PushTransform(
                   RotationAround(
                       centre,
                       IsIndeterminate ? IndeterminateRotationDegrees() + 90 : 0)))
        {
            if (trackEnd > trackStart)
            {
                var track = TrimCubicPath(trackCubics, trackStart, trackEnd - trackStart);
                if (track is not null)
                    context.DrawGeometry(null, new ImmutablePen(_colors.TrackColor, _tokens.TrackThickness,
                        lineCap: PenLineCap.Round), track);
            }

            if (waveCubics.Count == 0 || progress <= 0)
                return;

            // The upstream modifier offsets the repeated wave then rotates it back, keeping the
            // progress endpoint fixed while the star lobes travel beneath it.
            // Compose's PathMeasure inputs and wave offset state are Floats. Preserve that precision at
            // the segment boundary: one-wave lands exactly on a lobe boundary, where carrying the
            // calculation in double precision selects a measurably different sub-pixel slice.
            var phase = amplitude > 0 && HasWaveMotion ? (double)(float)ConfigureWave(vertices).Value : 0;
            var active = phase == 0
                ? TrimCubicPath(waveCubics, 0, progress)
                : TrimRepeatedCubicPath(waveCubics, phase, progress);
            if (active is null)
                return;

            var pen = new ImmutablePen(_colors.ActiveIndicatorColor, thickness, lineCap: PenLineCap.Round);
            // The shifted segment is rotated back around the complete path's measured centre, exactly
            // as CircularProgressDrawingCache.updateDrawPaths() does upstream.
            using (context.PushTransform(RotationAround(centre, -(double)((float)phase * 360f))))
            {
                context.DrawGeometry(null, pen, active);
            }
        }
    }

    private static List<CubicBezier> BuildWaveCubics(
        Point centre, double diameter, int vertices, double amplitude)
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
        // processPath() centers Compose Path.getBounds(), which is Skia's cached bounds over path
        // anchors and controls. A sampled/tight curve bound shifts this odd-sided star and makes
        // the error rotate with a non-zero wave phase.
        var boundsCentre = CubicPointBoundsCentre(transformed);
        var shift = new Vector(centre.X - boundsCentre.X, centre.Y - boundsCentre.Y);
        return transformed.Select(cubic => TransformCubic(cubic, point => point + shift)).ToList();
    }

    private static Point CubicPointBoundsCentre(IReadOnlyList<CubicBezier> cubics)
    {
        var first = cubics[0].Anchor0;
        var minX = first.X;
        var maxX = first.X;
        var minY = first.Y;
        var maxY = first.Y;
        foreach (var cubic in cubics)
        {
            Include(cubic.Anchor0);
            Include(cubic.Control0);
            Include(cubic.Control1);
            Include(cubic.Anchor1);
        }

        return new Point((minX + maxX) / 2, (minY + maxY) / 2);

        void Include(Point point)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }
    }

    private static Geometry? TrimRepeatedCubicPath(
        IReadOnlyList<CubicBezier> cubics,
        double startFraction,
        double lengthFraction)
    {
        if (cubics.Count == 0 || lengthFraction <= 0)
            return null;

        var geometry = CreateCubicPath(cubics, repeatPath: true);
        var singleLength = (float)(geometry.ContourLength / 2);
        if (singleLength <= 0)
            return null;

        var start = (float)Math.Clamp(startFraction, 0, 1) * singleLength;
        var end = start + (float)Math.Min(1, lengthFraction) * singleLength;
        return geometry.TryGetSegment(start, end, startOnBeginFigure: true, out var segment)
            ? segment
            : null;
    }

    private static double CubicPathLength(IReadOnlyList<CubicBezier> cubics)
    {
        return CreateCubicPath(cubics, repeatPath: false).ContourLength;
    }

    private static Geometry? TrimCubicPath(
        IReadOnlyList<CubicBezier> cubics,
        double startFraction,
        double lengthFraction)
    {
        if (cubics.Count == 0 || lengthFraction <= 0)
            return null;

        var geometry = CreateCubicPath(cubics, repeatPath: false);
        var length = geometry.ContourLength;
        var start = Math.Clamp(startFraction, 0, 1) * length;
        var end = Math.Min(length, start + Math.Min(1, lengthFraction) * length);
        return geometry.TryGetSegment(start, end, startOnBeginFigure: true, out var segment)
            ? segment
            : null;
    }

    private static StreamGeometry CreateCubicPath(
        IReadOnlyList<CubicBezier> cubics,
        bool repeatPath)
    {
        var geometry = new StreamGeometry();
        using var sink = geometry.Open();
        sink.BeginFigure(cubics[0].Anchor0, false);
        for (var pass = 0; pass < (repeatPath ? 2 : 1); pass++)
        {
            if (pass != 0)
                sink.LineTo(cubics[0].Anchor0);
            foreach (var cubic in cubics)
                sink.CubicBezierTo(cubic.Control0, cubic.Control1, cubic.Anchor1);
        }
        sink.EndFigure(true);
        return geometry;
    }

    private static CubicBezier TransformCubic(CubicBezier cubic, Func<Point, Point> transform) =>
        new(transform(cubic.Anchor0), transform(cubic.Control0), transform(cubic.Control1), transform(cubic.Anchor1));

    private double IndeterminateProgress()
    {
        var phase = Sweep.Value;
        if (phase <= .5)
        {
            var fraction = IndeterminateProgressEasing(phase * 2);
            return Interpolate(
                IndeterminateMinimumProgress,
                IndeterminateMaximumProgress,
                fraction);
        }

        return Interpolate(
            IndeterminateMaximumProgress,
            IndeterminateMinimumProgress,
            (phase - .5) * 2);
    }

    private double IndeterminateRotationDegrees()
    {
        var phase = Rotation.Value;
        var globalRotation = phase * 1080;
        var milliseconds = phase * RotationPeriod.TotalMilliseconds;
        var segment = Math.Min(3, (int)(milliseconds / 1500));
        var segmentTime = milliseconds - segment * 1500;
        var additional = segment * 90d;
        if (segmentTime < 300)
            additional += 90 * IndeterminateRotationEasing(segmentTime / 300);
        else
            additional += 90;
        return globalRotation + additional;
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

    private static Point Scale(Point point, Point centre, double diameter) =>
        new(centre.X + (point.X - .5) * diameter, centre.Y + (point.Y - .5) * diameter);

    private static Point RotateAroundOrigin(Point point, double radians)
    {
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return new Point(point.X * cosine - point.Y * sine, point.X * sine + point.Y * cosine);
    }

    private static Matrix RotationAround(Point centre, double degrees) =>
        Matrix.CreateTranslation(-centre.X, -centre.Y)
        * Matrix.CreateRotation(degrees * Math.PI / 180)
        * Matrix.CreateTranslation(centre.X, centre.Y);

    private static double Interpolate(double start, double end, double fraction) =>
        start + (end - start) * Math.Clamp(fraction, 0, 1);
}

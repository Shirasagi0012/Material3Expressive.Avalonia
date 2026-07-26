using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

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

    private static readonly TimeSpan RotationPeriod = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan SweepPeriod = TimeSpan.FromSeconds(2.5);

    /// <summary>Twelve o'clock, which is where a Material arc starts.</summary>
    private const double StartAngle = -90;

    private LoopingAnimation? _rotation;
    private LoopingAnimation? _sweep;
    private SpringAnimation? _progress;

    private ProgressTokens _tokens = ProgressTokens.Circular;
    private ProgressColors _colors = ProgressTokens.Circular.Resolve(null, false);

    static MaterialCircularProgressIndicator()
    {
        AffectsRender<MaterialCircularProgressIndicator>(IsIndeterminateProperty);
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

    private LoopingAnimation Rotation => _rotation ??= Motion.Add(new LoopingAnimation(RotationPeriod));

    private LoopingAnimation Sweep => _sweep ??= Motion.Add(new LoopingAnimation(SweepPeriod));

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
        return new Size(_tokens.Size, _tokens.Size);
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
            Motion.Start();
        }
        else if (change.Property == IsIndeterminateProperty)
        {
            UpdateLoops();
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

        Motion.Start();
        InvalidateVisual();
    }

    private static double Clamp(double value)
    {
        return double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
    }
}

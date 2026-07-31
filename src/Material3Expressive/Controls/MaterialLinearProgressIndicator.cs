using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 Expressive linear progress indicator, flat or wavy, determinate or not.
/// </summary>
/// <remarks>
/// Expressive's visible change here is the gap: the active indicator and the track no longer meet,
/// and a stop dot marks the far end. Both are tokens (<c>TrackActiveSpace</c>, <c>StopSize</c>)
/// rather than drawing decisions.
/// <para>
/// The wave is drawn as a polyline sampled from a sine rather than as a stroked path of Béziers.
/// Sampling at roughly a point per pixel is both simpler and cheaper than fitting curves, and at
/// a 3dp amplitude the difference is not visible.
/// </para>
/// </remarks>
public class MaterialLinearProgressIndicator : MaterialControl
{
    /// <inheritdoc cref="Value" />
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MaterialLinearProgressIndicator, double>(nameof(Value));

    /// <inheritdoc cref="IsIndeterminate" />
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MaterialLinearProgressIndicator, bool>(nameof(IsIndeterminate));

    /// <inheritdoc cref="IsWavy" />
    public static readonly StyledProperty<bool> IsWavyProperty =
        AvaloniaProperty.Register<MaterialLinearProgressIndicator, bool>(nameof(IsWavy));

    private static readonly TimeSpan WavePeriod = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SweepPeriod = TimeSpan.FromSeconds(1.75);

    private LoopingAnimation? _wave;
    private LoopingAnimation? _sweep;
    private SpringAnimation? _progress;

    private ProgressTokens _tokens = ProgressTokens.Linear;
    private ProgressColors _colors = ProgressTokens.Linear.Resolve(null, false);

    static MaterialLinearProgressIndicator()
    {
        AffectsRender<MaterialLinearProgressIndicator>(IsWavyProperty, IsIndeterminateProperty);
    }

    /// <summary>Progress from 0 to 1. Ignored while <see cref="IsIndeterminate" /> is set.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Runs the indeterminate animation instead of showing a value.</summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>Draws the active indicator as a travelling wave — the Expressive form.</summary>
    public bool IsWavy
    {
        get => GetValue(IsWavyProperty);
        set => SetValue(IsWavyProperty, value);
    }

    private LoopingAnimation Wave => _wave ??= Motion.Add(new LoopingAnimation(WavePeriod));

    private LoopingAnimation Sweep => _sweep ??= Motion.Add(new LoopingAnimation(SweepPeriod));

    // Progress is sprung, so a value that jumps forward glides rather than snapping.
    private SpringAnimation Progress =>
        _progress ??= Motion.AddSpring(Clamp(Value), Spring(MotionRole.DefaultSpatial));

    public override void Render(DrawingContext context)
    {
        var height = Math.Max(_tokens.ActiveThickness, _tokens.TrackThickness);
        var waveRoom = IsWavy ? _tokens.WaveSize : height;
        var centreY = Bounds.Height / 2;
        var width = Bounds.Width;

        if (width <= 0 || waveRoom <= 0)
            return;

        var (activeStart, activeEnd) = ActiveSpan(width);

        // Track: whatever is not the active indicator, minus the Expressive gap at each side.
        var gap = _tokens.TrackActiveSpace;
        var stopRoom = _tokens.StopSize > 0 ? _tokens.StopSize + gap : 0;

        DrawSegment(context, _colors.TrackColor, activeEnd + gap, width - stopRoom, centreY, _tokens.TrackThickness);

        if (activeStart > 0)
            DrawSegment(context, _colors.TrackColor, 0, activeStart - gap, centreY, _tokens.TrackThickness);

        if (IsWavy)
            DrawWave(context, activeStart, activeEnd, centreY);
        else
            DrawSegment(context, _colors.ActiveIndicatorColor, activeStart, activeEnd, centreY, _tokens.ActiveThickness);

        if (_tokens.StopSize > 0 && !IsIndeterminate)
        {
            var radius = _tokens.StopSize / 2;
            context.DrawEllipse(_colors.StopColor, null, new Point(width - radius, centreY), radius, radius);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = IsWavy ? _tokens.WaveSize : Math.Max(_tokens.ActiveThickness, _tokens.TrackThickness);
        var width = double.IsInfinity(availableSize.Width) ? 240 : availableSize.Width;
        return new Size(width, height);
    }

    protected override void OnThemeChanged(ResolvedTheme theme, bool isDark)
    {
        _tokens = theme.GetProgressTokens(ProgressIndicatorShape.Linear);
        _colors = theme.GetProgressColors(ProgressIndicatorShape.Linear, isDark);
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
        else if (change.Property == IsIndeterminateProperty || change.Property == IsWavyProperty)
        {
            UpdateLoops();
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

    /// <summary>Where the active indicator starts and ends, in pixels.</summary>
    private (double Start, double End) ActiveSpan(double width)
    {
        if (!IsIndeterminate)
            return (0, width * Progress.Value);

        // Material's indeterminate linear indicator is one segment that grows out of the left,
        // slides across and shrinks away — expressed here as a head and a tail running the same
        // loop a third of a cycle apart.
        var phase = Sweep.Value;
        var head = Ease(Math.Min(1, phase * 1.5));
        var tail = Ease(Math.Max(0, phase * 1.5 - 0.5));

        return (width * tail, width * head);
    }

    private void DrawSegment(DrawingContext context, IBrush brush, double from, double to, double centreY, double thickness)
    {
        if (to - from < 0.5 || thickness <= 0)
            return;

        var radius = thickness / 2;
        context.DrawRectangle(
            brush,
            null,
            new RoundedRect(new Rect(from, centreY - radius, to - from, thickness), new CornerRadius(radius)));
    }

    private void DrawWave(DrawingContext context, double start, double end, double centreY)
    {
        if (end - start < 0.5)
            return;

        var wavelength = IsIndeterminate ? _tokens.IndeterminateWaveWavelength : _tokens.WaveWavelength;
        if (wavelength <= 0)
        {
            DrawSegment(context, _colors.ActiveIndicatorColor, start, end, centreY, _tokens.ActiveThickness);
            return;
        }

        var amplitude = _tokens.WaveAmplitude;
        var shift = Wave.Value * wavelength;
        var pen = new ImmutablePen(
            _colors.ActiveIndicatorColor,
            _tokens.ActiveThickness,
            lineCap: PenLineCap.Round,
            lineJoin: PenLineJoin.Round);

        var geometry = new StreamGeometry();
        using (var sink = geometry.Open())
        {
            sink.BeginFigure(new Point(start, centreY + Sine(start, shift, wavelength, amplitude)), false);

            for (var x = start + 1; x < end; x += 1)
                sink.LineTo(new Point(x, centreY + Sine(x, shift, wavelength, amplitude)));

            sink.LineTo(new Point(end, centreY + Sine(end, shift, wavelength, amplitude)));
            sink.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private static double Sine(double x, double shift, double wavelength, double amplitude)
    {
        return Math.Sin((x + shift) / wavelength * Math.PI * 2) * amplitude;
    }

    /// <summary>Smooths the indeterminate sweep's ends so it does not start and stop abruptly.</summary>
    private static double Ease(double fraction)
    {
        return Easings.FastOutSlowIn(Math.Clamp(fraction, 0, 1));
    }

    private void UpdateLoops()
    {
        if (IsWavy)
            Wave.Resume();
        else
            Wave.Settle();

        if (IsIndeterminate)
            Sweep.Resume();
        else
            Sweep.Settle();

        Motion.Start();
        InvalidateVisual();
    }

    private static double Clamp(double value)
    {
        return double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
    }
}

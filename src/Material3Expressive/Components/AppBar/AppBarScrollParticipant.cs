using SmoothScroll.Avalonia.Interaction;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using SmoothScroll.Avalonia.Composition;

namespace Material3Expressive.Components;

// Values resolved on the UI thread. No controls, bindings or application callbacks cross
// this boundary. The running participant and its animation evaluators belong to the compositor.
internal readonly record struct AppBarSpring(double Damping, double Stiffness, float Threshold)
{
    internal static bool TryRead(IAnimationSpec<float>? spec, out AppBarSpring? result)
    {
        result = spec is SpringSpec<float> spring
            ? new(spring.DampingRatio, spring.Stiffness, spring.VisibilityThreshold)
            : null;
        return spec is null || spec.GetType() == typeof(SpringSpec<float>);
    }

    internal SpringSpec<float> Create() => new(Damping, Stiffness, Threshold);
}

internal readonly record struct AppBarScrollConfiguration(
    TopScrollKind? TopKind,
    double TopRange,
    double BottomRange,
    double Density,
    bool TopEnabled,
    bool BottomEnabled,
    bool SuppressBottomLayout,
    bool ContentAtStart,
    bool TwoRows,
    Color BaseColor,
    Color ScrolledColor,
    AppBarSpring? TopSnap,
    AppBarSpring? BottomSnap,
    AppBarSpring ColorSpring,
    double ExpandedViewport,
    double Extent);

internal readonly record struct AppBarScrollSnapshot(
    long Generation,
    long Sequence,
    double TopCollapsed,
    double BottomHidden,
    double TopContentOffset,
    double BottomContentOffset,
    double BodyOffset,
    double Viewport);

internal sealed class AppBarScrollParticipant : IScrollMovementParticipant
{
    private AppBarScrollConfiguration _config;
    private readonly CompositionVector3OutputWriter _offsets, _appearance;
    private readonly CompositionColorOutputWriter _color;
    private readonly Action<AppBarScrollSnapshot> _publish;
    private double _top, _bottom, _topContent, _bottomContent, _body;
    private long _generation, _sequence;
    private TargetBasedAnimation<float>? _topSnap, _bottomSnap;
    private TargetBasedAnimation<Color>? _colorAnimation;
    private Color _paintedColor, _targetColor;
    private long _colorStarted;
    private double _lastDirection;
    private bool _detached;
    private static readonly CubicBezierEasing TitleAlpha = new(.8f, 0, .8f, .15f);
    private static readonly ColorConverter Colors = new();

    internal AppBarScrollParticipant(
        AppBarScrollConfiguration config,
        AppBarScrollSnapshot initial,
        CompositionVector3OutputWriter offsets,
        CompositionVector3OutputWriter appearance,
        CompositionColorOutputWriter color,
        Action<AppBarScrollSnapshot> publish)
    {
        _config = config;
        _offsets = offsets;
        _appearance = appearance;
        _color = color;
        _publish = publish;
        _paintedColor = _targetColor = config.BaseColor;
        SetState(initial);
    }

    internal void Configure(AppBarScrollConfiguration config)
    {
        var rangeChanged = _config.TopRange != config.TopRange || _config.BottomRange != config.BottomRange;
        _config = config;
        if (rangeChanged)
            _topSnap = _bottomSnap = null;
        _top = Math.Clamp(_top, 0, config.TopRange);
        _bottom = Math.Clamp(_bottom, 0, config.BottomRange);
        Output();
    }

    internal void SetState(AppBarScrollSnapshot state)
    {
        _generation = state.Generation;
        _top = Math.Clamp(state.TopCollapsed, 0, _config.TopRange);
        _bottom = Math.Clamp(state.BottomHidden, 0, _config.BottomRange);
        _topContent = state.TopContentOffset;
        _bottomContent = state.BottomContentOffset;
        _body = state.BodyOffset;
        _topSnap = _bottomSnap = null;
        Output();
    }

    public double PreScroll(in ScrollMovementContext context)
    {
        _topSnap = _bottomSnap = null;
        _lastDirection = context.Delta;
        if (!_config.TopEnabled || _config.TopKind is null or TopScrollKind.Pinned ||
            _config.TopKind == TopScrollKind.ExitUntilCollapsed && context.Delta < 0)
            return 0;
        var old = _top;
        _top = Math.Clamp(_top + context.Delta, 0, _config.TopRange);
        // Unlike Compose's whole-input return at the edge, account for actual movement.
        // The rest goes to the body in this same transaction (the approved SmoothScroll path).
        return _top - old;
    }

    public double PostScroll(in ScrollMovementContext context, double selfConsumed)
    {
        if (_config.BottomEnabled)
        {
            _bottomContent -= selfConsumed;
            _bottom = Math.Clamp(_bottom + selfConsumed, 0, _config.BottomRange);
        }

        if (!_config.TopEnabled)
            return 0;
        _topContent -= selfConsumed;
        if (_config.TopKind != TopScrollKind.ExitUntilCollapsed || context.Delta >= 0)
            return 0;
        var old = _top;
        _top = Math.Clamp(_top + context.Delta, 0, _config.TopRange);
        return _top - old;
    }

    public ScrollBounds GetVerticalBounds(in ScrollMovementContext context) =>
        new(0, Math.Max(0, _config.Extent - LiveViewport));
    private double LiveViewport =>
        Math.Max(0, _config.ExpandedViewport + Quantize(_top) + (_config.SuppressBottomLayout ? 0 : Quantize(_bottom)));

    public void OnScrollCompleted(in ScrollMovementResult result)
    {
        _body = result.Position;
        Output();
    }

    public void OnPositionChanged(double position)
    {
        // Absolute positioning and a live-range clamp update the mirror without inventing
        // pre/post consumption. In particular, bar-only snap can change the body maximum.
        _body = position;
        Output();
    }

    public void OnIdle(ScrollMovementSource source)
    {
        // Releasing a reverse gesture away from the start must not erase accumulated
        // content displacement (or the pinned bar's overlap). Reset only at the boundary.
        if (_lastDirection < 0 && _body <= 1e-7)
        {
            _topContent = 0;
            if (_bottom == 0 || _bottom == _config.BottomRange)
                _bottomContent = 0;
        }

        _topSnap = StartSnap(
            _top,
            _config.TopRange,
            _config.TopEnabled && _config.TopKind != TopScrollKind.Pinned ? _config.TopSnap : null,
            _config.Density);
        _bottomSnap = StartSnap(
            _bottom,
            _config.BottomRange,
            _config.BottomEnabled ? _config.BottomSnap : null,
            _config.Density);
        Output();
    }

    private static TargetBasedAnimation<float>? StartSnap(double position, double range, AppBarSpring? spring, double density)
    {
        if (spring is null || range <= 0 || position / range < .01 || position >= range)
            return null;
        // Existing AppBar animation specs and their visibility threshold are physical-pixel
        // quantities. Keep the evaluator in those units; only the scroll protocol uses DIP.
        return new(
            spring.Value.Create(),
            new FloatConverter(),
            (float)(position * density),
            (float)(position / range < .5 ? 0 : range * density));
    }

    public bool OnFrame(double elapsedSeconds)
    {
        var time = TimeSpan.FromSeconds(elapsedSeconds);
        if (_topSnap is { } top)
        {
            _top = Math.Clamp(top.GetValue(time) / _config.Density, 0, _config.TopRange);
            if (top.IsFinished(time))
                _topSnap = null;
        }

        if (_bottomSnap is { } bottom)
        {
            _bottom = Math.Clamp(bottom.GetValue(time) / _config.Density, 0, _config.BottomRange);
            if (bottom.IsFinished(time))
                _bottomSnap = null;
        }

        Output();
        return _topSnap is { } || _bottomSnap is { } || _colorAnimation is { };
    }

    public void OnCancelled()
    {
        _topSnap = _bottomSnap = null;
        _colorAnimation = null;
    }

    public void Detach()
    {
        _detached = true;
        OnCancelled();
    }

    private double Quantize(double value) =>
        -MathUtils.RoundToInt(-value * _config.Density) / _config.Density;

    private void Output()
    {
        if (_detached)
            return;
        var fraction = _config.TopRange > 0 ? (float)(_top / _config.TopRange) : 0;
        _offsets.SetValue(new Vector3D(Quantize(_top), _config.SuppressBottomLayout ? 0 : Quantize(_bottom), _body));
        _appearance.SetValue(new Vector3D(_config.TwoRows ? TitleAlpha.Ease(fraction) : 1, 1 - fraction, fraction));
        var overlap = !_config.ContentAtStart && _topContent == 0
            ? 1
            : _config.TopRange > 0
                ? Math.Clamp(Math.Abs(_topContent) / _config.TopRange, 0, 1)
                : 0;
        var target = _config.TwoRows
            ? Interpolate(
                _config.BaseColor,
                _config.ScrolledColor,
                (float)ComposeEasings.FastOutLinearIn.Ease(fraction))
            : overlap > .01 ? _config.ScrolledColor : _config.BaseColor;
        if (_config.TwoRows)
            _paintedColor = _targetColor = target;
        else
        {
            if (target != _targetColor)
            {
                _targetColor = target;
                _colorStarted = Stopwatch.GetTimestamp();
                // Same default effects spring as the existing single-row chrome.
                var spring = _config.ColorSpring;
                _colorAnimation = new(new SpringSpec<Color>(spring.Damping, spring.Stiffness), Colors, _paintedColor, target);
            }

            if (_colorAnimation is { } animation)
            {
                var time = Stopwatch.GetElapsedTime(_colorStarted);
                _paintedColor = animation.GetValue(time);
                if (animation.IsFinished(time))
                    _colorAnimation = null;
            }
        }

        _color.SetValue(_paintedColor);
        _publish(new(_generation, ++_sequence, _top, _bottom, _topContent, _bottomContent, _body, LiveViewport));
    }

    private static Color Interpolate(Color a, Color b, float fraction)
    {
        Span<double> first = stackalloc double[4];
        Span<double> last = stackalloc double[4];
        Colors.To(a, first);
        Colors.To(b, last);
        for (var i = 0; i < 4; i++)
            first[i] = MathUtils.Lerp((float)first[i], (float)last[i], fraction);
        return Colors.From(first);
    }
}

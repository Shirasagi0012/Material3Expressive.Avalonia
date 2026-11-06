using System;
using Avalonia;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Foundation.Interaction;

// Upstream source: material3-ripple {RippleAnimation,CommonRipple}.kt. Compose supplies a Color
// with its state alpha replaced before Skia compositing;
// MaterialInteractionRendering.ComposeOpacity preserves that nearest-8-bit behavior for Avalonia's
// opacity stack.

/// <summary>
/// One press ripple: a circle growing from the pointer and fading away on release.
/// </summary>
/// <remarks>
/// Ported from androidx <c>material-ripple/RippleAnimation.kt</c>. Three curves run at once — alpha
/// in over 75ms linear, radius out over 225ms eased, and the centre drifting from the touch point
/// to the middle of the control over the same 225ms — and the fade-out only starts once the press
/// is released.
/// <para>
/// The detail that makes a fast tap look right: if the press is released before the fade-in has
/// finished, the ripple jumps to full alpha rather than fading out from wherever it had got to.
/// Without it a quick tap produces a barely visible flicker.
/// </para>
/// </remarks>
public sealed class RippleAnimation : IDisposable
{
    private static readonly TimeSpan FadeIn = TimeSpan.FromMilliseconds(75);
    private static readonly TimeSpan RadiusGrowth = TimeSpan.FromMilliseconds(225);
    private static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(150);

    /// <summary>A bounded ripple grows 10dp past the container's corner.</summary>
    private const float BoundedExtraRadius = 10;

    private readonly Animatable<float> _alpha;
    private readonly Animatable<float> _radiusFraction;
    private readonly Animatable<float> _centerFraction;
    private readonly TaskCompletionSource _finishSignal = new();
    private readonly AnimationChildScope _motion;
    internal AnimationChildScope Motion => _motion;
    private Action? _onFrame;

    private Point _origin;
    private Point _center;
    private float _startRadius;
    private readonly float _endRadius;
    private readonly float _density;
    private readonly bool _bounded;
    private readonly Size _initialBounds;
    private bool _drawInitialized;

    private bool _finishedFadingIn;
    private bool _finishRequested;
    private bool _started;
    private bool _disposed;

    /// <summary>Creates a ripple in its own child scope; disposing it leaves the parent active.</summary>
    public RippleAnimation(Point origin, Size bounds, AnimationScope parentScope,
        bool bounded = true, double? radius = null)
        : this(origin, bounds, parentScope, bounded, radius, 1f) { }

    internal RippleAnimation(Point origin, Size bounds, AnimationScope parentScope,
        bool bounded, double? radius, float density)
    {
        ArgumentNullException.ThrowIfNull(parentScope);
        _motion = parentScope.CreateChild("Ripple");
        _bounded = bounded;
        _density = density;
        _initialBounds = bounds;
        var pixels = PixelSize(bounds);
        _center = new Point((float)pixels.Width / 2f, (float)pixels.Height / 2f);
        // CommonRipple deliberately discards the press position for an unbounded ripple. It
        // starts in the centre and therefore has no centre drift at all.
        _origin = bounded ? new Point((float)(origin.X * density), (float)(origin.Y * density)) : _center;
        _startRadius = StartRadius(pixels);
        _endRadius = radius is { } explicitRadius ? (float)explicitRadius * density : EndRadiusPixels(pixels, bounded, density);

        _alpha = new Animatable<float>(new FloatConverter(), 0f, VisibilityThresholds.Float);
        _radiusFraction = new Animatable<float>(new FloatConverter(), 0f, VisibilityThresholds.Float);
        _centerFraction = new Animatable<float>(new FloatConverter(), 0f, VisibilityThresholds.Float);
        _motion.SubscribeValue(_alpha, _ => _onFrame?.Invoke());
        _motion.SubscribeValue(_radiusFraction, _ => _onFrame?.Invoke());
        _motion.SubscribeValue(_centerFraction, _ => _onFrame?.Invoke());
    }

    /// <summary>The ripple's opacity multiplier, before the state layer's own opacity.</summary>
    public double Value => _finishRequested && !_finishedFadingIn ? 1 : _alpha.Value;

    public bool IsRunning => _alpha.IsRunning || _radiusFraction.IsRunning || _centerFraction.IsRunning;

    /// <summary>True once the ripple has faded out and can be dropped.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>Whether this ripple is clipped to its target layout.</summary>
    public bool IsBounded => _bounded;

    /// <summary>The fully expanded radius, including an explicit per-call-site override.</summary>
    public double TargetRadius => _endRadius / _density;

    public double Radius => Lerp(_startRadius, _endRadius, _radiusFraction.Value) / _density;

    public Point Center => new(
        Lerp((float)_origin.X, (float)_center.X, _centerFraction.Value) / _density,
        Lerp((float)_origin.Y, (float)_center.Y, _centerFraction.Value) / _density);

    // ui-util lerp(Float), including its rounding boundaries. Geometry is evaluated in
    // physical pixels like DrawScope; only the Avalonia drawing boundary converts to DIP.
    private static float Lerp(float start, float stop, float fraction) => (1f - fraction) * start + fraction * stop;
    private Size PixelSize(Size size) => new((float)(size.Width * _density), (float)(size.Height * _density));

    /// <summary>
    /// Requests completion. The fade-out starts only after all three fade-in animations finish.
    /// </summary>
    public void Finish()
    {
        if (_finishRequested)
            return;

        _finishRequested = true;
        _finishSignal.TrySetResult();
        _onFrame?.Invoke();
    }

    /// <summary>Runs the three entry curves, waits for release, then fades out. One run per ripple.</summary>
    public async Task AnimateAsync(Action? onFrame = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
            throw new InvalidOperationException("A ripple can only be played once.");
        _started = true;
        _onFrame = onFrame;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_motion.Lifetime, cancellationToken);
        var token = lifetime.Token;
        try
        {
            await Task.WhenAll(
                _alpha.AnimateToAsync(_motion, 1, new TweenSpec<float>(FadeIn, easing: ComposeEasings.Linear),
                    cancellationToken: token),
                _radiusFraction.AnimateToAsync(_motion, 1, new TweenSpec<float>(RadiusGrowth, easing: ComposeEasings.FastOutSlowIn),
                    cancellationToken: token),
                _centerFraction.AnimateToAsync(_motion, 1, new TweenSpec<float>(RadiusGrowth, easing: ComposeEasings.Linear),
                    cancellationToken: token));

            // All three curves must finish, including the 225ms expansion on a quick tap.
            _finishedFadingIn = true;
            await _finishSignal.Task.WaitAsync(token);
            await _alpha.AnimateToAsync(_motion, 0, new TweenSpec<float>(FadeOut, easing: ComposeEasings.Linear),
                cancellationToken: token);
            IsComplete = true;
        }
        finally
        {
            lifetime.Cancel();
            _onFrame = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _motion.Dispose();
        _onFrame = null;
    }

    /// <summary>
    /// Draws the ripple at its current radius and opacity. The caller supplies a clip for a
    /// bounded node and deliberately omits it for an unbounded one.
    /// </summary>
    public void Draw(DrawingContext context, IBrush brush, double opacity)
        => Draw(context, brush, opacity, _initialBounds);

    internal void Draw(DrawingContext context, IBrush brush, double opacity, Size size,
        bool followUnboundedCenter = false)
    {
        InitializeDrawGeometry(size);
        // CommonRipple first creates color.copy(alpha = pressedAlpha), which packs sRGB
        // alpha to 8 bits, then RippleAnimation.draw copies color.alpha * animatedAlpha.
        // Combining those operations before quantization changes the interior by one byte.
        var alpha = (float)Value * (float)MaterialInteractionRendering.ComposeOpacity(opacity);
        if (alpha <= 0)
            return;

        using (context.PushOpacity(MaterialInteractionRendering.ComposeOpacity(alpha)))
            context.DrawEllipse(brush, null,
                followUnboundedCenter && !_bounded ? new Rect(size).Center : Center, Radius, Radius);
    }

    internal void InitializeDrawGeometry(Size size)
    {
        // RippleAnimation.draw captures these values on its first draw, even at zero alpha.
        // Press feedback may resize the target between the input event and that draw (Switch).
        // The target radius was already resolved by CommonRipple when the press was added.
        if (!_drawInitialized)
        {
            var pixels = PixelSize(size);
            _startRadius = StartRadius(pixels);
            _center = new Point((float)pixels.Width / 2f, (float)pixels.Height / 2f);
            if (!_bounded) _origin = _center;
            _drawInitialized = true;
        }
    }

    /// <summary>Per the spec, the ripple starts at 30% of the container's largest dimension.</summary>
    private static float StartRadius(Size bounds)
    {
        return Math.Max((float)bounds.Width, (float)bounds.Height) * 0.3f;
    }

    internal static double EndRadius(Size bounds, bool bounded) => EndRadiusPixels(bounds, bounded, 1);

    private static float EndRadiusPixels(Size bounds, bool bounded, float density)
    {
        var width = (float)bounds.Width;
        var height = (float)bounds.Height;
        var covering = MathF.Sqrt(width * width + height * height) / 2f;
        return bounded ? covering + BoundedExtraRadius * density : covering;
    }
}

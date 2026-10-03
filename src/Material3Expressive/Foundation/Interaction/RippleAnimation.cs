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
    private const double BoundedExtraRadius = 10;

    private readonly Animatable<double> _alpha;
    private readonly Animatable<double> _radiusFraction;
    private readonly Animatable<double> _centerFraction;
    private readonly TaskCompletionSource _finishSignal = new();
    private readonly VisualAnimationScope _motion;
    internal VisualAnimationScope Motion => _motion;
    private Action? _onFrame;

    private readonly Point _origin;
    private readonly Point _center;
    private readonly double _startRadius;
    private readonly double _endRadius;
    private readonly bool _bounded;

    private bool _finishedFadingIn;
    private bool _finishRequested;
    private bool _started;
    private bool _disposed;

    /// <summary>Creates a ripple in its own child scope; disposing it leaves the parent active.</summary>
    public RippleAnimation(Point origin, Size bounds, VisualAnimationScope parentScope,
        bool bounded = true, double? radius = null)
    {
        ArgumentNullException.ThrowIfNull(parentScope);
        _motion = parentScope.CreateChild("Ripple");
        _bounded = bounded;
        _center = new Point(bounds.Width / 2, bounds.Height / 2);
        // CommonRipple deliberately discards the press position for an unbounded ripple. It
        // starts in the centre and therefore has no centre drift at all.
        _origin = bounded ? origin : _center;
        _startRadius = StartRadius(bounds);
        _endRadius = radius ?? EndRadius(bounds, bounded);

        _alpha = _motion.CreateAnimatable(0d);
        _radiusFraction = _motion.CreateAnimatable(0d);
        _centerFraction = _motion.CreateAnimatable(0d);
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
    public double TargetRadius => _endRadius;

    public double Radius => MathUtils.Lerp(_startRadius, _endRadius, _radiusFraction.Value);

    public Point Center => new(
        MathUtils.Lerp(_origin.X, _center.X, _centerFraction.Value),
        MathUtils.Lerp(_origin.Y, _center.Y, _centerFraction.Value));

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
                _alpha.AnimateToAsync(1, new TweenSpec<double>(FadeIn, easing: ComposeEasings.Linear),
                    cancellationToken: token),
                _radiusFraction.AnimateToAsync(1, new TweenSpec<double>(RadiusGrowth, easing: ComposeEasings.FastOutSlowIn),
                    cancellationToken: token),
                _centerFraction.AnimateToAsync(1, new TweenSpec<double>(RadiusGrowth, easing: ComposeEasings.Linear),
                    cancellationToken: token));

            // All three curves must finish, including the 225ms expansion on a quick tap.
            _finishedFadingIn = true;
            await _finishSignal.Task.WaitAsync(token);
            await _alpha.AnimateToAsync(0, new TweenSpec<double>(FadeOut, easing: ComposeEasings.Linear),
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
    {
        // CommonRipple first creates color.copy(alpha = pressedAlpha), which packs sRGB
        // alpha to 8 bits, then RippleAnimation.draw copies color.alpha * animatedAlpha.
        // Combining those operations before quantization changes the interior by one byte.
        var alpha = Value * MaterialInteractionRendering.ComposeOpacity(opacity);
        if (alpha <= 0)
            return;

        using (context.PushOpacity(MaterialInteractionRendering.ComposeOpacity(alpha)))
            context.DrawEllipse(brush, null, Center, Radius, Radius);
    }

    /// <summary>Per the spec, the ripple starts at 30% of the container's largest dimension.</summary>
    private static double StartRadius(Size bounds)
    {
        return Math.Max(bounds.Width, bounds.Height) * 0.3;
    }

    internal static double EndRadius(Size bounds, bool bounded)
    {
        var covering = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) / 2;
        return bounded ? covering + BoundedExtraRadius : covering;
    }
}

using System;
using Avalonia;
using Avalonia.Media;
using Material3ExpressiveRefactor.Foundation.Motion;

namespace Material3ExpressiveRefactor.Foundation.Interaction;

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
public sealed class RippleAnimation : IAnimation
{
    private static readonly TimeSpan FadeIn = TimeSpan.FromMilliseconds(75);
    private static readonly TimeSpan RadiusGrowth = TimeSpan.FromMilliseconds(225);
    private static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(150);

    /// <summary>A bounded ripple grows 10dp past the container's corner.</summary>
    private const double BoundedExtraRadius = 10;

    private readonly TweenAnimation _alpha;
    private readonly TweenAnimation _radiusFraction;
    private readonly TweenAnimation _centerFraction;

    private readonly Point _origin;
    private readonly Point _center;
    private readonly double _startRadius;
    private readonly double _endRadius;
    private readonly bool _bounded;

    private bool _finishedFadingIn;
    private bool _finishRequested;
    private bool _fadingOut;

    public RippleAnimation(Point origin, Size bounds, bool bounded = true, double? radius = null)
    {
        _bounded = bounded;
        _center = new Point(bounds.Width / 2, bounds.Height / 2);
        // CommonRipple deliberately discards the press position for an unbounded ripple. It
        // starts in the centre and therefore has no centre drift at all.
        _origin = bounded ? origin : _center;
        _startRadius = StartRadius(bounds);
        _endRadius = radius ?? EndRadius(bounds, bounded);

        _alpha = new TweenAnimation(0, FadeIn, Easings.Linear);
        _radiusFraction = new TweenAnimation(0, RadiusGrowth, Easings.FastOutSlowIn);
        _centerFraction = new TweenAnimation(0, RadiusGrowth, Easings.Linear);

        _alpha.AnimateTo(1);
        _radiusFraction.AnimateTo(1);
        _centerFraction.AnimateTo(1);
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

    public double Radius => _startRadius + (_endRadius - _startRadius) * _radiusFraction.Value;

    public Point Center => new(
        _origin.X + (_center.X - _origin.X) * _centerFraction.Value,
        _origin.Y + (_center.Y - _origin.Y) * _centerFraction.Value);

    /// <summary>
    /// Requests completion. The fade-out starts only after all three fade-in animations finish.
    /// </summary>
    public void Finish()
    {
        if (_finishRequested)
            return;

        _finishRequested = true;
        if (_finishedFadingIn)
            BeginFadeOut();
    }

    /// <summary>Compatibility name for ending the ripple associated with a released press.</summary>
    public void Release() => Finish();

    public bool Tick(TimeSpan delta)
    {
        var alphaRunning = _alpha.Tick(delta);
        var radiusRunning = _radiusFraction.Tick(delta);
        var centerRunning = _centerFraction.Tick(delta);

        // RippleAnimation.fadeIn() uses coroutineScope, so the phase completes only when alpha,
        // radius, and centre have all arrived. Alpha reaches one at 75ms but radius/centre take
        // 225ms. Starting fade-out at 75ms makes quick taps visibly collapse too early.
        if (!_finishedFadingIn && !alphaRunning && !radiusRunning && !centerRunning)
        {
            _finishedFadingIn = true;
            if (_finishRequested)
            {
                BeginFadeOut();
                return true;
            }
        }

        if (_fadingOut && !alphaRunning)
        {
            IsComplete = true;
            return false;
        }

        return alphaRunning || radiusRunning || centerRunning;
    }

    public void Settle()
    {
        _alpha.Settle();
        _radiusFraction.Settle();
        _centerFraction.Settle();

        _finishedFadingIn = true;
        if (_finishRequested)
            IsComplete = true;
    }

    private void BeginFadeOut()
    {
        if (_fadingOut || IsComplete)
            return;

        _fadingOut = true;
        _alpha.AnimateTo(0, FadeOut, Easings.Linear);
    }

    /// <summary>
    /// Draws the ripple at its current radius and opacity. The caller supplies a clip for a
    /// bounded node and deliberately omits it for an unbounded one.
    /// </summary>
    public void Draw(DrawingContext context, IBrush brush, double opacity)
    {
        var alpha = Value * opacity;
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

    private static double EndRadius(Size bounds, bool bounded)
    {
        var covering = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) / 2;
        return bounded ? covering + BoundedExtraRadius : covering;
    }
}

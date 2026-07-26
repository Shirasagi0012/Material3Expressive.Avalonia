using System;
using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

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

    private bool _fadedIn;
    private bool _releaseRequested;

    public RippleAnimation(Point origin, Size bounds, bool bounded = true)
    {
        _origin = origin;
        _center = new Point(bounds.Width / 2, bounds.Height / 2);
        _startRadius = StartRadius(bounds);
        _endRadius = EndRadius(bounds, bounded);

        _alpha = new TweenAnimation(0, FadeIn, Easings.Linear);
        _radiusFraction = new TweenAnimation(0, RadiusGrowth, Easings.FastOutSlowIn);
        _centerFraction = new TweenAnimation(0, RadiusGrowth, Easings.Linear);

        _alpha.AnimateTo(1);
        _radiusFraction.AnimateTo(1);
        _centerFraction.AnimateTo(1);
    }

    /// <summary>The ripple's opacity multiplier, before the state layer's own opacity.</summary>
    public double Value => _releaseRequested && !_fadedIn ? 1 : _alpha.Value;

    public bool IsRunning => _alpha.IsRunning || _radiusFraction.IsRunning || _centerFraction.IsRunning;

    /// <summary>True once the ripple has faded out and can be dropped.</summary>
    public bool IsComplete { get; private set; }

    public double Radius => _startRadius + (_endRadius - _startRadius) * _radiusFraction.Value;

    public Point Center => new(
        _origin.X + (_center.X - _origin.X) * _centerFraction.Value,
        _origin.Y + (_center.Y - _origin.Y) * _centerFraction.Value);

    /// <summary>Begins the fade-out. Safe to call more than once.</summary>
    public void Release()
    {
        if (_releaseRequested)
            return;

        _releaseRequested = true;
        _alpha.AnimateTo(0, FadeOut, Easings.Linear);
    }

    public bool Tick(TimeSpan delta)
    {
        var alphaRunning = _alpha.Tick(delta);
        var radiusRunning = _radiusFraction.Tick(delta);
        var centerRunning = _centerFraction.Tick(delta);

        if (!alphaRunning && !_releaseRequested)
            _fadedIn = true;

        if (_releaseRequested && !alphaRunning)
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

        if (_releaseRequested)
            IsComplete = true;
    }

    /// <summary>
    /// Draws the ripple at its current radius and opacity. The caller supplies the clip, because
    /// the state layer under it needs the same one.
    /// </summary>
    public void Draw(DrawingContext context, IBrush brush, double opacity)
    {
        var alpha = Value * opacity;
        if (alpha <= 0)
            return;

        using (context.PushOpacity(alpha))
            context.DrawEllipse(brush, null, Center, Radius, Radius);
    }

    /// <summary>Per the spec, the ripple starts at 60% of the container's largest dimension.</summary>
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

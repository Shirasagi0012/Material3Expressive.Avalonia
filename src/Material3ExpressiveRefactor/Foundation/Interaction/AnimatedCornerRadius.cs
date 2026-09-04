using System;
using Avalonia;
using Material3ExpressiveRefactor.Foundation.Motion;

namespace Material3ExpressiveRefactor.Foundation.Interaction;

/// <summary>
/// Morphs between two corner radii with a spring, handling interruption.
/// </summary>
/// <remarks>
/// Ported from androidx <c>material3/internal/AnimatedShape.kt</c>, narrowed to what a Material
/// button's press actually does: a corner-radius interpolation, not a polygon morph.
/// <para>
/// The endpoints are clamped to the half-extent of the current <see cref="Size" /> BEFORE the
/// interpolation runs. The stadium shape arrives as <c>CornerRadius(1000)</c> — deliberately far
/// larger than any control extent — and lerping 1000→8 would look round until the last instant;
/// clamped to the half-pill first, the morph is the honest 20→8 the spec shows.
/// </para>
/// <para>
/// Interruption splits into two cases, exactly like upstream. Returning to the shape being
/// animated away from is a reversal: progress flips and velocity negates, so the corners keep
/// their momentum instead of restarting. Heading somewhere new freezes whatever is on screen as
/// the new start — evaluated at the progress boundary, so a rapid toggle cannot stack rounding
/// error into the frozen value.
/// </para>
/// </remarks>
public sealed class AnimatedCornerRadius
{
    private readonly SpringAnimation _progress;
    private CornerRadius _start;
    private CornerRadius _target;

    public AnimatedCornerRadius(CornerRadius initial, SpringSpec spec)
    {
        _start = initial;
        _target = initial;
        _progress = new SpringAnimation(1.0, spec);
    }

    /// <summary>The animation to register with a material motion controller.</summary>
    public IAnimation Progress => _progress;

    /// <summary>The corners being animated towards, before clamping.</summary>
    public CornerRadius Target => _target;

    public bool IsRunning => _progress.IsRunning;

    /// <summary>
    /// The layout size the endpoints clamp against. A non-positive size disables clamping — the
    /// endpoints are then taken as given.
    /// </summary>
    public Size Size { get; set; }

    /// <summary>The corner radii as they look right now: endpoints clamped, then interpolated.</summary>
    public CornerRadius Current =>
        Lerp(ClampToSize(_start), ClampToSize(_target), _progress.Value);

    /// <summary>Jumps to corners without animating.</summary>
    public void SnapTo(CornerRadius corners)
    {
        _start = corners;
        _target = corners;
        _progress.SnapTo(1.0);
    }

    /// <summary>Starts morphing towards corners, or reverses if it is the one being left.</summary>
    /// <param name="corners">The corners to head for.</param>
    /// <param name="spec">A spring to switch to; the current one is kept when omitted.</param>
    public void AnimateTo(CornerRadius corners, SpringSpec? spec = null)
    {
        if (_target.Equals(corners))
            return;

        if (corners.Equals(_start))
        {
            // Reversal: keep the momentum by flipping progress and negating velocity.
            _start = _target;
            _target = corners;

            var progress = _progress.Value;
            var velocity = _progress.Velocity;

            _progress.SnapTo(1.0 - progress);
            _progress.AnimateTo(1.0, spec, -velocity);
            return;
        }

        // A new destination. Freeze what is on screen as the starting corners, taking the operand
        // itself at a progress boundary so a rapid toggle accumulates no rounding error.
        _start = _progress.Value switch
        {
            >= 1.0 => _target,
            <= 0.0 => _start,
            var progress => Lerp(ClampToSize(_start), ClampToSize(_target), progress)
        };

        _target = corners;
        _progress.SnapTo(0.0);
        _progress.AnimateTo(1.0, spec);
    }

    internal static CornerRadius Lerp(CornerRadius from, CornerRadius to, double progress)
    {
        return new CornerRadius(
            from.TopLeft + (to.TopLeft - from.TopLeft) * progress,
            from.TopRight + (to.TopRight - from.TopRight) * progress,
            from.BottomRight + (to.BottomRight - from.BottomRight) * progress,
            from.BottomLeft + (to.BottomLeft - from.BottomLeft) * progress);
    }

    private CornerRadius ClampToSize(CornerRadius corners)
    {
        if (Size.Width <= 0 || Size.Height <= 0)
            return corners;

        // CornerFull's 1000 is a sentinel for "half the smaller extent" (Compose's 50%).
        var max = Math.Min(Size.Width, Size.Height) / 2;
        return new CornerRadius(
            Math.Min(corners.TopLeft, max),
            Math.Min(corners.TopRight, max),
            Math.Min(corners.BottomRight, max),
            Math.Min(corners.BottomLeft, max));
    }
}

using System;
using Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// Morphs between corner-based shapes with a spring, handling interruption.
/// </summary>
/// <remarks>
/// Ported from androidx <c>material3/internal/AnimatedShape.kt</c>. This is what a Material button
/// actually does when pressed — a corner-size interpolation, not a polygon morph — and it covers
/// Button, IconButton, FAB, Chip and Menu.
/// <para>
/// Interruption is the whole substance of the class, and it splits into two cases. Returning to the
/// shape being animated <i>away from</i> is a reversal: the progress is flipped and the velocity
/// negated, so the corners keep their momentum instead of restarting. Heading somewhere new
/// freezes whatever is on screen as the new starting shape and restarts progress from zero.
/// </para>
/// <para>
/// Freezing has a trap upstream calls out: naïvely capturing the interpolation would nest a new
/// lerp inside the old one on every interruption, and a shape toggled fast enough would build a
/// chain deep enough to overflow the stack when resolved. Progress exactly at a boundary therefore
/// captures the operand itself, and <see cref="RoundedCornerShape.Lerp" /> collapses at 0 and 1 for
/// the same reason.
/// </para>
/// </remarks>
public sealed class AnimatedShape
{
    private readonly SpringAnimation _progress;

    private RoundedCornerShape _start;
    private RoundedCornerShape _target;

    private double _cachedProgress = double.NaN;
    private RoundedCornerShape? _cachedShape;

    public AnimatedShape(RoundedCornerShape initial, SpringSpec spec)
    {
        _start = initial ?? throw new ArgumentNullException(nameof(initial));
        _target = initial;
        _progress = new SpringAnimation(1.0, spec);
    }

    /// <summary>The animation to register with a <c>MotionGroup</c>.</summary>
    public IAnimation Progress => _progress;

    /// <summary>The shape currently being animated towards.</summary>
    public RoundedCornerShape Target => _target;

    public bool IsRunning => _progress.IsRunning;

    /// <summary>
    /// The shape as it looks right now. Memoised, because layout and render both ask for it within
    /// a frame and each call would otherwise allocate a fresh interpolation.
    /// </summary>
    public RoundedCornerShape Current
    {
        get
        {
            var progress = _progress.Value;

            if (_cachedShape is { } cached && progress.Equals(_cachedProgress))
                return cached;

            var shape = RoundedCornerShape.Lerp(_start, _target, progress);
            _cachedProgress = progress;
            _cachedShape = shape;
            return shape;
        }
    }

    /// <summary>Resolves the current shape to corner radii for a control of the given size.</summary>
    public CornerRadius ToCornerRadius(Size shapeSize, bool isRightToLeft = false)
    {
        return Current.ToCornerRadius(shapeSize, isRightToLeft);
    }

    /// <summary>Jumps to a shape without animating.</summary>
    public void SnapTo(RoundedCornerShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        _start = shape;
        _target = shape;
        _cachedShape = null;
        _progress.SnapTo(1.0);
    }

    /// <summary>Starts morphing towards a shape, or reverses if it is the one being left.</summary>
    /// <param name="spec">A spring to switch to; the current one is kept when omitted.</param>
    public void AnimateTo(RoundedCornerShape shape, SpringSpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(shape);

        if (_target.Equals(shape))
            return;

        _cachedShape = null;

        if (shape.Equals(_start))
        {
            // Reversal: keep the momentum by flipping progress and negating velocity.
            _start = _target;
            _target = shape;

            var progress = _progress.Value;
            var velocity = _progress.Velocity;

            _progress.SnapTo(1.0 - progress);
            _progress.AnimateTo(1.0, spec, -velocity);
            return;
        }

        // A new destination. Freeze what is on screen as the starting shape, taking care not to
        // nest an interpolation inside another one.
        _start = _progress.Value switch
        {
            >= 1.0 => _target,
            <= 0.0 => _start,
            var progress => RoundedCornerShape.Lerp(_start, _target, progress)
        };

        _target = shape;
        _progress.SnapTo(0.0);
        _progress.AnimateTo(1.0, spec);
    }
}

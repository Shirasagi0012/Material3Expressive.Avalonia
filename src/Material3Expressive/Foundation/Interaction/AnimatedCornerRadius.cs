using System;
using Avalonia;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;

namespace Material3Expressive.Foundation.Interaction;

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
public sealed class AnimatedCornerRadius : IDisposable
{
    private readonly VisualAnimationScope _motion;
    private readonly Animatable<float> _progress;
    private readonly Action? _onFrame;
    private SpringSpec _spec;
    private CornerRadius _start;
    private CornerRadius _target;
    private CornerRadius _requestedTarget;
    private int _generation;
    private bool _disposed;

    /// <summary>Creates a morph in its own child scope; disposing it leaves the parent active.</summary>
    public AnimatedCornerRadius(CornerRadius initial, SpringSpec spec, VisualAnimationScope parentScope,
        Action? onFrame = null)
    {
        ArgumentNullException.ThrowIfNull(parentScope);
        _motion = parentScope.CreateChild("Shape");
        _start = initial;
        _target = initial;
        _requestedTarget = initial;
        _spec = spec;
        _onFrame = onFrame;
        _progress = _motion.CreateAnimatable(1f);
    }

    /// <summary>The current interpolation fraction.</summary>
    public double Progress => _progress.Value;

    /// <summary>The corners being animated towards, before clamping.</summary>
    public CornerRadius Target => _requestedTarget;

    public bool IsRunning => _progress.IsRunning;

    /// <summary>
    /// The layout size the endpoints clamp against. A non-positive size disables clamping — the
    /// endpoints are then taken as given.
    /// </summary>
    public Size Size { get; set; }

    /// <summary>The corner radii as they look right now: endpoints clamped, then interpolated.</summary>
    public CornerRadius Current =>
        Lerp(ClampToSize(_start), ClampToSize(_target), _progress.Value);

    /// <summary>Starts morphing towards corners, or reverses if it is the one being left.</summary>
    /// <param name="corners">The corners to head for.</param>
    /// <param name="spec">A spring to switch to; the current one is kept when omitted.</param>
    public async Task AnimateToAsync(CornerRadius corners, SpringSpec? spec = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_requestedTarget.Equals(corners))
            return;

        var generation = ++_generation;
        _requestedTarget = corners;
        _spec = spec ?? _spec;
        var progress = _progress.Value;
        var velocity = _progress.Velocity;
        CornerRadius start;
        float initialProgress;
        float initialVelocity;
        if (corners.Equals(_start))
        {
            // Reversal: keep the momentum by flipping progress and negating velocity.
            start = _target;
            initialProgress = 1f - progress;
            initialVelocity = -velocity;
        }
        else
        {
            // Preserve the visible shape for a third destination; exact endpoints avoid drift.
            start = progress switch
            {
                1f => _target,
                0f => _start,
                _ => Lerp(ClampToSize(_start), ClampToSize(_target), progress)
            };
            initialProgress = 0;
            initialVelocity = 0;
        }

        // Snapping resets the progress coordinate system, not the visible shape. Publish only
        // after both endpoints and progress agree; a Value subscription would expose half an update.
        await _progress.SnapToAsync(initialProgress, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (_disposed || generation != _generation)
            throw new OperationCanceledException(new CancellationToken(true));

        _start = start;
        _target = corners;
        _onFrame?.Invoke();
        if (_disposed || generation != _generation)
            throw new OperationCanceledException(new CancellationToken(true));
        await _progress.AnimateToAsync(1f,
            new ComposeAnimation.Runtime.SpringSpec<float>(_spec.DampingRatio, _spec.Stiffness, (float)_spec.VisibilityThreshold), initialVelocity,
            block: _ => _onFrame?.Invoke(), cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _generation++;
        _motion.Dispose();
    }

    internal static CornerRadius Lerp(CornerRadius from, CornerRadius to, double progress)
    {
        // Compose's corner interpolation and progress are Float, including each lerp operation.
        var fraction = (float)progress;
        float Interpolate(double start, double end) => (1f - fraction) * (float)start + fraction * (float)end;
        return new CornerRadius(
            Interpolate(from.TopLeft, to.TopLeft),
            Interpolate(from.TopRight, to.TopRight),
            Interpolate(from.BottomRight, to.BottomRight),
            Interpolate(from.BottomLeft, to.BottomLeft));
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

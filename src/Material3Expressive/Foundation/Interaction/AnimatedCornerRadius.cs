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
/// Full (the legacy <c>CornerRadius(1000)</c>) resolves as 50% of the smaller extent.
/// Ordinary DIP corners are interpolated without clamping. The consumer normalizes the
/// resulting outline, after interpolation, using CornerBasedShape's rules.
/// </para>
/// <para>
/// Interruption splits into two cases, exactly like upstream. Returning to the shape being
/// animated away from is a reversal: progress flips and velocity negates, so the corners keep
/// their momentum instead of restarting. Heading somewhere new captures the interpolated shape,
/// retaining its percentage corners so a later resize can re-evaluate the new start.
/// </para>
/// </remarks>
public sealed class AnimatedCornerRadius : IDisposable
{
    private readonly AnimationChildScope _motion;
    private readonly Animatable<float> _progress;
    private readonly Action? _onFrame;
    private SpringSpec _spec;
    private CornerShape _start;
    private CornerShape.Defined _target;
    private CornerShape.Defined _requestedTarget;
    private int _generation;
    private bool _disposed;

    /// <summary>Creates a morph in its own child scope; disposing it leaves the parent active.</summary>
    public AnimatedCornerRadius(CornerRadius initial, SpringSpec spec, AnimationScope parentScope,
        Action? onFrame = null)
        : this(new CornerShape.Defined(initial), spec, parentScope, onFrame) { }

    internal AnimatedCornerRadius(CornerShape.Defined initial, SpringSpec spec, AnimationScope parentScope,
        Action? onFrame = null)
    {
        ArgumentNullException.ThrowIfNull(parentScope);
        _motion = parentScope.CreateChild("Shape");
        _start = initial;
        _target = initial;
        _requestedTarget = initial;
        _spec = spec;
        _onFrame = onFrame;
        _progress = new Animatable<float>(new FloatConverter(), 1f, VisibilityThresholds.Float);
    }

    /// <summary>The current interpolation fraction.</summary>
    public double Progress => _progress.Value;

    /// <summary>The original corners being animated towards.</summary>
    public CornerRadius Target => _requestedTarget.Corners;
    internal CornerShape.Defined TargetShape => _requestedTarget;

    public bool IsRunning => _progress.IsRunning;

    /// <summary>
    /// The layout size used to resolve percentage corners. With a non-positive size the
    /// legacy Full value is left unresolved.
    /// </summary>
    public Size Size { get; set; }

    /// <summary>Resolved, interpolated corners before outline normalization.</summary>
    public CornerRadius Current =>
        Lerp(_start.Resolve(Size), _target.Resolve(Size), _progress.Value);

    /// <summary>Starts morphing towards corners, or reverses if it is the one being left.</summary>
    /// <param name="corners">The corners to head for.</param>
    /// <param name="spec">A spring to switch to; the current one is kept when omitted.</param>
    public Task AnimateToAsync(CornerRadius corners, SpringSpec? spec = null,
        CancellationToken cancellationToken = default) =>
        AnimateToAsync(new CornerShape.Defined(corners), spec, cancellationToken);

    internal async Task AnimateToAsync(CornerShape.Defined corners, SpringSpec? spec = null,
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
        CornerShape start;
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
                _ => new CornerShape.Blend(_start, _target, progress)
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
        await _progress.AnimateToAsync(_motion, 1f,
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
}

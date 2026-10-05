namespace ComposeAnimation.Runtime;

/// <summary>
/// Turns a <see cref="IFloatAnimationSpec"/> into a multi-dimensional
/// <see cref="IVectorizedFiniteAnimationSpec"/> by using the same float spec on each dimension.
/// </summary>
public sealed class VectorizedFloatAnimationSpec : IVectorizedFiniteAnimationSpec
{
    private readonly IFloatAnimationSpec _anim;

    /// <summary>
    /// Creates a vectorized animation from a float animation spec. The given spec is used to
    /// animate every dimension of the vector.
    /// </summary>
    public VectorizedFloatAnimationSpec(IFloatAnimationSpec anim)
    {
        ArgumentNullException.ThrowIfNull(anim);
        _anim = anim;
    }

    /// <summary>
    /// Creates a vectorized animation from per-dimension float specs (same shape as Compose's
    /// internal <c>Animations</c> indexing).
    /// </summary>
    public VectorizedFloatAnimationSpec(IFloatAnimationSpec[] anims)
    {
        ArgumentNullException.ThrowIfNull(anims);
        ArgumentOutOfRangeException.ThrowIfLessThan(anims.Length, 1);
        _anims = anims;
        _anim = anims[0];
        _usePerDimension = true;
    }

    private readonly IFloatAnimationSpec[]? _anims;
    private readonly bool _usePerDimension;

    private IFloatAnimationSpec AnimAt(int index)
        => _usePerDimension ? _anims![index] : _anim;

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination)
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        if (_usePerDimension && _anims!.Length != destination.Length)
            throw new ArgumentException("Per-dimension float specs length must match vector dimension.");
        for (var i = 0; i < destination.Length; i++)
            destination[i] = AnimAt(i).GetValue(playTime, initial[i], target[i], initialVelocity[i]);
    }

    public void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination)
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        if (_usePerDimension && _anims!.Length != destination.Length)
            throw new ArgumentException("Per-dimension float specs length must match vector dimension.");
        for (var i = 0; i < destination.Length; i++)
            destination[i] = AnimAt(i).GetVelocity(playTime, initial[i], target[i], initialVelocity[i]);
    }

    public void GetEndVelocity(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination)
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        if (_usePerDimension && _anims!.Length != destination.Length)
            throw new ArgumentException("Per-dimension float specs length must match vector dimension.");
        for (var i = 0; i < destination.Length; i++)
            destination[i] = AnimAt(i).GetEndVelocity(initial[i], target[i], initialVelocity[i]);
    }

    public TimeSpan GetDuration(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity)
    {
        VectorValidation.Validate(initial, target, initialVelocity);
        if (_usePerDimension && _anims!.Length != initial.Length)
            throw new ArgumentException("Per-dimension float specs length must match vector dimension.");
        var maxDuration = TimeSpan.Zero;
        for (var i = 0; i < initial.Length; i++)
        {
            var duration = AnimAt(i).GetDuration(initial[i], target[i], initialVelocity[i]);
            if (duration > maxDuration)
                maxDuration = duration;
        }
        return maxDuration;
    }
}
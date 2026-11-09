using SmoothScroll.Avalonia.Interaction;

namespace Material3Expressive.Components;

internal readonly record struct PullScrollConfiguration(float Density, float ThresholdPx,
    bool IsRefreshing, bool IsEnabled, bool IsAnimating);
internal readonly record struct PullScrollSnapshot(long Sequence, float DistancePulled, float VerticalOffset, bool Publish);

// All fields and methods belong to the compositor. State methods and application
// callbacks run only after the ordered numeric snapshot reaches the UI thread.
internal sealed class PullToRefreshScrollParticipant : IScrollMovementParticipant
{
    private PullScrollConfiguration _configuration;
    private float _distancePulled, _verticalOffset;
    private readonly Action<PullScrollSnapshot> _publish;
    private long _sequence;
    private bool _publishDistance;

    internal PullToRefreshScrollParticipant(PullScrollConfiguration configuration, float distance,
        float offset, Action<PullScrollSnapshot> publish)
    {
        _configuration = configuration;
        _distancePulled = distance;
        _verticalOffset = offset;
        _publish = publish;
    }

    internal void Configure(PullScrollConfiguration configuration) => _configuration = configuration;
    internal void Reset(float distance, float offset) { _distancePulled = distance; _verticalOffset = offset; }
    private bool CanConsume(in ScrollMovementContext context) =>
        !_configuration.IsAnimating && _configuration.IsEnabled && context.Source != ScrollMovementSource.Inertia;

    public double PreScroll(in ScrollMovementContext context)
    {
        _publishDistance = false;
        return CanConsume(in context) && context.Delta > 0 ? Consume(context.Delta) : 0;
    }

    public double PostScroll(in ScrollMovementContext context, double selfConsumed)
    {
        if (!CanConsume(in context)) return 0;
        _publishDistance = true;
        return Consume(context.Delta);
    }

    private double Consume(double delta)
    {
        if (_configuration.IsRefreshing) return 0;
        var consumed = -PullToRefreshScrollDistance.Consume((float)-delta, _configuration.Density,
            _configuration.ThresholdPx, ref _distancePulled, out _verticalOffset);
        // The component retains Float physical-pixel arithmetic. Its rounded result
        // must not offer more than the original Double DIP transaction to siblings.
        return Math.Clamp((double)consumed, Math.Min(0, delta), Math.Max(0, delta));
    }

    public void OnScrollCompleted(in ScrollMovementResult result) =>
        _publish(new(++_sequence, _distancePulled, _verticalOffset, _publishDistance));

    internal sealed record Factory(PullScrollConfiguration Configuration, float Distance,
        float Offset, Action<PullScrollSnapshot> Publish) : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create() => new PullToRefreshScrollParticipant(Configuration, Distance, Offset, Publish);
    }
}

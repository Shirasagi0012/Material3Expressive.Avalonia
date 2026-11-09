using Avalonia.Controls;
using Avalonia.Data;
using Material3Expressive.Foundation.Input;
using SmoothScroll.Avalonia.Controls;
using SmoothScroll.Avalonia.Interaction;

namespace Material3Expressive.Components;

// FloatingToolbar.kt ExitAlwaysFloatingToolbarScrollBehavior observes the nested
// post-consumption and settles after the content's fling. It never consumes a delta.
internal sealed class FloatingToolbarScrollConnection : IDisposable
{
    private readonly FloatingToolbar _owner;
    private readonly FloatingToolbarScrollBehavior _behavior;
    private readonly ScrollViewerPresenter _presenter;
    private readonly Control _scrollHost;
    private readonly IDisposable _registration;
    private readonly IDisposable? _touchSlop;
    private bool _attached, _disposed;
    private long _generation;

    internal FloatingToolbarScrollConnection(FloatingToolbar owner, FloatingToolbarScrollBehavior behavior,
        ScrollViewerPresenter presenter, Control scrollHost)
    {
        _owner = owner; _behavior = behavior; _presenter = presenter; _scrollHost = scrollHost;
        _touchSlop = presenter.SetValue(ScrollPresenter.VerticalTouchSlopProperty, 18d, BindingPriority.Template);
        _registration = MaterialScrollCoordinator.Register(presenter, scrollHost, Attach);
    }

    internal bool Matches(ScrollViewerPresenter presenter, Control scrollHost) =>
        ReferenceEquals(_presenter, presenter) && ReferenceEquals(_scrollHost, scrollHost);

    private IDisposable Attach(MaterialScrollCoordinator.Registration registration)
    {
        _attached = true;
        var generation = ++_generation;
        var tracker = registration.Tracker;
        var weak = new WeakReference<FloatingToolbarScrollConnection>(this);
        void Dispatch(float? consumed) => tracker.DispatchToUI(() =>
        {
            if (!weak.TryGetTarget(out var connection) || !connection._attached || connection._disposed || connection._generation != generation) return;
            if (consumed is { } value) connection._behavior.OnPostScroll(value);
            else connection._behavior.Interrupt();
        });
        registration.Configure(new Factory(Dispatch), postFling: async (_, available, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            if (!_attached || _generation != generation) return 0;
            var offered = (float)-available;
            var consumed = await _behavior.OnPostFlingAsync(offered, ct);
            // The fixed upstream returns settleFloatingToolbar's remaining velocity
            // as the nested connection's consumption. Preserve that exact contract.
            return consumed == offered ? available : Math.Clamp(-(double)consumed, Math.Min(0, available), Math.Max(0, available));
        });
        _owner.ScrollConnectionChanged(true);
        return new Attachment(this);
    }

    private void Detach()
    {
        _attached = false;
        ++_generation;
        _behavior.Interrupt();
        _owner.ScrollConnectionChanged(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _registration.Dispose();
        _touchSlop?.Dispose();
    }

    private sealed class Attachment(FloatingToolbarScrollConnection owner) : IDisposable
    { public void Dispose() => owner.Detach(); }
    private sealed record Factory(Action<float?> Dispatch) : IScrollMovementParticipantFactory
    { public IScrollMovementParticipant Create() => new Participant(Dispatch); }
    private sealed class Participant(Action<float?> dispatch) : IScrollMovementParticipant
    {
        private float _consumed;
        public double PreScroll(in ScrollMovementContext context) => 0;
        public double PostScroll(in ScrollMovementContext context, double selfConsumed)
        { _consumed = -(float)selfConsumed; return 0; }
        public void OnScrollCompleted(in ScrollMovementResult result) => dispatch(_consumed);
        public void OnCancelled() => dispatch(null);
    }
}

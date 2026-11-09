using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SmoothScroll.Avalonia.Controls;
using SmoothScroll.Avalonia.Interaction;

namespace Material3Expressive.Foundation.Input;

// One physical viewport has one tracker attachment. Components register their own
// nested connection here; controls and async callbacks never enter the compositor.
internal sealed class MaterialScrollCoordinator
{
    private static readonly ConditionalWeakTable<ScrollViewerPresenter, MaterialScrollCoordinator> Coordinators = new();
    private readonly ScrollViewerPresenter _presenter;
    private readonly List<Registration> _registrations = [];
    private IDisposable? _attachment;
    private InteractionTracker? _tracker;
    private readonly Func<double, CancellationToken, Task<double>> _preFling;
    private readonly Func<double, double, CancellationToken, Task<double>> _postFling;

    private MaterialScrollCoordinator(ScrollViewerPresenter presenter)
    {
        _presenter = presenter;
        _preFling = PreFlingAsync;
        _postFling = PostFlingAsync;
    }

    internal static IDisposable Register(ScrollViewerPresenter presenter, Control owner,
        Func<Registration, IDisposable> attach, bool ownsBounds = false)
    {
        var coordinator = Coordinators.GetValue(presenter, static value => new(value));
        if (ownsBounds && coordinator._registrations.Any(item => item.OwnsBounds))
            throw new InvalidOperationException("A physical viewport can have only one dynamic bounds owner.");
        var registration = new Registration(coordinator, owner, attach, ownsBounds);
        coordinator._registrations.Add(registration);
        if (coordinator._attachment is null)
            coordinator._attachment = presenter.AttachVerticalScroll(coordinator.Attach);
        else if (coordinator._tracker is not null)
            registration.Attach();
        return registration;
    }

    private IDisposable Attach(InteractionTracker tracker)
    {
        _tracker = tracker;
        tracker.ConfigureVerticalScroll(new Factory(),
            delta => _registrations.Any(item => item.CanConsumeAtBoundary?.Invoke(delta) == true));
        foreach (var registration in _registrations.ToArray()) registration.Attach();
        return new Attachment(this);
    }

    private void UpdateRelease() => _tracker?.ConfigureVerticalScrollRelease(
        _registrations.Any(item => item.PreFling is not null) ? _preFling : null,
        _registrations.Any(item => item.PostFling is not null) ? _postFling : null);

    private async Task<double> PreFlingAsync(double available, CancellationToken cancellationToken)
    {
        var consumed = 0d;
        foreach (var registration in _registrations.OrderBy(item => item.Depth).ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (registration.PreFling is { } callback)
                consumed += await callback(available - consumed, cancellationToken);
        }
        return consumed;
    }

    private async Task<double> PostFlingAsync(double selfConsumed, double available, CancellationToken cancellationToken)
    {
        var consumed = 0d;
        foreach (var registration in _registrations.OrderByDescending(item => item.Depth).ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (registration.PostFling is { } callback)
                consumed += await callback(selfConsumed + consumed, available - consumed, cancellationToken);
        }
        return consumed;
    }

    private void Detach()
    {
        foreach (var registration in _registrations.ToArray()) registration.Detach();
        _tracker?.ConfigureVerticalScroll(null);
        _tracker = null;
    }

    private sealed class Attachment(MaterialScrollCoordinator owner) : IDisposable
    {
        public void Dispose() => owner.Detach();
    }

    internal sealed class Registration : IDisposable
    {
        private readonly MaterialScrollCoordinator _coordinator;
        private readonly Control _owner;
        private readonly Func<Registration, IDisposable> _attach;
        private IDisposable? _connection;
        private bool _disposed;
        internal bool OwnsBounds { get; }
        internal int Depth => _owner.GetVisualAncestors().Count();
        internal Func<double, bool>? CanConsumeAtBoundary { get; private set; }
        internal Func<double, CancellationToken, Task<double>>? PreFling { get; private set; }
        internal Func<double, double, CancellationToken, Task<double>>? PostFling { get; private set; }
        internal InteractionTracker Tracker => _coordinator._tracker ??
            throw new InvalidOperationException("The physical scroll connection is detached.");

        internal Registration(MaterialScrollCoordinator coordinator, Control owner,
            Func<Registration, IDisposable> attach, bool ownsBounds)
        {
            _coordinator = coordinator;
            _owner = owner;
            _attach = attach;
            OwnsBounds = ownsBounds;
        }

        internal void Attach() => _connection = _attach(this);
        internal void Configure(IScrollMovementParticipantFactory factory, Func<double, bool>? boundary = null,
            Func<double, CancellationToken, Task<double>>? preFling = null,
            Func<double, double, CancellationToken, Task<double>>? postFling = null)
        {
            CanConsumeAtBoundary = boundary;
            PreFling = preFling;
            PostFling = postFling;
            var depth = Depth;
            Tracker.RunOnScrollParticipant(participant =>
                ((Participants)participant).Set(this, depth, OwnsBounds, factory));
            _coordinator.UpdateRelease();
        }

        internal void Run(Action<IScrollMovementParticipant> action) =>
            Tracker.RunOnScrollParticipant(participant => ((Participants)participant).Run(this, action));

        internal void Detach()
        {
            // Remove the server participant before the component disposes composition outputs.
            _coordinator._tracker?.CancelScroll();
            _coordinator._tracker?.RunOnScrollParticipant(participant => ((Participants)participant).Remove(this));
            _connection?.Dispose();
            _connection = null;
            CanConsumeAtBoundary = null;
            PreFling = null;
            PostFling = null;
            _coordinator.UpdateRelease();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Detach();
            _coordinator._registrations.Remove(this);
            if (_coordinator._registrations.Count != 0) return;
            _coordinator._attachment?.Dispose();
            _coordinator._attachment = null;
            Coordinators.Remove(_coordinator._presenter);
        }
    }

    private sealed class Factory : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create() => new Participants();
    }

    // Ordered compositor-only list. Per-connection updates retain the other connections'
    // state, including an AppBar's running color/snap animation and live viewport bounds.
    internal sealed class Participants : IScrollMovementParticipant
    {
        private sealed record Entry(object Key, int Depth, bool OwnsBounds, IScrollMovementParticipant Value);
        private readonly List<Entry> _entries = [];

        internal void Set(object key, int depth, bool ownsBounds, IScrollMovementParticipantFactory factory)
        {
            Remove(key);
            _entries.Add(new(key, depth, ownsBounds, factory.Create()));
            _entries.Sort((left, right) => left.Depth.CompareTo(right.Depth));
        }

        internal void Run(object key, Action<IScrollMovementParticipant> action)
        {
            var entry = _entries.FirstOrDefault(item => ReferenceEquals(item.Key, key));
            if (entry is not null) action(entry.Value);
        }

        internal void Remove(object key)
        {
            var index = _entries.FindIndex(item => ReferenceEquals(item.Key, key));
            if (index < 0) return;
            _entries[index].Value.Detach();
            _entries.RemoveAt(index);
        }

        public double PreScroll(in ScrollMovementContext context)
        {
            var consumed = 0d;
            foreach (var entry in _entries)
            {
                var remaining = context with { Delta = context.Delta - consumed };
                consumed += entry.Value.PreScroll(in remaining);
            }
            return consumed;
        }

        public double PostScroll(in ScrollMovementContext context, double selfConsumed)
        {
            var consumed = 0d;
            for (var index = _entries.Count - 1; index >= 0; index--)
            {
                var remaining = context with { Delta = context.Delta - consumed };
                consumed += _entries[index].Value.PostScroll(in remaining, selfConsumed + consumed);
            }
            return consumed;
        }

        public ScrollBounds GetVerticalBounds(in ScrollMovementContext context)
        {
            foreach (var entry in _entries)
                if (entry.OwnsBounds) return entry.Value.GetVerticalBounds(in context);
            return new(context.Minimum, context.Maximum);
        }

        public void OnScrollCompleted(in ScrollMovementResult result)
        {
            foreach (var entry in _entries) entry.Value.OnScrollCompleted(in result);
        }
        public void OnPositionChanged(double position)
        {
            foreach (var entry in _entries) entry.Value.OnPositionChanged(position);
        }
        public void OnIdle(ScrollMovementSource source)
        {
            foreach (var entry in _entries) entry.Value.OnIdle(source);
        }
        public bool OnFrame(double elapsedSeconds)
        {
            var running = false;
            foreach (var entry in _entries) running |= entry.Value.OnFrame(elapsedSeconds);
            return running;
        }
        public void OnCancelled()
        {
            foreach (var entry in _entries) entry.Value.OnCancelled();
        }
        public void Detach()
        {
            foreach (var entry in _entries) entry.Value.Detach();
            _entries.Clear();
        }
    }
}

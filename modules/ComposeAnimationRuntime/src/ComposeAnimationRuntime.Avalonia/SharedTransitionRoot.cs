using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class SharedTransitionRoot : Panel
{
    internal static SharedTransitionRoot? LastCreated { get; private set; }
    public SharedParticipantRegistry Participants { get; } = new();
    public Panel ContentLayer { get; } = new()
    {
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
    };
    public Canvas Overlay { get; } = new()
    {
        IsHitTestVisible = false,
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
    };
    public event EventHandler? TransitionCompleted;
    private readonly List<SharedBounds> _activeBounds = [];
    private readonly List<SharedElement> _activeElements = [];
    private readonly List<IDisposable> _placeholderLeases = [];
    public bool IsTransitioning => _activeBounds.Any(item => item.IsTransitioning) || _activeElements.Any(item => item.IsTransitioning);

    public SharedTransitionRoot()
    {
        LastCreated = this;
        Children.Add(ContentLayer);
        Children.Add(Overlay);
        Overlay.ZIndex = int.MaxValue;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        ContentLayer.Measure(availableSize);
        Overlay.Measure(availableSize);
        return new Size(
            double.IsInfinity(availableSize.Width) ? ContentLayer.DesiredSize.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? ContentLayer.DesiredSize.Height : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var bounds = new Rect(finalSize);
        ContentLayer.Arrange(bounds);
        Overlay.Arrange(bounds);
        return finalSize;
    }

    internal void Register(object key, Control control, SharedParticipantRole role = SharedParticipantRole.Active)
    {
        ArgumentNullException.ThrowIfNull(key);
        Participants.Register(key, control, role);
    }

    internal void Unregister(object key, Control control)
    {
        Participants.Unregister(key, control);
    }

    public SharedTransitionPlan BeginTransition()
        => new(Participants.Participants.ToArray());

    public SharedTransitionTransaction BeginTransaction()
        => new(this, BeginTransition());

    public IReadOnlyList<SharedTransitionPair> RunTransition(Action updateContent)
    {
        ArgumentNullException.ThrowIfNull(updateContent);
        var plan = BeginTransition();
        updateContent();
        GetLayoutManager()?.ExecuteLayoutPass();
        var pairs = CommitTransition(plan);
        Animate(pairs);
        return pairs;
    }

    /// <summary>
    /// Captures participants, applies the content update, waits for the next
    /// layout notification, and starts the transition from the resulting
    /// destination geometry.
    /// </summary>
    public async Task<IReadOnlyList<SharedTransitionPair>> RunTransitionAsync(
        Action updateContent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateContent);
        cancellationToken.ThrowIfCancellationRequested();

        var plan = BeginTransition();
        try
        {
            updateContent();
            await WaitForLayoutAsync(cancellationToken).ConfigureAwait(true);
            var pairs = CommitTransition(plan);
            Animate(pairs);
            return pairs;
        }
        catch
        {
            CancelTransitions();
            throw;
        }
    }

    internal Task WaitForLayoutAsync(CancellationToken cancellationToken)
    {
        if (VisualRoot is null)
        {
            GetLayoutManager()?.ExecuteLayoutPass();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        CancellationTokenRegistration registration = default;
        handler = (_, _) =>
        {
            LayoutUpdated -= handler;
            registration.Dispose();
            tcs.TrySetResult();
        };
        LayoutUpdated += handler;
        registration = cancellationToken.Register(() =>
        {
            LayoutUpdated -= handler;
            tcs.TrySetCanceled(cancellationToken);
        });
        GetLayoutManager()?.ExecuteLayoutPass();
        return tcs.Task;
    }

    public IReadOnlyList<SharedTransitionPair> CommitTransition(SharedTransitionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var participants = plan.Participants
            .Where(item => Participants.Participants.Any(current =>
                Equals(current.Key, item.Key) && ReferenceEquals(current.Control, item.Control)))
            .Select(item => item with { Snapshot = LayoutSnapshot.Capture(item.Control, this) })
            .ToArray();
        var pairs = participants.GroupBy(item => item.Key)
            .Select(group => new SharedTransitionPair(group.Key,
                group.FirstOrDefault(item => item.Role == SharedParticipantRole.Source),
                group.FirstOrDefault(item => item.Role == SharedParticipantRole.Destination)))
            .ToArray();
        return pairs;
    }

    public void Animate(IReadOnlyList<SharedTransitionPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        foreach (var pair in pairs)
        {
            if (pair.Source?.Control is SharedBounds source && pair.Destination?.Control is SharedBounds destination)
            {
                if (destination.IsTransitioning)
                    destination.RetargetTo(pair.Destination.Snapshot.RootBounds);
                else
                {
                    var sourceBounds = pair.Source.Snapshot.RootBounds.Width > 0 && pair.Source.Snapshot.RootBounds.Height > 0
                        ? pair.Source.Snapshot.RootBounds : source.CurrentBounds;
                    var destinationBounds = pair.Destination.Snapshot.RootBounds.Width > 0 && pair.Destination.Snapshot.RootBounds.Height > 0
                        ? pair.Destination.Snapshot.RootBounds : destination.CurrentBounds;
                    destination.AnimateFrom(sourceBounds, destinationBounds);
                }
            }
            else if (pair.Source?.Control is SharedBounds exiting)
                exiting.AnimateExit();
            else if (pair.Destination?.Control is SharedBounds entering)
                entering.AnimateEnter();
            else if (pair.Source?.Control is SharedElement sourceElement && pair.Destination?.Control is SharedElement destinationElement)
            {
                sourceElement.AnimateTo(destinationElement, pair.Destination.Snapshot.RootBounds);
            }
            RegisterActive(pair.Source?.Control as SharedBounds);
            RegisterActive(pair.Destination?.Control as SharedBounds);
            RegisterActive(pair.Source?.Control as SharedElement);
            RegisterActive(pair.Destination?.Control as SharedElement);
            if (pair.Source?.Control is SharedBounds sourcePlaceholderBounds)
                AddPlaceholderLease(sourcePlaceholderBounds);
        }
    }

    public SharedInteractiveTransition BeginInteractiveTransition(IReadOnlyList<SharedTransitionPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        var transition = new SharedInteractiveTransition();
        foreach (var pair in pairs)
        {
            if (pair.Source?.Control is SharedBounds sourceBounds && pair.Destination?.Control is SharedBounds destinationBounds)
                transition.Add(destinationBounds, sourceBounds.CurrentBounds, pair.Destination.Snapshot.RootBounds);
            else if (pair.Source?.Control is SharedElement sourceElement && pair.Destination?.Control is SharedElement destinationElement)
            {
                sourceElement.MoveVisualToOverlay();
                transition.Add(sourceElement, sourceElement.CurrentBounds, pair.Destination.Snapshot.RootBounds);
            }
            if (pair.Source?.Control is SharedBounds sourcePlaceholderBounds)
                AddPlaceholderLease(sourcePlaceholderBounds);
        }
        transition.Settled += completed =>
        {
            foreach (var pair in pairs)
            {
                if (pair.Source?.Control is SharedElement source && pair.Destination?.Control is SharedElement destination)
                {
                    if (completed && destination.InputPolicy == SharedElementInputPolicy.TransferToDestination)
                        source.TransferVisualTo(destination);
                    else if (!completed)
                        source.RestoreVisual();
                }
            }
            ReleasePlaceholders();
        };
        return transition;
    }

    public SharedTransitionCoordinator CreateCoordinator(IReadOnlyList<SharedTransitionPair> pairs, IAnimationClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        var coordinator = new SharedTransitionCoordinator(clock);
        foreach (var pair in pairs)
        {
            if (pair.Source?.Control is SharedBounds sourceBounds) coordinator.Add(sourceBounds);
            if (pair.Destination?.Control is SharedBounds destinationBounds) coordinator.Add(destinationBounds);
            if (pair.Source?.Control is SharedElement sourceElement) coordinator.Add(sourceElement);
            if (pair.Destination?.Control is SharedElement destinationElement) coordinator.Add(destinationElement);
        }
        return coordinator;
    }

    public IReadOnlyList<SharedTransitionPlaceholder> CreatePlaceholders(IReadOnlyList<SharedTransitionPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        return pairs.Select(pair =>
        {
            var participant = pair.Source ?? pair.Destination;
            return new SharedTransitionPlaceholder(participant?.Snapshot.LocalBounds.Size ?? default, pair.Key);
        }).ToArray();
    }

    public void Complete(IReadOnlyList<SharedTransitionPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        foreach (var pair in pairs)
        {
            if (pair.Source?.Control is SharedElement source && pair.Destination?.Control is SharedElement destination)
            {
                if (destination.InputPolicy == SharedElementInputPolicy.TransferToDestination)
                    source.TransferVisualTo(destination);
                source.CompleteTransition();
                destination.CompleteTransition();
            }
            else if (pair.Source?.Control is SharedBounds sourceBounds) sourceBounds.CompleteTransition();
            else if (pair.Destination?.Control is SharedBounds destinationBounds) destinationBounds.CompleteTransition();
        }
        ReleasePlaceholders();
        RemoveCompletedActive();
        if (!IsTransitioning)
            TransitionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void PauseTransitions() { foreach (var bounds in _activeBounds) bounds.PauseTransition(); }
    public void ResumeTransitions() { foreach (var bounds in _activeBounds) bounds.ResumeTransition(); }
    public void CompleteTransitions()
    {
        foreach (var bounds in _activeBounds.ToArray()) bounds.CompleteTransition();
        foreach (var element in _activeElements.ToArray()) element.CompleteTransition();
        _activeBounds.Clear();
        _activeElements.Clear();
        ReleasePlaceholders();
        TransitionCompleted?.Invoke(this, EventArgs.Empty);
    }
    public void CancelTransitions()
    {
        foreach (var bounds in _activeBounds.ToArray()) bounds.CancelTransition();
        foreach (var element in _activeElements.ToArray()) { element.CancelTransition(); element.RestoreVisual(); }
        foreach (var bounds in _activeBounds) bounds.TransitionCompleted -= OnActiveTransitionCompleted;
        _activeBounds.Clear();
        _activeElements.Clear();
        ReleasePlaceholders();
    }

    private void RegisterActive(SharedBounds? bounds)
    {
        if (bounds is null || !bounds.IsTransitioning || _activeBounds.Contains(bounds)) return;
        _activeBounds.Add(bounds);
        bounds.TransitionCompleted += OnActiveTransitionCompleted;
    }

    public void PauseSharedElements() { foreach (var element in _activeElements) element.PauseTransition(); }
    public void ResumeSharedElements() { foreach (var element in _activeElements) element.ResumeTransition(); }
    public void CompleteSharedElements() { foreach (var element in _activeElements) element.CompleteTransition(); _activeElements.Clear(); }
    public void CancelSharedElements() { foreach (var element in _activeElements) { element.CancelTransition(); element.RestoreVisual(); } _activeElements.Clear(); }

    private void AddPlaceholderLease(SharedBounds bounds)
    {
        var lease = bounds.CreateParentPlaceholder();
        if (lease is not null) _placeholderLeases.Add(lease);
    }

    private void ReleasePlaceholders()
    {
        foreach (var lease in _placeholderLeases) lease.Dispose();
        _placeholderLeases.Clear();
    }

    private void RegisterActive(SharedElement? element)
    {
        if (element is null || _activeElements.Contains(element)) return;
        _activeElements.Add(element);
        element.TransitionCompleted += OnActiveTransitionCompleted;
    }

    private global::Avalonia.Layout.ILayoutManager? GetLayoutManager()
        => VisualRoot?.GetLayoutManager();

    private void OnActiveTransitionCompleted(object? sender, EventArgs e)
    {
        if (sender is SharedBounds bounds)
            bounds.TransitionCompleted -= OnActiveTransitionCompleted;
        if (sender is SharedElement element)
            element.TransitionCompleted -= OnActiveTransitionCompleted;
        RemoveCompletedActive();
        if (!IsTransitioning)
        {
            ReleasePlaceholders();
            TransitionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RemoveCompletedActive()
    {
        _activeBounds.RemoveAll(item => !item.IsTransitioning);
        _activeElements.RemoveAll(item => !item.IsTransitioning);
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        CancelTransitions();
        CancelSharedElements();
        ReleasePlaceholders();
        Participants.Clear();
        base.OnDetachedFromVisualTree(e);
    }
}

public sealed class SharedTransitionTransaction : IDisposable
{
    private readonly SharedTransitionRoot _root;
    private readonly SharedTransitionPlan _plan;
    private bool _completed;

    internal SharedTransitionTransaction(SharedTransitionRoot root, SharedTransitionPlan plan)
    {
        _root = root;
        _plan = plan;
    }

    public bool IsCompleted => _completed;

    public async Task<IReadOnlyList<SharedTransitionPair>> CommitAsync(
        Action updateContent,
        CancellationToken cancellationToken = default)
    {
        if (_completed)
            throw new InvalidOperationException("The transition transaction has already completed.");
        ArgumentNullException.ThrowIfNull(updateContent);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            updateContent();
            await _root.WaitForLayoutAsync(cancellationToken).ConfigureAwait(true);
            var pairs = _root.CommitTransition(_plan);
            _root.Animate(pairs);
            _completed = true;
            return pairs;
        }
        catch
        {
            _root.CancelTransitions();
            _completed = true;
            throw;
        }
    }

    public void Dispose()
    {
        if (_completed)
            return;
        _root.CancelTransitions();
        _completed = true;
    }
}

public sealed record SharedTransitionPlan(IReadOnlyList<SharedTransitionParticipant> Participants);
public enum SharedTransitionPhase { Paired, ExitOnly, EnterOnly }

public sealed record SharedTransitionPair(object Key, SharedTransitionParticipant? Source, SharedTransitionParticipant? Destination)
{
    public bool IsPaired => Source is not null && Destination is not null;
    public SharedTransitionPhase Phase => IsPaired ? SharedTransitionPhase.Paired : Source is not null ? SharedTransitionPhase.ExitOnly : SharedTransitionPhase.EnterOnly;
}

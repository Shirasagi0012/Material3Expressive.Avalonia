using Avalonia;
using Avalonia.Input;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace SmoothScroll.Avalonia.Interaction;

public partial class InteractionTracker : CompositionObject, IDisposable
{
    private int _requestId = 0;

    private readonly List<InteractionTrackerRequest> _pendingRequests = [];
    private InteractionTrackerContentBounds? _contentBounds;
    private Func<double, bool>? _canConsumeAtBoundary;
    private Func<double, CancellationToken, Task<double>>? _configuredPreFling;
    private Func<double, double, CancellationToken, Task<double>>? _configuredPostFling;
    private (int Id, CancellationTokenSource Cancellation)? _scrollRelease;

    /// <summary>Reports a fling callback failure on the UI thread. Without a subscriber,
    /// the exception is rethrown through the UI dispatcher. Failed callbacks never resume later release stages.</summary>
    public event EventHandler<Exception>? ScrollReleaseFailed;

    internal new ServerInteractionTracker Server { get; }


    internal InteractionTracker(
        Compositor compositor,
        ServerInteractionTracker server) : base(compositor, server)
    {
        Server = server;
        RunOnServerThread(serverTracker => serverTracker.AttachClient(this));
    }


    public IInteractionTrackerOwner? Owner { get; init; }

    /// <summary>
    /// Queues cancellation and participant detachment before composition-object disposal.
    /// Call on the UI thread, before disposing outputs referenced by the participant.
    /// </summary>
    public new void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (IsDisposed)
            return;
        CancelScrollRelease();
        ConfigureVerticalScroll(null);
        base.Dispose();
    }

    /// <summary>
    /// Installs a vertical scroll participant through the ordered request queue. Passing null
    /// cancels movement and detaches it. An attached participant clamps the body and excludes zoom,
    /// overscroll and inertia resting-value overrides. Absolute position updates bypass consumption.
    /// </summary>
    /// <param name="canConsumeAtBoundary">Optional UI-thread query for consumption outside the body.
    /// Positive DIP delta increases Offset. The query must not mutate state. Null or false preserves
    /// native boundary chaining; true lets the current tracker receive the input. This is a UI
    /// snapshot decision, not a guarantee of subsequent compositor consumption.</param>
    public void ConfigureVerticalScroll(IScrollMovementParticipantFactory? factory,
        Func<double, bool>? canConsumeAtBoundary = null) =>
        ConfigureVerticalScroll(factory, canConsumeAtBoundary, preFling: null);

    /// <summary>Installs a vertical movement participant and its asynchronous release callback.</summary>
    /// <param name="factory">Factory for the compositor participant; null detaches it.</param>
    /// <param name="canConsumeAtBoundary">UI-thread boundary query, in positive Offset coordinates.</param>
    /// <param name="preFling">Optional asynchronous UI-thread release callback. Positive velocity
    /// increases Offset; return the consumed velocity with the same sign and no greater magnitude.
    /// It runs after all preceding movement notifications and before inertia, including zero-velocity
    /// releases. Input, cancellation, reconfiguration and disposal cancel its token.</param>
    public void ConfigureVerticalScroll(IScrollMovementParticipantFactory? factory,
        Func<double, bool>? canConsumeAtBoundary,
        Func<double, CancellationToken, Task<double>>? preFling) =>
        ConfigureVerticalScroll(factory, canConsumeAtBoundary, preFling, postFling: null);

    /// <summary>Installs movement and release callbacks. Post-fling runs after body inertia,
    /// with the body's consumed and remaining velocities in positive Offset coordinates.
    /// It also runs for zero-velocity releases. Return consumption of the remaining velocity.</summary>
    public void ConfigureVerticalScroll(IScrollMovementParticipantFactory? factory,
        Func<double, bool>? canConsumeAtBoundary,
        Func<double, CancellationToken, Task<double>>? preFling,
        Func<double, double, CancellationToken, Task<double>>? postFling)
    {
        Dispatcher.UIThread.VerifyAccess();
        QueueRequest(new ConfigureVerticalScrollRequest(NextRequestId(), factory,
            factory is null ? null : preFling, factory is null ? null : postFling));
        HasVerticalScrollParticipant = factory is not null;
        _canConsumeAtBoundary = factory is null ? null : canConsumeAtBoundary;
        _configuredPreFling = factory is null ? null : preFling;
        _configuredPostFling = factory is null ? null : postFling;
    }

    /// <summary>Updates only the UI-thread release callback in request order, retaining the
    /// movement participant and its running state. Changing it cancels a pending release.
    /// Pass null to restore the original immediate release path.</summary>
    public void ConfigureVerticalScrollRelease(Func<double, CancellationToken, Task<double>>? preFling) =>
        ConfigureVerticalScrollRelease(preFling, postFling: null);

    /// <summary>Updates release callbacks without replacing the movement participant.
    /// Post-fling is dispatched after the last body movement snapshot and before participant idle.
    /// New input, cancellation, reconfiguration and disposal cancel either callback.</summary>
    public void ConfigureVerticalScrollRelease(Func<double, CancellationToken, Task<double>>? preFling,
        Func<double, double, CancellationToken, Task<double>>? postFling)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (!HasVerticalScrollParticipant)
            throw new InvalidOperationException("Configure a vertical scroll participant before its release callback.");
        if (Equals(_configuredPreFling, preFling) && Equals(_configuredPostFling, postFling)) return;
        _configuredPreFling = preFling;
        _configuredPostFling = postFling;
        QueueRequest(new ConfigureScrollReleaseRequest(NextRequestId(), preFling, postFling));
    }

    internal bool CanConsumeVerticalInputAtBoundary(double delta) =>
        delta != 0 && _canConsumeAtBoundary?.Invoke(delta) == true;

    /// <summary>Queues a direct vertical input delta for custom hosts (DIP).</summary>
    public void ApplyVerticalScrollDelta(double delta)
    {
        if (!double.IsFinite(delta))
            throw new ArgumentOutOfRangeException(nameof(delta));
        QueueRequest(new VerticalScrollDeltaRequest(NextRequestId(), delta));
    }

    /// <summary>Cancels current input and inertia in request order, retaining the participant.</summary>
    public void CancelScroll()
        => QueueRequest(new CancelScrollRequest(NextRequestId()));

    /// <summary>Completes explicit host input and starts optional participant settling in request order.</summary>
    public void CompleteVerticalScroll()
        => QueueRequest(new CompleteVerticalScrollRequest(NextRequestId()));

    /// <summary>Updates the existing compositor participant in request order without replacing its state.</summary>
    public void RunOnScrollParticipant(Action<IScrollMovementParticipant> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        QueueRequest(new ScrollParticipantUpdateRequest(NextRequestId(), update));
    }

    /// <summary>
    /// Posts a captured numeric snapshot to the UI through the tracker's ordered notification queue.
    /// Call only on this tracker's compositor thread; the callback itself runs on the UI thread.
    /// </summary>
    public void DispatchToUI(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Server.Compositor.VerifyAccess();
        Server.DispatchToUI(callback);
    }

    internal bool HasVerticalScrollParticipant { get; private set; }
    internal bool HasVerticalScrollRelease => _configuredPreFling is not null || _configuredPostFling is not null;

    internal void BeginScrollRelease(int id, double velocity,
        Func<double, CancellationToken, Task<double>> callback) =>
        RunScrollRelease(id, velocity, token => callback(velocity, token), isPostFling: false);

    internal void BeginScrollPostFling(int id, double consumed, double available,
        Func<double, double, CancellationToken, Task<double>> callback) =>
        RunScrollRelease(id, available, token => callback(consumed, available, token), isPostFling: true);

    private void RunScrollRelease(int id, double velocity,
        Func<CancellationToken, Task<double>> callback, bool isPostFling)
    {
        if (IsDisposed) return;
        CancelScrollRelease();
        var cancellation = new CancellationTokenSource();
        _scrollRelease = (id, cancellation);
        CompleteAsync();

        async void CompleteAsync()
        {
            try
            {
                var consumed = await callback(cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (!double.IsFinite(consumed) || consumed < Math.Min(0, velocity) - 1e-7 ||
                    consumed > Math.Max(0, velocity) + 1e-7)
                    throw new InvalidOperationException("Fling consumption must be finite, have the offered sign and not exceed the offered velocity.");
                if (!IsDisposed)
                    QueueRequest(isPostFling ? new CompleteScrollPostFlingRequest(NextRequestId(), id)
                        : new ResumeScrollReleaseRequest(NextRequestId(), id, Math.Clamp(consumed, Math.Min(0, velocity), Math.Max(0, velocity))));
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (!IsDisposed) QueueRequest(new AbortScrollReleaseRequest(NextRequestId(), id));
                if (ScrollReleaseFailed is { } failed) failed(this, error);
                else throw;
            }
            finally
            {
                if (_scrollRelease?.Id == id) _scrollRelease = null;
                cancellation.Dispose();
            }
        }
    }

    internal void CancelScrollRelease(int? id = null)
    {
        if (_scrollRelease is not { } release || id is not null && release.Id != id) return;
        _scrollRelease = null;
        release.Cancellation.Cancel();
        // CompleteAsync owns disposal after the callback acknowledges cancellation.
    }

    public int TryUpdatePosition(Vector3D value)
        => TryUpdatePosition(value, InteractionTrackerClampingOption.Auto);

    public int TryUpdatePositionBy(Vector3D amount)
        => TryUpdatePosition(Position + amount);

    public int TryUpdatePosition(Vector3D value, InteractionTrackerClampingOption option)
    {
        var id = NextRequestId();
        QueueRequest(new TryUpdatePositionRequest(id, value, option));
        return id;
    }

    public int TryUpdatePositionBy(Vector3D amount, InteractionTrackerClampingOption option)
        => TryUpdatePosition(Position + amount, option);

    public int TryUpdateScale(double scale, Vector3D centerPoint)
    {
        var id = NextRequestId();
        QueueRequest(new TryUpdateScaleRequest(id, scale, centerPoint));
        return id;
    }

    public int TryUpdatePositionWithAnimation(CompositionAnimation animation)
    {
        if (animation is not Vector3DKeyFrameAnimation and not ExpressionAnimation)
        {
            throw new ArgumentException("Only Vector3DKeyFrameAnimation and ExpressionAnimation are supported.", nameof(animation));
        }

        animation.Target = nameof(Server.Position);
        var id = NextRequestId();
        QueueRequest(new StartAnimationRequest(id, animation, null));
        return id;
    }

    public int TryUpdateScaleWithAnimation(CompositionAnimation animation, Vector3D centerPoint)
    {
        if (animation is not DoubleKeyFrameAnimation and not ExpressionAnimation)
        {
            throw new ArgumentException("Only DoubleKeyFrameAnimation and ExpressionAnimation are supported.", nameof(animation));
        }

        animation.Target = nameof(Server.Scale);
        var id = NextRequestId();
        QueueRequest(new StartAnimationRequest(id, animation, centerPoint));
        return id;
    }


    internal void BeginUserManipulation(Point position, IPointer pointer)
    {
        var id = NextRequestId();
        QueueRequest(new BeginUserManipulationRequest(id, position, pointer));
    }

    internal void CompleteUserManipulation()
    {
        var id = NextRequestId();
        QueueRequest(new CompleteManipulationRequest(id));
    }

    internal void ApplyManipulationDelta(Vector translationDelta)
    {
        var id = NextRequestId();
        QueueRequest(new ApplyManipulationDeltaRequest(id, translationDelta));
    }

    internal void StartInertia(Point linearVelocity, bool includeScaleVelocity)
    {
        var id = NextRequestId();
        QueueRequest(new StartInertiaRequest(id, linearVelocity, includeScaleVelocity));
    }

    internal void AddScaleVelocity(Point origin, double delta, bool useInertia)
    {
        var id = NextRequestId();
        QueueRequest(new AddScaleVelocityRequest(id, origin, delta, useInertia));
    }

    internal void ApplyWheelDelta(Vector delta, bool useInertia)
    {
        var id = NextRequestId();
        QueueRequest(new ApplyWheelDeltaRequest(id, delta, useInertia));
    }

    /// <summary>
    /// Retargets the current position inertia without interrupting its state or animation.
    /// </summary>
    /// <param name="value">The new resting position.</param>
    /// <returns>The identifier of the queued request.</returns>
    /// <remarks>
    /// The request is ignored when the tracker is not in its inertia state.
    /// </remarks>
    public int TryUpdatePositionInertiaRestingValue(Vector3D value)
    {
        var id = NextRequestId();
        QueueRequest(new UpdateInertiaRestingPositionRequest(id, value));
        return id;
    }

    public void ConfigurePhysics(double overscrollElasticity, double overscrollBounceRate)
    {
        var id = NextRequestId();
        QueueRequest(new ConfigurePhysicsRequest(
            id,
            Math.Clamp(overscrollElasticity, 0, 1),
            Math.Max(overscrollBounceRate, 0.01)));
    }

    /// <summary>
    /// Configures bounds calculated from content geometry at the current composition scale.
    /// Pass null to return to explicitly managed MinPosition and MaxPosition bounds.
    /// </summary>
    public void ConfigureContentBounds(InteractionTrackerContentBounds? bounds)
    {
        if (_contentBounds == bounds)
            return;

        _contentBounds = bounds;
        UpdateContentBounds(Scale);
        QueueRequest(new ConfigureContentBoundsRequest(NextRequestId(), bounds));
    }

    private void UpdateContentBounds(double scale)
    {
        // These are mirrors, not UI-authored bounds to serialize back over a newer server scale.
        if (_contentBounds is { } bounds)
            (_minPosition, _maxPosition) = bounds.Calculate(scale);
    }

    internal void RaiseValuesChanged(Vector3D position, double scale, int requestId)
    {
        // Server notifications update the local mirror and must not be serialized back to the server.
        _position = position;
        _scale = scale;
        UpdateContentBounds(scale);
        Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(position, scale, requestId));
    }

    internal void RaiseRequestIgnored(int requestId)
    {
        Owner?.RequestIgnored(this, new InteractionTrackerRequestIgnoredArgs(requestId));
    }

    internal void RaiseIdleStateEntered(int requestId, bool isFromBinding)
    {
        Owner?.IdleStateEntered(this, new InteractionTrackerIdleStateEnteredArgs(requestId, isFromBinding));
    }

    internal void RaiseInteractingStateEntered(int requestId, bool isFromBinding)
    {
        Owner?.InteractingStateEntered(this, new InteractionTrackerInteractingStateEnteredArgs(requestId, isFromBinding));
    }

    internal void RaiseCustomAnimationStateEntered()
    {
        Owner?.CustomAnimationStateEntered(this, new InteractionTrackerCustomAnimationStateEnteredArgs());
    }

    internal void RaiseInertiaStateEntered(
        Vector3D? modifiedRestingPosition,
        double? modifiedRestingScale,
        Vector3D naturalRestingPosition,
        double naturalRestingScale,
        Vector3D positionVelocityInPixelsPerSecond,
        int requestId,
        float scaleVelocityInPercentPerSecond,
        bool isInertiaFromImpulse,
        bool isFromBinding)
    {
        Owner?.InertiaStateEntered(this, new InteractionTrackerInertiaStateEnteredArgs()
        {
            ModifiedRestingPosition = modifiedRestingPosition,
            ModifiedRestingScale = modifiedRestingScale,
            NaturalRestingPosition = naturalRestingPosition,
            NaturalRestingScale = naturalRestingScale,
            PositionVelocityInPixelsPerSecond = positionVelocityInPixelsPerSecond,
            RequestId = requestId,
            ScaleVelocityInPercentPerSecond = scaleVelocityInPercentPerSecond,
            IsInertiaFromImpulse = isInertiaFromImpulse,
            IsFromBinding = isFromBinding,
        });
    }

    internal void RunOnServerThread(Action<ServerInteractionTracker> action)
    {
        if (Server.Compositor.CheckAccess())
        {
            action(Server);
            return;
        }

        Compositor.PostServerJob(() => action(Server), false);
    }

    private int NextRequestId() => Interlocked.Increment(ref _requestId);

    private void QueueRequest(InteractionTrackerRequest request)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _pendingRequests.Add(request);
        RegisterForSerialization();
    }

    partial void SerializeRequests(BatchStreamWriter writer)
    {
        writer.Write(_pendingRequests.Count);
        foreach (var request in _pendingRequests)
        {
            writer.WriteObject(request);
        }

        _pendingRequests.Clear();
    }
}

internal record InteractionTrackerRequest(int RequestId);

internal record TryUpdatePositionRequest(int RequestId, Vector3D Position, InteractionTrackerClampingOption ClampingOption) : InteractionTrackerRequest(RequestId);

internal record TryUpdateScaleRequest(int RequestId, double Scale, Vector3D CenterPoint) : InteractionTrackerRequest(RequestId);

internal record StartAnimationRequest(int RequestId, CompositionAnimation Animation, Vector3D? ScaleCenterPoint) : InteractionTrackerRequest(RequestId);

internal record BeginUserManipulationRequest(int RequestId, Point Position, IPointer Pointer) : InteractionTrackerRequest(RequestId);

internal record CompleteManipulationRequest(int RequestId) : InteractionTrackerRequest(RequestId);

internal record ApplyManipulationDeltaRequest(int RequestId, Vector TranslationDelta) : InteractionTrackerRequest(RequestId);

internal record StartInertiaRequest(int RequestId, Point LinearVelocity, bool IncludeScaleVelocity) : InteractionTrackerRequest(RequestId);

internal record AddScaleVelocityRequest(int RequestId, Point Origin, double Delta, bool UseInertia) : InteractionTrackerRequest(RequestId);

internal record ApplyWheelDeltaRequest(int RequestId, Vector Delta, bool UseInertia) : InteractionTrackerRequest(RequestId);

internal record UpdateInertiaRestingPositionRequest(int RequestId, Vector3D Position) : InteractionTrackerRequest(RequestId);

internal record ConfigurePhysicsRequest(int RequestId, double OverscrollElasticity, double OverscrollBounceRate) : InteractionTrackerRequest(RequestId);

internal record ConfigureContentBoundsRequest(int RequestId, InteractionTrackerContentBounds? Bounds) : InteractionTrackerRequest(RequestId);

internal record ConfigureVerticalScrollRequest(int RequestId, IScrollMovementParticipantFactory? Factory,
    Func<double, CancellationToken, Task<double>>? PreFling,
    Func<double, double, CancellationToken, Task<double>>? PostFling) : InteractionTrackerRequest(RequestId);
internal record VerticalScrollDeltaRequest(int RequestId, double Delta) : InteractionTrackerRequest(RequestId);
internal record CancelScrollRequest(int RequestId) : InteractionTrackerRequest(RequestId);
internal record CompleteVerticalScrollRequest(int RequestId) : InteractionTrackerRequest(RequestId);
internal record ScrollParticipantUpdateRequest(int RequestId, Action<IScrollMovementParticipant> Update) : InteractionTrackerRequest(RequestId);
internal record ResumeScrollReleaseRequest(int RequestId, int ReleaseId, double Consumed) : InteractionTrackerRequest(RequestId);
internal record CompleteScrollPostFlingRequest(int RequestId, int ReleaseId) : InteractionTrackerRequest(RequestId);
internal record AbortScrollReleaseRequest(int RequestId, int ReleaseId) : InteractionTrackerRequest(RequestId);
internal record ConfigureScrollReleaseRequest(int RequestId,
    Func<double, CancellationToken, Task<double>>? PreFling,
    Func<double, double, CancellationToken, Task<double>>? PostFling) : InteractionTrackerRequest(RequestId);

public static class CompositorExtensions
{
    extension(Compositor compositor)
    {
        public InteractionTracker CreateInteractionTracker(IInteractionTrackerOwner? owner) =>
            new(compositor, new ServerInteractionTracker(compositor.Server)) { Owner = owner };
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Input;
using SmoothScroll.Avalonia.Controls;
using SmoothScroll.Avalonia.Interaction;

namespace Material3Expressive.Components;

public partial class BottomSheet
{
    public static readonly StyledProperty<bool> AutoConnectScrollProperty =
        AvaloniaProperty.Register<BottomSheet, bool>(nameof(AutoConnectScroll), true);
    public static readonly DirectProperty<BottomSheet, bool> IsScrollConnectedProperty =
        AvaloniaProperty.RegisterDirect<BottomSheet, bool>(nameof(IsScrollConnected), owner => owner.IsScrollConnected);

    /// <summary>Connects the unique physical vertical ScrollViewer in the sheet content.
    /// Disable when a custom host dispatches nested scrolling explicitly.</summary>
    public bool AutoConnectScroll { get => GetValue(AutoConnectScrollProperty); set => SetValue(AutoConnectScrollProperty, value); }
    public bool IsScrollConnected => _isScrollConnected;
    private bool _isScrollConnected, _scanningScroll;
    private BottomSheetScrollConnection? _scrollConnection;

    private void UpdateScrollConnection(object? sender, EventArgs e)
    {
        if (_scanningScroll) return;
        _scanningScroll = true;
        try
        {
            var viewers = AutoConnectScroll ? this.GetVisualDescendants().OfType<ScrollViewer>()
                .Where(viewer => !viewer.GetVisualAncestors().TakeWhile(ancestor => ancestor != this)
                    .Any(ancestor => ancestor is ScrollViewer or BottomSheet))
                .Take(2).ToArray() : [];
            var viewer = viewers.Length == 1 ? viewers[0] : null;
            var presenter = viewer?.Presenter as ScrollViewerPresenter;
            if (viewer is null || presenter is null || viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled ||
                viewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled || float.IsNaN(State.MinimumAnchor))
            { DisconnectScroll(); return; }
            if (_scrollConnection is not null && !_scrollConnection.Matches(presenter)) DisconnectScroll();
            _scrollConnection ??= new BottomSheetScrollConnection(this, viewer, presenter);
            _scrollConnection.UpdateConfiguration();
        }
        finally { _scanningScroll = false; }
    }

    private void DisconnectScroll() { _scrollConnection?.Dispose(); _scrollConnection = null; }
    internal void ScrollConnectionChanged(bool connected) => SetAndRaise(IsScrollConnectedProperty, ref _isScrollConnected, connected);
}

// SheetDefaults.ConsumeSwipeWithinBottomSheetBoundsNestedScrollConnection. Numeric
// consumption stays on the compositor; state/confirmation/animations stay on the UI.
internal readonly record struct SheetScrollConfiguration(float Density, float Minimum, float Maximum, bool Enabled);
internal readonly record struct SheetScrollSnapshot(long Generation, long Sequence, float Offset);

internal sealed class BottomSheetScrollParticipant : IScrollMovementParticipant
{
    private SheetScrollConfiguration _configuration;
    private float _offset;
    private long _generation, _sequence;
    private readonly Action<SheetScrollSnapshot> _publish;

    internal BottomSheetScrollParticipant(SheetScrollConfiguration configuration, float offset,
        long generation, Action<SheetScrollSnapshot> publish)
    { _configuration = configuration; _offset = offset; _generation = generation; _publish = publish; }

    internal void Configure(SheetScrollConfiguration configuration) => _configuration = configuration;
    internal void SetOffset(float offset, long generation) { _offset = offset; _generation = generation; }
    public double PreScroll(in ScrollMovementContext context) => context.Delta > 0 ? Consume(in context) : 0;
    public double PostScroll(in ScrollMovementContext context, double selfConsumed) => Consume(in context);
    private double Consume(in ScrollMovementContext context)
    {
        if (!_configuration.Enabled || context.Source == ScrollMovementSource.Inertia || context.Delta == 0) return 0;
        var previous = float.IsNaN(_offset) ? 0 : _offset;
        _offset = Math.Clamp(previous + (float)(-context.Delta * _configuration.Density),
            _configuration.Minimum, _configuration.Maximum);
        var consumed = -(double)(_offset - previous) / _configuration.Density;
        return Math.Clamp(consumed, Math.Min(0, context.Delta), Math.Max(0, context.Delta));
    }
    public void OnScrollCompleted(in ScrollMovementResult result) => _publish(new(_generation, ++_sequence, _offset));

    internal sealed record Factory(SheetScrollConfiguration Configuration, float Offset, long Generation,
        Action<SheetScrollSnapshot> Publish) : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create() => new BottomSheetScrollParticipant(Configuration, Offset, Generation, Publish);
    }
}

internal sealed class BottomSheetScrollConnection : IDisposable
{
    private readonly BottomSheet _owner;
    private readonly SheetState _state;
    private readonly ScrollViewer _viewer;
    private readonly ScrollViewerPresenter _presenter;
    private readonly IDisposable _registration;
    private readonly IDisposable? _touchSlop;
    private MaterialScrollCoordinator.Registration? _participant;
    private SheetScrollConfiguration? _configuration;
    private long _generation, _lastSequence;
    private bool _mirroring, _disposed;

    internal BottomSheetScrollConnection(BottomSheet owner, ScrollViewer viewer, ScrollViewerPresenter presenter)
    {
        _owner = owner; _state = owner.State; _viewer = viewer; _presenter = presenter;
        _touchSlop = presenter.SetValue(ScrollPresenter.VerticalTouchSlopProperty, 18d, BindingPriority.Template);
        viewer.AddHandler(InputElement.PointerPressedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        viewer.AddHandler(InputElement.PointerMovedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        viewer.AddHandler(InputElement.PointerWheelChangedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        _registration = MaterialScrollCoordinator.Register(presenter, owner, Attach);
    }
    internal bool Matches(ScrollViewerPresenter presenter) => ReferenceEquals(_presenter, presenter);
    internal bool Contains(object? source) => _participant is not null && source is Visual visual &&
        (visual == _viewer || visual.GetVisualAncestors().Contains(_viewer));

    private IDisposable Attach(MaterialScrollCoordinator.Registration registration)
    {
        _participant = registration; ++_generation; _lastSequence = 0;
        var tracker = registration.Tracker;
        var configuration = ReadConfiguration(); _configuration = configuration;
        var weak = new WeakReference<BottomSheetScrollConnection>(this);
        void Publish(SheetScrollSnapshot snapshot) => tracker.DispatchToUI(() =>
        {
            if (!weak.TryGetTarget(out var connection) || connection._disposed ||
                snapshot.Generation != connection._generation || snapshot.Sequence <= connection._lastSequence) return;
            connection._lastSequence = snapshot.Sequence;
            connection._mirroring = true;
            try { connection._state.ApplyScrollOffset(snapshot.Offset); }
            finally { connection._mirroring = false; }
            // A binding can synchronously start a new programmatic state operation.
            if (connection._state.OffsetPx != snapshot.Offset) connection.StateChanged();
        });
        registration.Configure(new BottomSheetScrollParticipant.Factory(configuration, _state.OffsetPx, _generation, Publish),
            delta => _owner.GesturesEnabled && _owner.IsEffectivelyEnabled &&
                (delta > 0 ? _state.OffsetPx > _state.MinimumAnchor : _state.OffsetPx < _state.MaximumAnchor),
            PreFlingAsync, PostFlingAsync);
        _owner.ScrollConnectionChanged(true);
        return new Attachment(this);
    }

    private async Task<double> PreFlingAsync(double available, CancellationToken cancellationToken) =>
        !_owner.IsEffectivelyEnabled ? 0 : -await _owner.OnPreFlingAsync(-available, cancellationToken);
    private async Task<double> PostFlingAsync(double consumed, double available, CancellationToken cancellationToken)
    {
        if (!_owner.IsEffectivelyEnabled) return 0;
        var result = -await _owner.OnPostFlingAsync(-available, cancellationToken);
        return Math.Clamp(result, Math.Min(0, available), Math.Max(0, available));
    }

    private SheetScrollConfiguration ReadConfiguration() => new((float)LayoutHelper.GetLayoutScale(_owner),
        _state.MinimumAnchor, _state.MaximumAnchor, _owner.GesturesEnabled && _owner.IsEffectivelyEnabled);
    private void BeforeInput(object? sender, PointerEventArgs e) => UpdateConfiguration();
    internal void UpdateConfiguration()
    {
        if (_participant is null || _disposed) return;
        var configuration = ReadConfiguration();
        if (configuration == _configuration) return;
        var densityChanged = _configuration?.Density != configuration.Density;
        _configuration = configuration;
        _participant.Run(participant => ((BottomSheetScrollParticipant)participant).Configure(configuration));
        // A density change can preserve the public DIP offset while changing its
        // Float pixel value, so CurrentOffset's notification alone is insufficient.
        if (densityChanged) StateChanged();
    }
    internal void StateChanged()
    {
        if (_participant is null || _mirroring || _disposed) return;
        var offset = _state.OffsetPx; var generation = ++_generation;
        _participant.Run(participant => ((BottomSheetScrollParticipant)participant).SetOffset(offset, generation));
    }
    private void Detach()
    {
        ++_generation; _participant = null; _configuration = null;
        _owner.ScrollConnectionChanged(false);
    }
    private sealed class Attachment(BottomSheetScrollConnection owner) : IDisposable
    { public void Dispose() => owner.Detach(); }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _viewer.RemoveHandler(InputElement.PointerPressedEvent, BeforeInput);
        _viewer.RemoveHandler(InputElement.PointerMovedEvent, BeforeInput);
        _viewer.RemoveHandler(InputElement.PointerWheelChangedEvent, BeforeInput);
        _registration.Dispose(); _touchSlop?.Dispose();
    }
}

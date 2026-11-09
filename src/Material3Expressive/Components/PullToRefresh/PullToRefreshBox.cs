using System.ComponentModel;
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

namespace Material3Expressive.Components;

/// <summary>A scrollable content container that requests refresh after a downward pull at its start.</summary>
/// <remarks>Automatically connects the unique physical Material ScrollViewer in its content.
/// RefreshEnabled gates pulling independently of the enabled state of the content.</remarks>
public sealed class PullToRefreshBox : ContentControl, IPullToRefreshScrollHost
{
    public static readonly StyledProperty<IPullToRefreshState?> StateProperty =
        AvaloniaProperty.Register<PullToRefreshBox, IPullToRefreshState?>(nameof(State));
    public static readonly StyledProperty<bool> IsRefreshingProperty =
        AvaloniaProperty.Register<PullToRefreshBox, bool>(nameof(IsRefreshing));
    public static readonly StyledProperty<bool> RefreshEnabledProperty =
        AvaloniaProperty.Register<PullToRefreshBox, bool>(nameof(RefreshEnabled), true);
    public static readonly StyledProperty<double> RefreshThresholdProperty =
        AvaloniaProperty.Register<PullToRefreshBox, double>(nameof(RefreshThreshold));
    public static readonly StyledProperty<Control?> IndicatorProperty =
        AvaloniaProperty.Register<PullToRefreshBox, Control?>(nameof(Indicator));
    public static readonly DirectProperty<PullToRefreshBox, IPullToRefreshState> ActualStateProperty =
        AvaloniaProperty.RegisterDirect<PullToRefreshBox, IPullToRefreshState>(nameof(ActualState), owner => owner.ActualState);
    public static readonly DirectProperty<PullToRefreshBox, Control> ActualIndicatorProperty =
        AvaloniaProperty.RegisterDirect<PullToRefreshBox, Control>(nameof(ActualIndicator), owner => owner.ActualIndicator);
    public static readonly DirectProperty<PullToRefreshBox, bool> IsScrollConnectedProperty =
        AvaloniaProperty.RegisterDirect<PullToRefreshBox, bool>(nameof(IsScrollConnected), owner => owner.IsScrollConnected);

    public IPullToRefreshState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public bool IsRefreshing { get => GetValue(IsRefreshingProperty); set => SetValue(IsRefreshingProperty, value); }
    public bool RefreshEnabled { get => GetValue(RefreshEnabledProperty); set => SetValue(RefreshEnabledProperty, value); }
    public double RefreshThreshold { get => GetValue(RefreshThresholdProperty); set => SetValue(RefreshThresholdProperty, value); }
    /// <summary>Custom overlay content. Null uses the circular indicator bound to this box's state.</summary>
    public Control? Indicator { get => GetValue(IndicatorProperty); set => SetValue(IndicatorProperty, value); }
    public IPullToRefreshState ActualState => State ?? _localState;
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Control ActualIndicator => Indicator ?? _defaultIndicator;
    public bool IsScrollConnected => _isScrollConnected;
    public event EventHandler? RefreshRequested;

    private readonly PullToRefreshState _localState = new();
    private readonly PullToRefreshIndicator _defaultIndicator = new();
    private readonly PullToRefreshScrollBehavior _behavior = new();
    private IPullToRefreshState? _attachedState;
    private PullToRefreshScrollConnection? _connection;
    private bool _scanning;
    private bool _isScrollConnected;
    Control IPullToRefreshScrollHost.Control => this;
    IPullToRefreshState IPullToRefreshScrollHost.EffectiveState => ActualState;
    internal PullToRefreshScrollBehavior ScrollBehavior => _behavior;

    static PullToRefreshBox()
    {
        TemplateProperty.OverrideDefaultValue<PullToRefreshBox>(null);
        HorizontalContentAlignmentProperty.OverrideDefaultValue<PullToRefreshBox>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<PullToRefreshBox>(VerticalAlignment.Stretch);
    }

    public PullToRefreshBox()
    {
        _defaultIndicator.Bind(PullToRefreshIndicatorBox.StateProperty, this.GetObservable(ActualStateProperty));
        _defaultIndicator.Bind(PullToRefreshIndicatorBox.IsRefreshingProperty, this.GetObservable(IsRefreshingProperty));
        _behavior.RefreshRequested += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        _behavior.DistanceReset += () => _connection?.ResetDistance();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachState();
        _behavior.Attach(this);
        LayoutUpdated += UpdateConnection;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= UpdateConnection;
        Disconnect();
        _behavior.Detach(this);
        DetachState();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Disconnect();
        base.OnApplyTemplate(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IndicatorProperty)
            RaisePropertyChanged(ActualIndicatorProperty,
                change.GetOldValue<Control?>() ?? _defaultIndicator, ActualIndicator);
        if (change.Property == StateProperty)
        {
            RaisePropertyChanged(ActualStateProperty,
                change.GetOldValue<IPullToRefreshState?>() ?? _localState, ActualState);
            if (this.IsAttachedToVisualTree())
            {
                Disconnect();
                _behavior.StateChanged(this);
                DetachState();
                AttachState();
                InvalidateArrange();
            }
        }
        if (change.Property == RefreshEnabledProperty) _behavior.IsEnabled = RefreshEnabled;
        if (change.Property == IsRefreshingProperty && this.IsAttachedToVisualTree()) _behavior.RefreshingChanged();
        if (change.Property == IsRefreshingProperty || change.Property == RefreshEnabledProperty ||
            change.Property == RefreshThresholdProperty) _connection?.UpdateConfiguration();
    }

    private void AttachState()
    {
        _attachedState = ActualState;
        if (_attachedState is PullToRefreshState builtIn) builtIn.Attach(this);
        _attachedState.PropertyChanged += StateChanged;
    }

    private void DetachState()
    {
        if (_attachedState is not { } state) return;
        state.PropertyChanged -= StateChanged;
        if (state is PullToRefreshState builtIn) builtIn.Detach(this);
        _attachedState = null;
    }

    private void StateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(IPullToRefreshState.IsAnimating))
            _connection?.UpdateConfiguration();
    }

    private void UpdateConnection(object? sender, EventArgs e)
    {
        if (_scanning) return;
        _scanning = true;
        try
        {
            var viewers = this.GetVisualDescendants().OfType<ScrollViewer>()
                .Where(viewer => !viewer.GetVisualAncestors().TakeWhile(ancestor => ancestor != this)
                    .Any(ancestor => ancestor is ScrollViewer or PullToRefreshBox))
                .Take(2).ToArray();
            var viewer = viewers.Length == 1 ? viewers[0] : null;
            var presenter = viewer?.GetVisualDescendants().OfType<ScrollViewerPresenter>()
                .FirstOrDefault(value => value.TemplatedParent == viewer);
            if (viewer is null || presenter is null || viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled ||
                viewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
            {
                Disconnect();
                return;
            }
            if (_connection is not null && !_connection.Matches(presenter)) Disconnect();
            _connection ??= new(this, viewer, presenter, _behavior);
            _connection.UpdateConfiguration();
        }
        finally { _scanning = false; }
    }

    private void Disconnect()
    {
        _connection?.Dispose();
        _connection = null;
    }

    internal void ConnectionChanged(bool connected) =>
        SetAndRaise(IsScrollConnectedProperty, ref _isScrollConnected, connected);
}

internal sealed class PullToRefreshScrollConnection : IDisposable
{
    private readonly PullToRefreshBox _owner;
    private readonly ScrollViewer _viewer;
    private readonly ScrollViewerPresenter _presenter;
    private readonly PullToRefreshScrollBehavior _behavior;
    private readonly IDisposable _registration;
    private readonly IDisposable? _touchSlopContribution;
    private MaterialScrollCoordinator.Registration? _participant;
    private PullScrollConfiguration? _configuration;
    private long _generation;
    private long _lastSequence;
    private bool _disposed;
    internal bool IsAttached => _participant is not null;

    internal PullToRefreshScrollConnection(PullToRefreshBox owner, ScrollViewer viewer,
        ScrollViewerPresenter presenter, PullToRefreshScrollBehavior behavior)
    {
        _owner = owner; _viewer = viewer; _presenter = presenter; _behavior = behavior;
        _touchSlopContribution = presenter.SetValue(ScrollPresenter.VerticalTouchSlopProperty, 18d, BindingPriority.Template);
        viewer.AddHandler(InputElement.PointerPressedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        viewer.AddHandler(InputElement.PointerMovedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        viewer.AddHandler(InputElement.PointerWheelChangedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        _registration = MaterialScrollCoordinator.Register(presenter, owner, Attach);
    }

    internal bool Matches(ScrollViewerPresenter presenter) => ReferenceEquals(_presenter, presenter);

    private IDisposable Attach(MaterialScrollCoordinator.Registration registration)
    {
        _participant = registration;
        var generation = ++_generation;
        _lastSequence = 0;
        var tracker = registration.Tracker;
        var configuration = ReadConfiguration();
        _configuration = configuration;
        var weak = new WeakReference<PullToRefreshScrollConnection>(this);
        void Publish(PullScrollSnapshot snapshot) => tracker.DispatchToUI(() =>
        {
            if (weak.TryGetTarget(out var owner) && !owner._disposed && generation == owner._generation &&
                snapshot.Sequence > owner._lastSequence)
            {
                owner._lastSequence = snapshot.Sequence;
                owner._behavior.ApplyScrollSnapshot(snapshot.DistancePulled, snapshot.VerticalOffset, snapshot.Publish);
            }
        });
        registration.Configure(new PullToRefreshScrollParticipant.Factory(configuration,
                _behavior.DistancePulled, _behavior.VerticalOffset, Publish),
            delta => _owner.RefreshEnabled && !_owner.IsRefreshing && !_owner.ActualState.IsAnimating &&
                     (delta < 0 || delta > 0 && _behavior.DistancePulled > 0),
            ConsumeReleaseAsync);
        _owner.ConnectionChanged(true);
        return new Attachment(this);
    }

    internal async Task<double> ConsumeReleaseAsync(double velocity, CancellationToken cancellationToken)
    {
        var offered = (float)-velocity;
        var consumed = await _behavior.OnPreFlingAsync(offered, cancellationToken);
        // Pull consumes either zero or the whole offered velocity. Preserve that
        // relation across Float conversion instead of returning its rounded magnitude.
        return consumed == offered && offered != 0 ? velocity :
            Math.Clamp(-(double)consumed, Math.Min(0, velocity), Math.Max(0, velocity));
    }

    private PullScrollConfiguration ReadConfiguration()
    {
        var density = (float)LayoutHelper.GetLayoutScale(_owner);
        return new(density, MathF.Floor((float)_owner.RefreshThreshold * density + .5f),
            _owner.IsRefreshing, _owner.RefreshEnabled, _owner.ActualState.IsAnimating);
    }

    private void BeforeInput(object? sender, PointerEventArgs e) => UpdateConfiguration();

    internal void UpdateConfiguration()
    {
        if (_participant is null || _disposed) return;
        var configuration = ReadConfiguration();
        if (configuration == _configuration) return;
        _configuration = configuration;
        _participant.Run(participant => ((PullToRefreshScrollParticipant)participant).Configure(configuration));
    }

    internal void ResetDistance()
    {
        if (_participant is null || _disposed) return;
        var distance = _behavior.DistancePulled;
        var offset = _behavior.VerticalOffset;
        _participant.Run(participant => ((PullToRefreshScrollParticipant)participant).Reset(distance, offset));
    }

    private void Detach()
    {
        ++_generation;
        _participant = null;
        _configuration = null;
        _owner.ConnectionChanged(false);
    }

    private sealed class Attachment(PullToRefreshScrollConnection owner) : IDisposable
    {
        public void Dispose() => owner.Detach();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _viewer.RemoveHandler(InputElement.PointerPressedEvent, BeforeInput);
        _viewer.RemoveHandler(InputElement.PointerMovedEvent, BeforeInput);
        _viewer.RemoveHandler(InputElement.PointerWheelChangedEvent, BeforeInput);
        _registration.Dispose();
        _touchSlopContribution?.Dispose();
    }
}

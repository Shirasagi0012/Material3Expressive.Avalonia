// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SnackbarHost.kt:49-177,
// 260-314. Compose's suspending Mutex queue is adapted to a FIFO task queue; cancellation removes
// the current or queued request, while Avalonia exposes the current state through notifications.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Threading;
using Material3Expressive.Controls.Automation;

namespace Material3Expressive.Controls;

/// <summary>Possible durations for a hosted snackbar.</summary>
public enum SnackbarDuration
{
    Short,
    Long,
    Indefinite
}

/// <summary>The result returned when a hosted snackbar leaves the queue.</summary>
public enum SnackbarResult
{
    Dismissed,
    ActionPerformed
}

/// <summary>Immutable visual and timing data for one snackbar request.</summary>
public sealed record SnackbarVisuals(
    string Message,
    string? ActionLabel = null,
    bool WithDismissAction = false,
    SnackbarDuration Duration = SnackbarDuration.Short);

/// <summary>
/// The currently displayed snackbar and the operations that complete its request.
/// </summary>
public sealed class SnackbarData
{
    private readonly SnackbarHostState _owner;

    internal SnackbarData(SnackbarHostState owner, SnackbarVisuals visuals)
    {
        _owner = owner;
        Visuals = visuals;
    }

    public SnackbarVisuals Visuals { get; }

    public void PerformAction() => _owner.Complete(this, SnackbarResult.ActionPerformed);

    public void Dismiss() => _owner.Complete(this, SnackbarResult.Dismissed);

}

/// <summary>
/// FIFO state for a <see cref="MaterialSnackbarHost" />. Only one item is current at a time.
/// </summary>
public sealed class SnackbarHostState : INotifyPropertyChanged
{
    private sealed class Pending
    {
        public required SnackbarVisuals Visuals { get; init; }
        public required TaskCompletionSource<SnackbarResult> Completion { get; init; }
        public CancellationTokenRegistration Cancellation { get; set; }
        public SnackbarData? Data { get; set; }
    }

    private readonly object _gate = new();
    private readonly Queue<Pending> _queue = new();
    private Pending? _current;
    private SnackbarData? _currentSnackbarData;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SnackbarData? CurrentSnackbarData
    {
        get => _currentSnackbarData;
        private set
        {
            if (ReferenceEquals(_currentSnackbarData, value))
                return;
            _currentSnackbarData = value;
            PropertyChanged?.Invoke(this, new(nameof(CurrentSnackbarData)));
        }
    }

    public Task<SnackbarResult> ShowSnackbarAsync(
        string message,
        string? actionLabel = null,
        bool withDismissAction = false,
        SnackbarDuration? duration = null,
        CancellationToken cancellationToken = default) =>
        ShowSnackbarAsync(
            new SnackbarVisuals(
                message,
                actionLabel,
                withDismissAction,
                duration ?? (actionLabel is null
                    ? SnackbarDuration.Short
                    : SnackbarDuration.Indefinite)),
            cancellationToken);

    public Task<SnackbarResult> ShowSnackbar(
        SnackbarVisuals visuals,
        CancellationToken cancellationToken = default) =>
        ShowSnackbarAsync(visuals, cancellationToken);

    public Task<SnackbarResult> ShowSnackbarAsync(
        SnackbarVisuals visuals,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visuals);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<SnackbarResult>(cancellationToken);

        var pending = new Pending
        {
            Visuals = visuals,
            Completion = new(TaskCreationOptions.RunContinuationsAsynchronously)
        };

        var shouldPublish = false;
        lock (_gate)
        {
            _queue.Enqueue(pending);
            if (_current is null)
            {
                _current = _queue.Dequeue();
                _current.Data = new SnackbarData(this, _current.Visuals);
                shouldPublish = true;
            }
        }

        pending.Cancellation = cancellationToken.Register(
            static state =>
            {
                var tuple = ((SnackbarHostState Owner, Pending Pending, CancellationToken Token))state!;
                tuple.Owner.Cancel(tuple.Pending, tuple.Token);
            },
            (this, pending, cancellationToken));

        if (shouldPublish)
            PublishCurrent();
        return pending.Completion.Task;
    }

    internal void Complete(SnackbarData data, SnackbarResult result)
    {
        Pending? completed = null;
        lock (_gate)
        {
            if (_current?.Data is not { } current || !ReferenceEquals(current, data))
                return;
            completed = _current;
            _current = _queue.Count == 0 ? null : _queue.Dequeue();
            if (_current is not null)
                _current.Data = new SnackbarData(this, _current.Visuals);
        }

        completed.Cancellation.Dispose();
        completed.Completion.TrySetResult(result);
        PublishCurrent();
    }

    private void Cancel(Pending pending, CancellationToken cancellationToken)
    {
        var wasCurrent = false;
        lock (_gate)
        {
            if (ReferenceEquals(_current, pending))
                wasCurrent = true;
            else if (_queue.Contains(pending))
            {
                var remaining = _queue.Where(item => !ReferenceEquals(item, pending)).ToArray();
                _queue.Clear();
                foreach (var item in remaining)
                    _queue.Enqueue(item);
            }
            else
                return;
        }

        if (wasCurrent)
        {
            CompleteCanceledCurrent(pending, cancellationToken);
        }
        else
        {
            pending.Cancellation.Dispose();
            pending.Completion.TrySetCanceled(cancellationToken);
        }
    }

    private void CompleteCanceledCurrent(Pending pending, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_current, pending))
                return;
            _current = _queue.Count == 0 ? null : _queue.Dequeue();
            if (_current is not null)
                _current.Data = new SnackbarData(this, _current.Visuals);
        }

        pending.Cancellation.Dispose();
        pending.Completion.TrySetCanceled(cancellationToken);
        PublishCurrent();
    }

    private void PublishCurrent()
    {
        SnackbarData? data;
        lock (_gate)
            data = _current?.Data;
        CurrentSnackbarData = data;
    }
}

/// <summary>Hosts the current snackbar and advances the state queue after its duration.</summary>
public sealed class MaterialSnackbarHost : ContentControl
{
    public static readonly DirectProperty<MaterialSnackbarHost, SnackbarHostState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialSnackbarHost, SnackbarHostState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    private readonly DispatcherTimer _timer = new();
    private SnackbarHostState _state = new();
    private SnackbarData? _current;
    private MaterialSnackbar? _snackbar;

    public MaterialSnackbarHost()
    {
        _timer.Tick += OnTimerTick;
        _state.PropertyChanged += OnStateChanged;
        UpdateCurrent(_state.CurrentSnackbarData);
    }

    public SnackbarHostState State
    {
        get => _state;
        set => SetState(value);
    }

    public void ResetState() => SetState(null);

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialSnackbarHostAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty)
            UpdateCurrent(_state.CurrentSnackbarData);
    }

    private void SetState(SnackbarHostState? value)
    {
        var next = value ?? new SnackbarHostState();
        if (ReferenceEquals(_state, next))
            return;
        _state.PropertyChanged -= OnStateChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStateChanged;
        UpdateCurrent(_state.CurrentSnackbarData);
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SnackbarHostState.CurrentSnackbarData))
            UpdateCurrent(_state.CurrentSnackbarData);
    }

    private void UpdateCurrent(SnackbarData? data)
    {
        _timer.Stop();
        _current = data;
        if (_snackbar is not null)
            _snackbar.PropertyChanged -= OnSnackbarPropertyChanged;

        if (data is null)
        {
            _snackbar = null;
            Content = null;
            return;
        }

        var snackbar = new MaterialSnackbar
        {
            Name = "snackbar",
            IsOpen = true,
            Duration = TimeSpan.Zero,
            Message = new TextBlock
            {
                Name = "message",
                Text = data.Visuals.Message,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            }
        };
        snackbar.Action = CreateAction(data, snackbar.ActionBrush);
        _snackbar = snackbar;
        _snackbar.PropertyChanged += OnSnackbarPropertyChanged;
        Content = _snackbar;
        var duration = data.Visuals.Duration switch
        {
            SnackbarDuration.Long => TimeSpan.FromSeconds(10),
            SnackbarDuration.Indefinite => Timeout.InfiniteTimeSpan,
            _ => TimeSpan.FromSeconds(4)
        };
        if (duration != Timeout.InfiniteTimeSpan)
        {
            _timer.Interval = duration;
            _timer.Start();
        }
    }

    private object? CreateAction(SnackbarData data, Avalonia.Media.IBrush actionBrush)
    {
        var label = data.Visuals.ActionLabel;
        if (label is null && !data.Visuals.WithDismissAction)
            return null;
        return new Button
        {
            Name = "action-button",
            Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = default,
            Padding = default,
            MinWidth = 0,
            MinHeight = 0,
            Foreground = actionBrush,
            Content = new TextBlock
            {
                Name = "action",
                Text = label ?? "Dismiss",
                Foreground = actionBrush,
                Classes = { "label-large" }
            },
            Command = new ActionCommand(
                label is null ? data.Dismiss : data.PerformAction)
        };
    }

    private void OnSnackbarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialSnackbar.IsOpenProperty && _snackbar?.IsOpen == false)
            _current?.Dismiss();
    }

    private void OnTimerTick(object? sender, EventArgs e) => _current?.Dismiss();

    private sealed class ActionCommand(Action execute) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}

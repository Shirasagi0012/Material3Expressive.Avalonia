// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SnackbarHost.kt:49-177,
// 217-239, 298-419 and Snackbar.kt:222-281. Compose's suspending Mutex queue is adapted to a FIFO
// task queue; cancellation removes the current or queued request. Host items retain the upstream
// FastEffects opacity plus FastSpatial 0.8-to-1 scale transition, and their timeout is driven by
// the same component clock so animation, dismissal, and deterministic replay remain coordinated.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
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

    private readonly MaterialThemeWatcher _theme;
    private readonly Grid _transitionLayer = new();
    private readonly List<TransitionItem> _items = [];
    private SnackbarHostState _state = new();
    private SnackbarData? _current;
    private TransitionItem? _currentItem;
    private bool _completingTimeout;

    static MaterialSnackbarHost()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSnackbarHost>(false);
    }

    public MaterialSnackbarHost()
    {
        _theme = new MaterialThemeWatcher(this);
        _state.PropertyChanged += OnStateChanged;
        UpdateCurrent(_state.CurrentSnackbarData);
    }

    public SnackbarHostState State
    {
        get => _state;
        set => SetState(value);
    }

    public void ResetState() => SetState(null);

    internal MaterialSnackbar? CurrentSnackbar => _currentItem?.Snackbar;

    internal int TransitionItemCount => _items.Count;

    internal double CurrentOpacity => _currentItem?.OpacityValue ?? 0;

    internal double CurrentScale => _currentItem?.ScaleValue ?? .8;

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
        _current = data;
        if (_currentItem is not null)
        {
            _currentItem.Snackbar.PropertyChanged -= OnSnackbarPropertyChanged;
            _currentItem.Hide(_completingTimeout);
            _currentItem = null;
        }

        if (data is null)
            return;

        var snackbar = new MaterialSnackbar
        {
            Name = "snackbar",
            Margin = new Thickness(12),
            IsOpen = true,
            Duration = TimeSpan.Zero,
            Message = new TextBlock
            {
                Name = "message",
                Text = data.Visuals.Message,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            }
        };
        snackbar.Action = CreateAction(data, snackbar);
        snackbar.SnapOpenState(true);

        var duration = data.Visuals.Duration switch
        {
            SnackbarDuration.Long => TimeSpan.FromSeconds(10),
            SnackbarDuration.Indefinite => Timeout.InfiniteTimeSpan,
            _ => TimeSpan.FromSeconds(4)
        };
        var item = new TransitionItem(
            snackbar,
            _theme.Spring(MotionRole.FastEffects),
            _theme.Spring(MotionRole.FastSpatial),
            duration,
            () => OnTimeout(data),
            RemoveItem);
        _items.Add(item);
        _transitionLayer.Children.Add(item);
        _currentItem = item;
        snackbar.PropertyChanged += OnSnackbarPropertyChanged;
        Content = _transitionLayer;
        item.Show();
    }

    private object? CreateAction(SnackbarData data, MaterialSnackbar snackbar)
    {
        var label = data.Visuals.ActionLabel;
        if (label is null && !data.Visuals.WithDismissAction)
            return null;
        var actionBrush = snackbar.ActionBrush;
        var text = new TextBlock
        {
            Name = "action",
            Text = label ?? "Dismiss",
            Foreground = actionBrush,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        return new Button
        {
            Name = "action-button",
            Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = default,
            // Avalonia centers content inside MinHeight; adding Compose's separate 8dp vertical
            // content padding here would apply that inset twice in the stock Button template.
            Padding = new Thickness(12, 0),
            MinWidth = 58,
            MinHeight = 40,
            Foreground = actionBrush,
            Content = text,
            Command = new ActionCommand(
                label is null ? data.Dismiss : data.PerformAction)
        };
    }

    private void OnSnackbarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialSnackbar.IsOpenProperty &&
            sender is MaterialSnackbar snackbar &&
            snackbar.IsOpen == false)
            _current?.Dismiss();
    }

    private void OnTimeout(SnackbarData data)
    {
        if (ReferenceEquals(_current, data))
        {
            _completingTimeout = true;
            try
            {
                data.Dismiss();
            }
            finally
            {
                _completingTimeout = false;
            }
        }
    }

    private void RemoveItem(TransitionItem item)
    {
        item.Snackbar.PropertyChanged -= OnSnackbarPropertyChanged;
        item.Child = null;
        _items.Remove(item);
        _transitionLayer.Children.Remove(item);
        if (_items.Count == 0)
            Content = null;
    }

    private sealed class TransitionItem : Decorator
    {
        private readonly MotionGroup _motion;
        private readonly SpringAnimation _opacity;
        private readonly SpringAnimation _scale;
        private readonly DelayAnimation _exitFrameDelay;
        private readonly DelayAnimation? _timeout;
        private readonly Action _timedOut;
        private readonly Action<TransitionItem> _remove;
        private readonly ScaleTransform _scaleTransform = new(.8, .8);
        private bool _isVisible;
        private bool _hideOnNextFrame;
        private bool _timeoutRaised;

        public TransitionItem(
            MaterialSnackbar snackbar,
            SpringSpec opacitySpec,
            SpringSpec scaleSpec,
            TimeSpan duration,
            Action timedOut,
            Action<TransitionItem> remove)
        {
            Snackbar = snackbar;
            _timedOut = timedOut;
            _remove = remove;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Bottom;
            RenderTransformOrigin = RelativePoint.Center;
            RenderTransform = _scaleTransform;
            Child = snackbar;

            _motion = new MotionGroup(this);
            _opacity = _motion.AddSpring(0, opacitySpec);
            _scale = _motion.AddSpring(.8, scaleSpec);
            _exitFrameDelay = _motion.Add(new DelayAnimation(TimeSpan.FromTicks(1)));
            if (duration != Timeout.InfiniteTimeSpan)
                _timeout = _motion.Add(new DelayAnimation(duration));
            _motion.Ticked += OnTicked;
            ApplyValues();
        }

        public MaterialSnackbar Snackbar { get; }

        public double OpacityValue => _opacity.Value;

        public double ScaleValue => _scale.Value;

        public void Show()
        {
            _isVisible = true;
            _opacity.AnimateTo(1);
            _scale.AnimateTo(1);
            _timeout?.Start();
            _motion.Start();
            ApplyValues();
        }

        public void Hide(bool deferAnimationOneFrame)
        {
            if (!_isVisible)
                return;

            _isVisible = false;
            _timeout?.Stop();
            if (deferAnimationOneFrame)
            {
                _hideOnNextFrame = true;
                _exitFrameDelay.Start();
                _motion.Start();
                return;
            }

            BeginHide();
        }

        private void BeginHide()
        {
            _opacity.AnimateTo(0);
            _scale.AnimateTo(.8);
            _motion.Start();
            ApplyValues();
            if (!_opacity.IsRunning && !_scale.IsRunning)
                _remove(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _motion.Start();
        }

        private void OnTicked(object? sender, EventArgs e)
        {
            ApplyValues();
            if (_hideOnNextFrame && _exitFrameDelay.HasElapsed)
            {
                _hideOnNextFrame = false;
                BeginHide();
                return;
            }

            if (!_timeoutRaised && _isVisible && _timeout?.HasElapsed == true)
            {
                _timeoutRaised = true;
                _timedOut();
            }

            if (_hideOnNextFrame)
                return;

            if (!_isVisible && !_opacity.IsRunning && !_scale.IsRunning)
                _remove(this);
        }

        private void ApplyValues()
        {
            if (_isVisible &&
                !_opacity.IsRunning &&
                !_scale.IsRunning &&
                _opacity.Value == 1 &&
                _scale.Value == 1)
            {
                ClearValue(OpacityProperty);
                ClearValue(RenderTransformProperty);
                return;
            }

            Opacity = Math.Clamp(_opacity.Value, 0, 1);
            if (!ReferenceEquals(RenderTransform, _scaleTransform))
                RenderTransform = _scaleTransform;
            var scale = _scale.Value;
            _scaleTransform.ScaleX = scale;
            _scaleTransform.ScaleY = scale;
        }
    }

    private sealed class DelayAnimation(TimeSpan duration) : IAnimation
    {
        private TimeSpan _elapsed;

        public double Value { get; private set; }

        public bool IsRunning { get; private set; }

        public bool HasElapsed { get; private set; }

        public void Start()
        {
            _elapsed = TimeSpan.Zero;
            Value = 0;
            HasElapsed = false;
            if (duration <= TimeSpan.Zero)
            {
                Value = 1;
                HasElapsed = true;
                IsRunning = false;
            }
            else
                IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public bool Tick(TimeSpan delta)
        {
            if (!IsRunning)
                return false;

            _elapsed += delta;
            if (_elapsed < duration)
            {
                Value = _elapsed.TotalSeconds / duration.TotalSeconds;
                return true;
            }

            _elapsed = duration;
            Value = 1;
            HasElapsed = true;
            IsRunning = false;
            return false;
        }

        public void Settle()
        {
            // A timeout is an operation deadline, not a visual interpolation. MotionGroup settles
            // visual springs when a control detaches or a static test asks for terminal visuals;
            // completing the deadline there would dismiss a snackbar merely because it left the
            // visual tree. Keep elapsed time paused and resume it on reattachment instead.
        }
    }

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

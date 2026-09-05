using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public enum SharedElementMode
{
    Geometry,
    MoveVisual,
    RenderSnapshot,
}

public enum SharedElementInputPolicy
{
    Preserve,
    DisableDuringTransition,
    TransferToDestination,
}

public sealed class SharedElement : Decorator, ITransitionTrack
{
    private readonly Animatable<Rect> _geometry = new(default, new RectVectorConverter());
    private AvaloniaMotionHost? _host;
    private SharedTransitionRoot? _root;
    private object? _registeredKey;
    private Transform? _originalTransform;
    private Rect _layoutBounds;
    private Control? _movedVisual;
    private bool _previousHitTestVisible;
    private bool _wasFocused;
    private object? _previousDataContext;
    private bool _dataContextTransferred;
    private Control? _snapshotVisual;
    private Control? _snapshotSource;
    private bool _snapshotPreviousHitTestVisible;
    private IDisposable? _clockRegistration;
    private bool _completionRaised;

    public static readonly StyledProperty<object?> KeyProperty = AvaloniaProperty.Register<SharedElement, object?>(nameof(Key));
    public object? Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public SharedParticipantRole Role { get; set; } = SharedParticipantRole.Active;
    public SharedElementMode Mode { get; set; } = SharedElementMode.Geometry;
    public SharedElementInputPolicy InputPolicy { get; set; } = SharedElementInputPolicy.DisableDuringTransition;
    public bool UseDestinationDataContext { get; set; }
    public SharedTransitionRoot? TransitionRoot { get; set; }
    public Func<Control, Control>? SnapshotFactory { get; set; }
    public IAnimationClock? Clock { get; private set; }

    public void AttachClock(IAnimationClock? clock)
    {
        if (ReferenceEquals(Clock, clock)) return;
        _clockRegistration?.Dispose();
        Clock = clock;
        if (clock is not null && _geometry.IsRunning)
            _clockRegistration = clock.Register(_geometry.Tick, () => _geometry.IsRunning);
    }
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
    public bool IsTransitioning => _geometry.IsRunning;
    public event EventHandler? TransitionCompleted;
    public Rect CurrentBounds => _geometry.Value;
    public SharedGeometry CurrentGeometry => new(
        _geometry.Value,
        RenderTransform,
        Clip,
        Opacity,
        ZIndex,
        Child is Border border ? border.CornerRadius : default);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _root = TransitionRoot ?? this.FindAncestorOfType<SharedTransitionRoot>();
        RegisterKey();
        _originalTransform = RenderTransform as Transform;
        _geometry.ValueChanged += ApplyGeometry;
        if (TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.Register(_geometry);
        }
        _layoutBounds = CaptureRootBounds();
        _geometry.SnapTo(_layoutBounds);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        RestoreVisual();
        _geometry.ValueChanged -= ApplyGeometry;
        UnregisterKey();
        _host?.Dispose();
        _clockRegistration?.Dispose();
        _host = null;
        _root = null;
        _originalTransform = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KeyProperty && _root is not null)
        {
            UnregisterKey();
            RegisterKey();
        }
    }

    public void AnimateTo(Rect target)
    {
        _completionRaised = false;
        _geometry.AnimateTo(target, new TweenSpec<Rect>(new RectVectorConverter(), Duration, Easings.FastOutSlowIn));
        if (Clock is not null && _clockRegistration is null)
            _clockRegistration = Clock.Register(_geometry.Tick, () => _geometry.IsRunning);
        _host?.RequestFrame();
    }

    internal void AnimateFrom(Rect source)
    {
        var target = CaptureRootBounds();
        _geometry.SnapTo(source);
        AnimateTo(target);
    }

    public void SnapTo(Rect target) => _geometry.SnapTo(target);

    public void CompleteTransition()
    {
        _geometry.Settle();
        ClearGeometryTransform();
        RestoreVisual();
        RaiseCompletion();
    }

    public void CancelTransition()
    {
        _geometry.Stop();
        ClearGeometryTransform();
        RestoreVisual();
        RaiseCompletion();
    }

    public void PauseTransition() => _geometry.Pause();
    public void ResumeTransition() => _geometry.Resume();
    internal bool Tick(TimeSpan delta)
    {
        var running = _geometry.Tick(delta);
        if (!running) RaiseCompletion();
        return running;
    }

    private void RaiseCompletion()
    {
        if (_completionRaised) return;
        _completionRaised = true;
        TransitionCompleted?.Invoke(this, EventArgs.Empty);
    }

    bool ITransitionTrack.IsRunning => IsTransitioning;
    bool ITransitionTrack.Tick(TimeSpan delta) => Tick(delta);
    void ITransitionTrack.Pause() => PauseTransition();
    void ITransitionTrack.Resume() => ResumeTransition();
    void ITransitionTrack.Complete() => CompleteTransition();
    void ITransitionTrack.Cancel() => CancelTransition();

    public bool MoveVisualToOverlay()
    {
        _root ??= TransitionRoot ?? this.FindAncestorOfType<SharedTransitionRoot>();
        if (_root is null || _movedVisual is not null || _snapshotVisual is not null || Child is not Control visual)
            return false;
        if (Mode == SharedElementMode.RenderSnapshot)
        {
            if (SnapshotFactory is null) return false;
            _snapshotVisual = SnapshotFactory(visual) ?? throw new InvalidOperationException("SnapshotFactory returned null.");
            _snapshotSource = visual;
            _snapshotPreviousHitTestVisible = _snapshotVisual.IsHitTestVisible;
            if (InputPolicy == SharedElementInputPolicy.DisableDuringTransition)
                _snapshotVisual.IsHitTestVisible = false;
            _root.Overlay.Children.Add(_snapshotVisual);
            Child = null;
            return true;
        }
        if (Mode != SharedElementMode.MoveVisual) return false;
        Child = null;
        _movedVisual = visual;
        _previousHitTestVisible = visual.IsHitTestVisible;
        _wasFocused = visual.IsFocused;
        _previousDataContext = visual.DataContext;
        if (InputPolicy == SharedElementInputPolicy.DisableDuringTransition)
            visual.IsHitTestVisible = false;
        _root.Overlay.Children.Add(visual);
        return true;
    }

    public bool RestoreVisual()
    {
        if (_snapshotVisual is not null && _root is not null)
        {
            _root.Overlay.Children.Remove(_snapshotVisual);
            _snapshotVisual.IsHitTestVisible = _snapshotPreviousHitTestVisible;
            if (_snapshotVisual is IDisposable disposable)
                disposable.Dispose();
            _snapshotVisual = null;
            Child = _snapshotSource;
            _snapshotSource = null;
            return true;
        }
        if (_movedVisual is null || _root is null)
            return false;
        _root.Overlay.Children.Remove(_movedVisual);
        _movedVisual.IsHitTestVisible = _previousHitTestVisible;
        if (_dataContextTransferred)
            _movedVisual.DataContext = _previousDataContext;
        Child = _movedVisual;
        if (_wasFocused)
            _movedVisual.Focus();
        _movedVisual = null;
        _wasFocused = false;
        _previousDataContext = null;
        _dataContextTransferred = false;
        return true;
    }

    public bool TransferVisualTo(SharedElement destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination._root ??= destination.TransitionRoot ?? destination.FindAncestorOfType<SharedTransitionRoot>();
        if (_movedVisual is null || destination.Child is not null)
            return false;
        if (_root is null || !ReferenceEquals(_root, destination._root))
            return false;
        _root.Overlay.Children.Remove(_movedVisual);
        _movedVisual.IsHitTestVisible = destination.InputPolicy != SharedElementInputPolicy.DisableDuringTransition;
        if (destination.UseDestinationDataContext)
        {
            _movedVisual.DataContext = destination.DataContext;
            _dataContextTransferred = true;
        }
        destination.Child = _movedVisual;
        if (_wasFocused)
            _movedVisual.Focus();
        _movedVisual = null;
        _wasFocused = false;
        _previousDataContext = null;
        _dataContextTransferred = false;
        return true;
    }

    private void RegisterKey()
    {
        if (_root is not null && Key is not null)
        {
            _root.Register(Key, this, Role);
            _registeredKey = Key;
        }
    }

    private void UnregisterKey()
    {
        if (_root is not null && _registeredKey is not null)
            _root.Unregister(_registeredKey, this);
        _registeredKey = null;
    }

    private Rect CaptureRootBounds() => _root is null ? Bounds : this.TranslateBoundsTo(_root);

    private void ApplyGeometry(Rect value)
    {
        if (_root is null) return;
        var delta = value.Position - _layoutBounds.Position;
        TransformComposition.Begin(this);
        TransformComposition.Add(this, new TranslateTransform(delta.X, delta.Y));
        if (_movedVisual is not null)
        {
            Canvas.SetLeft(_movedVisual, value.X);
            Canvas.SetTop(_movedVisual, value.Y);
            _movedVisual.Width = value.Width;
            _movedVisual.Height = value.Height;
        }
        if (_snapshotVisual is not null)
        {
            Canvas.SetLeft(_snapshotVisual, value.X);
            Canvas.SetTop(_snapshotVisual, value.Y);
            _snapshotVisual.Width = value.Width;
            _snapshotVisual.Height = value.Height;
        }
        Width = value.Width;
        Height = value.Height;
    }

    private void ClearGeometryTransform()
    {
        RenderTransform = _originalTransform;
    }
}

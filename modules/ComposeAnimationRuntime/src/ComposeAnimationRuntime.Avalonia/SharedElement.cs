using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ComposeAnimationRuntime.Avalonia;

public enum SharedElementMode { Geometry, MoveVisual, RenderSnapshot }
public enum SharedElementInputPolicy { Preserve, DisableDuringTransition, TransferToDestination }
public interface ISharedSnapshot : IDisposable { Control Visual { get; } }

public sealed class SharedElement : Decorator, ITransitionTrack
{
    private readonly Animatable<Rect> _geometry = new(default, new RectVectorConverter());
    private AvaloniaMotionHost? _host;
    private IDisposable? _clockRegistration;
    private SharedTransitionRoot? _root;
    private object? _registeredKey;
    private SharedVisualLease? _visualLease;
    private SharedElement? _destination;
    private IDisposable? _destinationVisibility;
    private bool _completionRaised = true;
    private bool _hasGeometry;
    private Rect _layoutBounds;
    private IDisposable? _geometryTransform;

    public SharedElement()
    {
        _geometry.ValueChanged += ApplyGeometry;
        _geometry.Completed += result => Finish(result.EndReason == AnimationEndReason.Finished);
    }

    public static readonly StyledProperty<object?> KeyProperty = AvaloniaProperty.Register<SharedElement, object?>(nameof(Key));
    public object? Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public SharedParticipantRole Role { get; set; } = SharedParticipantRole.Active;
    public SharedElementMode Mode { get; set; } = SharedElementMode.Geometry;
    public SharedElementInputPolicy InputPolicy { get; set; } = SharedElementInputPolicy.DisableDuringTransition;
    public bool UseDestinationDataContext { get; set; }
    public SharedTransitionRoot? TransitionRoot { get; set; }
    public Func<Control, Control>? SnapshotFactory { get; set; }
    public Func<Control, ISharedSnapshot>? SnapshotResourceFactory { get; set; }
    public IAnimationClock? Clock { get; private set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
    public bool IsTransitioning => _geometry.IsRunning;
    public event EventHandler? TransitionCompleted;
    public Rect CurrentBounds => _hasGeometry ? _geometry.Value : CaptureRootBounds();
    public SharedGeometry CurrentGeometry => new(CurrentBounds, RenderTransform, Clip, Opacity, ZIndex,
        Child is Border border ? border.CornerRadius : default);

    public void AttachClock(IAnimationClock? clock)
    {
        _clockRegistration?.Dispose();
        _clockRegistration = null;
        _host?.Dispose();
        _host = null;
        Clock = clock;
        EnsureClock();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _root = TransitionRoot ?? this.FindAncestorOfType<SharedTransitionRoot>();
        RegisterKey();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelTransition();
        UnregisterKey();
        _clockRegistration?.Dispose();
        _clockRegistration = null;
        _host?.Dispose();
        _host = null;
        _root = null;
        _hasGeometry = false;
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
        if (!_hasGeometry) SnapTo(CaptureRootBounds());
        _completionRaised = false;
        _geometry.AnimateTo(target, new TweenSpec<Rect>(new RectVectorConverter(), Duration, Easings.FastOutSlowIn));
        EnsureClock();
    }

    internal void AnimateFrom(Rect source)
    {
        var target = CaptureRootBounds();
        SnapTo(source);
        AnimateTo(target);
    }

    internal void AnimateTo(SharedElement destination, Rect target)
    {
        _destinationVisibility?.Dispose();
        _destination = destination;
        _destinationVisibility = destination.SetValue(OpacityProperty, 0, BindingPriority.Animation);
        try
        {
            MoveVisualToOverlay();
            AnimateTo(target);
        }
        catch
        {
            Finish(false);
            throw;
        }
    }

    public void SnapTo(Rect target)
    {
        if (!_hasGeometry) _layoutBounds = CaptureRootBounds();
        _hasGeometry = true;
        _geometry.SnapTo(target);
    }

    public void CompleteTransition()
    {
        _geometry.Settle();
        Finish(true);
    }

    public void CancelTransition()
    {
        _geometry.Stop();
        Finish(false);
    }

    public void PauseTransition() => _geometry.Pause();
    public void ResumeTransition() { _geometry.Resume(); EnsureClock(); }
    internal bool Tick(TimeSpan delta) => _geometry.Tick(delta);
    bool ITransitionTrack.IsRunning => IsTransitioning;
    bool ITransitionTrack.Tick(TimeSpan delta) => Tick(delta);
    void ITransitionTrack.Seek(TimeSpan playTime) => _geometry.Seek(playTime);
    void ITransitionTrack.Pause() => PauseTransition();
    void ITransitionTrack.Resume() => ResumeTransition();
    void ITransitionTrack.Complete() => CompleteTransition();
    void ITransitionTrack.Cancel() => CancelTransition();

    public bool MoveVisualToOverlay()
    {
        _root ??= TransitionRoot ?? FindRoot() ?? SharedTransitionRoot.LastCreated;
        if (_root is null || _visualLease is not null || Child is not { } visual || Mode == SharedElementMode.Geometry)
            return false;
        var bounds = _hasGeometry ? CurrentBounds : CaptureRootBounds();
        _layoutBounds = CaptureRootBounds();
        var lease = SharedVisualLease.Create(this, _root, visual);
        if (lease is null) return false;
        _visualLease = lease;
        _hasGeometry = true;
        _geometry.SnapTo(bounds);
        return true;
    }

    public bool RestoreVisual()
    {
        var lease = _visualLease;
        _visualLease = null;
        if (lease is null) return false;
        lease.Dispose();
        return true;
    }

    public bool TransferVisualTo(SharedElement destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var root = destination._root ?? destination.TransitionRoot ?? destination.FindRoot();
        if (ReferenceEquals(this, destination) || _visualLease is not { IsSnapshot: false } lease || !ReferenceEquals(_root, root))
            return false;
        lease.TransferTo(destination);
        _visualLease = null;
        return true;
    }

    private void Finish(bool completed)
    {
        var notify = !_completionRaised;
        _completionRaised = true;
        try
        {
            if (!completed || _destination?.InputPolicy != SharedElementInputPolicy.TransferToDestination || !TransferVisualTo(_destination))
                RestoreVisual();
        }
        finally
        {
            _destinationVisibility?.Dispose();
            _destinationVisibility = null;
            _destination = null;
            _geometryTransform?.Dispose();
            _geometryTransform = null;
            if (notify) TransitionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void EnsureClock()
    {
        if (!_geometry.IsRunning && !_geometry.IsPaused) return;
        _clockRegistration?.Dispose();
        _clockRegistration = null;
        if (Clock is { } clock)
            _clockRegistration = clock.Register(Tick, () => _geometry.IsRunning || _geometry.IsPaused);
        else if (TopLevel.GetTopLevel(this) is { } top)
        {
            _host ??= new AvaloniaMotionHost(top);
            _clockRegistration = _host.RegisterTrack(Tick, () => _geometry.IsRunning || _geometry.IsPaused);
        }
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
        if (_root is not null && _registeredKey is not null) _root.Unregister(_registeredKey, this);
        _registeredKey = null;
    }

    private SharedTransitionRoot? FindRoot()
    {
        var ancestor = this.FindAncestorOfType<SharedTransitionRoot>();
        if (ancestor is not null) return ancestor;
        global::Avalonia.LogicalTree.ILogical? logical = this;
        while (logical is not null)
        {
            if (logical is SharedTransitionRoot root) return root;
            logical = logical.LogicalParent;
        }
        return null;
    }

    private Rect CaptureRootBounds() => (_root ?? TransitionRoot ?? FindRoot()) is { } root
        ? this.TranslateBoundsTo(root) : Bounds;

    private void ApplyGeometry(Rect value)
    {
        if (_visualLease is { } lease) { lease.Arrange(value); return; }
        if (_root is null || _layoutBounds.Width <= 0 || _layoutBounds.Height <= 0) return;
        _geometryTransform?.Dispose();
        var scale = Matrix.CreateScale(value.Width / _layoutBounds.Width, value.Height / _layoutBounds.Height);
        var origin = RenderTransformOrigin.ToPixels(Bounds.Size);
        var delta = value.Position - _layoutBounds.Position;
        var matrix = Matrix.CreateTranslation(origin.X, origin.Y) * scale * Matrix.CreateTranslation(delta.X, delta.Y) * Matrix.CreateTranslation(-origin.X, -origin.Y);
        _geometryTransform = SetValue(RenderTransformProperty, new MatrixTransform(matrix), BindingPriority.Animation);
    }
}

internal sealed class SharedVisualLease : IDisposable
{
    private readonly SharedElement _owner;
    private readonly SharedTransitionRoot _root;
    private readonly Control _visual;
    private readonly bool _previousHitTestVisible;
    private readonly object? _previousDataContext;
    private readonly bool _wasFocused;
    private int _disposed;

    private SharedVisualLease(SharedElement owner, SharedTransitionRoot root, Control visual)
    {
        _owner = owner; _root = root; _visual = visual;
        _previousHitTestVisible = visual.IsHitTestVisible;
        _previousDataContext = visual.DataContext;
        _wasFocused = visual.IsFocused;
    }

    public bool IsSnapshot => false;
    public static SharedVisualLease? Create(SharedElement owner, SharedTransitionRoot root, Control visual)
    {
        var lease = new SharedVisualLease(owner, root, visual);
        owner.Child = null;
        root.Overlay.Children.Add(visual);
        visual.IsHitTestVisible = owner.InputPolicy == SharedElementInputPolicy.Preserve;
        Canvas.SetLeft(visual, owner.CurrentBounds.X);
        Canvas.SetTop(visual, owner.CurrentBounds.Y);
        return lease;
    }
    public void Arrange(Rect bounds)
    {
        Canvas.SetLeft(_visual, bounds.X); Canvas.SetTop(_visual, bounds.Y);
        _visual.Width = bounds.Width; _visual.Height = bounds.Height;
    }
    public void TransferTo(SharedElement destination)
    {
        _root.Overlay.Children.Remove(_visual);
        if (destination.Child is not null) destination.Child = null;
        if (destination.UseDestinationDataContext) _visual.DataContext = destination.DataContext;
        _visual.IsHitTestVisible = destination.InputPolicy != SharedElementInputPolicy.DisableDuringTransition;
        destination.Child = _visual;
        if (_wasFocused) _visual.Focus();
        Interlocked.Exchange(ref _disposed, 1);
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _root.Overlay.Children.Remove(_visual);
        _visual.IsHitTestVisible = _previousHitTestVisible;
        _visual.DataContext = _previousDataContext;
        _owner.Child = _visual;
        if (_wasFocused) _visual.Focus();
    }
}

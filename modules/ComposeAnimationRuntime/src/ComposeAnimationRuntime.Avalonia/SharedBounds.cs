using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class SharedBounds : Decorator, ITransitionTrack
{
    private readonly Animatable<Rect> _bounds = new(default, new RectVectorConverter());
    private AvaloniaMotionHost? _host;
    private IDisposable? _clockRegistration;
    private SharedTransitionRoot? _root;
    private object? _registeredKey;
    private Size _layoutSize;
    private SharedGeometry _geometryTarget;
    private SharedGeometry _geometryStart;

    public static readonly StyledProperty<object?> KeyProperty = AvaloniaProperty.Register<SharedBounds, object?>(nameof(Key));
    public object? Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public SharedParticipantRole Role { get; set; } = SharedParticipantRole.Active;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
    public IAnimationClock? Clock { get; set; }
    public event EventHandler? TransitionCompleted;
    private bool _completionRaised;

    public void AttachClock(IAnimationClock? clock)
    {
        if (ReferenceEquals(Clock, clock)) return;
        _clockRegistration?.Dispose();
        _clockRegistration = null;
        _host?.Dispose();
        _host = null;
        Clock = clock;
        if (clock is not null && _bounds.IsRunning)
            _clockRegistration = clock.Register(_bounds.Tick, () => _bounds.IsRunning);
        else if (clock is null && TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.Register(_bounds);
        }
    }
    public bool IsTransitioning => _bounds.IsRunning;
    public Rect CurrentBounds => _bounds.Value;
    public SharedGeometry CurrentGeometry => new(
        _bounds.Value,
        RenderTransform,
        Clip,
        Opacity,
        ZIndex,
        Child is Border border ? border.CornerRadius : default);

    public void AnimateTo(SharedGeometry target)
    {
        _geometryStart = CurrentGeometry;
        _geometryTarget = target;
        AnimateTo(target.Bounds);
    }

    public void SnapTo(SharedGeometry geometry)
    {
        ApplyVisualState(geometry);
        _layoutSize = geometry.Size;
        _bounds.SnapTo(geometry.Bounds);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _root = this.FindAncestorOfType<SharedTransitionRoot>();
        if (_root is not null && Key is not null) { _root.Register(Key, this, Role); _registeredKey = Key; }
        if (Clock is not null)
            _clockRegistration = Clock.Register(_bounds.Tick, () => _bounds.IsRunning);
        else if (TopLevel.GetTopLevel(this) is { } top)
        {
            _host = new AvaloniaMotionHost(top);
            _host.Register(_bounds);
        }
        _bounds.ValueChanged += ApplyBounds;
        var current = CaptureRootBounds();
        _bounds.SnapTo(current);
        _layoutSize = current.Size;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _bounds.ValueChanged -= ApplyBounds;
        if (_root is not null && _registeredKey is not null) _root.Unregister(_registeredKey, this);
        _clockRegistration?.Dispose(); _clockRegistration = null;
        _host?.Dispose(); _host = null; _root = null; _registeredKey = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KeyProperty && _root is not null)
        {
            if (_root is not null && _registeredKey is not null) _root.Unregister(_registeredKey, this);
            _registeredKey = Key;
            if (_root is not null && Key is not null) _root.Register(Key, this, Role);
        }
    }

    public void AnimateTo(Rect target)
    {
        _completionRaised = false;
        if (_geometryTarget.Bounds != target || _geometryTarget.Equals(default))
        {
            _geometryStart = CurrentGeometry;
            _geometryTarget = new SharedGeometry(target, RenderTransform, Clip, Opacity, ZIndex);
        }
        _bounds.AnimateTo(target, new TweenSpec<Rect>(new RectVectorConverter(), Duration, Easings.FastOutSlowIn));
        if (Clock is not null && _clockRegistration is null)
            _clockRegistration = Clock.Register(_bounds.Tick, () => _bounds.IsRunning);
        _host?.RequestFrame();
    }

    public void RetargetTo(Rect target) => AnimateTo(target);

    public void SnapTo(Rect target)
    {
        _layoutSize = target.Size;
        _geometryTarget = _geometryStart = new SharedGeometry(target, RenderTransform, Clip, Opacity, ZIndex);
        _bounds.SnapTo(target);
    }
    public SharedTransitionPlaceholder CreatePlaceholder() => new(_layoutSize == default ? Bounds.Size : _layoutSize, Key);

    public IDisposable? CreateParentPlaceholder()
    {
        if (Parent is not Panel parent) return null;
        var index = parent.Children.IndexOf(this);
        if (index < 0) return null;
        var placeholder = CreatePlaceholder();
        parent.Children[index] = placeholder;
        return new PlaceholderLease(parent, placeholder, this);
    }

    private sealed class PlaceholderLease(Panel parent, SharedTransitionPlaceholder placeholder, SharedBounds bounds) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            var currentIndex = parent.Children.IndexOf(placeholder);
            if (currentIndex >= 0) parent.Children[currentIndex] = bounds;
        }
    }

    internal void AnimateFrom(Rect source)
    {
        var target = CaptureRootBounds();
        _bounds.SnapTo(source);
        AnimateTo(target);
    }

    internal void AnimateExit()
    {
        AnimateTo(new Rect(_bounds.Value.Position, new Size(0, 0)));
    }

    internal void AnimateEnter()
    {
        var target = CaptureRootBounds();
        _bounds.SnapTo(new Rect(target.Position, new Size(0, 0)));
        AnimateTo(target);
    }

    public void CompleteTransition()
    {
        _bounds.Settle();
        ApplyVisualState(_geometryTarget);
        RaiseCompletion();
    }
    public void CancelTransition()
    {
        _bounds.Stop();
        RaiseCompletion();
    }
    public void PauseTransition() => _bounds.Pause();
    public void ResumeTransition() => _bounds.Resume();
    internal bool Tick(TimeSpan delta)
    {
        var running = _bounds.Tick(delta);
        if (!running) RaiseCompletion();
        return running;
    }

    bool ITransitionTrack.IsRunning => IsTransitioning;
    bool ITransitionTrack.Tick(TimeSpan delta) => Tick(delta);
    void ITransitionTrack.Pause() => PauseTransition();
    void ITransitionTrack.Resume() => ResumeTransition();
    void ITransitionTrack.Complete() => CompleteTransition();
    void ITransitionTrack.Cancel() => CancelTransition();

    private void RaiseCompletion()
    {
        if (_completionRaised) return;
        _completionRaised = true;
        TransitionCompleted?.Invoke(this, EventArgs.Empty);
    }

    private Rect CaptureRootBounds() => _root is null ? Bounds : this.TranslateBoundsTo(_root);
    private void ApplyBounds(Rect value)
    {
        var progress = _bounds.Progress;
        Opacity = _geometryStart.Opacity + (_geometryTarget.Opacity - _geometryStart.Opacity) * progress;
        Clip = progress >= 1 ? _geometryTarget.Clip : _geometryStart.Clip;
        if (Child is Border border)
            border.CornerRadius = progress >= 1 ? _geometryTarget.CornerRadius : _geometryStart.CornerRadius;
        ZIndex = progress >= 1 ? _geometryTarget.ZIndex : _geometryStart.ZIndex;
        if (_root is not null)
        {
            var local = value.Position - _root.Bounds.Position;
            RenderTransform = progress >= 1 && _geometryTarget.Transform is not null
                ? _geometryTarget.Transform
                : _geometryStart.Transform ?? new TranslateTransform(local.X, local.Y);
            Width = value.Width; Height = value.Height;
        }
    }

    private void ApplyVisualState(SharedGeometry geometry)
    {
        Clip = geometry.Clip;
        if (Child is Border border)
            border.CornerRadius = geometry.CornerRadius;
        Opacity = geometry.Opacity;
        ZIndex = geometry.ZIndex;
        if (geometry.Transform is not null)
            RenderTransform = geometry.Transform;
    }
}

internal sealed class RectVectorConverter : IAnimationVectorConverter<Rect>
{
    public Rect VisibilityThreshold => new(0.01, 0.01, 0.01, 0.01);
    public AnimationVector ConvertToVector(Rect value) => new AnimationVector4D((float)value.X, (float)value.Y, (float)value.Width, (float)value.Height);
    public Rect ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1], vector[2], vector[3]);
    public AnimationVector CreateZeroVector() => new AnimationVector4D();
    public bool IsFinished(AnimationVector current, AnimationVector target)
        => Enumerable.Range(0, current.Dimension).All(index => Math.Abs(current[index] - target[index]) < 0.01f);
}

internal static class SharedVisualExtensions
{
    public static Rect TranslateBoundsTo(this Control control, Visual root)
    {
        var transform = control.TransformToVisual(root) ?? Matrix.Identity;
        var bounds = new Rect(control.Bounds.Size);
        var p1 = transform.Transform(bounds.TopLeft);
        var p2 = transform.Transform(bounds.TopRight);
        var p3 = transform.Transform(bounds.BottomRight);
        var p4 = transform.Transform(bounds.BottomLeft);
        var minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
        var minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        var maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
        var maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;
using SmoothScroll.Avalonia.Controls;

namespace Material3Expressive.Components;

/// <summary>Connects a screen's main SmoothScroll viewport to its AppBars. Embedded viewports scroll independently.</summary>
public class AppBarScrollScope : Decorator
{
    public static readonly StyledProperty<TopAppBarScrollBehavior?> TopScrollBehaviorProperty = AvaloniaProperty.Register<AppBarScrollScope, TopAppBarScrollBehavior?>(nameof(TopScrollBehavior));
    public static readonly StyledProperty<BottomAppBarScrollBehavior?> BottomScrollBehaviorProperty = AvaloniaProperty.Register<AppBarScrollScope, BottomAppBarScrollBehavior?>(nameof(BottomScrollBehavior));
    public TopAppBarScrollBehavior? TopScrollBehavior { get => GetValue(TopScrollBehaviorProperty); set => SetValue(TopScrollBehaviorProperty, value); }
    public BottomAppBarScrollBehavior? BottomScrollBehavior { get => GetValue(BottomScrollBehaviorProperty); set => SetValue(BottomScrollBehaviorProperty, value); }
    private ScrollViewer? _mainViewer;
    private AppBarSmoothScrollConnection? _connection;
    private readonly HashSet<CancellationTokenSource> _runs = [];
    private bool _scanning;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopScrollBehaviorProperty || change.Property == BottomScrollBehaviorProperty)
        {
            Interrupt();
            Disconnect();
            InvalidateMeasure();
        }
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LayoutUpdated += UpdateConnection;
        UpdateConnection(null, EventArgs.Empty);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= UpdateConnection;
        Interrupt();
        Disconnect();
        if (_mainViewer is not null) _mainViewer.PropertyChanged -= ViewerChanged;
        _mainViewer = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void UpdateConnection(object? sender, EventArgs e)
    {
        if (_scanning) return;
        _scanning = true;
        try
        {
            var candidates = this.GetVisualDescendants().OfType<ScrollViewer>()
                .Where(viewer => viewer.GetVisualAncestors().OfType<AppBarScrollScope>().FirstOrDefault() == this &&
                    !viewer.GetVisualAncestors().OfType<ScrollViewer>().Any() &&
                    viewer.GetVisualAncestors().OfType<ContentPresenter>().Any(presenter =>
                        presenter.Name == "PART_ContentPresenter" && presenter.TemplatedParent is Scaffold))
                .Take(2).ToArray();
            // A scope owns one main body. Ambiguous bodies are not resolved by tree order.
            var viewer = candidates.Length == 1 ? candidates[0] : null;
            if (!ReferenceEquals(viewer, _mainViewer))
            {
                Disconnect();
                if (_mainViewer is not null) _mainViewer.PropertyChanged -= ViewerChanged;
                _mainViewer = viewer;
                if (viewer is not null) viewer.PropertyChanged += ViewerChanged;
            }
            if (viewer is null || !viewer.IsArrangeValid ||
                viewer.GetVisualAncestors().OfType<Scaffold>().FirstOrDefault() is not { IsArrangeValid: true }) return;
            var presenter = viewer.GetVisualDescendants().OfType<ScrollViewerPresenter>()
                .FirstOrDefault(p => p.TemplatedParent == viewer);
            if (presenter is not { IsPhysicalScrollActive: true } || !AppBarSmoothScrollConnection.IsSupported(this, viewer))
            { Disconnect(); return; }
            if (_connection is not null && !_connection.Matches(presenter)) Disconnect();
            _connection ??= new(this, viewer, presenter);
            _connection.Update();
        }
        finally { _scanning = false; }
    }
    private void ViewerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TemplatedControl.TemplateProperty || e.Property == StyledElement.ThemeProperty) Disconnect();
    }
    private void Disconnect()
    {
        _connection?.Dispose();
        _connection = null;
    }
    /// <summary>Cancel scope-managed content inertia, bar flings and snaps. Independent nested
    /// SmoothScroll viewports retain their own input and animation lifetimes.</summary>
    public void Interrupt()
    {
        InterruptUiAnimations();
        _connection?.Interrupt();
    }
    internal void InterruptUiAnimations()
    {
        foreach (var run in _runs.ToArray()) run.Cancel();
    }
    internal AppBarSmoothScrollConnection? GetSmoothConnection(ScrollViewer viewer) => ReferenceEquals(viewer, _mainViewer) ? _connection : null;
    internal void Register(CancellationTokenSource run) => _runs.Add(run);
    internal void Unregister(CancellationTokenSource run) => _runs.Remove(run);
    internal Vector Pre(Vector available, AppBarScrollSource source)
    {
        if (TopScrollBehavior is DefaultTopAppBarScrollBehavior topDefaults) topDefaults.AttachDefaults(this);
        if (BottomScrollBehavior is DefaultBottomAppBarScrollBehavior bottomDefaults) bottomDefaults.AttachDefaults(this);
        var top = TopScrollBehavior?.NestedScrollConnection.OnPreScroll(available, source) ?? default;
        var bottom = BottomScrollBehavior?.NestedScrollConnection.OnPreScroll(available - top, source) ?? default;
        return top + bottom;
    }
    internal Vector Post(Vector consumed, Vector available, AppBarScrollSource source)
    {
        var bottom = BottomScrollBehavior?.NestedScrollConnection.OnPostScroll(consumed, available, source) ?? default;
        var top = TopScrollBehavior?.NestedScrollConnection.OnPostScroll(consumed + bottom, available - bottom, source) ?? default;
        return bottom + top;
    }
    internal async ValueTask<Vector> PreFling(Vector available, IAnimationFrameClock clock, CancellationToken token)
    {
        if (TopScrollBehavior is DefaultTopAppBarScrollBehavior topDefaults) topDefaults.AttachDefaults(this);
        if (BottomScrollBehavior is DefaultBottomAppBarScrollBehavior bottomDefaults) bottomDefaults.AttachDefaults(this);
        var top = TopScrollBehavior is { } t ? await t.NestedScrollConnection.OnPreFlingAsync(available, clock, token) : default;
        var bottom = BottomScrollBehavior is { } b ? await b.NestedScrollConnection.OnPreFlingAsync(available - top, clock, token) : default;
        return top + bottom;
    }
    internal async ValueTask<Vector> PostFling(Vector consumed, Vector available, IAnimationFrameClock clock, CancellationToken token)
    {
        var bottom = BottomScrollBehavior is { } b ? await b.NestedScrollConnection.OnPostFlingAsync(consumed, available, clock, token) : default;
        var top = TopScrollBehavior is { } t ? await t.NestedScrollConnection.OnPostFlingAsync(consumed + bottom, available - bottom, clock, token) : default;
        return bottom + top;
    }
    /// <summary>Explicit custom-host path. The content callback returns consumed physical pixels.</summary>
    public Vector DispatchScroll(Vector available, Func<Vector, Vector> consumeContent, AppBarScrollSource source = AppBarScrollSource.UserInput)
    {
        var scopes = new[] { this }.Concat(this.GetVisualAncestors().OfType<AppBarScrollScope>()).ToArray();
        var remaining = available;
        foreach (var scope in scopes.Reverse()) remaining -= scope.Pre(remaining, source);
        var consumed = consumeContent(remaining); remaining -= consumed;
        foreach (var scope in scopes) { var post = scope.Post(consumed, remaining, source); consumed += post; remaining -= post; }
        return available - remaining;
    }
    /// <summary>Custom-host fling path. The callback returns consumed velocity in physical px/s.
    /// Send each content-inertia delta through DispatchScroll with SideEffect.</summary>
    public async Task<Vector> DispatchFlingAsync(Vector available,
        Func<Vector, CancellationToken, Task<Vector>> consumeContent, IAnimationFrameClock clock, CancellationToken cancellationToken = default)
    {
        var scopes = new[] { this }.Concat(this.GetVisualAncestors().OfType<AppBarScrollScope>()).ToArray();
        using var run = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        foreach (var scope in scopes) scope.Register(run);
        try
        {
            var remaining = available;
            foreach (var scope in scopes.Reverse()) remaining -= await scope.PreFling(remaining, clock, run.Token);
            var consumed = await consumeContent(remaining, run.Token); remaining -= consumed;
            foreach (var scope in scopes) { var post = await scope.PostFling(consumed, remaining, clock, run.Token); consumed += post; remaining -= post; }
            return available - remaining;
        }
        finally { foreach (var scope in scopes) scope.Unregister(run); }
    }
}

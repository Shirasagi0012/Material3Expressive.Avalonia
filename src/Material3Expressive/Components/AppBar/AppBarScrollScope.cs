using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Opt-in nested scrolling around a screen, including native embedded ScrollViewers.</summary>
public class AppBarScrollScope : Decorator
{
    public static readonly StyledProperty<TopAppBarScrollBehavior?> TopScrollBehaviorProperty = AvaloniaProperty.Register<AppBarScrollScope, TopAppBarScrollBehavior?>(nameof(TopScrollBehavior));
    public static readonly StyledProperty<BottomAppBarScrollBehavior?> BottomScrollBehaviorProperty = AvaloniaProperty.Register<AppBarScrollScope, BottomAppBarScrollBehavior?>(nameof(BottomScrollBehavior));
    internal static readonly AttachedProperty<bool> UseStandardPresenterProperty = AvaloniaProperty.RegisterAttached<AppBarScrollScope, ScrollViewer, bool>("UseStandardPresenter");
    public TopAppBarScrollBehavior? TopScrollBehavior { get => GetValue(TopScrollBehaviorProperty); set => SetValue(TopScrollBehaviorProperty, value); }
    public BottomAppBarScrollBehavior? BottomScrollBehavior { get => GetValue(BottomScrollBehaviorProperty); set => SetValue(BottomScrollBehaviorProperty, value); }
    internal static bool GetUseStandardPresenter(ScrollViewer viewer) => viewer.GetValue(UseStandardPresenterProperty);
    internal static void SetUseStandardPresenter(ScrollViewer viewer, bool value) => viewer.SetValue(UseStandardPresenterProperty, value);
    private readonly HashSet<ScrollViewer> _viewers = [];
    private readonly HashSet<CancellationTokenSource> _runs = [];
    private IControlTemplate? _nativeTemplate;
    private IControlTemplate? _adaptedTemplate;
    private bool _scanning;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopScrollBehaviorProperty || change.Property == BottomScrollBehaviorProperty) Interrupt();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Match the actual Fluent theme's template by identity. Resource overrides and explicit
        // Template/Theme values are not inferred to be standard just because they use ScrollViewer.
        var styles = this.GetLogicalAncestors().OfType<StyledElement>().SelectMany(element => element.Styles)
            .Concat(Application.Current?.Styles ?? []);
        var fluent = styles.FirstOrDefault(style => style.GetType().FullName == "Avalonia.Themes.Fluent.FluentTheme") as IResourceProvider;
        if (fluent?.TryGetResource(typeof(ScrollViewer), ActualThemeVariant, out var resource) == true && resource is ControlTheme theme)
            _nativeTemplate = theme.Setters.OfType<Setter>().FirstOrDefault(setter => setter.Property == TemplatedControl.TemplateProperty)?.Value as IControlTemplate;
        if (this.TryFindResource("AppBarStandardScrollViewerTemplate", out var adapted)) _adaptedTemplate = adapted as IControlTemplate;
        LayoutUpdated += ScanViewers;
        ScanViewers(null, EventArgs.Empty);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= ScanViewers;
        Interrupt();
        foreach (var viewer in _viewers.ToArray()) Release(viewer);
        _nativeTemplate = _adaptedTemplate = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void ScanViewers(object? sender, EventArgs e)
    {
        if (_scanning || _nativeTemplate is null) return;
        _scanning = true;
        try
        {
            var candidates = this.GetVisualDescendants().OfType<ScrollViewer>()
                .Where(viewer => viewer.GetVisualAncestors().OfType<AppBarScrollScope>().FirstOrDefault() == this).ToHashSet();
            foreach (var removed in _viewers.Except(candidates).ToArray()) Release(removed);
            foreach (var viewer in candidates)
            {
                if (_viewers.Add(viewer)) viewer.PropertyChanged += ViewerChanged;
                if (viewer.Theme is null && ReferenceEquals(viewer.Template, _nativeTemplate)) Adapt(viewer);
            }
        }
        finally { _scanning = false; }
    }
    private void ViewerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not ScrollViewer viewer) return;
        if (e.Property == StyledElement.ThemeProperty && viewer.Theme is not null) Restore(viewer);
        else if (e.Property == TemplatedControl.TemplateProperty && !Equals(e.OldValue, e.NewValue))
        {
            ReleaseTemplateContent(viewer);
            if (viewer.Theme is null && ReferenceEquals(viewer.Template, _nativeTemplate)) Adapt(viewer);
            else if (GetUseStandardPresenter(viewer) && !ReferenceEquals(viewer.Template, _adaptedTemplate))
                SetUseStandardPresenter(viewer, false);
        }
    }
    private void Adapt(ScrollViewer viewer)
    {
        if (_adaptedTemplate is null) return;
        SetUseStandardPresenter(viewer, true);
        // Preserve the native template's value source and priority. A class-trigger setter
        // would outrank a custom plain Style added after activation, hiding that replacement.
        // SetCurrentValue allows subsequent style/binding values to replace the adapter.
        viewer.SetCurrentValue(TemplatedControl.TemplateProperty, _adaptedTemplate);
    }
    private void Restore(ScrollViewer viewer)
    {
        var wasAdapted = GetUseStandardPresenter(viewer) && ReferenceEquals(viewer.Template, _adaptedTemplate);
        SetUseStandardPresenter(viewer, false);
        if (wasAdapted)
        {
            ReleaseTemplateContent(viewer);
            viewer.SetCurrentValue(TemplatedControl.TemplateProperty, _nativeTemplate);
        }
    }
    private static void ReleaseTemplateContent(ScrollViewer viewer)
    {
        // A native presenter retains its visual child when its template is discarded. Release
        // it before a new presenter attaches an embedded TextPresenter/ItemsPresenter instance.
        foreach (var presenter in viewer.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>()
                     .Where(presenter => presenter.TemplatedParent == viewer))
            presenter.Content = null;
    }
    private void Release(ScrollViewer viewer)
    {
        viewer.PropertyChanged -= ViewerChanged;
        Restore(viewer); _viewers.Remove(viewer);
    }
    /// <summary>Cancel in-flight content inertia, bar flings and snaps in this scope.</summary>
    public void Interrupt() { foreach (var run in _runs.ToArray()) run.Cancel(); }
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

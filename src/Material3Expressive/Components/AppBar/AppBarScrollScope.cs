using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using SmoothScroll.Avalonia.Controls;

namespace Material3Expressive.Components;

/// <summary>Connects a screen's main SmoothScroll viewport to its AppBars. Embedded viewports scroll independently.</summary>
public class AppBarScrollScope : Decorator
{
    public static readonly StyledProperty<TopAppBarScrollBehavior?> TopScrollBehaviorProperty =
        AvaloniaProperty.Register<AppBarScrollScope, TopAppBarScrollBehavior?>(nameof(TopScrollBehavior));

    public static readonly StyledProperty<BottomAppBarScrollBehavior?> BottomScrollBehaviorProperty =
        AvaloniaProperty.Register<AppBarScrollScope, BottomAppBarScrollBehavior?>(nameof(BottomScrollBehavior));

    public TopAppBarScrollBehavior? TopScrollBehavior
    {
        get => GetValue(TopScrollBehaviorProperty);
        set => SetValue(TopScrollBehaviorProperty, value);
    }

    public BottomAppBarScrollBehavior? BottomScrollBehavior
    {
        get => GetValue(BottomScrollBehaviorProperty);
        set => SetValue(BottomScrollBehaviorProperty, value);
    }

    internal Scaffold? Host { get; private set; }

    internal void SetHost(Scaffold? host)
    {
        Interrupt();
        Disconnect();
        Host = host;
        if (host is null)
        {
            SetCurrentValue(TopScrollBehaviorProperty, null);
            SetCurrentValue(BottomScrollBehaviorProperty, null);
        }
        else
            RefreshBehaviors();
    }

    private void RefreshBehaviors()
    {
        if (Host is not { } scaffold)
            return;
        SetCurrentValue(TopScrollBehaviorProperty, scaffold.PresentedTopBar?.ScrollBehavior);
        SetCurrentValue(BottomScrollBehaviorProperty, scaffold.PresentedBottomBar?.ScrollBehavior);
    }

    private ScrollViewer? _mainViewer;
    private AppBarSmoothScrollConnection? _connection;
    private readonly HashSet<CancellationTokenSource> _runs = [];
    private bool _scanning;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopScrollBehaviorProperty || change.Property == BottomScrollBehaviorProperty)
        {
            if (change.OldValue is StyledElement oldBehavior)
                oldBehavior.PropertyChanged -= BehaviorChanged;
            if (change.NewValue is StyledElement behavior)
                behavior.PropertyChanged += BehaviorChanged;
            Interrupt();
            Disconnect();
            InvalidateMeasure();
        }
    }

    private void BehaviorChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property.OwnerType != typeof(TopAppBarScrollBehavior) &&
            change.Property.OwnerType != typeof(BottomAppBarScrollBehavior))
            return;
        if (change.Property != TopAppBarScrollBehavior.CanScrollProperty &&
            change.Property != BottomAppBarScrollBehavior.CanScrollProperty &&
            change.Property != TopAppBarScrollBehavior.IsScrollingContentAtStartProperty &&
            change.Property != TopAppBarScrollBehavior.IsPinnedProperty)
            Interrupt();
        if (change.Property == TopAppBarScrollBehavior.StateProperty ||
            change.Property == BottomAppBarScrollBehavior.StateProperty)
            Disconnect();
        InvalidateMeasure();
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
        if (_mainViewer is { })
            _mainViewer.PropertyChanged -= ViewerChanged;

        _mainViewer = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateConnection(object? sender, EventArgs e)
    {
        if (_scanning)
            return;

        _scanning = true;
        try
        {
            RefreshBehaviors();
            var candidates = this.GetVisualDescendants()
                .OfType<ScrollViewer>()
                .Where(viewer =>
                {
                    var ancestors = viewer.GetVisualAncestors().ToArray();
                    return ancestors.FirstOrDefault(ancestor => ancestor is AppBarScrollScope) == this &&
                           !ancestors.Any(ancestor => ancestor is ScrollViewer) &&
                           ancestors.Any(ancestor => ancestor is ContentPresenter
                           {
                               Name: "PART_ContentPresenter",
                               TemplatedParent: Scaffold,
                           } presenter && (Host is null || presenter.TemplatedParent == Host));
                })
                .Take(2)
                .ToArray();

            // A scope owns one main body. Ambiguous bodies are not resolved by tree order.
            var viewer = candidates.Length == 1 ? candidates[0] : null;
            if (!ReferenceEquals(viewer, _mainViewer))
            {
                Disconnect();
                if (_mainViewer is { })
                    _mainViewer.PropertyChanged -= ViewerChanged;

                _mainViewer = viewer;
                if (viewer is { })
                    viewer.PropertyChanged += ViewerChanged;
            }

            if (viewer is not { IsArrangeValid: true })
                return;

            var scaffold = Host ?? viewer.GetVisualAncestors()
                .OfType<Scaffold>()
                .FirstOrDefault();
            if (scaffold is not { IsArrangeValid: true })
                return;

            var presenter = viewer.GetVisualDescendants()
                .OfType<ScrollViewerPresenter>()
                .FirstOrDefault(presenter => presenter.TemplatedParent == viewer);
            if (presenter is not { IsPhysicalScrollActive: true } ||
                !AppBarSmoothScrollConnection.IsSupported(this, viewer))
            {
                Disconnect();
                return;
            }

            if (_connection is { } && !_connection.Matches(presenter))
                Disconnect();

            _connection ??= new(this, viewer, presenter);
            _connection.Update();
        }
        finally
        {
            _scanning = false;
        }
    }

    private void ViewerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TemplatedControl.TemplateProperty || e.Property == StyledElement.ThemeProperty)
            Disconnect();
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
        foreach (var run in _runs.ToArray())
            run.Cancel();
    }

    internal void Register(CancellationTokenSource run) => _runs.Add(run);
    internal void Unregister(CancellationTokenSource run) => _runs.Remove(run);
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using SmoothScroll.Avalonia.Controls;
using SmoothScroll.Avalonia.Interaction;
using SmoothScroll.Avalonia.Composition;

namespace Material3Expressive.Components;

// Bridges the standard Scaffold + physical SmoothScroll viewport layout.
// Arbitrary sibling layouts cannot be reconstructed on the compositor.
internal sealed class AppBarSmoothScrollConnection : IDisposable
{
    private readonly AppBarScrollScope _scope;
    private readonly ScrollViewer _viewer;
    private readonly ScrollViewerPresenter _presenter;
    private readonly Scaffold _scaffold;
    private readonly TopAppBar? _top;
    private readonly BottomAppBar? _bottom;
    private readonly TopAppBarScrollBehavior? _topBehavior;
    private readonly BottomAppBarScrollBehavior? _bottomBehavior;
    private readonly TopAppBarState? _topState;
    private readonly BottomAppBarState? _bottomState;
    private readonly IDisposable _registration;
    private InteractionTracker? _tracker;
    private CompositionVector3Output? _offsets, _appearance;
    private CompositionColorOutput? _color;
    private AppBarCompositionVisuals? _visuals;
    private AppBarTopChrome? _topChrome;
    private AppBarScrollConfiguration? _configuration;
    private bool _mirroring, _disposed;
    private long _generation, _lastSequence;
    internal AppBarScrollSnapshot LastSnapshot { get; private set; }
    internal bool IsConnected => _tracker is { };
    internal InteractionTracker? Tracker => _tracker;

    internal bool Matches(ScrollViewerPresenter presenter) =>
        ReferenceEquals(_presenter, presenter) &&
        _scope.TopScrollBehavior == _topBehavior &&
        _scope.BottomScrollBehavior == _bottomBehavior &&
        ReferenceEquals(_topState, _topBehavior?.State) &&
        ReferenceEquals(_bottomState, _bottomBehavior?.State) &&
        ReferenceEquals(_top, _scaffold.PresentedTopBar) &&
        ReferenceEquals(_bottom, _scaffold.PresentedBottomBar) &&
        (_tracker is null || ReferenceEquals(
            _topChrome,
            _top?.GetVisualDescendants().OfType<AppBarTopChrome>().FirstOrDefault()));

    internal static bool IsSupported(AppBarScrollScope scope, ScrollViewer viewer)
    {
        // The tracker participant owns the live range. Native snap retargeting
        // is not composable with that range yet; leave the native presenter unmodified.
        if (viewer.VerticalSnapPointsType != Avalonia.Controls.Primitives.SnapPointsType.None ||
            viewer.HorizontalSnapPointsType != Avalonia.Controls.Primitives.SnapPointsType.None)
            return false;
        var scaffold = scope.Host ?? viewer.GetVisualAncestors().OfType<Scaffold>().FirstOrDefault();
        if (scaffold is null || viewer.GetVisualAncestors().OfType<AppBarScrollScope>().FirstOrDefault() != scope)
            return false;
        if (viewer.RenderTransform is { })
            return false;
        if (viewer.GetVisualAncestors().OfType<ScrollViewer>().Any())
            return false;
        if (scope.TopScrollBehavior is { } && scaffold.PresentedTopBar is not TopAppBar)
            return false;
        if (scope.BottomScrollBehavior is { } && scaffold.PresentedBottomBar is not BottomAppBar)
            return false;
        if (scaffold.PresentedTopBar is TopAppBar top && top.ScrollBehavior != scope.TopScrollBehavior)
            return false;
        if (scaffold.PresentedBottomBar is BottomAppBar bottom && bottom.ScrollBehavior != scope.BottomScrollBehavior)
            return false;
        if (scope.TopScrollBehavior is null && scope.BottomScrollBehavior is null)
            return false;
        if (scaffold.PresentedTopBar is TopAppBar actualTop && actualTop.IsArrangeValid &&
            !actualTop.GetVisualDescendants().OfType<AppBarTopChrome>().Any())
            return false;

        var content = viewer.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .FirstOrDefault(presenter => presenter.Name == "PART_ContentPresenter" &&
                                         presenter.TemplatedParent == viewer)?.Child;
        if (content is Avalonia.Controls.Primitives.ILogicalScrollable { IsLogicalScrollEnabled: true })
            return false;
        // Only the documented fill-size body with Scaffold.ContentPadding follows bar geometry.
        // A fixed viewport, an overlay body or a caller's extra layout must keep native layout.
        // Read layout Bounds, not TranslatePoint: that can include asynchronous compositor motion.
        if (viewer.IsArrangeValid && scaffold.Bounds.Height > 0)
        {
            var y = viewer.Bounds.Y;
            foreach (var ancestor in viewer.GetVisualAncestors().TakeWhile(ancestor => ancestor != scaffold))
            {
                if (ancestor.RenderTransform is { })
                    return false;
                y += ancestor.Bounds.Y;
            }

            var tolerance = 1 / LayoutHelper.GetLayoutScale(viewer);
            if (Math.Abs(y - scaffold.ContentPadding.Top) > tolerance ||
                Math.Abs(viewer.Bounds.Height - Math.Max(
                    0,
                    scaffold.Bounds.Height - scaffold.ContentPadding.Top - scaffold.ContentPadding.Bottom)) > tolerance)
                return false;
        }

        return AppBarSpring.TryRead(scope.TopScrollBehavior?.GetSnapAnimationSpec(), out _) &&
               AppBarSpring.TryRead(scope.BottomScrollBehavior?.GetSnapAnimationSpec(), out _);
    }

    internal AppBarSmoothScrollConnection(AppBarScrollScope scope, ScrollViewer viewer,
        ScrollViewerPresenter presenter)
    {
        _scope = scope;
        _viewer = viewer;
        _presenter = presenter;
        _scaffold = scope.Host ?? viewer.GetVisualAncestors().OfType<Scaffold>().First();
        _top = _scaffold.PresentedTopBar;
        _bottom = _scaffold.PresentedBottomBar;
        _topBehavior = scope.TopScrollBehavior;
        _bottomBehavior = scope.BottomScrollBehavior;
        _topState = _topBehavior?.State;
        _bottomState = _bottomBehavior?.State;
        if (_topState is { })
            _topState.PropertyChanged += StateChanged;
        if (_bottomState is { })
            _bottomState.PropertyChanged += StateChanged;
        _viewer.PropertyChanged += ViewerChanged;
        _viewer.AddHandler(InputElement.PointerPressedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        _viewer.AddHandler(InputElement.PointerWheelChangedEvent, BeforeInput, RoutingStrategies.Tunnel, true);
        _registration = presenter.AttachVerticalScroll(Attach);
    }

    private IDisposable Attach(InteractionTracker tracker)
    {
        ++_generation;
        _tracker = tracker;
        _offsets = CompositionVector3Output.Create(tracker.Compositor);
        _appearance = CompositionVector3Output.Create(tracker.Compositor);
        _color = CompositionColorOutput.Create(tracker.Compositor);
        _visuals = new(_offsets, _appearance, _color);
        _topChrome = _top?.GetVisualDescendants().OfType<AppBarTopChrome>().FirstOrDefault();
        _topChrome?.SetComposition(_offsets, _appearance, _color);
        var config = ReadConfiguration(refreshGeometry: true);
        _configuration = config;
        var initial = ReadState();
        var weak = new WeakReference<AppBarSmoothScrollConnection>(this);
        void Publish(AppBarScrollSnapshot snapshot) => tracker.DispatchToUI(() =>
        {
            if (weak.TryGetTarget(out var owner))
                owner.ApplySnapshot(snapshot);
        });
        tracker.ConfigureVerticalScroll(
            new ParticipantFactory(config, initial, _offsets.Writer, _appearance.Writer, _color.Writer, Publish),
            CanConsumeAtBoundary);
        Update();
        return new DetachRegistration(this);
    }

    private sealed record ParticipantFactory(
        AppBarScrollConfiguration Config,
        AppBarScrollSnapshot Initial,
        CompositionVector3OutputWriter Offsets,
        CompositionVector3OutputWriter Appearance,
        CompositionColorOutputWriter Color,
        Action<AppBarScrollSnapshot> Publish) : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create() =>
            new AppBarScrollParticipant(Config, Initial, Offsets, Appearance, Color, Publish);
    }

    private sealed class DetachRegistration(AppBarSmoothScrollConnection owner) : IDisposable
    {
        public void Dispose() => owner.Detach();
    }

    private void BeforeInput(object? sender, PointerEventArgs e)
    {
        if (e is PointerPressedEventArgs)
            _scope.Interrupt();
        else if (e is PointerWheelEventArgs)
            _scope.InterruptUiAnimations();
        UpdateConfiguration(); // Capture current bindable configuration before enqueuing input.
    }

    private void ViewerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ScrollViewer.OffsetProperty)
        {
            _topBehavior?.UpdateContentAtStart(_viewer.Offset.Y == 0);
            UpdateConfiguration();
        }
    }

    private bool CanConsumeAtBoundary(double delta)
    {
        // Bottom ExitAlways observes body movement; it never consumes leftover input.
        // ExitUntilCollapsed can expand in post even though its reverse pre consumes zero.
        if (_configuration is not { TopEnabled: true } config ||
            config.TopKind is null or TopAppBarScrollMode.Pinned || _topBehavior is null)
            return false;
        var collapsed = -_topState!.HeightOffset / config.Density;
        return delta > 0 ? collapsed < config.TopRange : delta < 0 && collapsed > 0;
    }

    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_mirroring || _disposed || _tracker is null)
            return;
        if (e.Property == TopAppBarState.HeightOffsetLimitProperty || e.Property == BottomAppBarState.HeightOffsetLimitProperty)
        {
            UpdateConfiguration();
            return;
        }

        if (e.Property != TopAppBarState.HeightOffsetProperty && e.Property != TopAppBarState.ContentOffsetProperty &&
            e.Property != BottomAppBarState.HeightOffsetProperty && e.Property != BottomAppBarState.ContentOffsetProperty)
            return;

        ++_generation;
        var state = ReadState();
        _tracker.RunOnScrollParticipant(participant =>
            ((AppBarScrollParticipant)participant).SetState(state));
    }

    private AppBarScrollSnapshot ReadState()
    {
        var density = LayoutHelper.GetLayoutScale(_viewer);
        return new(
            _generation,
            0,
            -(_topState?.HeightOffset ?? 0) / density,
            -(_bottomState?.HeightOffset ?? 0) / density,
            (_topState?.ContentOffset ?? 0) / density,
            (_bottomState?.ContentOffset ?? 0) / density,
            _viewer.Offset.Y,
            _viewer.Viewport.Height);
    }

    private AppBarScrollConfiguration ReadConfiguration(bool refreshGeometry = false)
    {
        _topBehavior?.UpdateContentAtStart(_viewer.Offset.Y == 0);
        var density = LayoutHelper.GetLayoutScale(_viewer);
        double Range(float? value) => value is > -1_000_000 and <= 0 ? -value.Value / density : 0;
        double Hidden(float? value) => -MathUtils.RoundToInt(value ?? 0) / density;
        var colors = _topChrome?.GetCompositionColors() ?? (Colors.Transparent, Colors.Transparent, false);
        AppBarSpring.TryRead(_topBehavior?.GetSnapAnimationSpec(), out var topSnap);
        AppBarSpring.TryRead(_bottomBehavior?.GetSnapAnimationSpec(), out var bottomSnap);
        var colorSpring = _topChrome is { } chrome
            ? new AppBarSpring(
                chrome.ContainerColorAnimation.DampingRatio,
                chrome.ContainerColorAnimation.Stiffness,
                .01f)
            : default;
        var bottomHidden = _bottom?.TouchExplorationEnabled == true ? 0 : Hidden(_bottomState?.HeightOffset);
        return new(
            _topBehavior?.Mode,
            Range(_topState?.HeightOffsetLimit),
            Range(_bottomState?.HeightOffsetLimit),
            density,
            _topBehavior?.CanScroll ?? false,
            _bottomBehavior?.CanScroll ?? false,
            _bottom?.TouchExplorationEnabled ?? false,
            _topState?.IsScrollingContentAtStart ?? true,
            colors.Item3,
            colors.Item1,
            colors.Item2,
            topSnap,
            bottomSnap,
            colorSpring,
            refreshGeometry || _configuration is null
                ? _presenter.Viewport.Height - Hidden(_topState?.HeightOffset) - bottomHidden
                : _configuration.Value.ExpandedViewport,
            refreshGeometry || _configuration is null
                ? _presenter.Extent.Height
                : _configuration.Value.Extent);
    }

    private void UpdateConfiguration(bool refreshGeometry = false)
    {
        if (_tracker is null || _disposed)
            return;
        var config = ReadConfiguration(refreshGeometry);
        if (_configuration == config)
            return;
        _configuration = config;
        _tracker.RunOnScrollParticipant(participant =>
            ((AppBarScrollParticipant)participant).Configure(config));
    }

    internal void Update()
    {
        if (_tracker is null || _visuals is null || _disposed || !_viewer.IsArrangeValid)
            return;
        UpdateConfiguration(refreshGeometry: true);
        var density = LayoutHelper.GetLayoutScale(_viewer);
        var top = -MathUtils.RoundToInt(_topState?.HeightOffset ?? 0) / density;
        var bottom = _bottom?.TouchExplorationEnabled == true
            ? 0
            : -MathUtils.RoundToInt(_bottomState?.HeightOffset ?? 0) / density;
        string N(double n) => AppBarCompositionVisuals.Number(n);
        var heightDelta = $"offsets.Value.X + offsets.Value.Y - {N(top + bottom)}";
        _visuals.Bind(_viewer, "Translation", $"Vector3(0, {N(top)} - offsets.Value.X, 0)");
        // Every clipping viewport inside the stock template must expand together. Content itself
        // keeps SmoothScroll's transform; no AppBar displacement is added to the scroll Offset.
        foreach (var visual in new Control[] { _viewer }
                     .Concat(_presenter.GetVisualAncestors()
                         .OfType<Control>()
                         .TakeWhile(control => control != _viewer))
                     .Append(_presenter))
            _visuals.Bind(
                visual,
                "Size",
                $"Vector2({N(visual.Bounds.Width)}, Max(0, {N(visual.Bounds.Height)} + {heightDelta}))");

        if (_bottom is { })
            _visuals.Bind(_bottom, "Translation", $"Vector3(0, offsets.Value.Y - {N(bottom)}, 0)");
        var layout = _scaffold.GetVisualDescendants()
            .OfType<ScaffoldLayout>()
            .FirstOrDefault();
        if (layout is { })
        {
            // Scaffold places FAB/snackbar above the bottom bar. Their visual positions follow
            // the same live bar height until the next real arrange replaces this baseline.
            var slots = layout.Children.OfType<ContentPresenter>().ToArray();
            var fab = slots.FirstOrDefault(slot => slot.Name == "PART_FloatingActionButton");
            var hasFab = fab is { DesiredSize.Width: > 0, DesiredSize.Height: > 0 };
            foreach (var slot in slots.Where(slot => slot.Name is "PART_SnackbarHost" or "PART_FloatingActionButton"))
            {
                var follows = _scaffold.FloatingActionButtonPosition != FabPosition.EndOverlay ||
                              slot.Name == "PART_SnackbarHost" && !hasFab;
                _visuals.Bind(
                    slot,
                    "Translation",
                    follows ? $"Vector3(0, offsets.Value.Y - {N(bottom)}, 0)" : "Vector3(0, 0, 0)");
            }
        }
    }

    private void ApplySnapshot(AppBarScrollSnapshot snapshot)
    {
        if (_disposed || _tracker is null || snapshot.Generation != _generation || snapshot.Sequence <= _lastSequence)
            return;
        _lastSequence = snapshot.Sequence;
        LastSnapshot = snapshot;
        var density = LayoutHelper.GetLayoutScale(_viewer);
        _mirroring = true;
        try
        {
            if (_topState is { } top)
            {
                top.HeightOffset = (float)(-snapshot.TopCollapsed * density);
                top.ContentOffset = (float)(snapshot.TopContentOffset * density);
            }

            if (_bottomState is { } bottom)
            {
                bottom.HeightOffset = (float)(-snapshot.BottomHidden * density);
                bottom.ContentOffset = (float)(snapshot.BottomContentOffset * density);
            }
        }
        finally
        {
            _mirroring = false;
        }
        // LayoutUpdated commits the resulting real layout and its matching expression baseline.
        // Do not publish old Offset as a command here; the presenter owns the tracker mirror.
    }

    internal void Interrupt() => _tracker?.CancelScroll();

    private void Detach()
    {
        ++_generation;
        _tracker?.ConfigureVerticalScroll(null);
        _tracker = null;
        _topChrome?.SetComposition(null, null);
        _topChrome = null;
        _visuals?.Dispose();
        _visuals = null;
        _offsets?.Dispose();
        _appearance?.Dispose();
        _color?.Dispose();
        _offsets = _appearance = null;
        _color = null;
        _configuration = null;
        _lastSequence = 0;
        _topBehavior?.UpdateContentAtStart(true);
        _viewer.InvalidateArrange();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _viewer.RemoveHandler(InputElement.PointerPressedEvent, BeforeInput);
        _viewer.RemoveHandler(InputElement.PointerWheelChangedEvent, BeforeInput);
        _viewer.PropertyChanged -= ViewerChanged;
        if (_topState is { })
            _topState.PropertyChanged -= StateChanged;
        if (_bottomState is { })
            _bottomState.PropertyChanged -= StateChanged;
        _registration.Dispose();
    }
}

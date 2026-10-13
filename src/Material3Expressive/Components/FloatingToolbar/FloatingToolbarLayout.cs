using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

// FloatingToolbar.kt: both ordinary layouts and both WithFab Layout measurement blocks.
internal sealed class FloatingToolbarLayout : Panel
{
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> ElevationAnimationProperty = AvaloniaProperty.Register<FloatingToolbarLayout, Foundation.Motion.SpringSpec>(nameof(ElevationAnimation));
    public Foundation.Motion.SpringSpec ElevationAnimation { get => GetValue(ElevationAnimationProperty); set => SetValue(ElevationAnimationProperty, value); }
    public static readonly StyledProperty<double> ContainerSizeProperty = AvaloniaProperty.Register<FloatingToolbarLayout, double>(nameof(ContainerSize));
    static FloatingToolbarLayout() => AffectsMeasure<FloatingToolbarLayout>(ContainerSizeProperty);
    public double ContainerSize { get => GetValue(ContainerSizeProperty); set => SetValue(ContainerSizeProperty, value); }
    private FloatingToolbar? _owner;
    private Transition<bool>? _transition;
    private Transition<bool>.TransitionAnimationState<float>? _progress;
    private Animatable<float>? _elevation;
    private FloatingToolbarScrollBehavior? _behavior;
    private ScrollViewer? _source;
    private readonly TranslateTransform _offset = new();
    private readonly TranslateTransform _zeroWidthPlacement = new();
    private readonly VelocityTracker _velocity = new();
    private bool _pending, _dragging;
    private bool _initialExpanded;
    private Point? _pressed;
    private float _dragPosition;
    private Size _naturalToolbar;
    private Size _toolbarSize;
    private double _fabSize;
    internal float ExpandedProgress => _progress?.Value ?? (_initialExpanded ? 1 : 0);
    private MaterialSurface Surface => (MaterialSurface)Children[0];
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<FloatingToolbar>()!;
        _initialExpanded = _owner.EffectiveExpanded;
        InitializeMotion();
        _owner.PropertyChanged += OwnerChanged;
        AddHandler(PointerPressedEvent, Pressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, Moved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, Released, RoutingStrategies.Tunnel, handledEventsToo: true);
        PointerCaptureLost += CaptureLost;
        AttachScroll(); InvalidateMeasure();
    }
    private void InitializeMotion()
    {
        if (!this.IsAttachedToVisualTree() || _transition is not null || _owner?.AnimationSpec is not { } animationSpec
            || ElevationAnimation.Stiffness <= 0) return;
        var scope = AnimationScope.Get(this);
        _transition = new Transition<bool>(_initialExpanded, "Floating toolbar expanded");
        _progress = _transition.AnimateValue(new FloatConverter(), expanded => expanded ? 1f : 0f,
            _ => animationSpec);
        _progress.PropertyChanged += ProgressChanged;
        // Capture Avalonia's Normal dispatcher context, independent of the context
        // which mounted the template. Transition waits asynchronously while idle.
        var transition = _transition;
        Dispatcher.UIThread.Post(() =>
        {
            if (!scope.Lifetime.IsCancellationRequested) _ = scope.Launch(ct => transition.RunAsync(scope, ct), "Floating toolbar transition");
        }, DispatcherPriority.Normal);
        _elevation = new Animatable<float>(new FloatConverter(), (float)ElevationTarget, VisibilityThresholds.Float);
        scope.SubscribeValue(_elevation, value => Surface.ShadowElevation = value);
        Surface.ShadowElevation = _elevation.Value;
        UpdateTarget();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ElevationAnimationProperty) { InitializeMotion(); UpdateTarget(); }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        DetachScroll();
        if (_progress is not null) _progress.PropertyChanged -= ProgressChanged;
        _transition?.Dispose(); _transition = null; _progress = null; _elevation = null; _owner = null; _pending = false;
        foreach (var lease in _focus.Values) lease.Dispose(); _focus.Clear();
        RemoveHandler(PointerPressedEvent, Pressed); RemoveHandler(PointerMovedEvent, Moved); RemoveHandler(PointerReleasedEvent, Released);
        PointerCaptureLost -= CaptureLost; _pressed = null; _dragging = false;
        base.OnDetachedFromVisualTree(e);
    }
    private double ElevationTarget => _owner!.EffectiveExpanded ? _owner.ExpandedShadowElevation : _owner.CollapsedShadowElevation;
    private void ProgressChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == "Value") InvalidateMeasure(); }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FloatingToolbar.ScrollBehaviorProperty || e.Property == FloatingToolbar.ScrollSourceProperty || e.Property == FloatingToolbar.TouchExplorationEnabledProperty)
        { DetachScroll(); AttachScroll(); }
        if (e.Property == FloatingToolbar.EffectiveExpandedProperty || e.Property == FloatingToolbar.ExpandedShadowElevationProperty || e.Property == FloatingToolbar.CollapsedShadowElevationProperty || e.Property == FloatingToolbar.AnimationSpecProperty)
        {
            InitializeMotion();
            UpdateTarget();
        }
        InvalidateMeasure();
    }
    private void UpdateTarget()
    {
        if (_pending || _transition is null || !this.IsAttachedToVisualTree()) return;
        var scope = AnimationScope.Get(this);
        _pending = true;
        _ = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct); _pending = false;
            if (_transition is null || _progress is null || _elevation is null) return;
            if (_owner?.AnimationSpec is not { } animationSpec || ElevationAnimation.Stiffness <= 0) return;
            var elevation = ElevationAnimation;
            _progress.Update(expanded => expanded ? 1f : 0f, _ => animationSpec);
            _transition.UpdateTarget(_owner.EffectiveExpanded);
            if (_owner.FloatingActionButton is null)
                await _elevation.AnimateToAsync(scope, (float)ElevationTarget,
                    new SpringSpec<float>((float)elevation.DampingRatio, (float)elevation.Stiffness, .01f), cancellationToken: ct);
        }, "Floating toolbar target");
    }
    private void AttachScroll()
    {
        if (_owner is null || !this.IsAttachedToVisualTree() || _owner.TouchExplorationEnabled) return;
        var scope = AnimationScope.Get(this);
        _behavior = _owner.ScrollBehavior;
        if (_behavior is not null) { _behavior.Attach(_owner, scope); _behavior.State.PropertyChanged += ScrollStateChanged; }
        _source = _owner.ScrollSource;
        if (_source is not null) _source.ScrollChanged += Scrolled;
    }
    private void DetachScroll()
    {
        if (_source is not null) _source.ScrollChanged -= Scrolled;
        if (_behavior is not null) { _behavior.State.PropertyChanged -= ScrollStateChanged; _behavior.Detach(); }
        _behavior = null; _source = null; _offset.X = _offset.Y = 0;
    }
    private void Scrolled(object? sender, ScrollChangedEventArgs e) => _behavior?.OnPostScroll(-(float)e.OffsetDelta.Y);
    private void ScrollStateChanged(object? sender, PropertyChangedEventArgs e) => InvalidateArrange();
    private double Axis(Point point) => _behavior?.ExitDirection is FloatingToolbarExitDirection.Start or FloatingToolbarExitDirection.End ? point.X : point.Y;
    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (_behavior is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pressed = e.GetPosition(this); _dragging = false; _velocity.Reset(); _dragPosition = 0; _velocity.Add(e.Timestamp, 0);
    }
    private void Moved(object? sender, PointerEventArgs e)
    {
        if (_pressed is not { } previous || _behavior is null) return;
        var point = e.GetPosition(this);
        var delta = (float)(Axis(point) - Axis(previous));
        if (!_dragging)
        {
            // Foundation awaitPointerSlopOrCancellation uses 0.125dp for a mouse and
            // the default ViewConfiguration's 18dp touch slop for other pointers.
            var threshold = e.Pointer.Type == PointerType.Mouse ? .125 : 18;
            if (Math.Abs(delta) <= threshold) return;
            delta -= Math.Sign(delta) * (float)threshold;
            _dragging = true; _behavior.Interrupt(); e.Pointer.Capture(this);
        }
        _pressed = point; _dragPosition += delta; _velocity.Add(e.Timestamp, _dragPosition);
        var offset = _behavior.ExitDirection is FloatingToolbarExitDirection.Start or FloatingToolbarExitDirection.End && FlowDirection == FlowDirection.RightToLeft ? -delta : delta;
        _behavior.State.Offset += _behavior.ExitDirection is FloatingToolbarExitDirection.Start or FloatingToolbarExitDirection.Top ? offset : -offset;
        e.Handled = true;
    }
    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging || _behavior is null || !this.IsAttachedToVisualTree()) { _pressed = null; return; }
        var behavior = _behavior;
        var velocity = _velocity.Calculate(e.Timestamp);
        var scope = AnimationScope.Get(this);
        _pressed = null; _dragging = false; e.Pointer.Capture(null); e.Handled = true;
        _ = scope.Launch(async ct => { await behavior.SettleAsync(velocity, ct); }, "Floating toolbar drag settle");
    }
    private void CaptureLost(object? sender, PointerCaptureLostEventArgs e) { _pressed = null; _dragging = false; }

    protected override Size MeasureOverride(Size available)
    {
        if (_owner is null || Children.Count == 0) return default;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Px(double value) => MathUtils.DipToPx(value, scale) / scale;
        if (_owner.FloatingActionButton is null)
        {
            Surface.Measure(available);
            var size = Surface.DesiredSize;
            return _owner.Horizontal ? new Size(size.Width, Math.Max(size.Height, Math.Min(Px(ContainerSize), available.Height)))
                : new Size(Math.Max(size.Width, Math.Min(Px(ContainerSize), available.Width)), size.Height);
        }
        // maxIntrinsicWidth/Height before the progress-constrained viewport measurement.
        if (_owner.Horizontal) Surface.MinHeight = Math.Min(Px(ContainerSize), available.Height);
        else Surface.MinWidth = Math.Min(Px(ContainerSize), available.Width);
        Surface.Measure(_owner.Horizontal ? new Size(double.PositiveInfinity, Px(ContainerSize)) : new Size(Px(ContainerSize), double.PositiveInfinity));
        _naturalToolbar = Surface.DesiredSize;
        _fabSize = Px(MathUtils.Lerp(56f, 80f, 1f - ExpandedProgress));
        Children[1].Measure(new Size(_fabSize, _fabSize));
        var target = Math.Max(0, Math.Truncate((_owner.Horizontal ? _naturalToolbar.Width : _naturalToolbar.Height) * scale * ExpandedProgress)) / scale;
        Surface.Measure(_owner.Horizontal ? new Size(Math.Min(target, available.Width), available.Height) : new Size(available.Width, Math.Min(target, available.Height)));
        _toolbarSize = Surface.DesiredSize;
        return _owner.Horizontal ? new Size(_naturalToolbar.Width + Px(8) + Px(56), Math.Min(Px(80), available.Height))
            : new Size(Math.Min(Px(80), available.Width), _naturalToolbar.Height + Px(8) + Px(56));
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (_owner is null || Children.Count == 0) return final;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Center(double outer, double inner) => Math.Truncate((outer - inner) * scale / 2) / scale;
        if (_owner.FloatingActionButton is null) Surface.Arrange(new Rect(final));
        else
        {
            var horizontal = _owner.Horizontal;
            var natural = horizontal ? _naturalToolbar.Width : _naturalToolbar.Height;
            var length = natural + MathUtils.DipToPx(8, scale) / scale + MathUtils.DipToPx(56, scale) / scale;
            // Compose Layout may report a size outside its constraints. Placeable then
            // centers that real allocation inside the parent's coerced apparent size.
            var apparentOffset = Math.Truncate(((horizontal ? final.Width : final.Height) - length) * scale / 2) / scale;
            var toolbar = horizontal ? _toolbarSize.Width : _toolbarSize.Height;
            var fabAtEnd = _owner.FabAtEnd;
            var toolbarPosition = fabAtEnd ? natural - toolbar : length - natural;
            var fabPosition = fabAtEnd ? length - _fabSize : 0;
            toolbarPosition += apparentOffset; fabPosition += apparentOffset;
            // Avalonia mirrors the visual subtree for RTL. Coordinates here remain logical.
            Surface.Arrange(horizontal ? new Rect(toolbarPosition, Center(final.Height, _toolbarSize.Height), _toolbarSize.Width, _toolbarSize.Height)
                : new Rect(Center(final.Width, _toolbarSize.Width), toolbarPosition, _toolbarSize.Width, _toolbarSize.Height));
            // Force the slot to the progress-selected constraints; helper FAB themes stretch to it.
            Children[1].Arrange(horizontal ? new Rect(fabPosition, Center(final.Height, _fabSize), _fabSize, _fabSize)
                : new Rect(Center(final.Width, _fabSize), fabPosition, _fabSize, _fabSize));
            var progress = Math.Min(ExpandedProgress, 1);
            Surface.ShadowElevation = MathUtils.Lerp((float)_owner.CollapsedShadowElevation, (float)_owner.ExpandedShadowElevation, progress);
            Surface.IsHitTestVisible = toolbar != 0;
            if (horizontal && Surface.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is { } scroller)
            {
                // Placeable.placeRelative does not mirror when its parentWidth is zero.
                // Avalonia still mirrors the zero-width viewport and padding layouts.
                // Restore those two skipped placements, keeping native scroll input/clipping.
                _zeroWidthPlacement.X = FlowDirection == FlowDirection.RightToLeft && scroller.Viewport.Width == 0
                    ? -scroller.Extent.Width - (toolbar == 0 ? Surface.Padding.Left + Surface.Padding.Right : 0) : 0;
                scroller.RenderTransform = _zeroWidthPlacement;
            }
        }
        if (_behavior is { } behavior && _owner.Parent is Control parent)
        {
            var position = _owner.TranslatePoint(default, parent) ?? default;
            var rtl = FlowDirection == FlowDirection.RightToLeft;
            var limit = behavior.ExitDirection switch
            {
                FloatingToolbarExitDirection.Start => rtl ? parent.Bounds.Width - position.X : final.Width + position.X,
                FloatingToolbarExitDirection.End => rtl ? final.Width + position.X : parent.Bounds.Width - position.X,
                FloatingToolbarExitDirection.Top => final.Height + position.Y,
                _ => parent.Bounds.Height - position.Y,
            };
            // positionInParent observes the rounded pixel placement, but subtracts the
            // unrounded Float state. Keep that difference and the physical Float arithmetic.
            var offsetPx = behavior.State.Offset * (float)scale;
            var roundedOffset = MathUtils.RoundToInt(offsetPx);
            behavior.State.OffsetLimit = -((float)(limit * scale) + roundedOffset - offsetPx) / (float)scale;
            var offset = roundedOffset / scale;
            // The visual subtree already mirrors horizontal translation for RTL.
            _offset.X = behavior.ExitDirection switch { FloatingToolbarExitDirection.Start => offset, FloatingToolbarExitDirection.End => -offset, _ => 0 };
            _offset.Y = behavior.ExitDirection switch { FloatingToolbarExitDirection.Top => offset, FloatingToolbarExitDirection.Bottom => -offset, _ => 0 };
        }
        foreach (var child in Children) child.RenderTransform = _offset;
        // Upstream ordinary overload suppresses descendant focus whenever offset is nonzero.
        // Keep each descendant's original Focusable value by using an animation-priority lease.
        UpdateFocus();
        return final;
    }
    private readonly Dictionary<Control, IDisposable> _focus = [];
    private void UpdateFocus()
    {
        var hidden = _owner is { FloatingActionButton: null } && (_behavior?.State.Offset ?? 0) != 0;
        if (!hidden) { foreach (var lease in _focus.Values) lease.Dispose(); _focus.Clear(); return; }
        var descendants = this.GetVisualDescendants().OfType<Control>().ToHashSet();
        foreach (var control in _focus.Keys.Where(control => !descendants.Contains(control)).ToArray())
        {
            _focus[control].Dispose(); _focus.Remove(control);
        }
        // Lease even initially non-focusable descendants, so an application update
        // cannot re-enable focus while the parent toolbar remains displaced.
        foreach (var control in descendants)
            if (!_focus.ContainsKey(control)) _focus[control] = control.SetValue(InputElement.FocusableProperty, false, Avalonia.Data.BindingPriority.Animation)!;
    }
}

// Row/Column without inserting spacing absent from the upstream composable.
internal sealed class FloatingToolbarContentLayout : Panel
{
    private FloatingToolbar Owner => this.FindAncestorOfType<FloatingToolbar>()!;
    protected override Size MeasureOverride(Size available)
    {
        var horizontal = Owner.Horizontal;
        var remaining = available;
        var main = 0d; var cross = 0d;
        foreach (var child in Children)
        {
            child.Measure(remaining);
            var size = child.DesiredSize;
            main += horizontal ? size.Width : size.Height;
            cross = Math.Max(cross, horizontal ? size.Height : size.Width);
            remaining = horizontal ? new Size(Math.Max(0, available.Width - main), available.Height) : new Size(available.Width, Math.Max(0, available.Height - main));
        }
        return horizontal ? new Size(main, cross) : new Size(cross, main);
    }
    protected override Size ArrangeOverride(Size final)
    {
        var horizontal = Owner.Horizontal;
        var scale = LayoutHelper.GetLayoutScale(this);
        // The ordinary Row/Column uses Arrangement.Center, including when callers
        // give the toolbar extra space through an explicit Width/Height.
        var occupied = Children.Sum(child => horizontal ? child.DesiredSize.Width : child.DesiredSize.Height);
        var offset = MathUtils.RoundToInt((float)(((horizontal ? final.Width : final.Height) - occupied) * scale / 2)) / scale;
        foreach (var child in Children)
        {
            var size = child.DesiredSize;
            var x = horizontal ? offset : Math.Floor((final.Width - size.Width) * scale / 2 + .5) / scale;
            var y = horizontal ? Math.Floor((final.Height - size.Height) * scale / 2 + .5) / scale : offset;
            child.Arrange(new Rect(x, y, size.Width, size.Height));
            offset += horizontal ? size.Width : size.Height;
        }
        return final;
    }
}

internal sealed class FloatingToolbarBalancedPadding : Decorator
{
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> PaddingAnimationProperty = AvaloniaProperty.Register<FloatingToolbarBalancedPadding, Foundation.Motion.SpringSpec>(nameof(PaddingAnimation));
    public Foundation.Motion.SpringSpec PaddingAnimation { get => GetValue(PaddingAnimationProperty); set => SetValue(PaddingAnimationProperty, value); }

    private FloatingToolbar? _owner;
    private Animatable<float>? _padding;
    private Point _lines;
    private bool _pending;
    private bool HasSide => _owner!.EffectiveExpanded && (_owner.LeadingContent is not null || _owner.TrailingContent is not null);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); _owner = this.FindAncestorOfType<FloatingToolbar>()!;
        var scope = AnimationScope.Get(this); _padding = new Animatable<float>(new FloatConverter(), HasSide ? 0f : 1f, VisibilityThresholds.Float);
        scope.SubscribeValue(_padding, _ => InvalidateMeasure()); _owner.PropertyChanged += Changed;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= Changed;
        _padding = null; _owner = null; _pending = false; _lines = default;
        base.OnDetachedFromVisualTree(e);
    }
    private void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FloatingToolbar.EffectiveExpandedProperty || e.Property == FloatingToolbar.LeadingContentProperty || e.Property == FloatingToolbar.TrailingContentProperty) UpdateTarget();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PaddingAnimationProperty) UpdateTarget();
    }
    private void UpdateTarget()
    {
        if (_pending || !this.IsAttachedToVisualTree()) return;
        var scope = AnimationScope.Get(this);
        _pending = true;
        _ = scope.Launch(async ct => { await scope.NextFrameAsync(ct); _pending = false;
            if (_padding is null || _owner is null) return;
            var animation = PaddingAnimation;
            if (animation.Stiffness <= 0) return;
            await _padding.AnimateToAsync(scope, HasSide ? 0 : 1, new SpringSpec<float>((float)animation.DampingRatio, (float)animation.Stiffness, .01f), cancellationToken: ct); }, "Floating toolbar balanced padding");
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        Child?.Measure(available);
        var size = Child?.DesiredSize ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var progress = _padding?.Value ?? (HasSide ? 0 : 1);
        var missingSide = !_owner!.EffectiveExpanded || _owner.LeadingContent is null || _owner.TrailingContent is null;
        var x = missingSide ? (float)_lines.X * progress : 0;
        var y = missingSide ? (float)_lines.Y * progress : 0;
        return new Size(size.Width + MathUtils.RoundToInt(Math.Max(0, (y - x) * 2) * scale) / scale,
            size.Height + MathUtils.RoundToInt(Math.Max(0, (x - y) * 2) * scale) / scale);
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (Child is null) return final;
        var size = Child.DesiredSize; var scale = LayoutHelper.GetLayoutScale(this);
        Child.Arrange(new Rect(Math.Truncate((final.Width - size.Width) * scale / 2) / scale, Math.Truncate((final.Height - size.Height) * scale / 2) / scale, size.Width, size.Height));
        var lines = MinimumInteractiveAlignment.Read(Child);
        if (_lines != lines) { _lines = lines; InvalidateMeasure(); }
        return final;
    }
}

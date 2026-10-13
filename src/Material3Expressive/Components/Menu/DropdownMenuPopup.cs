/* Menu.kt: DropdownMenuPopup / DropdownMenuPopupContent; Core Popup.skiko.kt.
 * Copyright 2024-2026 The Android Open Source Project; Apache-2.0. */
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Overlays;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

/// <summary>A controlled menu popup with composable content and Material appearance motion.</summary>
/// <remarks>Like Compose, clicking an item does not implicitly dismiss the menu.</remarks>
public class DropdownMenuPopup : ItemsControl
{
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<DropdownMenuPopup, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<Control?> PlacementTargetProperty = AvaloniaProperty.Register<DropdownMenuPopup, Control?>(nameof(PlacementTarget));
    public static readonly StyledProperty<Vector> OffsetProperty = AvaloniaProperty.Register<DropdownMenuPopup, Vector>(nameof(Offset));
    public static readonly StyledProperty<MenuAnchorPosition> AnchorPositionProperty = AvaloniaProperty.Register<DropdownMenuPopup, MenuAnchorPosition>(nameof(AnchorPosition), MenuAnchorPosition.Below);
    public static readonly StyledProperty<IDropdownMenuPositionProvider?> PositionProviderProperty = AvaloniaProperty.Register<DropdownMenuPopup, IDropdownMenuPositionProvider?>(nameof(PositionProvider));
    public static readonly StyledProperty<bool> IsPopupFocusableProperty = AvaloniaProperty.Register<DropdownMenuPopup, bool>(nameof(IsPopupFocusable), true);
    public static readonly StyledProperty<bool> DismissOnBackProperty = AvaloniaProperty.Register<DropdownMenuPopup, bool>(nameof(DismissOnBack), true);
    public static readonly StyledProperty<bool> DismissOnPointerOutsideProperty = AvaloniaProperty.Register<DropdownMenuPopup, bool>(nameof(DismissOnPointerOutside), true);
    public static readonly StyledProperty<bool?> ConsumePointerInputOutsideProperty = AvaloniaProperty.Register<DropdownMenuPopup, bool?>(nameof(ConsumePointerInputOutside));
    public static readonly StyledProperty<bool> ClippingEnabledProperty = AvaloniaProperty.Register<DropdownMenuPopup, bool>(nameof(ClippingEnabled), true);
    public static readonly StyledProperty<bool> UsePlatformDefaultWidthProperty = AvaloniaProperty.Register<DropdownMenuPopup, bool>(nameof(UsePlatformDefaultWidth));
    public static readonly DirectProperty<DropdownMenuPopup, bool> IsPresentedProperty = AvaloniaProperty.RegisterDirect<DropdownMenuPopup, bool>(nameof(IsPresented), o => o.IsPresented);
    internal static readonly StyledProperty<MaterialSpring?> ScaleAnimationProperty = AvaloniaProperty.Register<DropdownMenuPopup, MaterialSpring?>(nameof(ScaleAnimation));
    internal static readonly StyledProperty<MaterialSpring?> AlphaAnimationProperty = AvaloniaProperty.Register<DropdownMenuPopup, MaterialSpring?>(nameof(AlphaAnimation));
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public Control? PlacementTarget { get => GetValue(PlacementTargetProperty); set => SetValue(PlacementTargetProperty, value); }
    public Vector Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }
    public MenuAnchorPosition AnchorPosition { get => GetValue(AnchorPositionProperty); set => SetValue(AnchorPositionProperty, value); }
    public IDropdownMenuPositionProvider? PositionProvider { get => GetValue(PositionProviderProperty); set => SetValue(PositionProviderProperty, value); }
    public bool IsPopupFocusable { get => GetValue(IsPopupFocusableProperty); set => SetValue(IsPopupFocusableProperty, value); }
    public bool DismissOnBack { get => GetValue(DismissOnBackProperty); set => SetValue(DismissOnBackProperty, value); }
    public bool DismissOnPointerOutside { get => GetValue(DismissOnPointerOutsideProperty); set => SetValue(DismissOnPointerOutsideProperty, value); }
    public bool? ConsumePointerInputOutside { get => GetValue(ConsumePointerInputOutsideProperty); set => SetValue(ConsumePointerInputOutsideProperty, value); }
    public bool ClippingEnabled { get => GetValue(ClippingEnabledProperty); set => SetValue(ClippingEnabledProperty, value); }
    public bool UsePlatformDefaultWidth { get => GetValue(UsePlatformDefaultWidthProperty); set => SetValue(UsePlatformDefaultWidthProperty, value); }
    public bool IsPresented { get => _isPresented; private set => SetAndRaise(IsPresentedProperty, ref _isPresented, value); }
    internal MaterialSpring? ScaleAnimation { get => GetValue(ScaleAnimationProperty); set => SetValue(ScaleAnimationProperty, value); }
    internal MaterialSpring? AlphaAnimation { get => GetValue(AlphaAnimationProperty); set => SetValue(AlphaAnimationProperty, value); }
    public event EventHandler<DismissRequestedEventArgs>? DismissRequested;

    private readonly Popup _popup;
    private OverlayPopupHost? _host;
    private Grid? _root;
    private Border? _presentation;
    private Control? _layer;
    private TopLevel? _topLevel;
    private bool _recomposePending;
    private MutableTransitionState<bool>? _state;
    private Transition<bool>? _transition;
    private AnimationChildScope? _attachScope, _presentationScope;
    private bool _isPresented, _redirectingFocus;
    private IInputElement? _previousFocus;
    private Control? _lastFocus;
    private Rect? _lastAnchor;
    private Size _lastWindowSize;
    private readonly Border _inputShield = new() { Background = Brushes.Transparent, FlowDirection = FlowDirection.LeftToRight };
    private Panel? _shieldLayer;
    internal Popup Popup => _popup;
    internal Control? Presentation => _layer;
    internal float AppearanceScale { get; private set; } = .8f;
    internal float AppearanceAlpha { get; private set; }

    public DropdownMenuPopup()
    {
        _popup = new Popup { ShouldUseOverlayLayer = true, IsLightDismissEnabled = false,
            WindowManagerAddShadowHint = false, Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = PlacePopup };
        _popup.Styles.Add(new Style(s => s.OfType<OverlayPopupHost>()) { Setters = { new Setter(ClipToBoundsProperty, false) } });
        _popup.Styles.Add(new Style(s => s.OfType<OverlayPopupHost>().Template().OfType<LayoutTransformControl>())
            { Setters = { new Setter(ClipToBoundsProperty, false) } });
        LogicalChildren.Add(_popup);
        // Items retain their logical owner after the presenter moves into Popup.
        AddHandler(KeyDownEvent, OnMenuKeyDown, RoutingStrategies.Bubble);
        LayoutUpdated += (_, _) => UpdatePosition();
    }

    protected override Size MeasureCore(Size availableSize) { ApplyTemplate(); return default; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        ClosePresentation(true);
        _root?.Children.Remove(_popup);
        if (_presentation is not null) _presentation.RemoveHandler(KeyDownEvent, OnMenuKeyDown);
        _popup.Child = null;
        base.OnApplyTemplate(e);
        _root = e.NameScope.Find<Grid>("PART_Root");
        _presentation = e.NameScope.Find<Border>("PART_Presentation");
        _layer = e.NameScope.Find<Control>("PART_MenuLayer");
        if (_presentation is not null)
        {
            _root?.Children.Remove(_presentation);
            _popup.Child = _presentation;
            RenderOptions.SetRequiresFullOpacityHandling(_presentation, true);
            _presentation.AddHandler(KeyDownEvent, OnMenuKeyDown, RoutingStrategies.Bubble);
            KeyboardNavigation.SetTabNavigation(_presentation, KeyboardNavigationMode.Cycle);
        }
        _root?.Children.Add(_popup);
        UpdatePresentation();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attachScope = AnimationScope.Get(this).CreateChild("Dropdown menu");
        Dispatcher.UIThread.Post(() => { if (this.IsAttachedToVisualTree()) { ApplyTemplate(); UpdatePresentation(); } });
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClosePresentation(true);
        _attachScope?.Dispose(); _attachScope = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty && _presentation is not null && this.IsAttachedToVisualTree())
            QueuePresentation();
        if (change.Property == ScaleAnimationProperty || change.Property == AlphaAnimationProperty)
            UpdatePresentation();
        if (change.Property == PlacementTargetProperty)
        { ClosePresentation(true); UpdatePresentation(); }
        if (change.Property == IsPopupFocusableProperty && IsPresented && _presentation is not null)
        {
            _presentation.Focusable = IsPopupFocusable;
            _popup.TakesFocusFromNativeControl = IsPopupFocusable;
            if (IsPopupFocusable)
            {
                _previousFocus = _topLevel?.FocusManager?.GetFocusedElement();
                _presentation.Focus();
            }
            else if (_previousFocus is Control { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } previous
                && previous.IsAttachedToVisualTree()) previous.Focus();
        }
        if (change.Property == IsPopupFocusableProperty || change.Property == ConsumePointerInputOutsideProperty)
            UpdateInputShield();
        if (change.Property == OffsetProperty || change.Property == AnchorPositionProperty || change.Property == PositionProviderProperty
            || change.Property == FlowDirectionProperty || change.Property == ClippingEnabledProperty
            || change.Property == UsePlatformDefaultWidthProperty) UpdatePosition(true);
    }

    private void QueuePresentation()
    {
        if (_recomposePending) return;
        if (!this.IsAttachedToVisualTree() || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested) return;
        _recomposePending = true;
        _ = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct);
            _recomposePending = false;
            UpdatePresentation();
        }, "Dropdown composition");
    }

    private void UpdatePresentation()
    {
        if (_presentation is null || _layer is null || !this.IsAttachedToVisualTree()) return;
        if (ScaleAnimation is null || AlphaAnimation is null) return;
        _state ??= new(false);
        if (!(_state.CurrentState || IsOpen))
        {
            var removedTransition = _transition is not null;
            // The guard belongs to the outer composable. Do not update a child Transition
            // that it removes. onDisposed completes that Transition's former target, while
            // the remembered MutableTransitionState outlives the popup composition.
            ClosePresentation(true, preserveState: true);
            _state.TargetState = IsOpen;
            if (removedTransition && _state.CurrentState) QueuePresentation();
            return;
        }
        _state.TargetState = IsOpen;
        if (_transition is not null) return;
        var anchor = PlacementTarget ?? this.GetVisualParent() as Control;
        if (anchor is null || !anchor.IsAttachedToVisualTree()) return;
        if (_attachScope is not { } attachScope || attachScope.Lifetime.IsCancellationRequested) return;
        _topLevel = TopLevel.GetTopLevel(anchor);
        if (_topLevel is null || _topLevel != TopLevel.GetTopLevel(this)) return;
        _previousFocus = _topLevel.FocusManager?.GetFocusedElement();
        _popup.PlacementTarget = anchor;
        _popup.TakesFocusFromNativeControl = IsPopupFocusable;
        _presentation.Focusable = IsPopupFocusable;
        _topLevel.AddHandler(PointerPressedEvent, OnOutsidePressed, RoutingStrategies.Tunnel, true);
        _topLevel.AddHandler(GotFocusEvent, OnWindowFocus, RoutingStrategies.Bubble, true);
        _topLevel.BackRequested += OnBackRequested;
        _topLevel.LayoutUpdated += OnWindowLayout;
        _topLevel.PropertyChanged += OnWindowPropertyChanged;
        var motion = _presentationScope = attachScope.CreateChild("Dropdown presentation");
        _transition = new Transition<bool>(_state, "DropDownMenu");
        var scale = _transition.AnimateValue(new FloatConverter(), open => open ? 1f : .8f,
            _ => Spec(ScaleAnimation!.Value));
        var alpha = _transition.AnimateValue(new FloatConverter(), open => open ? 1f : 0f,
            _ => Spec(AlphaAnimation!.Value));
        motion.SubscribeValue(scale, value => { AppearanceScale = value; UpdateAppearance(); });
        motion.SubscribeValue(alpha, value => { AppearanceAlpha = value; UpdateAppearance(); });
        AppearanceScale = _state.CurrentState ? 1 : .8f;
        AppearanceAlpha = _state.CurrentState ? 1 : 0; UpdateAppearance();
        _state.PropertyChanged += OnTransitionChanged;
        _popup.Open();
        // Popup.Show attaches the host immediately, but during Window's initial layout
        // its template may not have attached Child yet. Find the host by the logical
        // parent assigned by Popup.Open, rather than walking Child's visual ancestors.
        _host = _topLevel.GetVisualDescendants().OfType<OverlayPopupHost>()
            .Single(h => ReferenceEquals(h.Parent, _popup));
        IsPresented = _popup.IsOpen;
        UpdateInputShield();
        UpdatePosition(true);
        if (IsPopupFocusable) _presentation.Focus();
        var transition = _transition;
        _ = motion.Launch(ct => transition.RunAsync(motion, ct), "Dropdown appearance");
    }

    private static SpringSpec<float> Spec(MaterialSpring spec) => new(spec.DampingRatio, spec.Stiffness, .01f);
    private void OnTransitionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_state is { CurrentState: false, TargetState: false, IsIdle: true })
            Dispatcher.UIThread.Post(() => { if (!IsOpen && _state is { IsIdle: true }) ClosePresentation(true, preserveState: true); });
    }
    private void UpdateAppearance()
    {
        if (_layer is null) return;
        _layer.Opacity = Math.Clamp(AppearanceAlpha, 0, 1);
        // Compose graphicsLayer uses an intermediate buffer at alpha < 1. Its
        // bounds implicitly clip overflowing content (including Surface shadows).
        _layer.ClipToBounds = _layer.Opacity < 1;
        _layer.RenderTransform = new ScaleTransform(AppearanceScale, AppearanceScale);
    }

    private void ClosePresentation(bool restoreFocus, bool preserveState = false)
    {
        var topLevel = _topLevel;
        var focused = topLevel?.FocusManager?.GetFocusedElement();
        var ownedFocus = focused is null || focused is Visual v && (_presentation == v || _presentation?.IsVisualAncestorOf(v) == true);
        if (topLevel is not null)
        {
            topLevel.RemoveHandler(PointerPressedEvent, OnOutsidePressed);
            topLevel.RemoveHandler(GotFocusEvent, OnWindowFocus);
            topLevel.BackRequested -= OnBackRequested;
            topLevel.LayoutUpdated -= OnWindowLayout;
            topLevel.PropertyChanged -= OnWindowPropertyChanged;
        }
        if (_state is not null) _state.PropertyChanged -= OnTransitionChanged;
        _transition = null;
        _presentationScope?.Dispose(); _presentationScope = null;
        if (!preserveState)
        {
            _state = null;
            _recomposePending = false;
        }
        _shieldLayer?.Children.Remove(_inputShield); _shieldLayer = null;
        _popup.Close(); _host = null; IsPresented = false;
        _topLevel = null; _lastFocus = null; _lastAnchor = null;
        if (restoreFocus && ownedFocus && _previousFocus is Control { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } previous
            && previous.IsAttachedToVisualTree()) previous.Focus();
        _previousFocus = null;
    }

    private bool IsTopmost()
    {
        if (!IsPresented) return false;
        var host = _host;
        return host is not null && host.GetVisualParent()?.GetVisualChildren().OfType<OverlayPopupHost>()
            .OrderBy(h => h.ZIndex).LastOrDefault() == host;
    }
    private void UpdateInputShield()
    {
        if (!IsPresented || !(ConsumePointerInputOutside ?? IsPopupFocusable))
        { _shieldLayer?.Children.Remove(_inputShield); _shieldLayer = null; return; }
        if (_shieldLayer is null)
        {
            var host = _host!;
            // The fixed OverlayPopupHost lives in PopupOverlayLayer (a Canvas). A transparent
            // sibling below this host blocks hit testing, including hover/wheel/touch, without
            // enabling native light-dismiss (which would close before the application's request).
            _shieldLayer = (Panel)host.GetVisualParent()!;
            _shieldLayer.Children.Insert(_shieldLayer.Children.IndexOf(host), _inputShield);
        }
        _inputShield.Width = _topLevel!.ClientSize.Width;
        _inputShield.Height = _topLevel.ClientSize.Height;
    }
    private void OnWindowFocus(object? sender, FocusChangedEventArgs e)
    {
        if (_redirectingFocus || !IsPopupFocusable || !IsTopmost() || e.Source is not Visual v) return;
        if (v == _presentation || _presentation!.IsVisualAncestorOf(v)) { _lastFocus = v as Control; return; }
        _redirectingFocus = true;
        try
        {
            if (_lastFocus is { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } last && last.IsAttachedToVisualTree()) last.Focus();
            else _presentation!.Focus();
        }
        finally { _redirectingFocus = false; }
    }
    private void OnOutsidePressed(object? sender, PointerPressedEventArgs e)
    {
        // Core's scene layer bounds are the untransformed popup layout, not the scaled paint.
        if (!IsTopmost() || _presentation is null || new Rect(_presentation.Bounds.Size).Contains(e.GetPosition(_presentation))) return;
        // Decide consumption before invoking user code, which may close this popup synchronously.
        if (ConsumePointerInputOutside ?? IsPopupFocusable) e.Handled = true;
        if (DismissOnPointerOutside) DismissRequested?.Invoke(this, new(DismissReason.PointerOutside));
    }
    private void OnMenuKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsPopupFocusable || !IsTopmost()) return;
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (DismissOnBack) DismissRequested?.Invoke(this, new(DismissReason.Escape));
        }
        else if (e.Key == Key.Tab || this is DropdownMenu && e.Key is Key.Up or Key.Down)
        {
            // Avalonia 12.1.2 FocusHelpers.GetFocusParent follows Visual.Parent (logical
            // ownership), while its child search is visual. Items kept by the declaration
            // therefore restart at the first popup item. Traverse this popup's actual content
            // in tab order, preserving arbitrary focusable content and excluding disabled items.
            var candidates = _layer!.GetVisualDescendants().OfType<Control>()
                .Where(c => c.Focusable && c.IsTabStop && c.IsEffectivelyEnabled && c.IsEffectivelyVisible)
                .OrderBy(KeyboardNavigation.GetTabIndex).ToArray();
            var focused = _topLevel?.FocusManager?.GetFocusedElement();
            var index = Array.FindIndex(candidates, c => ReferenceEquals(c, focused));
            var backwards = e.Key == Key.Up || e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if (candidates.Length > 0)
                candidates[backwards ? (index <= 0 ? candidates.Length - 1 : index - 1) : (index + 1) % candidates.Length]
                    .Focus(NavigationMethod.Tab, e.KeyModifiers);
            e.Handled = true;
        }
    }
    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (e.Handled || !IsPopupFocusable || !IsTopmost()) return;
        e.Handled = true;
        if (DismissOnBack) DismissRequested?.Invoke(this, new(DismissReason.Back));
    }
    private void OnWindowLayout(object? sender, EventArgs e) => UpdatePosition();
    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // WindowBase.HandleActivated restores the window focus scope before setting
        // IsActive. An overlay opened during initial layout must take focus afterwards.
        if (e.Property == WindowBase.IsActiveProperty && e.NewValue is true && IsPopupFocusable && IsTopmost())
            (_lastFocus ?? _presentation)?.Focus();
    }
    private Rect AnchorBounds()
    {
        var anchor = _popup.PlacementTarget!;
        var mirrored = false;
        for (Visual? visual = anchor; visual is not null && visual != _topLevel; visual = visual.GetVisualParent())
            mirrored ^= visual.HasMirrorTransform;
        return new(anchor.TranslatePoint(mirrored ? new Point(anchor.Bounds.Width, 0) : default, _topLevel!)!.Value, anchor.Bounds.Size);
    }
    private void UpdatePosition(bool force = false)
    {
        if (!IsPresented || _topLevel is null) return;
        if (_popup.PlacementTarget?.IsAttachedToVisualTree() != true) { ClosePresentation(true); return; }
        var bounds = AnchorBounds();
        if (!force && _lastAnchor == bounds && _lastWindowSize == _topLevel.ClientSize) return;
        _lastAnchor = bounds; _lastWindowSize = _topLevel.ClientSize;
        UpdateInputShield();
        var shortest = Math.Min(_topLevel.ClientSize.Width, _topLevel.ClientSize.Height);
        var platformWidth = shortest >= 600 ? 580 : shortest >= 480 ? 440 : 320;
        _popup.MaxWidth = UsePlatformDefaultWidth ? Math.Min(_topLevel.ClientSize.Width, platformWidth) : _topLevel.ClientSize.Width;
        _popup.MaxHeight = _topLevel.ClientSize.Height;
        _popup.PlacementRect = null; _popup.PlacementRect = bounds;
    }
    private void PlacePopup(CustomPopupPlacement placement)
    {
        var density = _topLevel!.RenderScaling;
        int Pixel(double dip) => MathUtils.DipToPx((float)dip, (float)density);
        var bounds = AnchorBounds();
        var provider = PositionProvider ?? new DropdownMenuPositionProvider(AnchorPosition, Offset, density,
            horizontalMargin: this is DropdownMenu ? 0 : 8);
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var point = provider.CalculatePosition(new(Pixel(bounds.X), Pixel(bounds.Y), Pixel(bounds.Width), Pixel(bounds.Height)),
            new(Pixel(_topLevel.ClientSize.Width), Pixel(_topLevel.ClientSize.Height)), rtl,
            new(Pixel(placement.PopupSize.Width), Pixel(placement.PopupSize.Height)));
        // The provider's pivot is physical; Avalonia reflects the popup's RTL subtree.
        if (_layer is not null) _layer.RenderTransformOrigin = rtl
            ? new RelativePoint(1 - provider.TransformOrigin.Point.X, provider.TransformOrigin.Point.Y, RelativeUnit.Relative)
            : provider.TransformOrigin;
        // Popup.skiko.kt clips after asking the provider (and calculating its pivot).
        if (ClippingEnabled)
            point = new(Math.Clamp(point.X, 0, Math.Max(0, Pixel(_topLevel.ClientSize.Width) - Pixel(placement.PopupSize.Width))),
                Math.Clamp(point.Y, 0, Math.Max(0, Pixel(_topLevel.ClientSize.Height) - Pixel(placement.PopupSize.Height))));
        placement.AnchorRectangle = new Rect(point.X / density, point.Y / density, 0, 0);
        placement.Anchor = rtl ? PopupAnchor.TopRight : PopupAnchor.TopLeft;
        placement.Gravity = rtl ? PopupGravity.BottomLeft : PopupGravity.BottomRight;
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
    }
}

/// <summary>Baseline Material dropdown: surface, intrinsic-width column and vertical scrolling.</summary>
public class DropdownMenu : DropdownMenuPopup
{
    public static readonly StyledProperty<double> TonalElevationProperty = AvaloniaProperty.Register<DropdownMenu, double>(nameof(TonalElevation));
    public static readonly StyledProperty<double> ShadowElevationProperty = AvaloniaProperty.Register<DropdownMenu, double>(nameof(ShadowElevation));
    public static readonly StyledProperty<Vector> ScrollOffsetProperty = AvaloniaProperty.Register<DropdownMenu, Vector>(nameof(ScrollOffset), defaultBindingMode: BindingMode.TwoWay);
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    public Vector ScrollOffset { get => GetValue(ScrollOffsetProperty); set => SetValue(ScrollOffsetProperty, value); }
}

// The peer belongs to the presented popup subtree, not the zero-sized declaration.
internal sealed class DropdownMenuPresenter : Border
{
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(DropdownMenuPresenter owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;
        protected override string GetClassNameCore() => nameof(DropdownMenu);
    }
}

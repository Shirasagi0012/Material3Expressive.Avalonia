// BottomSheet.kt (fixed AndroidX baseline). Copyright 2026 The Android Open Source Project.
// Licensed under Apache-2.0.
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Input;

using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>A bottom sheet in the current visual tree. Place it in a finite-height overlay host.</summary>
public partial class BottomSheet : ContentControl
{
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = MaterialSurface.ShapeProperty.AddOwner<BottomSheet>();
    /// <summary>Explicit outline; null uses the themed CornerRadius.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    public static readonly StyledProperty<SheetState> StateProperty = AvaloniaProperty.Register<BottomSheet, SheetState>(nameof(State));
    public static readonly StyledProperty<double> SheetMaxWidthProperty = AvaloniaProperty.Register<BottomSheet, double>(nameof(SheetMaxWidth));
    public static readonly StyledProperty<bool> GesturesEnabledProperty = AvaloniaProperty.Register<BottomSheet, bool>(nameof(GesturesEnabled), true);
    public static readonly StyledProperty<bool> BackHandlerEnabledProperty = AvaloniaProperty.Register<BottomSheet, bool>(nameof(BackHandlerEnabled), true);
    public static readonly StyledProperty<object?> DragHandleProperty = AvaloniaProperty.Register<BottomSheet, object?>(nameof(DragHandle), string.Empty);
    public static readonly StyledProperty<IDataTemplate?> DragHandleTemplateProperty = AvaloniaProperty.Register<BottomSheet, IDataTemplate?>(nameof(DragHandleTemplate));
    public static readonly StyledProperty<Thickness> WindowInsetsProperty = AvaloniaProperty.Register<BottomSheet, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<double> TonalElevationProperty = AvaloniaProperty.Register<BottomSheet, double>(nameof(TonalElevation));
    public static readonly StyledProperty<double> ShadowElevationProperty = AvaloniaProperty.Register<BottomSheet, double>(nameof(ShadowElevation));
    public static readonly RoutedEvent<RoutedEventArgs> DismissRequestedEvent = RoutedEvent.Register<BottomSheet, RoutedEventArgs>(nameof(DismissRequested), RoutingStrategies.Bubble);
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> ShowAnimationProperty = AvaloniaProperty.Register<BottomSheet, Foundation.Motion.SpringSpec>(nameof(ShowAnimation));
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> HideAnimationProperty = AvaloniaProperty.Register<BottomSheet, Foundation.Motion.SpringSpec>(nameof(HideAnimation));
    internal static readonly StyledProperty<double?> StandardPeekHeightProperty = AvaloniaProperty.Register<BottomSheet, double?>(nameof(StandardPeekHeight));
    internal double? StandardPeekHeight { get => GetValue(StandardPeekHeightProperty); set => SetValue(StandardPeekHeightProperty, value); }
    internal Foundation.Motion.SpringSpec ShowAnimation { get => GetValue(ShowAnimationProperty); set => SetValue(ShowAnimationProperty, value); }
    internal Foundation.Motion.SpringSpec HideAnimation { get => GetValue(HideAnimationProperty); set => SetValue(HideAnimationProperty, value); }
    public SheetState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public double SheetMaxWidth { get => GetValue(SheetMaxWidthProperty); set => SetValue(SheetMaxWidthProperty, value); }
    public bool GesturesEnabled { get => GetValue(GesturesEnabledProperty); set => SetValue(GesturesEnabledProperty, value); }
    public bool BackHandlerEnabled { get => GetValue(BackHandlerEnabledProperty); set => SetValue(BackHandlerEnabledProperty, value); }
    public object? DragHandle { get => GetValue(DragHandleProperty); set => SetValue(DragHandleProperty, value); }
    public IDataTemplate? DragHandleTemplate { get => GetValue(DragHandleTemplateProperty); set => SetValue(DragHandleTemplateProperty, value); }
    /// <summary>Logical start/top/end/bottom content insets, excluding ancestor consumption.</summary>
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    public event EventHandler<RoutedEventArgs>? DismissRequested { add => AddHandler(DismissRequestedEvent, value); remove => RemoveHandler(DismissRequestedEvent, value); }

    private SheetState? _attachedState;
    private BottomSheetLayout? _layout;
    private Button? _handle;
    private TopLevel? _topLevel;
    private AnimationChildScope? _effects;
    private IPointer? _pointer;
    private Point _down, _last;
    private bool _dragging;
    private readonly VelocityTracker _velocity = new();
    private bool CanDrag => GesturesEnabled && IsEffectivelyEnabled && (StandardPeekHeight is not null || State.CurrentValue != SheetValue.Hidden);

    public BottomSheet()
    {
        SetCurrentValue(StateProperty, new SheetState());
        AddHandler(PointerPressedEvent, PointerDown, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, PointerMove, RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, PointerUp, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, PointerLost);
        // Native Thumb reports ownership through DragStarted, not PointerMoved.Handled.
        AddHandler(Thumb.DragStartedEvent, (_, _) => CancelPointer(), RoutingStrategies.Bubble, true);
    }
    protected override bool RegisterContentPresenter(ContentPresenter presenter) => presenter.Name == "PART_DragHandle" || base.RegisterContentPresenter(presenter);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        DisconnectScroll();
        CancelPointer();
        if (_layout is not null) _layout.Owner = null;
        if (_handle is not null) _handle.Click -= HandleClick;
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<BottomSheetLayout>("PART_Layout");
        if (_layout is not null) _layout.Owner = this;
        _handle = e.NameScope.Find<Button>("PART_HandleButton");
        if (_handle is not null) { _handle.Click += HandleClick; AutomationProperties.SetName(_handle, BottomSheetStrings.Get("BottomSheetDragHandleDescription")); }
        if (e.NameScope.Find<TooltipBox>("PART_Tooltip") is { } tooltip)
            tooltip.Tooltip = new PlainTooltip { Content = BottomSheetStrings.Get("BottomSheetDragHandleDescription") };
        AttachState(); UpdateHandle();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); AttachState();
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, EscapeDown, RoutingStrategies.Bubble);
        LayoutUpdated += UpdateScrollConnection;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= UpdateScrollConnection;
        DisconnectScroll();
        _topLevel?.RemoveHandler(KeyDownEvent, EscapeDown); _topLevel = null;
        CancelPointer();
        _effects?.Dispose(); _effects = null;
        if (_attachedState is { } state) { state.PropertyChanged -= StateChanged; state.Detach(this); }
        _attachedState = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DragHandleProperty)
        {
            if (change.OldValue is ILogical oldChild) LogicalChildren.Remove(oldChild);
            // A scaffold owns its public slot; the nested sheet only presents that slot.
            if (change.NewValue is ILogical newChild && (newChild.GetLogicalParent() is not { } parent || parent != TemplatedParent)) LogicalChildren.Add(newChild);
        }
        if (change.Property == DragHandleProperty || change.Property == DragHandleTemplateProperty) UpdateHandle();
        if (change.Property == StateProperty) { DisconnectScroll(); CancelPointer(); AttachState(); }
        if (change.Property == StateProperty || change.Property == GesturesEnabledProperty || change.Property == IsEffectivelyEnabledProperty || change.Property == DragHandleProperty)
            (_handle as BottomSheetHandle)?.RefreshAutomation();
        if (change.Property == GesturesEnabledProperty && _pointer is { } pointer && _effects is { } scope)
            _ = scope.Launch(async ct => { if (!await scope.WaitForNextFrameAsync(ct)) return; if (_pointer == pointer && !GesturesEnabled) CancelPointer(); }, "Sheet gesture configuration");
        if (change.Property == IsEffectivelyEnabledProperty) CancelPointer();
        if (change.Property == AutoConnectScrollProperty && this.IsAttachedToVisualTree()) UpdateScrollConnection(null, EventArgs.Empty);
        if (change.Property == GesturesEnabledProperty || change.Property == IsEffectivelyEnabledProperty)
            _scrollConnection?.UpdateConfiguration();
        if (change.Property == SheetMaxWidthProperty || change.Property == WindowInsetsProperty || change.Property == FlowDirectionProperty || change.Property == StandardPeekHeightProperty
            || change.Property == Foundation.Rendering.MaterialWindowInsets.ConsumedPaddingProperty) _layout?.InvalidateMeasure();
    }
    private void AttachState()
    {
        if (!this.IsAttachedToVisualTree() || _attachedState == State) return;
        if (_attachedState is { } old) { old.PropertyChanged -= StateChanged; old.Detach(this); }
        _effects?.Dispose(); _effects = AnimationScope.Get(this).CreateChild("Bottom sheet effects");
        State.Attach(this); _attachedState = State; State.PropertyChanged += StateChanged;
        _layout?.InvalidateArrange();
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SheetState.CurrentOffsetProperty) _scrollConnection?.StateChanged();
        if (e.Property == SheetState.CurrentValueProperty || e.Property == SheetState.HasExpandedStateProperty || e.Property == SheetState.HasPartiallyExpandedStateProperty)
            (_handle as BottomSheetHandle)?.RefreshAutomation();
        if (e.Property == SheetState.CurrentOffsetProperty || StandardPeekHeight is not null &&
            (e.Property == SheetState.CurrentValueProperty || e.Property == SheetState.IsAnimationRunningProperty))
            _layout?.InvalidateArrange();
    }
    private void UpdateHandle()
    { if (_handle is not null) _handle.IsVisible = DragHandle is not null; }
    private void HandleClick(object? sender, RoutedEventArgs e)
    {
        if (State.CurrentValue == SheetValue.Expanded && StandardPeekHeight is not null)
            _ = State.HiddenEnabled ? State.HideAsync() : State.PartialExpandAsync();
        else if (State.CurrentValue == SheetValue.Expanded) Dismiss();
        else if (State.CurrentValue == SheetValue.PartiallyExpanded) _ = State.ExpandAsync();
        else _ = State.ShowAsync();
    }
    private void EscapeDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !BackHandlerEnabled || !State.IsVisible) return;
        e.Handled = true;
        if (State.CurrentValue == SheetValue.Expanded && State.HasPartiallyExpandedState) _ = State.PartialExpandAsync();
        else Dismiss(confirmBeforeHide: false);
    }
    internal void Dismiss(bool confirmBeforeHide = true)
    {
        if ((confirmBeforeHide && !State.Confirm(SheetValue.Hidden)) || _effects is not { } scope) return;
        var state = State;
        _ = scope.Launch(async _ =>
        {
            try { await state.HideAsync(); }
            finally { if (_attachedState == state && !state.IsVisible) RaiseEvent(new RoutedEventArgs(DismissRequestedEvent)); }
        }, "Bottom sheet dismiss");
    }
    private async Task<float> Fling(float velocity, bool endDrag, CancellationToken cancellationToken = default)
    {
        var state = State;
        try { return await (endDrag ? state.EndDrag(velocity, cancellationToken) : state.FlingAsync(velocity, cancellationToken)); }
        finally { if (StandardPeekHeight is null && _attachedState == state && !state.IsVisible) RaiseEvent(new RoutedEventArgs(DismissRequestedEvent)); }
    }
    /// <summary>Nested scroll connection: consumes upward user scroll before the child, in DIP.</summary>
    public double OnPreScroll(double availableY, bool isUserInput = true) => GesturesEnabled && isUserInput && availableY < 0
        ? State.DispatchRawDelta((float)(availableY * LayoutHelper.GetLayoutScale(this))) / LayoutHelper.GetLayoutScale(this) : 0;
    /// <summary>Nested scroll connection: consumes the child's remaining user scroll, in DIP.</summary>
    public double OnPostScroll(double availableY, bool isUserInput = true) => GesturesEnabled && isUserInput && availableY != 0
        ? State.DispatchRawDelta((float)(availableY * LayoutHelper.GetLayoutScale(this))) / LayoutHelper.GetLayoutScale(this) : 0;
    public async Task<double> OnPreFlingAsync(double availableY, CancellationToken cancellationToken = default)
    {
        if (!GesturesEnabled || availableY >= 0 || State.OffsetPx <= State.MinimumAnchor) return 0;
        await Fling((float)(availableY * LayoutHelper.GetLayoutScale(this)), false, cancellationToken); return availableY;
    }
    public async Task<double> OnPostFlingAsync(double availableY, CancellationToken cancellationToken = default) => !GesturesEnabled ? 0
        : await Fling((float)(availableY * LayoutHelper.GetLayoutScale(this)), false, cancellationToken) / LayoutHelper.GetLayoutScale(this);
    private void PointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (!CanDrag || _pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        // The physical child owns touch slop, capture and velocity. Its nested connection
        // moves the sheet before/after body consumption; do not run a second parent drag.
        if (e.Pointer.Type is PointerType.Touch or PointerType.Pen && _scrollConnection?.Contains(e.Source) == true) return;
        _pointer = e.Pointer; _down = _last = e.GetPosition(_topLevel);
        _velocity.Reset(); _velocity.Add(e.Timestamp, (float)(_down.Y * LayoutHelper.GetLayoutScale(this)));
        if (State.IsAnimationRunning) StartDrag(e);
    }
    private void StartDrag(PointerEventArgs e) { State.BeginDrag(); _dragging = true; e.Pointer.Capture(this); e.Handled = true; }
    private void PointerMove(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || !CanDrag) return;
        var point = e.GetPosition(_topLevel); var density = LayoutHelper.GetLayoutScale(this);
        _velocity.Add(e.Timestamp, (float)(point.Y * density));
        var total = point.Y - _down.Y; var slop = e.Pointer.Type == PointerType.Mouse ? .125 : 18;
        if (!_dragging && Math.Abs(total) <= slop) return;
        if (!_dragging) { StartDrag(e); _last = new Point(_down.X, _down.Y + Math.Sign(total) * slop); }
        State.DragBy((float)((point.Y - _last.Y) * density)); _last = point; e.Handled = true;
    }
    private void PointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var dragged = _dragging; _pointer = null; _dragging = false;
        if (!dragged) return;
        e.Pointer.Capture(null); e.Handled = true; _ = Fling(_velocity.Calculate(e.Timestamp), true);
    }
    private void PointerLost(object? sender, PointerCaptureLostEventArgs e) { if (e.Source == this && _pointer == e.Pointer) CancelPointer(); }
    private void CancelPointer()
    {
        var pointer = _pointer; var dragged = _dragging; _pointer = null; _dragging = false;
        // A pending parent gesture must not cancel a child's independent press.
        if (pointer?.Captured == this) pointer.Capture(null);
        if (dragged && _attachedState is { } state)
        { if (this.IsAttachedToVisualTree()) _ = Fling(0, true); else state.CancelDrag(); }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SheetPeer(this);
    private sealed class SheetPeer(BottomSheet owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string? GetNameCore() => owner.StandardPeekHeight is null && string.IsNullOrEmpty(AutomationProperties.GetName(owner))
            ? BottomSheetStrings.Get("BottomSheetPaneTitle") : base.GetNameCore();
    }
}

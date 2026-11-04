// ModalBottomSheet.kt, fixed AndroidX baseline; Core ModalBottomSheet.skiko.kt.
// Copyright 2024-2026 The Android Open Source Project. Licensed under Apache-2.0.
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Overlays;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>A modal bottom sheet in the window's overlay layer. The declaration takes no layout space.</summary>
/// <remarks>DismissRequested asks the application to set IsOpen to false after the hide animation.</remarks>
public class ModalBottomSheet : ContentControl
{
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<ModalBottomSheet, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<ModalBottomSheet, bool> IsPresentedProperty = AvaloniaProperty.RegisterDirect<ModalBottomSheet, bool>(nameof(IsPresented), s => s.IsPresented);
    public static readonly StyledProperty<SheetState> StateProperty = BottomSheet.StateProperty.AddOwner<ModalBottomSheet>();
    public static readonly StyledProperty<double> SheetMaxWidthProperty = BottomSheet.SheetMaxWidthProperty.AddOwner<ModalBottomSheet>();
    public static readonly StyledProperty<bool> GesturesEnabledProperty = BottomSheet.GesturesEnabledProperty.AddOwner<ModalBottomSheet>();
    public static readonly StyledProperty<object?> DragHandleProperty = BottomSheet.DragHandleProperty.AddOwner<ModalBottomSheet>();
    public static readonly StyledProperty<IDataTemplate?> DragHandleTemplateProperty = BottomSheet.DragHandleTemplateProperty.AddOwner<ModalBottomSheet>();
    public static readonly StyledProperty<Thickness> WindowInsetsProperty = BottomSheet.WindowInsetsProperty.AddOwner<ModalBottomSheet>();
    public static readonly StyledProperty<double> TonalElevationProperty = BottomSheet.TonalElevationProperty.AddOwner<ModalBottomSheet>();
    public static readonly StyledProperty<Color?> ScrimColorProperty = AvaloniaProperty.Register<ModalBottomSheet, Color?>(nameof(ScrimColor));
    public static readonly StyledProperty<bool> ShouldDismissOnBackPressProperty = AvaloniaProperty.Register<ModalBottomSheet, bool>(nameof(ShouldDismissOnBackPress), true);
    public static readonly StyledProperty<bool> ShouldDismissOnClickOutsideProperty = AvaloniaProperty.Register<ModalBottomSheet, bool>(nameof(ShouldDismissOnClickOutside), true);
    public static readonly RoutedEvent<RoutedEventArgs> DismissRequestedEvent = RoutedEvent.Register<ModalBottomSheet, RoutedEventArgs>(nameof(DismissRequested), RoutingStrategies.Bubble);
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> ScrimAnimationProperty = AvaloniaProperty.Register<ModalBottomSheet, Foundation.Motion.SpringSpec>(nameof(ScrimAnimation));
    internal Foundation.Motion.SpringSpec ScrimAnimation { get => GetValue(ScrimAnimationProperty); set => SetValue(ScrimAnimationProperty, value); }
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public bool IsPresented { get => _isPresented; private set => SetAndRaise(IsPresentedProperty, ref _isPresented, value); }
    public SheetState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public double SheetMaxWidth { get => GetValue(SheetMaxWidthProperty); set => SetValue(SheetMaxWidthProperty, value); }
    public bool GesturesEnabled { get => GetValue(GesturesEnabledProperty); set => SetValue(GesturesEnabledProperty, value); }
    public object? DragHandle { get => GetValue(DragHandleProperty); set => SetValue(DragHandleProperty, value); }
    public IDataTemplate? DragHandleTemplate { get => GetValue(DragHandleTemplateProperty); set => SetValue(DragHandleTemplateProperty, value); }
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    /// <summary>Null omits both the scrim drawing and its click action; transparent retains the action.</summary>
    public Color? ScrimColor { get => GetValue(ScrimColorProperty); set => SetValue(ScrimColorProperty, value); }
    public bool ShouldDismissOnBackPress { get => GetValue(ShouldDismissOnBackPressProperty); set => SetValue(ShouldDismissOnBackPressProperty, value); }
    public bool ShouldDismissOnClickOutside { get => GetValue(ShouldDismissOnClickOutsideProperty); set => SetValue(ShouldDismissOnClickOutsideProperty, value); }
    public event EventHandler<RoutedEventArgs>? DismissRequested { add => AddHandler(DismissRequestedEvent, value); remove => RemoveHandler(DismissRequestedEvent, value); }

    private readonly Popup _popup = new() { ShouldUseOverlayLayer = true, WindowManagerAddShadowHint = false };
    private Grid? _root;
    private ModalBottomSheetPresenter? _presenter;
    private BottomSheet? _sheet;
    private Scrim? _scrim;
    private SheetState? _observedState;
    private AnimationChildScope? _effects;
    private Animatable<float>? _alpha;
    private IDisposable? _scrimAlphaBinding;
    private bool _isPresented, _showPending, _hadExpandedAnchor, _scrimUpdatePending;
    internal BottomSheet? PresentedSheet => _sheet;
    internal float ScrimAlpha => _alpha?.Value ?? 0;

    public ModalBottomSheet()
    {
        SetCurrentValue(StateProperty, new SheetState());
        LogicalChildren.Add(_popup);
        AnimationScope.AddScopeChangedHandler(this, (_, e) =>
        {
            if (e.NewScope is not null && IsPresented)
            {
                Close();
                Dispatcher.UIThread.Post(UpdatePresentation);
            }
        });
    }
    protected override Size MeasureCore(Size availableSize) { ApplyTemplate(); return default; }
    protected override AutomationPeer OnCreateAutomationPeer() => new DeclarationPeer(this);
    private sealed class DeclarationPeer(Control owner) : ControlAutomationPeer(owner)
    {
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => [];
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Close();
        if (_presenter is not null) { _presenter.DismissRequested -= BackRequested; _presenter.Closed -= Closed; }
        if (_sheet is not null) _sheet.DismissRequested -= SheetDismissed;
        if (_scrim is not null) _scrim.Click -= ScrimClicked;
        _root?.Children.Remove(_popup);
        base.OnApplyTemplate(e);
        _root = e.NameScope.Find<Grid>("PART_Root");
        _presenter = e.NameScope.Find<ModalBottomSheetPresenter>("PART_Presentation");
        _sheet = e.NameScope.Find<BottomSheet>("PART_Sheet");
        _scrim = e.NameScope.Find<Scrim>("PART_Scrim");
        if (_presenter is not null)
        {
            _root?.Children.Remove(_presenter);
            _presenter.DismissRequested += BackRequested; _presenter.Closed += Closed;
        }
        if (_sheet is not null) _sheet.DismissRequested += SheetDismissed;
        if (_scrim is not null) { _scrim.Click += ScrimClicked; AutomationProperties.SetName(_scrim, BottomSheetStrings.Get("CloseSheet")); }
        _root?.Children.Add(_popup);
        // Finish template attachment/detachment before moving the presentation into a popup.
        Dispatcher.UIThread.Post(UpdatePresentation);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); Dispatcher.UIThread.Post(UpdatePresentation); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { Close(); base.OnDetachedFromVisualTree(e); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DragHandleProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical next) LogicalChildren.Add(next);
        }
        if (change.Property == StateProperty) { Close(); Dispatcher.UIThread.Post(UpdatePresentation); }
        if (change.Property == IsOpenProperty) UpdatePresentation();
        if (change.Property == ScrimColorProperty || change.Property == ShouldDismissOnClickOutsideProperty) DrawScrim();
        if (change.Property == ScrimAnimationProperty) UpdateScrimTarget();
        if (change.Property == MaterialWindowInsets.ConsumedPaddingProperty) UpdateInsets();
    }
    private void UpdatePresentation()
    {
        if (!IsOpen) { Close(); return; }
        if (!this.IsAttachedToVisualTree() || _presenter is null || _sheet is null || IsPresented) return;
        _observedState = State; _observedState.PropertyChanged += StateChanged;
        _effects = AnimationScope.Get(this).CreateChild("Modal bottom sheet");
        _alpha = new Animatable<float>(new FloatConverter(), State.TargetValue == SheetValue.Hidden ? 0f : 1f, VisibilityThresholds.Float, "Bottom sheet scrim");
        if (_scrim is not null) _scrimAlphaBinding = _effects.BindAnimatedValue(_scrim, Scrim.AlphaProperty, _alpha);
        _hadExpandedAnchor = false;
        IsPresented = true;
        DrawScrim(); UpdateInsets();
        _presenter.Open(_popup, this, focusContent: false);
        QueueShow();
    }
    private void Close()
    {
        if (_observedState is { } old) old.PropertyChanged -= StateChanged;
        _observedState = null; _showPending = false; _hadExpandedAnchor = false; _scrimUpdatePending = false;
        _effects?.Dispose(); _effects = null;
        _alpha = null;
        _scrimAlphaBinding?.Dispose(); _scrimAlphaBinding = null;
        _presenter?.Close(true);
        IsPresented = false;
    }
    private void Closed(object? sender, EventArgs e) => Close();
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SheetState.HasExpandedStateProperty) QueueShow();
        if (e.Property == SheetState.TargetValueProperty) UpdateScrimTarget();
        if (e.Property == SheetState.CurrentOffsetProperty) UpdateInsets();
    }
    private void QueueShow()
    {
        if (!IsPresented || _observedState is not { } state) return;
        if (!state.HasExpandedState) { _hadExpandedAnchor = false; return; }
        if (_hadExpandedAnchor) return;
        _hadExpandedAnchor = true; _showPending = true;
        var scope = _effects;
        // LaunchedEffect runs after layout establishes anchors. A restored visible state
        // is retained; a later programmatic hide must not launch another show operation.
        Dispatcher.UIThread.Post(() =>
        {
            if (!_showPending || scope != _effects || _observedState != state) return;
            _showPending = false;
            if (state.HasExpandedState && state.TargetValue == SheetValue.Hidden && scope is not null)
                _ = scope.Launch(async ct =>
                {
                    // hasExpandedState is read by composition after the anchor layout writes it.
                    await scope.NextFrameAsync(ct);
                    if (state.HasExpandedState && state.TargetValue == SheetValue.Hidden) await state.ShowAsync(ct);
                }, "Modal bottom sheet initial show");
        });
    }
    private void UpdateScrimTarget()
    {
        if (_alpha is null || _observedState is null || _effects is not { } scope || _scrimUpdatePending) return;
        _scrimUpdatePending = true;
        _ = scope.Launch(async ct =>
        {
            // derivedStateOf(targetValue) is consumed at recomposition, after sampling the
            // previous animation. The fixed AnimateAsState.kt cancels its job BEFORE animateTo:
            // cancellation clears velocity and LastFrameTime, unlike Animatable interruption.
            await scope.NextFrameAsync(ct);
            _scrimUpdatePending = false;
            if (_alpha is { } alpha && _observedState is { } state && ScrimAnimation is { Stiffness: > 0 } motion)
            {
                var target = state.TargetValue == SheetValue.Hidden ? 0f : 1f;
                if (target == alpha.TargetValue) return;
                await alpha.StopAsync();
                ct.ThrowIfCancellationRequested();
                await alpha.AnimateToAsync(scope, target,
                    new ComposeAnimation.Runtime.SpringSpec<float>(motion.DampingRatio, motion.Stiffness, 0.01f));
            }
        }, "Modal bottom sheet scrim target");
    }
    private void DrawScrim()
    {
        if (_scrim is null) return;
        _scrim.Color = ScrimColor;
        _scrim.IsDismissEnabled = ShouldDismissOnClickOutside;
    }
    private void UpdateInsets()
    {
        if (_sheet is null) return;
        var inherited = GetValue(MaterialWindowInsets.ConsumedPaddingProperty);
        var scale = LayoutHelper.GetLayoutScale(this);
        var offset = State.CurrentOffset;
        // consumeWindowInsets(WindowInsets) takes the per-side maximum (not padding addition).
        var top = double.IsNaN(offset) ? 0 : Math.Max(0, (int)(offset * scale)) / scale;
        _sheet.SetValue(MaterialWindowInsets.ConsumedPaddingProperty, new Thickness(inherited.Left, Math.Max(inherited.Top, top), inherited.Right, inherited.Bottom));
    }
    private void ScrimClicked(object? sender, RoutedEventArgs e)
    {
        if (ShouldDismissOnClickOutside && ScrimColor is not null && ReferenceEquals(e.Source, _scrim)) _sheet?.Dismiss();
    }
    private void SheetDismissed(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(DismissRequestedEvent));
    }
    private void BackRequested(object? sender, DismissRequestedEventArgs e)
    {
        if (!ShouldDismissOnBackPress || _effects is not { } scope || _observedState is not { } state) return;
        if (state.CurrentValue == SheetValue.Expanded && state.HasPartiallyExpandedState) _ = state.PartialExpandAsync();
        else if (state.IsVisible) _sheet?.Dismiss(confirmBeforeHide: false);
        else _ = scope.Launch(async _ =>
        {
            // Core's dialog-level settleToDismiss notifies even if hide is vetoed.
            try { await state.HideAsync(); }
            finally { if (_observedState == state) RaiseEvent(new RoutedEventArgs(DismissRequestedEvent)); }
        }, "Modal bottom sheet back");
    }
    public double OnPreScroll(double availableY, bool isUserInput = true) => IsPresented ? _sheet?.OnPreScroll(availableY, isUserInput) ?? 0 : 0;
    public double OnPostScroll(double availableY, bool isUserInput = true) => IsPresented ? _sheet?.OnPostScroll(availableY, isUserInput) ?? 0 : 0;
    public Task<double> OnPreFlingAsync(double availableY, CancellationToken cancellationToken = default) => IsPresented && _sheet is { } sheet ? sheet.OnPreFlingAsync(availableY, cancellationToken) : Task.FromResult(0d);
    public Task<double> OnPostFlingAsync(double availableY, CancellationToken cancellationToken = default) => IsPresented && _sheet is { } sheet ? sheet.OnPostFlingAsync(availableY, cancellationToken) : Task.FromResult(0d);
}

internal sealed class ModalBottomSheetPresenter : ModalPresenter
{
    public ModalBottomSheetPresenter() => EscapeOnKeyDown = true;
}

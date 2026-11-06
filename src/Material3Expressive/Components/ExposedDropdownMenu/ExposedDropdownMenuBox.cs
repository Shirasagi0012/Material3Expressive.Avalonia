/* ExposedDropdownMenu.kt / ExposedDropdownMenu.skiko.kt at docs/upstream-baseline.json.
 * Copyright 2023-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

public enum ExposedDropdownMenuAnchorType { PrimaryNotEditable, PrimaryEditable, SecondaryEditable }

/// <summary>Coordinates a field, its optional secondary anchor, and a controlled exposed menu.</summary>
public class ExposedDropdownMenuBox : ContentControl
{
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<ExposedDropdownMenuBox, bool>(nameof(IsExpanded));
    public static readonly StyledProperty<ExposedDropdownMenu?> MenuProperty = AvaloniaProperty.Register<ExposedDropdownMenuBox, ExposedDropdownMenu?>(nameof(Menu));
    public static readonly AttachedProperty<ExposedDropdownMenuAnchorType?> AnchorTypeProperty =
        AvaloniaProperty.RegisterAttached<ExposedDropdownMenuBox, Control, ExposedDropdownMenuAnchorType?>("AnchorType");
    public static readonly AttachedProperty<bool> IsAnchorEnabledProperty =
        AvaloniaProperty.RegisterAttached<ExposedDropdownMenuBox, Control, bool>("IsAnchorEnabled", true);
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public ExposedDropdownMenu? Menu { get => GetValue(MenuProperty); set => SetValue(MenuProperty, value); }
    public static ExposedDropdownMenuAnchorType? GetAnchorType(Control control) => control.GetValue(AnchorTypeProperty);
    public static void SetAnchorType(Control control, ExposedDropdownMenuAnchorType? value) => control.SetValue(AnchorTypeProperty, value);
    public static bool GetIsAnchorEnabled(Control control) => control.GetValue(IsAnchorEnabledProperty);
    public static void SetIsAnchorEnabled(Control control, bool value) => control.SetValue(IsAnchorEnabledProperty, value);
    public event EventHandler<ExpandedChangeRequestedEventArgs>? ExpandedChangeRequested;

    private readonly HashSet<Control> _anchors = [];
    private readonly HashSet<TextBox> _retainedFields = [];
    private ExposedDropdownMenuAnchorType _anchorType;
    private Control? _pressedAnchor;
    private IPointer? _pointer;
    private Control? _secondaryCapture;
    private bool _alwaysFocusable;
    private TopLevel? _topLevel;
    private bool _needsInitialFocus;
    internal ExposedDropdownMenuAnchorType ActiveAnchorType => _anchorType;

    public ExposedDropdownMenuBox()
    {
        AddHandler(PointerPressedEvent, AnchorPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerMovedEvent, AnchorMoved, RoutingStrategies.Tunnel, true);
        AddHandler(PointerMovedEvent, (_, e) =>
        {
            // Like awaitEachGesture's final pass: a child text-selection/scroll gesture
            // consuming movement cancels the pending expansion gesture.
            if (e.Handled && e.Pointer == _pointer) _pressedAnchor = null;
        }, RoutingStrategies.Bubble, true);
        AddHandler(PointerReleasedEvent, AnchorReleased, RoutingStrategies.Tunnel, true);
        AddHandler(PointerCaptureLostEvent, (_, _) => { _pressedAnchor = null; _pointer = null; _secondaryCapture = null; }, RoutingStrategies.Bubble, true);
        AddHandler(KeyDownEvent, AnchorKey, RoutingStrategies.Tunnel, true);
        AddHandler(KeyUpEvent, AnchorKey, RoutingStrategies.Tunnel, true);
        LayoutUpdated += (_, _) => RegisterAnchors();
    }

    protected override bool RegisterContentPresenter(ContentPresenter presenter) => presenter.Name == "PART_MenuPresenter" || base.RegisterContentPresenter(presenter);
    protected override AutomationPeer OnCreateAutomationPeer() => new BoxPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MenuProperty)
        {
            if (change.OldValue is ExposedDropdownMenu old)
            {
                old.DismissRequested -= Dismiss; old.PropertyChanged -= MenuChanged; old.SetCurrentValue(DropdownMenuPopup.IsOpenProperty, false);
                old.SetCurrentValue(DropdownMenuPopup.PlacementTargetProperty, null); LogicalChildren.Remove(old);
            }
            if (Menu is { } menu)
            {
                LogicalChildren.Add(menu); menu.DismissRequested += Dismiss; menu.PropertyChanged += MenuChanged;
                menu.SetCurrentValue(DropdownMenuPopup.PlacementTargetProperty, this);
                UpdateMenu();
            }
            UpdateRetainedFocus();
        }
        if (change.Property == IsExpandedProperty)
        {
            if (IsExpanded) FocusPrimaryAnchor();
            UpdateMenu();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _needsInitialFocus = true;
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, WindowKey, RoutingStrategies.Bubble, true);
        if (_topLevel is not null) _topLevel.BackRequested += WindowBack;
        RegisterAnchors(); UpdateMenu();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(KeyDownEvent, WindowKey);
        if (_topLevel is not null) _topLevel.BackRequested -= WindowBack;
        _topLevel = null;
        ReleaseSecondaryCapture();
        foreach (var field in _retainedFields) field.ClearValue(TextFieldThemeDefaults.RetainPopupFocusProperty);
        _retainedFields.Clear();
        _anchors.Clear(); _pressedAnchor = null; _pointer = null; _alwaysFocusable = false;
        _anchorType = ExposedDropdownMenuAnchorType.PrimaryNotEditable;
        base.OnDetachedFromVisualTree(e);
    }
    private void RegisterAnchors()
    {
        if (!this.IsAttachedToVisualTree()) return;
        var current = this.GetVisualDescendants().OfType<Control>().Where(c => GetAnchorType(c) is not null
            && c.FindAncestorOfType<ExposedDropdownMenuBox>() == this).ToHashSet();
        _anchors.IntersectWith(current);
        foreach (var anchor in current)
            if (_anchors.Add(anchor) && GetAnchorType(anchor) is { } type
                && (type != ExposedDropdownMenuAnchorType.SecondaryEditable || _anchorType == type)) _anchorType = type;
        UpdateMenu();
        if (_needsInitialFocus && _anchors.Count > 0)
        {
            _needsInitialFocus = false;
            if (IsExpanded) FocusPrimaryAnchor();
        }
    }
    private Control? FindAnchor(object? source)
    {
        for (var node = source as Visual; node is not null && node != this; node = node.GetVisualParent())
            if (node is Control control && GetAnchorType(control) is not null)
                return control.FindAncestorOfType<ExposedDropdownMenuBox>() == this
                    && GetIsAnchorEnabled(control) && control.IsEffectivelyEnabled ? control : null;
        return null;
    }
    private void FocusPrimaryAnchor()
    {
        RegisterAnchors();
        var anchor = _anchors.FirstOrDefault(c => GetAnchorType(c) != ExposedDropdownMenuAnchorType.SecondaryEditable) ?? _anchors.FirstOrDefault();
        anchor?.Focus();
    }
    private void UpdateMenu()
    {
        if (Menu is not { } menu) return;
        // Core's rememberAccessibilityServiceState currently returns false on Skiko.
        UpdateRetainedFocus();
        menu.SetCurrentValue(DropdownMenuPopup.IsPopupFocusableProperty, _alwaysFocusable || _anchorType == ExposedDropdownMenuAnchorType.PrimaryNotEditable);
        menu.SetCurrentValue(DropdownMenuPopup.IsOpenProperty, IsExpanded);
    }
    private void MenuChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != DropdownMenuPopup.IsPresentedProperty) return;
        // DropdownMenu restores native focus immediately after publishing disposal. Retain
        // the same FocusInteraction through that handoff, without an Unfocus/Focus pair.
        if (e.NewValue is false) Dispatcher.UIThread.Post(UpdateRetainedFocus);
        else UpdateRetainedFocus();
    }
    private void UpdateRetainedFocus()
    {
        var fields = Menu is { } menu && (_alwaysFocusable || _anchorType == ExposedDropdownMenuAnchorType.PrimaryNotEditable) && (IsExpanded || menu.IsPresented)
            ? _anchors.OfType<TextBox>().Where(field => GetAnchorType(field) != ExposedDropdownMenuAnchorType.SecondaryEditable).ToHashSet()
            : [];
        foreach (var field in _retainedFields.Except(fields)) field.ClearValue(TextFieldThemeDefaults.RetainPopupFocusProperty);
        foreach (var field in fields) field.SetValue(TextFieldThemeDefaults.RetainPopupFocusProperty, true);
        _retainedFields.Clear(); _retainedFields.UnionWith(fields);
    }
    private void Request(bool expanded, Control? anchor = null)
    {
        if (expanded && !IsEffectivelyEnabled) return;
        if (anchor is not null) _anchorType = GetAnchorType(anchor)!.Value;
        UpdateMenu();
        ExpandedChangeRequested?.Invoke(this, new(expanded));
    }
    private void Dismiss(object? sender, DismissRequestedEventArgs e) => Request(false);
    private void WindowKey(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && IsExpanded && Menu is { IsPopupFocusable: false } && e.Key == Key.Escape)
        { Request(false); e.Handled = true; }
    }
    private void WindowBack(object? sender, RoutedEventArgs e)
    {
        if (!e.Handled && IsExpanded && Menu is { IsPopupFocusable: false }) { Request(false); e.Handled = true; }
    }
    private void AnchorPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || FindAnchor(e.Source) is not { } anchor) return;
        _pressedAnchor = anchor; _pointer = e.Pointer;
        if (GetAnchorType(anchor) == ExposedDropdownMenuAnchorType.SecondaryEditable)
        {
            // Consuming the down prevents the editor from capturing. Keep the gesture's
            // hit path ourselves so a release outside cannot leave a pending activation.
            _secondaryCapture = anchor; e.Pointer.Capture(anchor); e.Handled = true;
        }
    }
    private void AnchorMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedAnchor is { } anchor && e.Pointer == _pointer && !new Rect(anchor.Bounds.Size).Contains(e.GetPosition(anchor)))
            _pressedAnchor = null;
    }
    private void AnchorReleased(object? sender, PointerReleasedEventArgs e)
    {
        var anchor = _pressedAnchor; _pressedAnchor = null;
        var activate = anchor is not null && e.Pointer == _pointer && e.InitialPressMouseButton == MouseButton.Left
            && GetIsAnchorEnabled(anchor) && new Rect(anchor.Bounds.Size).Contains(e.GetPosition(anchor));
        ReleaseSecondaryCapture();
        if (activate) Request(!IsExpanded, anchor);
        _pointer = null;
    }
    private void ReleaseSecondaryCapture()
    {
        if (_secondaryCapture is not null && _pointer?.Captured == _secondaryCapture) _pointer.Capture(null);
        _secondaryCapture = null;
    }
    private void AnchorKey(object? sender, KeyEventArgs e)
    {
        if (FindAnchor(e.Source) is not { } anchor) return;
        var type = GetAnchorType(anchor)!.Value;
        if (e.RoutedEvent == KeyUpEvent && e.Key is Key.Enter or Key.Space)
        {
            if (type != ExposedDropdownMenuAnchorType.PrimaryEditable) Request(!IsExpanded, anchor);
            else if (e.Key == Key.Enter) { Request(!IsExpanded, anchor); e.Handled = true; return; }
        }
        if (type == ExposedDropdownMenuAnchorType.PrimaryEditable && IsExpanded && e.Key is Key.Tab or Key.Up or Key.Down)
        { _alwaysFocusable = true; UpdateMenu(); e.Handled = true; return; }
        _alwaysFocusable = false; UpdateMenu();
    }

    private sealed class BoxPeer : ControlAutomationPeer, IExpandCollapseProvider
    {
        private readonly ExposedDropdownMenuBox _box;
        public BoxPeer(ExposedDropdownMenuBox owner) : base(owner)
        {
            _box = owner;
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsExpandedProperty)
                    RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                        e.OldValue is true ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed, ExpandCollapseState);
            };
        }
        public ExpandCollapseState ExpandCollapseState => _box.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public bool ShowsMenu => true;
        public void Expand() { if (_box._anchors.Any(GetIsAnchorEnabled)) { _box.FocusPrimaryAnchor(); _box.Request(true); } }
        public void Collapse() { if (_box._anchors.Any(GetIsAnchorEnabled)) _box.Request(false); }
        protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider)
            ? _box._anchors.Any(GetIsAnchorEnabled) ? this : null : base.GetProviderCore(providerType);
        protected override bool IsControlElementCore() => _box._anchors.Any(GetIsAnchorEnabled);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;
    }
}

public sealed class ExpandedChangeRequestedEventArgs(bool expanded) : EventArgs
{
    public bool Expanded { get; } = expanded;
}

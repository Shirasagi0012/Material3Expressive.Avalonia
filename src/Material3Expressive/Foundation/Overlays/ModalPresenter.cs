using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Material3Expressive.Foundation.Overlays;

/// <summary>Per-popup modal input and focus scope. Contains no component state or animation.</summary>
internal class ModalPresenter : Panel
{
    private Popup? _popup;
    private TopLevel? _topLevel;
    private IInputElement? _previousFocus;
    private Control? _lastFocus;
    private bool _redirecting;
    private bool _escapeDown;
    internal bool EscapeOnKeyDown { get; init; }
    private bool _restoreFocus = true;

    internal event EventHandler<DismissRequestedEventArgs>? DismissRequested;
    internal event EventHandler? Closed;
    internal bool IsPresented => _popup?.IsOpen == true;

    public ModalPresenter()
    {
        Focusable = true;
        FocusAdorner = null;
        Background = Brushes.Transparent;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        AddHandler(KeyDownEvent, OnModalKeyDown, RoutingStrategies.Bubble, true);
        AddHandler(KeyUpEvent, OnModalKeyUp, RoutingStrategies.Bubble, true);
    }

    internal void Open(Popup popup, Control owner, bool focusContent)
    {
        if (_popup is not null) return;
        _topLevel = TopLevel.GetTopLevel(owner) ?? throw new InvalidOperationException("A modal must have an attached owner.");
        _previousFocus = _topLevel.FocusManager?.GetFocusedElement();
        _restoreFocus = true;
        _popup = popup;
        popup.ShouldUseOverlayLayer = true;
        popup.IsLightDismissEnabled = false;
        popup.PlacementTarget = owner;
        popup.Placement = PlacementMode.Custom;
        popup.CustomPopupPlacementCallback = PlaceAtClientOrigin;
        popup.Child = this;
        popup.Closed += OnPopupClosed;
        _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
        _topLevel.AddHandler(GotFocusEvent, OnTopLevelGotFocus, RoutingStrategies.Bubble, true);
        _topLevel.BackRequested += OnBackRequested;
        Resize();
        popup.Open();
        UpdateLayout();
        FocusInitialContent();
        // Initial IsOpen can mount during Window.Show's layout, before the window accepts focus.
        Dispatcher.UIThread.Post(() =>
        {
            if (IsActiveModal() && (_topLevel?.FocusManager?.GetFocusedElement() is not Visual focused || !ContainsFocus(focused)))
                FocusInitialContent();
        });

        void FocusInitialContent()
        {
            if (focusContent && FocusManager.FindFirstFocusableElement(this) is { } first && first != this)
                first.Focus();
            else Focus();
        }
    }

    private void PlaceAtClientOrigin(CustomPopupPlacement placement)
    {
        // Custom placement already uses TopLevel coordinates, not target-local coordinates.
        // Keep the physical client origin while inheriting RTL for content layout.
        placement.AnchorRectangle = new Rect(0, 0, 1, 1);
        // Avalonia mirrors anchor/gravity after invoking this callback.
        var rtl = _popup!.FlowDirection == FlowDirection.RightToLeft;
        placement.Anchor = rtl ? PopupAnchor.TopRight : PopupAnchor.TopLeft;
        placement.Gravity = rtl ? PopupGravity.BottomLeft : PopupGravity.BottomRight;
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
    }

    private void Resize()
    {
        if (_popup is null || _topLevel is null) return;
        _popup.Width = _topLevel.ClientSize.Width;
        _popup.Height = _topLevel.ClientSize.Height;
    }

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty) Resize();
        // IsActive is published after WindowBase restores the Window focus scope.
        if (e.Property == WindowBase.IsActiveProperty && e.NewValue is true) RestoreActiveModalFocus();
    }

    internal void Close(bool restoreFocus)
    {
        _restoreFocus = restoreFocus;
        _popup?.Close();
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        var popup = _popup!;
        var topLevel = _topLevel!;
        popup.Closed -= OnPopupClosed;
        topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
        topLevel.RemoveHandler(GotFocusEvent, OnTopLevelGotFocus);
        topLevel.BackRequested -= OnBackRequested;
        _popup = null;
        _topLevel = null;
        popup.Child = null;
        popup.CustomPopupPlacementCallback = null;
        popup.PlacementTarget = null;
        var previous = _previousFocus;
        _previousFocus = null;
        _lastFocus = null;
        _escapeDown = false;
        // A later modal may own focus. Never steal it when an older sibling is closed.
        if (_restoreFocus && (topLevel.FocusManager?.GetFocusedElement() is not Visual focused
                || !focused.IsAttachedToVisualTree() || focused == this || this.IsVisualAncestorOf(focused))
            && previous is Control { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } control
            && control.IsAttachedToVisualTree())
            control.Focus();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    // The actual host order is the source of truth; no global registry or competing focus stacks.
    private bool IsActiveModal()
    {
        if (!IsPresented) return false;
        var host = this.GetVisualAncestors().OfType<OverlayPopupHost>().FirstOrDefault();
        var layer = host?.GetVisualParent();
        return layer?.GetVisualChildren().OrderBy(v => v.ZIndex)
            .OfType<OverlayPopupHost>()
            .SelectMany(h => h.GetVisualDescendants().OfType<ModalPresenter>())
            .LastOrDefault(m => m.IsPresented) == this;
    }

    private bool ContainsFocus(Visual visual)
    {
        // Child popups are visual siblings, but their placement target belongs to this modal.
        var visited = new HashSet<Visual>();
        while (visited.Add(visual))
        {
            if (visual == this || this.IsVisualAncestorOf(visual)) return true;
            var host = visual.GetSelfAndVisualAncestors().OfType<OverlayPopupHost>().FirstOrDefault();
            if (host?.Parent is not Popup { PlacementTarget: { } target }) return false;
            visual = target;
        }
        return false;
    }

    private void OnTopLevelGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (_redirecting || !IsActiveModal() || e.Source is not Visual visual) return;
        if (ContainsFocus(visual))
        {
            _lastFocus = visual as Control;
            return;
        }
        _redirecting = true;
        try
        {
            if (_lastFocus is { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } last
                && last.IsAttachedToVisualTree() && ContainsFocus(last)) last.Focus();
            else Focus();
        }
        finally { _redirecting = false; }
    }

    private void RestoreActiveModalFocus()
    {
        // Window activation restores the Window's focus scope, which can clear an overlay's
        // focus. Re-enter only the top modal, retaining its last valid child when possible.
        if (!IsActiveModal() || _topLevel?.FocusManager?.GetFocusedElement() is Visual focused && ContainsFocus(focused)) return;
        if (_lastFocus is { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } last && last.IsAttachedToVisualTree()) last.Focus();
        else Focus();
    }

    private void OnModalKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && e.Key == Key.Tab && IsActiveModal() && IsDirectInput(e.Source))
        {
            _topLevel?.FocusManager?.TryMoveFocus(e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? NavigationDirection.Previous : NavigationDirection.Next);
            e.Handled = true;
        }
        // A handled IME key-down must not become an Escape dismissal on key-up.
        if (e.Key == Key.Escape)
        {
            _escapeDown = !e.Handled && IsActiveModal() && IsDirectInput(e.Source);
            if (EscapeOnKeyDown && _escapeDown)
            {
                _escapeDown = false;
                e.Handled = true;
                DismissRequested?.Invoke(this, new(DismissReason.Escape));
            }
        }
    }

    private void OnModalKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        var eligible = _escapeDown;
        _escapeDown = false;
        if (!e.Handled && eligible && IsActiveModal() && IsDirectInput(e.Source))
        {
            e.Handled = true;
            DismissRequested?.Invoke(this, new(DismissReason.Escape));
        }
    }

    private bool IsDirectInput(object? source) => source is Visual v && (v == this || this.IsVisualAncestorOf(v));

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (e.Handled || !IsActiveModal()) return;
        e.Handled = true;
        DismissRequested?.Invoke(this, new(DismissReason.Back));
    }
}

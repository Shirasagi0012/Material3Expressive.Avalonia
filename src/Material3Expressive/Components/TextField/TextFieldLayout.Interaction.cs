// FocusInteraction.collectIsFocusedAsState / FocusableNode / TextFieldPressGestureFilter.kt.
// Fixed sources: docs/upstream-baseline.json. Native TextBox retains editing and capture.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

internal sealed partial class TextFieldLayout
{
    private readonly MaterialInteractionSource _privateSource = new();
    private readonly List<MaterialInteractionEvent.Focus> _focusInteractions = [];
    private MaterialInteractionSource? _interactionSource;
    private Action<MaterialInteractionEvent>? _interactionObserver;
    private MaterialInteractionEvent.Focus? _nativeFocus;
    private MaterialInteractionEvent.Press? _nativePress;
    private IPointer? _interactionPointer;
    private bool _sourceFocused;
    private bool _indicatorFocused;
    private int _interactionGeneration;

    private void AttachInteractions()
    {
        ReplaceInteractionSource();
        _owner!.GotFocus += NativeGotFocus;
        _owner.LostFocus += NativeLostFocus;
        _owner.PropertyChanged += PopupFocusChanged;
        _owner.AddHandler(InputElement.PointerPressedEvent, NativePointerPressed, RoutingStrategies.Bubble, true);
        _owner.AddHandler(InputElement.PointerReleasedEvent, NativePointerReleased, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.PointerMovedEvent, NativePointerMoved, RoutingStrategies.Bubble, true);
        _owner.PointerCaptureLost += NativeCaptureLost;
        if (_owner.IsFocused && _owner.IsEffectivelyEnabled) StartNativeFocus();
    }

    private void DetachInteractions()
    {
        if (_owner is not { } owner) return;
        owner.GotFocus -= NativeGotFocus;
        owner.LostFocus -= NativeLostFocus;
        owner.PropertyChanged -= PopupFocusChanged;
        owner.RemoveHandler(InputElement.PointerPressedEvent, NativePointerPressed);
        owner.RemoveHandler(InputElement.PointerReleasedEvent, NativePointerReleased);
        owner.RemoveHandler(InputElement.PointerMovedEvent, NativePointerMoved);
        owner.PointerCaptureLost -= NativeCaptureLost;
        DisconnectInteractions();
        _sourceFocused = false;
        _indicatorFocused = false;
        TextFieldThemeDefaults.SetIsFocused(owner, false);
        TextFieldThemeDefaults.SetIndicatorFocused(owner, false);
    }

    private void DisconnectInteractions(bool publish = true)
    {
        ++_interactionGeneration;
        var source = _interactionSource;
        var focus = _nativeFocus;
        var press = _nativePress;
        _interactionSource = null;
        if (publish) _owner?.ClearValue(TextFieldThemeDefaults.EffectiveInteractionSourceProperty);
        _nativeFocus = null;
        _nativePress = null;
        _interactionPointer = null;
        if (source is not null) source.Interaction -= _interactionObserver;
        _interactionObserver = null;
        _focusInteractions.Clear();
        // IndicatorLineNode.trackFocusState resets its local boolean without invalidating
        // the remembered animation values. Its first non-focus event does not repaint.
        _indicatorFocused = false;
        // Dispose the old collector before input nodes publish their paired endings. A source
        // change preserves the remembered boolean until the new collector receives an event.
        if (press is not null) source?.Emit(new MaterialInteractionEvent.CancelPress(press));
        if (focus is not null) source?.Emit(new MaterialInteractionEvent.Unfocus(focus));
    }

    private void ReplaceInteractionSource()
    {
        if (_owner is not { } owner) return;
        var source = MaterialTextField.GetInteractionSource(owner) ?? _privateSource;
        if (ReferenceEquals(source, _interactionSource)) return;
        var generation = _interactionGeneration + 1;
        DisconnectInteractions(publish: false);
        if (generation != _interactionGeneration || !ReferenceEquals(owner, _owner)) return;
        _interactionSource = source;
        _interactionObserver = interaction =>
        {
            if (generation != _interactionGeneration) return;
            if (interaction is MaterialInteractionEvent.Focus focus) _focusInteractions.Add(focus);
            else if (interaction is MaterialInteractionEvent.Unfocus unfocus) _focusInteractions.Remove(unfocus.Start);
            var focused = _focusInteractions.Count != 0;
            if (!Outlined && focused != _indicatorFocused)
            {
                _indicatorFocused = focused;
                TextFieldThemeDefaults.SetIndicatorFocused(owner, focused);
                _pendingIndicatorUpdate = false;
                RetargetFilledIndicator();
            }
            if (generation != _interactionGeneration) return;
            if (focused == _sourceFocused) return;
            _sourceFocused = focused;
            TextFieldThemeDefaults.SetIsFocused(owner, focused);
            UpdateSlots();
            Retarget();
        };
        source.Interaction += _interactionObserver;
        owner.SetValue(TextFieldThemeDefaults.EffectiveInteractionSourceProperty, source);
    }

    private void NativeGotFocus(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, _owner) && _owner is { IsEffectivelyEnabled: true }) StartNativeFocus();
    }

    private void NativeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, _owner) && _owner?.GetValue(TextFieldThemeDefaults.RetainPopupFocusProperty) != true) EndNativeFocus();
    }

    private void PopupFocusChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextFieldThemeDefaults.RetainPopupFocusProperty && e.NewValue is false && _owner?.IsFocused != true)
            EndNativeFocus();
    }

    private void StartNativeFocus()
    {
        if (_nativeFocus is not null || _interactionSource is not { } source) return;
        var focus = _nativeFocus = new MaterialInteractionEvent.Focus();
        source.Emit(focus);
    }

    private void EndNativeFocus()
    {
        var focus = _nativeFocus;
        _nativeFocus = null;
        if (focus is not null) _interactionSource?.Emit(new MaterialInteractionEvent.Unfocus(focus));
    }

    private void NativePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_owner is not { IsEffectivelyEnabled: true } owner ||
            !e.GetCurrentPoint(owner).Properties.IsLeftButtonPressed) return;
        // Embedded actions own their input. TextPresenter and decoration content still belong
        // to the native editor; observing its handled event must not suppress native selection.
        if (e.Source is Visual target && target.GetSelfAndVisualAncestors()
            .TakeWhile(v => !ReferenceEquals(v, owner)).OfType<Control>()
            .Any(c => c.Focusable && c is not Avalonia.Controls.Presenters.TextPresenter)) return;
        var generation = _interactionGeneration;
        EndNativePress(cancelled: true);
        if (generation != _interactionGeneration || _interactionSource is not { } source) return;
        _interactionPointer = e.Pointer;
        var press = _nativePress = new MaterialInteractionEvent.Press(e.GetPosition(owner));
        source.Emit(press);
    }

    private void NativePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ReferenceEquals(e.Pointer, _interactionPointer) && e.InitialPressMouseButton == MouseButton.Left)
            EndNativePress(cancelled: false);
    }

    private void NativePointerMoved(object? sender, PointerEventArgs e)
    {
        if (ReferenceEquals(e.Pointer, _interactionPointer) && _owner is { } owner &&
            (e.Handled || !new Rect(owner.Bounds.Size).Contains(e.GetPosition(owner))))
            EndNativePress(cancelled: true);
    }

    private void NativeCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (ReferenceEquals(e.Pointer, _interactionPointer)) EndNativePress(cancelled: true);
    }

    private void EndNativePress(bool cancelled)
    {
        var press = _nativePress;
        _nativePress = null;
        _interactionPointer = null;
        if (press is not null)
            _interactionSource?.Emit(cancelled
                ? new MaterialInteractionEvent.CancelPress(press)
                : new MaterialInteractionEvent.Release(press));
    }
}

// Scrim.kt at the fixed AndroidX baseline. Apache-2.0.
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>A full-size scrim with optional pointer and accessibility click actions. It does not create a modal scope.</summary>
public class Scrim : Control
{
    public static readonly StyledProperty<Color?> ColorProperty = AvaloniaProperty.Register<Scrim, Color?>(nameof(Color));
    public static readonly StyledProperty<float> AlphaProperty = AvaloniaProperty.Register<Scrim, float>(nameof(Alpha), 1f);
    public static readonly StyledProperty<bool> IsDismissEnabledProperty = AvaloniaProperty.Register<Scrim, bool>(nameof(IsDismissEnabled), true);
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = RoutedEvent.Register<Scrim, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);
    /// <summary>Null omits drawing, layout and click semantics. Transparent retains layout and input.</summary>
    public Color? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    /// <summary>Drawing alpha, clamped to zero through one without changing hit testing.</summary>
    public float Alpha { get => GetValue(AlphaProperty); set => SetValue(AlphaProperty, value); }
    /// <summary>False corresponds to the upstream null onClick callback.</summary>
    public bool IsDismissEnabled { get => GetValue(IsDismissEnabledProperty); set => SetValue(IsDismissEnabledProperty, value); }
    public event EventHandler<RoutedEventArgs>? Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }
    private readonly HashSet<IPointer> _pointers = [];
    private bool _canceled;
    private bool CanInvoke => Color is not null && IsDismissEnabled && IsEffectivelyEnabled;

    static Scrim()
    {
        AffectsMeasure<Scrim>(ColorProperty);
        AffectsRender<Scrim>(ColorProperty, AlphaProperty);
    }
    public Scrim()
    {
        SetValue(IsHitTestVisibleProperty, false, BindingPriority.Style);
        // Main-pass tap detection consumes the initial down and final up, not motion.
        AddHandler(PointerPressedEvent, Pressed, RoutingStrategies.Bubble, true);
        AddHandler(PointerMovedEvent, Moved, RoutingStrategies.Bubble, true);
        AddHandler(PointerReleasedEvent, Released, RoutingStrategies.Bubble, true);
    }
    protected override Size MeasureOverride(Size availableSize) => Color is null ? default : new(
        double.IsFinite(availableSize.Width) ? availableSize.Width : 0,
        double.IsFinite(availableSize.Height) ? availableSize.Height : 0);
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Color is { } color) context.DrawRectangle(new SolidColorBrush(color, Math.Clamp(Alpha, 0, 1)), null, new Rect(Bounds.Size));
    }
    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (!CanInvoke || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.Handled) { if (_pointers.Count != 0) _canceled = true; return; }
        if (_pointers.Count == 0) _canceled = false;
        _pointers.Add(e.Pointer); e.Pointer.Capture(this); e.Handled = true;
    }
    private void Moved(object? sender, PointerEventArgs e)
    {
        if (!_pointers.Contains(e.Pointer)) return;
        var padding = e.Pointer.Type == PointerType.Touch
            ? new Thickness(Math.Max(0, (48 - Bounds.Width) / 2), Math.Max(0, (48 - Bounds.Height) / 2)) : default;
        if (e.Handled || !new Rect(Bounds.Size).Inflate(padding).Contains(e.GetPosition(this))) _canceled = true;
    }
    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (!_pointers.Remove(e.Pointer)) return;
        _canceled |= e.Handled;
        e.Pointer.Capture(null);
        if (_pointers.Count != 0) return;
        var invoke = !_canceled && CanInvoke; _canceled = false;
        if (invoke) { e.Handled = true; RaiseEvent(new RoutedEventArgs(ClickEvent)); }
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        if (_pointers.Remove(e.Pointer)) _canceled = true;
        base.OnPointerCaptureLost(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColorProperty || change.Property == IsDismissEnabledProperty)
        {
            SetValue(IsHitTestVisibleProperty, Color is not null && IsDismissEnabled, BindingPriority.Style);
            CancelPointers();
        }
        if (change.Property == IsEffectivelyEnabledProperty) CancelPointers();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { CancelPointers(); base.OnDetachedFromVisualTree(e); }
    private void CancelPointers()
    {
        var pointers = _pointers.ToArray(); _pointers.Clear(); _canceled = true;
        foreach (var pointer in pointers) pointer.Capture(null);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ScrimPeer(this);
    private sealed class ScrimPeer(Scrim owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override bool IsControlElementCore() => owner.Color is not null && owner.IsDismissEnabled;
        protected override bool IsContentElementCore() => owner.Color is not null && owner.IsDismissEnabled;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
        public void Invoke() { if (owner.CanInvoke) owner.RaiseEvent(new RoutedEventArgs(ClickEvent)); }
    }
}

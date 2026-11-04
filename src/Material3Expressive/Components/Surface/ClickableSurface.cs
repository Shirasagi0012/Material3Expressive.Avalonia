using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>A Material surface with click and command handling, without button padding or shape defaults.</summary>
public class ClickableSurface : Button
{
    public static readonly StyledProperty<double> TonalElevationProperty = MaterialSurface.TonalElevationProperty.AddOwner<ClickableSurface>();
    public static readonly StyledProperty<double> ShadowElevationProperty = MaterialSurface.ShadowElevationProperty.AddOwner<ClickableSurface>();
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty =
        AvaloniaProperty.Register<ClickableSurface, MaterialInteractionSource?>(nameof(InteractionSource));

    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    /// <summary>The shared input and feedback stream. Null uses a private source.</summary>
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }

    private readonly MaterialInteractionSource _localSource = new();
    private MaterialInteractionSource? _source;
    private readonly HashSet<Key> _activationKeys = [];
    private bool _suppressKeyboardClick;

    static ClickableSurface() => TemplateProperty.OverrideDefaultValue<ClickableSurface>(null);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ConnectSource();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _activationKeys.Clear();
        MaterialInteraction.GetOrCreate(this).ConnectSource(null);
        _source = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InteractionSourceProperty) ConnectSource();
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) _activationKeys.Clear();
    }

    private void ConnectSource()
    {
        if (!this.IsAttachedToVisualTree()) return;
        var source = InteractionSource ?? _localSource;
        if (ReferenceEquals(source, _source)) return;
        var replacing = _source is not null;
        if (replacing && _activationKeys.Count != 0)
        {
            _activationKeys.Clear();
            // AbstractClickableNode.disposeInteractions cancels pending key activation on
            // source replacement. Reset Button's pressed flag without moving actual focus.
            base.OnLostFocus(new FocusChangedEventArgs(LostFocusEvent));
        }
        var controller = MaterialInteraction.GetOrCreate(this);
        controller.ConnectSource(null);
        _source = source;
        controller.ConnectSource(source);
        if (replacing) controller.ResetSourceIndication();
    }

    // Core clickable/selectable/toggleable activate on release for Space and Enter.
    // Reuse native Button press/capture handling without its Enter-on-KeyDown activation.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter && IsFocused && IsEffectivelyEnabled)
        {
            _activationKeys.Add(e.Key);
            _suppressKeyboardClick = true;
            try { base.OnKeyDown(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressKeyboardClick = false; }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            // Each activation key has its own press, just as the source bridge does. An
            // unmatched release must not activate or consume a different held key.
            if (!_activationKeys.Remove(e.Key)) return;
            var invoke = IsFocused && IsEffectivelyEnabled;
            if (_activationKeys.Count == 0)
            {
                _suppressKeyboardClick = true;
                try { base.OnKeyUp(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
                finally { _suppressKeyboardClick = false; }
            }
            e.Handled = true;
            if (invoke) OnClick();
            return;
        }
        base.OnKeyUp(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        _activationKeys.Clear();
        base.OnLostFocus(e);
    }

    protected override void OnClick()
    {
        if (!_suppressKeyboardClick && IsEffectivelyEnabled) base.OnClick();
    }

    internal bool CanRequestActivation => !_suppressKeyboardClick && IsEffectivelyEnabled;
    protected override AutomationPeer OnCreateAutomationPeer() => new SurfacePeer(this);
    private sealed class SurfacePeer(ClickableSurface owner) : ButtonAutomationPeer(owner)
    {
        // Surface does not prescribe a semantic role. Its invoke pattern remains available.
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }
}

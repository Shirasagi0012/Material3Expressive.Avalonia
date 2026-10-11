using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Data.Converters;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>A standard icon toggle. The application owns the checked state.</summary>
public class IconToggleButton : IconButton
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<IconToggleButton, bool>(nameof(IsChecked));

    // IconButtonDefaults.iconToggleButtonColors: ambient, Primary when checked, ambient with
    // replaced alpha when disabled. Standard icon toggle colors use rememberUpdatedState.
    public static readonly FuncMultiValueConverter<object, object> AmbientColorConverter = new((IReadOnlyList<object?> values) =>
    {
        if (values.Count < 4 || values[0] is not bool enabled || values[1] is not ISolidColorBrush ambient ||
            values[2] is not Color selectedColor || values[3] is not double disabledOpacity)
            return AvaloniaProperty.UnsetValue;
        var selected = values.Count > 4 && values[4] is true;
        return new SolidColorBrush(!enabled ? Color.FromArgb((byte)Math.Round(Math.Clamp(disabledOpacity, 0, 1) * 255), ambient.Color.R, ambient.Color.G, ambient.Color.B)
            : selected ? selectedColor : ambient.Color);
    });

    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public event EventHandler<IconCheckedChangeEventArgs>? CheckedChangeRequested;

    protected override void OnClick()
    {
        if (!IsEffectivelyEnabled) return;
        CheckedChangeRequested?.Invoke(this, new(!IsChecked));
        base.OnClick();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty) PseudoClasses.Set(":checked", IsChecked);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);

    private sealed class Peer : ButtonAutomationPeer, IToggleProvider
    {
        public Peer(IconToggleButton owner) : base(owner)
        {
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsCheckedProperty)
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                        e.OldValue is true ? ToggleState.On : ToggleState.Off,
                        e.NewValue is true ? ToggleState.On : ToggleState.Off);
            };
        }

        public ToggleState ToggleState => ((IconToggleButton)Owner).IsChecked ? ToggleState.On : ToggleState.Off;
        public void Toggle() => ((IInvokeProvider)this).Invoke();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;
    }
}

public sealed class IconCheckedChangeEventArgs(bool value) : EventArgs
{
    public bool Value { get; } = value;
}

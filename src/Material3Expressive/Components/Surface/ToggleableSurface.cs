using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Components;

/// <summary>A surface requesting changes to an application-owned checked state.</summary>
public class ToggleableSurface : ClickableSurface
{
    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<ToggleableSurface, bool>(nameof(IsChecked));
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public event EventHandler<SurfaceCheckedChangeEventArgs>? CheckedChangeRequested;

    protected override void OnClick()
    {
        if (!CanRequestActivation) return;
        CheckedChangeRequested?.Invoke(this, new(!IsChecked));
        base.OnClick();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty) PseudoClasses.Set(":checked", IsChecked);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SurfacePeer(this);
    private sealed class SurfacePeer : ButtonAutomationPeer, IToggleProvider
    {
        public SurfacePeer(ToggleableSurface owner) : base(owner)
        {
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property == IsCheckedProperty)
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                        change.OldValue is true ? ToggleState.On : ToggleState.Off,
                        change.NewValue is true ? ToggleState.On : ToggleState.Off);
            };
        }
        public ToggleState ToggleState => ((ToggleableSurface)Owner).IsChecked ? ToggleState.On : ToggleState.Off;
        public void Toggle() => Invoke();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }
}

public sealed class SurfaceCheckedChangeEventArgs(bool value) : EventArgs
{
    public bool Value { get; } = value;
}

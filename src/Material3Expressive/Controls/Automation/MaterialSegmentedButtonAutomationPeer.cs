// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt:
// single-choice items add Role.RadioButton and the single row adds selectableGroup(); multi-choice
// items use toggleable semantics. The alpha22 executable reference exposes the same roles.

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialSegmentedButtonAutomationPeer :
    ContentControlAutomationPeer,
    IToggleProvider,
    ISelectionItemProvider
{
    public MaterialSegmentedButtonAutomationPeer(MaterialSegmentedButton owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialSegmentedButton Owner => (MaterialSegmentedButton)base.Owner;

    private MaterialSegmentedButtonRow? Row => Owner.OwnerRow;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Row?.SelectionMode == SegmentedButtonSelectionMode.Single
            ? AutomationControlType.RadioButton
            : AutomationControlType.CheckBox;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

    protected override string? GetNameCore()
    {
        var explicitName = AutomationProperties.GetName(Owner);
        if (!string.IsNullOrWhiteSpace(explicitName))
            return explicitName;

        return Owner.Content switch
        {
            string text when !string.IsNullOrWhiteSpace(text) => text,
            TextBlock { Text: { } text } when !string.IsNullOrWhiteSpace(text) => text,
            Control control when !string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)) =>
                AutomationProperties.GetName(control),
            _ => base.GetNameCore()
        };
    }

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(ISelectionItemProvider))
            return Row?.SelectionMode == SegmentedButtonSelectionMode.Single ? this : null;

        if (providerType == typeof(IToggleProvider))
            return Row?.SelectionMode != SegmentedButtonSelectionMode.Single ? this : null;

        return base.GetProviderCore(providerType);
    }

    ToggleState IToggleProvider.ToggleState =>
        Owner.IsChecked == true ? ToggleState.On : ToggleState.Off;

    void IToggleProvider.Toggle()
    {
        EnsureEnabled();
        Owner.InvokeFromAutomation();
    }

    bool ISelectionItemProvider.IsSelected => Owner.IsChecked == true;

    ISelectionProvider? ISelectionItemProvider.SelectionContainer =>
        Row is { } row
            ? ControlAutomationPeer.CreatePeerForElement(row)?.GetProvider<ISelectionProvider>()
            : null;

    void ISelectionItemProvider.AddToSelection()
    {
        EnsureEnabled();
        ((ISelectionItemProvider)this).Select();
    }

    void ISelectionItemProvider.RemoveFromSelection()
    {
        EnsureEnabled();
        if (Owner.IsChecked == true)
            throw new InvalidOperationException("A single-choice segmented button cannot be removed.");
    }

    void ISelectionItemProvider.Select()
    {
        EnsureEnabled();
        Row?.SelectFromAutomation(Owner);
    }

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MaterialSegmentedButton.IsCheckedProperty)
            return;

        var oldValue = e.GetOldValue<bool?>();
        var newValue = e.GetNewValue<bool?>();
        if (Row?.SelectionMode == SegmentedButtonSelectionMode.Single)
        {
            RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                oldValue == true,
                newValue == true);
        }
        else
        {
            RaisePropertyChangedEvent(
                TogglePatternIdentifiers.ToggleStateProperty,
                oldValue == true ? ToggleState.On : ToggleState.Off,
                newValue == true ? ToggleState.On : ToggleState.Off);
        }
    }
}

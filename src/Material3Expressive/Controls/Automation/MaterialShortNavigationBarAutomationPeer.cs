using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialShortNavigationBarAutomationPeer :
    ControlAutomationPeer,
    ISelectionProvider
{
    public MaterialShortNavigationBarAutomationPeer(MaterialShortNavigationBar owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialShortNavigationBar Owner => (MaterialShortNavigationBar)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Tab;

    protected override string? GetNameCore() =>
        AutomationProperties.GetName(Owner) ?? "Short navigation";

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() =>
        Owner.VisibleItems
            .Select(ControlAutomationPeer.CreatePeerForElement)
            .Where(static peer => peer is not null)
            .Cast<AutomationPeer>()
            .ToArray();

    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(ISelectionProvider) ? this : base.GetProviderCore(providerType);

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => false;

    IReadOnlyList<AutomationPeer> ISelectionProvider.GetSelection() =>
        Owner.VisibleItems
            .Where(static item => item.IsChecked == true)
            .Select(ControlAutomationPeer.CreatePeerForElement)
            .Where(static peer => peer is not null)
            .Cast<AutomationPeer>()
            .ToArray();

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialShortNavigationBar.SelectedIndexProperty)
            RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
    }
}

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Automation.Peers;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialNavigationItemAutomationPeer :
    ToggleButtonAutomationPeer,
    ISelectionItemProvider
{
    public MaterialNavigationItemAutomationPeer(MaterialNavigationItem owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialNavigationItem Owner => (MaterialNavigationItem)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Owner.OwnerShortNavigationBar is null && Owner.OwnerWideNavigationRail is null
            ? base.GetAutomationControlTypeCore()
            : AutomationControlType.TabItem;

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(ISelectionItemProvider))
            return Owner.OwnerShortNavigationBar is null && Owner.OwnerWideNavigationRail is null
                ? null
                : this;
        return base.GetProviderCore(providerType);
    }

    bool ISelectionItemProvider.IsSelected => Owner.IsChecked == true;

    ISelectionProvider? ISelectionItemProvider.SelectionContainer =>
        Owner.OwnerShortNavigationBar is { } bar
            ? ControlAutomationPeer.CreatePeerForElement(bar)?.GetProvider<ISelectionProvider>()
            : Owner.OwnerWideNavigationRail is { } rail
                ? ControlAutomationPeer.CreatePeerForElement(rail)?.GetProvider<ISelectionProvider>()
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
            throw new InvalidOperationException("A selected navigation destination cannot be removed.");
    }

    void ISelectionItemProvider.Select()
    {
        EnsureEnabled();
        if (Owner.OwnerShortNavigationBar is { } bar)
            bar.SelectFromAutomation(Owner);
        else
            Owner.OwnerWideNavigationRail?.SelectFromAutomation(Owner);
    }

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty)
        {
            RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                e.GetOldValue<bool?>() == true,
                e.GetNewValue<bool?>() == true);
        }
    }
}

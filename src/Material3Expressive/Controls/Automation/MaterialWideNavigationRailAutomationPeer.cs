using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialWideNavigationRailAutomationPeer :
    ControlAutomationPeer,
    ISelectionProvider,
    IExpandCollapseProvider
{
    public MaterialWideNavigationRailAutomationPeer(MaterialWideNavigationRail owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialWideNavigationRail Owner => (MaterialWideNavigationRail)base.Owner;

    public ExpandCollapseState ExpandCollapseState => Owner.IsExpanded
        ? ExpandCollapseState.Expanded
        : ExpandCollapseState.Collapsed;

    public bool ShowsMenu => false;

    public void Expand()
    {
        EnsureEnabled();
        Owner.IsExpanded = true;
    }

    public void Collapse()
    {
        EnsureEnabled();
        Owner.IsExpanded = false;
    }

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => false;

    IReadOnlyList<AutomationPeer> ISelectionProvider.GetSelection() =>
        Owner.VisibleItems
            .Where(static item => item.IsChecked == true)
            .Select(ControlAutomationPeer.CreatePeerForElement)
            .Where(static peer => peer is not null)
            .Cast<AutomationPeer>()
            .ToArray();

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Tab;

    protected override string? GetNameCore() =>
        AutomationProperties.GetName(Owner) ?? "Wide navigation rail";

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>();
        if (Owner.Header is Control header
            && ControlAutomationPeer.CreatePeerForElement(header) is { } headerPeer)
        {
            children.Add(headerPeer);
        }

        children.AddRange(
            Owner.VisibleItems
                .Select(ControlAutomationPeer.CreatePeerForElement)
                .Where(static peer => peer is not null)
                .Cast<AutomationPeer>());
        return children;
    }

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(ISelectionProvider))
            return this;
        if (providerType == typeof(IExpandCollapseProvider))
            return this;
        return base.GetProviderCore(providerType);
    }

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialWideNavigationRail.SelectedIndexProperty)
        {
            RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
        }
        else if (e.Property == MaterialWideNavigationRail.IsExpandedProperty)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                e.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                e.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }
    }
}

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialFloatingToolbarAutomationPeer :
    ControlAutomationPeer,
    IExpandCollapseProvider
{
    public MaterialFloatingToolbarAutomationPeer(MaterialFloatingToolbar owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialFloatingToolbar Owner => (MaterialFloatingToolbar)base.Owner;

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

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.ToolBar;

    protected override string? GetNameCore() =>
        AutomationProperties.GetName(Owner) ?? "Floating toolbar";

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MaterialFloatingToolbar.IsExpandedProperty)
            return;

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            e.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            e.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }
}

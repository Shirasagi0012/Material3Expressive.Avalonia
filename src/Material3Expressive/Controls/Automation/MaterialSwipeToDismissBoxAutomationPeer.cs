// Compose exposes a dismiss accessibility action through semantics. Avalonia has no custom-action
// provider, so Invoke performs the common EndToStart action (or StartToEnd when it is the only
// enabled anchor). This is the explicit D003 desktop accessibility adaptation.

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialSwipeToDismissBoxAutomationPeer :
    ContentControlAutomationPeer,
    IInvokeProvider
{
    public MaterialSwipeToDismissBoxAutomationPeer(MaterialSwipeToDismissBox owner)
        : base(owner)
    {
    }

    private new MaterialSwipeToDismissBox Owner =>
        (MaterialSwipeToDismissBox)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.ListItem;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IInvokeProvider)
            ? Owner.HasDismissAction ? this : null
            : base.GetProviderCore(providerType);

    void IInvokeProvider.Invoke()
    {
        EnsureEnabled();
        Owner.DismissFromAutomation();
    }
}

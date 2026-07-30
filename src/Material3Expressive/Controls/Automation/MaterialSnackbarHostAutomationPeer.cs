// Upstream semantics source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SnackbarHost.kt:296-313.
// Compose exposes a live-region pane title and a dismiss action; Avalonia maps those to a Pane
// peer with an Invoke provider that dismisses the current snackbar.

using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Material3Expressive.Controls;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialSnackbarHostAutomationPeer :
    ContentControlAutomationPeer,
    IInvokeProvider
{
    public MaterialSnackbarHostAutomationPeer(MaterialSnackbarHost owner)
        : base(owner)
    {
    }

    private new MaterialSnackbarHost Owner => (MaterialSnackbarHost)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Pane;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override string? GetNameCore() =>
        Owner.State.CurrentSnackbarData?.Visuals.Message ?? "Snackbar";

    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IInvokeProvider) ? this : base.GetProviderCore(providerType);

    void IInvokeProvider.Invoke()
    {
        EnsureEnabled();
        Owner.State.CurrentSnackbarData?.Dismiss();
    }
}

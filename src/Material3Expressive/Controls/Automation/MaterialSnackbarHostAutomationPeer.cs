// Upstream semantics source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SnackbarHost.kt:359-374.
// Compose exposes a polite live-region pane title and a dismiss action; Avalonia maps those to a
// live Pane peer whose name change raises the native live-region event, plus Invoke for dismiss.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Material3Expressive.Controls;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialSnackbarHostAutomationPeer :
    ContentControlAutomationPeer,
    IInvokeProvider
{
    private SnackbarHostState _observedState;
    private string _lastName;

    public MaterialSnackbarHostAutomationPeer(MaterialSnackbarHost owner)
        : base(owner)
    {
        _observedState = owner.State;
        _lastName = NameFor(_observedState);
        _observedState.PropertyChanged += StatePropertyChanged;
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialSnackbarHost Owner => (MaterialSnackbarHost)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Pane;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override AutomationLiveSetting GetLiveSettingCore() =>
        AutomationLiveSetting.Polite;

    protected override string? GetNameCore() => NameFor(_observedState);

    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IInvokeProvider) ? this : base.GetProviderCore(providerType);

    void IInvokeProvider.Invoke()
    {
        EnsureEnabled();
        Owner.State.CurrentSnackbarData?.Dismiss();
    }

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MaterialSnackbarHost.StateProperty)
            return;

        _observedState.PropertyChanged -= StatePropertyChanged;
        _observedState = Owner.State;
        _observedState.PropertyChanged += StatePropertyChanged;
        RaiseNameChanged();
    }

    private void StatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SnackbarHostState.CurrentSnackbarData))
            RaiseNameChanged();
    }

    private void RaiseNameChanged()
    {
        var next = NameFor(_observedState);
        if (next == _lastName)
            return;

        var previous = _lastName;
        _lastName = next;
        RaisePropertyChangedEvent(
            AutomationElementIdentifiers.NameProperty,
            previous,
            next);
    }

    private static string NameFor(SnackbarHostState state) =>
        state.CurrentSnackbarData?.Visuals.Message ?? "Snackbar";
}

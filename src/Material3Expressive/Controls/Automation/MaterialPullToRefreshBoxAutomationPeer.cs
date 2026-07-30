// PullToRefresh exposes progress semantics only while visible. Avalonia cannot dynamically add and
// remove a peer, so the host keeps a read-only RangeValue provider and reports the clamped progress.
// Invoke is the D003 equivalent of the platform refresh action.

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialPullToRefreshBoxAutomationPeer :
    ContentControlAutomationPeer,
    IInvokeProvider,
    IRangeValueProvider
{
    public MaterialPullToRefreshBoxAutomationPeer(MaterialPullToRefreshBox owner)
        : base(owner)
    {
        var previous = Math.Clamp(owner.State.DistanceFraction, 0, 1);
        owner.ProgressChanged += (_, _) =>
        {
            var next = Math.Clamp(owner.State.DistanceFraction, 0, 1);
            RaisePropertyChangedEvent(
                RangeValuePatternIdentifiers.ValueProperty,
                previous,
                next);
            previous = next;
        };
    }

    private new MaterialPullToRefreshBox Owner => (MaterialPullToRefreshBox)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Pane;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IInvokeProvider)
            ? Owner.HasRefreshAction ? this : null
            : providerType == typeof(IRangeValueProvider)
                ? this
                : base.GetProviderCore(providerType);

    void IInvokeProvider.Invoke()
    {
        EnsureEnabled();
        Owner.RequestRefreshFromAutomation();
    }

    bool IRangeValueProvider.IsReadOnly => true;

    double IRangeValueProvider.Minimum => 0;

    double IRangeValueProvider.Maximum => 1;

    double IRangeValueProvider.Value => Math.Clamp(Owner.State.DistanceFraction, 0, 1);

    double IRangeValueProvider.LargeChange => double.NaN;

    double IRangeValueProvider.SmallChange => double.NaN;

    void IRangeValueProvider.SetValue(double value) =>
        throw new InvalidOperationException("Pull-to-refresh progress is read-only.");
}

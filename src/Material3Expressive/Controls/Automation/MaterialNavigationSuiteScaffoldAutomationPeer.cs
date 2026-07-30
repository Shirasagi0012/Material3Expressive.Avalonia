// Upstream NavigationSuiteScaffold adds no container semantics. Keep one neutral Pane so the
// active navigation component, its item peers, primary action, and body retain native semantics.

using Avalonia.Automation.Peers;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialNavigationSuiteScaffoldAutomationPeer(
    MaterialNavigationSuiteScaffold owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Pane;
}

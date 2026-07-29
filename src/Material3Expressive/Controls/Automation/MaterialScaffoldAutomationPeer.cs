// Upstream behavior source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Scaffold.kt:87-305.
// Compose adds no Scaffold semantics node; its slot descendants keep their own semantics and focus
// order. Avalonia exposes only a neutral Pane peer so the five descendant presenters remain in the
// automation tree without inventing selection, activation, or snackbar state providers.

using Avalonia.Automation.Peers;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialScaffoldAutomationPeer(MaterialScaffold owner)
    : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Pane;
}

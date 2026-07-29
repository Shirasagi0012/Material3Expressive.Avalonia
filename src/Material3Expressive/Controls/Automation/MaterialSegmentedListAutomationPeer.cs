// Upstream behavior source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt:552-855.
// Compose leaves the group as caller-owned layout and places action/selection semantics on each
// SegmentedListItem. Avalonia adds only a List container role; it exposes no owned selection
// provider and preserves each MaterialListItem peer and InteractionMode.

using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialSegmentedListAutomationPeer(MaterialSegmentedList owner)
    : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.List;
}

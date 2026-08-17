using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by <see cref="MaterialFabMenu"/>.
/// </summary>
/// <remarks>
/// The menu currently consumes only its two arrangement gaps. Expansion progress, stagger,
/// child placement and motion remain control policy; the complete upstream token record is not
/// copied into a semantic profile merely because it exists in the source table.
/// </remarks>
public static class FabMenuValueKeys
{
    /// <summary>Gets the gap between neighbouring expanded items.</summary>
    public static MaterialValueKey<double> ItemSpacing { get; } =
        MaterialValue.FromRaw(
            "Value.FabMenu.ItemSpacing",
            FabMenuBaselineTokensKeys.ListItemBetweenSpace);

    /// <summary>Gets the gap between the last expanded item and the close button.</summary>
    public static MaterialValueKey<double> CloseButtonSpacing { get; } =
        MaterialValue.FromRaw(
            "Value.FabMenu.CloseButtonSpacing",
            FabMenuBaselineTokensKeys.CloseButtonBetweenSpace);
}

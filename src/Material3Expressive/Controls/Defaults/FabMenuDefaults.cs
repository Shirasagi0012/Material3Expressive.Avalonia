using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>The semantic layout and typography metrics consumed by a Material FAB menu.</summary>
internal sealed record FabMenuMetrics(
    double CloseButtonSize,
    double CloseButtonIconSize,
    ShapeRole CloseButtonShape,
    double CloseButtonBetweenSpace,
    double CloseButtonElevation,
    double ItemHeight,
    ShapeRole ItemShape,
    double ItemBetweenSpace,
    double ItemElevation,
    double ItemIconSize,
    double ItemIconLabelSpace,
    double ItemLeadingSpace,
    double ItemTrailingSpace,
    TypographyRole ItemLabelTextStyle);

/// <summary>
/// Component-owned assembly of the FAB-menu values that are not individually exposed by the raw
/// upstream table.
/// </summary>
/// <remarks>
/// <see cref="FabMenuBaselineTokens"/> preserves the AndroidX token data. The menu's one
/// Compose-default choice absent from that raw table — the TitleMedium item label — belongs with
/// the menu's C# layout policy.
/// </remarks>
internal static class FabMenuDefaults
{
    private static readonly FabMenuMetrics DefaultMetrics = new(
        FabMenuBaselineTokens.CloseButtonContainerHeight,
        FabMenuBaselineTokens.CloseButtonIconSize,
        FabMenuBaselineTokens.CloseButtonContainerShape,
        FabMenuBaselineTokens.CloseButtonBetweenSpace,
        FabMenuBaselineTokens.CloseButtonContainerElevation,
        FabMenuBaselineTokens.ListItemContainerHeight,
        FabMenuBaselineTokens.ListItemContainerShape,
        FabMenuBaselineTokens.ListItemBetweenSpace,
        FabMenuBaselineTokens.ListItemContainerElevation,
        FabMenuBaselineTokens.ListItemIconSize,
        FabMenuBaselineTokens.ListItemIconLabelSpace,
        FabMenuBaselineTokens.ListItemLeadingSpace,
        FabMenuBaselineTokens.ListItemTrailingSpace,
        TypographyRole.TitleMedium);

    /// <summary>Returns the immutable layout contract consumed by one FAB menu.</summary>
    public static FabMenuMetrics Metrics() => DefaultMetrics;
}

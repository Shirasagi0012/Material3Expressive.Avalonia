using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

[Obsolete(
    "Legacy button-group geometry snapshot. Use ButtonGroupValueKeys profiles instead.",
    DiagnosticId = "M3E1005")]
/// <summary>Compatibility-only geometry snapshot used by unmigrated tests.</summary>
internal sealed record ButtonGroupMetrics(
    double BetweenSpace,
    double ContainerHeight,
    double InnerCornerSize,
    double PressedInnerCornerSize,
    double SelectedInnerCornerPercent,
    bool IsConnected);

[Obsolete(
    "Legacy button-group defaults. Use ButtonGroupValueKeys profiles instead.",
    DiagnosticId = "M3E1005")]
/// <summary>Compatibility-only policy for the pre-MaterialValue button-group tests.</summary>
internal static class ButtonGroupDefaults
{
    [Obsolete(
        "Legacy button-group defaults. Use ButtonGroupValueKeys profiles instead.",
        DiagnosticId = "M3E1005")]
    public static ButtonGroupMetrics Metrics(ButtonGroupVariant variant) => variant switch
    {
        ButtonGroupVariant.Standard => new(
            ButtonGroupSmallTokens.BetweenSpace,
            ButtonGroupSmallTokens.ContainerHeight,
            0,
            0,
            50,
            false),
        ButtonGroupVariant.Connected => new(
            ConnectedButtonGroupSmallTokens.BetweenSpace,
            ConnectedButtonGroupSmallTokens.ContainerHeight,
            ConnectedButtonGroupSmallTokens.InnerCornerSize,
            ConnectedButtonGroupSmallTokens.PressedInnerCornerSize,
            ConnectedButtonGroupSmallTokens.SelectedInnerCornerCornerSizePercent,
            true),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };
}

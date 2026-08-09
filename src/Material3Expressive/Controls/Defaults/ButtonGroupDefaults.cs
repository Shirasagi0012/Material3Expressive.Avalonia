using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Resolved geometry used by <see cref="MaterialButtonGroup"/>.</summary>
internal sealed record ButtonGroupMetrics(
    double BetweenSpace,
    double ContainerHeight,
    double InnerCornerSize,
    double PressedInnerCornerSize,
    double SelectedInnerCornerPercent,
    bool IsConnected);

/// <summary>Component-owned policy for button-group geometry.</summary>
internal static class ButtonGroupDefaults
{
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

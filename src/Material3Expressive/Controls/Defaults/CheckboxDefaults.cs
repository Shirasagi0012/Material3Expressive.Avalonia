using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// Geometry consumed by the self-rendering Material checkbox.
/// </summary>
/// <remarks>
/// <see cref="CheckboxTokens"/> is a verbatim upstream token transcription.  The 20dp canvas is
/// the compatibility path used by the pinned Compose reference, so that correction belongs in
/// this component-owned defaults mapping rather than in a generated defaults table.
/// </remarks>
internal sealed record CheckboxMetrics(
    double ContainerSize,
    double ContainerCornerRadius,
    double IconSize,
    double StateLayerSize,
    double OutlineWidth);

/// <summary>Theme-resolved colour roles used by a Material checkbox.</summary>
internal sealed record CheckboxColors(
    IImmutableBrush SelectedContainerColor,
    IImmutableBrush SelectedIconColor,
    IImmutableBrush SelectedStateLayerColor,
    IImmutableBrush UnselectedOutlineColor,
    IImmutableBrush UnselectedStateLayerColor,
    IImmutableBrush DisabledSelectedContainerColor,
    IImmutableBrush DisabledSelectedIconColor,
    IImmutableBrush DisabledOutlineColor,
    IImmutableBrush ErrorContainerColor,
    IImmutableBrush ErrorIconColor,
    IImmutableBrush ErrorOutlineColor);

/// <summary>
/// Resolves the raw checkbox token set into the semantic profile the renderer consumes.
/// </summary>
/// <remarks>
/// This class is intentionally stateless.  Theme and variant lifecycle belongs to the control's
/// <see cref="MaterialElementContext"/>; no component-default snapshot or override bag is needed
/// for a self-rendering control.
/// </remarks>
internal static class CheckboxDefaults
{
    // Checkbox.kt in the pinned alpha22 reference still uses CheckboxSize (20dp), while the
    // newer CheckboxTokens.ContainerSize is 18dp. Keep that lineage correction explicit here.
    private const double ReferenceContainerSize = 20;

    public static CheckboxMetrics Metrics() => new(
        ReferenceContainerSize,
        CheckboxTokens.ContainerShape,
        CheckboxTokens.IconSize,
        CheckboxTokens.StateLayerSize,
        CheckboxTokens.UnselectedOutlineWidth);

    public static CheckboxColors Colors(ResolvedTheme theme, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var scheme = theme.ColorScheme;
        return new CheckboxColors(
            CheckboxTokens.SelectedContainerColor.ResolveBrush(scheme, isDark),
            CheckboxTokens.SelectedIconColor.ResolveBrush(scheme, isDark),
            // CheckboxDefaults uses the selected container role for its state layer.
            CheckboxTokens.SelectedContainerColor.ResolveBrush(scheme, isDark),
            CheckboxTokens.UnselectedOutlineColor.ResolveBrush(scheme, isDark),
            CheckboxTokens.UnselectedFocusOutlineColor.ResolveBrush(scheme, isDark),
            CheckboxTokens.SelectedDisabledContainerColor.ResolveBrush(
                scheme, isDark, CheckboxTokens.SelectedDisabledContainerOpacity),
            CheckboxTokens.SelectedDisabledIconColor.ResolveBrush(scheme, isDark),
            CheckboxTokens.UnselectedDisabledOutlineColor.ResolveBrush(
                scheme, isDark, CheckboxTokens.SelectedDisabledContainerOpacity),
            CheckboxTokens.SelectedErrorContainerColor.ResolveBrush(scheme, isDark),
            CheckboxTokens.SelectedErrorIconColor.ResolveBrush(scheme, isDark),
            CheckboxTokens.UnselectedErrorOutlineColor.ResolveBrush(scheme, isDark));
    }
}

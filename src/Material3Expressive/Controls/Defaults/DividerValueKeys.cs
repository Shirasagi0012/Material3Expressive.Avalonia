using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by <see cref="MaterialDivider" />.
/// </summary>
/// <remarks>
/// The divider has two independent, property-ready leaves.  Orientation, hairline behaviour,
/// render scaling and bounds are deliberately not dependencies of either endpoint: those are
/// late control policies rather than token values.
/// </remarks>
public static class DividerValueKeys
{
    /// <summary>Gets the immutable brush used for the divider stroke.</summary>
    public static MaterialValueKey<IImmutableBrush> DividerBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Divider.DividerBrush",
            DividerTokensKeys.Color);

    /// <summary>Gets the logical cross-axis thickness of a divider.</summary>
    public static MaterialValueKey<double> Thickness { get; } =
        MaterialValue.FromRaw(
            "Value.Divider.Thickness",
            DividerTokensKeys.Thickness);
}

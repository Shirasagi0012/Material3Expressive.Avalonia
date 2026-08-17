using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready geometry endpoints used by <see cref="MaterialButtonGroup"/>.
/// </summary>
/// <remarks>
/// The group has two component policies, rather than one endpoint that inspects the requesting
/// group. Variant selection belongs to the control theme; each plan only evaluates the Raw
/// tokens declared by that policy. Position, orientation and the resulting segment shapes stay
/// late in <see cref="MaterialButtonGroup"/> because they depend on its children and layout.
/// </remarks>
public static class ButtonGroupValueKeys
{
    /// <summary>Geometry profile for a spaced (standard) button group.</summary>
    public static MaterialValueKey<ButtonGroupGeometryProfile> StandardProfile { get; } =
        new(
            "Value.ButtonGroup.Standard.GeometryProfile",
            new RawRecipePlan<ButtonGroupGeometryProfile>(
                new ButtonGroupGeometryProfileRecipe(
                    ButtonGroupSmallTokensKeys.BetweenSpace,
                    ButtonGroupSmallTokensKeys.ContainerHeight,
                    innerCornerSize: null,
                    isConnected: false)));

    /// <summary>Geometry profile for a contiguous (connected) button group.</summary>
    public static MaterialValueKey<ButtonGroupGeometryProfile> ConnectedProfile { get; } =
        new(
            "Value.ButtonGroup.Connected.GeometryProfile",
            new RawRecipePlan<ButtonGroupGeometryProfile>(
                new ButtonGroupGeometryProfileRecipe(
                    ConnectedButtonGroupSmallTokensKeys.BetweenSpace,
                    ConnectedButtonGroupSmallTokensKeys.ContainerHeight,
                    ConnectedButtonGroupSmallTokensKeys.InnerCornerSize,
                    isConnected: true)));
}

/// <summary>
/// The immutable geometry inputs directly consumed by a button group during measure and arrange.
/// </summary>
/// <remarks>
/// This profile deliberately excludes the upstream connected-group values for pressed and
/// selected inner corners. The current group does not consume those values; copying them here
/// would turn the Flat schema into a mechanical mirror of the Raw table.
/// </remarks>
public sealed record ButtonGroupGeometryProfile(
    double BetweenSpace,
    double ContainerHeight,
    double InnerCornerSize,
    bool IsConnected);

internal sealed class ButtonGroupGeometryProfileRecipe : IRawValueRecipe<ButtonGroupGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _betweenSpace;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<double>? _innerCornerSize;
    private readonly bool _isConnected;

    internal ButtonGroupGeometryProfileRecipe(
        MaterialToken<double> betweenSpace,
        MaterialToken<double> containerHeight,
        MaterialToken<double>? innerCornerSize,
        bool isConnected)
    {
        _betweenSpace = betweenSpace ?? throw new ArgumentNullException(nameof(betweenSpace));
        _containerHeight = containerHeight ??
            throw new ArgumentNullException(nameof(containerHeight));
        if (isConnected && innerCornerSize is null)
            throw new ArgumentException(
                "A connected button group profile must declare its inner-corner token.",
                nameof(innerCornerSize));

        _innerCornerSize = innerCornerSize;
        _isConnected = isConnected;
        _dependencies = innerCornerSize is null
            ? Array.AsReadOnly<MaterialToken>([betweenSpace, containerHeight])
            : Array.AsReadOnly<MaterialToken>([betweenSpace, containerHeight, innerCornerSize]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ButtonGroupGeometryProfile Evaluate(MaterialValueContext context)
    {
        var innerCornerSize = _innerCornerSize is null
            ? 0
            : context.Get(_innerCornerSize);

        return new ButtonGroupGeometryProfile(
            context.Get(_betweenSpace),
            context.Get(_containerHeight),
            innerCornerSize,
            _isConnected);
    }
}

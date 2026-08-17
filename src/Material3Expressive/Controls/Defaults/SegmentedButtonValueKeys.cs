using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by segmented-button controls.
/// </summary>
/// <remarks>
/// The row and segment consume the same typed geometry profile.  Selection, enabled-state
/// policy, segment position, and interaction remain control concerns; the visual profile merely
/// contains the state alternatives selected by that policy.
/// </remarks>
public static class SegmentedButtonValueKeys
{
    /// <summary>Gets the layout and late-shape inputs shared by a row and its segments.</summary>
    public static MaterialValueKey<SegmentedButtonGeometryProfile> GeometryProfile { get; } =
        new(
            "Value.SegmentedButton.GeometryProfile",
            new RawRecipePlan<SegmentedButtonGeometryProfile>(
                new SegmentedButtonGeometryProfileRecipe(
                    OutlinedSegmentedButtonTokensKeys.ContainerHeight,
                    OutlinedSegmentedButtonTokensKeys.Shape,
                    OutlinedSegmentedButtonTokensKeys.IconSize,
                    OutlinedSegmentedButtonTokensKeys.OutlineWidth)));

    /// <summary>Gets the state alternatives consumed by a segment renderer.</summary>
    public static MaterialValueKey<SegmentedButtonVisualColors> VisualColors { get; } =
        new(
            "Value.SegmentedButton.VisualColors",
            new RawRecipePlan<SegmentedButtonVisualColors>(
                new SegmentedButtonVisualColorsRecipe(
                    OutlinedSegmentedButtonTokensKeys.SelectedContainerColor,
                    OutlinedSegmentedButtonTokensKeys.SelectedLabelTextColor,
                    OutlinedSegmentedButtonTokensKeys.OutlineColor,
                    OutlinedSegmentedButtonTokensKeys.UnselectedLabelTextColor,
                    OutlinedSegmentedButtonTokensKeys.DisabledLabelTextColor,
                    OutlinedSegmentedButtonTokensKeys.DisabledLabelTextOpacity,
                    OutlinedSegmentedButtonTokensKeys.DisabledOutlineOpacity)));

    /// <summary>Gets the label typography role used by the segment content presenter.</summary>
    public static MaterialValueKey<TypographyRole> LabelTextStyle { get; } =
        MaterialValue.FromRaw(
            "Value.SegmentedButton.LabelTextStyle",
            OutlinedSegmentedButtonTokensKeys.LabelTextFont);
}

/// <summary>Immutable layout inputs consumed by a segmented-button row and its segments.</summary>
public sealed record SegmentedButtonGeometryProfile(
    double ContainerHeight,
    ShapeRole ContainerShape,
    double IconSize,
    double IconSpacing,
    Thickness ContentPadding,
    double MinimumWidth,
    double OutlineWidth);

/// <summary>Immutable state alternatives consumed by the segmented-button renderer.</summary>
public sealed record SegmentedButtonVisualColors(
    IImmutableBrush ActiveContainerColor,
    IImmutableBrush ActiveContentColor,
    IImmutableBrush BorderColor,
    IImmutableBrush InactiveContainerColor,
    IImmutableBrush InactiveContentColor,
    IImmutableBrush DisabledActiveContainerColor,
    IImmutableBrush DisabledActiveContentColor,
    IImmutableBrush DisabledInactiveContainerColor,
    IImmutableBrush DisabledInactiveContentColor,
    IImmutableBrush DisabledBorderColor);

internal sealed class SegmentedButtonGeometryProfileRecipe
    : IRawValueRecipe<SegmentedButtonGeometryProfile>
{
    private const double IconSpacing = 8;
    private const double HorizontalContentPadding = 12;
    private const double VerticalContentPadding = 8;
    private const double MinimumWidth = 58;

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<ShapeRole> _containerShape;
    private readonly MaterialToken<double> _iconSize;
    private readonly MaterialToken<double> _outlineWidth;

    internal SegmentedButtonGeometryProfileRecipe(
        MaterialToken<double> containerHeight,
        MaterialToken<ShapeRole> containerShape,
        MaterialToken<double> iconSize,
        MaterialToken<double> outlineWidth)
    {
        _containerHeight = containerHeight ??
            throw new ArgumentNullException(nameof(containerHeight));
        _containerShape = containerShape ??
            throw new ArgumentNullException(nameof(containerShape));
        _iconSize = iconSize ?? throw new ArgumentNullException(nameof(iconSize));
        _outlineWidth = outlineWidth ?? throw new ArgumentNullException(nameof(outlineWidth));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerHeight, containerShape, iconSize, outlineWidth]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SegmentedButtonGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_containerHeight),
        context.Get(_containerShape),
        context.Get(_iconSize),
        IconSpacing,
        new Thickness(HorizontalContentPadding, VerticalContentPadding),
        MinimumWidth,
        context.Get(_outlineWidth));
}

internal sealed class SegmentedButtonVisualColorsRecipe
    : IRawValueRecipe<SegmentedButtonVisualColors>
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Colors.Transparent);

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _activeContainerColor;
    private readonly MaterialToken<Color> _activeContentColor;
    private readonly MaterialToken<Color> _borderColor;
    private readonly MaterialToken<Color> _inactiveContentColor;
    private readonly MaterialToken<Color> _disabledContentColor;
    private readonly MaterialToken<double> _disabledContentOpacity;
    private readonly MaterialToken<double> _disabledBorderOpacity;

    internal SegmentedButtonVisualColorsRecipe(
        MaterialToken<Color> activeContainerColor,
        MaterialToken<Color> activeContentColor,
        MaterialToken<Color> borderColor,
        MaterialToken<Color> inactiveContentColor,
        MaterialToken<Color> disabledContentColor,
        MaterialToken<double> disabledContentOpacity,
        MaterialToken<double> disabledBorderOpacity)
    {
        _activeContainerColor = activeContainerColor ??
            throw new ArgumentNullException(nameof(activeContainerColor));
        _activeContentColor = activeContentColor ??
            throw new ArgumentNullException(nameof(activeContentColor));
        _borderColor = borderColor ?? throw new ArgumentNullException(nameof(borderColor));
        _inactiveContentColor = inactiveContentColor ??
            throw new ArgumentNullException(nameof(inactiveContentColor));
        _disabledContentColor = disabledContentColor ??
            throw new ArgumentNullException(nameof(disabledContentColor));
        _disabledContentOpacity = disabledContentOpacity ??
            throw new ArgumentNullException(nameof(disabledContentOpacity));
        _disabledBorderOpacity = disabledBorderOpacity ??
            throw new ArgumentNullException(nameof(disabledBorderOpacity));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            activeContainerColor,
            activeContentColor,
            borderColor,
            inactiveContentColor,
            disabledContentColor,
            disabledContentOpacity,
            disabledBorderOpacity,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SegmentedButtonVisualColors Evaluate(MaterialValueContext context)
    {
        var activeContainer = Brush(context, _activeContainerColor);
        var activeContent = Brush(context, _activeContentColor);
        var border = Brush(context, _borderColor);
        var disabledContent = Brush(
            context,
            _disabledContentColor,
            _disabledContentOpacity);

        // Compose's alpha22 compatibility baseline applies disabled alpha to Outline rather than
        // the newer DisabledOutlineColor role.  Keep that mapping until parity deliberately moves.
        var disabledBorder = Brush(context, _borderColor, _disabledBorderOpacity);

        return new SegmentedButtonVisualColors(
            activeContainer,
            activeContent,
            border,
            Transparent,
            Brush(context, _inactiveContentColor),
            activeContainer,
            disabledContent,
            Transparent,
            disabledContent,
            disabledBorder);
    }

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color> colorToken,
        MaterialToken<double>? opacityToken = null)
    {
        var color = context.Get(colorToken);
        if (opacityToken is not null)
        {
            var opacity = Math.Clamp(context.Get(opacityToken), 0d, 1d);
            color = new Color(
                (byte)Math.Round(color.A * opacity),
                color.R,
                color.G,
                color.B);
        }

        return new ImmutableSolidColorBrush(color);
    }
}

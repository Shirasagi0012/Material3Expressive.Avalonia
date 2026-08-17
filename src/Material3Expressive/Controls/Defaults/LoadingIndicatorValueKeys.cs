using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by <see cref="MaterialLoadingIndicator" />.
/// </summary>
/// <remarks>
/// Variant selection is a ControlTheme policy: standard and contained indicators select separate
/// endpoints. The plans themselves only read their declared Raw dependencies. Shape resolution,
/// polygon morphing, bounds, progress state and motion remain late control concerns.
/// </remarks>
public static class LoadingIndicatorValueKeys
{
    /// <summary>Gets the geometry profile for the standard indicator.</summary>
    public static MaterialValueKey<LoadingIndicatorGeometryProfile> StandardGeometry { get; } =
        Geometry(
            "Value.LoadingIndicator.Standard.Geometry",
            hasContainer: false);

    /// <summary>Gets the geometry profile for the contained indicator.</summary>
    public static MaterialValueKey<LoadingIndicatorGeometryProfile> ContainedGeometry { get; } =
        Geometry(
            "Value.LoadingIndicator.Contained.Geometry",
            hasContainer: true);

    /// <summary>Gets the active and transparent-container brushes for the standard indicator.</summary>
    public static MaterialValueKey<LoadingIndicatorVisualColors> StandardVisualColors { get; } =
        new(
            "Value.LoadingIndicator.Standard.VisualColors",
            new RawRecipePlan<LoadingIndicatorVisualColors>(
                new LoadingIndicatorVisualColorsRecipe(
                    LoadingIndicatorTokensKeys.ActiveIndicatorColor,
                    containerColor: null)));

    /// <summary>Gets the active and container brushes for the contained indicator.</summary>
    public static MaterialValueKey<LoadingIndicatorVisualColors> ContainedVisualColors { get; } =
        new(
            "Value.LoadingIndicator.Contained.VisualColors",
            new RawRecipePlan<LoadingIndicatorVisualColors>(
                new LoadingIndicatorVisualColorsRecipe(
                    LoadingIndicatorTokensKeys.ContainedActiveColor,
                    LoadingIndicatorTokensKeys.ContainedContainerColor)));

    private static MaterialValueKey<LoadingIndicatorGeometryProfile> Geometry(
        string path,
        bool hasContainer) =>
        new(
            path,
            new RawRecipePlan<LoadingIndicatorGeometryProfile>(
                new LoadingIndicatorGeometryProfileRecipe(
                    LoadingIndicatorTokensKeys.ActiveSize,
                    LoadingIndicatorTokensKeys.ContainerWidth,
                    LoadingIndicatorTokensKeys.ContainerHeight,
                    LoadingIndicatorTokensKeys.ContainerShape,
                    hasContainer)));
}

/// <summary>Raw-derived layout and late-shape inputs consumed by a loading indicator.</summary>
public sealed record LoadingIndicatorGeometryProfile(
    double ActiveSize,
    double ContainerWidth,
    double ContainerHeight,
    ShapeRole ContainerShape,
    bool HasContainer);

/// <summary>State-independent brushes consumed by a loading indicator.</summary>
public sealed record LoadingIndicatorVisualColors(
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush ContainerColor);

internal sealed class LoadingIndicatorGeometryProfileRecipe
    : IRawValueRecipe<LoadingIndicatorGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _activeSize;
    private readonly MaterialToken<double> _containerWidth;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<ShapeRole> _containerShape;
    private readonly bool _hasContainer;

    internal LoadingIndicatorGeometryProfileRecipe(
        MaterialToken<double> activeSize,
        MaterialToken<double> containerWidth,
        MaterialToken<double> containerHeight,
        MaterialToken<ShapeRole> containerShape,
        bool hasContainer)
    {
        _activeSize = activeSize ?? throw new ArgumentNullException(nameof(activeSize));
        _containerWidth = containerWidth ?? throw new ArgumentNullException(nameof(containerWidth));
        _containerHeight = containerHeight ?? throw new ArgumentNullException(nameof(containerHeight));
        _containerShape = containerShape ?? throw new ArgumentNullException(nameof(containerShape));
        _hasContainer = hasContainer;
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [activeSize, containerWidth, containerHeight, containerShape]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public LoadingIndicatorGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_activeSize),
        context.Get(_containerWidth),
        context.Get(_containerHeight),
        context.Get(_containerShape),
        _hasContainer);
}

internal sealed class LoadingIndicatorVisualColorsRecipe
    : IRawValueRecipe<LoadingIndicatorVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _activeIndicatorColor;
    private readonly MaterialToken<Color>? _containerColor;

    internal LoadingIndicatorVisualColorsRecipe(
        MaterialToken<Color> activeIndicatorColor,
        MaterialToken<Color>? containerColor)
    {
        _activeIndicatorColor = activeIndicatorColor ??
            throw new ArgumentNullException(nameof(activeIndicatorColor));
        _containerColor = containerColor;
        _dependencies = containerColor is null
            ? Array.AsReadOnly<MaterialToken>([activeIndicatorColor])
            : Array.AsReadOnly<MaterialToken>([activeIndicatorColor, containerColor]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public LoadingIndicatorVisualColors Evaluate(MaterialValueContext context) => new(
        new ImmutableSolidColorBrush(context.Get(_activeIndicatorColor)),
        _containerColor is null
            ? new ImmutableSolidColorBrush(Colors.Transparent)
            : new ImmutableSolidColorBrush(context.Get(_containerColor)));
}

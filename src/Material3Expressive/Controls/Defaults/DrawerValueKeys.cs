using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>Consumer-ready Material value endpoints for navigation drawers.</summary>
public static class DrawerValueKeys
{
    public static MaterialValueKey<DrawerGeometryProfile> ModalGeometry { get; } =
        Geometry(
            "Value.NavigationDrawer.Modal.Geometry",
            NavigationDrawerTokensKeys.ModalContainerElevation,
            ScrimTokensKeys.ContainerOpacity,
            isModal: true);

    public static MaterialValueKey<DrawerGeometryProfile> StandardGeometry { get; } =
        new(
            "Value.NavigationDrawer.Standard.Geometry",
            new RawRecipePlan<DrawerGeometryProfile>(
                new DrawerGeometryRecipe(
                    NavigationDrawerTokensKeys.StandardContainerElevation,
                    NavigationDrawerTokensKeys.ContainerWidth,
                    NavigationDrawerTokensKeys.ContainerShape,
                    NavigationDrawerTokensKeys.ActiveIndicatorHeight,
                    NavigationDrawerTokensKeys.ActiveIndicatorShape,
                    NavigationDrawerTokensKeys.IconSize,
                    NavigationDrawerTokensKeys.HeadlineFont,
                    NavigationDrawerTokensKeys.LabelTextFont,
                    opacity: null,
                    isModal: false)));

    public static MaterialValueKey<DrawerVisualColors> ModalVisualColors { get; } =
        Colors("Value.NavigationDrawer.Modal.VisualColors", NavigationDrawerTokensKeys.ModalContainerColor);

    public static MaterialValueKey<DrawerVisualColors> StandardVisualColors { get; } =
        Colors("Value.NavigationDrawer.Standard.VisualColors", NavigationDrawerTokensKeys.StandardContainerColor);

    private static MaterialValueKey<DrawerGeometryProfile> Geometry(
        string path,
        MaterialToken<double> elevation,
        MaterialToken<double> opacity,
        bool isModal) =>
        new(
            path,
            new RawRecipePlan<DrawerGeometryProfile>(
                new DrawerGeometryRecipe(
                    elevation,
                    NavigationDrawerTokensKeys.ContainerWidth,
                    NavigationDrawerTokensKeys.ContainerShape,
                    NavigationDrawerTokensKeys.ActiveIndicatorHeight,
                    NavigationDrawerTokensKeys.ActiveIndicatorShape,
                    NavigationDrawerTokensKeys.IconSize,
                    NavigationDrawerTokensKeys.HeadlineFont,
                    NavigationDrawerTokensKeys.LabelTextFont,
                    opacity,
                    isModal)));

    private static MaterialValueKey<DrawerVisualColors> Colors(
        string path,
        MaterialToken<Color> container) =>
        new(
            path,
            new RawRecipePlan<DrawerVisualColors>(
                new DrawerVisualColorsRecipe(
                    container,
                    SystemColorTokens.Scrim,
                    NavigationDrawerTokensKeys.HeadlineColor,
                    NavigationDrawerTokensKeys.ActiveIndicatorColor,
                    NavigationDrawerTokensKeys.ActiveIconColor,
                    NavigationDrawerTokensKeys.ActiveLabelTextColor,
                    NavigationDrawerTokensKeys.InactiveIconColor,
                    NavigationDrawerTokensKeys.InactiveLabelTextColor,
                    SystemColorTokens.OnSurface)));
}

public sealed record DrawerGeometryProfile(
    double ContainerElevation,
    double ContainerWidth,
    ShapeRole ContainerShape,
    double ItemHeight,
    ShapeRole ItemShape,
    double IconSize,
    double ScrimOpacity,
    TypographyRole HeadlineTextStyle,
    TypographyRole LabelTextStyle,
    bool IsModal);

public sealed record DrawerVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ScrimColor,
    IImmutableBrush HeadlineColor,
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush ActiveIconColor,
    IImmutableBrush ActiveLabelTextColor,
    IImmutableBrush InactiveIconColor,
    IImmutableBrush InactiveLabelTextColor,
    IImmutableBrush StateLayerColor);

internal sealed class DrawerGeometryRecipe : IRawValueRecipe<DrawerGeometryProfile>
{
    private readonly MaterialToken<double> _elevation;
    private readonly MaterialToken<double> _width;
    private readonly MaterialToken<ShapeRole> _containerShape;
    private readonly MaterialToken<double> _itemHeight;
    private readonly MaterialToken<ShapeRole> _itemShape;
    private readonly MaterialToken<double> _iconSize;
    private readonly MaterialToken<TypographyRole> _headline;
    private readonly MaterialToken<TypographyRole> _label;
    private readonly MaterialToken<double>? _opacity;
    private readonly bool _isModal;
    private readonly IReadOnlyList<MaterialToken> _dependencies;

    internal DrawerGeometryRecipe(
        MaterialToken<double> elevation,
        MaterialToken<double> width,
        MaterialToken<ShapeRole> containerShape,
        MaterialToken<double> itemHeight,
        MaterialToken<ShapeRole> itemShape,
        MaterialToken<double> iconSize,
        MaterialToken<TypographyRole> headline,
        MaterialToken<TypographyRole> label,
        MaterialToken<double>? opacity,
        bool isModal)
    {
        _elevation = elevation;
        _width = width;
        _containerShape = containerShape;
        _itemHeight = itemHeight;
        _itemShape = itemShape;
        _iconSize = iconSize;
        _headline = headline;
        _label = label;
        _opacity = opacity;
        _isModal = isModal;
        var dependencies = new List<MaterialToken>
        {
            elevation, width, containerShape, itemHeight, itemShape, iconSize, headline, label,
        };
        if (opacity is not null)
            dependencies.Add(opacity);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public DrawerGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_elevation),
        context.Get(_width),
        context.Get(_containerShape),
        context.Get(_itemHeight),
        context.Get(_itemShape),
        context.Get(_iconSize),
        _opacity is null ? 0 : context.Get(_opacity),
        context.Get(_headline),
        context.Get(_label),
        _isModal);
}

internal sealed class DrawerVisualColorsRecipe : IRawValueRecipe<DrawerVisualColors>
{
    private readonly MaterialToken<Color>[] _tokens;
    private readonly IReadOnlyList<MaterialToken> _dependencies;

    internal DrawerVisualColorsRecipe(params MaterialToken<Color>[] tokens)
    {
        if (tokens.Length != 9)
            throw new ArgumentException("A drawer colour profile requires nine tokens.", nameof(tokens));
        _tokens = tokens;
        _dependencies = Array.AsReadOnly<MaterialToken>(tokens);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public DrawerVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _tokens[0]),
        Brush(context, _tokens[1]),
        Brush(context, _tokens[2]),
        Brush(context, _tokens[3]),
        Brush(context, _tokens[4]),
        Brush(context, _tokens[5]),
        Brush(context, _tokens[6]),
        Brush(context, _tokens[7]),
        Brush(context, _tokens[8]));

    private static IImmutableBrush Brush(MaterialValueContext context, MaterialToken<Color> token) =>
        new ImmutableSolidColorBrush(context.Get(token));
}

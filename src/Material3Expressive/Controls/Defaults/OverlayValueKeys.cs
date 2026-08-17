using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>Stable Material Value endpoints shared by snackbar and tooltip overlays.</summary>
public static class OverlayValueKeys
{
    public static MaterialValueKey<SnackbarGeometryProfile> SnackbarGeometry { get; } =
        new(
            "Value.Overlay.Snackbar.Geometry",
            new RawRecipePlan<SnackbarGeometryProfile>(new SnackbarGeometryRecipe()));

    public static MaterialValueKey<SnackbarVisualProfile> SnackbarVisuals { get; } =
        new(
            "Value.Overlay.Snackbar.Visuals",
            new RawRecipePlan<SnackbarVisualProfile>(new SnackbarVisualRecipe()));

    public static MaterialValueKey<SpringSpec> SnackbarMotion { get; } =
        MaterialValue.FromRaw(
            "Value.Overlay.Snackbar.Motion",
            FoundationThemeRawTokens.Motion.DefaultSpatial);

    public static MaterialValueKey<TooltipGeometryProfile> PlainTooltipGeometry { get; } =
        new(
            "Value.Overlay.Tooltip.Plain.Geometry",
            new RawRecipePlan<TooltipGeometryProfile>(
                new TooltipGeometryRecipe(
                    PlainTooltipTokensKeys.ContainerShape,
                    PlainTooltipTokensKeys.SupportingTextFont,
                    titleText: null,
                    fixedTitleText: TypographyRole.TitleSmall,
                    elevation: null,
                    minHeight: 24,
                    maxWidth: 200,
                    horizontalSpace: 8,
                    verticalSpace: 4)));

    public static MaterialValueKey<TooltipGeometryProfile> RichTooltipGeometry { get; } =
        new(
            "Value.Overlay.Tooltip.Rich.Geometry",
            new RawRecipePlan<TooltipGeometryProfile>(
                new TooltipGeometryRecipe(
                    RichTooltipTokensKeys.ContainerShape,
                    RichTooltipTokensKeys.SupportingTextFont,
                    titleText: RichTooltipTokensKeys.SubheadFont,
                    fixedTitleText: default,
                    elevation: RichTooltipTokensKeys.ContainerElevation,
                    minHeight: 40,
                    maxWidth: 320,
                    horizontalSpace: 16,
                    verticalSpace: 12)));

    public static MaterialValueKey<TooltipVisualProfile> PlainTooltipVisuals { get; } =
        new(
            "Value.Overlay.Tooltip.Plain.Visuals",
            new RawRecipePlan<TooltipVisualProfile>(
                new TooltipVisualRecipe(
                    PlainTooltipTokensKeys.ContainerColor,
                    PlainTooltipTokensKeys.SupportingTextColor,
                    SystemColorTokens.InverseOnSurface,
                    SystemColorTokens.InversePrimary)));

    public static MaterialValueKey<TooltipVisualProfile> RichTooltipVisuals { get; } =
        new(
            "Value.Overlay.Tooltip.Rich.Visuals",
            new RawRecipePlan<TooltipVisualProfile>(
                new TooltipVisualRecipe(
                    RichTooltipTokensKeys.ContainerColor,
                    RichTooltipTokensKeys.SupportingTextColor,
                    RichTooltipTokensKeys.SubheadColor,
                    RichTooltipTokensKeys.ActionLabelTextColor)));
}

public sealed record SnackbarGeometryProfile(
    double ContainerElevation,
    RoundedCornerShape ContainerShape,
    double SingleLineHeight,
    double TwoLineHeight,
    double MinWidth,
    double MaxWidth,
    Thickness ContentPadding,
    double IconSize,
    TypographyRole SupportingTextStyle,
    TypographyRole ActionTextStyle);

public sealed record SnackbarVisualProfile(
    IImmutableBrush ContainerColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush ActionLabelTextColor,
    IImmutableBrush IconColor,
    Color ShadowColor);

public sealed record TooltipGeometryProfile(
    RoundedCornerShape ContainerShape,
    double ContainerElevation,
    double MinHeight,
    double MaxWidth,
    Thickness ContentPadding,
    TypographyRole SupportingTextStyle,
    TypographyRole TitleTextStyle);

public sealed record TooltipVisualProfile(
    IImmutableBrush ContainerColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush TitleColor,
    IImmutableBrush ActionLabelTextColor,
    Color ShadowColor);

internal sealed class SnackbarGeometryRecipe : IRawValueRecipe<SnackbarGeometryProfile>
{
    private static readonly IReadOnlyList<MaterialToken> Tokens = CreateDependencies();

    public IReadOnlyList<MaterialToken> Dependencies => Tokens;

    public SnackbarGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(SnackbarTokensKeys.ContainerElevation),
        ShapeRawProjection.Resolve(context.Get(SnackbarTokensKeys.ContainerShape), context),
        context.Get(SnackbarTokensKeys.SingleLineContainerHeight),
        context.Get(SnackbarTokensKeys.TwoLinesContainerHeight),
        344,
        600,
        new Thickness(16, 0, 8, 0),
        context.Get(SnackbarTokensKeys.IconSize),
        context.Get(SnackbarTokensKeys.SupportingTextFont),
        context.Get(SnackbarTokensKeys.ActionLabelTextFont));

    private static IReadOnlyList<MaterialToken> CreateDependencies()
    {
        var dependencies = new List<MaterialToken>
        {
            SnackbarTokensKeys.ContainerElevation,
            SnackbarTokensKeys.ContainerShape,
            SnackbarTokensKeys.SingleLineContainerHeight,
            SnackbarTokensKeys.TwoLinesContainerHeight,
            SnackbarTokensKeys.IconSize,
            SnackbarTokensKeys.SupportingTextFont,
            SnackbarTokensKeys.ActionLabelTextFont,
        };
        dependencies.AddRange(FoundationThemeRawTokens.Shape.All);
        return Array.AsReadOnly(dependencies.ToArray());
    }
}

internal sealed class SnackbarVisualRecipe : IRawValueRecipe<SnackbarVisualProfile>
{
    private static readonly IReadOnlyList<MaterialToken> Tokens =
        Array.AsReadOnly<MaterialToken>(
        [
            SnackbarTokensKeys.ContainerColor,
            SnackbarTokensKeys.SupportingTextColor,
            SnackbarTokensKeys.ActionLabelTextColor,
            SnackbarTokensKeys.IconColor,
            SystemColorTokens.Shadow,
        ]);

    public IReadOnlyList<MaterialToken> Dependencies => Tokens;

    public SnackbarVisualProfile Evaluate(MaterialValueContext context) => new(
        Brush(context.Get(SnackbarTokensKeys.ContainerColor)),
        Brush(context.Get(SnackbarTokensKeys.SupportingTextColor)),
        Brush(context.Get(SnackbarTokensKeys.ActionLabelTextColor)),
        Brush(context.Get(SnackbarTokensKeys.IconColor)),
        context.Get(SystemColorTokens.Shadow));

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}

internal sealed class TooltipGeometryRecipe : IRawValueRecipe<TooltipGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<ShapeRole> _shape;
    private readonly MaterialToken<TypographyRole> _supportingText;
    private readonly MaterialToken<TypographyRole>? _titleText;
    private readonly TypographyRole _fixedTitleText;
    private readonly MaterialToken<double>? _elevation;
    private readonly double _minHeight;
    private readonly double _maxWidth;
    private readonly Thickness _padding;

    internal TooltipGeometryRecipe(
        MaterialToken<ShapeRole> shape,
        MaterialToken<TypographyRole> supportingText,
        MaterialToken<TypographyRole>? titleText,
        TypographyRole fixedTitleText,
        MaterialToken<double>? elevation,
        double minHeight,
        double maxWidth,
        double horizontalSpace,
        double verticalSpace)
    {
        _shape = shape;
        _supportingText = supportingText;
        _titleText = titleText;
        _fixedTitleText = fixedTitleText;
        _elevation = elevation;
        _minHeight = minHeight;
        _maxWidth = maxWidth;
        _padding = new Thickness(horizontalSpace, verticalSpace);
        var dependencies = new List<MaterialToken> { shape, supportingText };
        if (titleText is not null)
            dependencies.Add(titleText);
        if (elevation is not null)
            dependencies.Add(elevation);
        dependencies.AddRange(FoundationThemeRawTokens.Shape.All);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TooltipGeometryProfile Evaluate(MaterialValueContext context) => new(
        ShapeRawProjection.Resolve(context.Get(_shape), context),
        _elevation is null ? 0 : context.Get(_elevation),
        _minHeight,
        _maxWidth,
        _padding,
        context.Get(_supportingText),
        _titleText is null ? _fixedTitleText : context.Get(_titleText));
}

internal sealed class TooltipVisualRecipe : IRawValueRecipe<TooltipVisualProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _container;
    private readonly MaterialToken<Color> _supporting;
    private readonly MaterialToken<Color> _title;
    private readonly MaterialToken<Color> _action;

    internal TooltipVisualRecipe(
        MaterialToken<Color> container,
        MaterialToken<Color> supporting,
        MaterialToken<Color> title,
        MaterialToken<Color> action)
    {
        _container = container;
        _supporting = supporting;
        _title = title;
        _action = action;
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [container, supporting, title, action, SystemColorTokens.Shadow]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TooltipVisualProfile Evaluate(MaterialValueContext context) => new(
        Brush(context.Get(_container)),
        Brush(context.Get(_supporting)),
        Brush(context.Get(_title)),
        Brush(context.Get(_action)),
        context.Get(SystemColorTokens.Shadow));

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}

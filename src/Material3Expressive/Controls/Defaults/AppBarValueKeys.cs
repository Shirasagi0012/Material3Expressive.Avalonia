using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready values used by <see cref="MaterialAppBar"/>.
/// </summary>
/// <remarks>
/// Size, variant selection, collapse interpolation and scroll state remain control policy. These
/// endpoints only project the Raw facts that the template and renderer actually consume.
/// </remarks>
public static class AppBarValueKeys
{
    /// <summary>Gets the small top-app-bar geometry profile.</summary>
    public static MaterialValueKey<AppBarGeometryProfile> SmallGeometry { get; } =
        new(
            "Value.AppBar.Small.Geometry",
            new RawRecipePlan<AppBarGeometryProfile>(
                new AppBarGeometryProfileRecipe(
                    AppBarSmallTokensKeys.ContainerHeight,
                    AppBarSmallTokensKeys.ContainerHeight,
                    AppBarSmallTokensKeys.ContainerHeight,
                    collapsedHeight: null)));

    /// <summary>Gets the medium top-app-bar geometry profile.</summary>
    public static MaterialValueKey<AppBarGeometryProfile> MediumGeometry { get; } =
        new(
            "Value.AppBar.Medium.Geometry",
            new RawRecipePlan<AppBarGeometryProfile>(
                new AppBarGeometryProfileRecipe(
                    AppBarMediumTokensKeys.ContainerHeight,
                    AppBarMediumFlexibleTokensKeys.ContainerHeight,
                    AppBarMediumFlexibleTokensKeys.LargeContainerHeight,
                    AppBarSmallTokensKeys.ContainerHeight)));

    /// <summary>Gets the large top-app-bar geometry profile.</summary>
    public static MaterialValueKey<AppBarGeometryProfile> LargeGeometry { get; } =
        new(
            "Value.AppBar.Large.Geometry",
            new RawRecipePlan<AppBarGeometryProfile>(
                new AppBarGeometryProfileRecipe(
                    AppBarLargeTokensKeys.ContainerHeight,
                    AppBarLargeFlexibleTokensKeys.ContainerHeight,
                    AppBarLargeFlexibleTokensKeys.LargeContainerHeight,
                    AppBarSmallTokensKeys.ContainerHeight)));

    /// <summary>Gets the theme-variant-aware visual colour profile.</summary>
    public static MaterialValueKey<AppBarVisualColors> VisualColors { get; } =
        new(
            "Value.AppBar.VisualColors",
            new RawRecipePlan<AppBarVisualColors>(
                new AppBarVisualColorsRecipe(
                    AppBarTokensKeys.ContainerColor,
                    AppBarTokensKeys.OnScrollContainerColor,
                    AppBarTokensKeys.TitleColor,
                    AppBarTokensKeys.SubtitleColor,
                    AppBarTokensKeys.TrailingIconColor)));

    /// <summary>Gets the small top-app-bar typography profile.</summary>
    public static MaterialValueKey<AppBarTypographyProfile> SmallTypography { get; } =
        Typography(
            "Value.AppBar.Small.Typography",
            AppBarSmallTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont,
            AppBarSmallTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont);

    /// <summary>Gets the medium top-app-bar typography profile.</summary>
    public static MaterialValueKey<AppBarTypographyProfile> MediumTypography { get; } =
        Typography(
            "Value.AppBar.Medium.Typography",
            AppBarMediumTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont,
            AppBarMediumFlexibleTokensKeys.TitleFont,
            AppBarMediumFlexibleTokensKeys.SubtitleFont);

    /// <summary>Gets the large top-app-bar typography profile.</summary>
    public static MaterialValueKey<AppBarTypographyProfile> LargeTypography { get; } =
        Typography(
            "Value.AppBar.Large.Typography",
            AppBarLargeTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont,
            AppBarLargeFlexibleTokensKeys.TitleFont,
            AppBarLargeFlexibleTokensKeys.SubtitleFont);

    private static MaterialValueKey<AppBarTypographyProfile> Typography(
        string path,
        MaterialToken<TypographyRole> standardTitle,
        MaterialToken<TypographyRole> standardSubtitle,
        MaterialToken<TypographyRole> flexibleTitle,
        MaterialToken<TypographyRole> flexibleSubtitle) =>
        new(
            path,
            new RawRecipePlan<AppBarTypographyProfile>(
                new AppBarTypographyProfileRecipe(
                    standardTitle,
                    standardSubtitle,
                    flexibleTitle,
                    flexibleSubtitle)));
}

/// <summary>Size-dependent geometry consumed by the app-bar layout and collapse policy.</summary>
public sealed record AppBarGeometryProfile(
    double ContainerHeight,
    double FlexibleContainerHeight,
    double FlexibleContainerHeightWithSubtitle,
    double CollapsedHeight,
    double HorizontalSpace);

/// <summary>Size-dependent typography roles consumed by app-bar presenters.</summary>
public sealed record AppBarTypographyProfile(
    TypographyRole StandardTitleTextStyle,
    TypographyRole StandardSubtitleTextStyle,
    TypographyRole FlexibleTitleTextStyle,
    TypographyRole FlexibleSubtitleTextStyle);

/// <summary>Theme-variant-aware brushes consumed by the app-bar renderer and template.</summary>
public sealed record AppBarVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ScrolledContainerColor,
    IImmutableBrush TitleColor,
    IImmutableBrush SubtitleColor,
    IImmutableBrush IconColor);

internal sealed class AppBarGeometryProfileRecipe : IRawValueRecipe<AppBarGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<double> _flexibleContainerHeight;
    private readonly MaterialToken<double> _flexibleContainerHeightWithSubtitle;
    private readonly MaterialToken<double>? _collapsedHeight;

    internal AppBarGeometryProfileRecipe(
        MaterialToken<double> containerHeight,
        MaterialToken<double> flexibleContainerHeight,
        MaterialToken<double> flexibleContainerHeightWithSubtitle,
        MaterialToken<double>? collapsedHeight)
    {
        _containerHeight = containerHeight ?? throw new ArgumentNullException(nameof(containerHeight));
        _flexibleContainerHeight = flexibleContainerHeight ??
            throw new ArgumentNullException(nameof(flexibleContainerHeight));
        _flexibleContainerHeightWithSubtitle = flexibleContainerHeightWithSubtitle ??
            throw new ArgumentNullException(nameof(flexibleContainerHeightWithSubtitle));
        _collapsedHeight = collapsedHeight;

        var dependencies = new List<MaterialToken>
        {
            containerHeight,
            flexibleContainerHeight,
            flexibleContainerHeightWithSubtitle,
        };
        if (collapsedHeight is not null)
            dependencies.Add(collapsedHeight);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public AppBarGeometryProfile Evaluate(MaterialValueContext context)
    {
        var containerHeight = context.Get(_containerHeight);
        return new(
            containerHeight,
            context.Get(_flexibleContainerHeight),
            context.Get(_flexibleContainerHeightWithSubtitle),
            _collapsedHeight is null ? containerHeight : context.Get(_collapsedHeight),
            16);
    }
}

internal sealed class AppBarTypographyProfileRecipe : IRawValueRecipe<AppBarTypographyProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<TypographyRole> _standardTitle;
    private readonly MaterialToken<TypographyRole> _standardSubtitle;
    private readonly MaterialToken<TypographyRole> _flexibleTitle;
    private readonly MaterialToken<TypographyRole> _flexibleSubtitle;

    internal AppBarTypographyProfileRecipe(
        MaterialToken<TypographyRole> standardTitle,
        MaterialToken<TypographyRole> standardSubtitle,
        MaterialToken<TypographyRole> flexibleTitle,
        MaterialToken<TypographyRole> flexibleSubtitle)
    {
        _standardTitle = standardTitle ?? throw new ArgumentNullException(nameof(standardTitle));
        _standardSubtitle = standardSubtitle ?? throw new ArgumentNullException(nameof(standardSubtitle));
        _flexibleTitle = flexibleTitle ?? throw new ArgumentNullException(nameof(flexibleTitle));
        _flexibleSubtitle = flexibleSubtitle ?? throw new ArgumentNullException(nameof(flexibleSubtitle));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [standardTitle, standardSubtitle, flexibleTitle, flexibleSubtitle]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public AppBarTypographyProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_standardTitle),
        context.Get(_standardSubtitle),
        context.Get(_flexibleTitle),
        context.Get(_flexibleSubtitle));
}

internal sealed class AppBarVisualColorsRecipe : IRawValueRecipe<AppBarVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _containerColor;
    private readonly MaterialToken<Color> _scrolledContainerColor;
    private readonly MaterialToken<Color> _titleColor;
    private readonly MaterialToken<Color> _subtitleColor;
    private readonly MaterialToken<Color> _iconColor;

    internal AppBarVisualColorsRecipe(
        MaterialToken<Color> containerColor,
        MaterialToken<Color> scrolledContainerColor,
        MaterialToken<Color> titleColor,
        MaterialToken<Color> subtitleColor,
        MaterialToken<Color> iconColor)
    {
        _containerColor = containerColor ?? throw new ArgumentNullException(nameof(containerColor));
        _scrolledContainerColor = scrolledContainerColor ??
            throw new ArgumentNullException(nameof(scrolledContainerColor));
        _titleColor = titleColor ?? throw new ArgumentNullException(nameof(titleColor));
        _subtitleColor = subtitleColor ?? throw new ArgumentNullException(nameof(subtitleColor));
        _iconColor = iconColor ?? throw new ArgumentNullException(nameof(iconColor));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerColor, scrolledContainerColor, titleColor, subtitleColor, iconColor]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public AppBarVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _containerColor),
        Brush(context, _scrolledContainerColor),
        Brush(context, _titleColor),
        Brush(context, _subtitleColor),
        Brush(context, _iconColor));

    private static IImmutableBrush Brush(MaterialValueContext context, MaterialToken<Color> token) =>
        new ImmutableSolidColorBrush(context.Get(token));
}

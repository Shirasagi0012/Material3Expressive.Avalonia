using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Theme-resolved colours published by the icon-button defaults adapter.</summary>
/// <remarks>
/// This is a library-owned fallback profile, not a generated token override bag. It is public
/// only because compiled library AXAML observes the attached-property contract. Applications
/// customise ordinary visual properties such as <see cref="Avalonia.Controls.Primitives.TemplatedControl.Background" />
/// through styles and local values.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record IconButtonColors(
    IImmutableBrush Container,
    IImmutableBrush Icon,
    IImmutableBrush Outline,
    IImmutableBrush StateLayer,
    IImmutableBrush DisabledContainer,
    IImmutableBrush DisabledIcon,
    IImmutableBrush DisabledOutline,
    bool HasOutline);

/// <summary>Size-dependent metrics published by an icon-button defaults adapter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record IconButtonMetrics(
    double ContainerHeight,
    double IconSize,
    Thickness ContentPadding,
    double ContainerWidth,
    double OutlineWidth);

/// <summary>Geometry roles consumed by an icon-button container presenter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record IconButtonShapes(
    ShapeRole Container,
    ShapeRole Pressed,
    ShapeRole Square,
    ShapeRole Selected,
    ShapeRole SelectedSquare);

/// <summary>
/// Component-owned policy for ordinary Material icon buttons.
/// </summary>
/// <remarks>
/// The generated raw tables are immutable upstream facts. This adapter boundary maps the variant
/// and size axes to those facts, resolves system roles, and applies the small amount of
/// Compose/Avalonia shaping needed by the native ControlTheme. It deliberately does not own a
/// control, subscribe to a theme, or write Avalonia properties; <see cref="IconButtonDefaultsAdapter"/>
/// owns that lifecycle.
/// </remarks>
internal static class IconButtonDefaults
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Avalonia.Media.Colors.Transparent);

    /// <summary>Resolves enabled and disabled colours for one icon-button variant.</summary>
    public static IconButtonColors Colors(ResolvedTheme theme, IconButtonVariant variant, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return variant switch
        {
            IconButtonVariant.Standard => new(
                Transparent,
                Brush(IconButtonTokens.Color, theme, isDark),
                Transparent,
                Brush(IconButtonTokens.Color, theme, isDark),
                Transparent,
                Brush(IconButtonTokens.DisabledColor, theme, isDark, IconButtonTokens.DisabledOpacity),
                Transparent,
                HasOutline: false),
            IconButtonVariant.Filled => new(
                Brush(FilledIconButtonTokens.ContainerColor, theme, isDark),
                Brush(FilledIconButtonTokens.Color, theme, isDark),
                Transparent,
                Brush(FilledIconButtonTokens.Color, theme, isDark),
                Brush(FilledIconButtonTokens.DisabledContainerColor, theme, isDark, FilledIconButtonTokens.DisabledContainerOpacity),
                Brush(FilledIconButtonTokens.DisabledColor, theme, isDark, FilledIconButtonTokens.DisabledOpacity),
                Transparent,
                HasOutline: false),
            IconButtonVariant.Tonal => new(
                Brush(FilledTonalIconButtonTokens.ContainerColor, theme, isDark),
                Brush(FilledTonalIconButtonTokens.Color, theme, isDark),
                Transparent,
                Brush(FilledTonalIconButtonTokens.Color, theme, isDark),
                Brush(FilledTonalIconButtonTokens.DisabledContainerColor, theme, isDark, FilledTonalIconButtonTokens.DisabledContainerOpacity),
                Brush(FilledTonalIconButtonTokens.DisabledColor, theme, isDark, FilledTonalIconButtonTokens.DisabledOpacity),
                Transparent,
                HasOutline: false),
            IconButtonVariant.Outlined => new(
                Transparent,
                Brush(OutlinedIconButtonTokens.Color, theme, isDark),
                Brush(OutlinedIconButtonTokens.OutlineColor, theme, isDark),
                Brush(OutlinedIconButtonTokens.Color, theme, isDark),
                Transparent,
                Brush(OutlinedIconButtonTokens.DisabledColor, theme, isDark, OutlinedIconButtonTokens.DisabledOpacity),
                Brush(OutlinedIconButtonTokens.DisabledOutlineColor, theme, isDark),
                HasOutline: true),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    /// <summary>
    /// Resolves a size and width axis into values consumed by the ControlTheme and presenter.
    /// </summary>
    public static IconButtonMetrics Metrics(IconButtonSize size, IconButtonWidth width)
    {
        var raw = size switch
        {
            IconButtonSize.ExtraSmall => new RawMetrics(
                XSmallIconButtonTokens.ContainerHeight,
                XSmallIconButtonTokens.IconSize,
                XSmallIconButtonTokens.NarrowLeadingSpace,
                XSmallIconButtonTokens.DefaultLeadingSpace,
                XSmallIconButtonTokens.WideLeadingSpace,
                XSmallIconButtonTokens.OutlinedOutlineWidth),
            IconButtonSize.Small => new RawMetrics(
                SmallIconButtonTokens.ContainerHeight,
                SmallIconButtonTokens.IconSize,
                SmallIconButtonTokens.NarrowLeadingSpace,
                SmallIconButtonTokens.DefaultLeadingSpace,
                SmallIconButtonTokens.WideLeadingSpace,
                SmallIconButtonTokens.OutlinedOutlineWidth),
            IconButtonSize.Medium => new RawMetrics(
                MediumIconButtonTokens.ContainerHeight,
                MediumIconButtonTokens.IconSize,
                MediumIconButtonTokens.NarrowLeadingSpace,
                MediumIconButtonTokens.DefaultLeadingSpace,
                MediumIconButtonTokens.WideLeadingSpace,
                MediumIconButtonTokens.OutlinedOutlineWidth),
            IconButtonSize.Large => new RawMetrics(
                LargeIconButtonTokens.ContainerHeight,
                LargeIconButtonTokens.IconSize,
                LargeIconButtonTokens.NarrowLeadingSpace,
                LargeIconButtonTokens.DefaultLeadingSpace,
                LargeIconButtonTokens.WideLeadingSpace,
                LargeIconButtonTokens.OutlinedOutlineWidth),
            IconButtonSize.ExtraLarge => new RawMetrics(
                XLargeIconButtonTokens.ContainerHeight,
                XLargeIconButtonTokens.IconSize,
                XLargeIconButtonTokens.NarrowLeadingSpace,
                XLargeIconButtonTokens.DefaultLeadingSpace,
                XLargeIconButtonTokens.WideLeadingSpace,
                XLargeIconButtonTokens.OutlinedOutlineWidth),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };

        var space = Space(raw, width);
        return new IconButtonMetrics(
            raw.ContainerHeight,
            raw.IconSize,
            new Thickness(space, 0),
            raw.IconSize + space * 2,
            raw.OutlineWidth);
    }

    /// <summary>Returns the five shape roles needed by the icon-button presenter.</summary>
    public static IconButtonShapes Shapes(IconButtonSize size)
    {
        return size switch
        {
            IconButtonSize.ExtraSmall => new(
                XSmallIconButtonTokens.ContainerShapeRound,
                XSmallIconButtonTokens.PressedContainerShape,
                XSmallIconButtonTokens.ContainerShapeSquare,
                XSmallIconButtonTokens.SelectedContainerShapeRound,
                XSmallIconButtonTokens.SelectedContainerShapeSquare),
            IconButtonSize.Small => new(
                SmallIconButtonTokens.ContainerShapeRound,
                SmallIconButtonTokens.PressedContainerShape,
                SmallIconButtonTokens.ContainerShapeSquare,
                SmallIconButtonTokens.SelectedContainerShapeRound,
                SmallIconButtonTokens.SelectedContainerShapeSquare),
            IconButtonSize.Medium => new(
                MediumIconButtonTokens.ContainerShapeRound,
                MediumIconButtonTokens.PressedContainerShape,
                MediumIconButtonTokens.ContainerShapeSquare,
                MediumIconButtonTokens.SelectedContainerShapeRound,
                MediumIconButtonTokens.SelectedContainerShapeSquare),
            IconButtonSize.Large => new(
                LargeIconButtonTokens.ContainerShapeRound,
                LargeIconButtonTokens.PressedContainerShape,
                LargeIconButtonTokens.ContainerShapeSquare,
                LargeIconButtonTokens.SelectedContainerShapeRound,
                LargeIconButtonTokens.SelectedContainerShapeSquare),
            IconButtonSize.ExtraLarge => new(
                XLargeIconButtonTokens.ContainerShapeRound,
                XLargeIconButtonTokens.PressedContainerShape,
                XLargeIconButtonTokens.ContainerShapeSquare,
                XLargeIconButtonTokens.SelectedContainerShapeRound,
                XLargeIconButtonTokens.SelectedContainerShapeSquare),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    /// <summary>Returns the motion profile for an ordinary icon-button press morph.</summary>
    public static SpringSpec ShapeMotion(ResolvedTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return theme.Motion.Get(MotionRole.DefaultEffects);
    }

    public static Thickness OutlineThickness(
        IconButtonVariant variant,
        IconButtonSize size) =>
        new(variant == IconButtonVariant.Outlined ? Metrics(size, IconButtonWidth.Default).OutlineWidth : 0);

    private static double Space(RawMetrics metrics, IconButtonWidth width) => width switch
    {
        IconButtonWidth.Narrow => metrics.NarrowSpace,
        IconButtonWidth.Wide => metrics.WideSpace,
        _ => metrics.DefaultSpace
    };

    private static IImmutableBrush Brush(
        ColorTokenValue token,
        ResolvedTheme theme,
        bool isDark,
        double opacity = 1) => token.ResolveBrush(theme.ColorScheme, isDark, opacity);

    private readonly record struct RawMetrics(
        double ContainerHeight,
        double IconSize,
        double NarrowSpace,
        double DefaultSpace,
        double WideSpace,
        double OutlineWidth);
}

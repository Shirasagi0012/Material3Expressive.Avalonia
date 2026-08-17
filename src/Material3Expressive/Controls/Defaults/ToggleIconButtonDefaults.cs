using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Theme-resolved checked and unchecked colours for an icon toggle button.</summary>
/// <remarks>
/// The checked contract is intentionally separate from <see cref="IconButtonColors" />. A toggle
/// has independent selected and unselected colors, and outlined toggles remove their outline when
/// checked; collapsing those states into an ordinary icon-button profile loses that contract.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("Use IconButtonValueKeys and typed MaterialValue endpoints instead.", DiagnosticId = "M3E1004")]
public sealed record ToggleIconButtonColors(
    IImmutableBrush UncheckedContainer,
    IImmutableBrush UncheckedIcon,
    IImmutableBrush UncheckedOutline,
    IImmutableBrush CheckedContainer,
    IImmutableBrush CheckedIcon,
    IImmutableBrush CheckedOutline,
    IImmutableBrush DisabledContainer,
    IImmutableBrush DisabledIcon,
    IImmutableBrush DisabledOutline,
    IImmutableBrush UncheckedStateLayer,
    IImmutableBrush CheckedStateLayer,
    bool HasOutline)
{
    public IImmutableBrush Container(bool enabled, bool isChecked) =>
        !enabled ? DisabledContainer
        : isChecked ? CheckedContainer
        : UncheckedContainer;

    public IImmutableBrush Icon(bool enabled, bool isChecked) =>
        !enabled ? DisabledIcon
        : isChecked ? CheckedIcon
        : UncheckedIcon;

    public IImmutableBrush Outline(bool enabled, bool isChecked) =>
        !enabled ? DisabledOutline
        : isChecked ? CheckedOutline
        : UncheckedOutline;

    public IImmutableBrush StateLayer(bool isChecked) =>
        isChecked ? CheckedStateLayer : UncheckedStateLayer;
}

/// <summary>Component-owned policy for Material icon toggle buttons.</summary>
/// <remarks>
/// Toggle icon buttons keep a separate checked/unchecked colour contract. Their geometry reuses
/// the raw size facts and the ordinary icon-button metrics, while the checked shape morph uses the
/// expressive fast-spatial motion profile.
/// </remarks>
[Obsolete("Use IconButtonValueKeys and typed MaterialValue endpoints instead.", DiagnosticId = "M3E1004")]
internal static class ToggleIconButtonDefaults
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Avalonia.Media.Colors.Transparent);

    public static ToggleIconButtonColors Colors(
        ResolvedTheme theme,
        IconButtonVariant variant,
        bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return variant switch
        {
            IconButtonVariant.Standard => new(
                Transparent,
                Brush(IconButtonTokens.UnselectedColor, theme, isDark),
                Transparent,
                Transparent,
                Brush(IconButtonTokens.SelectedColor, theme, isDark),
                Transparent,
                Transparent,
                Brush(IconButtonTokens.DisabledColor, theme, isDark, IconButtonTokens.DisabledOpacity),
                Transparent,
                Brush(IconButtonTokens.UnselectedColor, theme, isDark),
                Brush(IconButtonTokens.SelectedColor, theme, isDark),
                HasOutline: false),
            IconButtonVariant.Filled => new(
                Brush(FilledIconButtonTokens.UnselectedContainerColor, theme, isDark),
                Brush(FilledIconButtonTokens.UnselectedColor, theme, isDark),
                Transparent,
                Brush(FilledIconButtonTokens.SelectedContainerColor, theme, isDark),
                Brush(FilledIconButtonTokens.SelectedColor, theme, isDark),
                Transparent,
                Brush(FilledIconButtonTokens.DisabledContainerColor, theme, isDark, FilledIconButtonTokens.DisabledContainerOpacity),
                Brush(FilledIconButtonTokens.DisabledColor, theme, isDark, FilledIconButtonTokens.DisabledOpacity),
                Transparent,
                Brush(FilledIconButtonTokens.UnselectedColor, theme, isDark),
                Brush(FilledIconButtonTokens.SelectedColor, theme, isDark),
                HasOutline: false),
            IconButtonVariant.Tonal => new(
                Brush(FilledTonalIconButtonTokens.UnselectedContainerColor, theme, isDark),
                Brush(FilledTonalIconButtonTokens.UnselectedColor, theme, isDark),
                Transparent,
                Brush(FilledTonalIconButtonTokens.SelectedContainerColor, theme, isDark),
                Brush(FilledTonalIconButtonTokens.SelectedColor, theme, isDark),
                Transparent,
                Brush(FilledTonalIconButtonTokens.DisabledContainerColor, theme, isDark, FilledTonalIconButtonTokens.DisabledContainerOpacity),
                Brush(FilledTonalIconButtonTokens.DisabledColor, theme, isDark, FilledTonalIconButtonTokens.DisabledOpacity),
                Transparent,
                Brush(FilledTonalIconButtonTokens.UnselectedColor, theme, isDark),
                Brush(FilledTonalIconButtonTokens.SelectedColor, theme, isDark),
                HasOutline: false),
            IconButtonVariant.Outlined => new(
                Transparent,
                Brush(OutlinedIconButtonTokens.UnselectedColor, theme, isDark),
                Brush(OutlinedIconButtonTokens.UnselectedOutlineColor, theme, isDark),
                Brush(OutlinedIconButtonTokens.SelectedContainerColor, theme, isDark),
                Brush(OutlinedIconButtonTokens.SelectedColor, theme, isDark),
                // AndroidX removes the outline while an outlined toggle is selected.
                Transparent,
                Transparent,
                Brush(OutlinedIconButtonTokens.DisabledColor, theme, isDark, OutlinedIconButtonTokens.DisabledOpacity),
                Brush(OutlinedIconButtonTokens.DisabledOutlineColor, theme, isDark),
                Brush(OutlinedIconButtonTokens.UnselectedColor, theme, isDark),
                Brush(OutlinedIconButtonTokens.SelectedColor, theme, isDark),
                HasOutline: true),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    public static IconButtonMetrics Metrics(IconButtonSize size, IconButtonWidth width) =>
        IconButtonDefaults.Metrics(size, width);

    public static IconButtonShapes Shapes(IconButtonSize size) =>
        IconButtonDefaults.Shapes(size);

    public static SpringSpec ShapeMotion(ResolvedTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return theme.Motion.Get(MotionRole.FastSpatial);
    }

    public static Thickness OutlineThickness(
        ToggleIconButtonColors colors,
        IconButtonSize size,
        bool isChecked) =>
        new(colors.HasOutline && !isChecked
            ? IconButtonDefaults.Metrics(size, IconButtonWidth.Default).OutlineWidth
            : 0);

    private static IImmutableBrush Brush(
        ColorTokenValue token,
        ResolvedTheme theme,
        bool isDark,
        double opacity = 1) => token.ResolveBrush(theme.ColorScheme, isDark, opacity);
}

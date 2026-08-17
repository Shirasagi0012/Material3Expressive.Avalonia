using System.ComponentModel;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// Compatibility-only shape retained for the pre-MaterialValue AppBar tests.
/// </summary>
[Obsolete(
    "Legacy AppBar metrics facade. Use AppBarValueKeys and the resolved AppBar profiles instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed record AppBarMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ScrolledContainerColor,
    ColorTokenValue TitleColor,
    ColorTokenValue SubtitleColor,
    ColorTokenValue IconColor,
    double ContainerHeight,
    double FlexibleContainerHeight,
    double FlexibleContainerHeightWithSubtitle,
    double HorizontalSpace,
    TypographyRole TitleTextStyle,
    TypographyRole SubtitleTextStyle,
    TypographyRole FlexibleTitleTextStyle,
    TypographyRole FlexibleSubtitleTextStyle,
    bool IsStacked);

/// <summary>
/// Compatibility-only bridge for source that still asserts the old AppBar geometry shape.
/// Runtime consumers use typed <see cref="AppBarValueKeys"/> endpoints exclusively.
/// </summary>
[Obsolete(
    "Legacy AppBar defaults facade. Use AppBarValueKeys instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class AppBarDefaults
{
    public static AppBarMetrics Metrics(AppBarSize size) => size switch
    {
        AppBarSize.Small => new(
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Surface),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.SurfaceContainer),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurface),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurfaceVariant),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurfaceVariant),
            64,
            64,
            64,
            16,
            TypographyRole.TitleLarge,
            TypographyRole.LabelMedium,
            TypographyRole.TitleLarge,
            TypographyRole.LabelMedium,
            false),
        AppBarSize.Medium => new(
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Surface),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.SurfaceContainer),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurface),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurfaceVariant),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurfaceVariant),
            112,
            112,
            136,
            16,
            TypographyRole.HeadlineSmall,
            TypographyRole.LabelMedium,
            TypographyRole.HeadlineMedium,
            TypographyRole.LabelLarge,
            true),
        AppBarSize.Large => new(
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Surface),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.SurfaceContainer),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurface),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurfaceVariant),
            ColorTokenValue.Role(MaterialColorUtilities.Avalonia.Tokens.SysColorToken.OnSurfaceVariant),
            152,
            120,
            152,
            16,
            TypographyRole.HeadlineMedium,
            TypographyRole.LabelMedium,
            TypographyRole.DisplaySmall,
            TypographyRole.TitleMedium,
            true),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };
}

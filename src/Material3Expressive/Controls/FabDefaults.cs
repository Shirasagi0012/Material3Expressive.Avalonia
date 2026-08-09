using System;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Theme-resolved colour roles consumed by a Material floating action button.</summary>
internal sealed record FabColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush IconColor,
    IImmutableBrush StateLayerColor);

/// <summary>The interaction elevation profile selected by a Material floating action button.</summary>
internal sealed record FabElevation(
    double Resting,
    double Hovered,
    double Focused,
    double Pressed,
    double Lowered)
{
    public double For(bool isLowered, InteractionState interaction)
    {
        if (isLowered)
            return Lowered;

        if (interaction.HasFlag(InteractionState.Pressed))
            return Pressed;

        if (interaction.HasFlag(InteractionState.Hovered))
            return Hovered;

        if (interaction.HasFlag(InteractionState.Focused))
            return Focused;

        return Resting;
    }
}

/// <summary>The size-dependent semantic metrics consumed by a Material floating action button.</summary>
internal sealed record FabMetrics(
    double ContainerHeight,
    double ContainerWidth,
    ShapeRole ContainerShape,
    ShapeRole PressedContainerShape,
    double IconSize,
    double LabelSpace,
    double IconLabelSpace,
    TypographyRole LabelTextStyle);

/// <summary>
/// Component-owned FAB policy: raw-token selection, system-role resolution and expressive layout
/// adaptations.
/// </summary>
/// <remarks>
/// The primary and secondary raw tables are direct AndroidX transcriptions. Tertiary and Surface
/// are Compose/spec defaults composed from system roles, while pressed shapes, extended padding
/// and text-style selection are control policy. None belong in a generated component snapshot.
/// </remarks>
internal static class FabDefaults
{
    /// <summary>Resolves the visual colour profile for one FAB variant.</summary>
    public static FabColors Colors(ResolvedTheme theme, FabVariant variant, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var scheme = theme.ColorScheme;
        return variant switch
        {
            FabVariant.Primary => new FabColors(
                FabPrimaryContainerTokens.ContainerColor.ResolveBrush(scheme, isDark),
                FabPrimaryContainerTokens.IconColor.ResolveBrush(scheme, isDark),
                FabPrimaryContainerTokens.IconColor.ResolveBrush(scheme, isDark)),
            FabVariant.Secondary => new FabColors(
                FabSecondaryContainerTokens.ContainerColor.ResolveBrush(scheme, isDark),
                FabSecondaryContainerTokens.IconColor.ResolveBrush(scheme, isDark),
                FabSecondaryContainerTokens.IconColor.ResolveBrush(scheme, isDark)),
            // AndroidX has no dedicated FabTertiaryContainerTokens source table. This policy is
            // the Compose/default-spec mapping formerly recorded in FabColors.default.jsonc.
            FabVariant.Tertiary => new FabColors(
                Brush(SysColorToken.TertiaryContainer, scheme, isDark),
                Brush(SysColorToken.OnTertiaryContainer, scheme, isDark),
                Brush(SysColorToken.OnTertiaryContainer, scheme, isDark)),
            FabVariant.Surface => new FabColors(
                Brush(SysColorToken.SurfaceContainerHigh, scheme, isDark),
                Brush(SysColorToken.Primary, scheme, isDark),
                Brush(SysColorToken.Primary, scheme, isDark)),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    /// <summary>Resolves the interaction elevation profile for one FAB variant.</summary>
    public static FabElevation Elevation(FabVariant variant)
    {
        return variant switch
        {
            FabVariant.Primary => Elevation(
                FabPrimaryContainerTokens.ContainerElevation,
                FabPrimaryContainerTokens.HoveredContainerElevation,
                FabPrimaryContainerTokens.FocusedContainerElevation,
                FabPrimaryContainerTokens.PressedContainerElevation,
                FabPrimaryContainerTokens.LoweredContainerElevation),
            FabVariant.Secondary => Elevation(
                FabSecondaryContainerTokens.ContainerElevation,
                FabSecondaryContainerTokens.HoveredContainerElevation,
                FabSecondaryContainerTokens.FocusedContainerElevation,
                FabSecondaryContainerTokens.PressedContainerElevation,
                FabSecondaryContainerTokens.LoweredContainerElevation),
            // Tertiary and Surface reuse the primary elevation treatment in Compose Defaults.
            FabVariant.Tertiary or FabVariant.Surface => Elevation(
                FabPrimaryContainerTokens.ContainerElevation,
                FabPrimaryContainerTokens.HoveredContainerElevation,
                FabPrimaryContainerTokens.FocusedContainerElevation,
                FabPrimaryContainerTokens.PressedContainerElevation,
                FabPrimaryContainerTokens.LoweredContainerElevation),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    /// <summary>Maps the size axis to the geometry and typography consumed by the FAB template.</summary>
    public static FabMetrics Metrics(FabSize size)
    {
        return size switch
        {
            FabSize.Small => new FabMetrics(
                FabBaselineTokens.ContainerHeight,
                FabBaselineTokens.ContainerWidth,
                FabBaselineTokens.ContainerShape,
                ShapeRole.CornerMedium,
                FabBaselineTokens.IconSize,
                LabelSpace: 16,
                IconLabelSpace: 12,
                TypographyRole.LabelLarge),
            FabSize.Medium => new FabMetrics(
                FabMediumTokens.ContainerHeight,
                FabMediumTokens.ContainerWidth,
                FabMediumTokens.ContainerShape,
                ShapeRole.CornerLarge,
                FabMediumTokens.IconSize,
                LabelSpace: 24,
                IconLabelSpace: 16,
                TypographyRole.TitleMedium),
            FabSize.Large => new FabMetrics(
                FabLargeTokens.ContainerHeight,
                FabLargeTokens.ContainerWidth,
                FabLargeTokens.ContainerShape,
                ShapeRole.CornerLarge,
                FabLargeTokens.IconSize,
                LabelSpace: 28,
                IconLabelSpace: 20,
                TypographyRole.HeadlineSmall),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    private static FabElevation Elevation(
        double resting,
        double hovered,
        double focused,
        double pressed,
        double lowered) => new(resting, hovered, focused, pressed, lowered);

    private static IImmutableBrush Brush(
        SysColorToken role,
        ResolvedColorScheme? scheme,
        bool isDark) => ColorTokenValue.Role(role).ResolveBrush(scheme, isDark);
}

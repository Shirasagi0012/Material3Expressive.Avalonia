using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>The interaction elevation profile selected by a Material card renderer.</summary>
internal sealed record CardElevation(
    double Resting,
    double Hovered,
    double Focused,
    double Pressed,
    double Dragged,
    double Disabled)
{
    public double For(bool enabled, InteractionState interaction)
    {
        if (!enabled)
            return Disabled;

        if (interaction.HasFlag(InteractionState.Dragged))
            return Dragged;

        if (interaction.HasFlag(InteractionState.Pressed))
            return Pressed;

        if (interaction.HasFlag(InteractionState.Hovered))
            return Hovered;

        if (interaction.HasFlag(InteractionState.Focused))
            return Focused;

        return Resting;
    }
}

/// <summary>The non-colour semantic defaults consumed by a Material card.</summary>
internal sealed record CardMetrics(
    ShapeRole ContainerShape,
    double OutlineWidth,
    double IconSize,
    bool HasOutline,
    CardElevation Elevation);

/// <summary>Theme-resolved colour roles consumed by a Material card.</summary>
internal sealed record CardColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush DisabledOutlineColor,
    IImmutableBrush IconColor,
    IImmutableBrush StateLayerColor);

/// <summary>
/// Component-owned policy that maps card axes to raw token facts and system-theme values.
/// </summary>
/// <remarks>
/// The raw card token tables remain a lossless upstream transcription.  The outlined-card
/// disabled-container exception and transparent outline policy are Compose defaults decisions,
/// so they live here instead of in a generated defaults JSON assembly table.
/// </remarks>
internal static class CardDefaults
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Avalonia.Media.Colors.Transparent);

    /// <summary>Resolves the metrics and interaction-elevation profile for one variant.</summary>
    public static CardMetrics Metrics(CardVariant variant)
    {
        return variant switch
        {
            CardVariant.Elevated => Metrics(
                ElevatedCardTokens.ContainerShape,
                outlineWidth: 0,
                ElevatedCardTokens.IconSize,
                hasOutline: false,
                Elevation(
                    ElevatedCardTokens.ContainerElevation,
                    ElevatedCardTokens.HoverContainerElevation,
                    ElevatedCardTokens.FocusContainerElevation,
                    ElevatedCardTokens.PressedContainerElevation,
                    ElevatedCardTokens.DraggedContainerElevation,
                    ElevatedCardTokens.DisabledContainerElevation)),
            CardVariant.Filled => Metrics(
                FilledCardTokens.ContainerShape,
                outlineWidth: 0,
                FilledCardTokens.IconSize,
                hasOutline: false,
                Elevation(
                    FilledCardTokens.ContainerElevation,
                    FilledCardTokens.HoverContainerElevation,
                    FilledCardTokens.FocusContainerElevation,
                    FilledCardTokens.PressedContainerElevation,
                    FilledCardTokens.DraggedContainerElevation,
                    FilledCardTokens.DisabledContainerElevation)),
            CardVariant.Outlined => Metrics(
                OutlinedCardTokens.ContainerShape,
                OutlinedCardTokens.OutlineWidth,
                OutlinedCardTokens.IconSize,
                hasOutline: true,
                Elevation(
                    OutlinedCardTokens.ContainerElevation,
                    OutlinedCardTokens.HoverContainerElevation,
                    OutlinedCardTokens.FocusContainerElevation,
                    OutlinedCardTokens.PressedContainerElevation,
                    OutlinedCardTokens.DraggedContainerElevation,
                    OutlinedCardTokens.DisabledContainerElevation)),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    /// <summary>Resolves one card variant's raw roles against a system theme snapshot.</summary>
    public static CardColors Colors(ResolvedTheme theme, CardVariant variant, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var scheme = theme.ColorScheme;

        return variant switch
        {
            CardVariant.Elevated => new CardColors(
                ElevatedCardTokens.ContainerColor.ResolveBrush(scheme, isDark),
                ElevatedCardTokens.DisabledContainerColor.ResolveBrush(
                    scheme, isDark, ElevatedCardTokens.DisabledContainerOpacity),
                Transparent,
                Transparent,
                ElevatedCardTokens.IconColor.ResolveBrush(scheme, isDark),
                ColorTokenValue.Role(SysColorToken.OnSurface).ResolveBrush(scheme, isDark)),
            CardVariant.Filled => new CardColors(
                FilledCardTokens.ContainerColor.ResolveBrush(scheme, isDark),
                FilledCardTokens.DisabledContainerColor.ResolveBrush(
                    scheme, isDark, FilledCardTokens.DisabledContainerOpacity),
                Transparent,
                Transparent,
                FilledCardTokens.IconColor.ResolveBrush(scheme, isDark),
                ColorTokenValue.Role(SysColorToken.OnSurface).ResolveBrush(scheme, isDark)),
            CardVariant.Outlined => new CardColors(
                OutlinedCardTokens.ContainerColor.ResolveBrush(scheme, isDark),
                // Compose keeps an outlined card's surface unchanged while disabled; only its
                // border dims.
                OutlinedCardTokens.ContainerColor.ResolveBrush(scheme, isDark),
                OutlinedCardTokens.OutlineColor.ResolveBrush(scheme, isDark),
                OutlinedCardTokens.DisabledOutlineColor.ResolveBrush(
                    scheme, isDark, OutlinedCardTokens.DisabledOutlineOpacity),
                OutlinedCardTokens.IconColor.ResolveBrush(scheme, isDark),
                ColorTokenValue.Role(SysColorToken.OnSurface).ResolveBrush(scheme, isDark)),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    private static CardMetrics Metrics(
        ShapeRole containerShape,
        double outlineWidth,
        double iconSize,
        bool hasOutline,
        CardElevation elevation) => new(containerShape, outlineWidth, iconSize, hasOutline, elevation);

    private static CardElevation Elevation(
        double resting,
        double hovered,
        double focused,
        double pressed,
        double dragged,
        double disabled) => new(resting, hovered, focused, pressed, dragged, disabled);
}

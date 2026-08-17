using System;
using Avalonia.Media;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation;

#pragma warning disable CS0618 // This file defines the legacy snapshot; see docs/refactor/migration-plan.md M0/M9.

/// <summary>
/// The immutable system form of a <see cref="MaterialTheme" /> that travels down the tree.
/// </summary>
/// <remarks>
/// A snapshot is rebuilt whenever the authoring theme changes, so it never needs invalidating —
/// consumers can compare references to decide whether anything can possibly have changed. Values
/// that depend on the consumer's theme variant cannot be resolved here; component-owned Defaults
/// resolve those values at the point of consumption.
/// </remarks>
[Obsolete(
    "Legacy whole-theme snapshot transport. Consume typed MaterialValueKey<T> resources through " +
    "a MaterialTheme placement; this compatibility type is removed after component migration. " +
    "See docs/refactor/migration-plan.md.",
    DiagnosticId = "M3E1004")]
public sealed class ResolvedTheme
{
    // This snapshot exists only for controls that still consume the inherited legacy theme. It is
    // intentionally not a MaterialTheme child/provider and therefore cannot mask an ancestor Raw
    // colour context in the new placement runtime.
    private static readonly ResolvedColorScheme LegacyBaselineColorScheme =
        new(new TonalSpotScheme(Color.FromRgb(0x67, 0x50, 0xA4)));

    public ResolvedTheme(MaterialTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ColorScheme = theme.ColorScheme is { } colorScheme
            ? new ResolvedColorScheme(colorScheme)
            : LegacyBaselineColorScheme;
        Shapes = new ResolvedShapeScheme(theme.ShapeScheme);
        Motion = new ResolvedMotionScheme(theme.MotionScheme);
        Ripple = new ResolvedRippleThemeConfiguration(theme.RippleTheme);

        Typography = new ResolvedTypographyScheme(theme.TypographyScheme, role =>
        {
            var scale = TypeScale.For(role);

            foreach (var overrides in theme.TypographyScheme.TypeScaleOverrides)
            {
                if (overrides.Variant == role)
                    scale = overrides.ApplyTo(scale);
            }

            return scale;
        });
    }

    /// <summary>The theme every element falls back to when no theme has been declared above it.</summary>
    public static ResolvedTheme Default { get; } = new(new MaterialTheme());

    public ResolvedColorScheme? ColorScheme { get; }

    public ResolvedTypographyScheme Typography { get; }

    public ResolvedShapeScheme Shapes { get; }

    public ResolvedMotionScheme Motion { get; }

    public ResolvedRippleThemeConfiguration Ripple { get; }
}

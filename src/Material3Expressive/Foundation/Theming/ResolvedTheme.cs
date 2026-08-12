using System;
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
    "Legacy theme snapshot transported via the MaterialThemeScope attached property. Token " +
    "consumers must resolve MaterialToken values through TokenReader/MaterialTokenResolver " +
    "instead; this type becomes the internal MaterialThemeSnapshot when the compat path is " +
    "removed. See docs/refactor/migration-plan.md.")]
public sealed class ResolvedTheme
{
    public ResolvedTheme(MaterialTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ColorScheme = theme.ColorScheme is { } colorScheme ? new ResolvedColorScheme(colorScheme) : null;
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

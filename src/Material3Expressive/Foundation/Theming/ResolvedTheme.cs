using System;
using System.Linq;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>
/// The immutable form of a <see cref="MaterialTheme" />: the system snapshot that travels down the
/// tree, plus legacy component projections for controls not yet migrated.
/// </summary>
/// <remarks>
/// A snapshot is rebuilt whenever the authoring theme changes, so it never needs invalidating —
/// consumers can compare references to decide whether anything can possibly have changed. Values
/// that depend on the consumer's theme variant cannot be resolved here; those are memoized per
/// variant instead, mirroring the per-theme-instance default bundles upstream keeps as ~60 fields
/// on <c>ColorScheme</c>.
/// <para>
/// The legacy component-token half of this class is generated from the remaining defaults tables
/// under <c>Tokens/Defaults/</c> (with raw values under <c>Tokens/</c>) — see
/// <c>ResolvedTheme.Components.g.cs</c>. Components on the new ownership model, such as ordinary
/// Button family controls read only the system schemes and map their raw tables in component code.
/// System schemes, including typography, resolve through their owning scheme.
/// </para>
/// </remarks>
public sealed partial class ResolvedTheme
{
    public ResolvedTheme(MaterialTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ColorScheme = theme.ColorScheme is { } colorScheme ? new ResolvedColorScheme(colorScheme) : null;
        Shapes = new ResolvedShapeScheme(theme.ShapeScheme);
        Motion = new ResolvedMotionScheme(theme.MotionScheme);
        Ripple = new ResolvedRippleThemeConfiguration(theme.RippleTheme);

        // Component defaults are initialized independently; TypeScale is a system table and is
        // resolved through TypographyScheme below rather than through MaterialTheme.Components.
        InitializeComponentTokens(theme);

#pragma warning disable CS0618 // Legacy Components-based TypeScaleOverrides remains supported.
        var legacyTypeScaleOverrides = theme.Components.OfType<TypeScaleOverrides>().ToArray();
#pragma warning restore CS0618

        Typography = new ResolvedTypographyScheme(theme.TypographyScheme, role =>
        {
            var scale = TypeScale.For(role);

            foreach (var overrides in theme.TypographyScheme.TypeScaleOverrides)
            {
                if (overrides.Variant == role)
                    scale = overrides.ApplyTo(scale);
            }

            foreach (var overrides in legacyTypeScaleOverrides)
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

using System;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>
/// The immutable form of a <see cref="MaterialTheme" />: what actually travels down the tree and
/// what every consumer reads.
/// </summary>
/// <remarks>
/// A snapshot is rebuilt whenever the authoring theme changes, so it never needs invalidating —
/// consumers can compare references to decide whether anything can possibly have changed. Values
/// that depend on the consumer's theme variant cannot be resolved here; those are memoized per
/// variant instead, mirroring the per-theme-instance default bundles upstream keeps as ~60 fields
/// on <c>ColorScheme</c>.
/// <para>
/// The component token half of this class is generated from the tables under <c>Tokens/</c> — see
/// <c>ResolvedTheme.Components.g.cs</c> — so adding a component domain costs one table file and no
/// edit here.
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

        // Typography reads the generated type scale, so the component tokens have to exist first.
        InitializeComponentTokens(theme);
        Typography = new ResolvedTypographyScheme(theme.TypographyScheme, GetTypeScale);
    }

    /// <summary>The theme every element falls back to when no theme has been declared above it.</summary>
    public static ResolvedTheme Default { get; } = new(new MaterialTheme());

    public ResolvedColorScheme? ColorScheme { get; }

    public ResolvedTypographyScheme Typography { get; }

    public ResolvedShapeScheme Shapes { get; }

    public ResolvedMotionScheme Motion { get; }

    public ResolvedRippleThemeConfiguration Ripple { get; }
}

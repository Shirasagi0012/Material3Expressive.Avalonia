using System;
using Avalonia.Media;
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
/// </remarks>
public sealed class ResolvedTheme
{
    private readonly ButtonColors?[] _buttonColors = new ButtonColors?[2];

    public ResolvedTheme(MaterialTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ColorScheme = theme.ColorScheme is { } colorScheme ? new ResolvedColorScheme(colorScheme) : null;
        Typography = new ResolvedTypographyScheme(theme.TypographyScheme);
        Shapes = new ResolvedShapeScheme(theme.ShapeScheme);
        Motion = new ResolvedMotionScheme(theme.MotionScheme);
        ButtonTokens = new ResolvedButtonTokens(theme.ButtonTokens);
    }

    /// <summary>The theme every element falls back to when no theme has been declared above it.</summary>
    public static ResolvedTheme Default { get; } = new(new MaterialTheme());

    public ResolvedColorScheme? ColorScheme { get; }

    public ResolvedTypographyScheme Typography { get; }

    public ResolvedShapeScheme Shapes { get; }

    public ResolvedMotionScheme Motion { get; }

    public ResolvedButtonTokens ButtonTokens { get; }

    /// <summary>
    /// The button's brushes for one theme variant, built once per snapshot and variant.
    /// </summary>
    public ButtonColors GetButtonColors(bool isDark)
    {
        var slot = isDark ? 1 : 0;
        return _buttonColors[slot] ??= BuildButtonColors(isDark);
    }

    private ButtonColors BuildButtonColors(bool isDark)
    {
        var tokens = ButtonTokens;

        return new ButtonColors(
            tokens.ContainerColor.ResolveBrush(ColorScheme, isDark),
            tokens.LabelTextColor.ResolveBrush(ColorScheme, isDark),
            tokens.StateLayerColor.ResolveBrush(ColorScheme, isDark),
            tokens.DisabledContainerColor.ResolveBrush(ColorScheme, isDark, tokens.DisabledContainerOpacity),
            tokens.DisabledLabelTextColor.ResolveBrush(ColorScheme, isDark, tokens.DisabledLabelTextOpacity));
    }
}

/// <summary>A button's brushes, resolved for one theme variant.</summary>
public sealed record ButtonColors(
    IImmutableBrush Container,
    IImmutableBrush LabelText,
    IImmutableBrush StateLayer,
    IImmutableBrush DisabledContainer,
    IImmutableBrush DisabledLabelText
);

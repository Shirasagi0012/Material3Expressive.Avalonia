using System;
using Avalonia;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// Component tokens for the filled button at the small (default) size.
/// </summary>
/// <remarks>
/// Transcribed from androidx <c>tokens/FilledButtonTokens.kt</c> and
/// <c>tokens/ButtonSmallTokens.kt</c> (both token-DB stamp <c>v0_11_0</c>), with state layer
/// opacities from <c>tokens/StateTokens.kt</c> (<c>v0_210</c>).
/// <para>
/// Colours are role <i>references</i>, so they follow the scheme and variant in scope wherever the
/// button ends up. Nothing here is a literal brush unless an app author overrides it with one.
/// This is also the shape the Phase 2 generator will emit from a token table — the variant/size
/// split upstream keeps (colour tokens per variant, metrics per size) is why the two source files
/// merge into one bag here for now.
/// </para>
/// </remarks>
public partial class ButtonTokens : AvaloniaObject
{
    public ButtonTokens()
    {
        ContainerColor = SysColorToken.Primary;
        LabelTextColor = SysColorToken.OnPrimary;
        DisabledContainerColor = SysColorToken.OnSurface;
        DisabledContainerOpacity = 0.1;
        DisabledLabelTextColor = SysColorToken.OnSurfaceVariant;
        DisabledLabelTextOpacity = 0.38;
        StateLayerColor = SysColorToken.OnPrimary;

        ContainerShape = ShapeRole.CornerFull;
        PressedContainerShape = ShapeRole.CornerSmall;
        ContainerHeight = 40;
        LeadingSpace = 16;
        TrailingSpace = 16;
        IconSize = 20;
        IconLabelSpace = 8;
        LabelTextStyle = TypographyRole.LabelLarge;

        HoverStateLayerOpacity = StateTokens.HoverStateLayerOpacity;
        FocusStateLayerOpacity = StateTokens.FocusStateLayerOpacity;
        PressedStateLayerOpacity = StateTokens.PressedStateLayerOpacity;
    }

    [GeneratedStyledProperty] public partial ColorTokenValue ContainerColor { get; set; }
    [GeneratedStyledProperty] public partial ColorTokenValue LabelTextColor { get; set; }
    [GeneratedStyledProperty] public partial ColorTokenValue DisabledContainerColor { get; set; }
    [GeneratedStyledProperty] public partial ColorTokenValue DisabledLabelTextColor { get; set; }
    [GeneratedStyledProperty] public partial ColorTokenValue StateLayerColor { get; set; }
    [GeneratedStyledProperty] public partial double DisabledContainerOpacity { get; set; }
    [GeneratedStyledProperty] public partial double DisabledLabelTextOpacity { get; set; }

    [GeneratedStyledProperty] public partial ShapeRole ContainerShape { get; set; }
    [GeneratedStyledProperty] public partial ShapeRole PressedContainerShape { get; set; }
    [GeneratedStyledProperty] public partial double ContainerHeight { get; set; }
    [GeneratedStyledProperty] public partial double LeadingSpace { get; set; }
    [GeneratedStyledProperty] public partial double TrailingSpace { get; set; }
    [GeneratedStyledProperty] public partial double IconSize { get; set; }
    [GeneratedStyledProperty] public partial double IconLabelSpace { get; set; }
    [GeneratedStyledProperty] public partial TypographyRole LabelTextStyle { get; set; }

    [GeneratedStyledProperty] public partial double HoverStateLayerOpacity { get; set; }
    [GeneratedStyledProperty] public partial double FocusStateLayerOpacity { get; set; }
    [GeneratedStyledProperty] public partial double PressedStateLayerOpacity { get; set; }

    public object ProvideValue()
    {
        return this;
    }
}

/// <summary>
/// The immutable copy of <see cref="ButtonTokens" /> carried by a theme snapshot.
/// </summary>
/// <remarks>
/// Colour tokens stay unresolved here because resolution needs the theme variant of the consuming
/// control, which is not known when the snapshot is built. Per-variant resolution is memoized on
/// <see cref="ResolvedTheme" /> instead.
/// </remarks>
public sealed record ResolvedButtonTokens(
    ColorTokenValue ContainerColor,
    ColorTokenValue LabelTextColor,
    ColorTokenValue DisabledContainerColor,
    ColorTokenValue DisabledLabelTextColor,
    ColorTokenValue StateLayerColor,
    double DisabledContainerOpacity,
    double DisabledLabelTextOpacity,
    ShapeRole ContainerShape,
    ShapeRole PressedContainerShape,
    double ContainerHeight,
    double LeadingSpace,
    double TrailingSpace,
    double IconSize,
    double IconLabelSpace,
    TypographyRole LabelTextStyle,
    double HoverStateLayerOpacity,
    double FocusStateLayerOpacity,
    double PressedStateLayerOpacity
)
{
    public ResolvedButtonTokens(ButtonTokens tokens)
        : this(
            (tokens ?? throw new ArgumentNullException(nameof(tokens))).ContainerColor,
            tokens.LabelTextColor,
            tokens.DisabledContainerColor,
            tokens.DisabledLabelTextColor,
            tokens.StateLayerColor,
            tokens.DisabledContainerOpacity,
            tokens.DisabledLabelTextOpacity,
            tokens.ContainerShape,
            tokens.PressedContainerShape,
            tokens.ContainerHeight,
            tokens.LeadingSpace,
            tokens.TrailingSpace,
            tokens.IconSize,
            tokens.IconLabelSpace,
            tokens.LabelTextStyle,
            tokens.HoverStateLayerOpacity,
            tokens.FocusStateLayerOpacity,
            tokens.PressedStateLayerOpacity)
    {
    }

    /// <summary>Content padding implied by the leading and trailing spaces.</summary>
    public Thickness ContentPadding => new(LeadingSpace, 0, TrailingSpace, 0);
}

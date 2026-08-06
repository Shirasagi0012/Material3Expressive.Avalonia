using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// The Phase 2 exit criterion: a component token domain is one table entry and no hand-written
/// plumbing. These tests pin the behaviour that plumbing used to provide.
/// </summary>
public class GeneratedTokenTests
{
    [Fact]
    public void TokenSets_TranscribeTheirUpstreamValues()
    {
        Assert.Equal(0.1, StateTokens.PressedStateLayerOpacity);
        Assert.Equal(0.08, StateTokens.HoverStateLayerOpacity);
        Assert.Equal(3, ElevationTokens.Level2);

        Assert.Equal(40, ButtonSmallTokens.ContainerHeight);
        Assert.Equal(136, ButtonXLargeTokens.ContainerHeight);
        Assert.Equal(ShapeRole.CornerSmall, ButtonSmallTokens.PressedContainerShape);
        Assert.Equal(ShapeRole.CornerExtraLarge, ButtonLargeTokens.ContainerShapeSquare);
    }

    [Fact]
    public void ComponentSets_ReferenceTokenSetsRatherThanRestatingThem()
    {
        // The value has one definition. If the table stopped referencing the transcription, these
        // would drift apart exactly the way the hand-written defaults used to.
        Assert.Equal(ButtonSmallTokens.ContainerHeight, ButtonSizeTokens.Small.ContainerHeight);
        Assert.Equal(ButtonSmallTokens.IconSize, ButtonSizeTokens.Small.IconSize);
        Assert.Equal(FilledButtonTokens.ContainerColor, ButtonColorTokens.Filled.ContainerColor);
    }

    [Fact]
    public void EachVariant_CarriesItsOwnColours()
    {
        Assert.Equal(SysColorToken.Primary, ButtonColorTokens.Filled.ContainerColor.RoleToken);
        Assert.Equal(SysColorToken.SecondaryContainer, ButtonColorTokens.Tonal.ContainerColor.RoleToken);
        Assert.Equal(SysColorToken.SurfaceContainerLow, ButtonColorTokens.Elevated.ContainerColor.RoleToken);

        // Outlined and text have no container of their own.
        Assert.True(ButtonColorTokens.Outlined.ContainerColor.IsLiteral);
        Assert.Equal(0, ButtonColorTokens.Outlined.ContainerColor.LiteralColor!.Value.A);
        Assert.True(ButtonColorTokens.Text.ContainerColor.IsLiteral);

        Assert.True(ButtonColorTokens.Outlined.HasOutline);
        Assert.False(ButtonColorTokens.Text.HasOutline);
        Assert.False(ButtonColorTokens.Filled.HasOutline);
    }

    [Fact]
    public void ToggleDefaults_HaveTheirOwnStateContractAndVariantEnum()
    {
        Assert.Equal(
            SysColorToken.OnSecondaryContainer,
            ToggleButtonColorTokens.Tonal.UncheckedContentColor.RoleToken);
        Assert.Equal(
            SysColorToken.OnSecondary,
            ToggleButtonColorTokens.Tonal.CheckedContentColor.RoleToken);
        Assert.True(ToggleButtonColorTokens.Outlined.HasOutline);
        Assert.Same(
            ToggleButtonColorTokens.Elevated,
            ToggleButtonColorTokens.For(ToggleButtonVariant.Elevated));
    }

    [Fact]
    public void ExtendedSets_InheritWhatTheyDoNotRestate()
    {
        // Tonal extends Filled and says nothing about the outline, so it keeps Filled's.
        Assert.Equal(ButtonColorTokens.Filled.OutlineColor, ButtonColorTokens.Tonal.OutlineColor);
        Assert.Equal(ButtonColorTokens.Filled.DisabledLabelTextOpacity, ButtonColorTokens.Tonal.DisabledLabelTextOpacity);
    }

    [Fact]
    public void For_SelectsTheSetNamedByTheVariant()
    {
        Assert.Same(ButtonColorTokens.Elevated, ButtonColorTokens.For(ButtonVariant.Elevated));
        Assert.Same(ButtonSizeTokens.Large, ButtonSizeTokens.For(ButtonSize.Large));
    }

    [Fact]
    public void ASparseOverride_ReplacesOnlyWhatItSets()
    {
        var theme = new MaterialTheme();
        theme.Components.Add(new ButtonColorOverrides
        {
            Variant = ButtonVariant.Filled,
            ContainerColor = Colors.Fuchsia
        });

        var tokens = theme.Resolved.GetButtonColorTokens(ButtonVariant.Filled);

        Assert.Equal(Colors.Fuchsia, tokens.ContainerColor.LiteralColor);
        Assert.Equal(ButtonColorTokens.Filled.LabelTextColor, tokens.LabelTextColor);
        Assert.Equal(ButtonColorTokens.Filled.DisabledContainerOpacity, tokens.DisabledContainerOpacity);
    }

    [Fact]
    public void AnOverride_LeavesTheOtherVariantsAlone()
    {
        var theme = new MaterialTheme();
        theme.Components.Add(new ButtonColorOverrides
        {
            Variant = ButtonVariant.Tonal,
            ContainerColor = Colors.Fuchsia
        });

        Assert.Equal(
            ButtonColorTokens.Filled.ContainerColor,
            theme.Resolved.GetButtonColorTokens(ButtonVariant.Filled).ContainerColor);
        Assert.Equal(
            Colors.Fuchsia,
            theme.Resolved.GetButtonColorTokens(ButtonVariant.Tonal).ContainerColor.LiteralColor);
    }

    [Fact]
    public void OverridingAComponent_RebuildsTheSnapshot()
    {
        var theme = new MaterialTheme();
        var before = theme.Resolved;

        theme.Components.Add(new ButtonSizeOverrides { Variant = ButtonSize.Small, ContainerHeight = 48 });

        Assert.NotSame(before, theme.Resolved);
        Assert.Equal(48, theme.Resolved.GetButtonSizeTokens(ButtonSize.Small).ContainerHeight);
    }

    [Fact]
    public void EditingAnOverrideInPlace_RebuildsTheSnapshot()
    {
        var theme = new MaterialTheme();
        var overrides = new ButtonSizeOverrides { Variant = ButtonSize.Medium };
        theme.Components.Add(overrides);

        var before = theme.Resolved;
        overrides.ContainerHeight = 64;

        Assert.NotSame(before, theme.Resolved);
        Assert.Equal(64, theme.Resolved.GetButtonSizeTokens(ButtonSize.Medium).ContainerHeight);
    }

    [Fact]
    public void OverriddenColours_ResolveThroughTheMemoisedAccessor()
    {
        var theme = new MaterialTheme();
        theme.Components.Add(new ButtonColorOverrides
        {
            Variant = ButtonVariant.Filled,
            ContainerColor = Colors.Fuchsia
        });

        var colors = theme.Resolved.GetButtonColors(ButtonVariant.Filled, isDark: false);

        Assert.Equal(Colors.Fuchsia, ((ISolidColorBrush)colors.ContainerColor).Color);
    }
}

using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>Contracts for raw token transcription and component-owned defaults assembly.</summary>
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
    public void ButtonDefaults_AssemblesMetricsFromRawSizeTables()
    {
        var small = ButtonDefaults.Metrics(ButtonSize.Small);
        var large = ButtonDefaults.Metrics(ButtonSize.Large);

        Assert.Equal(ButtonSmallTokens.ContainerHeight, small.ContainerHeight);
        Assert.Equal(ButtonSmallTokens.IconSize, small.IconSize);
        Assert.Equal(new Avalonia.Thickness(16, 10), small.ContentPadding);
        Assert.Equal(ButtonLargeTokens.ContainerHeight, large.ContainerHeight);
        Assert.Equal(TypographyRole.HeadlineSmall, large.LabelTextStyle);
    }

    [Fact]
    public void ButtonDefaults_AssemblesVariantPolicyFromRawColourTables()
    {
        var theme = ResolvedTheme.Default;
        var filled = ButtonDefaults.Colors(theme, ButtonVariant.Filled, isDark: false);
        var tonal = ButtonDefaults.Colors(theme, ButtonVariant.Tonal, isDark: false);
        var outlined = ButtonDefaults.Colors(theme, ButtonVariant.Outlined, isDark: false);
        var text = ButtonDefaults.Colors(theme, ButtonVariant.Text, isDark: false);

        Assert.Equal(
            theme.ColorScheme!.GetColor(SysColorToken.Primary, isDark: false),
            ((ISolidColorBrush)filled.Container).Color);
        Assert.Equal(
            theme.ColorScheme.GetColor(SysColorToken.SecondaryContainer, isDark: false),
            ((ISolidColorBrush)tonal.Container).Color);
        Assert.Equal(0, ((ISolidColorBrush)outlined.Container).Color.A);
        Assert.Equal(0, ((ISolidColorBrush)text.Container).Color.A);
        Assert.Equal(
            theme.ColorScheme.GetColor(SysColorToken.OutlineVariant, isDark: false),
            ((ISolidColorBrush)outlined.Outline).Color);
    }

    [Fact]
    public void ButtonDefaults_KeepComposeCorrectionsInComponentCode()
    {
        var extraSmall = ButtonDefaults.Metrics(ButtonSize.ExtraSmall);
        var small = ButtonDefaults.Metrics(ButtonSize.Small);

        // The raw XSmall table says 16dp; Compose ButtonDefaults corrects the shipped horizontal
        // padding to 12dp. Small's vertical padding is likewise Avalonia's coarse-pointer choice.
        Assert.Equal(new Avalonia.Thickness(12, 6), extraSmall.ContentPadding);
        Assert.Equal(new Avalonia.Thickness(16, 10), small.ContentPadding);
        Assert.Equal(new Avalonia.Thickness(0), ButtonDefaults.OutlineThickness(ButtonVariant.Text, ButtonSize.Small));
        Assert.Equal(
            new Avalonia.Thickness(ButtonLargeTokens.OutlinedOutlineWidth),
            ButtonDefaults.OutlineThickness(ButtonVariant.Outlined, ButtonSize.Large));
    }

    [Fact]
    public void ToggleButtonDefaults_AssembleTheirOwnStateContractFromRawButtonTables()
    {
        var theme = ResolvedTheme.Default;
        var tonal = ToggleButtonDefaults.Colors(theme, ButtonVariant.Tonal, isDark: false);
        var outlined = ToggleButtonDefaults.Colors(theme, ButtonVariant.Outlined, isDark: false);

        Assert.Equal(
            theme.ColorScheme!.GetColor(SysColorToken.OnSecondaryContainer, isDark: false),
            ((ISolidColorBrush)tonal.UncheckedContent).Color);
        Assert.Equal(
            theme.ColorScheme.GetColor(SysColorToken.OnSecondary, isDark: false),
            ((ISolidColorBrush)tonal.CheckedContent).Color);
        Assert.True(outlined.HasOutline);
        Assert.Equal(
            theme.ColorScheme.GetColor(SysColorToken.InverseOnSurface, isDark: false),
            ((ISolidColorBrush)outlined.CheckedContent).Color);
    }
}

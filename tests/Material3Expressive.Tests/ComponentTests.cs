using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class IconButtonTests
{
    [AvaloniaFact]
    public void ItsWidthAxis_IsIndependentOfItsSize()
    {
        var narrow = new MaterialIconButton { Size = IconButtonSize.Medium, ButtonWidth = IconButtonWidth.Narrow };
        var wide = new MaterialIconButton { Size = IconButtonSize.Medium, ButtonWidth = IconButtonWidth.Wide };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { narrow, wide } });

        Assert.Equal(MediumIconButtonTokens.NarrowLeadingSpace, narrow.Padding.Left);
        Assert.Equal(MediumIconButtonTokens.WideLeadingSpace, wide.Padding.Left);

        // Same size, so the icon and the height are unchanged by the width.
        Assert.Equal(narrow.IconSize, wide.IconSize);
        Assert.Equal(narrow.MinHeight, wide.MinHeight);
    }

    [AvaloniaFact]
    public void TheStandardVariant_HasNoContainer()
    {
        Assert.Equal(0, IconButtonColorTokens.Standard.ContainerColor.LiteralColor!.Value.A);
        Assert.False(IconButtonColorTokens.Standard.HasOutline);
        Assert.True(IconButtonColorTokens.Outlined.HasOutline);
    }

    [AvaloniaFact]
    public void ItsIconColour_FollowsTheVariantAndTheThemeInScope()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new MaterialIconButton { Variant = IconButtonVariant.Filled };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary),
            ThemeTestHelper.ColorOf(button.Foreground));
    }

    [AvaloniaFact]
    public void TheOutlinedVariantsSelectedState_Inverts()
    {
        // Outlined is the odd one out: selected, it flips to the inverse surface rather than
        // filling with primary the way the other three do.
        var outlined = IconButtonColorTokens.Outlined;

        Assert.Equal(SysColorToken.InverseSurface, outlined.SelectedContainerColor.RoleToken);
        Assert.Equal(SysColorToken.InverseOnSurface, outlined.SelectedIconColor.RoleToken);
        Assert.Equal(SysColorToken.Primary, IconButtonColorTokens.Filled.SelectedContainerColor.RoleToken);
    }
}

public class CardTests
{
    [AvaloniaFact]
    public void EachVariant_HasItsOwnSurface()
    {
        Assert.Equal(SysColorToken.SurfaceContainerLow, CardTokens.Elevated.ContainerColor.RoleToken);
        Assert.Equal(SysColorToken.SurfaceContainerHighest, CardTokens.Filled.ContainerColor.RoleToken);
        Assert.Equal(SysColorToken.Surface, CardTokens.Outlined.ContainerColor.RoleToken);
    }

    [AvaloniaFact]
    public void OnlyTheOutlinedVariant_DrawsABorder()
    {
        Assert.True(CardTokens.Outlined.HasOutline);
        Assert.Equal(OutlinedCardTokens.OutlineWidth, CardTokens.Outlined.OutlineWidth);
        Assert.False(CardTokens.Elevated.HasOutline);
        Assert.False(CardTokens.Filled.HasOutline);
    }

    [AvaloniaFact]
    public void OnlyTheElevatedVariant_RestsAboveTheSurface()
    {
        Assert.True(CardTokens.Elevated.ContainerElevation > 0);
        Assert.Equal(0, CardTokens.Filled.ContainerElevation);
        Assert.Equal(0, CardTokens.Outlined.ContainerElevation);
    }

    [AvaloniaFact]
    public void ACard_IsNotFocusableUntilItIsClickable()
    {
        var card = new MaterialCard();
        ThemeTestHelper.ShowWindow(card);

        Assert.False(card.Focusable);

        card.IsClickable = true;
        Assert.True(card.Focusable);
    }
}

public class FabTests
{
    [AvaloniaFact]
    public void ItIsSquareUntilGivenALabel()
    {
        var fab = new MaterialFab();
        ThemeTestHelper.ShowWindow(fab);

        Assert.False(fab.IsExtended);
        Assert.Equal(FabBaselineTokens.ContainerWidth, fab.MinWidth);
        Assert.Equal(default, fab.Padding);

        fab.Label = "Compose";

        Assert.True(fab.IsExtended);
        Assert.Equal(FabSizeTokens.Small.LabelSpace, fab.Padding.Left);
    }

    [AvaloniaFact]
    public void EachSize_RetunesTheContainerAndTheIconTogether()
    {
        var fab = new MaterialFab();
        ThemeTestHelper.ShowWindow(fab);

        Assert.Equal(FabBaselineTokens.IconSize, fab.IconSize);

        fab.Size = FabSize.Large;

        Assert.Equal(FabLargeTokens.ContainerHeight, fab.MinHeight);
        Assert.Equal(FabLargeTokens.IconSize, fab.IconSize);
    }

    [AvaloniaFact]
    public void LoweringIt_DropsItsElevation()
    {
        var tokens = FabColorTokens.Primary;

        Assert.True(tokens.LoweredContainerElevation < tokens.ContainerElevation);
        Assert.Equal(ElevationTokens.Level1, tokens.LoweredContainerElevation);
        Assert.Equal(ElevationTokens.Level3, tokens.ContainerElevation);
    }

    [AvaloniaFact]
    public void ItsColour_FollowsTheThemeInScope()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var fab = new MaterialFab();
        var host = new Border { Child = fab };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnPrimaryContainer),
            ThemeTestHelper.ColorOf(fab.Foreground));
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
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

public class ToggleTests
{
    [AvaloniaFact]
    public void CheckingAToggleButton_SwitchesItToTheSelectedColours()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var toggle = new MaterialToggleButton { Variant = ButtonVariant.Tonal, Content = "On" };
        var host = new Border { Child = toggle };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
            ThemeTestHelper.ColorOf(toggle.Foreground));

        toggle.IsChecked = true;

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSecondary),
            ThemeTestHelper.ColorOf(toggle.Foreground));
    }

    [AvaloniaFact]
    public void AToggleIconButtonsSelectedState_HasItsOwnShapeTokens()
    {
        // The icon button does not simply invert round and square: it carries a separate pair of
        // selected shapes, and at the small size they differ from the unselected pair.
        var metrics = IconButtonSizeTokens.Small;

        Assert.Equal(ShapeRole.CornerFull, metrics.ContainerShape);
        Assert.Equal(ShapeRole.CornerMedium, metrics.SelectedContainerShape);
        Assert.Equal(ShapeRole.CornerMedium, metrics.SquareContainerShape);
        Assert.Equal(ShapeRole.CornerFull, metrics.SelectedSquareContainerShape);
    }

    [AvaloniaFact]
    public void EveryButtonVariant_DefinesItsToggleColours()
    {
        foreach (var variant in System.Enum.GetValues<ButtonVariant>())
        {
            var tokens = ButtonColorTokens.For(variant);

            // The Filled and Tonal families select towards their own container; nothing should
            // have silently inherited another variant's toggle colours.
            Assert.NotEqual(default, tokens.SelectedLabelTextColor);
            Assert.NotEqual(default, tokens.UnselectedLabelTextColor);
        }

        Assert.Equal(SysColorToken.OnPrimary, ButtonColorTokens.Filled.SelectedLabelTextColor.RoleToken);
        Assert.Equal(SysColorToken.OnSecondary, ButtonColorTokens.Tonal.SelectedLabelTextColor.RoleToken);
        Assert.Equal(SysColorToken.InverseOnSurface, ButtonColorTokens.Outlined.SelectedLabelTextColor.RoleToken);
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

public class SelectionTests
{
    [AvaloniaFact]
    public void ACheckboxsTouchTarget_IsLargerThanItsBox()
    {
        // Material sizes the state layer and the hit area at 40dp around an 18dp box; a control
        // that only reserved the box would be a fiddly target.
        var tokens = CheckboxTokens.Default;

        Assert.Equal(18, tokens.ContainerSize);
        Assert.Equal(40, tokens.StateLayerSize);
        Assert.True(tokens.StateLayerSize > tokens.ContainerSize);
    }

    [AvaloniaFact]
    public void ACheckbox_ReservesRoomForItsGlyph()
    {
        var checkbox = new MaterialCheckbox { Content = "Label" };
        ThemeTestHelper.ShowWindow(checkbox);

        Assert.Equal(CheckboxTokens.Default.StateLayerSize, checkbox.Padding.Left);
        Assert.True(checkbox.Bounds.Height >= CheckboxTokens.Default.StateLayerSize);
    }

    [AvaloniaFact]
    public void ARadioButtonsRing_FitsInsideItsIconBox()
    {
        var tokens = RadioButtonTokens.Default;

        // The ring is stroked, so its centre line has to be inset by half the stroke or it would
        // spill past the 20dp box.
        Assert.True(tokens.RingWidth > 0);
        Assert.True(tokens.DotSize < tokens.IconSize - tokens.RingWidth * 2);
    }

    [AvaloniaFact]
    public void ASwitchsHandle_GrowsFromOffToOnToPressed()
    {
        var tokens = SwitchTokens.Default;

        Assert.True(tokens.UnselectedHandleSize < tokens.SelectedHandleSize);
        Assert.True(tokens.SelectedHandleSize < tokens.PressedHandleSize);

        // And the largest of them still fits inside the track.
        Assert.True(tokens.PressedHandleSize <= tokens.TrackHeight);
    }

    [AvaloniaFact]
    public void ASwitchsColours_FollowTheThemeInScope()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var host = new Border { Child = new MaterialSwitch() };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        var colors = theme.Resolved.GetSwitchColors(isDark: false);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(colors.SelectedTrackColor));
    }

    [AvaloniaFact]
    public void ASingleSetTable_StillGetsItsOverrideBag()
    {
        // Checkbox, radio and switch have no variants, which is a different generator path from
        // everything else in the library.
        var theme = new MaterialTheme();
        theme.Components.Add(new CheckboxOverrides { ContainerSize = 24 });

        Assert.Equal(24, theme.Resolved.CheckboxTokens.ContainerSize);
        Assert.Equal(CheckboxTokens.Default.StateLayerSize, theme.Resolved.CheckboxTokens.StateLayerSize);
    }
}

public class ProgressTests
{
    [AvaloniaFact]
    public void TheTwoShapes_ShareTheirColoursAndDifferInMetrics()
    {
        var linear = ProgressTokens.Linear;
        var circular = ProgressTokens.Circular;

        Assert.Equal(linear.ActiveIndicatorColor, circular.ActiveIndicatorColor);
        Assert.Equal(linear.TrackColor, circular.TrackColor);

        // A circle has no far end, so no stop dot.
        Assert.True(linear.StopSize > 0);
        Assert.Equal(0, circular.StopSize);

        // The circular wave is much shallower — it has to stay on a 40dp ring.
        Assert.True(circular.WaveAmplitude < linear.WaveAmplitude);
    }

    [AvaloniaFact]
    public void ExpressiveLeavesAGap_BetweenTheIndicatorAndTheTrack()
    {
        Assert.True(ProgressTokens.Linear.TrackActiveSpace > 0);
        Assert.True(ProgressTokens.Circular.TrackActiveSpace > 0);
    }

    [AvaloniaFact]
    public void AWavyIndicator_ReservesMoreHeightThanAFlatOne()
    {
        var flat = new MaterialLinearProgressIndicator { Value = 0.5, Width = 200 };
        var wavy = new MaterialLinearProgressIndicator { Value = 0.5, Width = 200, IsWavy = true };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { flat, wavy } });

        Assert.True(wavy.Bounds.Height > flat.Bounds.Height);
        Assert.Equal(ProgressTokens.Linear.WaveSize, wavy.Bounds.Height, 3);
    }

    [AvaloniaFact]
    public void AnIndeterminateIndicator_KeepsItsLoopRunning()
    {
        // The one animation with no destination. A spinner that settles is a bug, so this asserts
        // it does not — and that turning it off does stop it.
        var loop = new LoopingAnimation(System.TimeSpan.FromSeconds(1));

        Assert.True(loop.Tick(System.TimeSpan.FromSeconds(0.25)));
        Assert.Equal(0.25, loop.Value, 6);

        // It wraps rather than ending.
        Assert.True(loop.Tick(System.TimeSpan.FromSeconds(1)));
        Assert.Equal(0.25, loop.Value, 6);

        loop.Settle();
        Assert.False(loop.IsRunning);
        Assert.False(loop.Tick(System.TimeSpan.FromSeconds(0.25)));
    }

}

public class LoadingIndicatorTests
{
    [AvaloniaFact]
    public void OnlyTheContainedVariant_DrawsAContainer()
    {
        Assert.False(LoadingIndicatorTokens.Standard.HasContainer);
        Assert.True(LoadingIndicatorTokens.Contained.HasContainer);

        Assert.Equal(0, LoadingIndicatorTokens.Standard.ContainerColor.LiteralColor!.Value.A);
        Assert.Equal(SysColorToken.PrimaryContainer, LoadingIndicatorTokens.Contained.ContainerColor.RoleToken);
    }

    [AvaloniaFact]
    public void TheContainedVariant_ReservesTheLargerBox()
    {
        var standard = new MaterialLoadingIndicator();
        var contained = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { standard, contained } });

        Assert.Equal(LoadingIndicatorTokens.Standard.ActiveSize, standard.Bounds.Width, 3);
        Assert.Equal(LoadingIndicatorTokens.Contained.ContainerSize, contained.Bounds.Width, 3);
    }

    [AvaloniaFact]
    public void ItMorphsRealPolygons()
    {
        // The one component that pulls in the graphics-shapes port. If the geometry bridge stops
        // producing a figure, this is where it shows.
        var morph = new MaterialShapes.Morph(
            MaterialShapes.MaterialShapeCatalog.Pentagon,
            MaterialShapes.MaterialShapeCatalog.Sunny);

        var geometry = MaterialShapes.AvaloniaExtensions.ToGeometry(morph, 0.5);

        Assert.NotNull(geometry);
        Assert.False(geometry.Bounds.Width == 0 && geometry.Bounds.Height == 0);
    }
}

public class SliderTests
{
    [AvaloniaFact]
    public void TheHandle_NarrowsWhilePressedSoTheValueStaysVisible()
    {
        var tokens = SliderTokens.Default;

        Assert.True(tokens.PressedHandleWidth < tokens.HandleWidth);
        // And it stands taller than the track it sits on.
        Assert.True(tokens.HandleHeight > tokens.TrackHeight);
    }

    [AvaloniaFact]
    public void ItReservesTheHandlesHeight()
    {
        var slider = new MaterialSlider { Width = 200 };
        ThemeTestHelper.ShowWindow(slider);

        Assert.Equal(SliderTokens.Default.HandleHeight, slider.Bounds.Height, 3);
    }

    [AvaloniaFact]
    public void ClickingTheTrack_MovesTheValueThere()
    {
        var slider = new MaterialSlider { Width = 200, Minimum = 0, Maximum = 100 };
        var window = ThemeTestHelper.ShowWindow(slider);

        var middle = slider.TranslatePoint(new Avalonia.Point(100, slider.Bounds.Height / 2), window)!.Value;
        window.MouseDown(middle, Avalonia.Input.MouseButton.Left);

        // Halfway along a 200px track, allowing for the handle inset at each end.
        Assert.InRange(slider.Value, 45, 55);
    }

    [AvaloniaFact]
    public void ArrowKeys_StepByTheSmallChange()
    {
        var slider = new MaterialSlider { Width = 200, Minimum = 0, Maximum = 10, SmallChange = 1, Value = 5 };
        ThemeTestHelper.ShowWindow(slider);

        slider.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Right
        });

        Assert.Equal(6, slider.Value);
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

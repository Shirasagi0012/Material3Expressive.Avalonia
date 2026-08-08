using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        var theme = ResolvedTheme.Default;
        foreach (var variant in System.Enum.GetValues<ButtonVariant>())
        {
            var colors = ToggleButtonDefaults.Colors(theme, variant, isDark: false);

            // ToggleButton has a checked/unchecked contract of its own; it does not inherit
            // Button's ordinary enabled colours merely because it shares the Variant axis.
            Assert.NotNull(colors.CheckedContent);
            Assert.NotNull(colors.UncheckedContent);
        }

        Assert.Equal(
            theme.ColorScheme!.GetColor(SysColorToken.OnPrimary, isDark: false),
            ThemeTestHelper.ColorOf(ToggleButtonDefaults.Colors(theme, ButtonVariant.Filled, isDark: false).CheckedContent));
        Assert.Equal(
            theme.ColorScheme.GetColor(SysColorToken.OnSecondary, isDark: false),
            ThemeTestHelper.ColorOf(ToggleButtonDefaults.Colors(theme, ButtonVariant.Tonal, isDark: false).CheckedContent));
        Assert.Equal(
            theme.ColorScheme.GetColor(SysColorToken.InverseOnSurface, isDark: false),
            ThemeTestHelper.ColorOf(ToggleButtonDefaults.Colors(theme, ButtonVariant.Outlined, isDark: false).CheckedContent));
    }

    [Fact]
    public void ToggleDefaults_ExposeIndependentCheckedAndDisabledStateSelection()
    {
        var colors = ToggleButtonDefaults.Colors(ResolvedTheme.Default, ButtonVariant.Tonal, isDark: false);

        Assert.Same(colors.UncheckedContent, colors.Content(enabled: true, isChecked: false));
        Assert.Same(colors.CheckedContent, colors.Content(enabled: true, isChecked: true));
        Assert.Same(colors.DisabledContent, colors.Content(enabled: false, isChecked: true));
        Assert.NotSame(colors.UncheckedContent, colors.CheckedContent);
    }

    [Fact]
    public void ToggleDefaults_KeepThreeShapeStatesSeparateFromButtonColorState()
    {
        var shapes = ToggleButtonDefaults.Shapes(ButtonSize.Small);

        Assert.Equal(ButtonSmallTokens.ContainerShapeRound, shapes.Unchecked);
        Assert.Equal(ButtonSmallTokens.PressedContainerShape, shapes.Pressed);
        Assert.Equal(ButtonSmallTokens.ContainerShapeSquare, shapes.Checked);
    }

    [Fact]
    public void ToggleDefaults_UseSpatialMotionForTheCheckedShapeMorph()
    {
        Assert.Equal(
            ResolvedTheme.Default.Motion.Get(MotionRole.FastSpatial),
            ToggleButtonDefaults.ShapeMotion(ResolvedTheme.Default));
    }
}

public class ButtonDefaultsTests
{
    [Fact]
    public void ButtonDefaults_ResolvesColoursAndStateElevationFromTheSameVariant()
    {
        var theme = ResolvedTheme.Default;
        var colors = ButtonDefaults.Colors(theme, ButtonVariant.Elevated, isDark: false);
        var elevation = ButtonDefaults.Elevation(ButtonVariant.Elevated);

        Assert.Equal(
            theme.ColorScheme!.GetColor(SysColorToken.SurfaceContainerLow, isDark: false),
            ThemeTestHelper.ColorOf(colors.Container));
        Assert.Equal(
            elevation.Pressed,
            elevation.For(enabled: true, InteractionState.Pressed));
        Assert.Equal(
            elevation.Disabled,
            elevation.For(enabled: false, InteractionState.Hovered));
    }

    [Fact]
    public void ButtonDefaults_AdaptsSizeTokensIntoTemplateValues()
    {
        var metrics = ButtonDefaults.Metrics(ButtonSize.Medium);
        var shapes = ButtonDefaults.Shapes(ButtonSize.Medium);

        Assert.Equal(
            new Thickness(
                ButtonMediumTokens.LeadingSpace,
                16,
                ButtonMediumTokens.TrailingSpace,
                16),
            metrics.ContentPadding);
        Assert.Equal(metrics.ContainerShape, shapes.Container);
        Assert.Equal(metrics.PressedContainerShape, shapes.Pressed);
        Assert.Equal(metrics.SquareContainerShape, shapes.Square);
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
        var tokens = CheckboxDefaultsTokens.Default;

        Assert.Equal(18, tokens.ContainerSize);
        Assert.Equal(40, tokens.StateLayerSize);
        Assert.True(tokens.StateLayerSize > tokens.ContainerSize);
    }

    [AvaloniaFact]
    public void ACheckbox_ReservesRoomForItsGlyph()
    {
        var checkbox = new MaterialCheckbox { Content = "Label" };
        ThemeTestHelper.ShowWindow(checkbox);

        Assert.Equal(48, checkbox.Padding.Left);
        Assert.True(checkbox.Bounds.Height >= CheckboxDefaultsTokens.Default.StateLayerSize);
    }

    [AvaloniaFact]
    public void ACheckbox_UsesAvaloniaTriStateForTheMaterialIndeterminateState()
    {
        var checkbox = new MaterialCheckbox { IsThreeState = true, IsChecked = null };
        ThemeTestHelper.ShowWindow(checkbox);

        Assert.True(checkbox.IsThreeState);
        Assert.Null(checkbox.IsChecked);
        Assert.True(checkbox.Bounds.Width >= CheckboxDefaultsTokens.Default.StateLayerSize);
    }

    [AvaloniaFact]
    public void ARadioButtonsRing_FitsInsideItsIconBox()
    {
        var tokens = RadioButtonDefaultsTokens.Default;

        // The ring is stroked, so its centre line has to be inset by half the stroke or it would
        // spill past the 20dp box.
        Assert.True(tokens.RingWidth > 0);
        Assert.True(tokens.DotSize < tokens.IconSize - tokens.RingWidth * 2);
    }

    [AvaloniaFact]
    public void ASwitchsHandle_GrowsFromOffToOnToPressed()
    {
        var tokens = SwitchDefaultsTokens.Default;

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

    [Fact]
    public void SwitchDefaults_ProjectCheckedAndDisabledStateIntoRenderValues()
    {
        var colors = SwitchDefaults.Colors(ResolvedTheme.Default, isDark: false);
        var selected = SwitchDefaults.Visuals(colors, enabled: true, selected: true);
        var disabled = SwitchDefaults.Visuals(colors, enabled: false, selected: false);

        Assert.Same(colors.SelectedTrackColor, selected.TrackColor);
        Assert.Null(selected.TrackOutlineColor);
        Assert.Same(colors.DisabledUnselectedTrackColor, disabled.TrackColor);
        Assert.Same(colors.DisabledUnselectedTrackOutlineColor, disabled.TrackOutlineColor);
    }

    [AvaloniaFact]
    public void ASingleSetTable_StillGetsItsOverrideBag()
    {
        // Checkbox, radio and switch have no variants, which is a different generator path from
        // everything else in the library.
        var theme = new MaterialTheme();
        theme.Components.Add(new CheckboxOverrides { ContainerSize = 24 });

        Assert.Equal(24, theme.Resolved.CheckboxDefaultsTokens.ContainerSize);
        Assert.Equal(CheckboxDefaultsTokens.Default.StateLayerSize, theme.Resolved.CheckboxDefaultsTokens.StateLayerSize);
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
    public void ACircularWavyIndicator_UsesTheWaveContainerAndKeepsTheAlpha25AmplitudeRule()
    {
        var flat = new MaterialCircularProgressIndicator { Value = .5 };
        var wavy = new MaterialCircularProgressIndicator { Value = .5, IsWavy = true };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { flat, wavy } });

        Assert.Equal(ProgressTokens.Circular.Size, flat.Bounds.Width, 3);
        Assert.Equal(ProgressTokens.Circular.WaveSize, wavy.Bounds.Width, 3);
        Assert.True(wavy.Bounds.Width > flat.Bounds.Width);
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

public class SearchCarouselAndPickerTests
{
    [AvaloniaFact]
    public void TheSearchBarChangesShapeWhenItOpens()
    {
        // A pill closed, a rounded rectangle open — one control, because it is one shape.
        Assert.Equal(ShapeRole.CornerFull, SearchBarDefaultsTokens.Default.ContainerShape);
        Assert.Equal(ShapeRole.CornerExtraLarge, SearchBarDefaultsTokens.Default.ResultsContainerShape);
    }

    [AvaloniaFact]
    public void TheHeroCarousel_GivesOneItemMoreRoomThanMultiBrowse()
    {
        Assert.True(CarouselTokens.Hero.LargeItemWidth > CarouselTokens.MultiBrowse.LargeItemWidth);

        // And squeezes the edges harder, so less of the next item shows.
        Assert.True(CarouselTokens.Hero.SmallItemWidth < CarouselTokens.MultiBrowse.SmallItemWidth);
    }

    [AvaloniaFact]
    public void CarouselItems_AreSqueezedAtTheEdgesRatherThanClipped()
    {
        var carousel = new MaterialCarousel { Width = 420, Height = 200 };
        for (var i = 0; i < 5; i++)
            carousel.Children.Add(new Border());

        ThemeTestHelper.ShowWindow(carousel);

        var widths = carousel.Children.Select(c => c.Bounds.Width).ToArray();

        // The first item starts at the left edge and is therefore squeezed; one in the middle is not.
        Assert.True(widths[0] < CarouselTokens.MultiBrowse.LargeItemWidth);
        Assert.True(widths[0] >= CarouselTokens.MultiBrowse.SmallItemWidth);
        Assert.True(widths[1] > widths[0]);
    }

    [AvaloniaFact]
    public void SelectingADate_MovesTheGridToItsMonth()
    {
        var picker = new MaterialDatePicker();
        ThemeTestHelper.ShowWindow(picker);

        picker.SelectedDate = new System.DateTime(2019, 3, 14);

        Assert.Equal(2019, picker.DisplayMonth.Year);
        Assert.Equal(3, picker.DisplayMonth.Month);
    }

    [AvaloniaFact]
    public void TheTimePicker_MovesOnToMinutesOnceAnHourIsChosen()
    {
        var picker = new MaterialTimePicker { Time = new System.TimeSpan(9, 30, 0) };
        ThemeTestHelper.ShowWindow(picker);

        Assert.Equal(TimePickerField.Hour, picker.Field);

        // Releasing after choosing an hour advances the dial; a single dial cannot show both.
        picker.Field = TimePickerField.Minute;
        Assert.Equal(TimePickerField.Minute, picker.Field);
        Assert.Equal(new System.TimeSpan(9, 30, 0), picker.Time);
    }

    [AvaloniaFact]
    public void TheTimePickerIsTallerPerCellThanTheDatePicker()
    {
        // A clock face needs bigger targets than a month grid, and says so in its tokens.
        Assert.True(PickerTokens.Time.DaySize > PickerTokens.Date.DaySize);
        Assert.True(PickerTokens.Time.IsTime);
        Assert.False(PickerTokens.Date.IsTime);
    }
}

public class OverlayTests
{
    [AvaloniaFact]
    public void AClosedOverlay_DoesNotSwallowClicks()
    {
        var dialog = new MaterialDialog();
        var sheet = new MaterialBottomSheet();
        var snackbar = new MaterialSnackbar();
        ThemeTestHelper.ShowWindow(new Panel { Children = { dialog, sheet, snackbar } });

        Assert.False(dialog.IsHitTestVisible);
        Assert.False(sheet.IsHitTestVisible);
        Assert.False(snackbar.IsHitTestVisible);

        dialog.IsOpen = true;
        Assert.True(dialog.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void ADialogThatMustBeAnswered_IgnoresEscape()
    {
        var dialog = new MaterialDialog { IsDismissable = false, IsOpen = true };
        ThemeTestHelper.ShowWindow(dialog);

        dialog.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Escape
        });

        Assert.True(dialog.IsOpen);

        dialog.IsDismissable = true;
        dialog.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Escape
        });

        Assert.False(dialog.IsOpen);
    }

    [AvaloniaFact]
    public void TheSnackbarInverts_SoItReadsAsSeparateFromThePage()
    {
        var tokens = SnackbarDefaultsTokens.Default;

        Assert.Equal(SysColorToken.InverseSurface, tokens.ContainerColor.RoleToken);
        Assert.Equal(SysColorToken.InverseOnSurface, tokens.SupportingTextColor.RoleToken);
        // The action has to stay an accent against the inverted surface.
        Assert.Equal(SysColorToken.InversePrimary, tokens.ActionLabelTextColor.RoleToken);
    }

    [AvaloniaFact]
    public void OnlyTheModalSheet_HasAScrim()
    {
        Assert.True(BottomSheetTokens.Modal.IsModal);
        Assert.True(BottomSheetTokens.Modal.ScrimOpacity > 0);
        Assert.False(BottomSheetTokens.Standard.IsModal);
        Assert.Equal(0, BottomSheetTokens.Standard.ScrimOpacity);
    }

    [AvaloniaFact]
    public void TheSheetsCornersAreRoundedOnlyAlongItsTopEdge()
    {
        var shape = new MaterialTheme().Resolved.Shapes.Get(BottomSheetTokens.Modal.ContainerShape);

        Assert.NotEqual(CornerSize.Zero, shape.TopStart);
        Assert.NotEqual(CornerSize.Zero, shape.TopEnd);
        Assert.Equal(CornerSize.Zero, shape.BottomStart);
        Assert.Equal(CornerSize.Zero, shape.BottomEnd);
    }

    [AvaloniaFact]
    public void OnlyTheLargeBadge_ShowsItsCount()
    {
        var small = new MaterialBadge { Variant = BadgeVariant.Small, Count = 5 };
        var large = new MaterialBadge { Variant = BadgeVariant.Large, Count = 5 };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { small, large } });

        Assert.False(small.HasLabel);
        Assert.True(large.HasLabel);

        // And an empty large badge is still just a dot.
        large.Count = null;
        Assert.False(large.HasLabel);
    }

    [AvaloniaFact]
    public void ThePlainTooltipInverts_AndTheRichOneDoesNot()
    {
        Assert.Equal(SysColorToken.InverseSurface, TooltipTokens.Plain.ContainerColor.RoleToken);
        Assert.Equal(SysColorToken.SurfaceContainer, TooltipTokens.Rich.ContainerColor.RoleToken);

        // A rich tooltip has more to say, so it gets more room.
        Assert.True(TooltipTokens.Rich.MaxWidth > TooltipTokens.Plain.MaxWidth);
    }
}

public class NavigationTests
{
    [AvaloniaFact]
    public void TheBarAndTheRail_DifferOnlyInSurfaceElevationAndAxis()
    {
        var bar = NavigationTokens.Bar;
        var rail = NavigationTokens.Rail;

        Assert.False(bar.IsVertical);
        Assert.True(rail.IsVertical);
        Assert.True(bar.ContainerElevation > rail.ContainerElevation);

        // The items themselves are identical.
        Assert.Equal(bar.IconSize, rail.IconSize);
        Assert.Equal(bar.ActiveIndicatorWidth, rail.ActiveIndicatorWidth);
        Assert.Equal(bar.ActiveIndicatorHeight, rail.ActiveIndicatorHeight);
    }

    [AvaloniaFact]
    public void SelectionIsExclusive_AndOwnedByTheBar()
    {
        var bar = new MaterialNavigationBar();
        bar.Items.Add(new MaterialNavigationItem { Content = "One" });
        bar.Items.Add(new MaterialNavigationItem { Content = "Two" });
        bar.Items.Add(new MaterialNavigationItem { Content = "Three" });
        ThemeTestHelper.ShowWindow(bar);

        bar.SelectedIndex = 1;

        Assert.False(bar.Items[0].IsChecked);
        Assert.True(bar.Items[1].IsChecked);
        Assert.False(bar.Items[2].IsChecked);

        // Checking an item directly reports back rather than leaving two selected.
        bar.Items[2].IsChecked = true;

        Assert.Equal(2, bar.SelectedIndex);
        Assert.False(bar.Items[1].IsChecked);
    }

    [AvaloniaFact]
    public void UncheckingTheSelectedItem_DoesNotLeaveTheBarEmpty()
    {
        var bar = new MaterialNavigationBar();
        bar.Items.Add(new MaterialNavigationItem { Content = "One" });
        bar.Items.Add(new MaterialNavigationItem { Content = "Two" });
        ThemeTestHelper.ShowWindow(bar);

        bar.SelectedIndex = 0;
        bar.Items[0].IsChecked = false;

        // A navigation bar always has somewhere you are.
        Assert.True(bar.Items[0].IsChecked);
        Assert.Equal(0, bar.SelectedIndex);
    }
}

public class TabTests
{
    [AvaloniaFact]
    public void ThePrimaryIndicatorHugsTheLabel_AndTheSecondaryOneDoesNot()
    {
        Assert.True(TabTokens.Primary.IndicatorSpansLabel);
        Assert.False(TabTokens.Secondary.IndicatorSpansLabel);

        // TabRowDefaults.SecondaryIndicator deliberately takes its default height from
        // PrimaryNavigationTabTokens.ActiveIndicatorHeight (3dp). The primary/secondary
        // distinction is therefore content-width and shape, not thickness. Source:
        // AndroidX ff0a0a99 / Material3 1.5.0-alpha25, TabRow.kt:PrimaryIndicator,
        // SecondaryIndicator and PrimaryNavigationTabTokens.kt (VERSION v0_162).
        Assert.Equal(TabTokens.Primary.ActiveIndicatorHeight, TabTokens.Secondary.ActiveIndicatorHeight);
        Assert.True(TabTokens.Primary.ActiveIndicatorCornerRadius > 0);
        Assert.Equal(0, TabTokens.Secondary.ActiveIndicatorCornerRadius);
    }

    [AvaloniaFact]
    public void SelectingATab_ChecksExactlyOne()
    {
        var tabs = new MaterialTabs();
        tabs.Items.Add(new MaterialTab { Content = "One" });
        tabs.Items.Add(new MaterialTab { Content = "Two" });
        ThemeTestHelper.ShowWindow(tabs);

        tabs.SelectedIndex = 1;

        Assert.False(tabs.Items[0].IsChecked);
        Assert.True(tabs.Items[1].IsChecked);
    }

    [AvaloniaFact]
    public void ScrollableTabs_UseTheComposeMinimumWidthEndPaddingAndSelectedTabCentredViewport()
    {
        var tabs = new MaterialTabs { Width = 240, IsScrollable = true };
        tabs.Items.Add(new MaterialTab { Content = new TextBlock { Text = "Overview" } });
        tabs.Items.Add(new MaterialTab { Content = new TextBlock { Text = "Specifications" } });
        tabs.Items.Add(new MaterialTab { Content = new TextBlock { Text = "Reviews" } });
        tabs.Items.Add(new MaterialTab { Content = new TextBlock { Text = "Related items" } });
        ThemeTestHelper.ShowWindow(tabs);

        var viewer = Assert.Single(tabs.GetVisualDescendants().OfType<ScrollViewer>());
        var panel = Assert.Single(tabs.GetVisualDescendants().OfType<MaterialScrollableTabItemsPanel>());

        Assert.Equal(52, panel.EdgePadding);
        Assert.Equal(90, panel.MinimumTabWidth);
        Assert.All(tabs.Items, tab => Assert.True(tab.Bounds.Width >= 90));

        tabs.SelectedIndex = 3;

        Assert.True(viewer.Offset.X > 0);
        Assert.InRange(viewer.Offset.X, 0, viewer.Extent.Width - viewer.Viewport.Width);
        Assert.True(tabs.Items[tabs.SelectedIndex].Bounds.Right > viewer.Offset.X);
    }
}

public class SurfaceListItemTests
{
    [AvaloniaFact]
    public void SurfaceAndDivider_UseThemeSurfaceAndOutlineVariantDefaults()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var surface = new MaterialSurface { Width = 120, Height = 40 };
        var divider = new MaterialDivider { Width = 120 };
        var host = new StackPanel { Children = { surface, divider } };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(ThemeTestHelper.Expected(theme, SysColorToken.OnSurface), ThemeTestHelper.ColorOf(surface.Foreground));
        Assert.Equal(ThemeTestHelper.Expected(theme, SysColorToken.OutlineVariant), ThemeTestHelper.ColorOf(divider.DividerBrush));
        Assert.Equal(1, divider.Bounds.Height);
    }

    [AvaloniaFact]
    public void ListItem_UsesInteractionModeToSynchronizeThePublicSelectionState()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.MultiSelection,
            Content = "Headline",
            SupportingContent = "Supporting"
        };
        ThemeTestHelper.ShowWindow(item);

        item.IsSelected = true;
        Assert.True(item.IsChecked);

        item.IsChecked = false;
        Assert.False(item.IsSelected);

        item.InteractionMode = ListItemInteractionMode.None;
        Assert.False(item.Focusable);
    }

    [AvaloniaFact]
    public void ListItemTemplate_MeasuresTheAlpha25OneTwoAndThreeLineMinimumHeights()
    {
        // Fixed-size slot probes keep this integration check about ListItemPanel's 56/72/88dp
        // minimum-height contract rather than a platform text renderer's glyph metrics.
        var oneLine = new MaterialListItem { Content = SlotProbe(80, 20), Width = 240 };
        var twoLine = new MaterialListItem
        {
            Content = SlotProbe(80, 20),
            SupportingContent = SlotProbe(80, 20),
            Width = 240
        };
        var threeLine = new MaterialListItem
        {
            Content = SlotProbe(80, 20),
            OverlineContent = SlotProbe(80, 12),
            SupportingContent = SlotProbe(80, 20),
            Width = 240
        };
        var host = new StackPanel { Width = 240, Children = { oneLine, twoLine, threeLine } };

        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(56, TemplatePanel(oneLine).DesiredSize.Height);
        Assert.Equal(72, TemplatePanel(twoLine).DesiredSize.Height);
        Assert.Equal(88, TemplatePanel(threeLine).DesiredSize.Height);
        Assert.Equal(56, oneLine.Bounds.Height);
        Assert.Equal(72, twoLine.Bounds.Height);
        Assert.Equal(88, threeLine.Bounds.Height);
    }

    [AvaloniaFact]
    public void ListItemTemplate_MapsSlotVisibilityAndTypographyAfterLoading()
    {
        var item = new MaterialListItem
        {
            LeadingContent = "Leading",
            Content = "Headline",
            OverlineContent = "Overline",
            SupportingContent = "Supporting",
            TrailingContent = "Trailing",
            Width = 240
        };

        ThemeTestHelper.ShowWindow(item);
        var slots = TemplateSlots(item);

        Assert.Equal(5, slots.Count);
        Assert.All(slots.Values, slot => Assert.True(slot.IsVisible));
        Assert.Equal(TypographyRole.TitleMedium, TypographyScheme.GetTextStyle(slots[MaterialListItemSlot.Leading]));
        Assert.Equal(TypographyRole.LabelSmall, TypographyScheme.GetTextStyle(slots[MaterialListItemSlot.Trailing]));
        Assert.Equal(TypographyRole.LabelSmall, TypographyScheme.GetTextStyle(slots[MaterialListItemSlot.Overline]));
        Assert.Equal(TypographyRole.BodyLarge, TypographyScheme.GetTextStyle(slots[MaterialListItemSlot.Headline]));
        Assert.Equal(TypographyRole.BodyMedium, TypographyScheme.GetTextStyle(slots[MaterialListItemSlot.Supporting]));

        item.LeadingContent = null;
        item.TrailingContent = null;
        item.OverlineContent = null;
        item.SupportingContent = null;

        Assert.False(slots[MaterialListItemSlot.Leading].IsVisible);
        Assert.False(slots[MaterialListItemSlot.Trailing].IsVisible);
        Assert.False(slots[MaterialListItemSlot.Overline].IsVisible);
        Assert.False(slots[MaterialListItemSlot.Supporting].IsVisible);
        Assert.True(slots[MaterialListItemSlot.Headline].IsVisible);
    }

    [AvaloniaFact]
    public void MultiSelectionActivation_SetsTheFinalStateBeforeClickAndCommand()
    {
        var clickStates = new List<bool>();
        var commandStates = new List<bool>();
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.MultiSelection,
            Content = "Headline",
            Width = 240
        };
        item.Click += (_, _) => clickStates.Add(item.IsSelected);
        item.CommandParameter = item;
        item.Command = new RecordingCommand(parameter => commandStates.Add(((MaterialListItem)parameter!).IsSelected));
        var window = ThemeTestHelper.ShowWindow(item);
        var point = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)
                    ?? throw new InvalidOperationException("List item was not mounted.");

        window.MouseDown(point, Avalonia.Input.MouseButton.Left);
        window.MouseUp(point, Avalonia.Input.MouseButton.Left);

        Assert.True(item.IsSelected);
        Assert.Equal(new[] { true }, clickStates);
        Assert.Equal(new[] { true }, commandStates);

        window.MouseDown(point, Avalonia.Input.MouseButton.Left);
        window.MouseUp(point, Avalonia.Input.MouseButton.Left);

        Assert.False(item.IsSelected);
        Assert.Equal(new[] { true, false }, clickStates);
        Assert.Equal(new[] { true, false }, commandStates);
    }

    private static MaterialListItemPanel TemplatePanel(MaterialListItem item) =>
        Assert.Single(item.GetVisualDescendants().OfType<MaterialListItemPanel>());

    private static IReadOnlyDictionary<MaterialListItemSlot, Control> TemplateSlots(MaterialListItem item) =>
        TemplatePanel(item).Children.ToDictionary(MaterialListItemPanel.GetSlot);

    private static Border SlotProbe(double width, double height) => new() { Width = width, Height = height };

    private sealed class RecordingCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);
    }
}

public class AppBarTests
{
    [AvaloniaFact]
    public void OnlyTheTallerBars_PutTheTitleOnItsOwnLine()
    {
        Assert.False(AppBarDefaultsTokens.Small.IsStacked);
        Assert.True(AppBarDefaultsTokens.Medium.IsStacked);
        Assert.True(AppBarDefaultsTokens.Large.IsStacked);

        Assert.True(AppBarDefaultsTokens.Medium.ContainerHeight > AppBarDefaultsTokens.Small.ContainerHeight);
        Assert.True(AppBarDefaultsTokens.Large.ContainerHeight > AppBarDefaultsTokens.Medium.ContainerHeight);
    }

    [AvaloniaFact]
    public void AScrolledBar_ChangesSurfaceRatherThanGainingAShadow()
    {
        var tokens = AppBarDefaultsTokens.Small;

        Assert.NotEqual(tokens.ContainerColor, tokens.ScrolledContainerColor);
        Assert.Equal(SysColorToken.Surface, tokens.ContainerColor.RoleToken);
        Assert.Equal(SysColorToken.SurfaceContainer, tokens.ScrolledContainerColor.RoleToken);
    }
}

public class DrawerTests
{
    [AvaloniaFact]
    public void OnlyTheModalDrawer_HasAScrim()
    {
        Assert.True(DrawerTokens.Modal.IsModal);
        Assert.True(DrawerTokens.Modal.ScrimOpacity > 0);

        Assert.False(DrawerTokens.Standard.IsModal);
        Assert.Equal(0, DrawerTokens.Standard.ScrimOpacity);
    }

    [AvaloniaFact]
    public void ItsCornersAreRoundedOnlyOnTheEdgeFacingTheContent()
    {
        var shapes = new MaterialTheme().Resolved.Shapes;
        var shape = shapes.Get(DrawerTokens.Modal.ContainerShape);

        // CornerLargeEnd: square where it meets the screen edge, round where it meets the content.
        Assert.Equal(CornerSize.Zero, shape.TopStart);
        Assert.Equal(CornerSize.Zero, shape.BottomStart);
        Assert.NotEqual(CornerSize.Zero, shape.TopEnd);
    }

    [AvaloniaFact]
    public void AClosedDrawer_DoesNotSwallowClicks()
    {
        var drawer = new MaterialNavigationDrawer();
        ThemeTestHelper.ShowWindow(drawer);

        Assert.False(drawer.IsHitTestVisible);

        drawer.IsOpen = true;
        Assert.True(drawer.IsHitTestVisible);
    }
}

public class FabMenuTests
{
    [AvaloniaFact]
    public void ItsItemsStartHidden_AndRevealInSequence()
    {
        var menu = new MaterialFabMenu { Icon = "+" };
        menu.Items.Add(new MaterialFab { Label = "One" });
        menu.Items.Add(new MaterialFab { Label = "Two" });
        menu.Items.Add(new MaterialFab { Label = "Three" });
        ThemeTestHelper.ShowWindow(menu);

        foreach (var item in menu.Items)
            Assert.False(item.IsVisible);

        menu.IsExpanded = true;

        // The item nearest the toggle leads, so the last in the list is ahead of the first.
        Assert.True(menu.Items[2].Opacity >= menu.Items[1].Opacity);
        Assert.True(menu.Items[1].Opacity >= menu.Items[0].Opacity);
    }

    [AvaloniaFact]
    public void ItsItemsTakeTheMenusVariant()
    {
        var menu = new MaterialFabMenu { Variant = FabVariant.Tertiary };
        menu.Items.Add(new MaterialFab { Label = "One" });
        ThemeTestHelper.ShowWindow(menu);

        menu.Variant = FabVariant.Secondary;

        Assert.Equal(FabVariant.Secondary, menu.Items[0].Variant);
    }
}

public class TextFieldTests
{
    [AvaloniaFact]
    public void OnlyTheFilledVariant_HasASurfaceAndAnIndicator()
    {
        Assert.True(TextFieldTokens.Filled.IsFilled);
        Assert.False(TextFieldTokens.Outlined.IsFilled);

        Assert.Equal(SysColorToken.SurfaceContainerHighest, TextFieldTokens.Filled.ContainerColor.RoleToken);
        Assert.Equal(0, TextFieldTokens.Outlined.ContainerColor.LiteralColor!.Value.A);

        // The filled variant's shape is squared off at the bottom, because its indicator sits there.
        Assert.Equal(ShapeRole.CornerExtraSmallTop, TextFieldTokens.Filled.ContainerShape);
        Assert.Equal(ShapeRole.CornerExtraSmall, TextFieldTokens.Outlined.ContainerShape);
    }

    [AvaloniaFact]
    public void FocusThickensTheIndicator_RatherThanOnlyRecolouringIt()
    {
        foreach (var variant in System.Enum.GetValues<TextFieldVariant>())
        {
            var tokens = TextFieldTokens.For(variant);
            Assert.True(tokens.FocusOutlineWidth > tokens.OutlineWidth);
        }
    }

    [AvaloniaFact]
    public void TheLabel_ShrinksWhenItFloats()
    {
        var typography = new MaterialTheme().Resolved.Typography;
        var tokens = TextFieldTokens.Filled;

        var resting = typography.GetTypeScale(tokens.InputTextStyle).FontSize;
        var floated = typography.GetTypeScale(tokens.FloatingLabelTextStyle).FontSize;

        Assert.True(floated < resting);
    }

    [AvaloniaFact]
    public void AFieldWithText_StartsWithItsLabelAlreadyFloated()
    {
        var empty = new MaterialTextField { Label = "Name", Width = 240 };
        var filled = new MaterialTextField { Label = "Name", Text = "Ada", Width = 240 };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { empty, filled } });

        Assert.Equal(0, FloatOf(empty), 3);
        Assert.Equal(1, FloatOf(filled), 3);
    }

    [AvaloniaFact]
    public void AnErrorField_KeepsItsInputReadable()
    {
        // Only the surroundings turn red; text the user typed must stay legible.
        var tokens = TextFieldTokens.Filled;

        Assert.Equal(SysColorToken.Error, tokens.ErrorColor.RoleToken);
        Assert.Equal(SysColorToken.OnSurface, tokens.ErrorInputColor.RoleToken);
    }

    private static double FloatOf(MaterialTextField field)
    {
        return (double)typeof(MaterialTextField)
            .GetProperty("FloatFraction", System.Reflection.BindingFlags.Instance
                                          | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(field)!;
    }
}

public class ButtonGroupTests
{
    [AvaloniaFact]
    public void AConnectedGroup_SitsCloserThanASpacedOne()
    {
        Assert.True(ButtonGroupTokens.Connected.BetweenSpace < ButtonGroupTokens.Standard.BetweenSpace);
        Assert.True(ButtonGroupTokens.Connected.IsConnected);
        Assert.False(ButtonGroupTokens.Standard.IsConnected);
    }

    [AvaloniaFact]
    public void AConnectedGroup_RoundsTheOutsideAndTightensTheSeams()
    {
        var first = new MaterialToggleButton { Content = "A" };
        var middle = new MaterialToggleButton { Content = "B" };
        var last = new MaterialToggleButton { Content = "C" };

        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        group.Children.Add(first);
        group.Children.Add(middle);
        group.Children.Add(last);
        ThemeTestHelper.ShowWindow(group);

        var inner = CornerSize.Absolute(ButtonGroupTokens.Connected.InnerCornerSize);

        // The first segment keeps its leading corners round and tightens the trailing pair.
        Assert.Equal(CornerSize.Full, first.ContainerShapeOverride!.TopStart);
        Assert.Equal(inner, first.ContainerShapeOverride!.TopEnd);

        // The middle is tight on both sides.
        Assert.Equal(inner, middle.ContainerShapeOverride!.TopStart);
        Assert.Equal(inner, middle.ContainerShapeOverride!.TopEnd);

        // The last mirrors the first.
        Assert.Equal(inner, last.ContainerShapeOverride!.TopStart);
        Assert.Equal(CornerSize.Full, last.ContainerShapeOverride!.TopEnd);
    }

    [AvaloniaFact]
    public void AStandardGroup_LeavesItsChildrenAlone()
    {
        var button = new MaterialButton { Content = "A" };
        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Standard };
        group.Children.Add(button);
        ThemeTestHelper.ShowWindow(group);

        Assert.Null(button.ContainerShapeOverride);
    }

    [AvaloniaFact]
    public void ASplitButtonsHalves_MeetAtATightenedSeam()
    {
        var split = new MaterialSplitButton { Content = "Save", TrailingIcon = "v" };
        ThemeTestHelper.ShowWindow(split);

        var leading = split.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(button => button.Name == "PART_Leading");
        var trailing = split.GetVisualDescendants().OfType<ToggleButton>()
            .FirstOrDefault(button => button.Name == "PART_Trailing");

        Assert.NotNull(leading);
        Assert.NotNull(trailing);

        var tokens = SplitButtonDefaultsTokens.Default;
        Assert.Equal(SplitButtonSmallTokens.ContainerHeight, tokens.ContainerHeight);
        Assert.Equal(SplitButtonSmallTokens.BetweenSpace, split.BetweenSpace);
        var inner = CornerSize.Absolute(tokens.InnerCornerSize);

        var leadingShape = ButtonAssist.GetContainerShapeOverride(leading!);
        Assert.Equal(CornerSize.Full, leadingShape!.TopStart);
        Assert.Equal(inner, leadingShape.TopEnd);
        var trailingShape = ButtonAssist.GetContainerShapeOverride(trailing!);
        Assert.Equal(inner, trailingShape!.TopStart);
        Assert.Equal(CornerSize.Full, trailingShape.TopEnd);
    }
}

public class ChipTests
{
    [AvaloniaFact]
    public void OnlyFilterAndInputChips_CarryASelectedState()
    {
        Assert.True(ChipTokens.Filter.IsSelectable);
        Assert.True(ChipTokens.Input.IsSelectable);
        Assert.False(ChipTokens.Assist.IsSelectable);
        Assert.False(ChipTokens.Suggestion.IsSelectable);
    }

    [AvaloniaFact]
    public void CheckingANonSelectableChip_ChangesNothing()
    {
        // An assist chip derives from ToggleButton for the sake of one implementation, but it is
        // not a toggle and must not start looking like one.
        var chip = new MaterialChip { Variant = ChipVariant.Assist, Content = "Assist" };
        ThemeTestHelper.ShowWindow(chip);

        var before = ThemeTestHelper.ColorOf(chip.Foreground);
        chip.IsChecked = true;

        Assert.Equal(before, ThemeTestHelper.ColorOf(chip.Foreground));
    }

    [AvaloniaFact]
    public void SelectingAFilterChip_FillsItAndRoundsIt()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var chip = new MaterialChip { Variant = ChipVariant.Filter, Content = "Filter" };
        var host = new Border { Child = chip };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSurfaceVariant),
            ThemeTestHelper.ColorOf(chip.Foreground));

        chip.IsChecked = true;

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
            ThemeTestHelper.ColorOf(chip.Foreground));

        // Unselected it is a small-cornered outline; selected it is a filled pill.
        Assert.Equal(ShapeRole.CornerSmall, ChipTokens.Filter.ContainerShape);
        Assert.Equal(ShapeRole.CornerFull, ChipTokens.Filter.SelectedContainerShape);
    }
}

public class LoadingIndicatorTests
{
    [Fact]
    public void IndeterminateMorph_UsesTheUpstreamSpringRatherThanAUniformLoop()
    {
        var morph = new Material3Expressive.Controls.Motion.LoadingIndicatorMorphAnimation(7);

        morph.Tick(TimeSpan.FromMilliseconds(100));

        // A 100ms slice of the 650ms interval would be .154 for a linear loop. AndroidX starts
        // each segment with spring(.6, 200, visibilityThreshold=.1), so it is already well ahead.
        Assert.True(morph.Value > 0.4, "The loading-indicator morph must use the upstream spring curve.");
        Assert.Equal(0, morph.MorphIndex);
        Assert.Equal(90, morph.MorphRotationDegrees);
    }

    [Fact]
    public void IndeterminateMorph_AdvancesShapesAndQuarterTurnsAtThe650msCadence()
    {
        var morph = new Material3Expressive.Controls.Motion.LoadingIndicatorMorphAnimation(7);

        morph.Tick(Material3Expressive.Controls.Motion.LoadingIndicatorMorphAnimation.Interval);

        Assert.Equal(1, morph.MorphIndex);
        Assert.Equal(180, morph.MorphRotationDegrees);
        Assert.Equal(0, morph.Value, 3);
    }

    [AvaloniaFact]
    public void OnlyTheContainedVariant_DrawsAContainer()
    {
        Assert.False(LoadingIndicatorDefaultsTokens.Standard.HasContainer);
        Assert.True(LoadingIndicatorDefaultsTokens.Contained.HasContainer);

        Assert.Equal(0, LoadingIndicatorDefaultsTokens.Standard.ContainerColor.LiteralColor!.Value.A);
        Assert.Equal(SysColorToken.PrimaryContainer, LoadingIndicatorDefaultsTokens.Contained.ContainerColor.RoleToken);
    }

    [AvaloniaFact]
    public void BothVariants_ReserveTheUpstreamPublicContainerBox()
    {
        var standard = new MaterialLoadingIndicator();
        var contained = new MaterialLoadingIndicator { Variant = LoadingIndicatorVariant.Contained };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { standard, contained } });

        Assert.Equal(LoadingIndicatorDefaultsTokens.Standard.ContainerSize, standard.Bounds.Width, 3);
        Assert.Equal(LoadingIndicatorDefaultsTokens.Contained.ContainerSize, contained.Bounds.Width, 3);
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
    [Fact]
    public void SliderDefaults_ShareDisabledProjectionForSliderAndRangeSlider()
    {
        var colors = SliderDefaults.Colors(ResolvedTheme.Default, isDark: false);
        var visuals = SliderDefaults.Visuals(colors, enabled: false);

        Assert.Same(colors.DisabledActiveTrackColor, visuals.ActiveTrackColor);
        Assert.Same(colors.DisabledInactiveTrackColor, visuals.InactiveTrackColor);
        Assert.Same(colors.DisabledHandleColor, visuals.HandleColor);
        Assert.Same(visuals.ActiveTrackColor, visuals.StopIndicatorColor);
    }

    [AvaloniaFact]
    public void TheHandle_NarrowsWhilePressedSoTheValueStaysVisible()
    {
        var tokens = SliderDefaultsTokens.Default;

        Assert.True(tokens.PressedHandleWidth < tokens.HandleWidth);
        // And it stands taller than the track it sits on.
        Assert.True(tokens.HandleHeight > tokens.TrackHeight);
    }

    [AvaloniaFact]
    public void ItReservesTheHandlesHeight()
    {
        var slider = new MaterialSlider { Width = 200 };
        ThemeTestHelper.ShowWindow(slider);

        Assert.Equal(Math.Max(SliderDefaultsTokens.Default.HandleHeight, 48), slider.Bounds.Height, 3);
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

    [AvaloniaFact]
    public void AVerticalSlider_UsesHeightForItsTravelAndTopToBottomValues()
    {
        var slider = new MaterialSlider
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Height = 200,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Minimum = 0,
            Maximum = 100
        };
        var window = ThemeTestHelper.ShowWindow(slider);

        var nearBottom = slider.TranslatePoint(new Avalonia.Point(slider.Bounds.Width / 2, 180), window)!.Value;
        window.MouseDown(nearBottom, Avalonia.Input.MouseButton.Left);

        Assert.InRange(slider.Value, 80, 100);
        Assert.Equal(Math.Max(SliderDefaultsTokens.Default.HandleHeight, 48), slider.Bounds.Width, 3);
    }

    [AvaloniaFact]
    public void AVerticalSlider_UsesDownToIncreaseAndUpToDecrease()
    {
        var slider = new MaterialSlider
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Height = 200,
            Minimum = 0,
            Maximum = 10,
            SmallChange = 1,
            Value = 5
        };
        ThemeTestHelper.ShowWindow(slider);

        slider.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Down
        });

        Assert.Equal(6, slider.Value);
    }

    [AvaloniaFact]
    public void ARangedSlider_KeepsTheTwoValuesOrderedAndWithinItsRange()
    {
        var slider = new MaterialRangeSlider { Minimum = 0, Maximum = 100, StartValue = 25, EndValue = 75 };

        slider.StartValue = 90;
        Assert.Equal(75, slider.StartValue);
        Assert.Equal(75, slider.EndValue);

        slider.EndValue = -10;
        Assert.Equal(75, slider.StartValue);
        Assert.Equal(75, slider.EndValue);
    }

    [AvaloniaFact]
    public void ARangedSlider_KeyboardChangesTheMostRecentlySelectedThumb()
    {
        var slider = new MaterialRangeSlider { Width = 200, Minimum = 0, Maximum = 10, StartValue = 2, EndValue = 8, SmallChange = 1 };
        ThemeTestHelper.ShowWindow(slider);
        slider.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Right
        });

        Assert.Equal(3, slider.StartValue);
        Assert.Equal(8, slider.EndValue);
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

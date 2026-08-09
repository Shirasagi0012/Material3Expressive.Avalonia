using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>Native-first contracts shared by ToggleButton and icon buttons.</summary>
public class NativeToggleAndIconButtonTests
{
    [AvaloniaFact]
    public void NativeToggleButton_UsesTheMaterialThemeAndLiveAttachedState()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new ToggleButton { Content = "Toggle" };
        M3Button.SetVariant(button, ButtonVariant.Tonal);
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        Assert.Single(button.GetVisualDescendants().OfType<MaterialToggleButtonContainerPresenter>());
        Assert.Equal(48, button.MinHeight);
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
            ThemeTestHelper.ColorOf(button.Foreground));

        button.IsChecked = true;

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSecondary),
            ThemeTestHelper.ColorOf(button.Foreground));
    }

    [AvaloniaFact]
    public void ToggleDefaultsAdapter_ProjectsCheckedStateAndIndependentSemanticProfiles()
    {
        var toggle = new ToggleButton { Content = "Toggle" };
        ThemeTestHelper.ShowWindow(toggle);

        var colors = Assert.IsType<ToggleButtonColors>(M3ToggleButton.GetDefaultColors(toggle));
        var metrics = Assert.IsType<ToggleButtonMetrics>(M3ToggleButton.GetDefaultMetrics(toggle));

        Assert.Equal(ButtonSize.Small, M3Button.GetSize(toggle));
        Assert.Equal(40, metrics.ContainerHeight);
        Assert.Equal(48, metrics.ToggleContainerHeight);
        Assert.Same(colors.UncheckedContent, M3ToggleButton.GetDefaultStateLayerBrush(toggle));

        M3Button.SetVariant(toggle, ButtonVariant.Outlined);
        Assert.Equal(new Thickness(1), M3ToggleButton.GetDefaultOutlineThickness(toggle));

        toggle.IsChecked = true;

        var checkedColors = Assert.IsType<ToggleButtonColors>(M3ToggleButton.GetDefaultColors(toggle));
        Assert.Same(checkedColors.CheckedContent, M3ToggleButton.GetDefaultStateLayerBrush(toggle));
        Assert.Equal(new Thickness(0), M3ToggleButton.GetDefaultOutlineThickness(toggle));
    }

    [AvaloniaFact]
    public void ToggleDefaultsAdapter_ClearsItsProjectionWhenTheMaterialMarkerIsRemoved()
    {
        var toggle = new ToggleButton { Content = "Toggle" };
        ThemeTestHelper.ShowWindow(toggle);

        Assert.NotNull(M3ToggleButton.GetDefaultColors(toggle));

        toggle.SetValue(M3ToggleButton.UseMaterialDefaultsProperty, false);

        Assert.Null(M3ToggleButton.GetDefaultColors(toggle));
        Assert.Null(M3ToggleButton.GetDefaultMetrics(toggle));
        Assert.Null(M3ToggleButton.GetDefaultShapes(toggle));
    }

    [AvaloniaFact]
    public void ToggleApplicationStyle_OverridesTheLibraryDefaultsProjection()
    {
        var style = new Style(selector => selector.Is<ToggleButton>());
        style.Add(new Setter(ToggleButton.BackgroundProperty, Brushes.HotPink));
        var checkedStyle = new Style(selector => selector.Is<ToggleButton>().Class(":checked"));
        checkedStyle.Add(new Setter(ToggleButton.BackgroundProperty, Brushes.LimeGreen));
        Application.Current!.Styles.Add(style);
        Application.Current.Styles.Add(checkedStyle);

        try
        {
            var toggle = new ToggleButton { Content = "Toggle" };
            ThemeTestHelper.ShowWindow(toggle);

            Assert.Same(Brushes.HotPink, toggle.Background);

            M3Button.SetVariant(toggle, ButtonVariant.Tonal);
            toggle.IsChecked = true;

            Assert.Same(Brushes.LimeGreen, toggle.Background);
        }
        finally
        {
            Application.Current!.Styles.Remove(checkedStyle);
            Application.Current!.Styles.Remove(style);
        }
    }

    [AvaloniaFact]
    public void NativeIconButton_UsesAnExplicitThemeAndLiveSizingAxes()
    {
        var button = new Button
        {
            Content = "icon",
            Theme = Theme("MaterialTonalIconButtonTheme")
        };
        M3IconButton.SetSize(button, IconButtonSize.Medium);
        M3IconButton.SetButtonWidth(button, IconButtonWidth.Narrow);
        ThemeTestHelper.ShowWindow(button);

        Assert.True(M3IconButton.GetIsIconButton(button));
        Assert.Single(button.GetVisualDescendants().OfType<MaterialIconButtonContainerPresenter>());
        Assert.Equal(MediumIconButtonTokens.ContainerHeight, button.Height);
        Assert.Equal(MediumIconButtonTokens.IconSize + MediumIconButtonTokens.NarrowLeadingSpace * 2, button.Width);
        Assert.Equal(MediumIconButtonTokens.NarrowLeadingSpace, button.Padding.Left);
        Assert.Equal(MediumIconButtonTokens.IconSize, M3IconButton.GetIconSize(button));

        M3IconButton.SetButtonWidth(button, IconButtonWidth.Wide);

        Assert.Equal(MediumIconButtonTokens.WideLeadingSpace, button.Padding.Left);
    }

    [AvaloniaFact]
    public void NativeToggleIconButton_UsesNativeCheckedStateForItsColoursAndShape()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new ToggleButton
        {
            Content = "icon",
            Theme = Theme("MaterialFilledToggleIconButtonTheme")
        };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        var presenter = Assert.Single(
            button.GetVisualDescendants().OfType<MaterialIconButtonContainerPresenter>());
        var unselectedCorner = presenter.CurrentCornerRadius.TopLeft;
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSurfaceVariant),
            ThemeTestHelper.ColorOf(button.Foreground));

        button.IsChecked = true;
        Assert.IsType<MaterialInteractionController>(presenter.Interaction).Motion.SettleAll();

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary),
            ThemeTestHelper.ColorOf(button.Foreground));
        Assert.NotEqual(unselectedCorner, presenter.CurrentCornerRadius.TopLeft);
    }

    [AvaloniaFact]
    public void IconThemes_DoNotTurnOrdinaryButtonsIntoIconButtons()
    {
        var ordinary = new Button { Content = "Action" };
        ThemeTestHelper.ShowWindow(ordinary);

        Assert.False(M3IconButton.GetIsIconButton(ordinary));
        Assert.Empty(ordinary.GetVisualDescendants().OfType<MaterialIconButtonContainerPresenter>());
    }

    [AvaloniaFact]
    public void NativeToggleIconButton_UsesTheSharedM3IconButtonAxes()
    {
        var toggle = new ToggleButton
        {
            Theme = Theme("MaterialOutlinedToggleIconButtonTheme"),
            IsChecked = true
        };
        M3IconButton.SetVariant(toggle, IconButtonVariant.Outlined);
        M3IconButton.SetSize(toggle, IconButtonSize.Large);
        M3IconButton.SetButtonWidth(toggle, IconButtonWidth.Wide);
        ThemeTestHelper.ShowWindow(toggle);

        Assert.Equal(IconButtonVariant.Outlined, M3IconButton.GetVariant(toggle));
        Assert.Equal(IconButtonSize.Large, M3IconButton.GetSize(toggle));
        Assert.Equal(IconButtonWidth.Wide, M3IconButton.GetButtonWidth(toggle));
        Assert.Equal(
            LargeIconButtonTokens.IconSize + LargeIconButtonTokens.WideLeadingSpace * 2,
            toggle.Width);
        Assert.Equal(
            LargeIconButtonTokens.WideLeadingSpace,
            Assert.IsType<IconButtonMetrics>(M3ToggleIconButton.GetDefaultMetrics(toggle)).ContentPadding.Left);
        Assert.Single(toggle.GetVisualDescendants().OfType<MaterialIconButtonContainerPresenter>());
    }

    [AvaloniaFact]
    public void Buttons_KeepTheirTokenHeightNextToTallerSiblings()
    {
        var textButton = new Button { Content = "Native button" };
        var iconButton = new Button
        {
            Theme = Theme("MaterialTonalIconButtonTheme"),
            Content = "i",
        };
        var checkbox = new MaterialCheckbox { Content = "Unbounded ripple" };
        var panel = new WrapPanel
        {
            ItemSpacing = 20,
            LineSpacing = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { textButton, iconButton, checkbox },
        };
        var window = ThemeTestHelper.ShowWindow(panel);

        Assert.Equal(48, checkbox.Bounds.Height);
        Assert.Equal(40, textButton.Bounds.Height);
        Assert.Equal(4, textButton.Bounds.Y);
        Assert.Equal(40, iconButton.Bounds.Height);
        Assert.Equal(40, iconButton.Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void ConnectedButtonGroup_SegmentsStayContiguousInATallerSlot()
    {
        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        group.Children.Add(new ToggleButton { Content = "Day" });
        group.Children.Add(new ToggleButton { Content = "Week" });
        var host = new Border { Child = group, Height = 48 };
        var window = ThemeTestHelper.ShowWindow(host);

        Assert.Equal(48, group.Bounds.Height);
        Assert.Equal(48, group.Children[0].Bounds.Height);
        Assert.Equal(48, group.Children[1].Bounds.Height);
        Assert.Equal(0, group.Children[0].Bounds.Y);
        Assert.Equal(0, group.Children[1].Bounds.Y);
        window.Close();
    }

    [AvaloniaFact]
    public void NativePropertyOverrides_SurviveIconTokenChanges()
    {
        var background = Brushes.HotPink;
        var button = new Button
        {
            Content = "icon",
            Theme = Theme("MaterialFilledIconButtonTheme"),
            Background = background,
            Padding = new Thickness(3)
        };
        ThemeTestHelper.ShowWindow(button);

        M3IconButton.SetVariant(button, IconButtonVariant.Tonal);
        M3IconButton.SetSize(button, IconButtonSize.Large);
        M3IconButton.SetButtonWidth(button, IconButtonWidth.Wide);

        Assert.Same(background, button.Background);
        Assert.Equal(new Thickness(3), button.Padding);
    }

    private static ControlTheme Theme(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, out var resource));
        return Assert.IsType<ControlTheme>(resource);
    }

}

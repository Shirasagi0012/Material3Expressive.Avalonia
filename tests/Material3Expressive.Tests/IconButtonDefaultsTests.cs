using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>Contracts for the typed IconButton MaterialValue projection.</summary>
public sealed class IconButtonDefaultsTests
{
    [AvaloniaFact]
    public void TypedGeometryEndpoint_ProjectsRawSizeAndWidthFacts()
    {
        var button = new Button { Theme = Theme("MaterialTonalIconButtonTheme") };
        M3IconButton.SetSize(button, IconButtonSize.Medium);
        M3IconButton.SetButtonWidth(button, IconButtonWidth.Narrow);

        var window = ThemeTestHelper.ShowWindow(button);

        var geometry = button.GetValue(M3IconButton.GeometryProfileProperty);
        Assert.Equal(56, geometry.ContainerHeight);
        Assert.Equal(24, geometry.IconSize);
        Assert.Equal(new Thickness(12, 0), geometry.ContentPadding);
        Assert.Equal(48, geometry.ContainerWidth);
        Assert.Equal(geometry.ContainerHeight, button.Height);
        Assert.Equal(geometry.ContainerWidth, button.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void TypedVisualEndpoint_RefreshesWhenThemeVariantAndVariantChange()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new Button { Theme = Theme("MaterialFilledIconButtonTheme") };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        var window = ThemeTestHelper.ShowWindow(host);

        var light = ThemeTestHelper.ColorOf(
            button.GetValue(M3IconButton.VisualColorsProperty).IconColor);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        var dark = ThemeTestHelper.ColorOf(
            button.GetValue(M3IconButton.VisualColorsProperty).IconColor);

        Assert.NotEqual(light, dark);
        Assert.Equal(ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary, isDark: true), dark);

        M3IconButton.SetVariant(button, IconButtonVariant.Outlined);
        var outlined = button.GetValue(M3IconButton.VisualColorsProperty);
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OutlineVariant, isDark: true),
            ThemeTestHelper.ColorOf(outlined.OutlineColor));
        Assert.Equal(new Thickness(1), button.BorderThickness);
        window.Close();
    }

    [AvaloniaFact]
    public void ToggleVisualEndpoint_SeparatesCheckedColoursAndOutlineState()
    {
        var toggle = new ToggleButton { Theme = Theme("MaterialOutlinedToggleIconButtonTheme") };
        var window = ThemeTestHelper.ShowWindow(toggle);

        var initial = toggle.GetValue(M3ToggleIconButton.VisualColorsProperty);
        Assert.Equal(new Thickness(1), toggle.BorderThickness);
        Assert.Equal(
            ThemeTestHelper.ColorOf(initial.UncheckedIconColor),
            ThemeTestHelper.ColorOf(toggle.Foreground));

        toggle.IsChecked = true;

        var selected = toggle.GetValue(M3ToggleIconButton.VisualColorsProperty);
        Assert.Equal(new Thickness(0), toggle.BorderThickness);
        Assert.Equal(
            ThemeTestHelper.ColorOf(selected.CheckedIconColor),
            ThemeTestHelper.ColorOf(toggle.Foreground));
        Assert.Same(selected.CheckedStateLayerColor, M3ToggleIconButton.GetStateLayerBrush(toggle));
        window.Close();
    }

    [AvaloniaFact]
    public void ApplicationStyle_OverridesIconButtonLibraryProjection()
    {
        var style = new Style(selector => selector.Is<Button>());
        style.Add(new Setter(Button.BackgroundProperty, Brushes.HotPink));
        Application.Current!.Styles.Add(style);

        try
        {
            var button = new Button { Theme = Theme("MaterialFilledIconButtonTheme") };
            var window = ThemeTestHelper.ShowWindow(button);
            Assert.Same(Brushes.HotPink, button.Background);

            M3IconButton.SetVariant(button, IconButtonVariant.Tonal);
            Assert.Same(Brushes.HotPink, button.Background);
            window.Close();
        }
        finally
        {
            Application.Current.Styles.Remove(style);
        }
    }

    [AvaloniaFact]
    public void FlatGeometryEndpointOverride_IsConsumedByThemeBindings()
    {
        var button = new Button { Theme = Theme("MaterialFilledIconButtonTheme") };
        var window = ThemeTestHelper.ShowWindow(button);

        var geometry = new IconButtonGeometryProfile(
            44,
            28,
            new Thickness(9, 0),
            46,
            3,
            ShapeRole.CornerMedium,
            ShapeRole.CornerSmall,
            ShapeRole.CornerLarge,
            ShapeRole.CornerMedium,
            ShapeRole.CornerFull);
        button.SetValue(M3IconButton.GeometryProfileProperty, geometry);

        Assert.Same(geometry, button.GetValue(M3IconButton.GeometryProfileProperty));
        Assert.Equal(44, button.Height);
        Assert.Equal(46, button.Width);
        Assert.Equal(new Thickness(9, 0), button.Padding);
        Assert.Equal(28, M3IconButton.GetIconSize(button));
        window.Close();
    }

    private static ControlTheme Theme(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, out var resource));
        return Assert.IsType<ControlTheme>(resource);
    }
}

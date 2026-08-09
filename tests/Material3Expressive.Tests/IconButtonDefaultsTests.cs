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

/// <summary>Contracts for the component-owned IconButton defaults projection.</summary>
public sealed class IconButtonDefaultsTests
{
    [AvaloniaFact]
    public void Adapter_ProjectsRawSizeAndWidthFactsToSemanticMetrics()
    {
        var button = new Button { Theme = Theme("MaterialTonalIconButtonTheme") };
        M3IconButton.SetSize(button, IconButtonSize.Medium);
        M3IconButton.SetButtonWidth(button, IconButtonWidth.Narrow);

        var window = ThemeTestHelper.ShowWindow(button);

        var metrics = Assert.IsType<IconButtonMetrics>(M3IconButton.GetDefaultMetrics(button));
        Assert.Equal(56, metrics.ContainerHeight);
        Assert.Equal(24, metrics.IconSize);
        Assert.Equal(new Thickness(12, 0), metrics.ContentPadding);
        Assert.Equal(48, metrics.ContainerWidth);
        Assert.Equal(metrics.ContainerHeight, button.Height);
        Assert.Equal(metrics.ContainerWidth, button.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void Adapter_RefreshesColorProfileWhenThemeVariantAndVariantChange()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new Button { Theme = Theme("MaterialFilledIconButtonTheme") };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        var window = ThemeTestHelper.ShowWindow(host);

        var light = ThemeTestHelper.ColorOf(
            Assert.IsType<IconButtonColors>(M3IconButton.GetDefaultColors(button)).Icon);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        var dark = ThemeTestHelper.ColorOf(
            Assert.IsType<IconButtonColors>(M3IconButton.GetDefaultColors(button)).Icon);

        Assert.NotEqual(light, dark);
        Assert.Equal(ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary, isDark: true), dark);

        M3IconButton.SetVariant(button, IconButtonVariant.Outlined);
        var outlined = Assert.IsType<IconButtonColors>(M3IconButton.GetDefaultColors(button));
        Assert.True(outlined.HasOutline);
        Assert.Equal(new Thickness(1), M3IconButton.GetDefaultOutlineThickness(button));
        window.Close();
    }

    [AvaloniaFact]
    public void ToggleAdapter_SeparatesCheckedColoursAndOutlineState()
    {
        var toggle = new ToggleButton { Theme = Theme("MaterialOutlinedToggleIconButtonTheme") };
        var window = ThemeTestHelper.ShowWindow(toggle);

        var initial = Assert.IsType<ToggleIconButtonColors>(
            M3ToggleIconButton.GetDefaultColors(toggle));
        Assert.Equal(new Thickness(1), M3ToggleIconButton.GetDefaultOutlineThickness(toggle));
        Assert.Equal(
            ThemeTestHelper.ColorOf(initial.UncheckedIcon),
            ThemeTestHelper.ColorOf(toggle.Foreground));

        toggle.IsChecked = true;

        var selected = Assert.IsType<ToggleIconButtonColors>(
            M3ToggleIconButton.GetDefaultColors(toggle));
        Assert.Equal(new Thickness(0), M3ToggleIconButton.GetDefaultOutlineThickness(toggle));
        Assert.Equal(
            ThemeTestHelper.ColorOf(selected.CheckedIcon),
            ThemeTestHelper.ColorOf(toggle.Foreground));
        Assert.Same(selected.CheckedStateLayer, M3ToggleIconButton.GetDefaultStateLayerBrush(toggle));
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
    public void Adapter_ClearsItsProjectionWhenMaterialMarkerIsRemoved()
    {
        var button = new Button { Theme = Theme("MaterialFilledIconButtonTheme") };
        var window = ThemeTestHelper.ShowWindow(button);

        Assert.NotNull(M3IconButton.GetDefaultColors(button));
        button.SetValue(M3IconButton.UseMaterialDefaultsProperty, false);

        Assert.Null(M3IconButton.GetDefaultColors(button));
        Assert.Null(M3IconButton.GetDefaultMetrics(button));
        Assert.Null(M3IconButton.GetDefaultShapes(button));
        window.Close();
    }

    private static ControlTheme Theme(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, out var resource));
        return Assert.IsType<ControlTheme>(resource);
    }
}

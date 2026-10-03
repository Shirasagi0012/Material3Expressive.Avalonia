using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

/// <summary>
/// The style axis applied: each named ControlTheme reproduces its baseline colors (and the
/// elevated elevation / outlined border), inherited from the filled theme through BasedOn. Also
/// the size axis: a size class re-points the metric slots live, and removing it reverts.
/// </summary>
public class ButtonVariantThemeTests
{
    private static MaterialSurface Container(Button button) =>
        button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");

    private static Color BrushColor(IBrush? brush) => Assert.IsType<ImmutableSolidColorBrush>(brush).Color;

    [AvaloniaFact]
    public void ElevatedTheme_AppliesSurfaceAndElevation()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialElevatedButtonTheme");

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.SurfaceContainerLow),
            BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(button.Foreground));

        // The resting elevation aliases the sys.elevation level-1 DIP onto MaterialSurface.
        var expected = Assert.IsType<double>(TokenScan.Resolve(window, Sys.Elevation.Level1));
        Assert.Equal(expected, Container(button).ShadowElevation);
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(Container(button).StateLayerBrush));
    }

    [AvaloniaFact]
    public void TonalTheme_AppliesSecondaryContainer()
    {
        ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialTonalButtonTheme");

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.SecondaryContainer),
            BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSecondaryContainer),
            BrushColor(button.Foreground));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSecondaryContainer),
            BrushColor(Container(button).StateLayerBrush));
        // The tonal style rests at Level0 and rises to Level1 on hover.
        Assert.Equal(0d, Container(button).ShadowElevation);
    }

    [AvaloniaFact]
    public void OutlinedTheme_AppliesTransparentContainerAndOutline()
    {
        ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialOutlinedButtonTheme");

        Assert.Equal(Colors.Transparent, BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSurfaceVariant),
            BrushColor(button.Foreground));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OutlineVariant),
            BrushColor(button.BorderBrush));
        Assert.Equal(1d, button.BorderThickness.Left);
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSurfaceVariant),
            BrushColor(Container(button).StateLayerBrush));
    }

    [AvaloniaFact]
    public void TextTheme_AppliesTransparentContainerAndPrimaryContent()
    {
        ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialTextButtonTheme");

        Assert.Equal(Colors.Transparent, BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(button.Foreground));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(Container(button).StateLayerBrush));
    }

    [AvaloniaFact]
    public void SizeClass_RePointsMetrics_AndReverts()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        Assert.Equal(40d, button.GetVisualDescendants().OfType<ButtonLayout>().Single().ContainerMinHeight);

        button.Classes.Add("large");

        Assert.Equal(96d, button.GetVisualDescendants().OfType<ButtonLayout>().Single().ContainerMinHeight);
        Assert.Equal(new Thickness(48, 32), button.Padding);
        Assert.Equal(
            Assert.IsType<double>(TokenScan.Resolve(window, Sys.TypeScale.HeadlineSmall.Size)),
            button.FontSize);
        Assert.Equal(
            Assert.IsType<FontWeight>(TokenScan.Resolve(window, Sys.TypeScale.HeadlineSmall.Weight)),
            button.FontWeight);

        // The template part bindings are re-pointed too (class styles run at StyleTrigger
        // priority, beating the template's inline bindings).
        Assert.Equal(new CornerRadius(1000), Container(button).CornerRadius);
        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().First();
        Assert.Equal(
            Assert.IsType<double>(TokenScan.Resolve(window, Sys.TypeScale.HeadlineSmall.LineHeight)),
            presenter.LineHeight);

        button.Classes.Remove("large");
        Assert.Equal(40d, button.GetVisualDescendants().OfType<ButtonLayout>().Single().ContainerMinHeight);
        Assert.Equal(14d, button.FontSize);
    }

    [AvaloniaFact]
    public void OutlinedTheme_SizeClass_UsesComposableOutlineDefault_AndKeepsLocalOverride()
    {
        ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialOutlinedButtonTheme", sizeClass: "xlarge");
        Assert.Equal(1d, button.BorderThickness.Left);
        button.BorderThickness = new Thickness(3);
        button.Classes.Remove("xlarge");
        button.Classes.Add("large");
        Assert.Equal(3d, button.BorderThickness.Left);
        button.Classes.Remove("large");
        button.Classes.Add("xlarge");
        Assert.Equal(136d, button.GetVisualDescendants().OfType<ButtonLayout>().Single().ContainerMinHeight);
    }
}

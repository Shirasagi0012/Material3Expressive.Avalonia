using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The style axis applied: each named ControlTheme reproduces its baseline colors (and the
/// elevated shadow / outlined border), inherited from the filled theme through BasedOn. Also the
/// size axis: a size class re-points the metric slots live, and removing it reverts.
/// </summary>
public class ButtonVariantThemeTests
{
    private static Border Container(Button button) =>
        button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Container");

    private static Border StateLayer(Button button) =>
        button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_StateLayer");

    private static Color BrushColor(IBrush? brush) => Assert.IsType<ImmutableSolidColorBrush>(brush).Color;

    [AvaloniaFact]
    public void ElevatedTheme_AppliesSurfaceAndShadow()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialElevatedButtonTheme");

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.SurfaceContainerLow),
            BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(button.Foreground));

        // Resting shadow aliases the baked level-1 recipe.
        var expected = Assert.IsType<BoxShadows>(TokenScan.Resolve(window, Sys.Elevation.Level1Shadow));
        Assert.Equal(expected, Container(button).BoxShadow);
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(StateLayer(button).Background));
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
            BrushColor(StateLayer(button).Background));
        // No shadow slots on the tonal style: the container stays flat.
        Assert.Equal(default, Container(button).BoxShadow);
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
            BrushColor(StateLayer(button).Background));
    }

    [AvaloniaFact]
    public void TextTheme_AppliesTransparentContainerAndPrimaryContent()
    {
        ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialTextButtonTheme");

        Assert.Equal(Colors.Transparent, BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(button.Foreground));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(StateLayer(button).Background));
    }

    [AvaloniaFact]
    public void SizeClass_RePointsMetrics_AndReverts()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        Assert.Equal(40d, button.MinHeight);

        button.Classes.Add("large");

        Assert.Equal(96d, button.MinHeight);
        Assert.Equal(new Thickness(48, 0, 48, 0), button.Padding);
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
        Assert.Equal(40d, button.MinHeight);
        Assert.Equal(14d, button.FontSize);
    }

    [AvaloniaFact]
    public void OutlinedTheme_SizeClass_ThickensOutline()
    {
        ButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialOutlinedButtonTheme", sizeClass: "xlarge");
        Assert.Equal(3d, button.BorderThickness.Left);
        Assert.Equal(136d, button.MinHeight);
    }
}

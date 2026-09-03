using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3ExpressiveRefactor.Baseline;
using Material3ExpressiveRefactor.Foundation.Rendering;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The standalone MaterialSurface: the implicit {x:Type} theme paints the baseline surface and
/// content colors, and the part's own defaults (no shadow, no state layer) stay flat until a
/// component theme drives them. Also the regression guard for hosting it next to Button: the
/// implicit theme must not bleed into component templates.
/// </summary>
public class MaterialSurfaceTests
{
    private static Avalonia.Styling.Styles LoadStyles(string name) =>
        Assert.IsAssignableFrom<Avalonia.Styling.Styles>(
            AvaloniaXamlLoader.Load(new Uri($"avares://Material3ExpressiveRefactor/Styles/{name}")));

    private static Color BrushColor(IBrush? brush) => Assert.IsType<ImmutableSolidColorBrush>(brush).Color;

    private static Window ShowSurface(out MaterialSurface surface, out TextBlock text)
    {
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(new MaterialResource
        {
            Children = { new ColorScheme() },
        });

        // A Window roots itself at construction, so styles must be in place before Content is set.
        var window = new Window
        {
            Resources = resources,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Styles.Add(LoadStyles("Surface.axaml"));

        text = new TextBlock { Text = "Hello" };
        surface = new MaterialSurface { Child = text };
        window.Content = surface;
        window.Show();
        return window;
    }

    [AvaloniaFact]
    public void ImplicitTheme_AppliesSurfaceAndContentColors()
    {
        ShowSurface(out var surface, out var text);

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Surface),
            BrushColor(surface.Background));
        // The content color arrives through the inherited TextElement.Foreground.
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSurface),
            BrushColor(text.Foreground));
    }

    [AvaloniaFact]
    public void Defaults_AreFlatAndQuiet()
    {
        ShowSurface(out var surface, out _);

        Assert.Equal(0d, surface.Elevation);
        Assert.Null(surface.ShadowColor);
        Assert.Null(surface.StateLayerBrush);
        Assert.Equal(0d, surface.StateLayerOpacity);
    }

    [AvaloniaFact]
    public void ImplicitTheme_ThemeVariantSwitch_ReResolves()
    {
        var window = ShowSurface(out var surface, out _);
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Surface),
            BrushColor(surface.Background));

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(ButtonThemeFixture.SchemeColor(true, static s => s.Surface),
            BrushColor(surface.Background));
    }

    [AvaloniaFact]
    public void MaterialTheme_SurfaceTheme_DoesNotBleedIntoButtonTemplate()
    {
        // MaterialTheme.axaml carries the token stock plus both style sheets. The implicit
        // MaterialSurface theme also applies to PART_Container inside the Button template; the
        // template's own bindings (Template priority) must still win over the theme frame.
        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(LoadStyles("MaterialTheme.axaml"));

        var button = new Button { Content = "OK" };
        var text = new TextBlock { Text = "Hello" };
        var surface = new MaterialSurface { Child = text };
        window.Content = new StackPanel { Children = { button, surface } };
        window.Show();

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(button.Background));
        var buttonText = button.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnPrimary),
            BrushColor(buttonText.Foreground));

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Surface),
            BrushColor(surface.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSurface),
            BrushColor(text.Foreground));
    }
}

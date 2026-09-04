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

        Assert.Equal(0d, surface.ShadowElevation);
        Assert.Null(surface.ShadowColor);
        Assert.Null(surface.StateLayerBrush);
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

    // Tonal elevation: the tint is a precomputed brush swap on the chrome (Background stays the
    // raw token value), gated on the background being exactly the scheme surface color, and
    // accumulated down nested surfaces (upstream: Surface.kt LocalAbsoluteTonalElevation,
    // ColorScheme.kt surfaceColorAtElevation/applyTonalElevation). The expected colors replicate
    // the upstream formula independently — no production helpers — so a drift in either the
    // formula or the compositing shows up here.

    private static Window ShowNestedSurfaces(out MaterialSurface outer, out MaterialSurface inner)
    {
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(new MaterialResource
        {
            Children = { new ColorScheme() },
        });

        var window = new Window
        {
            Resources = resources,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Styles.Add(LoadStyles("Surface.axaml"));

        inner = new MaterialSurface { TonalElevation = 3, Child = new TextBlock { Text = "Inner" } };
        outer = new MaterialSurface { TonalElevation = 1, Child = inner };
        window.Content = outer;
        window.Show();
        return window;
    }

    private static Color ExpectedTonalColor(bool isDark, double elevation)
    {
        var surface = ButtonThemeFixture.SchemeColor(isDark, static s => s.Surface);
        var tint = ButtonThemeFixture.SchemeColor(isDark, static s => s.SurfaceTint);
        var alpha = ((4.5 * Math.Log(elevation + 1)) + 2) / 100;
        return CompositeOver(tint, alpha, surface);
    }

    private static Color CompositeOver(Color foreground, double foregroundAlpha, Color background)
    {
        var fgAlpha = Math.Clamp((foreground.A / 255d) * foregroundAlpha, 0, 1);
        var bgAlpha = background.A / 255d;
        var outAlpha = fgAlpha + (bgAlpha * (1 - fgAlpha));
        if (outAlpha <= 0)
            return Colors.Transparent;

        byte Channel(byte foregroundChannel, byte backgroundChannel) =>
            (byte)Math.Round(
                (((foregroundChannel / 255d) * fgAlpha) + ((backgroundChannel / 255d) * bgAlpha * (1 - fgAlpha))) / outAlpha * 255,
                MidpointRounding.AwayFromZero);

        return Color.FromArgb(
            (byte)Math.Round(outAlpha * 255, MidpointRounding.AwayFromZero),
            Channel(foreground.R, background.R),
            Channel(foreground.G, background.G),
            Channel(foreground.B, background.B));
    }

    [AvaloniaFact]
    public void TonalElevation_Default_BackgroundUntinted()
    {
        ShowSurface(out var surface, out _);

        Assert.Equal(0d, surface.TonalElevation);
        Assert.Equal(0d, surface.AbsoluteTonalElevation);
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Surface),
            BrushColor(surface.EffectiveBackground));
    }

    [AvaloniaFact]
    public void TonalElevation_TintsSurfaceBackground()
    {
        ShowSurface(out var surface, out _);

        surface.TonalElevation = 3;

        Assert.Equal(ExpectedTonalColor(false, 3), BrushColor(surface.EffectiveBackground));
        // The styled property itself stays the raw token brush; only the chrome's paint changes.
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Surface),
            BrushColor(surface.Background));
    }

    [AvaloniaFact]
    public void TonalElevation_NonSurfaceBackground_StaysVerbatim()
    {
        ShowSurface(out var surface, out _);
        var container = ButtonThemeFixture.SchemeColor(false, static s => s.SurfaceContainer);
        surface.Background = new ImmutableSolidColorBrush(container);

        surface.TonalElevation = 3;

        Assert.Equal(container, BrushColor(surface.EffectiveBackground));
    }

    [AvaloniaFact]
    public void TonalElevation_NestedSurfaces_Accumulate()
    {
        ShowNestedSurfaces(out var outer, out var inner);

        Assert.Equal(1d, outer.AbsoluteTonalElevation);
        Assert.Equal(4d, inner.AbsoluteTonalElevation);
        Assert.Equal(ExpectedTonalColor(false, 1), BrushColor(outer.EffectiveBackground));
        Assert.Equal(ExpectedTonalColor(false, 4), BrushColor(inner.EffectiveBackground));
    }

    [AvaloniaFact]
    public void TonalElevation_AncestorChange_PropagatesToDescendants()
    {
        ShowNestedSurfaces(out var outer, out var inner);

        outer.TonalElevation = 6;

        Assert.Equal(9d, inner.AbsoluteTonalElevation);
        Assert.Equal(ExpectedTonalColor(false, 9), BrushColor(inner.EffectiveBackground));
    }

    [AvaloniaFact]
    public void TonalElevation_DetachFromAncestor_DropsAccumulation()
    {
        var window = ShowNestedSurfaces(out var outer, out var inner);

        // Re-host the inner surface standalone: still attached (so tokens resolve), but with no
        // MaterialSurface ancestor left, the accumulated elevation drops back to its own value.
        outer.Child = null;
        window.Content = inner;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(3d, inner.AbsoluteTonalElevation);
        Assert.Equal(ExpectedTonalColor(false, 3), BrushColor(inner.EffectiveBackground));
    }

    [AvaloniaFact]
    public void TonalElevation_ThemeVariantSwitch_ReTints()
    {
        var window = ShowSurface(out var surface, out _);
        surface.TonalElevation = 3;

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(ExpectedTonalColor(true, 3), BrushColor(surface.EffectiveBackground));
    }
}

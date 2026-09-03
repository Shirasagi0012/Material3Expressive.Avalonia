using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Tokens;
using MaterialColorUtilities.DynamicColors;
using MaterialColorUtilities.HCT;
using MaterialColorUtilities.Scheme;
using MaterialColorUtilities.Utils;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The compiled-AXAML path of the first component slice: Themes/Button.axaml loads, the implicit
/// {x:Type Button} theme applies the filled defaults, and the {m3:Token} setter bindings resolve
/// against a real MaterialTheme placement including a ThemeVariant switch. Interaction states
/// (pointer/keyboard) are verified manually for now.
/// </summary>
public class ButtonThemeTests
{
    private static Styles LoadButtonStyles() =>
        Assert.IsType<Styles>(AvaloniaXamlLoader.Load(new Uri("avares://Material3ExpressiveRefactor/Themes/Button.axaml")));

    private static Color SchemeColor(bool isDark, Func<DynamicScheme, ArgbColor> pick)
    {
        var scheme = new SchemeTonalSpot(
            Hct.From(new ArgbColor(0xFF6750A4u)),
            isDark, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform);
        var argb = pick(scheme);
        return Color.FromArgb(argb.Alpha, argb.Red, argb.Green, argb.Blue);
    }

    private static Window ShowWindow(out Button button)
    {
        var theme = new MaterialTheme
        {
            Children =
            {
                new ColorScheme(),
                new StateTokenSource(),
                new ShapeTokenSource(),
                new TypographyTokenSource(),
                new FilledButtonTokenSource(),
            },
        };
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);

        // A Window roots itself at construction (StyledElement assigns _logicalRoot for
        // ILogicalRoot), so assigning Content attaches the button immediately and captures the
        // implicit theme at that moment. Styles must be in place before Content is set.
        var window = new Window
        {
            Resources = resources,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Styles.Add(LoadButtonStyles());

        button = new Button { Content = "OK" };
        window.Content = button;
        window.Show();
        return window;
    }

    [AvaloniaFact]
    public void ImplicitTheme_AppliesFilledDefaults()
    {
        ShowWindow(out var button);

        // Container/content colors resolve through the comp slots into sys.color and are
        // converted to brushes per-consumer (§5.7 item 4).
        Assert.Equal(SchemeColor(false, static s => s.Primary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Background).Color);
        Assert.Equal(SchemeColor(false, static s => s.OnPrimary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Foreground).Color);
        Assert.Equal(40d, button.MinHeight);
        Assert.Equal(new Thickness(24, 0, 24, 0), button.Padding);
        Assert.Equal(14d, button.FontSize);
        Assert.Equal(FontWeight.Medium, button.FontWeight);

        var container = button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Container");
        Assert.Equal(new CornerRadius(1000), container.CornerRadius);

        // §7.4: state layer present, at rest, tinted with the (aliased) content color.
        var stateLayer = button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_StateLayer");
        Assert.Equal(0d, stateLayer.Opacity);
        Assert.Equal(SchemeColor(false, static s => s.OnPrimary),
            Assert.IsType<ImmutableSolidColorBrush>(stateLayer.Background).Color);

        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().First();
        Assert.Equal(0.1d, presenter.LetterSpacing);
        Assert.Equal(20d, presenter.LineHeight);
    }

    [AvaloniaFact]
    public void ImplicitTheme_ThemeVariantSwitch_ReResolves()
    {
        var window = ShowWindow(out var button);
        Assert.Equal(SchemeColor(false, static s => s.Primary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Background).Color);

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(SchemeColor(true, static s => s.Primary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Background).Color);
    }
}

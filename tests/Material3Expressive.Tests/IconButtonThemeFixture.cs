using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Material3Expressive.Baseline;
using Material3Expressive.Controls;
using Material3Expressive.Foundation.Tokens;
using MaterialSymbols;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Shared harness for the IconButton theme tests, mirroring <see cref="ButtonThemeFixture"/>:
/// loads the compiled Styles/IconButton.axaml and shows a real headless window carrying the full
/// baseline token set plus the icon button sources, hosting one IconButton with a real symbol.
/// Expected colors come from <see cref="ButtonThemeFixture.SchemeColor"/>.
/// </summary>
internal static class IconButtonThemeFixture
{
    public static Avalonia.Styling.Styles LoadIconButtonStyles() =>
        Assert.IsType<Avalonia.Styling.Styles>(
            AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/Styles/IconButton.axaml")));

    /// <summary>
    /// Shows a window with the whole baseline token set and the IconButton styles, hosting one
    /// icon button. <paramref name="themeKey"/> applies a named variant theme,
    /// <paramref name="sizeClass"/> a size class.
    /// </summary>
    public static Window ShowWindow(out IconButton button, string? themeKey = null, string? sizeClass = null)
    {
        var theme = new MaterialResource
        {
            Children =
            {
                new ColorScheme(),
                new StateTokenSource(),
                new ShapeTokenSource(),
                new TypographyTokenSource(),
                new ElevationTokenSource(),
                new MotionTokenSource(),
                new IconButtonSizesTokenSource(),
                new StandardIconButtonTokenSource(),
                new FilledIconButtonTokenSource(),
                new TonalIconButtonTokenSource(),
                new OutlinedIconButtonTokenSource(),
            },
        };
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);

        // A Window roots itself at construction, so Styles must be in place before Content is set.
        var window = new Window
        {
            Resources = resources,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        var styles = LoadIconButtonStyles();
        window.Styles.Add(styles);

        button = new IconButton { Symbol = MaterialSymbolCodepoint.Home };
        if (themeKey is not null)
            button.Theme = Assert.IsType<ControlTheme>(styles.Resources[themeKey]);
        if (sizeClass is not null)
            button.Classes.Add(sizeClass);
        window.Content = button;
        window.Show();
        return window;
    }
}

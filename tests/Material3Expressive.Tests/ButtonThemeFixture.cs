using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Baseline;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.DynamicColors;
using MaterialColorUtilities.HCT;
using MaterialColorUtilities.Scheme;
using MaterialColorUtilities.Utils;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Shared harness for the Button theme tests: loads the compiled Components/Button/Button.axaml, computes
/// the expected scheme colors straight from MaterialColorUtilities (same seed/spec as the
/// baseline ColorScheme source), and shows a real headless window carrying the full baseline
/// token set.
/// </summary>
internal static class ButtonThemeFixture
{
    public static Avalonia.Styling.Styles LoadButtonStyles() =>
        Assert.IsType<Avalonia.Styling.Styles>(
            AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/Components/Button/Button.axaml")));

    public static Color SchemeColor(bool isDark, Func<DynamicScheme, ArgbColor> pick)
    {
        var scheme = new SchemeTonalSpot(
            Hct.From(new ArgbColor(0xFF6750A4u)),
            isDark, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform);
        var argb = pick(scheme);
        return Color.FromArgb(argb.Alpha, argb.Red, argb.Green, argb.Blue);
    }

    /// <summary>
    /// Shows a window with the whole baseline token set and the Button styles, hosting one
    /// button. <paramref name="themeKey"/> applies a named variant theme, <paramref name="sizeClass"/>
    /// a size class.
    /// </summary>
    public static Window ShowWindow(out Button button, string? themeKey = null, string? sizeClass = null, CornerRadius? pressedCornerRadius = null)
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
                new ButtonSizesTokenSource(),
                new FilledButtonTokenSource(),
                new ElevatedButtonTokenSource(),
                new TonalButtonTokenSource(),
                new OutlinedButtonTokenSource(),
                new TextButtonTokenSource(),
            },
        };
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);

        // A Window roots itself at construction (StyledElement assigns _logicalRoot for
        // ILogicalRoot), so assigning Content attaches the button immediately and captures the
        // implicit theme at that moment. Styles must be in place before Content is set.
        var window = new MotionTestWindow()
        {
            Resources = resources,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        var styles = LoadButtonStyles();
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/Foundation/Typography/Typography.axaml")));
        window.Styles.Add(styles);

        button = new Button { Content = "OK" };

        if (pressedCornerRadius is not null)
            MaterialButton.SetPressedCornerRadius(button, pressedCornerRadius);

        if (themeKey is not null)
            button.Theme = Assert.IsType<ControlTheme>(styles.Resources[themeKey]);
        if (sizeClass is not null)
            button.Classes.Add(sizeClass);
        window.Content = button;
        window.Show();
        return window;
    }
}

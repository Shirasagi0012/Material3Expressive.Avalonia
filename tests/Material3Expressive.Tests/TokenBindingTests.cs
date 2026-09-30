using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using Material3Expressive.Baseline;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using MaterialColorUtilities.DynamicColors;
using MaterialColorUtilities.HCT;
using MaterialColorUtilities.Scheme;
using MaterialColorUtilities.Utils;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// End-to-end coverage for {m3:Token}'s MultiBinding bridge: initial resolution, ThemeVariant
/// switching, live intermediate-override propagation (R2'), the Setter.Value path, template
/// priority handling, and miss → UnsetValue fallback (design doc §5.7 / §8.1).
/// </summary>
public class TokenBindingTests
{
    private static readonly MaterialTokenKey<Color> CompColor = new("md.comp.test-widget.container-color");

    private static Color SchemeColor(bool isDark, Func<DynamicScheme, ArgbColor> pick)
    {
        var scheme = new SchemeTonalSpot(
            Hct.From(new ArgbColor(0xFF6750A4u)),
            isDark, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform);
        var argb = pick(scheme);
        return Color.FromArgb(argb.Alpha, argb.Red, argb.Green, argb.Blue);
    }

    private static Window ShowWindow(ColorScheme scheme, TestTokenSource extras, out Button button)
    {
        var theme = new MaterialResource { Children = { scheme, extras } };
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);

        button = new Button();
        var window = new MotionTestWindow()
        {
            Content = button,
            Resources = resources,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Show();
        return window;
    }

    private static void BindToken(AvaloniaObject target, AvaloniaProperty property, MaterialTokenKey key) =>
        target.Bind(property, new TokenExtension(key).ProvideValue(new StubServiceProvider()));

    private static Color BackgroundColorOf(Button button) =>
        Assert.IsType<ImmutableSolidColorBrush>(button.Background).Color;

    [AvaloniaFact]
    public void Token_ResolvesSchemeColor_AsImmutableBrush()
    {
        ShowWindow(new ColorScheme(), new TestTokenSource(), out var button);

        BindToken(button, TemplatedControl.BackgroundProperty, MaterialTokens.Sys.Color.Primary);

        Assert.Equal(SchemeColor(false, static s => s.Primary), BackgroundColorOf(button));
    }

    [AvaloniaFact]
    public void Token_ThemeVariantSwitch_ReResolves()
    {
        var window = ShowWindow(new ColorScheme(), new TestTokenSource(), out var button);
        BindToken(button, TemplatedControl.BackgroundProperty, MaterialTokens.Sys.Color.Primary);
        Assert.Equal(SchemeColor(false, static s => s.Primary), BackgroundColorOf(button));

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(SchemeColor(true, static s => s.Primary), BackgroundColorOf(button));
    }

    [AvaloniaFact]
    public void Token_IntermediateOverride_LiveUpdate_WithoutRootEntryChanging()
    {
        // R2' load-bearing test: the root entry (CompColor → TokenRef(Sys.Color.Primary)) sits at
        // window level and never changes identity; overriding the intermediate key in an inner
        // scope must re-run resolution and republish. This pins the "converter re-runs even when
        // the DynamicResource trigger's own value is unchanged" behavior the bridge relies on.
        var extras = new TestTokenSource();
        extras.Set(CompColor, new TokenRef(MaterialTokens.Sys.Color.Primary));
        ShowWindow(new ColorScheme(), extras, out var button);
        BindToken(button, TemplatedControl.BackgroundProperty, CompColor);
        Assert.Equal(SchemeColor(false, static s => s.Primary), BackgroundColorOf(button));

        var inner = new TestTokenSource();
        var innerTheme = new MaterialResource { Children = { inner } };
        var innerResources = new ResourceDictionary();
        innerResources.MergedDictionaries.Add(innerTheme);
        button.Resources = innerResources;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(SchemeColor(false, static s => s.Primary), BackgroundColorOf(button));

        inner.Set(MaterialTokens.Sys.Color.Primary, Colors.LimeGreen);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Colors.LimeGreen, BackgroundColorOf(button));
    }

    [AvaloniaFact]
    public void Token_InSetterValue_AppliesAtStylePriority_AndFollowsTheme()
    {
        var window = ShowWindow(new ColorScheme(), new TestTokenSource(), out var button);
        var style = new Style(static x => x.OfType<Button>())
        {
            Setters =
            {
                new Setter(
                    TemplatedControl.BackgroundProperty,
                    new TokenExtension(MaterialTokens.Sys.Color.Secondary).ProvideValue(new StubServiceProvider())),
            },
        };
        window.Styles.Add(style);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(SchemeColor(false, static s => s.Secondary), BackgroundColorOf(button));

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(SchemeColor(true, static s => s.Secondary), BackgroundColorOf(button));
    }

    [AvaloniaFact]
    public void Token_SeedChange_BroadcastsToBindings()
    {
        var scheme = new ColorScheme();
        ShowWindow(scheme, new TestTokenSource(), out var button);
        BindToken(button, TemplatedControl.BackgroundProperty, MaterialTokens.Sys.Color.Primary);
        Assert.Equal(SchemeColor(false, static s => s.Primary), BackgroundColorOf(button));

        scheme.Seed = Color.FromRgb(0x66, 0xCC, 0xFF);
        Dispatcher.UIThread.RunJobs();

        var expected = new SchemeTonalSpot(
            Hct.From(new ArgbColor(0xFF66CCFFu)),
            false, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform).Primary;
        Assert.Equal(
            Color.FromArgb(expected.Alpha, expected.Red, expected.Green, expected.Blue),
            BackgroundColorOf(button));
    }

    [AvaloniaFact]
    public void Token_MissingKey_LeavesPropertyUnset()
    {
        ShowWindow(new ColorScheme(), new TestTokenSource(), out var button);

        BindToken(button, TemplatedControl.BackgroundProperty, new MaterialTokenKey<Color>("md.comp.nonexistent.color"));

        Assert.Null(button.Background);
    }

    [Fact]
    public void Token_TemplateContext_SetsTemplatePriority()
    {
        var inTemplate = Assert.IsType<MultiBinding>(
            new TokenExtension(MaterialTokens.Sys.Color.Primary).ProvideValue(new StubServiceProvider(inTemplate: true)));
        Assert.Equal(BindingPriority.Template, inTemplate.Priority);

        var plain = Assert.IsType<MultiBinding>(
            new TokenExtension(MaterialTokens.Sys.Color.Primary).ProvideValue(new StubServiceProvider()));
        Assert.Equal(BindingPriority.LocalValue, plain.Priority);
    }
}

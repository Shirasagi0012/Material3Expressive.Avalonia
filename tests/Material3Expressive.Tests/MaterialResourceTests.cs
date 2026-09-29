using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
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
/// MaterialResource as a pure combinator (design doc §6.1): ordered children, later wins, miss falls
/// through, child changes broadcast.
/// </summary>
public class MaterialResourceTests
{
    private static readonly MaterialTokenKey<Avalonia.Media.Color> Key = new("md.comp.test-widget.container-color");

    [AvaloniaFact]
    public void LaterChild_Wins()
    {
        var first = new TestTokenSource();
        first.Set(Key, Avalonia.Media.Colors.Red);
        var second = new TestTokenSource();
        second.Set(Key, Avalonia.Media.Colors.Blue);
        var theme = new MaterialResource { Children = { first, second } };

        Assert.True(theme.TryGetResource(Key, null, out var value));
        Assert.Equal(Avalonia.Media.Colors.Blue, value);
    }

    [AvaloniaFact]
    public void Miss_FallsThrough()
    {
        var theme = new MaterialResource { Children = { new TestTokenSource() } };

        Assert.False(theme.TryGetResource(Key, null, out _));
    }

    [AvaloniaFact]
    public void NonTokenKey_IsNotAnswered()
    {
        var theme = new MaterialResource { Children = { new ColorScheme() } };

        Assert.False(theme.TryGetResource("Sys.Color.Primary", null, out _));
    }

    [AvaloniaFact]
    public void ChildChange_BroadcastsThroughHost()
    {
        // ResourceProvider has no public change event; the observable contract is that a hosted
        // consumer re-reads. A DynamicResource on Control.Tag is the smallest such consumer.
        var source = new TestTokenSource();
        source.Set(Key, Avalonia.Media.Colors.Red);
        var theme = new MaterialResource { Children = { source } };
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);
        var button = new Button();
        var window = new Window
        {
            Content = button,
            Resources = resources,
        };
        button.Bind(Control.TagProperty, new DynamicResourceExtension(Key));
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal((object)Avalonia.Media.Colors.Red, button.Tag);

        source.Set(Key, Avalonia.Media.Colors.Blue);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal((object)Avalonia.Media.Colors.Blue, button.Tag);
    }

    [AvaloniaFact]
    public void ColorScheme_AnswersThroughMaterialTheme()
    {
        var theme = new MaterialResource { Children = { new ColorScheme() } };

        Assert.True(theme.TryGetResource(MaterialTokens.Sys.Color.Primary, ThemeVariant.Dark, out var value));
        var expected = new SchemeTonalSpot(
            Hct.From(new ArgbColor(0xFF6750A4u)),
            true, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform).Primary;
        Assert.Equal(
            Avalonia.Media.Color.FromArgb(expected.Alpha, expected.Red, expected.Green, expected.Blue),
            Assert.IsType<Avalonia.Media.Color>(value));
    }
}

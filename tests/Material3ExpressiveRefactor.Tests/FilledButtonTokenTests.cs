using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Xunit;

using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The first component slice (design doc §7.5): Comp.FilledButton facade↔source completeness,
/// baseline alias chains resolved end to end, WithAlpha disabled derivations, and R2' rebinds
/// through comp slots.
/// </summary>
public class FilledButtonTokenTests
{
    private static Button HostIn(params ITokenSource[] sources)
    {
        var theme = new MaterialTheme();
        foreach (var source in sources)
            theme.Children.Add(source);
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);
        return new Button { Resources = resources };
    }

    // Later children win in MaterialTheme, so overrides go after the baseline set.
    private static Button BaselineHost(params ITokenSource[] overrides) =>
        HostIn(new ITokenSource[]
        {
            new ColorScheme(),
            new StateTokenSource(),
            new ShapeTokenSource(),
            new TypographyTokenSource(),
            new FilledButtonTokenSource(),
        }.Concat(overrides).ToArray());

    [AvaloniaFact]
    public void FilledButton_AnswersEveryFacadeKey() =>
        TokenScan.AssertComplete(new FilledButtonTokenSource(), typeof(Comp.FilledButton));

    [AvaloniaFact]
    public void FilledButton_RestColors_FollowTheSysColorChain()
    {
        var host = BaselineHost();

        // One hop: container → sys.color.primary.
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.Primary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.ContainerColor)));

        // Three hops: per-state slots → state-layer color → content → on-primary.
        var onPrimary = Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnPrimary));
        Assert.Equal(onPrimary, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.ContentColor)));
        Assert.Equal(onPrimary, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.StateLayerColor)));
        Assert.Equal(onPrimary, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.HoverStateLayerColor)));
        Assert.Equal(onPrimary, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.PressedStateLayerColor)));
        Assert.Equal(onPrimary, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.FocusedStateLayerColor)));
    }

    [AvaloniaFact]
    public void FilledButton_DisabledColors_AreWithAlphaDerivations()
    {
        var host = BaselineHost();

        // WithAlpha(Sys.Color.OnSurface, …): the resolver multiplies the opacity into the alpha
        // channel of the (opaque) baseline OnSurface.
        var onSurface = Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSurface));
        Assert.Equal(byte.MaxValue, onSurface.A);

        var container = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.DisabledContainerColor));
        Assert.Equal((byte)Math.Round(0.12 * onSurface.A), container.A);
        Assert.Equal(onSurface.R, container.R);
        Assert.Equal(onSurface.G, container.G);
        Assert.Equal(onSurface.B, container.B);

        var content = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.DisabledContentColor));
        Assert.Equal((byte)Math.Round(0.38 * onSurface.A), content.A);
        Assert.Equal(onSurface.R, content.R);
    }

    [AvaloniaFact]
    public void FilledButton_MetricsAndShape()
    {
        var host = BaselineHost();

        Assert.Equal(40d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.FilledButton.ContainerHeight)));
        Assert.Equal(new Thickness(24, 0, 24, 0), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.FilledButton.ContentPadding)));
        // The stadium shape hops into the sys corner scale (CornerRadius(1000)).
        Assert.Equal(new CornerRadius(1000), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.FilledButton.ContainerShape)));
    }

    [AvaloniaFact]
    public void FilledButton_LabelText_AliasesLabelLarge()
    {
        var host = BaselineHost();

        // LabelTextFont → sys.typescale.label-large.font → ref.typeface.plain → literal: 3 hops.
        Assert.Equal("Roboto", Assert.IsType<FontFamily>(TokenScan.Resolve(host, Comp.FilledButton.LabelTextFont)).Name);
        Assert.Equal(14d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.FilledButton.LabelTextSize)));
        Assert.Equal(FontWeight.Medium, Assert.IsType<FontWeight>(TokenScan.Resolve(host, Comp.FilledButton.LabelTextWeight)));
        Assert.Equal(0.1d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.FilledButton.LabelTextTracking)));
        Assert.Equal(20d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.FilledButton.LabelTextLineHeight)));
    }

    [AvaloniaFact]
    public void FilledButton_SysColorOverride_RebindsCompSlots()
    {
        // R2': overriding the system role in a later child rebinds the comp slot even though the
        // baseline's ContainerColor → Primary TokenRef entry is untouched.
        var overrides = new TestTokenSource();
        overrides.Set(Sys.Color.Primary, Colors.Red);
        var host = BaselineHost(overrides);

        Assert.Equal(Colors.Red, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.ContainerColor)));
    }

    [AvaloniaFact]
    public void FilledButton_ContentColorOverride_ReroutesEveryStateLayer()
    {
        // §7.4: overriding the shared content entry recolors the shared and per-state slots.
        var overrides = new TestTokenSource();
        overrides.Set(Comp.FilledButton.ContentColor, Colors.Lime);
        var host = BaselineHost(overrides);

        Assert.Equal(Colors.Lime, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.StateLayerColor)));
        Assert.Equal(Colors.Lime, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.HoverStateLayerColor)));
        Assert.Equal(Colors.Lime, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.PressedStateLayerColor)));
        Assert.Equal(Colors.Lime, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledButton.FocusedStateLayerColor)));
    }

    [AvaloniaFact]
    public void FilledButton_Miss_StaysHonest()
    {
        // The comp source only answers its own slots, and no subsystem source answers comp keys.
        Assert.False(new FilledButtonTokenSource().TryGetToken(Sys.Color.Primary, null, out _));
        Assert.False(new StateTokenSource().TryGetToken(Comp.FilledButton.ContainerColor, null, out _));
        Assert.False(new ColorScheme().TryGetToken(Comp.FilledButton.ContainerColor, null, out _));
    }
}

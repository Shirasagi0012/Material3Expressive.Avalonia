using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Baseline;
using Material3ExpressiveRefactor.Controls;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Xunit;

using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The Button style axis (design doc §7.5): facade↔source completeness for the elevated, tonal,
/// outlined and text tables, and their baseline chains into sys.color / sys.elevation. Values are
/// the upstream v0_11_0 tables with the two documented composable-level corrections (text button
/// content = Primary; outlined disabled outline = outline color at the disabled-container
/// opacity).
/// </summary>
public class ButtonStyleVariantTokenTests
{
    private static Button HostIn(params ITokenSource[] sources)
    {
        var theme = new MaterialResource();
        foreach (var source in sources)
            theme.Children.Add(source);
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);
        return new Button { Resources = resources };
    }

    // The style tables reference sys.color, sys.state (via WithAlpha) and sys.elevation.
    private static Button BaselineHost() =>
        HostIn(
            new ColorScheme(),
            new StateTokenSource(),
            new ElevationTokenSource(),
            new ElevatedButtonTokenSource(),
            new TonalButtonTokenSource(),
            new OutlinedButtonTokenSource(),
            new TextButtonTokenSource());

    [AvaloniaFact]
    public void StyleVariants_AnswerEveryFacadeKey()
    {
        TokenScan.AssertComplete(new ElevatedButtonTokenSource(), typeof(Comp.ElevatedButton));
        TokenScan.AssertComplete(new TonalButtonTokenSource(), typeof(Comp.TonalButton));
        TokenScan.AssertComplete(new OutlinedButtonTokenSource(), typeof(Comp.OutlinedButton));
        TokenScan.AssertComplete(new TextButtonTokenSource(), typeof(Comp.TextButton));
    }

    [AvaloniaFact]
    public void ElevatedButton_ColorsAndShadows()
    {
        var host = BaselineHost();

        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.SurfaceContainerLow)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.ElevatedButton.ContainerColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.Primary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.ElevatedButton.ContentColor)));

        // Per-state shadows alias the baked elevation recipes: rest/focus/press at level 1,
        // hover at level 2, disabled at level 0.
        Assert.Equal(
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Sys.Elevation.Level1Shadow)),
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Comp.ElevatedButton.ContainerShadow)));
        Assert.Equal(
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Sys.Elevation.Level2Shadow)),
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Comp.ElevatedButton.HoveredContainerShadow)));
        Assert.Equal(
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Sys.Elevation.Level1Shadow)),
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Comp.ElevatedButton.FocusedContainerShadow)));
        Assert.Equal(
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Sys.Elevation.Level1Shadow)),
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Comp.ElevatedButton.PressedContainerShadow)));
        Assert.Equal(
            default,
            Assert.IsType<BoxShadows>(TokenScan.Resolve(host, Comp.ElevatedButton.DisabledContainerShadow)));
    }

    [AvaloniaFact]
    public void TonalButton_Colors()
    {
        var host = BaselineHost();

        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.SecondaryContainer)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TonalButton.ContainerColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSecondaryContainer)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TonalButton.ContentColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSecondaryContainer)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TonalButton.HoverStateLayerColor)));
    }

    [AvaloniaFact]
    public void OutlinedButton_ColorsAndOutline()
    {
        var host = BaselineHost();

        Assert.Equal(Colors.Transparent, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.ContainerColor)));
        Assert.Equal(Colors.Transparent, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.DisabledContainerColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSurfaceVariant)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.ContentColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OutlineVariant)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.OutlineColor)));

        // The composable's derivation: resting outline color at the disabled-container opacity
        // (0.12), NOT the raw DisabledOutlineColor token.
        var outline = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.OutlineColor));
        var disabled = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.DisabledOutlineColor));
        Assert.Equal((byte)Math.Round(0.12 * outline.A), disabled.A);
        Assert.Equal(outline.R, disabled.R);
        Assert.Equal(outline.G, disabled.G);
        Assert.Equal(outline.B, disabled.B);
    }

    [AvaloniaFact]
    public void TextButton_Colors()
    {
        var host = BaselineHost();

        Assert.Equal(Colors.Transparent, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TextButton.ContainerColor)));
        // The upstream composable overrides its own buggy LabelColor token with Primary.
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.Primary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TextButton.ContentColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.Primary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TextButton.PressedStateLayerColor)));
    }

    [AvaloniaFact]
    public void StyleVariants_Miss_StaysHonest()
    {
        Assert.False(new ElevatedButtonTokenSource().TryGetToken(Comp.FilledButton.ContainerColor, null, out _));
        Assert.False(new TonalButtonTokenSource().TryGetToken(Comp.OutlinedButton.OutlineColor, null, out _));
        Assert.False(new OutlinedButtonTokenSource().TryGetToken(Comp.ElevatedButton.ContainerShadow, null, out _));
        Assert.False(new TextButtonTokenSource().TryGetToken(Sys.Color.Primary, null, out _));
    }
}

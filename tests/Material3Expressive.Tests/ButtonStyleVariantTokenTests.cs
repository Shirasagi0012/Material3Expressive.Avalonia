using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Baseline;
using Material3Expressive.Components;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Xunit;

using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

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
    public void ElevatedButton_ColorsAndElevations()
    {
        var host = BaselineHost();

        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.SurfaceContainerLow)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.ElevatedButton.ContainerColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.Primary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.ElevatedButton.ContentColor)));

        // Per-state elevations alias the sys.elevation DIP levels: rest/focus/press at level 1,
        // hover at level 2, disabled at level 0.
        Assert.Equal(
            Assert.IsType<double>(TokenScan.Resolve(host, Sys.Elevation.Level1)),
            Assert.IsType<double>(TokenScan.Resolve(host, Comp.ElevatedButton.ContainerElevation)));
        Assert.Equal(
            Assert.IsType<double>(TokenScan.Resolve(host, Sys.Elevation.Level2)),
            Assert.IsType<double>(TokenScan.Resolve(host, Comp.ElevatedButton.HoveredContainerElevation)));
        Assert.Equal(
            Assert.IsType<double>(TokenScan.Resolve(host, Sys.Elevation.Level1)),
            Assert.IsType<double>(TokenScan.Resolve(host, Comp.ElevatedButton.FocusedContainerElevation)));
        Assert.Equal(
            Assert.IsType<double>(TokenScan.Resolve(host, Sys.Elevation.Level1)),
            Assert.IsType<double>(TokenScan.Resolve(host, Comp.ElevatedButton.PressedContainerElevation)));
        Assert.Equal(
            0d,
            Assert.IsType<double>(TokenScan.Resolve(host, Comp.ElevatedButton.DisabledContainerElevation)));
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
        // (0.10), not the raw DisabledOutlineColor token.
        var outline = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.OutlineColor));
        var disabled = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedButton.DisabledOutlineColor));
        Assert.Equal((byte)Math.Round(0.10 * outline.A), disabled.A);
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
        Assert.False(new OutlinedButtonTokenSource().TryGetToken(Comp.ElevatedButton.ContainerElevation, null, out _));
        Assert.False(new TextButtonTokenSource().TryGetToken(Sys.Color.Primary, null, out _));
    }
}

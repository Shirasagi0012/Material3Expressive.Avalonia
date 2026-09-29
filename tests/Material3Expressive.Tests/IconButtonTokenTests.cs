using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Baseline;
using Material3Expressive.Controls;
using Material3Expressive.Foundation.Tokens;
using Xunit;

using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

/// <summary>
/// The IconButton tables: facade↔source completeness for the four style tables and the five size
/// tables, and their baseline chains into sys.color / sys.state / sys.shape. Values are the
/// upstream tables (14_1_0, Large still v0_11_0) with the documented composable-level choices:
/// standard/outlined content uses the token table (OnSurfaceVariant) rather than the plain
/// composable's LocalContentColor, and the outlined disabled border is the outline color at the
/// disabled-content opacity per both upstream border paths.
/// </summary>
public class IconButtonTokenTests
{
    private static IconButton HostIn(params ITokenSource[] sources)
    {
        var theme = new MaterialResource();
        foreach (var source in sources)
            theme.Children.Add(source);
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);
        return new IconButton { Resources = resources };
    }

    // The style tables reference sys.color and sys.state (via WithAlpha); the size tables sys.shape.
    private static IconButton BaselineHost() =>
        HostIn(
            new ColorScheme(),
            new StateTokenSource(),
            new ShapeTokenSource(),
            new IconButtonSizesTokenSource(),
            new StandardIconButtonTokenSource(),
            new FilledIconButtonTokenSource(),
            new TonalIconButtonTokenSource(),
            new OutlinedIconButtonTokenSource());

    [AvaloniaFact]
    public void IconButtonSources_AnswerEveryFacadeKey()
    {
        TokenScan.AssertComplete(new StandardIconButtonTokenSource(), typeof(Comp.StandardIconButton));
        TokenScan.AssertComplete(new FilledIconButtonTokenSource(), typeof(Comp.FilledIconButton));
        TokenScan.AssertComplete(new TonalIconButtonTokenSource(), typeof(Comp.TonalIconButton));
        TokenScan.AssertComplete(new OutlinedIconButtonTokenSource(), typeof(Comp.OutlinedIconButton));
        TokenScan.AssertComplete(new IconButtonSizesTokenSource(),
            typeof(Comp.IconButtonXSmall), typeof(Comp.IconButtonSmall), typeof(Comp.IconButtonMedium),
            typeof(Comp.IconButtonLarge), typeof(Comp.IconButtonXLarge));
    }

    [AvaloniaFact]
    public void Standard_Colors()
    {
        var host = BaselineHost();

        Assert.Equal(Colors.Transparent, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.StandardIconButton.ContainerColor)));
        Assert.Equal(Colors.Transparent, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.StandardIconButton.DisabledContainerColor)));

        // The token-table content color (the vibrant variant upstream), aliased by every state.
        var content = Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSurfaceVariant));
        Assert.Equal(content, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.StandardIconButton.ContentColor)));
        Assert.Equal(content, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.StandardIconButton.HoverStateLayerColor)));
        Assert.Equal(content, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.StandardIconButton.PressedStateLayerColor)));
        Assert.Equal(content, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.StandardIconButton.FocusedStateLayerColor)));

        // Disabled content: OnSurface at the disabled-content opacity (0.38).
        var onSurface = Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSurface));
        var disabled = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.StandardIconButton.DisabledContentColor));
        Assert.Equal((byte)Math.Round(0.38 * onSurface.A), disabled.A);
        Assert.Equal(onSurface.R, disabled.R);
        Assert.Equal(onSurface.G, disabled.G);
        Assert.Equal(onSurface.B, disabled.B);
    }

    [AvaloniaFact]
    public void Filled_Colors()
    {
        var host = BaselineHost();

        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.Primary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledIconButton.ContainerColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnPrimary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledIconButton.ContentColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnPrimary)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledIconButton.HoverStateLayerColor)));

        // The icon button tables disable the container at 0.10, NOT the sys 0.12 the Button
        // family uses.
        var onSurface = Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSurface));
        var disabled = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.FilledIconButton.DisabledContainerColor));
        Assert.Equal((byte)Math.Round(0.10 * onSurface.A), disabled.A);
        Assert.Equal(onSurface.R, disabled.R);
    }

    [AvaloniaFact]
    public void Tonal_Colors()
    {
        var host = BaselineHost();

        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.SecondaryContainer)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TonalIconButton.ContainerColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSecondaryContainer)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TonalIconButton.ContentColor)));

        var onSurface = Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSurface));
        var disabled = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.TonalIconButton.DisabledContainerColor));
        Assert.Equal((byte)Math.Round(0.10 * onSurface.A), disabled.A);
    }

    [AvaloniaFact]
    public void Outlined_ColorsAndOutline()
    {
        var host = BaselineHost();

        Assert.Equal(Colors.Transparent, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedIconButton.ContainerColor)));
        Assert.Equal(Colors.Transparent, Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedIconButton.DisabledContainerColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OnSurfaceVariant)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedIconButton.ContentColor)));
        Assert.Equal(
            Assert.IsType<Color>(TokenScan.Resolve(host, Sys.Color.OutlineVariant)),
            Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedIconButton.OutlineColor)));

        // Both upstream border paths derive the disabled border as the outline color at the
        // disabled-content opacity (0.38), not the raw DisabledOutlineColor token.
        var outline = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedIconButton.OutlineColor));
        var disabled = Assert.IsType<Color>(TokenScan.Resolve(host, Comp.OutlinedIconButton.DisabledOutlineColor));
        Assert.Equal((byte)Math.Round(0.38 * outline.A), disabled.A);
        Assert.Equal(outline.R, disabled.R);
        Assert.Equal(outline.G, disabled.G);
        Assert.Equal(outline.B, disabled.B);
    }

    [AvaloniaFact]
    public void Sizes_MetricsShapesAndSpaces()
    {
        var host = BaselineHost();

        // Heights and icon sizes across the five tables.
        Assert.Equal(32d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonXSmall.ContainerHeight)));
        Assert.Equal(40d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonSmall.ContainerHeight)));
        Assert.Equal(56d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonMedium.ContainerHeight)));
        Assert.Equal(96d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonLarge.ContainerHeight)));
        Assert.Equal(136d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonXLarge.ContainerHeight)));

        Assert.Equal(20d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonXSmall.IconSize)));
        Assert.Equal(24d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonSmall.IconSize)));
        Assert.Equal(24d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonMedium.IconSize)));
        Assert.Equal(32d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonLarge.IconSize)));
        Assert.Equal(40d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.IconButtonXLarge.IconSize)));

        // Every resting round shape is the full corner; square and pressed step down the scale.
        var full = Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Sys.Shape.Full));
        Assert.Equal(full, Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.IconButtonSmall.ContainerShapeRound)));
        Assert.Equal(
            Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Sys.Shape.Medium)),
            Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.IconButtonSmall.ContainerShapeSquare)));
        Assert.Equal(
            Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Sys.Shape.Small)),
            Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.IconButtonSmall.PressedContainerShape)));
        Assert.Equal(
            Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Sys.Shape.ExtraLarge)),
            Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.IconButtonLarge.ContainerShapeSquare)));

        // The default width option is square: space + icon + space = height at every size.
        foreach (var table in new[]
        {
            typeof(Comp.IconButtonXSmall), typeof(Comp.IconButtonSmall), typeof(Comp.IconButtonMedium),
            typeof(Comp.IconButtonLarge), typeof(Comp.IconButtonXLarge),
        })
        {
            var height = Assert.IsType<double>(TokenScan.Resolve(host,
                (MaterialTokenKey)table.GetField(nameof(Comp.IconButtonSmall.ContainerHeight))!.GetValue(null)!));
            var icon = Assert.IsType<double>(TokenScan.Resolve(host,
                (MaterialTokenKey)table.GetField(nameof(Comp.IconButtonSmall.IconSize))!.GetValue(null)!));
            var leading = Assert.IsType<double>(TokenScan.Resolve(host,
                (MaterialTokenKey)table.GetField(nameof(Comp.IconButtonSmall.DefaultLeadingSpace))!.GetValue(null)!));
            var trailing = Assert.IsType<double>(TokenScan.Resolve(host,
                (MaterialTokenKey)table.GetField(nameof(Comp.IconButtonSmall.DefaultTrailingSpace))!.GetValue(null)!));
            Assert.Equal(height, leading + icon + trailing);
        }

        // The outline width thickens at the two largest sizes.
        Assert.Equal(new Thickness(1), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.IconButtonSmall.OutlinedOutlineWidth)));
        Assert.Equal(new Thickness(2), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.IconButtonLarge.OutlinedOutlineWidth)));
        Assert.Equal(new Thickness(3), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.IconButtonXLarge.OutlinedOutlineWidth)));
    }

    [AvaloniaFact]
    public void Sources_Miss_StaysHonest()
    {
        Assert.False(new StandardIconButtonTokenSource().TryGetToken(Comp.FilledIconButton.ContainerColor, null, out _));
        Assert.False(new FilledIconButtonTokenSource().TryGetToken(Comp.OutlinedIconButton.OutlineColor, null, out _));
        Assert.False(new TonalIconButtonTokenSource().TryGetToken(Comp.IconButtonSmall.IconSize, null, out _));
        Assert.False(new OutlinedIconButtonTokenSource().TryGetToken(Comp.IconButtonSmall.ContainerHeight, null, out _));
        Assert.False(new IconButtonSizesTokenSource().TryGetToken(Comp.StandardIconButton.ContentColor, null, out _));
    }
}

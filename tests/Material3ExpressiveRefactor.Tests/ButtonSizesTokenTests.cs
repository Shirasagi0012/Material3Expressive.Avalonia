using Avalonia;
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
/// The shared Button size axis (upstream ButtonXSmall…XLargeTokens v0_11_0): facade↔source
/// completeness for all five tables, the metric literals, the sys.shape hops, and the typography
/// aliases following the composable's textStyleFor mapping.
/// </summary>
public class ButtonSizesTokenTests
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

    // The size tables only reference the shape and type-scale subsystems.
    private static Button BaselineHost() =>
        HostIn(new ShapeTokenSource(), new TypographyTokenSource(), new ButtonSizesTokenSource());

    [AvaloniaFact]
    public void ButtonSizes_AnswerEveryFacadeKey() =>
        TokenScan.AssertComplete(
            new ButtonSizesTokenSource(),
            typeof(Comp.ButtonXSmall),
            typeof(Comp.ButtonSmall),
            typeof(Comp.ButtonMedium),
            typeof(Comp.ButtonLarge),
            typeof(Comp.ButtonXLarge));

    [AvaloniaFact]
    public void ButtonSizes_Metrics()
    {
        var host = BaselineHost();

        Assert.Equal(32d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonXSmall.ContainerHeight)));
        Assert.Equal(40d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonSmall.ContainerHeight)));
        Assert.Equal(56d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonMedium.ContainerHeight)));
        Assert.Equal(96d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonLarge.ContainerHeight)));
        Assert.Equal(136d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonXLarge.ContainerHeight)));

        // Horizontal-only padding; the template centers content vertically.
        Assert.Equal(new Thickness(16, 0, 16, 0), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonXSmall.ContentPadding)));
        Assert.Equal(new Thickness(16, 0, 16, 0), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonSmall.ContentPadding)));
        Assert.Equal(new Thickness(24, 0, 24, 0), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonMedium.ContentPadding)));
        Assert.Equal(new Thickness(48, 0, 48, 0), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonLarge.ContentPadding)));
        Assert.Equal(new Thickness(64, 0, 64, 0), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonXLarge.ContentPadding)));

        Assert.Equal(20d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonSmall.IconSize)));
        Assert.Equal(24d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonMedium.IconSize)));
        Assert.Equal(32d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonLarge.IconSize)));
        Assert.Equal(40d, Assert.IsType<double>(TokenScan.Resolve(host, Comp.ButtonXLarge.IconSize)));

        Assert.Equal(new Thickness(1), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonMedium.OutlinedOutlineWidth)));
        Assert.Equal(new Thickness(2), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonLarge.OutlinedOutlineWidth)));
        Assert.Equal(new Thickness(3), Assert.IsType<Thickness>(TokenScan.Resolve(host, Comp.ButtonXLarge.OutlinedOutlineWidth)));
    }

    [AvaloniaFact]
    public void ButtonSizes_Shapes_HopIntoTheCornerScale()
    {
        var host = BaselineHost();

        // The stadium (CornerRadius(1000)) is the resting round shape at every size.
        foreach (var table in new[]
        {
            typeof(Comp.ButtonXSmall), typeof(Comp.ButtonSmall), typeof(Comp.ButtonMedium),
            typeof(Comp.ButtonLarge), typeof(Comp.ButtonXLarge),
        })
        {
            var round = (MaterialTokenKey<CornerRadius>)table.GetField(nameof(Comp.ButtonSmall.ContainerShapeRound))!.GetValue(null)!;
            Assert.Equal(new CornerRadius(1000), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, round)));
        }

        // Square and pressed-morph shapes step down the corner scale with size.
        Assert.Equal(new CornerRadius(12), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonXSmall.ContainerShapeSquare)));
        Assert.Equal(new CornerRadius(8), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonXSmall.PressedContainerShape)));
        Assert.Equal(new CornerRadius(16), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonMedium.ContainerShapeSquare)));
        Assert.Equal(new CornerRadius(12), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonMedium.PressedContainerShape)));
        Assert.Equal(new CornerRadius(28), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonLarge.ContainerShapeSquare)));
        Assert.Equal(new CornerRadius(16), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonLarge.PressedContainerShape)));
        Assert.Equal(new CornerRadius(28), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonXLarge.ContainerShapeSquare)));
        Assert.Equal(new CornerRadius(16), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, Comp.ButtonXLarge.PressedContainerShape)));
    }

    [AvaloniaFact]
    public void ButtonSizes_Typography_FollowsTextStyleFor()
    {
        var host = BaselineHost();

        // Alias equivalence, not literals: the comp slot must resolve to exactly what the
        // referenced type-scale role resolves to from the same position.
        static void AssertAliased(IResourceHost host, MaterialTokenKey compFont, MaterialTokenKey sysFont)
        {
            var expected = TokenScan.Resolve(host, sysFont);
            var actual = TokenScan.Resolve(host, compFont);
            Assert.Equal(expected, actual);
        }

        AssertAliased(host, Comp.ButtonSmall.LabelTextSize, Sys.TypeScale.LabelLarge.Size);
        AssertAliased(host, Comp.ButtonSmall.LabelTextWeight, Sys.TypeScale.LabelLarge.Weight);
        AssertAliased(host, Comp.ButtonXSmall.LabelTextSize, Sys.TypeScale.LabelLarge.Size);
        AssertAliased(host, Comp.ButtonMedium.LabelTextSize, Sys.TypeScale.TitleMedium.Size);
        AssertAliased(host, Comp.ButtonMedium.LabelTextLineHeight, Sys.TypeScale.TitleMedium.LineHeight);
        AssertAliased(host, Comp.ButtonLarge.LabelTextSize, Sys.TypeScale.HeadlineSmall.Size);
        AssertAliased(host, Comp.ButtonLarge.LabelTextTracking, Sys.TypeScale.HeadlineSmall.LetterSpacing);
        AssertAliased(host, Comp.ButtonXLarge.LabelTextSize, Sys.TypeScale.HeadlineLarge.Size);
        AssertAliased(host, Comp.ButtonXLarge.LabelTextFont, Sys.TypeScale.HeadlineLarge.Font);
    }

    [AvaloniaFact]
    public void ButtonSizes_Miss_StaysHonest()
    {
        Assert.False(new ButtonSizesTokenSource().TryGetToken(Comp.FilledButton.ContainerColor, null, out _));
        Assert.False(new ShapeTokenSource().TryGetToken(Comp.ButtonSmall.ContainerHeight, null, out _));
    }
}

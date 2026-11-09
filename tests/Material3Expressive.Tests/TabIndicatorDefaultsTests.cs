using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class TabIndicatorDefaultsTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void PrimaryRequiredSizeOverflowsAndSecondaryHeightObeysOuterSize(bool rtl)
    {
        var primary = new PrimaryTabIndicator { Width = 1, Height = 1, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var secondary = new SecondaryTabIndicator { Width = 1, Height = 1 };
        var root = new StackPanel { Children = { primary, secondary } };
        var window = Show(root);
        try
        {
            Assert.Equal(new Size(1, 1), primary.Bounds.Size);
            Assert.Equal(new Size(24, 3), Surface(primary).Bounds.Size);
            Assert.Equal(rtl ? -12 : -11, Surface(primary).Bounds.X);
            Assert.Equal(-1, Surface(primary).Bounds.Y);
            Assert.Equal(new Size(1, 1), Surface(secondary).Bounds.Size);
            Assert.Equal(0, secondary.Bounds.X);
            Assert.False(primary.ClipToBounds); Assert.False(primary.IsHitTestVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PrimaryShapeWidthAndThemeDefaultsRemainLive()
    {
        var primary = new PrimaryTabIndicator();
        var window = Show(primary);
        try
        {
            Assert.Equal(new Size(24, 3), Surface(primary).Bounds.Size);
            window.Resources[Comp.TabRow.PrimaryIndicatorWidth] = 36d;
            window.Resources[Comp.TabRow.IndicatorHeight] = 7d;
            window.Resources[Comp.TabRow.IndicatorColor] = Colors.Red;
            window.UpdateLayout();
            Assert.Equal(new Size(36, 7), Surface(primary).Bounds.Size);
            Assert.Equal(Colors.Red, ((ISolidColorBrush)primary.Background!).Color);
            var shape = new CutCornerShape(3);
            primary.Shape = shape; window.UpdateLayout();
            Assert.Same(shape, ((MaterialSurface)Surface(primary)).Shape);
            primary.IndicatorWidth = 12; window.Resources[Comp.TabRow.PrimaryIndicatorWidth] = 48d; window.UpdateLayout();
            Assert.Equal(12, Surface(primary).Bounds.Width);
            primary.ClearValue(PrimaryTabIndicator.IndicatorWidthProperty); window.UpdateLayout();
            Assert.Equal(48, Surface(primary).Bounds.Width);
            primary.Shape = null; window.UpdateLayout();
            Assert.Null(((MaterialSurface)Surface(primary)).Shape);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void UnspecifiedPrimaryWidthFollowsExplicitAllocationAndSecondaryFillsAvailableWidth()
    {
        var primary = new PrimaryTabIndicator { Width = 75, IndicatorWidth = double.NaN };
        var secondary = new SecondaryTabIndicator();
        var root = new StackPanel { Width = 120, Children = { primary, secondary } };
        var window = Show(root);
        try
        {
            Assert.Equal(new Size(75, 3), Surface(primary).Bounds.Size);
            Assert.Equal(new Size(120, 3), Surface(secondary).Bounds.Size);
            primary.IndicatorHeight = 0; secondary.IndicatorHeight = 8; window.UpdateLayout();
            Assert.Equal(0, Surface(primary).Bounds.Height); Assert.Equal(8, Surface(secondary).Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void PrimaryCanFollowTheAnimatedRowWidthInEitherEntry(bool native)
    {
        using var clock = new MotionTestClockScope();
        var indicator = new PrimaryTabIndicator { IndicatorWidth = double.NaN, IndicatorHeight = 6, HorizontalAlignment = HorizontalAlignment.Stretch,
            Shape = new CutCornerShape(3) };
        ItemsControl row = native ? new TabControl() : new TabRow();
        row.Width = 240; row.VerticalAlignment = VerticalAlignment.Top;
        TabRowAppearance.SetIndicator(row, new FuncControlTemplate((_, _) => indicator));
        foreach (var width in new[] { 32d, 64d })
        {
            var label = new Border { Width = width, Height = 16 };
            row.Items.Add(native ? new TabItem { Header = label } : new Tab { Content = label });
        }
        var window = Show(row);
        try
        {
            Assert.Equal(32, Surface(indicator).Bounds.Width);
            Assert.Equal(6, Surface(indicator).Bounds.Height);
            clock.SendFrame(TimeSpan.Zero);
            if (row is TabRow controlled) controlled.SelectedIndex = 1;
            else ((TabControl)row).SelectedIndex = 1;
            window.UpdateLayout();
            for (var frame = 0; frame < 125; frame++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.Equal(64, Surface(indicator).Bounds.Width);
            Assert.Same(indicator.Shape, ((MaterialSurface)Surface(indicator)).Shape);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static Control Surface(Control control) => control.GetVisualDescendants().OfType<Control>().Single(x => x.Name == "PART_IndicatorSurface");
    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 320, Height = 180 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } }; window.Show(); window.UpdateLayout(); return window;
    }
}

using Material3Expressive.Foundation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class ChipArrangementTests
{
    [AvaloniaTheory]
    [InlineData("Start", 0, 10, 30)]
    [InlineData("End", 40, 50, 70)]
    [InlineData("Center", 20, 30, 50)]
    [InlineData("SpaceBetween", 0, 30, 70)]
    [InlineData("SpaceAround", 7, 30, 63)]
    [InlineData("SpaceEvenly", 10, 30, 60)]
    public void StandardArrangementPlacesAllThreeSlots(string arrangement, double first, double second, double third)
    {
        var panel = new ChipContentLayout { HorizontalArrangement = MaterialHorizontalArrangement.Parse(arrangement) };
        foreach (var width in new[] { 10, 20, 30 }) panel.Children.Add(new Border { Width = width, Height = 16 });
        panel.Measure(new Size(100, 32)); panel.Arrange(new Rect(0, 0, 100, 32));
        Assert.Equal(new Size(60, 16), panel.DesiredSize);
        Assert.Equal(new[] { first, second, third }, panel.Children.Select(c => c.Bounds.X));
        panel.FlowDirection = FlowDirection.RightToLeft;
        panel.InvalidateArrange(); panel.Arrange(new Rect(0, 0, 100, 32));
        Assert.Equal(new[] { first, second, third }, panel.Children.Select(c => c.Bounds.X));
    }

    [AvaloniaFact]
    public void FractionalGapsRoundInPhysicalRtlOrder()
    {
        var panel = new ChipContentLayout { HorizontalArrangement = MaterialHorizontalArrangement.SpaceAround };
        foreach (var width in new[] { 10, 20, 30 }) panel.Children.Add(new Border { Width = width, Height = 16 });
        panel.Measure(new Size(101, 32)); panel.Arrange(new Rect(0, 0, 101, 32));
        Assert.Equal(new[] { 7d, 31d, 64d }, panel.Children.Select(c => c.Bounds.X));
        panel.FlowDirection = FlowDirection.RightToLeft;
        panel.InvalidateArrange(); panel.Arrange(new Rect(0, 0, 101, 32));
        Assert.Equal(new[] { 7d, 30d, 64d }, panel.Children.Select(c => c.Bounds.X));
    }

    [AvaloniaFact]
    public void CustomArrangementOverridesSpacingAndClearingItRestoresTheLatestConfiguration()
    {
        var chip = new FilterChip { Width = 160, Content = new Border { Width = 32, Height = 16 },
            HorizontalArrangement = MaterialHorizontalArrangement.Center, Spacing = new ChipSpacing(20, 30) };
        var window = new MotionTestWindow { Width = 300, Height = 160 };
        window.Styles.Add(new MaterialTheme()); window.Content = chip; window.Show(); Flush(window);
        try
        {
            var panel = chip.GetVisualDescendants().OfType<ChipContentLayout>().Single();
            var before = panel.Children[1].Bounds;
            chip.Spacing = new ChipSpacing(8, 12); Flush(window);
            Assert.Equal(before, panel.Children[1].Bounds);
            chip.HorizontalArrangement = null; Flush(window);
            Assert.Null(panel.HorizontalArrangement);
            Assert.Equal(12, panel.Children[1].Bounds.X);
            Assert.Same(chip.Spacing, panel.Spacing);
        }
        finally { window.Close(); }
    }

    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void BasicChipLabelFillsRemainingSlotBeforeApplyingArrangement(bool suggestion)
    {
        Chip chip = suggestion ? new SuggestionChip() : new AssistChip();
        chip.Width = 160; chip.Content = new Border { Width = 32, Height = 16 };
        chip.HorizontalArrangement = MaterialHorizontalArrangement.Center;
        var window = new MotionTestWindow { Width = 300, Height = 160 };
        window.Styles.Add(new MaterialTheme()); window.Content = chip; window.Show(); Flush(window);
        try
        {
            var panel = chip.GetVisualDescendants().OfType<ChipContentLayout>().Single();
            Assert.Equal(0, panel.Children[1].Bounds.X);
            Assert.Equal(panel.Bounds.Width, panel.Children[1].Bounds.Width);
            chip.HorizontalArrangement = MaterialHorizontalArrangement.End; Flush(window);
            Assert.Equal(0, panel.Children[1].Bounds.X);
            Assert.Equal(panel.Bounds.Width, panel.Children[1].Bounds.Width);
        }
        finally { window.Close(); }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class FabShapeTests
{
    [AvaloniaTheory]
    [InlineData(false, FloatingActionButtonSize.Small)]
    [InlineData(false, FloatingActionButtonSize.Default)]
    [InlineData(false, FloatingActionButtonSize.Medium)]
    [InlineData(false, FloatingActionButtonSize.Large)]
    [InlineData(true, FloatingActionButtonSize.Small)]
    [InlineData(true, FloatingActionButtonSize.Default)]
    [InlineData(true, FloatingActionButtonSize.Medium)]
    [InlineData(true, FloatingActionButtonSize.Large)]
    public void ShapeDoesNotChangeAllocationOrActivationAndClearRestoresCorners(bool extended, FloatingActionButtonSize size)
    {
        using var clock = new MotionTestClockScope();
        FloatingActionButton fab = extended ? new ExtendedFloatingActionButton { Icon = new Border { Width = 24, Height = 24 } } : new();
        fab.Size = size; fab.Content = new Border { Width = 48, Height = 16 };
        var window = DialogTests.Show(new Grid { Children = { fab } });
        try
        {
            var before = fab.DesiredSize;
            var cut = new CutCornerShape(8); fab.Shape = cut; window.UpdateLayout();
            var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Same(cut, surface.Shape); Assert.Equal(before, fab.DesiredSize);
            Assert.Equal(8, Cut(surface));
            cut.TopStart = new(12, RelativeUnit.Absolute); Assert.Equal(12, Cut(surface));
            var clicks = 0; fab.Click += (_, _) => clicks++;
            fab.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.Equal(1, clicks);
            fab.Shape = null; window.UpdateLayout();
            Assert.Null(surface.ContainerGeometry); Assert.Equal(fab.CornerRadius, surface.EffectiveCornerRadius);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ExtendedPercentPathFollowsResizeAndReattachesWithLatestShape()
    {
        using var clock = new MotionTestClockScope();
        var cut = new CutCornerShape(new RelativeScalar(.25, RelativeUnit.Relative));
        var fab = new ExtendedFloatingActionButton { Shape = cut, Icon = new Border { Width = 24, Height = 24 },
            Content = new Border { Width = 100, Height = 16 } };
        var panel = new StackPanel { Children = { fab } };
        var window = DialogTests.Show(panel);
        try
        {
            var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(14, Cut(surface));
            fab.Expanded = false;
            for (var i = 0; i < 50; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout(); Assert.Equal(14, Cut(surface));
            fab.Size = FloatingActionButtonSize.Large; window.UpdateLayout(); Assert.Equal(24, Cut(surface));
            panel.Children.Remove(fab); cut.TopStart = new(4, RelativeUnit.Absolute);
            panel.Children.Add(fab); window.UpdateLayout();
            Assert.Equal(4, Cut(fab.GetVisualDescendants().OfType<MaterialSurface>().Single()));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
    private static double Cut(MaterialSurface surface) => ((PathGeometry)surface.ContainerGeometry!).Figures![0].StartPoint.Y;
}

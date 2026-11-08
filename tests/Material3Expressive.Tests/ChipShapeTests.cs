using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class ChipShapeTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void CutCornersMorphAndPressOverridesSelection(bool input)
    {
        using var clock = new MotionTestClockScope();
        SelectableChip chip = input ? new InputChip() : new FilterChip();
        chip.Shapes = new(new CutCornerShape(12), new CutCornerShape(new RelativeScalar(.5, RelativeUnit.Relative)), new CutCornerShape(4));
        var source = chip.InteractionSource = new();
        var window = Show(chip);
        try
        {
            var surface = Surface(chip);
            Assert.IsType<CutCornerShape>(surface.Shape);
            Assert.Equal(12, Corner(surface));
            chip.IsSelected = true; Frames(clock, 5);
            Assert.InRange(Corner(surface), 12.01, 15.99);
            Frames(clock, 60); Assert.Equal(16, Corner(surface));
            var press = new MaterialInteractionEvent.Press(new Point(16, 16)); source.Emit(press);
            Frames(clock, 60); Assert.Equal(4, Corner(surface));
            chip.IsSelected = false; Frames(clock, 4); Assert.Equal(4, Corner(surface));
            source.Emit(new MaterialInteractionEvent.Release(press)); Frames(clock, 60);
            Assert.Equal(12, Corner(surface));
            Assert.False(chip.IsSelected);
            Assert.Null(MotionTestClockScope.Error(chip));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void MixedFamiliesSwitchAtTheAnimatedMidpointAndReverse()
    {
        using var clock = new MotionTestClockScope();
        var chip = new FilterChip { Shapes = new(new CutCornerShape(12), new RoundedCornerShape(8), new CutCornerShape(4)) };
        var window = Show(chip);
        try
        {
            var surface = Surface(chip);
            chip.IsSelected = true; Frames(clock, 2);
            Assert.IsType<CutCornerShape>(surface.Shape);
            Assert.Equal(12, Corner(surface));
            Frames(clock, 60);
            Assert.Null(surface.Shape); Assert.Equal(8, Corner(surface));
            chip.IsSelected = false; Frames(clock, 2);
            Assert.Null(surface.Shape);
            Frames(clock, 60); Assert.IsType<CutCornerShape>(surface.Shape);
            Assert.Equal(12, Corner(surface));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OneGenericEntryDisablesInterpolationForAllStates()
    {
        using var clock = new MotionTestClockScope();
        var rest = new CutCornerShape(12); var selected = new RoundedCornerShape(4);
        var generic = new Triangle();
        var chip = new InputChip { Shapes = new(rest, selected, generic), InteractionSource = new() };
        var window = Show(chip);
        try
        {
            Assert.Same(rest, Surface(chip).Shape);
            chip.IsSelected = true; Frames(clock, 1);
            Assert.Same(selected, Surface(chip).Shape);
            var press = new MaterialInteractionEvent.Press(new Point(16, 16)); chip.InteractionSource.Emit(press);
            Frames(clock, 1); Assert.Same(generic, Surface(chip).Shape);
            chip.InteractionSource.Emit(new MaterialInteractionEvent.CancelPress(press)); Frames(clock, 1);
            Assert.Same(selected, Surface(chip).Shape);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void MutableConfigurationRekeysAndDetachesWhileFixedShapeKeepsItsBinding(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var corners = new CutCornerShape { TopStart = new(12, RelativeUnit.Absolute) };
        var chip = new FilterChip { Shapes = new(corners, new CutCornerShape(8), new CutCornerShape(4)),
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(chip);
        try
        {
            var surface = Surface(chip);
            Assert.Equal(12, Corner(surface));
            corners.TopStart = new(6, RelativeUnit.Absolute); Frames(clock, 1);
            Assert.Equal(6, Corner(surface));
            var fixedShape = new Triangle(); chip.Shape = fixedShape; Frames(clock, 1);
            Assert.Same(fixedShape, surface.Shape);
            corners.TopStart = new(10, RelativeUnit.Absolute); Frames(clock, 2);
            Assert.Same(fixedShape, surface.Shape);
            chip.Shape = null; Frames(clock, 1); Assert.Equal(10, Corner(surface));
            window.Content = null; Flush(window);
            corners.TopStart = new(2, RelativeUnit.Absolute); Frames(clock, 2);
            Assert.Equal(0, clock.PendingFrameCount);
            window.Content = chip; Flush(window); Frames(clock, 1);
            Assert.Equal(2, Corner(Surface(chip)));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PercentCornersReevaluateAfterResizeAndEqualSnapshotsKeepTheAnimation()
    {
        using var clock = new MotionTestClockScope();
        static ChipShapes Shapes() => new(new CutCornerShape(4), new CutCornerShape(new RelativeScalar(.5, RelativeUnit.Relative)), new CutCornerShape(2));
        var chip = new InputChip { Shapes = Shapes() }; var window = Show(chip);
        try
        {
            chip.IsSelected = true; Frames(clock, 5);
            var surface = Surface(chip); var before = Corner(surface);
            chip.Shapes = Shapes(); Frames(clock, 1);
            Assert.InRange(Corner(surface), before, 15.99);
            Frames(clock, 60); Assert.Equal(16, Corner(surface));
            chip.Width = 120; chip.Height = 64; Flush(window); Assert.Equal(32, Corner(surface));
            chip.Shapes = null; Frames(clock, 1);
            Assert.Null(surface.Shape); Assert.Equal(chip.CornerRadius, surface.EffectiveCornerRadius);
        }
        finally { window.Close(); }
    }

    private sealed class Triangle : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
            => PathGeometry.Parse(FormattableString.Invariant($"M0,{size.Height} L{size.Width / 2},0 L{size.Width},{size.Height} Z"));
    }
    private static MotionTestWindow Show(SelectableChip chip)
    {
        var window = new MotionTestWindow { Width = 300, Height = 160 };
        window.Styles.Add(new MaterialTheme());
        chip.Content = new Border { Width = 32, Height = 16 };
        chip.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        chip.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        window.Content = chip; window.Show(); Flush(window); return window;
    }
    private static double Corner(MaterialSurface surface)
    {
        if (surface.Shape is not CutCornerShape) return surface.EffectiveCornerRadius.TopLeft;
        var figure = ((PathGeometry)surface.ContainerGeometry!).Figures![0];
        return surface.FlowDirection == FlowDirection.RightToLeft
            ? ((LineSegment)figure.Segments![2]).Point.Y : figure.StartPoint.Y;
    }
    private static MaterialSurface Surface(Chip chip) => chip.GetVisualDescendants().OfType<MaterialSurface>().Single();
    private static void Frames(MotionTestClockScope clock, int count) { for (var i = 0; i < count; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

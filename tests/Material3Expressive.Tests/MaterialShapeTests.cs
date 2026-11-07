using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class MaterialShapeTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectableChipExplicitShapeKeepsItsOutlineAcrossSelectionAndRestoresMorphWhenCleared(bool input)
    {
        using var clock = new MotionTestClockScope();
        SelectableChip chip = input ? new InputChip() : new FilterChip();
        chip.Width = 120; chip.Height = 48; chip.Content = "Choice";
        chip.Shape = new CutCornerShape(12);
        var source = chip.InteractionSource = new MaterialInteractionSource();
        var window = DialogTests.Show(new Grid { Children = { chip } });
        try
        {
            window.UpdateLayout();
            var surface = chip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var outline = surface.ContainerGeometry!.ToString();
            chip.IsSelected = true;
            var press = new MaterialInteractionEvent.Press(new(60, 24)); source.Emit(press);
            for (var i = 0; i < 30; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(outline, surface.ContainerGeometry!.ToString());
            Assert.True(chip.IsSelected);
            source.Emit(new MaterialInteractionEvent.Release(press));
            chip.Shape = null;
            for (var i = 0; i < 30; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Null(surface.ContainerGeometry);
            Assert.Equal(surface.Bounds.Height / 2, surface.EffectiveCornerRadius.TopLeft);
            chip.IsSelected = false;
            for (var i = 0; i < 30; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(chip.Shapes!.Shape.TopLeft, surface.EffectiveCornerRadius.TopLeft);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CutsNormalizeEachLogicalSideAndResolvePercentAtCurrentSize(bool rtl)
    {
        var shape = new CutCornerShape
        {
            TopStart = new(60, RelativeUnit.Absolute), BottomStart = new(20, RelativeUnit.Absolute),
            TopEnd = new(.25, RelativeUnit.Relative), BottomEnd = new(.5, RelativeUnit.Relative)
        };
        var direction = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var outline = shape.CreateOutline(new Size(100, 40), direction, 4);
        Assert.Equal(new Point(0, rtl ? 10 : 30), outline.Figures![0].StartPoint);
        var smaller = shape.CreateOutline(new Size(100, 20), direction, 4);
        Assert.Equal(new Point(0, rtl ? 5 : 15), smaller.Figures![0].StartPoint);
        Assert.Throws<ArgumentException>(() => shape.TopStart = new(-1, RelativeUnit.Absolute));
        Assert.Throws<ArgumentException>(() => shape.BottomEnd = new(1.1, RelativeUnit.Relative));
    }

    [AvaloniaTheory]
    [InlineData("surface")]
    [InlineData("click")]
    [InlineData("select")]
    [InlineData("toggle")]
    [InlineData("card")]
    [InlineData("elevated")]
    [InlineData("outlined")]
    [InlineData("assist")]
    [InlineData("suggestion")]
    [InlineData("filter")]
    [InlineData("input")]
    public void ShapeUpdatesFlowThroughRealTemplatesAndReleaseOldSubscriptions(string kind)
    {
        var shape = new CutCornerShape(20);
        Control control = kind switch
        {
            "surface" => new MaterialSurface { Shape = shape, Child = new Border() },
            "click" => new ClickableSurface { Shape = shape, Content = new Border() },
            "select" => new SelectableSurface { Shape = shape, Content = new Border() },
            "toggle" => new ToggleableSurface { Shape = shape, Content = new Border() },
            "elevated" => new ElevatedCard { Shape = shape, Content = new Border() },
            "outlined" => new OutlinedCard { Shape = shape, Content = new Border() },
            "assist" => new AssistChip { Shape = shape, Content = new Border() },
            "suggestion" => new SuggestionChip { Shape = shape, Content = new Border() },
            "filter" => new FilterChip { Shape = shape, Content = new Border() },
            "input" => new InputChip { Shape = shape, Content = new Border() },
            _ => new Card { Shape = shape, IsClickable = true, Content = new Border() }
        };
        control.Width = 120; control.Height = 64;
        var parent = new Grid { Children = { control } };
        var window = DialogTests.Show(parent);
        try
        {
            window.UpdateLayout();
            var surface = control as MaterialSurface ?? control.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Same(shape, surface.Shape);
            Assert.False(surface.ContainerGeometry!.FillContains(new Point(1, 1)));
            Assert.True(surface.ContainerGeometry.FillContains(new Point(60, 32)));
            var bounds = surface.Child!.Bounds;
            shape.TopStart = new(0, RelativeUnit.Absolute);
            Assert.True(surface.ContainerGeometry.FillContains(new Point(1, 1)));
            Assert.Equal(bounds, surface.Child.Bounds);
            var replacement = new CutCornerShape(5);
            surface.Shape = replacement;
            var geometry = surface.ContainerGeometry;
            shape.TopStart = new(30, RelativeUnit.Absolute);
            Assert.Same(geometry, surface.ContainerGeometry);
            parent.Children.Clear();
            replacement.TopStart = new(15, RelativeUnit.Absolute);
            Assert.Same(geometry, surface.ContainerGeometry);
            parent.Children.Add(control); window.UpdateLayout();
            Assert.NotSame(geometry, surface.ContainerGeometry);
            surface.Shape = null; surface.CornerRadius = new(0);
            Assert.Null(surface.ContainerGeometry);
            Assert.Equal(bounds, surface.Child.Bounds);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CustomPathTransformFillRuleAndContentClipShareOneSnapshot()
    {
        var surface = new MaterialSurface { Shape = new HoleShape(), Width = 100, Height = 60,
            Child = new Border { Background = Brushes.Blue }, HorizontalAlignment = HorizontalAlignment.Left };
        var window = DialogTests.Show(surface);
        try
        {
            window.UpdateLayout();
            var geometry = Assert.IsType<PathGeometry>(surface.ContainerGeometry);
            Assert.False(geometry.FillContains(new Point(50, 30)));
            Assert.True(geometry.FillContains(new Point(10, 10)));
            Assert.Same(geometry, surface.GetVisualChildren().OfType<Border>().Single().Clip);
            surface.FlowDirection = FlowDirection.RightToLeft;
            Assert.False(surface.ContainerGeometry!.FillContains(new Point(50, 30)));
        }
        finally { window.Close(); }
    }

    private sealed class HoleShape : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density) =>
            PathGeometry.Parse("F0 M0,0 L100,0 L100,60 L0,60 Z M40,20 L60,20 L60,40 L40,40 Z");
    }

    [AvaloniaFact]
    public void CutCornerRejectsMouseOutsideOutlineButKeepsCenterActivation()
    {
        var surface = new ClickableSurface { Shape = new CutCornerShape(24), Width = 120, Height = 64,
            Background = Brushes.Blue, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = DialogTests.Show(surface);
        var clicks = 0; surface.Click += (_, _) => clicks++;
        try
        {
            window.UpdateLayout();
            using var frame = window.CaptureRenderedFrame();
            var origin = surface.TranslatePoint(default, window)!.Value;
            window.MouseDown(origin + new Vector(1, 1), MouseButton.Left);
            window.MouseUp(origin + new Vector(1, 1), MouseButton.Left);
            Assert.Equal(0, clicks);
            window.MouseDown(origin + new Vector(60, 32), MouseButton.Left);
            window.MouseUp(origin + new Vector(60, 32), MouseButton.Left);
            Assert.Equal(1, clicks);
        }
        finally { window.Close(); }
    }
}

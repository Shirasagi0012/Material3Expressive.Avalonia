using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class MenuGroupShapeTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void MenuItemSelectionMorphsAndRekeysLiveShapeConfiguration(bool checkable)
    {
        using var clock = new MotionTestClockScope();
        DropdownMenuItem item = checkable ? new CheckableDropdownMenuItem() : new SelectableDropdownMenuItem();
        item.Width = 200; item.Height = 64; item.Shape = new CutCornerShape(4);
        item.SelectedShape = new CutCornerShape(new RelativeScalar(.25, RelativeUnit.Relative));
        var window = DialogTests.Show(item);
        MaterialSurface Surface() => item.GetVisualDescendants().OfType<MaterialSurface>().Single();
        double Corner() => ((PathGeometry)Surface().ContainerGeometry!).Figures![0].StartPoint.Y;
        void Frames(int count = 50) { for (var i = 0; i < count; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
        void Select(bool value) { if (item is CheckableDropdownMenuItem check) check.IsChecked = value; else ((SelectableDropdownMenuItem)item).IsSelected = value; }
        try
        {
            Assert.Equal(4, Corner()); Select(true); Frames(4);
            Assert.InRange(Corner(), 4.01, 15.99);
            item.Shape = new CutCornerShape(4); Frames(1);
            Assert.InRange(Corner(), 4.01, 15.99); // equal corner values must not rekey to the final selected state
            Select(false); Frames(); Assert.Equal(4, Corner());
            Select(true); Frames(); Assert.Equal(Math.Min(Surface().Bounds.Width, Surface().Bounds.Height) * .25, Corner());
            item.SelectedShape = new Triangle(); Frames(1); Assert.Same(item.SelectedShape, Surface().Shape);
            item.SelectedShape = null; Frames(1); Assert.Null(Surface().Shape);
            Assert.Equal(item.SelectedCornerRadius, Surface().EffectiveCornerRadius);
            var old = Surface(); var template = item.Template;
            item.Template = null; item.ApplyTemplate(); item.Template = template; item.ApplyTemplate(); window.UpdateLayout();
            Assert.NotSame(old, Surface()); Assert.Null(old.Shape);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
    [AvaloniaFact]
    public void CutCornersRetainHoverHistoryReverseAndRecomputePercentageOnResize()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var inactive = new CutCornerShape(new RelativeScalar(.25, RelativeUnit.Relative));
        var group = new DropdownMenuGroup { Width = 180, Height = 64, InteractionSource = source,
            Shape = new CutCornerShape(4), InactiveShape = inactive };
        var window = DialogTests.Show(group);
        var surface = group.GetVisualDescendants().OfType<MaterialSurface>().Single();
        double Corner() => ((PathGeometry)surface.ContainerGeometry!).Figures![0].StartPoint.Y;
        void Frames(int count = 50) { for (var i = 0; i < count; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
        try
        {
            Assert.Equal(4, Corner()); Frames(); Assert.Equal(4, Corner());
            var first = new MaterialInteractionEvent.HoverEnter(); source.Emit(first); Frames();
            source.Emit(new MaterialInteractionEvent.HoverExit(first)); Frames(4);
            Assert.InRange(Corner(), 4.01, 15.99);
            var second = new MaterialInteractionEvent.HoverEnter(); source.Emit(second); Frames();
            Assert.Equal(4, Corner());
            source.Emit(new MaterialInteractionEvent.HoverExit(second)); Frames(); Assert.Equal(16, Corner());
            group.Height = 80; window.UpdateLayout(); Assert.Equal(20, Corner());
            inactive.TopStart = new(.4, RelativeUnit.Relative); Frames(1); Assert.Equal(32, Corner());
            window.Content = null; Assert.Equal(0, clock.PendingFrameCount);
            window.Content = group; window.UpdateLayout(); Assert.Equal(4, Corner());
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void GenericEndpointSwitchesDirectlyAndClearingItRestoresCornerAnimation()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var generic = new Triangle();
        var group = new DropdownMenuGroup { Width = 180, Height = 64, InteractionSource = source,
            Shape = new CutCornerShape(4), InactiveShape = generic, InactiveCornerRadius = new(12) };
        var window = DialogTests.Show(group);
        MaterialSurface Surface() => group.GetVisualDescendants().OfType<MaterialSurface>().Single();
        void Frame() => clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        try
        {
            var hover = new MaterialInteractionEvent.HoverEnter(); source.Emit(hover); Frame();
            source.Emit(new MaterialInteractionEvent.HoverExit(hover)); Frame();
            Assert.Same(generic, Surface().Shape);
            group.InactiveShape = null; Frame(); Assert.Null(Surface().Shape);
            Assert.Equal(new CornerRadius(12), Surface().EffectiveCornerRadius);
            var old = Surface(); var template = group.Template;
            group.Template = null; group.ApplyTemplate(); group.Template = template; group.ApplyTemplate(); window.UpdateLayout();
            Assert.NotSame(old, Surface()); Assert.Null(old.Shape);
            Assert.Equal(new CornerRadius(12), Surface().EffectiveCornerRadius);
            window.Content = null; generic.Invalidate(); Assert.Equal(0, clock.PendingFrameCount);
        }
        finally { window.Close(); }
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    private sealed class Triangle : MaterialShape
    {
        public static readonly StyledProperty<double> InsetProperty = AvaloniaProperty.Register<Triangle, double>("Inset");
        internal void Invalidate() => SetValue(InsetProperty, 1);
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        {
            var path = new PathGeometry(); using var context = path.Open();
            context.BeginFigure(default, true); context.LineTo(new(size.Width, 0));
            context.LineTo(new(size.Width / 2, size.Height)); context.EndFigure(true); return path;
        }
    }
}

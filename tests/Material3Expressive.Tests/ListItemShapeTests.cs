using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class ListItemShapeTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void KeyboardPressBeforeAnyPointerEventStartsAtPhysicalTopLeft(bool rtl)
    {
        var source = new MaterialInteractionSource(); var presses = new List<MaterialInteractionEvent.Press>();
        source.Interaction += e => { if (e is MaterialInteractionEvent.Press press) presses.Add(press); };
        var item = Create(false, source); item.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var window = DialogTests.Show(item);
        try
        {
            item.Focus(NavigationMethod.Tab); window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            var press = Assert.Single(presses);
            Assert.Equal(new Point(rtl ? item.Bounds.Width : 0, 0), press.Position);
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void SingleIndicationFocusRingIsClippedAndUsesTheSurfaceDirection()
    {
        var item = Create(false, new()); item.FlowDirection = FlowDirection.RightToLeft;
        MaterialInteraction.SetFocusFeedback(item, FocusFeedback.InsetRing);
        var window = DialogTests.Show(item);
        try
        {
            var ring = item.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
            Assert.Same(item.Surface!.ContainerGeometry, ring.Clip);
            Assert.Equal(item.Surface.FlowDirection, ring.FlowDirection);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void SixCutStatesMorphWithPriorityReversalAndLivePercentageSize(bool segmented)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var item = Create(segmented, source);
        var window = DialogTests.Show(new Grid { Children = { item } });
        void Frames(int count = 50) { for (var i = 0; i < count; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
        double Corner() => ((PathGeometry)item.Surface!.ContainerGeometry!).Figures![0].StartPoint.Y;
        try
        {
            Assert.Equal(4, Corner());
            var hover = new MaterialInteractionEvent.HoverEnter(); source.Emit(hover); Frames();
            Assert.Equal(8, Corner());
            var focus = new MaterialInteractionEvent.Focus(); source.Emit(focus); Frames();
            Assert.Equal(12, Corner());
            item.IsChecked = true; Frames(); Assert.Equal(16, Corner());
            var drag = new MaterialInteractionEvent.DragStart(); source.Emit(drag); Frames();
            Assert.Equal(20, Corner());
            var press = new MaterialInteractionEvent.Press(new Point(30, 30)); source.Emit(press); Frames(4);
            Assert.InRange(Corner(), 4.01, 19.99);
            source.Emit(new MaterialInteractionEvent.Release(press)); Frames(); Assert.Equal(20, Corner());
            source.Emit(new MaterialInteractionEvent.DragCancel(drag)); Frames(); Assert.Equal(16, Corner());
            item.IsChecked = false; Frames(); Assert.Equal(12, Corner());
            source.Emit(new MaterialInteractionEvent.Unfocus(focus)); Frames(); Assert.Equal(8, Corner());
            source.Emit(new MaterialInteractionEvent.HoverExit(hover)); Frames(); Assert.Equal(4, Corner());

            item.Shape = new CutCornerShape(new RelativeScalar(.25, RelativeUnit.Relative)); Frames();
            Assert.Equal(16, Corner());
            item.Height = 80; window.UpdateLayout(); Assert.Equal(20, Corner());
            ((CutCornerShape)item.Shape).TopStart = new(.4, RelativeUnit.Relative); Frames(1);
            Assert.Equal(32, Corner());
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void GenericEntryDisablesCornerAnimationAndChangingConfigurationRekeysIt()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var item = Create(false, source);
        var path = new TriangleShape(); item.DraggedShape = path;
        var window = DialogTests.Show(new Grid { Children = { item } });
        try
        {
            var hover = new MaterialInteractionEvent.HoverEnter(); source.Emit(hover);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Same(item.HoveredShape, item.Surface!.Shape); // no corner morph while any entry is generic
            var drag = new MaterialInteractionEvent.DragStart(); source.Emit(drag);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Assert.Same(path, item.Surface.Shape);
            item.DraggedShape = new CutCornerShape(18);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(18, ((PathGeometry)item.Surface.ContainerGeometry!).Figures![0].StartPoint.Y);
            source.Emit(new MaterialInteractionEvent.DragStop(drag));
            for (var i = 0; i < 50; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(8, ((PathGeometry)item.Surface.ContainerGeometry!).Figures![0].StartPoint.Y);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void UnmountAndTemplateReplacementReleaseShapeObserversAndRestoreCurrentConfiguration()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var item = Create(false, source);
        var root = new Grid { Children = { item } }; var window = DialogTests.Show(root);
        try
        {
            source.Emit(new MaterialInteractionEvent.HoverEnter());
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            var old = item.Surface; var template = item.Template;
            item.Template = null; item.ApplyTemplate(); item.Template = template; item.ApplyTemplate(); window.UpdateLayout();
            Assert.NotSame(old, item.Surface); Assert.Null(old!.Shape);
            root.Children.Remove(item); Assert.Equal(0, clock.PendingFrameCount);
            ((CutCornerShape)item.Shape!).TopStart = new(9, RelativeUnit.Absolute);
            Assert.Equal(0, clock.PendingFrameCount);
            root.Children.Add(item); window.UpdateLayout();
            Assert.Equal(9, ((PathGeometry)item.Surface!.ContainerGeometry!).Figures![0].StartPoint.Y);
            item.Shape = item.SelectedShape = item.PressedShape = item.FocusedShape = item.HoveredShape = item.DraggedShape = null;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Null(item.Surface.ContainerGeometry); Assert.Equal(new CornerRadius(4), item.Surface.EffectiveCornerRadius);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void MixedFamilyInterpolationUsesHalfwayEndpointAndPreservesCapturedFamily()
    {
        CornerShape round = new CornerShape.Defined(new CornerRadius(8));
        CornerShape cut = CornerShape.Cut.From(new CutCornerShape(20));
        Assert.Same(round, CornerShape.Interpolate(round, cut, .49f));
        Assert.Same(cut, CornerShape.Interpolate(round, cut, .5f));
        var otherCut = CornerShape.Cut.From(new CutCornerShape(4));
        var captured = CornerShape.Interpolate(cut, otherCut, .25f);
        Assert.True(captured.IsCut); Assert.Equal(new CornerRadius(16), captured.Resolve(new Size(120, 64)));
        Assert.Same(captured, CornerShape.Interpolate(captured, round, .49f));
    }

    private static ListItem Create(bool segmented, MaterialInteractionSource source)
    {
        ListItem item = segmented ? new SegmentedListItem() : new ListItem();
        item.Width = 200; item.Height = 64; item.InteractionMode = ListItemInteractionMode.MultipleSelection;
        item.InteractionSource = source; item.Shape = new CutCornerShape(4); item.SelectedShape = new CutCornerShape(16);
        item.PressedShape = new CutCornerShape(4); item.FocusedShape = new CutCornerShape(12);
        item.HoveredShape = new CutCornerShape(8); item.DraggedShape = new CutCornerShape(20);
        return item;
    }

    private sealed class TriangleShape : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        {
            var path = new PathGeometry(); using var context = path.Open();
            context.BeginFigure(default, true); context.LineTo(new(size.Width, size.Height / 2));
            context.LineTo(new(0, size.Height)); context.EndFigure(true); return path;
        }
    }
}

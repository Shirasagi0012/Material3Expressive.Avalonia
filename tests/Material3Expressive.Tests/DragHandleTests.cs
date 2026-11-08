using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class DragHandleTests
{
    [AvaloniaFact]
    public void ChangingClipShapePreservesTheExistingRippleAndHover()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var handle = new VerticalDragHandle { InteractionSource = source, Shape = new CutCornerShape(1) };
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            var controller = MaterialInteraction.GetOrCreate(Part(handle));
            source.Emit(new MaterialInteractionEvent.HoverEnter());
            var press = new MaterialInteractionEvent.Press(new Point(2, 24)); source.Emit(press);
            for (var i = 0; i < 10; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(1, controller.RippleCount); Assert.True(controller.StateLayerOpacity > 0);
            handle.Shape = new CutCornerShape(2);
            for (var i = 0; i < 10; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(1, controller.RippleCount); Assert.True(controller.StateLayerOpacity > 0);
            source.Emit(new MaterialInteractionEvent.Release(press));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ExplicitPathsFollowDragPressPriorityAndRestoreConfiguredCorners()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var normal = new CutCornerShape(1);
        var pressed = new CutCornerShape(3);
        var dragged = new CutCornerShape(5);
        var handle = new VerticalDragHandle { InteractionSource = source, Shape = normal,
            PressedShape = pressed, DraggedShape = dragged };
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            var part = Part(handle);
            var surface = part.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Same(normal, surface.Shape);
            var point = part.TranslatePoint(new Point(2, 24), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.UpdateLayout();
            Assert.Same(pressed, surface.Shape);
            var drag = new MaterialInteractionEvent.DragStart(); source.Emit(drag); window.UpdateLayout();
            Assert.Same(dragged, surface.Shape);
            var outline = surface.ContainerGeometry!.ToString();
            dragged.TopStart = new RelativeScalar(.5, RelativeUnit.Relative);
            Assert.NotEqual(outline, surface.ContainerGeometry!.ToString());
            source.Emit(new MaterialInteractionEvent.DragCancel(drag)); window.UpdateLayout();
            Assert.Same(pressed, surface.Shape);
            handle.PressedShape = null;
            Assert.Null(surface.Shape);
            Assert.Equal(handle.Shapes!.PressedShape, surface.CornerRadius);
            window.MouseUp(point, MouseButton.Left); window.UpdateLayout();
            Assert.Same(normal, surface.Shape);
            handle.Shape = null;
            Assert.Null(surface.ContainerGeometry);
            Assert.Equal(handle.Shapes.Shape, surface.CornerRadius);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void PathConfigurationSurvivesTemplateReplacementAndReattachment()
    {
        using var clock = new MotionTestClockScope();
        var shape = new CutCornerShape(1);
        var handle = new VerticalDragHandle { Shape = shape };
        var root = new Grid { Children = { handle } };
        var window = DialogTests.Show(root);
        try
        {
            var old = Part(handle).GetVisualDescendants().OfType<MaterialSurface>().Single();
            var template = handle.Template;
            handle.Template = null; window.UpdateLayout();
            handle.Template = template; window.UpdateLayout();
            var current = Part(handle).GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.NotSame(old, current); Assert.Same(shape, current.Shape);
            root.Children.Remove(handle);
            shape.TopStart = new RelativeScalar(.5, RelativeUnit.Relative);
            root.Children.Add(handle); window.UpdateLayout();
            Assert.Same(shape, Part(handle).CurrentShape);
            Assert.NotNull(current.ContainerGeometry);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void CompiledXaml_ProvidesCompleteThreeStateConfiguration()
    {
        using var clock = new MotionTestClockScope();
        var handle = (VerticalDragHandle)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/DragHandleConfiguration.axaml"));
        var source = new MaterialInteractionSource(); handle.InteractionSource = source;
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            Assert.Equal(new Size(6, 40), Part(handle).Bounds.Size);
            Assert.Equal(Colors.Red, ((ISolidColorBrush)Part(handle).Background!).Color);
            source.Emit(new MaterialInteractionEvent.DragStart()); window.UpdateLayout();
            Assert.Equal(new Size(14, 56), Part(handle).Bounds.Size);
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)Part(handle).Background!).Color);
            Assert.Equal(new CornerRadius(6), Part(handle).CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DefaultLayout_SeparatesHandleAndMinimumInteractiveSpace()
    {
        using var clock = new MotionTestClockScope();
        var handle = new VerticalDragHandle();
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            var part = Part(handle);
            Assert.Equal(new Size(48, 48), handle.Bounds.Size);
            Assert.Equal(new Size(4, 48), part.Bounds.Size);
            Assert.Equal(new Point(22, 0), part.TranslatePoint(default, handle));
            MaterialInteractiveSize.SetMinimum(handle, 80); window.UpdateLayout();
            Assert.Equal(new Size(80, 80), handle.Bounds.Size);
            Assert.Equal(new Point(38, 16), part.TranslatePoint(default, handle));
            handle.Width = 20; handle.Height = 20; window.UpdateLayout();
            Assert.Equal(new Size(20, 20), handle.Bounds.Size);
            Assert.Equal(new Size(4, 48), part.Bounds.Size);
            Assert.Equal(new Point(8, -14), part.TranslatePoint(default, handle));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Press_ChangesAppearanceWithoutConsumptionCaptureOrPressInteraction()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var handle = new VerticalDragHandle { InteractionSource = source };
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            var part = Part(handle);
            var point = part.TranslatePoint(new Point(2, 24), window)!.Value;
            var reachedHost = false;
            window.AddHandler(InputElement.PointerPressedEvent, (_, e) => reachedHost = !e.Handled, RoutingStrategies.Bubble);
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.UpdateLayout();
            Assert.True(reachedHost); Assert.True(part.IsPressed); Assert.False(part.IsFocused);
            Assert.Equal(new Size(12, 52), part.Bounds.Size);
            Assert.DoesNotContain(events, e => e is MaterialInteractionEvent.Press);
            Assert.Equal(0, MaterialInteraction.GetOrCreate(part).RippleCount);
            window.MouseUp(point, MouseButton.Left); window.UpdateLayout();
            Assert.False(part.IsPressed); Assert.Equal(new Size(4, 48), part.Bounds.Size);
            window.MouseDown(point, MouseButton.Left);
            window.MouseMove(new Point(1, 1)); window.UpdateLayout();
            Assert.False(part.IsPressed);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DragSource_PreservesOverlappingIdentitiesAndDraggedPriority()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var handle = new VerticalDragHandle { InteractionSource = source,
            Sizes = new(new(4, 48), new(8, 50), new(16, 60)),
            Colors = new(Colors.Red, Colors.Green, Colors.Blue) };
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            var part = Part(handle); var point = part.TranslatePoint(new Point(2, 24), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            var first = new MaterialInteractionEvent.DragStart(); var second = new MaterialInteractionEvent.DragStart();
            source.Emit(first); source.Emit(second); window.UpdateLayout();
            Assert.True(part.IsDragged); Assert.Equal(new Size(16, 60), part.Bounds.Size);
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)part.Background!).Color);
            source.Emit(new MaterialInteractionEvent.DragStop(first)); Assert.True(part.IsDragged);
            source.Emit(new MaterialInteractionEvent.DragCancel(second)); window.UpdateLayout();
            Assert.False(part.IsDragged); Assert.Equal(new Size(8, 50), part.Bounds.Size);
            window.MouseUp(point, MouseButton.Left); window.UpdateLayout();
            Assert.Equal(new Size(4, 48), part.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SourceReplacementAndDetach_ReleaseCollectorsAndFrameWaiters()
    {
        using var clock = new MotionTestClockScope();
        var first = new MaterialInteractionSource(); var second = new MaterialInteractionSource();
        var handle = new VerticalDragHandle { InteractionSource = first };
        var root = new Grid { Children = { handle } };
        var window = DialogTests.Show(root);
        try
        {
            var part = Part(handle); var drag = new MaterialInteractionEvent.DragStart(); first.Emit(drag);
            handle.InteractionSource = second;
            Assert.True(part.IsDragged);
            first.Emit(new MaterialInteractionEvent.DragStop(drag)); Assert.True(part.IsDragged);
            second.Emit(new MaterialInteractionEvent.HoverEnter()); Assert.False(part.IsDragged);
            second.Emit(new MaterialInteractionEvent.DragStart()); Assert.True(part.IsDragged);
            root.Children.Remove(handle);
            Assert.False(part.IsDragged); Assert.Equal(0, clock.PendingFrameCount);
            root.Children.Add(handle); window.UpdateLayout();
            Assert.False(Part(handle).IsDragged);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TokensRemainLive_AndExplicitConfigurationWins()
    {
        using var clock = new MotionTestClockScope();
        var handle = new VerticalDragHandle();
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            var part = Part(handle);
            window.Resources[Sys.Color.Outline] = Colors.Red;
            Assert.Equal(Colors.Red, ((ISolidColorBrush)part.Background!).Color);
            window.Resources[Comp.DragHandle.Size] = new Size(6, 40); window.UpdateLayout();
            Assert.Equal(new Size(6, 40), part.Bounds.Size);
            handle.Colors = new(Colors.Blue, Colors.Green, Colors.Yellow);
            window.Resources[Sys.Color.Outline] = Colors.Black;
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)part.Background!).Color);
            handle.ClearValue(VerticalDragHandle.ColorsProperty);
            Assert.Equal(Colors.Black, ((ISolidColorBrush)part.Background!).Color);
        }
        finally { window.Close(); }
    }

    private static DragHandlePresenter Part(VerticalDragHandle handle) => handle.GetVisualDescendants().OfType<DragHandlePresenter>().Single();

    [AvaloniaFact]
    public void TemplateReplacement_RetainsOwnerDragCollector_AndReleasesOldIndication()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var handle = new VerticalDragHandle { InteractionSource = source };
        var window = DialogTests.Show(new Grid { Children = { handle } });
        try
        {
            var drag = new MaterialInteractionEvent.DragStart(); source.Emit(drag);
            var old = Part(handle); var template = handle.Template;
            handle.Template = null; window.UpdateLayout(); handle.Template = template; window.UpdateLayout();
            Assert.NotSame(old, Part(handle)); Assert.True(Part(handle).IsDragged);
            Assert.Equal(new Size(12, 52), Part(handle).Bounds.Size);
            source.Emit(new MaterialInteractionEvent.DragStop(drag)); window.UpdateLayout();
            Assert.False(Part(handle).IsDragged); Assert.Equal(new Size(4, 48), Part(handle).Bounds.Size);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}

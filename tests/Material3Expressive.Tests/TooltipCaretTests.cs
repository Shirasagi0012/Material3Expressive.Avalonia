using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class TooltipCaretTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomCaret_ReceivesSurfaceSizeAndDirection_OverridesDefault_AndSnapshotsCallerPath(bool rich)
    {
        using var clock = new MotionTestClockScope();
        ContentControl tooltip=rich?new RichTooltip():new PlainTooltip();
        tooltip.Content=new Border {Width=64,Height=8};
        tooltip.SetValue(PlainTooltip.CaretSizeProperty,new Size(16,8));
        var path=PathGeometry.Parse("M -10,0 C -10,6 -4,14 0,14 C 4,14 10,6 10,0 Z");
        var transform=new TranslateTransform(2,0);path.Transform=transform;
        Size suppliedSize=default;FlowDirection suppliedDirection=default;
        tooltip.SetValue(PlainTooltip.CaretShapeProperty,new TooltipCaretShape((size,direction)=>{suppliedSize=size;suppliedDirection=direction;return path;}));
        var box=new TooltipBox {Tooltip=tooltip,Content=new Border {Width=24,Height=24},State=new(initialIsVisible:true,mutatorMutex:new()),EnableUserInput=false,
            HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
        var window=DialogTests.Show(box);
        try
        {
            var surface=tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(surface.Bounds.Size,suppliedSize);
            Assert.Same(transform,path.Transform);
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(surface.Bounds.Width/2+2,surface.Bounds.Height+12)));
            box.FlowDirection=FlowDirection.RightToLeft;Flush();
            Assert.Equal(FlowDirection.RightToLeft,suppliedDirection);
            tooltip.SetValue(PlainTooltip.CaretShapeProperty,null);Flush();
            Assert.False(surface.ContainerGeometry!.FillContains(new Point(surface.Bounds.Width/2,surface.Bounds.Height+12)));
            Assert.True(surface.ContainerGeometry.FillContains(new Point(surface.Bounds.Width/2,surface.Bounds.Height+4)));
            tooltip.SetValue(PlainTooltip.CaretSizeProperty,null);Flush();Assert.Null(surface.ContainerGeometry);
        }
        finally {window.Close();}
        void Flush(){Dispatcher.UIThread.RunJobs();window.UpdateLayout();Dispatcher.UIThread.RunJobs();}
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaretSharesContainerClipWithoutChangingLayout_AndCanBeRemoved(bool rich)
    {
        using var clock = new MotionTestClockScope();
        ContentControl tooltip = rich ? new RichTooltip() : new PlainTooltip();
        tooltip.Content = new Border { Width = 64, Height = 8 };
        var box = new TooltipBox
        {
            Content = new Border { Width = 24, Height = 24 }, Tooltip = tooltip,
            State = new(initialIsVisible: true, mutatorMutex: new()), EnableUserInput = false,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        };
        var window = DialogTests.Show(box);
        try
        {
            var originalSize = tooltip.Bounds.Size;
            var surface = tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Null(surface.ContainerGeometry);
            SetCaret(new(16, 8)); Flush();
            Assert.Equal(originalSize, tooltip.Bounds.Size);
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(surface.Bounds.Width / 2, surface.Bounds.Height + 4)));
            var chrome = surface.GetVisualChildren().OfType<Border>().First();
            Assert.Same(surface.ContainerGeometry, chrome.Clip);
            Assert.False(chrome.ClipToBounds);

            box.AnchorPosition = TooltipAnchorPosition.Below; Flush();
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(surface.Bounds.Width / 2, -4)));
            Assert.False(surface.ContainerGeometry.FillContains(new Point(surface.Bounds.Width / 2, surface.Bounds.Height + 4)));
            SetCaret(new(24, 12)); Flush();
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(surface.Bounds.Width / 2, -10)));
            SetCaret(null); Flush();
            Assert.Null(surface.ContainerGeometry); Assert.Null(chrome.Clip); Assert.True(chrome.ClipToBounds);
            Assert.Equal(originalSize, tooltip.Bounds.Size);
        }
        finally { window.Close(); }
        void Flush() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        void SetCaret(Size? size)
        {
            if (tooltip is PlainTooltip plain) plain.CaretSize = size;
            else ((RichTooltip)tooltip).CaretSize = size;
        }
    }

    [AvaloniaFact]
    public void PersistentLabel_ProvidesCaretPlacement_AndReattachmentRefreshesIt()
    {
        using var clock = new MotionTestClockScope();
        var tooltip = new PlainTooltip { CaretSize = TooltipDefaults.CaretSize, Content = new Border { Width = 64, Height = 8 } };
        var label = new TooltipLabel { Label = tooltip, IsPersistent = true, Content = new Border { Width = 24, Height = 24 } };
        var canvas = new Canvas(); canvas.Children.Add(label); Canvas.SetLeft(label, 100); Canvas.SetTop(label, 100);
        var window = DialogTests.Show(canvas);
        try
        {
            var surface = tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(40, 28)));
            Canvas.SetTop(label, 0); Flush();
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(40, -4)));
            canvas.Children.Remove(label); Flush();
            Canvas.SetTop(label, 100); canvas.Children.Add(label); Flush();
            surface = tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(40, 28)));
        }
        finally { window.Close(); }
        void Flush() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaTheory]
    [InlineData(TooltipAnchorPosition.Left, false, 80, 12, 88, 12)]
    [InlineData(TooltipAnchorPosition.Right, true, 0, 12, -8, 12)]
    [InlineData(TooltipAnchorPosition.Start, true, 0, 12, -8, 12)]
    [InlineData(TooltipAnchorPosition.End, false, 80, 12, 88, 12)]
    public void SideCaretPointsBackTowardAnchorAfterFallback(TooltipAnchorPosition position, bool right,
        double baseX, double baseY, double tipX, double tipY)
    {
        var transform = TooltipCaret.CalculateTransform(new(80, 24), new(new(100, 100, 24, 24),
            new(320, 240), new(right ? 128 : 16, 100), position, false, 1));
        Assert.Equal(new Point(baseX, baseY), new Point(0, 0).Transform(transform));
        Assert.Equal(new Point(tipX, tipY), new Point(0, 8).Transform(transform));
    }
}

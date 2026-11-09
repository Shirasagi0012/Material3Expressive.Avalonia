using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class TooltipContainerShapeTests
{
    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MutableShape_ControlsContentClip_AndClearRestoresThemedCorners(bool rich, bool caret)
    {
        using var clock = new MotionTestClockScope();
        var shape = new CutCornerShape { TopStart = new(12, RelativeUnit.Absolute) };
        var tooltip = Tooltip(rich, caret);
        tooltip.SetValue(PlainTooltip.ShapeProperty, shape);
        var box = Box(tooltip);
        var window = DialogTests.Show(box);
        try
        {
            var originalSize = tooltip.Bounds.Size;
            var surface = tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var chrome = surface.GetVisualChildren().OfType<Border>().First();
            Assert.False(surface.ContainerGeometry!.FillContains(new Point(1, 1)));
            Assert.True(surface.ContainerGeometry.FillContains(new Point(1, 13)));
            Assert.Same(surface.ContainerGeometry, chrome.Clip);
            shape.TopStart = new(24, RelativeUnit.Absolute); Flush();
            Assert.False(surface.ContainerGeometry.FillContains(new Point(1, 13)));
            Assert.Equal(originalSize, tooltip.Bounds.Size);
            tooltip.SetValue(PlainTooltip.ShapeProperty, new CutCornerShape(0)); Flush();
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(1, 1)));
            var replacement = surface.ContainerGeometry;
            shape.TopStart = new(40, RelativeUnit.Absolute); Flush();
            Assert.Same(replacement, surface.ContainerGeometry);
            tooltip.ClearValue(PlainTooltip.ShapeProperty); Flush();
            Assert.Null(surface.Shape);
            if (caret)
            {
                Assert.NotNull(surface.ContainerGeometry);
                Assert.True(surface.ContainerGeometry.FillContains(new Point(surface.Bounds.Width / 2, surface.Bounds.Height + 4)));
            }
            else Assert.Null(surface.ContainerGeometry);
        }
        finally { window.Close(); }
        void Flush() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectionResizeAndReattachment_RecreateTheOutline_WithoutObservingDetachedShapes(bool caret)
    {
        using var clock = new MotionTestClockScope();
        var shape = new ObservedShape();
        var tooltip = Tooltip(false, caret);
        tooltip.SetValue(PlainTooltip.ShapeProperty, shape);
        var box = Box(tooltip);
        var window = DialogTests.Show(box);
        try
        {
            Assert.Equal(new Size(96, 64), shape.Size);
            box.FlowDirection = FlowDirection.RightToLeft; Flush();
            Assert.Equal(FlowDirection.RightToLeft, shape.Direction);
            tooltip.Width = 120; Flush();
            Assert.Equal(new Size(120, 64), shape.Size);
            window.Content = null; Flush();
            var calls = shape.Calls;
            shape.Inset = 20; Flush();
            Assert.Equal(calls, shape.Calls);
            window.Content = box; Flush();
            Assert.True(shape.Calls > calls);
            var surface = tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.False(surface.ContainerGeometry!.FillContains(new Point(1, 1)));
        }
        finally { window.Close(); }
        void Flush() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomTransformedPath_IsSnapshotted_AndCaretUsesTheUpstreamNonZeroCopy(bool caret)
    {
        using var clock = new MotionTestClockScope();
        var path = PathGeometry.Parse("M 0,0 L 96,0 L 96,64 L 0,64 Z M 24,16 L 48,16 L 48,32 L 24,32 Z");
        path.FillRule = FillRule.EvenOdd;
        var transform = new TranslateTransform(2, 0); path.Transform = transform;
        var shape = new SnapshotShape(path);
        var tooltip = Tooltip(false, caret);
        tooltip.SetValue(PlainTooltip.ShapeProperty, shape);
        var window = DialogTests.Show(Box(tooltip));
        try
        {
            var surface = tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Same(transform, path.Transform);
            // Fixed TooltipCaretShape copies contours into Path() via addPath. Equal
            // winding fills this hole with a caret, while direct Surface retains EvenOdd.
            Assert.Equal(caret, surface.ContainerGeometry!.FillContains(new Point(32, 24)));
            Assert.True(surface.ContainerGeometry.FillContains(new Point(16, 24)));
            Assert.Equal(caret, surface.ContainerGeometry.FillContains(new Point(48, 68)));
        }
        finally { window.Close(); }
    }

    private static ContentControl Tooltip(bool rich, bool caret)
    {
        ContentControl tooltip = rich ? new RichTooltip() : new PlainTooltip();
        tooltip.Width = 96; tooltip.Height = 64; tooltip.Padding = default;
        tooltip.Content = new Border { Background = Brushes.Red };
        if (caret) tooltip.SetValue(PlainTooltip.CaretSizeProperty, new Size(16, 8));
        return tooltip;
    }
    private static TooltipBox Box(ContentControl tooltip) => new()
    {
        Tooltip = tooltip, Content = new Border { Width = 24, Height = 24 },
        State = new(initialIsVisible: true, mutatorMutex: new()), EnableUserInput = false,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
    };
    private sealed class SnapshotShape(PathGeometry path) : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density) => path;
    }
    private sealed class ObservedShape : MaterialShape
    {
        public static readonly StyledProperty<double> InsetProperty = AvaloniaProperty.Register<ObservedShape, double>(nameof(Inset), 8);
        public double Inset { get => GetValue(InsetProperty); set => SetValue(InsetProperty, value); }
        public int Calls { get; private set; }
        public Size Size { get; private set; }
        public FlowDirection Direction { get; private set; }
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        {
            Calls++; Size = size; Direction = layoutDirection;
            return new CutCornerShape(Inset).CreateOutline(size, layoutDirection, density);
        }
    }
}

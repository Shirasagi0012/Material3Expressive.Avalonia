using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class CarouselShapeTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiAspect_LogicalCornersFollowDirectionAndMaskedSize(bool rtl)
    {
        var shape = new CutCornerShape { TopStart = new(.5, RelativeUnit.Relative) };
        var item = new MultiAspectCarouselItem
        {
            Width = 100, Height = 80, Shape = shape,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            DrawInfo = new(30, 100, 60, 100, 10, Avalonia.Layout.Orientation.Horizontal),
        };
        var window = DialogTests.Show(item);
        try
        {
            var path = Mask(item);
            Assert.Equal(new Rect(30, 0, 70, 80), path.Bounds);
            Assert.False(path.FillContains(new Point(rtl ? 99 : 31, 1)));
            Assert.True(path.FillContains(new Point(rtl ? 31 : 99, 1)));
            Assert.True(path.FillContains(new Point(65, 40)));
            Assert.Equal(new Size(100, 80), item.Bounds.Size);
            shape.TopStart = new(0, RelativeUnit.Absolute);
            Flush(window);
            Assert.True(Mask(item).FillContains(new Point(rtl ? 99 : 31, 1)));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MultiAspect_ReplacesClipButRemembersBorderUntilRemoved()
    {
        var original = new CutCornerShape(20);
        var item = new MultiAspectCarouselItem { Width = 100, Height = 80, Shape = original,
            BorderBrush = Brushes.Black, BorderThickness = new(2) };
        var window = DialogTests.Show(item);
        try
        {
            var border = Outline(item);
            var first = border.ShapePath;
            item.Shape = new CutCornerShape(0); Flush(window);
            Assert.True(Mask(item).FillContains(new Point(1, 1)));
            Assert.Same(first, border.ShapePath);
            Assert.False(border.ShapePath!.FillContains(new Point(1, 1)));
            // The remembered instance remains observable, even after the public property changes.
            original.TopStart = new(0, RelativeUnit.Absolute); Flush(window);
            Assert.True(border.ShapePath!.FillContains(new Point(1, 1)));
            item.BorderThickness = default; Flush(window);
            item.BorderThickness = new(2); Flush(window);
            Assert.True(border.ShapePath!.FillContains(new Point(99, 1)));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Classic_RemembersInitialShapeButRecreatesOnRemount()
    {
        var original = new CutCornerShape(20);
        var item = new CarouselItem { Shape = original, BorderBrush = Brushes.Black, BorderThickness = new(2) };
        var carousel = new HorizontalCarousel { Width = 300, Height = 80, ItemWidth = 180, Items = { item } };
        var window = DialogTests.Show(carousel);
        try
        {
            var first = Mask(item);
            Assert.False(item.ClipToBounds);
            Assert.All(item.GetVisualDescendants(), child => Assert.False(child.ClipToBounds, child.GetType().Name));
            item.Shape = new CutCornerShape(0); Flush(window);
            Assert.Same(first, Mask(item));
            original.TopStart = new(0, RelativeUnit.Absolute); Flush(window);
            Assert.True(Mask(item).FillContains(new Point(1, 1)));
            Assert.False(Mask(item).FillContains(new Point(item.Bounds.Width - 1, 1)));
            window.Content = null; Flush(window);
            window.Content = carousel; Flush(window);
            Assert.True(Mask(item).FillContains(new Point(item.Bounds.Width - 1, 1)));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void GenericBorder_KeepsFullLayoutAndTranslatedPathCoordinates()
    {
        var item = new MultiAspectCarouselItem { Width = 100, Height = 80, Shape = new CutCornerShape(12),
            BorderBrush = Brushes.Black, BorderThickness = new(30),
            DrawInfo = new(70, 100, 60, 100, 10, Avalonia.Layout.Orientation.Horizontal) };
        var window = DialogTests.Show(item);
        try
        {
            var outline = Outline(item);
            Assert.Equal(item.Bounds.Size, outline.Bounds.Size);
            Assert.Equal(new Rect(70, 0, 30, 80), outline.ShapePath!.Bounds);
            Assert.Null(outline.RenderTransform);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShapeFactoryResult_IsSnapshottedAndNotMutatedByMaskTranslation()
    {
        var shape = new ReusedShape();
        var item = new MultiAspectCarouselItem { Width = 100, Height = 80, Shape = shape,
            DrawInfo = new(30, 100, 60, 100, 10, Avalonia.Layout.Orientation.Horizontal) };
        var window = DialogTests.Show(item);
        try
        {
            Assert.Null(shape.Path.Transform);
            Assert.Equal(new Rect(30, 0, 50, 50), Mask(item).Bounds);
            Assert.NotSame(shape.Path, Mask(item));
            window.Content = null; Flush(window);
            var calls = shape.Calls;
            item.Shape = new CutCornerShape(4); Flush(window);
            Assert.Equal(calls, shape.Calls);
        }
        finally { window.Close(); }
    }

    private sealed class ReusedShape : MaterialShape
    {
        internal PathGeometry Path { get; } = PathGeometry.Parse("M0,0 L50,0 L50,50 L0,50 Z");
        internal int Calls { get; private set; }
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        { Calls++; return Path; }
    }

    [AvaloniaFact]
    public void MaskShape_AddsContoursToNonZeroPath_AndMultiAspectFallsBackForEmptyPath()
    {
        var item = new MultiAspectCarouselItem { Width = 100, Height = 80, Shape = new CompoundShape() };
        var window = DialogTests.Show(item);
        try
        {
            Assert.True(Mask(item).FillContains(new Point(40, 40)));
            item.Shape = new EmptyShape(); Flush(window);
            Assert.Equal(new Rect(0, 0, 100, 80), Mask(item).Bounds);
            Assert.True(Mask(item).FillContains(new Point(40, 40)));
        }
        finally { window.Close(); }
    }

    private sealed class CompoundShape : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density) =>
            PathGeometry.Parse("F0 M0,0 L80,0 L80,80 L0,80 Z M20,20 L60,20 L60,60 L20,60 Z");
    }
    private sealed class EmptyShape : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density) => new();
    }

    private static Geometry Mask(Control owner) => owner.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "PART_Mask").Clip!;
    private static SurfaceOutline Outline(Control owner) => owner.GetVisualDescendants().OfType<SurfaceOutline>().Single();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

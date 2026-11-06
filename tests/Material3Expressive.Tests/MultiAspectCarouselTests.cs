using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class MultiAspectCarouselTests
{
    [Theory]
    [InlineData(100, 100, -40, 30, 100, 10, 75)]
    [InlineData(100, 100, 240, 0, 70, -10, 75)]
    [InlineData(100, 100, 100, 0, 100, 0, 75)]
    [InlineData(200, 100, -100, 50, 200, 50, 100)]
    [InlineData(50, 100, -20, 15, 50, 5, 37.5)]
    public void MaskAndParallax_PreserveAspectBranches(float main, float cross, float offset,
        float start, float end, float parallax, float minimum)
    {
        var info = MultiAspectCarouselItemDrawInfo.Calculate(0, 300, offset, main, cross);
        Assert.Equal(start, info.MaskStart); Assert.Equal(end, info.MaskEnd);
        Assert.Equal(parallax, info.Parallax); Assert.Equal(minimum, info.MinSize);
        Assert.Equal(end - start, info.Size);
    }

    [Fact]
    public void ExactRatioBoundaries_KeepFixedUpstreamDiscontinuities()
    {
        Assert.Equal(120, MultiAspectCarouselItemDrawInfo.Calculate(0, 300, 0, 160, 90).MinSize);
        Assert.Equal(75, MultiAspectCarouselItemDrawInfo.Calculate(0, 300, 0, 100, 100).MinSize);
        Assert.True(MultiAspectCarouselItemDrawInfo.Calculate(0, 300, 0, 160.01f, 90).MinSize < 81);
        Assert.True(MultiAspectCarouselItemDrawInfo.Calculate(0, 300, 0, 99.99f, 100).MinSize < 67);
    }

    [Fact]
    public void OffscreenAndOversizedItems_KeepStartBranchPrecedenceAndBoundedInterpolation()
    {
        var hidden = MultiAspectCarouselItemDrawInfo.Calculate(0, 300, 400, 0, 100, false);
        Assert.Equal(0, hidden.Size); Assert.Equal(0, hidden.Parallax);
        var oversized = MultiAspectCarouselItemDrawInfo.Calculate(0, 100, -50, 200, 100);
        Assert.Equal(25, oversized.MaskStart); Assert.Equal(200, oversized.MaskEnd);
        var beyond = MultiAspectCarouselItemDrawInfo.Calculate(0, 100, -400, 200, 100);
        Assert.Equal(100, beyond.MaskStart); Assert.Equal(100, beyond.Parallax);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeScrolling_UpdatesMaskWithoutChangingItemLayout(bool vertical)
    {
        var item = new MultiAspectCarouselItem { Width = 100, Height = 100, Content = new Border { Background = Brushes.Blue },
            Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal, CornerRadius = new(12) };
        var stack = new StackPanel { Orientation = item.Orientation, Children = { item, new Border { Width = 400, Height = 400 } } };
        var viewer = Viewer(stack, vertical); var window = Show(viewer);
        try
        {
            Assert.False(item.ClipToBounds); // Only the content mask clips; cached border sampling may extend past the item.
            Assert.Equal(100, item.ActualDrawInfo.Size);
            viewer.Offset = vertical ? new Vector(0, 40) : new Vector(40, 0); Flush(window);
            Assert.Equal(new Size(100, 100), item.Bounds.Size);
            Assert.Equal(30, item.ActualDrawInfo.MaskStart);
            Assert.Equal(10, item.ActualDrawInfo.Parallax);
            Assert.Equal(70, item.ActualDrawInfo.Size);
            Assert.NotNull(item.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "PART_Mask").Clip);
            viewer.Offset = default; Flush(window); Assert.Equal(100, item.ActualDrawInfo.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitDrawInfo_OverridesViewportAndPreservesContentTransform()
    {
        var transform = new RotateTransform(15);
        var child = new Border { Width = 100, Height = 80, RenderTransform = transform };
        var item = new MultiAspectCarouselItem { Content = child,
            DrawInfo = MultiAspectCarouselItemDrawInfo.Calculate(0, 200, -40, 100, 100) };
        var window = Show(item);
        try
        {
            Assert.Same(transform, child.RenderTransform);
            Assert.Equal(30, item.ActualDrawInfo.MaskStart);
            Assert.Equal(new Size(100, 80), item.DesiredSize);
            item.Content = new Border { Width = 40, Height = 20 }; item.DrawInfo = null; Flush(window);
            Assert.Equal(new Size(40, 20), item.DesiredSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ViewportChangesAndReparenting_ReconnectWithoutStaleScrollSubscription()
    {
        var item = new MultiAspectCarouselItem { Width = 100, Height = 100 };
        var firstStack = new StackPanel { Orientation = Orientation.Horizontal, Children = { item, new Border { Width = 400, Height = 100 } } };
        var first = Viewer(firstStack, false); var window = Show(first);
        try
        {
            first.Offset = new(40, 0); Flush(window); Assert.Equal(30, item.ActualDrawInfo.MaskStart);
            firstStack.Children.Remove(item);
            var secondStack = new StackPanel { Orientation = Orientation.Horizontal, Children = { item, new Border { Width = 400, Height = 100 } } };
            window.Content = Viewer(secondStack, false); Flush(window);
            Assert.Equal(0, item.ActualDrawInfo.MaskStart);
            first.Offset = new(80, 0); Flush(window); Assert.Equal(0, item.ActualDrawInfo.MaskStart);
        }
        finally { window.Close(); }
    }

    internal static ScrollViewer Viewer(Control content, bool vertical) => new()
    {
        Content = content, Width = vertical ? 100 : 300, Height = vertical ? 300 : 100,
        HorizontalScrollBarVisibility = vertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden,
        VerticalScrollBarVisibility = vertical ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled,
        Template = new FuncControlTemplate<ScrollViewer>((owner, scope) => new ScrollContentPresenter
        {
            Name = "PART_ContentPresenter", Content = owner.Content,
            CanHorizontallyScroll = !vertical, CanVerticallyScroll = vertical,
            [!ScrollContentPresenter.OffsetProperty] = owner[!ScrollViewer.OffsetProperty],
        }.RegisterInNameScope(scope)),
    };

    [AvaloniaFact]
    public void GridItems_UseTheirOwnCrossAxisAndPublishChangingDrawInfo()
    {
        var viewport = new Canvas { Width = 300, Height = 240, ClipToBounds = true };
        var item = new MultiAspectCarouselItem { Width = 120, Height = 90, Viewport = viewport };
        Canvas.SetLeft(item, -40); viewport.Children.Add(item);
        var notifications = 0;
        item.PropertyChanged += (_, e) => { if (e.Property == MultiAspectCarouselItem.ActualDrawInfoProperty) notifications++; };
        var window = Show(viewport);
        try
        {
            Assert.Equal(30, item.ActualDrawInfo.MaskStart);
            item.UseItemCrossAxisSize = true; Flush(window);
            var grid = MultiAspectCarouselItemDrawInfo.Calculate(0, 300, -40, 120, 90);
            Assert.Equal(grid, item.ActualDrawInfo);
            Assert.NotEqual(30, item.ActualDrawInfo.MaskStart);
            Assert.True(notifications >= 2);
            Canvas.SetLeft(item, 40); Flush(window);
            Assert.Equal(0, item.ActualDrawInfo.Parallax);
            Assert.Equal(120, item.ActualDrawInfo.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitViewport_UpdatesAfterAncestorMovesAndCanBeReplaced()
    {
        var root = new Canvas { Width = 320, Height = 100, ClipToBounds = true };
        var inner = new Canvas { Width = 200, Height = 100 };
        Canvas.SetLeft(inner, 80); root.Children.Add(inner);
        var item = new MultiAspectCarouselItem { Width = 100, Height = 100, Viewport = inner };
        Canvas.SetLeft(item, -40); inner.Children.Add(item);
        var window = Show(root);
        try
        {
            Assert.Equal(30, item.ActualDrawInfo.MaskStart);
            item.Viewport = root; Flush(window); Assert.Equal(0, item.ActualDrawInfo.MaskStart);
            Canvas.SetLeft(inner, 0); Flush(window); Assert.Equal(30, item.ActualDrawInfo.MaskStart);
            item.DrawInfo = new(0, 50, 25, 100, -20, Orientation.Vertical); Flush(window);
            var presenter = item.GetVisualDescendants().OfType<ContentPresenter>().Single(x => x.Name == "PART_ContentPresenter");
            var translation = Assert.IsType<TranslateTransform>(presenter.RenderTransform);
            Assert.Equal(0, translation.X); Assert.Equal(-20, translation.Y);
        }
        finally { window.Close(); }
    }

    private static Window Show(Control content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Left; content.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 500, Height = 500 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = content; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

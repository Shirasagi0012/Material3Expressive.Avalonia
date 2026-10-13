using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedLayoutBoundsTest
{
    private static TweenSpec<T> Tween<T>(int ms = 200) => new(TimeSpan.FromMilliseconds(ms));

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; }
        public Window Window { get; }
        public Control Stage { get; }
        public Fixture(Control stage, ManualFrameSource? clock = null, double width = 800, double height = 600)
        {
            Frames = clock ?? new ManualFrameSource();
            StampClock(stage);
            Stage = stage;
            Window = new AnimationTestWindow(Frames) { Width = width, Height = height, Content = stage };
            Window.Show();
            Layout();
        }
        private void StampClock(Control root)
        {

            switch (root)
            {
                case Panel panel:
                    foreach (var child in panel.Children) if (child is Control control) StampClock(control);
                    break;
                case Decorator decorator when decorator.Child is Control child:
                    StampClock(child);
                    break;
                case ContentControl contentControl when contentControl.Content is Control content:
                    StampClock(content);
                    break;
            }
        }
        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }
        public void Step(AnimatedVisibility host, int milliseconds)
        {
            Layout();
            Assert.True(SpinWait.SpinUntil(() =>
            {
                Layout();
                return host.IsIdle || Frames.PendingFrameCount > 0;
            }, TimeSpan.FromSeconds(5)), "The visibility loop did not request a frame.");
            Frames.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds));
            Layout();
        }
        public void Dispose() => Window.Close();
    }

    private static AnimatedVisibility Visibility(Control content, bool visible = true) => new()
    {
        Content = content,
        Visible = visible,
        Enter = EnterTransition.None,
        Exit = ExitTransition.None,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top
    };

    [Fact]
    public Task Canvas_ReadsHostSpaceLayout() => OnUi(() =>
    {
        var content = new Border { Width = 100, Height = 80, Background = Brushes.Blue };
        var host = Visibility(content);
        Canvas.SetLeft(host, 10); Canvas.SetTop(host, 20);
        using var fixture = new Fixture(new Canvas { Children = { host } });
        fixture.Layout();
        var bounds = SharedLayoutBounds.ReadLayout(content, fixture.Stage);
        Assert.Equal(new Rect(10, 20, 100, 80), bounds);
    });

    [Fact]
    public Task UserRenderTransform_IsKept_LibraryScale_IsStrippedFromLayout() => OnUi(() =>
    {
        var content = new Border
        {
            Width = 100, Height = 80, Background = Brushes.Blue,
            RenderTransform = new TranslateTransform(15, 0),
            RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute)
        };
        var host = Visibility(content);
        host.Visible = false;
        host.Enter = new ScaleIn { InitialScale = 0.5, Spec = Tween<double>(), TransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute) };
        Canvas.SetLeft(host, 40); Canvas.SetTop(host, 30);
        using var fixture = new Fixture(new Canvas { Children = { host } });
        host.Visible = true;
        fixture.Step(host, 100);
        var layer = host.GetVisualDescendants().OfType<VisibilityLayer>().Single();
        Assert.NotNull(layer.Motion);
        Assert.NotEqual(1, ((MatrixTransform)layer.Surface.RenderTransform!).Matrix.M11);
        var layout = SharedLayoutBounds.ReadLayout(content, fixture.Stage);
        var display = SharedLayoutBounds.ReadDisplay(content, fixture.Stage);
        Assert.NotNull(layout);
        Assert.NotNull(display);
        Assert.Equal(55, layout!.Value.X, 1);
        Assert.Equal(30, layout.Value.Y, 1);
        Assert.Equal(100, layout.Value.Width, 1);
        Assert.Equal(80, layout.Value.Height, 1);
        Assert.NotEqual(layout.Value.Width, display!.Value.Width);
    });

    [Theory]
    [InlineData("grid")]
    [InlineData("stack")]
    [InlineData("scroll")]
    public Task CommonPanels_ReturnStableHostBounds(string panel) => OnUi(() =>
    {
        var content = new Border { Width = 120, Height = 40, Background = Brushes.Red };
        var host = Visibility(content);
        Control stage = panel switch
        {
            "grid" => new Grid { Children = { host } },
            "stack" => new StackPanel { Children = { host } },
            _ => new ScrollViewer { Width = 400, Height = 300, Content = host }
        };
        using var fixture = new Fixture(stage);
        fixture.Layout();
        var first = SharedLayoutBounds.ReadLayout(content, fixture.Stage);
        fixture.Layout();
        var second = SharedLayoutBounds.ReadLayout(content, fixture.Stage);
        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.True(first!.Value.Width >= 120);
        Assert.True(first.Value.Height >= 40);
    });

    [Fact]
    public Task Expand_DoesNotTreatAnimatedSlotAsFinalLayout() => OnUi(() =>
    {
        var content = new Border { Width = 200, Height = 160, Background = Brushes.Green };
        var host = Visibility(content);
        host.Visible = false;
        host.Enter = new ExpandVertically { InitialHeightRatio = 0, ExpandFrom = VerticalAlignment.Center, Spec = Tween<Size>(400) };
        Canvas.SetLeft(host, 50); Canvas.SetTop(host, 60);
        using var fixture = new Fixture(new Canvas { Children = { host } });
        host.Visible = true;
        fixture.Step(host, 80);
        var layout = SharedLayoutBounds.ReadLayout(content, fixture.Stage);
        var display = SharedLayoutBounds.ReadDisplay(content, fixture.Stage);
        Assert.NotNull(layout);
        Assert.Equal(200, layout!.Value.Width, 1);
        Assert.Equal(160, layout.Value.Height, 1);
        Assert.Equal(50, layout.Value.X, 1);
        Assert.Equal(60, layout.Value.Y, 1);
        Assert.NotNull(display);
        Assert.NotEqual(layout.Value.Y, display!.Value.Y, 1);
    });

    [Fact]
    public Task RtlMirror_IsIncludedInLayout() => OnUi(() =>
    {
        var content = new Border { Width = 80, Height = 40, Background = Brushes.Black };
        var host = Visibility(content);
        var canvas = new Canvas { Width = 400, Height = 200, FlowDirection = FlowDirection.RightToLeft, Children = { host } };
        Canvas.SetLeft(host, 20); Canvas.SetTop(host, 10);
        using var fixture = new Fixture(canvas);
        fixture.Layout();
        var layout = SharedLayoutBounds.ReadLayout(content, canvas);
        var display = SharedLayoutBounds.ReadDisplay(content, canvas);
        Assert.NotNull(layout);
        Assert.Equal(display, layout);
    });

    [Fact]
    public Task MissingArrangeOrCrossRoot_ReturnsNull() => OnUi(() =>
    {
        var content = new Border { Width = 10, Height = 10 };
        Assert.Null(SharedLayoutBounds.ReadLayout(content, content));
        var host = Visibility(content);
        using var first = new Fixture(host);
        using var second = new Fixture(new Border { Width = 20, Height = 20 });

        first.Layout();
        Assert.Null(SharedLayoutBounds.ReadLayout(content, second.Stage));
    });
}

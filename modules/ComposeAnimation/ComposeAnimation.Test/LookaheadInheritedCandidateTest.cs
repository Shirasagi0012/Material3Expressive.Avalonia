using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace ComposeAnimation.Test;

public class LookaheadInheritedCandidateTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private sealed class Fixture : IDisposable
    {
        public LookaheadRoot Root { get; }
        public Window Window { get; }

        public Fixture(Control opaqueContent)
        {
            var opaque = new Border { Width = 260, Height = 180, Child = opaqueContent };
            Root = new LookaheadRoot
            {
                Width = 320,
                Height = 220,
                Child = new LookaheadBox { Children = { opaque } },
                UseLayoutRounding = false
            };
            Window = new AnimationTestWindow() { Width = 400, Height = 300, Content = Root };
            Window.Show();
            Layout();
        }

        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }

        public void Dispose() => Window.Close();
    }

    [Fact]
    public Task NativeConsumers_DoNotTreatInheritedCandidateAsMembership() => OnUi(() =>
    {
        var contentSize = new AnimatedContentSize
        {
            Content = new Border { Width = 80, Height = 40 },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        var boundary = new LookaheadBoundary
        {
            Width = 100,
            Height = 60,
            Child = new Grid { Children = { new Border { Width = 30, Height = 20 } } }
        };
        var shared = new SharedBounds
        {
            ResizeMode = SharedBoundsResizeMode.ScaleToBounds,
            Content = new Border { Width = 60, Height = 35 },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var native = new Grid { Children = { contentSize, boundary, shared } };

        using var fixture = new Fixture(native);

        Assert.Same(fixture.Root.Context, LookaheadEnvironment.GetContext(contentSize));
        Assert.Same(fixture.Root.Context, LookaheadEnvironment.GetContext(boundary));
        Assert.Same(fixture.Root.Context, LookaheadEnvironment.GetContext(shared));
        Assert.False(fixture.Root.Context.Contains(contentSize));
        Assert.False(fixture.Root.Context.Contains(boundary));
        Assert.False(fixture.Root.Context.Contains(shared));
        Assert.Null(Lookahead.GetSnapshot(contentSize));
        Assert.Null(Lookahead.GetSnapshot(boundary));
        Assert.Null(Lookahead.GetSnapshot(shared));
        Assert.Equal(new Size(80, 40), contentSize.Bounds.Size);
        Assert.Equal(new Size(100, 60), boundary.Bounds.Size);
        Assert.Equal(new Size(60, 35), shared.Bounds.Size);
    });

    [Fact]
    public Task AutomaticConsumers_CreateLocalDomainsBelowOpaqueLeaf() => OnUi(() =>
    {
        var bounds = new AnimatedBounds
        {
            TargetSize = new Size(90, 50),
            Child = new LookaheadTextBlock { Text = "local bounds" },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        var boxChild = new LookaheadTextBlock { Text = "local box", Width = 70, Height = 30 };
        var box = new LookaheadBox
        {
            Children = { boxChild },
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            UseLayoutRounding = false
        };
        var native = new Grid { Children = { bounds, box } };

        using var fixture = new Fixture(native);

        var boundsSnapshot = Lookahead.GetSnapshot(bounds);
        var boxSnapshot = Lookahead.GetSnapshot(box);
        Assert.NotNull(boundsSnapshot);
        Assert.NotNull(boxSnapshot);
        Assert.NotSame(fixture.Root.Snapshot, boundsSnapshot);
        Assert.NotSame(fixture.Root.Snapshot, boxSnapshot);
        Assert.NotSame(boundsSnapshot, boxSnapshot);
        Assert.Same(boundsSnapshot, Lookahead.GetSnapshot(bounds.Child!));
        Assert.Same(boxSnapshot, Lookahead.GetSnapshot(boxChild));
        Assert.DoesNotContain(bounds, fixture.Root.Snapshot!.Results.Keys);
        Assert.DoesNotContain(box, fixture.Root.Snapshot.Results.Keys);
    });

    [Fact]
    public Task CoordinateAndMoveQueries_RejectCandidateOnlyDescendant() => OnUi(() =>
    {
        var native = new Border { Width = 80, Height = 40 };
        using var fixture = new Fixture(native);

        Assert.False(LookaheadCoordinates.TryGetTargetBounds(native, out _));
        Assert.False(LookaheadCoordinates.TryGetActualBounds(native, out _));
        Assert.Throws<InvalidOperationException>(() => Lookahead.MoveContent(native, () => { }));
        Assert.Throws<InvalidOperationException>(() => fixture.Root.MoveContent(native, () => { }));
    });
}

using Avalonia;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Material3Expressive.Foundation.Input;
using Material3Expressive.Styles;
using SmoothScroll.Avalonia.Controls;
using SmoothScroll.Avalonia.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class MaterialScrollCoordinatorTests
{
    [AvaloniaFact]
    public void ReleaseUsesAncestorPreAndDescendantPostOrderWithCumulativeConsumption()
    {
        using var clock = new MotionTestClockScope();
        var viewer = new ScrollViewer { Content = new Border { Height = 1000 },
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        var inner = new Border { Child = viewer };
        var outer = new Border { Child = inner };
        var window = new MotionTestWindow { Width = 240, Height = 320 };
        window.Styles.Add(new MaterialTheme());
        window.Content = outer;
        window.Show();
        var registrations = new List<IDisposable>();
        try
        {
            Render();
            var presenter = Assert.IsType<ScrollViewerPresenter>(viewer.Presenter);
            viewer.Offset = new Vector(0, 1000);
            Render();
            InteractionTracker? tracker = null;
            var calls = new List<(string Name, double Consumed, double Available)>();
            foreach (var (owner, name, pre, postFraction) in new[]
                { (inner, "inner", 50d, .25), (outer, "outer", 100d, .5) })
                registrations.Add(MaterialScrollCoordinator.Register(presenter, owner, registration =>
                {
                    tracker = registration.Tracker;
                    registration.Configure(new Factory(new Participant(name, [], 0, 0)),
                        preFling: (available, _) =>
                        {
                            calls.Add(($"{name}.pre", 0, available));
                            return Task.FromResult(pre);
                        },
                        postFling: (consumed, available, _) =>
                        {
                            calls.Add(($"{name}.post", consumed, available));
                            return Task.FromResult(available * postFraction);
                        });
                    return new EmptyDisposable();
                }));
            Render();
            Assert.NotNull(tracker);
            // Exercise the real release transaction without adding a public gesture-testing API.
            typeof(InteractionTracker).GetMethod("StartInertia", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(tracker, [new Point(0, 500), false]);
            for (var turn = 0; turn < 100 && calls.Count < 4; turn++)
            {
                Thread.Sleep(1);
                Render();
            }
            Assert.Equal(["outer.pre", "inner.pre", "inner.post", "outer.post"], calls.Select(call => call.Name));
            Assert.Equal(500, calls[0].Available);
            Assert.Equal(400, calls[1].Available);
            var innerPost = calls[2];
            var outerPost = calls[3];
            Assert.Equal(350, innerPost.Consumed + innerPost.Available, 10);
            Assert.True(innerPost.Available > 0);
            Assert.Equal(innerPost.Available * .75, outerPost.Available, 10);
            Assert.Equal(innerPost.Consumed + innerPost.Available * .25, outerPost.Consumed, 10);
        }
        finally
        {
            foreach (var registration in registrations) registration.Dispose();
            Render();
            window.Close();
        }

        void Render()
        {
            for (var turn = 0; turn < 4; turn++)
            {
                Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
                using var frame = window.CaptureRenderedFrame();
            }
            Dispatcher.UIThread.RunJobs();
        }
    }

    private sealed class EmptyDisposable : IDisposable { public void Dispose() { } }

    [Fact]
    public void NestedDispatchUsesAncestorOrderAndIncludesInnerPostConsumption()
    {
        var calls = new List<string>();
        var outer = new Participant("outer", calls, 10, 3);
        var inner = new Participant("inner", calls, 5, 7);
        var group = new MaterialScrollCoordinator.Participants();
        group.Set(inner, 4, false, new Factory(inner));
        group.Set(outer, 2, true, new Factory(outer));
        var pre = new ScrollMovementContext(100, 0, 0, 0, 500, 0, ScrollMovementSource.Direct);
        Assert.Equal(15, group.PreScroll(in pre));
        var post = pre with { Delta = 25, Position = 60 };
        Assert.Equal(10, group.PostScroll(in post, 60));
        Assert.Equal(["outer.pre:100", "inner.pre:90", "inner.post:25:60", "outer.post:18:67"], calls);
        Assert.Equal(new ScrollBounds(0, 400), group.GetVerticalBounds(in pre));
        group.Remove(inner);
        Assert.True(inner.Detached);
        Assert.False(outer.Detached);
        group.Run(outer, item => Assert.Same(outer, item));
    }

    private sealed record Factory(Participant Participant) : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create() => Participant;
    }

    private sealed class Participant(string name, List<string> calls, double pre, double post) : IScrollMovementParticipant
    {
        internal bool Detached;
        public double PreScroll(in ScrollMovementContext context)
        { calls.Add($"{name}.pre:{context.Delta}"); return pre; }
        public double PostScroll(in ScrollMovementContext context, double selfConsumed)
        { calls.Add($"{name}.post:{context.Delta}:{selfConsumed}"); return post; }
        public ScrollBounds GetVerticalBounds(in ScrollMovementContext context) => new(0, 400);
        public void Detach() => Detached = true;
    }
}

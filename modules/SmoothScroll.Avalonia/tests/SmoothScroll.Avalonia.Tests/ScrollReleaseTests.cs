using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using SmoothScroll.Avalonia.Interaction;

namespace SmoothScroll.Avalonia.Tests;

public sealed class ScrollReleaseTests
{
    [AvaloniaFact]
    public void PostFlingReceivesActualBoundaryRemainderAfterMovementAndBeforeIdle()
    {
        using var host = new Host();
        host.Tracker.MaxPosition = default;
        var calls = new List<string>();
        var participant = new Participant();
        participant.Publish = () => host.Tracker.DispatchToUI(() => calls.Add("movement"));
        var gate = new TaskCompletionSource<double>();
        (double Consumed, double Available)? post = null;
        host.Tracker.ConfigureVerticalScroll(new Factory(participant), null,
            (velocity, _) =>
            {
                calls.Add("pre");
                Assert.Equal(400, velocity);
                return Task.FromResult(100d);
            },
            (consumed, available, _) =>
            {
                Dispatcher.UIThread.VerifyAccess();
                calls.Add("post");
                post = (consumed, available);
                return gate.Task;
            });
        host.Tracker.StartInertia(new Point(0, 400), false);
        host.RenderUntil(() => post is not null);
        Assert.Equal("pre", calls[0]);
        Assert.Equal("movement", calls[^2]);
        Assert.Equal("post", calls[^1]);
        Assert.Equal(0, participant.IdleCount);
        var result = participant.Results[^1];
        Assert.Equal(0, result.SelfConsumed);
        Assert.InRange(post!.Value.Available, 1, 300);
        Assert.Equal(result.Velocity * Math.Pow(.95, result.ElapsedSeconds * 60), post.Value.Available, 10);
        Assert.Equal(300, post.Value.Consumed + post.Value.Available, 10);
        gate.SetResult(post.Value.Available);
        host.Render();
        Assert.Equal(1, participant.IdleCount);
    }

    [AvaloniaFact]
    public void PostFlingReceivesLastDecayVelocityOnNaturalCompletion()
    {
        using var host = new Host();
        var participant = new Participant();
        (double Consumed, double Available)? post = null;
        host.Tracker.ConfigureVerticalScroll(new Factory(participant), null, null,
            (consumed, available, _) =>
            {
                post = (consumed, available);
                return Task.FromResult(0d);
            });
        host.Tracker.StartInertia(new Point(0, 4), false);
        host.RenderUntil(() => post is not null);
        Assert.InRange(post!.Value.Available, double.Epsilon, 4);
        Assert.Equal(4, post.Value.Consumed + post.Value.Available, 10);
        Assert.True(host.Tracker.Position.Y > 0);
        Assert.Equal(1, participant.IdleCount);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void PostOnlyConnectionHandlesZeroVelocityRelease(bool explicitHost)
    {
        using var host = new Host();
        var participant = new Participant();
        var gate = new TaskCompletionSource<double>();
        var calls = 0;
        host.Tracker.ConfigureVerticalScroll(new Factory(participant));
        host.Tracker.ConfigureVerticalScrollRelease(null, (consumed, available, _) =>
        {
            calls++;
            Assert.Equal(0, consumed);
            Assert.Equal(0, available);
            return gate.Task;
        });
        if (!explicitHost)
            host.Tracker.BeginUserManipulation(default, new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true));
        host.Tracker.ApplyVerticalScrollDelta(20);
        if (explicitHost) host.Tracker.CompleteVerticalScroll();
        else host.Tracker.CompleteUserManipulation();
        host.Render();
        Assert.Equal(1, calls);
        Assert.Equal(0, participant.IdleCount);
        gate.SetResult(0);
        host.Render();
        Assert.Equal(1, participant.IdleCount);
        Assert.Empty(host.Owner.InertiaVelocities);
    }

    [AvaloniaTheory]
    [InlineData("input")]
    [InlineData("wheel")]
    [InlineData("cancel")]
    [InlineData("configure")]
    [InlineData("absolute")]
    [InlineData("dispose")]
    public void InterruptedPostFlingCannotCompleteOldRelease(string interruption)
    {
        using var host = new Host();
        var participant = new Participant();
        var gate = new TaskCompletionSource<double>();
        var canceled = false;
        host.Tracker.ConfigureVerticalScroll(new Factory(participant), null, null, (_, _, token) =>
        {
            token.Register(() => { Dispatcher.UIThread.VerifyAccess(); canceled = true; });
            return gate.Task;
        });
        host.Tracker.CompleteVerticalScroll();
        host.Render();
        switch (interruption)
        {
            case "input": host.Tracker.ApplyVerticalScrollDelta(10); break;
            case "wheel": host.Tracker.ApplyWheelDelta(new Vector(0, 10), false); break;
            case "cancel": host.Tracker.CancelScroll(); break;
            case "configure": host.Tracker.ConfigureVerticalScrollRelease(null); break;
            case "absolute": host.Tracker.TryUpdatePosition(new Vector3D(0, 10, 0)); break;
            case "dispose": host.Tracker.Dispose(); break;
        }
        host.Render();
        Assert.True(canceled);
        gate.SetResult(0);
        host.Render();
        Assert.Equal(0, participant.IdleCount);
    }

    [AvaloniaFact]
    public void NewInputDuringBodyInertiaPreventsPostFlingForOldGesture()
    {
        using var host = new Host();
        var calls = 0;
        host.Tracker.ConfigureVerticalScroll(new Factory(new()), null, null, (_, _, _) =>
        {
            calls++;
            return Task.FromResult(0d);
        });
        host.Tracker.StartInertia(new Point(0, 400), false);
        host.Render();
        host.Tracker.ApplyVerticalScrollDelta(10);
        host.Render();
        Assert.Equal(0, calls);
        host.Tracker.CompleteVerticalScroll();
        host.Render();
        Assert.Equal(1, calls);
    }

    [AvaloniaFact]
    public void PostFailureIsReportedWithoutParticipantIdle()
    {
        using var host = new Host();
        var participant = new Participant();
        var failure = new InvalidOperationException("test post-fling failure");
        Exception? reported = null;
        host.Tracker.ScrollReleaseFailed += (_, error) => reported = error;
        host.Tracker.ConfigureVerticalScroll(new Factory(participant), null, null,
            (_, _, _) => Task.FromException<double>(failure));
        host.Tracker.CompleteVerticalScroll();
        host.Render();
        Assert.Same(failure, reported);
        Assert.Equal(0, participant.IdleCount);
        host.Tracker.ApplyVerticalScrollDelta(10);
        host.Render();
        Assert.Equal(10, host.Tracker.Position.Y);
    }

    [AvaloniaFact]
    public void UpdatingReleaseCallbackRetainsParticipantAndRemovingItCancelsPendingRelease()
    {
        using var host = new Host();
        var participant = new Participant();
        var factory = new Factory(participant);
        var gate = new TaskCompletionSource<double>();
        var canceled = false;
        host.Tracker.ConfigureVerticalScroll(factory);
        host.Tracker.ApplyVerticalScrollDelta(10);
        host.Tracker.ConfigureVerticalScrollRelease((_, token) =>
        {
            token.Register(() => canceled = true);
            return gate.Task;
        });
        host.Tracker.ApplyVerticalScrollDelta(20);
        host.Tracker.StartInertia(new Point(0, 300), false);
        host.Render();
        Assert.Equal(1, factory.CreateCount);
        Assert.Equal(0, participant.DetachCount);
        Assert.Equal(30, host.Tracker.Position.Y);
        Assert.Empty(host.Owner.InertiaVelocities);
        host.Tracker.ConfigureVerticalScrollRelease(null);
        host.Tracker.StartInertia(new Point(0, 500), false);
        host.Render();
        Assert.True(canceled);
        gate.SetResult(0);
        host.Render();
        Assert.Equal(new Vector3D(0, 500, 0), Assert.Single(host.Owner.InertiaVelocities));
        Assert.Equal(1, factory.CreateCount);
        Assert.Equal(0, participant.DetachCount);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void RealPointerSourceUsesReleaseCallbackWithAndWithoutInertia(bool inertia)
    {
        using var host = new Host();
        using var source = new InputElementInteractionSource(host.Window, host.Tracker)
        {
            PositionXSourceMode = InteractionSourceMode.Disabled,
            PositionYSourceMode = inertia ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.EnabledWithoutInertia,
        };
        var gate = new TaskCompletionSource<double>();
        var offered = new List<double>();
        host.Tracker.ConfigureVerticalScroll(new Factory(new()), canConsumeAtBoundary: null, preFling: (velocity, _) =>
        {
            offered.Add(velocity);
            return gate.Task;
        });
        host.Render();
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        try
        {
            var window = host.Window;
            pointer.Capture(window);
            window.RaiseEvent(new PointerPressedEventArgs(window, pointer, window, new Point(40, 120), 100,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            foreach (var (time, y) in new[] { (116UL, 100d), (132UL, 70d), (148UL, 40d) })
                window.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, window, pointer, window, new Point(40, y), time,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            host.Render();
            var before = host.Tracker.Position.Y;
            Assert.True(before > 0);
            window.RaiseEvent(new PointerReleasedEventArgs(window, pointer, window, new Point(40, 40), 164,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            host.Render();
            Assert.Equal(before, host.Tracker.Position.Y);
            Assert.Empty(host.Owner.InertiaVelocities);
            var velocity = Assert.Single(offered);
            Assert.Equal(inertia, velocity > 0);
            gate.SetResult(velocity);
            host.Render();
            Assert.Equal(before, host.Tracker.Position.Y);
        }
        finally { pointer.Capture(null); }
    }

    [AvaloniaFact]
    public void ReleaseWaitsForPriorSnapshotsAndResumesOnlyRemainingVelocity()
    {
        using var host = new Host();
        var calls = new List<string>();
        var participant = new Participant();
        participant.Publish = () => host.Tracker.DispatchToUI(() => calls.Add("movement"));
        var gate = new TaskCompletionSource<double>();
        host.Tracker.ConfigureVerticalScroll(new Factory(participant), canConsumeAtBoundary: null, preFling: async (velocity, token) =>
        {
            Dispatcher.UIThread.VerifyAccess();
            calls.Add("release");
            Assert.Equal(400, velocity);
            return await gate.Task.WaitAsync(token);
        });
        host.Tracker.ApplyVerticalScrollDelta(10);
        host.Tracker.StartInertia(new Point(30, 400), includeScaleVelocity: false);
        host.Render();
        Assert.Equal(new[] { "movement", "release" }, calls);
        Assert.Empty(host.Owner.InertiaVelocities);
        Assert.Equal(10, host.Tracker.Position.Y);
        host.Render();
        Assert.Equal(10, host.Tracker.Position.Y);

        gate.SetResult(125);
        host.Render();
        Assert.Equal(new Vector3D(30, 275, 0), Assert.Single(host.Owner.InertiaVelocities));
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ZeroVelocityWaitsBeforeParticipantIdleForExplicitAndManipulationRelease(bool explicitHost)
    {
        using var host = new Host();
        var participant = new Participant();
        var gate = new TaskCompletionSource<double>();
        var offered = new List<double>();
        host.Tracker.ConfigureVerticalScroll(new Factory(participant), canConsumeAtBoundary: null, preFling: (velocity, _) =>
        {
            offered.Add(velocity);
            return gate.Task;
        });
        if (!explicitHost)
            host.Tracker.BeginUserManipulation(default, new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true));
        host.Tracker.ApplyVerticalScrollDelta(20);
        if (explicitHost) host.Tracker.CompleteVerticalScroll();
        else host.Tracker.CompleteUserManipulation();
        host.Render();
        Assert.Equal(0, Assert.Single(offered));
        Assert.Equal(0, participant.IdleCount);
        gate.SetResult(0);
        host.Render();
        Assert.Equal(1, participant.IdleCount);
        Assert.Empty(host.Owner.InertiaVelocities);
    }

    [AvaloniaTheory]
    [InlineData("input")]
    [InlineData("wheel")]
    [InlineData("cancel")]
    [InlineData("configure")]
    [InlineData("absolute")]
    [InlineData("dispose")]
    public void InterruptedReleaseCancelsOnUiAndLateCompletionCannotStartOldInertia(string interruption)
    {
        using var host = new Host();
        var gate = new TaskCompletionSource<double>();
        var canceled = false;
        host.Tracker.ConfigureVerticalScroll(new Factory(new()), canConsumeAtBoundary: null, preFling: (_, token) =>
        {
            token.Register(() =>
            {
                Dispatcher.UIThread.VerifyAccess();
                canceled = true;
            });
            // Deliberately ignore cancellation to verify the stale-result barrier.
            return gate.Task;
        });
        host.Tracker.StartInertia(new Point(0, 400), includeScaleVelocity: false);
        host.Render();
        switch (interruption)
        {
            case "input":
                host.Tracker.BeginUserManipulation(default, new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true));
                host.Tracker.ApplyManipulationDelta(new Vector(0, 10));
                break;
            case "wheel": host.Tracker.ApplyWheelDelta(new Vector(0, 10), false); break;
            case "cancel": host.Tracker.CancelScroll(); break;
            case "configure": host.Tracker.ConfigureVerticalScroll(new Factory(new())); break;
            case "absolute": host.Tracker.TryUpdatePosition(new Vector3D(0, 10, 0)); break;
            case "dispose": host.Tracker.Dispose(); break;
        }
        host.Render();
        Assert.True(canceled);
        var position = host.Tracker.Position;
        gate.SetResult(0);
        host.Render();
        Assert.Empty(host.Owner.InertiaVelocities);
        Assert.Equal(position, host.Tracker.Position);
    }

    [AvaloniaFact]
    public void CallbackFailureIsReportedAndDoesNotFallThroughToInertia()
    {
        using var host = new Host();
        var failure = new InvalidOperationException("test release failure");
        Exception? reported = null;
        host.Tracker.ScrollReleaseFailed += (_, error) => reported = error;
        host.Tracker.ConfigureVerticalScroll(new Factory(new()), canConsumeAtBoundary: null, preFling: (_, _) => Task.FromException<double>(failure));
        host.Tracker.StartInertia(new Point(0, 400), false);
        host.Render();
        Assert.Same(failure, reported);
        Assert.Empty(host.Owner.InertiaVelocities);
        host.Tracker.ApplyWheelDelta(new Vector(0, 10), false);
        host.Render();
        Assert.Equal(10, host.Tracker.Position.Y);
    }

    [AvaloniaTheory]
    [InlineData(double.NaN)]
    [InlineData(-1)]
    [InlineData(401)]
    public void InvalidConsumptionIsReportedWithoutStartingInertia(double consumed)
    {
        using var host = new Host();
        Exception? reported = null;
        host.Tracker.ScrollReleaseFailed += (_, error) => reported = error;
        host.Tracker.ConfigureVerticalScroll(new Factory(new()), canConsumeAtBoundary: null, preFling: (_, _) => Task.FromResult(consumed));
        host.Tracker.StartInertia(new Point(0, 400), false);
        host.Render();
        Assert.IsType<InvalidOperationException>(reported);
        Assert.Empty(host.Owner.InertiaVelocities);
    }

    [AvaloniaFact]
    public void MissingCallbackRetainsImmediateOriginalReleasePath()
    {
        using var host = new Host();
        host.Tracker.ConfigureVerticalScroll(new Factory(new()));
        host.Tracker.StartInertia(new Point(30, 400), false);
        host.Render();
        Assert.Equal(new Vector3D(30, 400, 0), Assert.Single(host.Owner.InertiaVelocities));
    }

    private sealed class Factory(Participant participant) : IScrollMovementParticipantFactory
    {
        internal int CreateCount;
        public IScrollMovementParticipant Create() { CreateCount++; return participant; }
    }

    private sealed class Participant : IScrollMovementParticipant
    {
        internal List<ScrollMovementResult> Results { get; } = [];
        internal int IdleCount;
        internal int DetachCount;
        internal Action? Publish;
        public double PreScroll(in ScrollMovementContext context) => 0;
        public double PostScroll(in ScrollMovementContext context, double selfConsumed) => 0;
        public void OnScrollCompleted(in ScrollMovementResult result) { Results.Add(result); Publish?.Invoke(); }
        public void OnIdle(ScrollMovementSource source) => IdleCount++;
        public void Detach() => DetachCount++;
    }

    private sealed class Owner : IInteractionTrackerOwner
    {
        internal readonly List<Vector3D> InertiaVelocities = [];
        public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args) { }
        public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args) { }
        public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args) { }
        public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args) { }
        public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args) { }
        public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args) =>
            InertiaVelocities.Add(args.PositionVelocityInPixelsPerSecond);
    }

    private sealed class Host : IDisposable
    {
        private readonly Window _window = new() { Width = 240, Height = 160, Content = new Border() };
        internal Owner Owner { get; } = new();
        internal Window Window => _window;
        internal InteractionTracker Tracker { get; }
        internal Host()
        {
            _window.Show();
            Tracker = ElementComposition.GetElementVisual(_window)!.Compositor.CreateInteractionTracker(Owner);
            Tracker.MaxPosition = new Vector3D(10000, 10000, 0);
        }
        internal void Render()
        {
            for (var i = 0; i < 5; i++)
            {
                Dispatcher.UIThread.RunJobs();
                using var frame = _window.CaptureRenderedFrame();
            }
            Dispatcher.UIThread.RunJobs();
        }
        internal void RenderUntil(Func<bool> completed)
        {
            for (var i = 0; i < 100 && !completed(); i++)
            {
                Thread.Sleep(1);
                Render();
            }
            Assert.True(completed());
        }
        public void Dispose()
        {
            Tracker.Dispose();
            Render();
            _window.Close();
        }
    }
}

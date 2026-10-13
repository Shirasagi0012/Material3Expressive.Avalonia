using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedBoundsLookaheadTest
{
    private static TimeSpan Ms(int time) => TimeSpan.FromMilliseconds(time);
    private sealed class TextProbe : LookaheadTextBlock
    {
        public int Measures { get; private set; }
        public bool RejectIntermediateWidth { get; set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            Measures++;
            if (RejectIntermediateWidth && availableSize.Width is > 140 and < 200)
                throw new InvalidOperationException("Rejected intermediate content width.");
            return base.MeasureOverride(availableSize);
        }
    }
    private sealed class Scene : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public Transition<bool> Parent { get; }
        public TextProbe FromText { get; } = Text();
        public TextProbe ToText { get; } = Text();
        public SharedBounds From { get; }
        public SharedBounds To { get; }
        public LookaheadRoot FromRoot { get; }
        public LookaheadRoot ToRoot { get; }
        public AnimatedVisibility Source { get; }
        public AnimatedVisibility Target { get; }
        public SharedTransitionHost Host { get; }
        public Window Window { get; }
        public Scene(int fadeMs = 200)
        {
            Parent = new Transition<bool>(true);
            From = Endpoint(FromText); To = Endpoint(ToText);
            FromRoot = new LookaheadRoot { Width = 120, Height = 160, Child = From, UseLayoutRounding = false };
            ToRoot = new LookaheadRoot { Width = 240, Height = 100, Child = To, UseLayoutRounding = false };
            Source = Visibility(FromRoot, true); Target = Visibility(ToRoot, false);
            var stage = new Canvas();
            Canvas.SetLeft(Source, 10); Canvas.SetTop(Source, 20);
            Canvas.SetLeft(Target, 250); Canvas.SetTop(Target, 100);
            stage.Children.Add(Source); stage.Children.Add(Target);
            Host = new SharedTransitionHost { Width = 700, Height = 400, Transition = Parent,
                PreparationTimeout = Timeout.InfiniteTimeSpan, Content = stage };
            Window = new AnimationTestWindow(Frames) { Width = 700, Height = 400, Content = Host };
            Window.Show(); Layout(); Parent.OnFrame(TimeSpan.Zero);

            SharedBounds Endpoint(Control content) => new()
            {
                Key = "text", Content = content, ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
                UseLayoutRounding = false,
                BoundsTransform = new SharedBoundsTransform { Spec = new TweenSpec<Rect>(Ms(200), easing: new LinearEasing()) },
                FadeSpec = new TweenSpec<double>(Ms(fadeMs), easing: new LinearEasing())
            };
            AnimatedVisibility Visibility(Control content, bool when) => new()
            { Content = content, Source = VisibilitySource.From(Parent, state => state == when), Enter = EnterTransition.None, Exit = ExitTransition.None };
        }
        private static TextProbe Text() => new()
        {
            Text = string.Join(" ", Enumerable.Repeat("Live shared text wraps at the current width.", 5)),
            TextWrapping = TextWrapping.Wrap, FontSize = 16, Background = Brushes.LightBlue, UseLayoutRounding = false
        };
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        public SharedTransitionMotion Start()
        {
            Parent.UpdateTarget(false); Layout();
            Assert.Null(Host.LastPreparationError);
            return Assert.Single(Host.Coordinator!.Motions);
        }
        public void Frame(int time) { Parent.OnFrame(Ms(time)); Layout(); }
        public void Dispose() { Window.Close(); Parent.Dispose(); }
    }
    private static void Equal(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 5); Assert.Equal(expected.Y, actual.Y, 5);
        Assert.Equal(expected.Width, actual.Width, 5); Assert.Equal(expected.Height, actual.Height, 5);
    }

    [Fact]
    public Task BothEndpointsRemeasureLiveText_KeepIndependentTargetsAndPlaceholders() => OnUi(() =>
    {
        using var scene = new Scene();
        var motion = scene.Start();
        Equal(new Rect(10, 20, 120, 160), motion.Pair.SourceBounds);
        Equal(new Rect(250, 100, 240, 100), motion.Pair.TargetBounds);
        var sourceSnapshot = scene.FromRoot.Snapshot;
        var targetSnapshot = scene.ToRoot.Snapshot;
        var sourceLines = scene.FromText.TextLayout.TextLines.Count;
        scene.Frame(0); scene.Frame(100);
        var sample = new Rect(130, 60, 180, 130);
        Equal(sample, motion.Bounds.Value);
        foreach (var endpoint in new[] { scene.From, scene.To })
        {
            Assert.Equal(sample.Size, endpoint.Capture!.Bounds.Size);
            Equal(sample, SharedLayoutBounds.ReadLayout(endpoint.Capture, scene.Host)!.Value);
            Assert.Equal(LookaheadConstraints.Fixed(sample.Size), LookaheadMeasure.ActualConstraints((Control)endpoint.Content!));
            Assert.True(endpoint.Suppress!.IsSuppressed);
        }
        Assert.True(scene.FromText.TextLayout.TextLines.Count < sourceLines);
        Assert.Equal(new Size(120, 160), scene.From.Bounds.Size);
        Assert.Equal(new Size(240, 100), scene.To.Bounds.Size);
        Assert.Same(sourceSnapshot, scene.FromRoot.Snapshot); Assert.Same(targetSnapshot, scene.ToRoot.Snapshot);
        Assert.Equal(0, motion.LayoutRevision);
        var item = Assert.Single(scene.Host.Overlay!.Items);
        Assert.Equal(Stretch.None, item.SourceBrush!.Stretch); Assert.Equal(Stretch.None, item.TargetBrush.Stretch);
        Assert.True(item.ReadyToDraw);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task CompletionWaitsForFinalRealLayoutBeforeReleasingVisibilityRetention() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start();
        scene.Frame(0); scene.Frame(100);
        scene.Parent.OnFrame(Ms(200)); // Deliberately do not run Avalonia layout yet.
        Assert.False(motion.Session.IsClosed);
        Assert.True(scene.Host.IsOverlayActive); Assert.True(scene.Source.IsContentPresent);
        Assert.Equal(new Size(180, 130), scene.To.Capture!.Bounds.Size);
        scene.Layout();
        Assert.True(motion.Session.IsClosed); Assert.False(scene.Host.IsOverlayActive);
        Assert.False(scene.To.Suppress!.IsSuppressed); Assert.Null(scene.To.FlightBounds);
        Assert.Equal(new Size(240, 100), scene.ToText.Bounds.Size);
        scene.Frame(216);
        Assert.False(scene.Source.IsContentPresent);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task ReverseDuringFlight_ReusesSessionAndRemeasuresBothSidesAtCurrentBounds() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start();
        scene.Frame(0); scene.Frame(100);
        var middle = motion.Bounds.Value;
        scene.Parent.UpdateTarget(true); scene.Layout();
        Assert.Same(motion, Assert.Single(scene.Host.Coordinator!.Motions));
        Equal(middle, motion.Bounds.Value);
        Equal(new Rect(10, 20, 120, 160), motion.Pair.TargetBounds);
        scene.Frame(200);
        Assert.InRange(scene.FromText.Bounds.Width, 120.01, 179.99);
        Assert.Equal(scene.FromText.Bounds.Size, scene.ToText.Bounds.Size);
        scene.Frame(400);
        Assert.True(motion.Session.IsClosed);
        Assert.False(scene.From.Suppress!.IsSuppressed);
        Assert.Equal(new Size(120, 160), scene.FromText.Bounds.Size);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task FadeAfterBoundsFinish_ContinuesDrawingWithoutAnotherBoundsChange() => OnUi(() =>
    {
        using var scene = new Scene(400); var motion = scene.Start();
        scene.Frame(0); scene.Frame(200);
        var item = Assert.Single(scene.Host.Overlay!.Items);
        var measures = scene.ToText.Measures;
        scene.Frame(300);
        Assert.True(item.ReadyToDraw); Assert.False(motion.Session.IsClosed);
        Assert.Equal(measures, scene.ToText.Measures);
        scene.Frame(400);
        Assert.True(motion.Session.IsClosed);
    });

    [Fact]
    public Task DestinationChange_RetargetsOnceFromCurrentSampleWithoutTargetFeedback() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        var current = motion.Bounds.Value;
        scene.ToRoot.Width = 300; scene.Layout();
        Equal(current, motion.Bounds.Value);
        Equal(new Rect(250, 100, 300, 100), motion.Pair.TargetBounds);
        var snapshot = scene.ToRoot.Snapshot;
        var revision = motion.LayoutRevision;
        Assert.Equal(1, revision);
        scene.Frame(200);
        // Transition uses its interruption spring when an active non-spring spec is retargeted.
        Assert.InRange(scene.ToText.Bounds.Width, 180.01, 299.99);
        Assert.Equal(motion.Bounds.Value.Size, scene.ToText.Bounds.Size);
        Assert.Equal(new Size(300, 100), scene.To.Bounds.Size);
        Assert.Same(snapshot, scene.ToRoot.Snapshot); Assert.Equal(revision, motion.LayoutRevision);
        scene.Frame(400);
        Assert.True(motion.Session.IsClosed); Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task TargetChangesBeforePendingHandoff_InvalidatesTheOldCompletion() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        scene.Parent.OnFrame(Ms(200));
        Assert.Equal(SharedSessionState.AwaitingHandoff, motion.Session.State);
        scene.ToRoot.Width = 300; scene.Layout();
        Assert.False(motion.Session.IsClosed); Assert.True(scene.Host.IsOverlayActive);
        Assert.Equal(1, motion.LayoutRevision);
        scene.Frame(300); Assert.InRange(scene.ToText.Bounds.Width, 240.01, 299.99);
        scene.Frame(500);
        Assert.True(motion.Session.IsClosed); Assert.False(scene.To.Suppress!.IsSuppressed);
        Assert.Equal(300, scene.ToText.Bounds.Width);
    });

    [Fact]
    public Task FractionalFrames_CommitWithoutRoundingLoopAndExternalRootMovementIsCorrected() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(17);
        var item = Assert.Single(scene.Host.Overlay!.Items);
        Assert.True(item.ReadyToDraw);
        Equal(motion.Bounds.Value, SharedLayoutBounds.ReadLayout(scene.To.Capture!, scene.Host)!.Value);
        Canvas.SetLeft(scene.Target, 310.5); scene.Layout();
        Assert.False(motion.Session.IsClosed);
        Equal(motion.Bounds.Value, SharedLayoutBounds.ReadLayout(scene.To.Capture!, scene.Host)!.Value);
        Assert.True(item.ReadyToDraw);
        scene.Frame(300);
        Assert.True(motion.Session.IsClosed);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task Cancellation_RestoresNativeTargetLayoutAndClearsBothSamples() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        motion.Session.Cancel(SharedSessionEndReason.Cancelled); scene.Layout();
        Assert.Null(scene.From.FlightBounds); Assert.Null(scene.To.FlightBounds);
        Assert.False(scene.Host.IsOverlayActive); Assert.False(scene.To.Suppress!.IsSuppressed);
        Assert.Equal(new Size(120, 160), scene.FromText.Bounds.Size);
        Assert.Equal(new Size(240, 100), scene.ToText.Bounds.Size);
    });

    [Fact]
    public Task MixedResizeModes_OnlyRemeasureTheOptedInEndpoint() => OnUi(() =>
    {
        using var scene = new Scene(); scene.From.ResizeMode = SharedBoundsResizeMode.ScaleToBounds;
        var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        Assert.Equal(new Size(120, 160), scene.FromText.Bounds.Size);
        Assert.Equal(new Size(180, 130), scene.ToText.Bounds.Size);
        var item = Assert.Single(scene.Host.Overlay!.Items);
        Assert.Equal(Stretch.Fill, item.SourceBrush!.Stretch); Assert.Equal(Stretch.None, item.TargetBrush.Stretch);
        Assert.True(item.ReadyToDraw); Assert.Null(scene.Host.LastSessionError);
        scene.Frame(200); Assert.True(motion.Session.IsClosed);
    });

    [Fact]
    public Task PlaceholderDoesNotMoveStackSiblingWhileContentRemeasures() => OnUi(() =>
    {
        using var scene = new Scene();
        scene.FromRoot.Child = null;
        scene.From.Height = 100;
        var sibling = new LookaheadTextBlock { Text = "Sibling", Height = 20 };
        var stack = new LookaheadStackPanel { Children = { scene.From, sibling } };
        scene.FromRoot.Child = stack; scene.Layout();
        var position = sibling.Bounds.Y;
        var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        Assert.Equal(position, sibling.Bounds.Y);
        Assert.Equal(100, scene.From.Bounds.Height);
        Assert.Equal(180, scene.FromText.Bounds.Width);
        Assert.Equal(new Size(180, 100), scene.From.Capture!.Bounds.Size);
        Assert.Equal(0, motion.LayoutRevision);
    });

    [Fact]
    public Task UnsupportedRemeasureContentIsRejectedBeforeSpeculativeNativeMeasure() => OnUi(() =>
    {
        var ordinary = new Border { Width = 100, Height = 80 };
        var endpoint = new SharedBounds { Content = ordinary, ResizeMode = SharedBoundsResizeMode.RemeasureToBounds };
        Assert.Throws<NotSupportedException>(() => endpoint.Measure(new Size(300, 200)));
        var builder = new LookaheadLayoutBuilder();
        Assert.Throws<NotSupportedException>(() => builder.Measure(endpoint, new Size(300, 200)));
        Assert.False(ordinary.IsMeasureValid);
    });

    [Fact]
    public Task DirtyTargetBeforeFrame_DoesNotCancelFlightOrReverseTheContentFade() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(40);
        var item = Assert.Single(scene.Host.Overlay!.Items);
        Assert.Equal(0.2, item.Fade, 5);
        scene.ToRoot.Width = 300;
        scene.ToText.Text += " Updated during flight.";
        Assert.Null(scene.ToRoot.Snapshot);
        scene.Parent.OnFrame(Ms(50)); // Target is dirty, but the last animation sample is still usable.
        scene.Layout();
        Assert.False(motion.Session.IsClosed);
        Assert.Equal(0.25, item.Fade, 5); // A layout update does not swap the source/target roles.
        Assert.True(item.ReadyToDraw);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task ActualContentMeasureFailure_ReleasesBothSamplesAndSessionRetention() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0);
        scene.ToText.RejectIntermediateWidth = true;
        Assert.Throws<InvalidOperationException>(() => scene.Frame(100));
        Assert.True(motion.Session.IsClosed); Assert.Equal(SharedSessionEndReason.Failed, motion.Session.EndReason);
        Assert.Null(scene.From.FlightBounds); Assert.Null(scene.To.FlightBounds);
        Assert.False(scene.Host.IsOverlayActive); Assert.NotNull(scene.Host.LastSessionError);
        scene.ToText.RejectIntermediateWidth = false; scene.Layout();
        Assert.Equal(new Size(240, 100), scene.ToText.Bounds.Size);
    });

    [Fact]
    public Task UnsupportedTargetReplacement_CancelsAndRecoversAfterContentIsRestored() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        scene.To.Content = new Border { Width = 100, Height = 100 };
        Assert.Throws<NotSupportedException>(scene.Layout);
        Assert.True(motion.Session.IsClosed); Assert.False(scene.Host.IsOverlayActive);
        Assert.Null(scene.From.FlightBounds); Assert.Null(scene.To.FlightBounds);
        scene.To.Content = scene.ToText; scene.Layout();
        Assert.Equal(new Size(240, 100), scene.ToText.Bounds.Size);
    });

    [Fact]
    public Task ZeroDurationScale_StillLaysOutTheDestinationBeforeHandoff() => OnUi(() =>
    {
        using var scene = new Scene(); scene.Parent.DurationScale = 0;
        var motion = scene.Start();
        scene.Parent.OnFrame(Ms(0));
        Assert.True(scene.Host.IsOverlayActive);
        scene.Layout();
        Assert.True(motion.Session.IsClosed); Assert.False(scene.Host.IsOverlayActive);
        Assert.Equal(new Size(240, 100), scene.ToText.Bounds.Size);
        Assert.False(scene.To.Suppress!.IsSuppressed);
    });

    [Fact]
    public Task DetachingHost_ClearsInFlightLayoutOwnership() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        scene.Window.Content = null; scene.Layout();
        Assert.True(motion.Session.IsClosed);
        Assert.Null(scene.From.FlightBounds); Assert.Null(scene.To.FlightBounds);
        Assert.False(scene.Host.IsOverlayActive);
    });

    [Fact]
    public Task NestedSharedBounds_EachUsesItsOwnSharedRectWithoutAncestorFeedback() => OnUi(() =>
    {
        using var scene = new Scene();
        scene.From.Content = null; scene.To.Content = null;
        var fromChild = new SharedBounds
        {
            Key = "nested", Width = 60, Height = 70, Content = scene.FromText,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            ResizeMode = SharedBoundsResizeMode.RemeasureToBounds, BoundsTransform = scene.From.BoundsTransform,
            FadeSpec = scene.From.FadeSpec, UseLayoutRounding = false
        };
        var toChild = new SharedBounds
        {
            Key = "nested", Width = 100, Height = 60, Content = scene.ToText,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            ResizeMode = SharedBoundsResizeMode.RemeasureToBounds, BoundsTransform = scene.To.BoundsTransform,
            FadeSpec = scene.To.FadeSpec, UseLayoutRounding = false
        };
        scene.From.Content = new LookaheadBox { Children = { fromChild }, UseLayoutRounding = false };
        scene.To.Content = new LookaheadBox { Children = { toChild }, UseLayoutRounding = false };
        scene.Layout(); scene.Parent.UpdateTarget(false); scene.Layout();
        Assert.Equal(2, scene.Host.Coordinator!.Motions.Count);
        var snapshot = scene.ToRoot.Snapshot;
        scene.Frame(0); scene.Frame(100);
        var childMotion = Assert.Single(scene.Host.Coordinator.Motions, motion => Equals(motion.Pair.Target.Key, "nested"));
        Equal(new Rect(230, 60, 80, 65), childMotion.Bounds.Value);
        Equal(childMotion.Bounds.Value, SharedLayoutBounds.ReadLayout(fromChild.Capture!, scene.Host)!.Value);
        Equal(childMotion.Bounds.Value, SharedLayoutBounds.ReadLayout(toChild.Capture!, scene.Host)!.Value);
        Assert.All(scene.Host.Overlay!.Items, item => Assert.True(item.ReadyToDraw));
        Assert.True(fromChild.Suppress!.IsSuppressed); Assert.True(toChild.Suppress!.IsSuppressed);
        Assert.Same(snapshot, scene.ToRoot.Snapshot);
        scene.Frame(200);
        Assert.False(scene.Host.IsOverlayActive); Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task RemovingSourceBeforeHandoff_CannotLeaveAnUnfinishableSession() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        scene.Parent.OnFrame(Ms(200));
        scene.Source.Content = null; scene.Layout();
        Assert.True(motion.Session.IsClosed); Assert.False(scene.Host.IsOverlayActive);
        Assert.Null(scene.From.FlightBounds); Assert.Null(scene.To.FlightBounds);
    });

    [Fact]
    public Task UnsupportedEndpointTransform_FailsRatherThanRetryingPlacementForever() => OnUi(() =>
    {
        using var scene = new Scene(); var motion = scene.Start(); scene.Frame(0);
        scene.To.RenderTransform = new TranslateTransform(10, 0);
        Assert.Throws<NotSupportedException>(() => scene.Frame(100));
        Assert.True(motion.Session.IsClosed); Assert.False(scene.Host.IsOverlayActive);
        scene.To.RenderTransform = null; scene.Layout();
        Assert.Null(scene.To.FlightBounds);
    });

    [Fact]
    public Task CollapsedIntermediateAxis_IsValidFlightGeometryAndCanExpandAgain() => OnUi(() =>
    {
        using var scene = new Scene();
        var config = new KeyframesSpecConfig<Rect> { Duration = Ms(200) };
        config.At(100, new Rect(130, 60, 0, 130));
        scene.To.BoundsTransform = new SharedBoundsTransform { Spec = new KeyframesSpec<Rect>(config) };
        var motion = scene.Start(); scene.Frame(0); scene.Frame(100);
        Assert.Equal(0, scene.From.Capture!.Bounds.Width);
        Assert.Equal(0, scene.To.Capture!.Bounds.Width);
        Assert.True(Assert.Single(scene.Host.Overlay!.Items).ReadyToDraw);
        scene.Frame(200);
        Assert.True(motion.Session.IsClosed); Assert.False(scene.Host.IsOverlayActive);
        Assert.Equal(240, scene.To.Capture.Bounds.Width);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task OrdinaryScaleMode_AcceptsFullConstraintParentsOutsideLookahead() => OnUi(() =>
    {
        var endpoint = new SharedBounds { Content = new Border { Width = 120, Height = 80 } };
        var size = new Size(200, 100);
        Assert.Equal(size, LookaheadMeasure.Measure(endpoint, LookaheadConstraints.Fixed(size)));
        endpoint.Arrange(new Rect(size));
        Assert.Equal(size, endpoint.Bounds.Size);
        Assert.Null(endpoint.FlightBounds);
    });
}

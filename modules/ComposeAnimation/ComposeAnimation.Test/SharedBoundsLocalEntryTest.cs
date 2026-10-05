using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedBoundsLocalEntryTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);

    private sealed class ConstraintProbe : Control, ILookaheadConstrainedLayoutNode
    {
        public LookaheadConstraints Target { get; private set; }
        public LookaheadConstraints Actual { get; private set; }
        public IEnumerable<Control> LookaheadChildren => [];
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, Size available) => new(20, 10);
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        {
            Target = constraints;
            return new Size(20, 10);
        }
        public Size MeasureApproach(LookaheadConstraints constraints)
        {
            Actual = constraints;
            return new Size(20, 10);
        }
        public void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds) { }
        protected override Size MeasureCore(Size availableSize)
            => LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired)
                ? desired : base.MeasureCore(availableSize);
        protected override Size ArrangeOverride(Size finalSize) => finalSize;
        protected override void ArrangeCore(Rect finalRect)
        {
            if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
            else base.ArrangeCore(finalRect);
        }
    }

    private sealed class Scene : IDisposable
    {
        public ManualAnimationFrameClock Clock { get; } = new();
        public Transition<bool> Parent { get; }
        public SharedBounds From { get; }
        public SharedBounds To { get; }
        public LookaheadTextBlock FromText { get; }
        public LookaheadTextBlock ToText { get; }
        public AnimatedVisibility Source { get; }
        public AnimatedVisibility Target { get; }
        public SharedTransitionHost Host { get; }
        public Window Window { get; }

        public Scene()
        {
            Parent = new Transition<bool>(true, Clock);
            FromText = Text();
            ToText = Text();
            From = Endpoint(FromText, 120, 160);
            To = Endpoint(ToText, 240, 100);
            Source = Visibility(From, true);
            Target = Visibility(To, false);
            var stage = new Canvas();
            Canvas.SetLeft(Source, 10);
            Canvas.SetTop(Source, 20);
            Canvas.SetLeft(Target, 250);
            Canvas.SetTop(Target, 100);
            stage.Children.Add(Source);
            stage.Children.Add(Target);
            Host = new SharedTransitionHost
            {
                Width = 700,
                Height = 400,
                Transition = Parent,
                PreparationTimeout = Timeout.InfiniteTimeSpan,
                Content = stage
            };
            Window = new AnimationTestWindow(Clock) { Width = 700, Height = 400, Content = Host };
            Window.Show();
            Layout();
            Parent.OnFrame(TimeSpan.Zero);
        }

        public SharedTransitionMotion Start()
        {
            Parent.UpdateTarget(false);
            Layout();
            Assert.Null(Host.LastPreparationError);
            return Assert.Single(Host.Coordinator!.Motions);
        }

        public void Frame(int value)
        {
            Parent.OnFrame(Ms(value));
            Layout();
        }

        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Window.Close();
            Parent.Dispose();
        }

        private SharedBounds Endpoint(Control content, double width, double height) => new()
        {
            Key = "text",
            Width = width,
            Height = height,
            Content = content,
            ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            UseLayoutRounding = false,
            BoundsTransform = new SharedBoundsTransform
            {
                Spec = new TweenSpec<Rect>(Ms(200), easing: new LinearEasing())
            },
            FadeSpec = new TweenSpec<double>(Ms(200), easing: new LinearEasing())
        };

        private static LookaheadTextBlock Text() => new()
        {
            Text = string.Join(" ", Enumerable.Repeat("Live shared text wraps at the current width.", 5)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            Background = Brushes.LightBlue,
            UseLayoutRounding = false
        };

        private AnimatedVisibility Visibility(Control content, bool state) => new()
        {
            Content = content,
            Source = VisibilitySource.From(Parent, value => value == state),
            Enter = EnterTransition.None,
            Exit = ExitTransition.None
        };
    }

    [Fact]
    public Task RemeasureWithoutRoot_UsesIndependentLocalDomainsAndHostCoordinates() => OnUi(() =>
    {
        using var scene = new Scene();
        var fromSnapshot = Assert.IsType<LookaheadLayoutSnapshot>(Lookahead.GetSnapshot(scene.From));
        var motion = scene.Start();
        var toSnapshot = Assert.IsType<LookaheadLayoutSnapshot>(Lookahead.GetSnapshot(scene.To));
        Assert.NotSame(fromSnapshot, toSnapshot);
        Assert.NotSame(LookaheadScope.FindContext(scene.From), LookaheadScope.FindContext(scene.To));
        AssertRect(new Rect(10, 20, 120, 160), motion.Pair.SourceBounds);
        AssertRect(new Rect(250, 100, 240, 100), motion.Pair.TargetBounds);

        scene.Frame(0);
        scene.Frame(100);
        var middle = new Rect(130, 60, 180, 130);
        AssertRect(middle, motion.Bounds.Value);
        AssertRect(middle, SharedLayoutBounds.ReadLayout(scene.From.Capture!, scene.Host)!.Value);
        AssertRect(middle, SharedLayoutBounds.ReadLayout(scene.To.Capture!, scene.Host)!.Value);
        Assert.Same(fromSnapshot, Lookahead.GetSnapshot(scene.From));
        Assert.Same(toSnapshot, Lookahead.GetSnapshot(scene.To));
        Assert.True(Assert.Single(scene.Host.Overlay!.Items).ReadyToDraw);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task FinalHandoff_RejectsOldLocalTargetGenerationAndCompletesAfterRelayout() => OnUi(() =>
    {
        using var scene = new Scene();
        var motion = scene.Start();
        scene.Frame(0);
        var oldVersion = Lookahead.GetSnapshot(scene.To)!.Version;

        scene.Parent.OnFrame(Ms(200));
        scene.To.Width = 300;
        scene.Layout();

        Assert.False(motion.Session.IsClosed);
        Assert.True(Lookahead.GetSnapshot(scene.To)!.Version > oldVersion);
        Assert.Equal(1, motion.LayoutRevision);
        Assert.True(scene.Host.IsOverlayActive);

        scene.Frame(400);
        Assert.True(motion.Session.IsClosed);
        Assert.False(scene.Host.IsOverlayActive);
        Assert.Equal(new Size(300, 100), scene.To.Capture!.Bounds.Size);
        Assert.False(scene.To.Suppress!.IsSuppressed);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task SameRectTargetGeneration_RearmsPendingHandoffAndDoesNotStall() => OnUi(() =>
    {
        using var scene = new Scene();
        var motion = scene.Start();
        scene.Frame(0);
        var oldVersion = Lookahead.GetSnapshot(scene.To)!.Version;

        scene.Parent.OnFrame(Ms(200));
        Assert.Equal(SharedSessionState.AwaitingHandoff, motion.Session.State);
        scene.ToText.Text += " Same outer rectangle, new target content.";
        scene.Layout();

        Assert.True(Lookahead.GetSnapshot(scene.To)!.Version > oldVersion);
        Assert.Equal(new Rect(250, 100, 240, 100), motion.Pair.TargetBounds);
        // Revision 1 proves the revision-0 handoff was withdrawn even when the newly armed
        // completion can finish in the same dispatcher/layout pump.
        Assert.Equal(1, motion.LayoutRevision);

        if (!motion.Session.IsClosed) scene.Layout();
        Assert.True(motion.Session.IsClosed);
        Assert.False(scene.Host.IsOverlayActive);
        Assert.Null(scene.Host.LastSessionError);
    });

    [Fact]
    public Task ReparentBeforeAttachment_ReleasesLocalDomainAndCoordinateReference() => OnUi(() =>
    {
        var endpoint = new SharedBounds
        {
            Width = 120,
            Height = 80,
            ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            Content = new LookaheadTextBlock { Text = "Local shared content" }
        };
        var firstParent = new Grid { Children = { endpoint } };
        endpoint.Measure(new Size(400, 300));
        endpoint.Arrange(new Rect(20, 30, endpoint.DesiredSize.Width, endpoint.DesiredSize.Height));
        var firstContext = Assert.IsType<LookaheadContext>(LookaheadEnvironment.GetContext(endpoint));

        firstParent.Children.Remove(endpoint);
        Assert.Null(LookaheadEnvironment.GetContext(endpoint));

        var secondParent = new Grid { Children = { endpoint } };
        endpoint.Measure(new Size(300, 200));
        endpoint.Arrange(new Rect(5, 8, endpoint.DesiredSize.Width, endpoint.DesiredSize.Height));
        var secondContext = Assert.IsType<LookaheadContext>(LookaheadEnvironment.GetContext(endpoint));
        Assert.NotSame(firstContext, secondContext);
        Assert.Same(secondParent, secondContext.CoordinateReference);
    });

    [Fact]
    public Task FirstFullConstraintMeasure_AndReparentedFullMeasure_PreserveMinima() => OnUi(() =>
    {
        var probe = new ConstraintProbe();
        var endpoint = new SharedBounds
        {
            ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            Content = probe,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        var firstParent = new Grid { Children = { endpoint } };
        var constraints = new LookaheadConstraints(120, 180, 80, 120);

        Assert.Equal(constraints.MinSize, LookaheadMeasure.Measure(endpoint, constraints));
        endpoint.Arrange(new Rect(10, 20, constraints.MinSize.Width, constraints.MinSize.Height));
        var firstContext = Assert.IsType<LookaheadContext>(LookaheadEnvironment.GetContext(endpoint));
        Assert.Equal(constraints, Lookahead.GetSnapshot(endpoint)!.Results[endpoint].FullMeasureConstraints);
        Assert.Equal(constraints, probe.Target);
        Assert.Equal(constraints, probe.Actual);

        firstParent.Children.Remove(endpoint);
        Assert.Null(LookaheadEnvironment.GetContext(endpoint));
        var secondParent = new Grid { Children = { endpoint } };
        Assert.Equal(constraints.MinSize, LookaheadMeasure.Measure(endpoint, constraints));
        endpoint.Arrange(new Rect(5, 8, constraints.MinSize.Width, constraints.MinSize.Height));

        var secondContext = Assert.IsType<LookaheadContext>(LookaheadEnvironment.GetContext(endpoint));
        Assert.NotSame(firstContext, secondContext);
        Assert.Same(secondParent, secondContext.CoordinateReference);
        Assert.Equal(constraints, probe.Target);
        Assert.Equal(constraints, probe.Actual);
    });

    [Fact]
    public Task ScaleToBoundsOutsideRoot_KeepsOrdinaryLayoutPath() => OnUi(() =>
    {
        var content = new Border { Width = 80, Height = 60 };
        var endpoint = new SharedBounds { Width = 120, Height = 90, Content = content };
        endpoint.Measure(new Size(300, 200));
        endpoint.Arrange(new Rect(5, 7, endpoint.DesiredSize.Width, endpoint.DesiredSize.Height));

        Assert.Null(LookaheadScope.FindContext(endpoint));
        Assert.Equal(new Size(120, 90), endpoint.Bounds.Size);
        Assert.Null(endpoint.FlightBounds);
    });

    private static void AssertRect(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 5);
        Assert.Equal(expected.Y, actual.Y, 5);
        Assert.Equal(expected.Width, actual.Width, 5);
        Assert.Equal(expected.Height, actual.Height, 5);
    }
}

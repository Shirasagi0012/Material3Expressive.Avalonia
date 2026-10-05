using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace ComposeAnimation.Test;

public class LookaheadApiTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    [Fact]
    public void StandaloneBuilderSnapshot_UsesMeasuredEntryAsItsCoordinateReference()
    {
        var entry = new Border { Width = 20, Height = 10 };
        var builder = new LookaheadLayoutBuilder();
        builder.Measure(entry, new Size(100, 80));
        builder.Place(entry, new Rect(4, 6, 100, 80));

        var snapshot = builder.Build(7, new Size(100, 80));

        Assert.Same(entry, snapshot.Entry);
        Assert.Same(entry, snapshot.CoordinateReference);
        Assert.Equal(7, snapshot.Version);
    }

    [Fact]
    public Task Snapshot_IsExposedOnlyForTheEntryAndCommittedMembers() => OnUi(() =>
    {
        var member = new Border { Width = 30, Height = 20 };
        var ordinary = new Grid { Children = { new Border { Width = 10, Height = 10 } } };
        var boundary = new LookaheadBoundary { Child = ordinary };
        var inheritedNonMember = new TextBlock { Text = "opaque content" };
        var opaqueMember = new Border { Width = 80, Height = 30, Child = inheritedNonMember };
        using var fixture = new Fixture(new LookaheadBox { Children = { member, boundary, opaqueMember } });

        Assert.Same(fixture.Root.Snapshot, Lookahead.GetSnapshot(fixture.Root));
        Assert.Same(fixture.Root.Snapshot, Lookahead.GetSnapshot(member));
        Assert.Same(fixture.Root.Snapshot!.Results[member], Lookahead.TargetFor(member));
        Assert.Null(Lookahead.TargetFor(fixture.Root));
        Assert.Null(Lookahead.GetSnapshot(ordinary));
        Assert.Null(Lookahead.GetSnapshot(ordinary.Children[0]));
        Assert.Same(fixture.Root.Snapshot, Lookahead.GetSnapshot(opaqueMember));
        Assert.Null(Lookahead.GetSnapshot(inheritedNonMember));
        Assert.Null(Lookahead.GetSnapshot(new Border()));
        Assert.Same(fixture.Root, fixture.Root.Snapshot!.Entry);
        Assert.Same(fixture.Root, fixture.Root.Snapshot.CoordinateReference);
    });

    [Fact]
    public Task CoordinateTryApis_ReturnCommittedGeometryAndRejectUnavailableReferences() => OnUi(() =>
    {
        var first = new Border { Width = 30, Height = 20 };
        var second = new Border { Width = 40, Height = 25 };
        using var fixture = new Fixture(new LookaheadStackPanel
        {
            Spacing = 7,
            Children = { first, second },
            UseLayoutRounding = false
        });

        Assert.True(LookaheadCoordinates.TryGetTargetBounds(second, out var target));
        Assert.True(LookaheadCoordinates.TryGetActualBounds(second, out var actual));
        Assert.Equal(fixture.Root.Snapshot!.Results[second].Bounds, target);
        Assert.Equal(target, actual);
        Assert.False(LookaheadCoordinates.TryGetTargetBounds(second, out _, new Border()));
        Assert.False(LookaheadCoordinates.TryGetActualBounds(new Border(), out _));

        Lookahead.InvalidateTargetLayout(second);
        Assert.False(LookaheadCoordinates.TryGetTargetBounds(second, out _));
        Assert.False(LookaheadCoordinates.TryGetActualBounds(second, out _));
    });

    [Fact]
    public Task DirectTargetEvent_ReachesEachProtocolParticipantOncePerCommit_AndQueuesInvalidation() => OnUi(() =>
    {
        var text = new LookaheadTextBlock { Text = "event" };
        var opaque = new Border { Width = 20, Height = 10 };
        var box = new LookaheadBox { Children = { text, opaque } };
        var boxVersions = new List<long>();
        var textVersions = new List<long>();
        var opaqueEvents = 0;
        var invalidateDuringNextEvent = false;
        var sawDirtyQuery = false;
        var sawReentryRejection = false;
        EventHandler<LookaheadTargetLayoutEventArgs> boxHandler = (sender, args) =>
        {
            Assert.Same(box, sender);
            boxVersions.Add(args.Snapshot.Version);
            if (!invalidateDuringNextEvent) return;
            invalidateDuringNextEvent = false;
            Lookahead.InvalidateTargetLayout(box);
            sawDirtyQuery = Lookahead.GetSnapshot(box) is null;
            sawReentryRejection = Assert.Throws<InvalidOperationException>(() =>
                args.Snapshot.Entry.Measure(new Size(200, 160))) is not null;
            text.InvalidateMeasure();
            Assert.Throws<InvalidOperationException>(() => text.Measure(new Size(100, 60)));
            Assert.Throws<InvalidOperationException>(() =>
                LookaheadMeasure.Measure(text, LookaheadConstraints.Fixed(new Size(100, 60))));
        };
        EventHandler<LookaheadTargetLayoutEventArgs> textHandler = (sender, args) =>
        {
            Assert.Same(text, sender);
            textVersions.Add(args.Snapshot.Version);
        };
        EventHandler<LookaheadTargetLayoutEventArgs> opaqueHandler = (_, _) => opaqueEvents++;
        Lookahead.AddTargetLayoutUpdatedHandler(box, boxHandler);
        Lookahead.AddTargetLayoutUpdatedHandler(text, textHandler);
        Lookahead.AddTargetLayoutUpdatedHandler(opaque, opaqueHandler);
        using var fixture = new Fixture(box);
        boxVersions.Clear();
        textVersions.Clear();
        opaqueEvents = 0;

        invalidateDuringNextEvent = true;
        Lookahead.InvalidateTargetLayout(box);
        fixture.Layout();
        fixture.Layout();

        Assert.True(sawDirtyQuery);
        Assert.True(sawReentryRejection);
        Assert.NotEmpty(boxVersions);
        Assert.Equal(boxVersions, textVersions);
        Assert.Equal(boxVersions.Count, boxVersions.Distinct().Count());
        Assert.Equal(0, opaqueEvents);
        Assert.NotNull(Lookahead.GetSnapshot(box));

        Lookahead.RemoveTargetLayoutUpdatedHandler(box, boxHandler);
        Lookahead.RemoveTargetLayoutUpdatedHandler(text, textHandler);
        Lookahead.RemoveTargetLayoutUpdatedHandler(opaque, opaqueHandler);
    });

    [Fact]
    public Task LocalSnapshot_CapturesEntryAndItsNativeParentReference() => OnUi(() =>
    {
        var entry = new AnimatedBounds
        {
            TargetSize = new Size(90, 50),
            Child = new LookaheadTextBlock { Text = "local" },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        var parent = new Grid { Width = 240, Height = 160, Children = { entry } };
        using var fixture = new WindowFixture(parent);

        var snapshot = Lookahead.GetSnapshot(entry)!;
        Assert.Same(entry, snapshot.Entry);
        Assert.Same(parent, snapshot.CoordinateReference);
        Assert.True(LookaheadCoordinates.TryGetTargetBounds(entry, out var target));
        Assert.Equal(snapshot.Results[entry].Bounds, target);
    });

    [Fact]
    public Task DirtyTarget_IsNotPublishedByTheFacade() => OnUi(() =>
    {
        var member = new Border { Width = 30, Height = 20 };
        using var fixture = new Fixture(new LookaheadBox { Children = { member } });
        var previous = fixture.Root.Snapshot!;

        Lookahead.InvalidateTargetLayout(member);
        Lookahead.InvalidateTargetLayout(member); // Membership remains valid while the target is dirty.

        Assert.Null(Lookahead.GetSnapshot(fixture.Root));
        Assert.Null(Lookahead.GetSnapshot(member));
        Assert.Null(Lookahead.TargetFor(member));
        fixture.Layout();
        Assert.NotNull(Lookahead.GetSnapshot(member));
        Assert.True(fixture.Root.Snapshot!.Version > previous.Version);
    });

    [Fact]
    public Task ApproachInvalidation_PreservesTheCommittedTarget() => OnUi(() =>
    {
        var member = new Probe { Width = 30, Height = 20 };
        using var fixture = new Fixture(new LookaheadBox { Children = { member } });
        var target = fixture.Root.Snapshot!;

        Lookahead.InvalidateApproachArrange(member);
        Assert.Same(target, Lookahead.GetSnapshot(member));
        Assert.False(fixture.Root.IsArrangeValid);
        fixture.Layout();
        Assert.Same(target, fixture.Root.Snapshot);

        Lookahead.InvalidateApproachMeasure(member);
        Assert.Same(target, Lookahead.GetSnapshot(member));
        Assert.False(member.IsMeasureValid);
        Assert.False(fixture.Root.IsArrangeValid);
        fixture.Layout();
        Assert.Same(target, fixture.Root.Snapshot);
    });

    [Fact]
    public Task MoveContent_UsesTheOwningDomainAndInvalidatesItsTarget() => OnUi(() =>
    {
        var member = new Border { Width = 30, Height = 20 };
        var first = new LookaheadBox { Children = { member } };
        var second = new LookaheadBox();
        using var fixture = new Fixture(new LookaheadBox { Children = { first, second } });

        Lookahead.MoveContent(member, () =>
        {
            first.Children.Remove(member);
            second.Children.Add(member);
        });

        Assert.Null(Lookahead.GetSnapshot(member));
        fixture.Layout();
        Assert.Same(fixture.Root.Snapshot, Lookahead.GetSnapshot(member));
        Assert.Same(second, member.Parent);
    });

    [Fact]
    public Task MutationApis_RejectControlsThatAreNotCommittedMembers() => OnUi(() =>
    {
        var ordinary = new Grid { Children = { new Border { Width = 10, Height = 10 } } };
        var boundary = new LookaheadBoundary { Child = ordinary };
        var inheritedNonMember = new TextBlock { Text = "opaque content" };
        var opaqueMember = new Border { Width = 80, Height = 30, Child = inheritedNonMember };
        using var fixture = new Fixture(new LookaheadBox { Children = { boundary, opaqueMember } });
        var outside = new Border();

        Assert.Throws<InvalidOperationException>(() => Lookahead.InvalidateTargetLayout(outside));
        Assert.Throws<InvalidOperationException>(() => Lookahead.InvalidateTargetLayout(inheritedNonMember));
        Assert.Throws<InvalidOperationException>(() => Lookahead.InvalidateApproachMeasure(ordinary));
        Assert.Throws<InvalidOperationException>(() => Lookahead.InvalidateApproachArrange(ordinary));
        Assert.Throws<InvalidOperationException>(() => Lookahead.MoveContent(ordinary, () => { }));
        Assert.Throws<InvalidOperationException>(() => Lookahead.MoveContent(fixture.Root, () => { }));
    });

    private sealed class Fixture : IDisposable
    {
        internal LookaheadRoot Root { get; }
        private Window Window { get; }

        internal Fixture(Control content)
        {
            Root = new LookaheadRoot
            {
                Width = 200,
                Height = 160,
                Child = content,
                UseLayoutRounding = false
            };
            Window = new AnimationTestWindow() { Width = 300, Height = 240, Content = Root };
            Window.Show();
            Layout();
        }

        internal void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => Window.Close();
    }

    private sealed class WindowFixture : IDisposable
    {
        private readonly Window _window;

        internal WindowFixture(Control content)
        {
            _window = new AnimationTestWindow() { Width = 300, Height = 220, Content = content };
            _window.Show();
            Dispatcher.UIThread.RunJobs();
            _window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            _window.UpdateLayout();
        }

        public void Dispose() => _window.Close();
    }

    private sealed class Probe : Control
    {
        protected override Size MeasureOverride(Size availableSize) => availableSize;
        protected override Size ArrangeOverride(Size finalSize) => finalSize;
    }
}

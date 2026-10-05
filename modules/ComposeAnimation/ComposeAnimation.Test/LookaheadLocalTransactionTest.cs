using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ComposeAnimation.Test;

public class LookaheadLocalTransactionTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private static AnimatedBounds Entry() => new()
    {
        Child = new LookaheadTextBlock { Text = "A local layout transaction", UseLayoutRounding = false },
        TargetSize = new Size(120, 60),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false
    };

    [Fact]
    public Task FirstFullConstraintMeasure_PreparesDomainBeforeCacheAndPreservesMinima() => OnUi(() =>
    {
        var entry = Entry();
        entry.TargetSize = new Size(20, 10);
        entry.Margin = new Thickness(5, 3);
        var incoming = new LookaheadConstraints(100, 160, 80, 120);
        Assert.Equal(new Size(100, 80), LookaheadMeasure.Measure(entry, incoming));
        entry.Arrange(new Rect(0, 0, 100, 80));
        Assert.Equal(new Size(90, 74), entry.Bounds.Size);
        Assert.Equal(new Size(90, 74), entry.CurrentBounds!.Value.Size);
        Assert.Equal(new Size(100, 80), Lookahead.GetSnapshot(entry)!.Results[entry].DesiredSize);
        var parent = new Grid { Children = { entry } };
        LookaheadMeasure.Measure(entry, incoming);
        entry.Arrange(new Rect(0, 0, 100, 80));
        Assert.Equal(new Size(90, 74), entry.CurrentBounds!.Value.Size);
    });

    [Fact]
    public Task MeasureDoesNotPublishTarget_DirectChildRemeasureIsSafe_AndArrangeCommitsPosition() => OnUi(() =>
    {
        var entry = Entry();
        var parent = new Grid { Children = { entry } };
        entry.Measure(new Size(400, 300));
        Assert.Equal(new Size(120, 60), entry.DesiredSize);
        Assert.Null(Lookahead.GetSnapshot(entry));
        Assert.Null(entry.TargetBounds);
        entry.Child!.InvalidateMeasure();
        entry.Child.Measure(new Size(120, 60));
        Assert.Null(Lookahead.GetSnapshot(entry.Child));
        Assert.False(entry.IsRunning);

        entry.Arrange(new Rect(30, 40, 400, 300));
        var first = Lookahead.GetSnapshot(entry)!;
        Assert.NotNull(first);
        Assert.Equal(new Rect(30, 40, 120, 60), first.Results[entry].Bounds);
        Assert.Equal(first.Results[entry].Bounds, entry.TargetBounds);
        Assert.Equal(entry.TargetBounds, entry.CurrentBounds);
        Assert.Same(parent, entry.Parent);

        entry.TargetSize = new Size(200, 90);
        entry.HorizontalAlignment = HorizontalAlignment.Right;
        Assert.Null(entry.TargetBounds);
        entry.Measure(new Size(500, 300));
        entry.Measure(new Size(400, 300));
        Assert.Null(Lookahead.GetSnapshot(entry));
        Assert.Equal(new Rect(30, 40, 120, 60), first.Results[entry].Bounds);
        entry.Child.InvalidateMeasure();
        entry.Child.Measure(new Size(200, 90));
        entry.Arrange(new Rect(10, 20, 400, 300));
        Assert.Equal(new Rect(210, 20, 200, 90), entry.TargetBounds);
        Assert.NotSame(first, Lookahead.GetSnapshot(entry));
    });

    [Fact]
    public Task ReparentBeforeVisualAttachment_ReleasesOwnedDomainAndItsCoordinateReference() => OnUi(() =>
    {
        var entry = Entry();
        var firstParent = new Grid { Children = { entry } };
        entry.Measure(new Size(400, 300));
        entry.Arrange(new Rect(30, 40, 400, 300));
        var firstContext = LookaheadEnvironment.GetContext(entry);
        firstParent.Children.Remove(entry);
        Assert.Null(LookaheadEnvironment.GetContext(entry));
        Assert.Null(entry.TargetBounds);
        var secondParent = new Grid { Children = { entry } };
        entry.Measure(new Size(300, 200));
        entry.Arrange(new Rect(5, 8, 300, 200));
        Assert.NotSame(firstContext, LookaheadEnvironment.GetContext(entry));
        Assert.Equal(new Rect(5, 8, 120, 60), entry.CurrentBounds);
        Assert.Same(secondParent, LookaheadEnvironment.GetContext(entry)!.CoordinateReference);
    });

    [Fact]
    public Task ExistingLocalValue_DoesNotHideNewCooperativeAncestorDomain() => OnUi(() =>
    {
        var entry = Entry();
        var box = new LookaheadBox { Children = { entry } };
        entry.Measure(new Size(400, 300));
        entry.Arrange(new Rect(0, 0, 400, 300));
        var local = LookaheadEnvironment.GetContext(entry);
        var root = new LookaheadRoot { Child = box };
        root.Measure(new Size(400, 300));
        root.Arrange(new Rect(0, 0, 400, 300));
        Assert.NotSame(local, LookaheadEnvironment.GetContext(entry));
        Assert.Same(root.Context, LookaheadEnvironment.GetContext(entry));
        Assert.Same(root.Snapshot, Lookahead.GetSnapshot(entry));
    });
}


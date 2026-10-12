using ComposeAnimation.Test.Probes;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadTextTest
{
    private const string Paragraph = "The quick brown fox jumps over the lazy dog. The same text wraps differently while the width changes, and the target remains independent.";
    private static T Text<T>() where T : TextBlock, new() => new()
    {
        Text = Paragraph, FontSize = 16, TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false
    };

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control child, params Control[] animations)
        {

            Root = new LookaheadRoot { Width = 350, Height = 500, Child = child };
            Window = new AnimationTestWindow(Frames) { Width = 600, Height = 600, Content = Root };
            Window.Show();
            Layout();
        }
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Step(int ms) { Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() { Window.Close(); }
    }

    [Theory]
    [InlineData(TextAlignment.Left, 0)]
    [InlineData(TextAlignment.Center, 7)]
    [InlineData(TextAlignment.Right, -2)]
    public Task IndependentFirstMeasure_MatchesTextBlockWithoutTouchingActualState(TextAlignment alignment, double spacing) => OnUi(() =>
    {
        var text = Text<CountingText>();
        var reference = Text<TextBlock>();
        foreach (var t in new TextBlock[] { text, reference })
        {
            t.TextAlignment = alignment;
            t.Padding = new Thickness(3, 5);
            t.LineSpacing = spacing;
            t.LineHeight = 24;
            t.LetterSpacing = 0.7;
            t.FlowDirection = FlowDirection.RightToLeft;
        }
        var available = new Size(190, double.PositiveInfinity);
        var builder = new LookaheadLayoutBuilder();
        var predicted = builder.Measure(text, available);
        reference.Measure(available);
        Assert.Equal(reference.DesiredSize.Width, predicted.Width, 5);
        Assert.Equal(reference.DesiredSize.Height, predicted.Height, 5);
        Assert.Equal(0, text.Measures);
        Assert.Equal(0, text.Arrangements);
        Assert.Equal(default, text.DesiredSize);
        Assert.Equal(default, text.Bounds);

        text.Measure(new Size(320, double.PositiveInfinity));
        text.Arrange(new Rect(text.DesiredSize));
        var actual = text.TextLayout;
        var bounds = text.Bounds;
        var measures = text.Measures;
        builder = new LookaheadLayoutBuilder();
        Assert.Equal(predicted, builder.Measure(text, available));
        Assert.Same(actual, text.TextLayout);
        Assert.Equal(bounds, text.Bounds);
        Assert.Equal(measures, text.Measures);
    });

    [Fact]
    public Task TextInputs_NotifyRootEvenWhenLeafAlreadyInvalid_AndUnsubscribeOnRemoval() => OnUi(() =>
    {
        var text = Text<LookaheadTextBlock>();
        var stack = new LookaheadStackPanel { Children = { text } };
        using var f = new Fixture(stack);
        var original = f.Root.Snapshot!;
        text.InvalidateMeasure(); // Actual-only leaf invalidation need not invalidate its ancestors.
        Assert.True(stack.IsMeasureValid);
        Assert.True(f.Root.IsMeasureValid);
        text.Text = "New content";
        Assert.Null(f.Root.Snapshot);
        Assert.False(f.Root.IsMeasureValid);
        text.FontSize = 30; // Must still notify while measure-invalid.
        text.Text = Paragraph + Paragraph;
        f.Layout();
        Assert.True(f.Root.Snapshot!.Version > original.Version);
        Assert.True(f.Root.Snapshot.Results[text].MeasuredSize.Height > original.Results[text].MeasuredSize.Height);
        var changed = f.Root.Snapshot;
        text.InvalidateMeasure();
        f.Layout();
        Assert.Same(changed, f.Root.Snapshot);
        text.InvalidateTargetMeasure();
        Assert.Null(f.Root.Snapshot);
        f.Layout();
        Assert.NotSame(changed, f.Root.Snapshot);

        stack.Children.Remove(text);
        f.Layout();
        var removed = f.Root.Snapshot;
        text.InvalidateTargetMeasure();
        Assert.Same(removed, f.Root.Snapshot);
        stack.Children.Add(text);
        f.Layout();
        f.Window.Content = null;
        Assert.Null(f.Root.Snapshot);
        text.Text = "Reattached";
        f.Window.Content = f.Root;
        f.Layout();
        Assert.Equal(text.DesiredSize.Height, f.Root.Snapshot!.Results[text].DesiredSize.Height, 5);
    });

    [Fact]
    public Task AnimatedContentSize_UsesAutoTextTargetAndKeepsTargetWrappingDuringFlight() => OnUi(() =>
    {
        var text = Text<LookaheadTextBlock>();
        text.MaxWidth = 160;
        var animation = new AnimatedContentSize
        {
            Content = text, HorizontalAlignment = HorizontalAlignment.Left,
            AnimationSpec = new TweenSpec<Size>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing())
        };
        var marker = new Border { Width = 20, Height = 10 };
        using var f = new Fixture(new LookaheadStackPanel { Children = { animation, marker } }, animation);
        var originalHeight = animation.Bounds.Height;
        text.MaxWidth = 320;
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        var target = snapshot.Results[animation].MeasuredSize;
        var lines = text.TextLayout.TextLines.Count;
        Assert.True(target.Height < originalHeight);
        Assert.Equal(snapshot.Results[animation].DesiredSize.Height, snapshot.Results[marker].Bounds.Y, 5);
        f.Step(16);
        f.Step(50);
        Assert.InRange(animation.Bounds.Height, target.Height + 1, originalHeight - 1);
        Assert.Equal(lines, text.TextLayout.TextLines.Count);
        Assert.Equal(target.Height, text.DesiredSize.Height, 5);
        Assert.Same(snapshot, f.Root.Snapshot);
        text.Text = Paragraph + Paragraph;
        f.Layout();
        var redirected = f.Root.Snapshot!;
        Assert.NotSame(snapshot, redirected);
        f.Step(100);
        Assert.Equal(redirected.Results[animation].DesiredSize.Height, animation.Bounds.Height, 5);
        Assert.Same(redirected, f.Root.Snapshot);
    });

    [Fact]
    public Task WidthApproach_RemeasuresRealText_WithoutChangingTarget_AndRetargets() => OnUi(() =>
    {
        var text = Text<CountingText>();
        var approach = new TextWidthApproach
        {
            Child = text, TargetWidth = 160, HorizontalAlignment = HorizontalAlignment.Left,
            AnimationSpec = new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing())
        };
        var marker = new Border { Width = 20, Height = 10 };
        using var f = new Fixture(new LookaheadStackPanel { Children = { approach, marker } }, approach);
        var initialLines = text.TextLayout.TextLines.Count;
        approach.TargetWidth = 320;
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        var targetHeight = snapshot.Results[approach].DesiredSize.Height;
        Assert.Equal(320, snapshot.Results[text].MeasureConstraints.Width);
        Assert.Equal(160, text.Bounds.Width);
        var measures = text.Measures;
        f.Step(16);
        f.Step(50);
        Assert.Equal(240, text.Bounds.Width, 5);
        Assert.True(text.Measures > measures);
        Assert.True(text.TextLayout.TextLines.Count < initialLines);
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.Equal(targetHeight, snapshot.Results[marker].Bounds.Y, 5);

        approach.TargetWidth = 200;
        f.Layout();
        var redirected = f.Root.Snapshot!;
        Assert.Equal(200, redirected.Results[text].MeasureConstraints.Width);
        Assert.Equal(240, text.Bounds.Width, 5);
        f.Step(50);
        Assert.Equal(220, text.Bounds.Width, 5);
        f.Step(50);
        Assert.Equal(200, text.Bounds.Width, 5);
        Assert.Equal(marker.Bounds.Y, redirected.Results[marker].Bounds.Y, 5);
        Assert.Same(redirected, f.Root.Snapshot);
        approach.TargetWidth = 300;
        f.Layout();
        f.Window.Content = null;
        Assert.Equal(0, f.Frames.PendingFrameCount);
        f.Window.Content = f.Root;
        f.Layout();
        Assert.Equal(300, text.Bounds.Width);
    });

    [Fact]
    public Task InlineInsertion_IsRejectedAfterInitialLayoutInsteadOfUsingStaleRuns() => OnUi(() =>
    {
        var text = Text<LookaheadTextBlock>();
        text.Inlines = new InlineCollection();
        using var f = new Fixture(new LookaheadStackPanel { Children = { text } });
        text.Inlines.Add(new Run("Unsupported"));
        Assert.Null(f.Root.Snapshot);
        // Assert via the pure builder, avoiding a failed dispatcher layout operation.
        Assert.Throws<NotSupportedException>(() => new LookaheadLayoutBuilder().Measure(text, new Size(300, 300)));
        text.Inlines.Clear();
        f.Layout();
    });

    private sealed class CountingText : LookaheadTextBlock
    {
        public int Measures { get; private set; }
        public int Arrangements { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return base.MeasureOverride(availableSize); }
        protected override Size ArrangeOverride(Size finalSize) { Arrangements++; return base.ArrangeOverride(finalSize); }
    }
}

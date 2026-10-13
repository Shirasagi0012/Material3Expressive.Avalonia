using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadLocalEntryTest
{

    private static BoundsTransform Tween(int milliseconds = 100) => new()
    {
        Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(milliseconds), easing: new LinearEasing())
    };

    private static AnimatedBounds Bounds(Control child, Size? target = null) => new()
    {
        Child = child,
        TargetSize = target,
        BoundsTransform = Tween(),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false
    };

    private sealed class CountingGrid : Grid
    {
        public int Measures { get; private set; }

        protected override Size MeasureOverride(Size constraint)
        {
            Measures++;
            return base.MeasureOverride(constraint);
        }
    }

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public Window Window { get; }

        public Fixture(Control content, params AnimatedBounds[] animations)
        {

            Window = new AnimationTestWindow(Frames) { Width = 420, Height = 320, Content = content };
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

        public void Step(int milliseconds)
        {
            Frames.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds));
            Layout();
        }

        public void Dispose() => Window.Close();
    }

    [Fact]
    public Task FirstLayout_InitializesAtCommittedTargetWithoutAnimation() => OnUi(() =>
    {
        var text = new LookaheadTextBlock { Text = "Local entry", FontSize = 16 };
        var card = Bounds(text, new Size(120, 60));
        var grid = new CountingGrid
        {
            Width = 320,
            Height = 220,
            Children = { card }
        };

        using var f = new Fixture(grid, card);

        var snapshot = Lookahead.GetSnapshot(card);
        Assert.NotNull(snapshot);
        Assert.Same(snapshot, Lookahead.GetSnapshot(text));
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Frames.PendingFrameCount);
        Assert.Equal(card.TargetBounds, card.CurrentBounds);
        Assert.Equal(card.Bounds, card.TargetBounds);
        Assert.Equal(new Size(120, 60), card.Bounds.Size);
        Assert.Equal(card.CurrentBounds!.Value.TopLeft, text.TranslatePoint(default, grid)!.Value);
    });

    [Fact]
    public Task SizeAndAlignmentChange_AnimatesCompleteRectWithoutRelayoutFeedback() => OnUi(() =>
    {
        var text = new LookaheadTextBlock
        {
            Text = "The real subtree is measured at the current animated size.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };
        var card = Bounds(text, new Size(80, 40));
        card.Margin = new Thickness(10, 12, 14, 16);
        var grid = new CountingGrid
        {
            Width = 320,
            Height = 240,
            Children = { card }
        };
        using var f = new Fixture(grid, card);
        var initial = card.CurrentBounds!.Value;

        card.TargetSize = new Size(160, 90);
        card.HorizontalAlignment = HorizontalAlignment.Right;
        card.VerticalAlignment = VerticalAlignment.Bottom;
        f.Layout();

        var target = card.TargetBounds!.Value;
        var snapshot = Lookahead.GetSnapshot(card)!;
        Assert.NotEqual(initial.TopLeft, target.TopLeft);
        Assert.NotEqual(initial.Size, target.Size);
        Assert.Equal(card.Bounds, target);
        Assert.Equal(initial, card.CurrentBounds);
        Assert.True(card.IsRunning);
        var parentMeasures = grid.Measures;

        f.Step(16);
        f.Step(50);

        var sample = card.CurrentBounds!.Value;
        Assert.InRange(sample.X, Math.Min(initial.X, target.X), Math.Max(initial.X, target.X));
        Assert.InRange(sample.Y, Math.Min(initial.Y, target.Y), Math.Max(initial.Y, target.Y));
        Assert.InRange(sample.Width, initial.Width + 0.01, target.Width - 0.01);
        Assert.InRange(sample.Height, initial.Height + 0.01, target.Height - 0.01);
        Assert.Equal(target, card.Bounds);
        var textPosition = text.TranslatePoint(default, grid)!.Value;
        Assert.Equal(sample.X, textPosition.X, 5);
        Assert.Equal(sample.Y, textPosition.Y, 5);
        Assert.Equal(sample.Width, text.Bounds.Width, 5);
        Assert.Equal(sample.Height, text.Bounds.Height, 5);
        Assert.Equal(parentMeasures, grid.Measures);
        Assert.Same(snapshot, Lookahead.GetSnapshot(card));

        f.Step(50);
        Assert.Equal(target, card.CurrentBounds);
        Assert.Equal(target, card.Bounds);
        Assert.False(card.IsRunning);
        Assert.Equal(parentMeasures, grid.Measures);
        Assert.Same(snapshot, Lookahead.GetSnapshot(card));
    });

    [Fact]
    public Task UnboundedStackPanelMeasure_UsesFiniteNaturalSize() => OnUi(() =>
    {
        var text = new LookaheadTextBlock
        {
            Text = "Natural local lookahead size",
            FontSize = 18,
            TextWrapping = TextWrapping.NoWrap
        };
        var card = Bounds(text);
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { card, new Border { Width = 20, Height = 10 } }
        };
        using var f = new Fixture(stack, card);

        Assert.NotNull(Lookahead.GetSnapshot(card));
        Assert.True(double.IsFinite(card.TargetBounds!.Value.Width));
        Assert.True(double.IsFinite(card.TargetBounds.Value.Height));
        Assert.True(card.TargetBounds.Value.Width > 0);
        Assert.True(card.TargetBounds.Value.Height > 0);
        Assert.Equal(card.TargetBounds, card.CurrentBounds);
        Assert.Equal(card.TargetBounds.Value.Size, text.Bounds.Size);
        Assert.False(card.IsRunning);
    });

    [Fact]
    public Task BoundaryContent_AutomaticEntryOwnsIndependentDomain() => OnUi(() =>
    {
        var text = new LookaheadTextBlock { Text = "Boundary local entry", FontSize = 16 };
        var card = Bounds(text, new Size(80, 40));
        var ordinary = new Grid { Children = { card } };
        var boundary = new LookaheadBoundary { Child = ordinary };
        var outerRoot = new LookaheadRoot
        {
            Width = 300,
            Height = 200,
            Child = new LookaheadBox { Children = { boundary } },
            UseLayoutRounding = false
        };
        using var f = new Fixture(outerRoot, card);

        var outerSnapshot = outerRoot.Snapshot!;
        var localSnapshot = Lookahead.GetSnapshot(card)!;
        Assert.NotSame(outerSnapshot, localSnapshot);
        Assert.True(outerSnapshot.Results.ContainsKey(boundary));
        Assert.False(outerSnapshot.Results.ContainsKey(card));
        Assert.Null(Lookahead.GetSnapshot(ordinary));
        Assert.Same(localSnapshot, Lookahead.GetSnapshot(text));

        card.TargetSize = new Size(160, 90);
        f.Layout();
        outerSnapshot = outerRoot.Snapshot!;
        localSnapshot = Lookahead.GetSnapshot(card)!;
        f.Step(16);
        f.Step(50);

        Assert.True(card.IsRunning);
        Assert.Same(outerSnapshot, outerRoot.Snapshot);
        Assert.Same(localSnapshot, Lookahead.GetSnapshot(card));
        Assert.Equal(card.CurrentBounds!.Value.Width, text.Bounds.Width, 5);
        Assert.Equal(card.CurrentBounds.Value.Height, text.Bounds.Height, 5);
    });

    [Fact]
    public Task FractionalMarginRoundingAndStretch_KeepNativeSlotStableWhileContentResizes() => OnUi(() =>
    {
        var text = new LookaheadTextBlock { Text = "Stretch", FontSize = 14 };
        var card = Bounds(text, new Size(80, 40));
        card.HorizontalAlignment = HorizontalAlignment.Stretch;
        card.VerticalAlignment = VerticalAlignment.Stretch;
        card.Margin = new Thickness(3.5, 4.5, 5.5, 6.5);
        card.UseLayoutRounding = true;
        var grid = new CountingGrid
        {
            Width = 301,
            Height = 201,
            UseLayoutRounding = false,
            Children = { card }
        };
        using var f = new Fixture(grid, card);
        var nativeSlot = card.Bounds;
        var initialChildSize = text.Bounds.Size;

        Assert.Equal(nativeSlot, card.TargetBounds);
        Assert.Equal(nativeSlot, card.CurrentBounds);
        Assert.Equal(Math.Round(nativeSlot.X), nativeSlot.X);
        Assert.Equal(Math.Round(nativeSlot.Y), nativeSlot.Y);

        card.TargetSize = new Size(160, 100);
        f.Layout();
        var snapshot = Lookahead.GetSnapshot(card)!;
        var parentMeasures = grid.Measures;
        Assert.Equal(nativeSlot, card.Bounds);
        Assert.Equal(nativeSlot, card.TargetBounds);

        f.Step(16);
        f.Step(50);
        Assert.Equal(nativeSlot, card.Bounds);
        Assert.Equal(nativeSlot, card.CurrentBounds);
        Assert.True(text.Bounds.Width > initialChildSize.Width);
        Assert.True(text.Bounds.Height > initialChildSize.Height);
        Assert.InRange(text.Bounds.Width, initialChildSize.Width + 0.01, 159.99);
        Assert.InRange(text.Bounds.Height, initialChildSize.Height + 0.01, 99.99);
        Assert.Equal(parentMeasures, grid.Measures);
        Assert.Same(snapshot, Lookahead.GetSnapshot(card));
    });
}

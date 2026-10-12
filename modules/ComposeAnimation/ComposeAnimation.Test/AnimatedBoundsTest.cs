using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public partial class AnimatedBoundsTest
{
    private static Task OnUi(Action action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);
    private static BoundsTransform Tween(int ms = 200) => new()
    {
        Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing())
    };

    private sealed class RecordingTransform : BoundsTransform
    {
        public List<(Rect From, Rect To)> Calls { get; } = [];
        public override IFiniteAnimationSpec<Rect> Create(Rect initialBounds, Rect targetBounds)
        {
            Calls.Add((initialBounds, targetBounds));
            return base.Create(initialBounds, targetBounds);
        }
    }

    private sealed class CallbackTransform(Func<Rect, Rect, IFiniteAnimationSpec<Rect>> create) : BoundsTransform
    {
        public override IFiniteAnimationSpec<Rect> Create(Rect initialBounds, Rect targetBounds)
            => create(initialBounds, targetBounds);
    }

    private sealed class BrokenClock : IFrameSource
    {
        public int Requests { get; private set; }
        public void VerifyAccess() { }
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
        {
            Requests++;
            return ValueTask.FromException<TimeSpan>(new InvalidOperationException("clock failure"));
        }
    }

    private sealed class MeasuredText : LookaheadTextBlock
    {
        public Size LastConstraints { get; private set; }
        public int MeasureCount { get; private set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            MeasureCount++;
            LastConstraints = availableSize;
            return base.MeasureOverride(availableSize);
        }
    }

    private static MeasuredText Text() => new()
    {
        Text = string.Join(" ", Enumerable.Repeat("Real content is measured while its rectangle moves.", 5)),
        FontSize = 16, TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false
    };

    private static AnimatedBounds Bounds(Control child) => new()
    {
        Child = child, TargetSize = new Size(120, 160), BoundsTransform = Tween(),
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false
    };

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Clock { get; } = new();
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control content, BoundsParentLayoutMode mode = BoundsParentLayoutMode.AnimatedSize)
        {
            SetClock(content);
            Root = new LookaheadRoot { Width = 400, Height = 300, Child = content, UseLayoutRounding = false };
            Window = new AnimationTestWindow(Clock) { Width = 500, Height = 400, Content = Root };
            Window.Show(); Layout();
            void SetClock(Control node)
            {

                // These legacy trajectory/sibling tests assert the size and position reported
                // to a cooperative parent. TargetSize presentation is covered independently.
                if (node is AnimatedBounds bounds) bounds.ParentLayoutMode = mode;
                if (node is ILookaheadLayoutNode policy)
                    foreach (var child in policy.LookaheadChildren) SetClock(child);
            }
        }
        public Rect Actual(Control node) => new(node.TranslatePoint(default, Root)!.Value, node.Bounds.Size);
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Step(int ms) { Clock.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() { Window.Close(); }
    }

    private static void Equal(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 5);
        Assert.Equal(expected.Y, actual.Y, 5);
        Assert.Equal(expected.Width, actual.Width, 5);
        Assert.Equal(expected.Height, actual.Height, 5);
    }

    [Fact]
    public Task SimultaneousSizeAndAlignment_UsesCompleteRectAndLiveText_WithoutTargetFeedback() => OnUi(() =>
    {
        var text = Text();
        var bounds = Bounds(text);
        var transform = new RecordingTransform { Spec = Tween().Spec };
        bounds.BoundsTransform = transform;
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        Assert.Empty(transform.Calls);
        var oldLines = text.TextLayout.TextLines.Count;
        bounds.TargetSize = new Size(240, 100);
        bounds.HorizontalAlignment = HorizontalAlignment.Right;
        bounds.VerticalAlignment = VerticalAlignment.Bottom;
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        Assert.Equal(2, transform.Calls.Count);
        Assert.All(transform.Calls, call =>
        {
            Equal(new Rect(0, 0, 120, 160), call.From);
            Equal(new Rect(160, 200, 240, 100), call.To);
        });
        Equal(new Rect(0, 0, 120, 160), f.Actual(text));
        f.Step(16); f.Step(100);
        Equal(new Rect(80, 100, 180, 130), f.Actual(bounds));
        Equal(f.Actual(bounds), f.Actual(text));
        Assert.Equal(new Size(180, 130), text.LastConstraints);
        Assert.True(text.TextLayout.TextLines.Count < oldLines);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
        Assert.False(bounds.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        bounds.InvalidateMeasure(); f.Layout();
        Assert.Equal(2, transform.Calls.Count);
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.Null(bounds.RenderTransform);
        Assert.False(bounds.ClipToBounds);
    });

    [Fact]
    public Task PaddingBetweenBoundaries_AnimatesContentAndSiblingLayout() => OnUi(() =>
    {
        var child = Text();
        child.Text = "Short text";
        var bounds = Bounds(child);
        bounds.TargetSize = null;
        bounds.Padding = new Thickness(10);
        var sibling = new Border { Width = 10, Height = 10 };
        using var f = new Fixture(new LookaheadStackPanel { Spacing = 10, Children = { bounds, sibling } });
        var from = f.Actual(bounds);
        var childFrom = f.Actual(child);
        var siblingFrom = f.Actual(sibling);
        bounds.Padding = new Thickness(30);
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        Equal(childFrom, f.Actual(child));
        f.Step(16); f.Step(100);
        Assert.Equal(from.Height + 20, bounds.Bounds.Height, 5);
        Equal(new Rect(childFrom.X + 10, childFrom.Y + 10, childFrom.Width, childFrom.Height), f.Actual(child));
        Assert.Equal(siblingFrom.Y + 20, f.Actual(sibling).Y, 5);
        f.Step(100);
        Equal(snapshot.Results[child].Bounds, f.Actual(child));
        Equal(snapshot.Results[sibling].Bounds, f.Actual(sibling));
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task Retarget_UsesCurrentRectangle_AndPolicyReplacementDoesNotRetimeActiveRun() => OnUi(() =>
    {
        var bounds = Bounds(Text());
        var transform = new RecordingTransform { Spec = Tween().Spec };
        bounds.BoundsTransform = transform;
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        bounds.TargetSize = new Size(240, 100);
        bounds.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout(); f.Step(16); f.Step(50);
        var before = f.Actual(bounds);
        var replacement = new RecordingTransform { Spec = Tween(400).Spec };
        bounds.BoundsTransform = replacement;
        f.Layout();
        Assert.Empty(replacement.Calls);
        bounds.TargetSize = new Size(180, 120);
        bounds.HorizontalAlignment = HorizontalAlignment.Center;
        f.Layout();
        Equal(before, f.Actual(bounds));
        Assert.Equal(2, replacement.Calls.Count);
        Assert.All(replacement.Calls, call => Equal(before, call.From));
        var target = f.Root.Snapshot!.Results[bounds].Bounds;
        f.Step(100);
        Equal(new Rect(before.X + (target.X - before.X) / 4, before.Y + (target.Y - before.Y) / 4,
            before.Width + (target.Width - before.Width) / 4, before.Height + (target.Height - before.Height) / 4), f.Actual(bounds));
        f.Step(300);
        Equal(target, f.Actual(bounds));
    });

    [Theory]
    [InlineData(100, 200)]
    [InlineData(200, 100)]
    public Task NestedBounds_KeepCompensatingWhenOneRunFinishes(int outerMs, int innerMs) => OnUi(() =>
    {
        var child = Bounds(Text());
        child.TargetSize = new Size(60, 70);
        child.BoundsTransform = Tween(innerMs);
        child.HorizontalAlignment = HorizontalAlignment.Right;
        child.VerticalAlignment = VerticalAlignment.Bottom;
        var parent = Bounds(new LookaheadBox { Children = { child } });
        parent.TargetSize = new Size(180, 160);
        parent.BoundsTransform = Tween(outerMs);
        using var f = new Fixture(new LookaheadBox { Children = { parent } });
        var start = f.Actual(child);
        parent.TargetSize = new Size(280, 220);
        parent.HorizontalAlignment = HorizontalAlignment.Right;
        parent.VerticalAlignment = VerticalAlignment.Bottom;
        f.Layout();
        var target = f.Root.Snapshot!.Results[child].Bounds;
        f.Step(16); f.Step(150);
        var fraction = Math.Min(1, 150.0 / innerMs);
        Equal(new Rect(start.X + (target.X - start.X) * fraction,
            start.Y + (target.Y - start.Y) * fraction, start.Width, start.Height), f.Actual(child));
        f.Step(50);
        Equal(target, f.Actual(child));
        Assert.False(parent.IsRunning);
        Assert.False(child.IsRunning);
    });

    [Fact]
    public Task NaturalContentChange_AndViewportResize_RebuildDestinationOnlyForInputs() => OnUi(() =>
    {
        var text = Text();
        text.Text = "Small";
        var bounds = Bounds(text);
        bounds.TargetSize = null;
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        var start = f.Actual(bounds);
        text.Text = string.Join(" ", Enumerable.Repeat("Growing natural text", 12));
        f.Layout();
        var target = f.Root.Snapshot!;
        Equal(start, f.Actual(bounds));
        f.Step(16); f.Step(100);
        Assert.True(bounds.Bounds.Width > start.Width);
        Assert.Same(target, f.Root.Snapshot);
        f.Root.Width = 250; f.Layout();
        var resized = f.Root.Snapshot!;
        Assert.NotSame(target, resized);
        f.Step(200);
        Equal(resized.Results[bounds].Bounds, f.Actual(bounds));
        Equal(resized.Results[text].Bounds, f.Actual(text));
    });


    [Fact]
    public Task LocalEntryWithoutClock_BoundaryRetainsHardNativeChildSize() => OnUi(() =>
    {
        var child = new Border { Width = 300, Height = 200, UseLayoutRounding = false };
        var bounds = Bounds(new LookaheadBoundary { Child = child, UseLayoutRounding = false });
        bounds.Measure(new Size(400, 300)); bounds.Arrange(new Rect(0, 0, 400, 300));
        Assert.False(bounds.IsRunning);
        Assert.Equal(new Size(120, 160), bounds.DesiredSize);
        Assert.Equal(new Size(300, 200), child.Bounds.Size);
        bounds.TargetSize = new Size(240, 100);
        bounds.Measure(new Size(400, 300)); bounds.Arrange(new Rect(0, 0, 400, 300));
        Assert.Equal(new Size(240, 100), bounds.Bounds.Size);
        Assert.Equal(new Size(300, 200), child.Bounds.Size);
        Assert.False(bounds.IsRunning);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ZeroDurationOrScale_CompletesAndStopsRequestingFrames(bool zeroScale) => OnUi(() =>
    {
        var previousScale = MotionDurationScale.ScaleFactor;
        try
        {
            MotionDurationScale.ScaleFactor = zeroScale ? 0 : 1;
            var bounds = Bounds(Text());
            if (!zeroScale) bounds.BoundsTransform = Tween(0);
            using var f = new Fixture(new LookaheadBox { Children = { bounds } });
            bounds.TargetSize = new Size(240, 100);
            bounds.HorizontalAlignment = HorizontalAlignment.Right;
            f.Layout(); f.Step(16);
            Equal(f.Root.Snapshot!.Results[bounds].Bounds, f.Actual(bounds));
            Assert.False(bounds.IsRunning);
            Assert.Equal(0, f.Clock.PendingFrameCount);
        }
        finally { MotionDurationScale.ScaleFactor = previousScale; }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task InvalidPolicyOrNonFiniteSample_FallsBackAndCanRecover(bool nonFinite) => OnUi(() =>
    {
        var bounds = Bounds(Text());
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        bounds.BoundsTransform = new CallbackTransform((_, _) =>
        {
            if (!nonFinite) return null!;
            var config = new KeyframesSpecConfig<Rect> { Duration = TimeSpan.FromSeconds(1) };
            config.At(0, new Rect(double.NaN, 0, 100, 100));
            return new KeyframesSpec<Rect>(config);
        });
        bounds.TargetSize = new Size(240, 100); f.Layout(); f.Step(16);
        Assert.NotNull(((AnimationTestWindow)TopLevel.GetTopLevel(bounds)!).AnimationRoot.Diagnostics.LastError);
        Equal(f.Root.Snapshot!.Results[bounds].Bounds, f.Actual(bounds));
        Assert.False(bounds.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        bounds.InvalidateMeasure(); f.Layout();
        Assert.Equal(0, f.Clock.PendingFrameCount);
        bounds.BoundsTransform = Tween();
        bounds.TargetSize = new Size(120, 160); f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(180, bounds.Bounds.Width, 5);
    });


    [Fact]
    public Task PolicyReset_DoesNotStartInnerBoundaryOnDisposedClock() => OnUi(() =>
    {
        var bounds = Bounds(Text());
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        bounds.BoundsTransform = new CallbackTransform((_, _) =>
        {
            bounds.ResetAnimation();
            return Tween().Spec;
        });
        bounds.TargetSize = new Size(240, 100); f.Layout();
        Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(bounds)!).AnimationRoot.Diagnostics.LastError);
        Assert.False(bounds.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        Equal(f.Root.Snapshot!.Results[bounds].Bounds, f.Actual(bounds));
    });

    [Fact]
    public Task ChildReplacementAndRootChange_RemoveOldInputsAndResetCoordinateSpace() => OnUi(() =>
    {
        var old = Text();
        var bounds = Bounds(old);
        var box = new LookaheadBox { Children = { bounds } };
        using var f = new Fixture(box);
        bounds.TargetSize = new Size(240, 100); f.Layout(); f.Step(16); f.Step(50);
        var before = f.Actual(bounds);
        var replacement = Text();
        bounds.Child = replacement; f.Layout();
        Equal(before, f.Actual(bounds));
        var snapshot = f.Root.Snapshot!;
        Assert.False(snapshot.Results.ContainsKey(old));
        old.Text = "Removed"; f.Layout();
        Assert.Same(snapshot, f.Root.Snapshot);
        box.Children.Remove(bounds); f.Layout();
        using var other = new Fixture(new LookaheadBox { Children = { bounds } });
        Equal(other.Root.Snapshot!.Results[bounds].Bounds, other.Actual(bounds));
        Assert.False(bounds.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task ArcPolicy_DrivesTheActualRectangleAlongBothCornerPaths() => OnUi(() =>
    {
        var text = Text();
        var bounds = Bounds(text);
        bounds.BoundsTransform = new BoundsTransform
        {
            Spec = new ArcAnimationSpec<Rect>(ArcMode.ArcAbove, TimeSpan.FromMilliseconds(200),
                easing: new LinearEasing())
        };
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        var initial = f.Actual(bounds);
        bounds.TargetSize = new Size(240, 100);
        bounds.HorizontalAlignment = HorizontalAlignment.Right;
        bounds.VerticalAlignment = VerticalAlignment.Bottom;
        f.Layout();
        var target = f.Root.Snapshot!.Results[bounds].Bounds;
        var reference = new TargetBasedAnimation<Rect>(bounds.BoundsTransform.Spec, new RectConverter(), initial, target);
        f.Step(16); f.Step(100);
        Equal(reference.GetValue(TimeSpan.FromMilliseconds(100)), f.Actual(bounds));
        Equal(f.Actual(bounds), f.Actual(text));
        Assert.NotEqual(80, bounds.Bounds.X);
        f.Step(100);
        Equal(target, f.Actual(text));
    });

    [Fact]
    public Task HardHostWidthClampsOuterBoundary_WhileInnerContentStillResizes() => OnUi(() =>
    {
        var text = Text();
        var bounds = Bounds(text);
        bounds.TargetSize = null;
        bounds.Width = 120;
        bounds.Height = 160;
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        bounds.Width = 240; f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(240, bounds.Bounds.Width); // Avalonia Width is a hard per-pass constraint.
        Assert.Equal(180, text.Bounds.Width, 5);
        Assert.Equal(180, text.LastConstraints.Width, 5);
        f.Step(100);
        Equal(f.Root.Snapshot!.Results[text].Bounds, f.Actual(text));
    });

    [Fact]
    public Task RoundingPaddingAndChildAlignment_ConvergeWithoutFinalJump() => OnUi(() =>
    {
        var text = Text();
        text.Text = "Small";
        text.UseLayoutRounding = true;
        text.HorizontalAlignment = HorizontalAlignment.Center;
        text.VerticalAlignment = VerticalAlignment.Bottom;
        text.Margin = new Thickness(1.5);
        var bounds = Bounds(text);
        bounds.UseLayoutRounding = true;
        bounds.Padding = new Thickness(2.5, 3.5);
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        bounds.Padding = new Thickness(10.5, 12.5);
        bounds.TargetSize = new Size(230.5, 150.5);
        bounds.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout(); f.Step(16); f.Step(200);
        var completed = f.Actual(text);
        Equal(f.Root.Snapshot!.Results[text].Bounds, completed);
        bounds.InvalidateMeasure(); f.Layout();
        Equal(completed, f.Actual(text));
        Assert.False(bounds.IsRunning);
    });

    [Fact]
    public Task FixedChildConstraints_OverrideHardSizesAndAlignment_InBothPasses() => OnUi(() =>
    {
        var text = Text();
        text.Width = 300;
        text.MinHeight = 220;
        text.HorizontalAlignment = HorizontalAlignment.Center;
        text.VerticalAlignment = VerticalAlignment.Bottom;
        var bounds = Bounds(text);
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        Equal(new Rect(0, 0, 120, 160), f.Actual(text));
        bounds.TargetSize = new Size(240, 100); f.Layout();
        var snapshot = f.Root.Snapshot!;
        Assert.Equal(LookaheadConstraints.Fixed(new Size(240, 100)), snapshot.Results[text].FullMeasureConstraints);
        f.Step(16); f.Step(100);
        Equal(new Rect(0, 0, 180, 130), f.Actual(text));
        Assert.Equal(new Size(180, 130), text.LastConstraints);
        Assert.Equal(LookaheadConstraints.Fixed(new Size(180, 130)), LookaheadMeasure.ActualConstraints(text));
        Assert.Equal(300, text.Width);
        Assert.Equal(220, text.MinHeight);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
    });

    [Fact]
    public Task DirectNestedBounds_ReceiveFullIncomingConstraints_AndKeepInnerMotion() => OnUi(() =>
    {
        var text = Text();
        var inner = Bounds(text);
        inner.Width = 300;
        inner.TargetSize = new Size(60, 70);
        inner.BoundsTransform = Tween(400);
        var outer = Bounds(inner);
        using var f = new Fixture(new LookaheadBox { Children = { outer } });
        Equal(new Rect(0, 0, 120, 160), f.Actual(inner));
        outer.TargetSize = new Size(240, 100); f.Layout();
        var snapshot = f.Root.Snapshot!;
        Assert.Equal(LookaheadConstraints.Fixed(new Size(240, 100)), snapshot.Results[inner].FullMeasureConstraints);
        f.Step(16); f.Step(100);
        Assert.Equal(180, inner.Bounds.Width, 5); // Incoming outer sample constrains the inner host.
        Assert.Equal(150, text.Bounds.Width, 5); // Its own inner boundary retains its slower animation.
        Assert.Equal(300, inner.Width);
        f.Step(300);
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
        Assert.False(inner.IsRunning);
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task RootRequiresFullChildProtocol_ContainersCanConsumeMinimaForTheirOwnChildren() => OnUi(() =>
    {
        var opaque = new Border { Width = 300, Height = 200 };
        var unsupported = Bounds(opaque);
        var builder = new LookaheadLayoutBuilder();
        Assert.Throws<NotSupportedException>(() => builder.Measure(unsupported, new Size(400, 300)));
        Assert.False(opaque.IsMeasureValid); // Rejection happened during the independent pass.

        unsupported.Child = null;
        var container = new LookaheadBox { Children = { opaque }, UseLayoutRounding = false };
        var bounds = Bounds(container);
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        Assert.Equal(new Size(120, 160), container.Bounds.Size);
        Assert.Equal(new Size(300, 200), opaque.Bounds.Size);
        bounds.TargetSize = new Size(240, 100); f.Layout(); f.Step(16); f.Step(100);
        Assert.Equal(new Size(180, 130), container.Bounds.Size);
        Assert.Equal(LookaheadConstraints.Fixed(new Size(180, 130)), LookaheadMeasure.ActualConstraints(container));
        Assert.Null(LookaheadMeasure.ActualConstraints(opaque));
        Assert.Null(f.Root.Snapshot!.Results[opaque].FullMeasureConstraints);
    });

    [Fact]
    public Task IntermediateSizePaddingAndOffset_AnimateDistinctBoundariesAndPreserveSnapshots() => OnUi(() =>
    {
        var text = Text();
        var bounds = Bounds(text);
        bounds.TargetSize = null;
        bounds.IntermediateLayout = BoundsLayout.FixedSize(new Size(140, 180))
            .Then(BoundsLayout.Padding(new Thickness(10))).Then(BoundsLayout.Offset(new Vector(5, 7)));
        var transform = new RecordingTransform { Spec = Tween().Spec };
        bounds.BoundsTransform = transform;
        var sibling = new Border { Width = 10, Height = 10 };
        using var f = new Fixture(new LookaheadStackPanel { Spacing = 10, Children = { bounds, sibling } });
        Equal(new Rect(0, 0, 140, 180), f.Actual(bounds));
        Equal(new Rect(15, 17, 120, 160), f.Actual(text));
        var oldSnapshot = f.Root.Snapshot!;
        var oldData = oldSnapshot.Results[bounds].LayoutData;
        bounds.IntermediateLayout = BoundsLayout.FixedSize(new Size(260, 140))
            .Then(BoundsLayout.Padding(new Thickness(20))).Then(BoundsLayout.Offset(new Vector(25, -3)));
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        Assert.Equal(2, transform.Calls.Count);
        Equal(new Rect(0, 0, 260, 140), transform.Calls[0].To);
        Equal(new Rect(45, 17, 220, 100), transform.Calls[1].To);
        f.Step(16); f.Step(100);
        Equal(new Rect(0, 0, 200, 160), f.Actual(bounds));
        Equal(new Rect(30, 17, 170, 130), f.Actual(text));
        Assert.Equal(170, f.Actual(sibling).Y);
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.Same(oldData, oldSnapshot.Results[bounds].LayoutData);
        Equal(new Rect(15, 17, 120, 160), oldSnapshot.Results[text].Bounds);
        f.Step(100);
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
        Equal(snapshot.Results[sibling].Bounds, f.Actual(sibling));
    });

    [Fact]
    public Task OffsetOnlyPolicy_MovesInnerBoundaryWithoutResizingOrMovingSibling() => OnUi(() =>
    {
        var text = Text();
        var bounds = Bounds(text);
        using var f = new Fixture(new LookaheadStackPanel
        {
            Children = { bounds, new Border { Width = 10, Height = 10 } }
        });
        var initial = f.Actual(bounds);
        bounds.IntermediateLayout = BoundsLayout.Offset(new Vector(60, 40)); f.Layout();
        var snapshot = f.Root.Snapshot!;
        f.Step(16); f.Step(100);
        Equal(initial, f.Actual(bounds));
        Equal(new Rect(30, 20, 120, 160), f.Actual(text));
        f.Step(100);
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    private sealed class RecordingLayout : BoundsLayout
    {
        public List<(LookaheadConstraints Incoming, Size Child)> Calls { get; } = [];
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
        {
            var child = measureChild(constraints.Deflate(new Thickness(10)));
            Calls.Add((constraints, child));
            return new(constraints.Constrain(child.Inflate(new Thickness(10))), new Point(10, 10));
        }
    }

    [Fact]
    public Task IntermediateActualPolicy_ReceivesOriginalConstraintsAndAnimatedInnerSize() => OnUi(() =>
    {
        var text = Text();
        var bounds = Bounds(text);
        bounds.TargetSize = null;
        text.Width = 100;
        text.Height = 80;
        var policy = new RecordingLayout();
        bounds.IntermediateLayout = policy;
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        var initialMeasureCount = text.MeasureCount;
        text.Width = 200;
        text.Height = 120;
        f.Layout(); f.Step(16);
        policy.Calls.Clear();
        var snapshot = f.Root.Snapshot!;
        f.Step(100);
        Assert.NotEmpty(policy.Calls);
        Assert.All(policy.Calls, call =>
        {
            Assert.Equal(LookaheadConstraints.AtMost(new Size(400, 300)), call.Incoming);
            Assert.Equal(new Size(150, 100), call.Child);
        });
        Assert.True(text.MeasureCount > initialMeasureCount);
        Equal(new Rect(10, 10, 150, 100), f.Actual(text));
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task PositionOnlyFrames_ReuseMeasurementButContinuePlacementThroughCompletion() => OnUi(() =>
    {
        var text = Text();
        var bounds = Bounds(text);
        var policy = new RecordingLayout();
        bounds.IntermediateLayout = policy;
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        var initial = f.Actual(text);
        bounds.HorizontalAlignment = HorizontalAlignment.Right;
        bounds.VerticalAlignment = VerticalAlignment.Bottom;
        f.Layout(); f.Step(16);
        policy.Calls.Clear();
        var count = text.MeasureCount;
        var snapshot = f.Root.Snapshot!;
        f.Step(100);
        Equal(new Rect(initial.X + 140, initial.Y + 70, initial.Width, initial.Height), f.Actual(text));
        Assert.Empty(policy.Calls);
        Assert.Equal(count, text.MeasureCount);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Assert.Empty(policy.Calls);
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
        Assert.False(bounds.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task IdleInnerBoundary_RestoresTargetConstraintsAfterPositionAnimation() => OnUi(() =>
    {
        var text = Text();
        text.Text = "Short";
        var bounds = Bounds(text);
        bounds.TargetSize = null;
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        var loose = LookaheadConstraints.AtMost(new Size(400, 300));
        Assert.Equal(loose, LookaheadMeasure.ActualConstraints(text));
        var size = text.Bounds.Size;
        bounds.HorizontalAlignment = HorizontalAlignment.Right; f.Layout(); f.Step(16);
        Assert.Equal(LookaheadConstraints.Fixed(size), LookaheadMeasure.ActualConstraints(text));
        var snapshot = f.Root.Snapshot!;
        f.Step(100);
        Assert.Equal(LookaheadConstraints.Fixed(size), LookaheadMeasure.ActualConstraints(text));
        f.Step(100);
        Assert.Equal(loose, LookaheadMeasure.ActualConstraints(text));
        Equal(snapshot.Results[text].Bounds, f.Actual(text));
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.False(bounds.IsRunning);
    });

    private sealed class RejectIntermediateWidth : BoundsLayout
    {
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
        {
            var child = measureChild(constraints);
            if (child.Width > 140 && child.Width < 160) throw new InvalidOperationException("Unsupported intermediate width");
            return new(constraints.Constrain(child), default);
        }
    }

    [Fact]
    public Task ActualLayoutFailure_StopsFramesAndCanRecoverAfterPolicyReplacement() => OnUi(() =>
    {
        var text = Text();
        text.Width = 100;
        text.Height = 80;
        var bounds = Bounds(text);
        bounds.TargetSize = null;
        bounds.IntermediateLayout = new RejectIntermediateWidth();
        using var f = new Fixture(new LookaheadBox { Children = { bounds } });
        text.Width = 200; f.Layout(); f.Step(16);
        Assert.Throws<InvalidOperationException>(() => f.Step(100));
        Assert.False(bounds.IsMeasureValid);
        Assert.False(bounds.IsRunning);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        bounds.IntermediateLayout = BoundsLayout.Empty; f.Layout();
        Equal(f.Root.Snapshot!.Results[text].Bounds, f.Actual(text));
        Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(bounds)!).AnimationRoot.Diagnostics.LastError); // The layout exception is propagated to the caller.
    });
}

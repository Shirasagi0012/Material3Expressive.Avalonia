using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ComposeAnimation.Test;

public class LookaheadConstraintProtocolTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private class Probe : Control, ILookaheadConstrainedLayoutNode
    {
        public int ActualMeasures { get; private set; }
        public int TargetMeasures { get; private set; }
        public Size LastMaximum { get; private set; }
        public LookaheadConstraints LastFull { get; private set; }
        public LookaheadConstraints LastTarget { get; private set; }
        public Size Natural { get; set; } = new(30, 20);
        public bool Throw { get; set; }
        public bool Bridge { get; set; } = true;
        public bool BadArrange { get; set; }
        public Probe()
        {
            UseLayoutRounding = false;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
        }
        public IEnumerable<Control> LookaheadChildren => [];
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, Size available) => Natural;
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        {
            TargetMeasures++;
            LastTarget = constraints;
            return Natural;
        }
        public void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds) { }
        public Size MeasureApproach(LookaheadConstraints constraints)
        {
            LastFull = constraints;
            return MeasureOverride(constraints.MaxSize);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            ActualMeasures++;
            LastMaximum = availableSize;
            if (Throw) throw new InvalidOperationException("Measure failure");
            return Natural;
        }
        protected override Size MeasureCore(Size availableSize)
            => Bridge && LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired) ? desired : base.MeasureCore(availableSize);
        protected override Size ArrangeOverride(Size finalSize) => BadArrange ? default : finalSize;
        protected override void ArrangeCore(Rect finalRect)
        {
            if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
            else base.ArrangeCore(finalRect);
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            LookaheadMeasure.Forget(this);
            base.OnDetachedFromVisualTree(e);
        }
    }

    // A consumer of the public protocol, not a production constraint widget. Null selects native layout.
    private sealed class ConstraintHost : Decorator, ILookaheadLayoutNode, ILookaheadPlacementHost
    {
        public static readonly StyledProperty<LookaheadConstraints?> ConstraintsProperty =
            AvaloniaProperty.Register<ConstraintHost, LookaheadConstraints?>(nameof(Constraints));
        public LookaheadConstraints? Constraints { get => GetValue(ConstraintsProperty); set => SetValue(ConstraintsProperty, value); }
        private LookaheadConstraints? _approach;
        public void SetApproach(LookaheadConstraints constraints) { _approach = constraints; InvalidateMeasure(); }
        static ConstraintHost() => AffectsMeasure<ConstraintHost>(ConstraintsProperty);
        public IEnumerable<Control> LookaheadChildren => Child is { } child ? [child] : [];
        public IEnumerable<AvaloniaProperty> LookaheadProperties => [ConstraintsProperty];
        private LookaheadConstraints Effective(Size available) => LookaheadConstraints.AtMost(available).Constrain(Constraints!.Value);
        protected override Size MeasureOverride(Size availableSize) => Child is not { } child ? default : Constraints is not null
            ? LookaheadMeasure.Measure(child, _approach ?? Effective(availableSize)) : LookaheadMeasure.Measure(child, availableSize);
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, Size available) => Child is not { } child ? default : Constraints is not null
            ? builder.Measure(child, Effective(available)) : builder.Measure(child, available);
        public void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        {
            if (Child is { } child) builder.Place(child, bounds);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
            return finalSize;
        }
    }

    private sealed class Fixture : IDisposable
    {
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control child)
        {
            Root = new LookaheadRoot { Width = 400, Height = 300, Child = child, UseLayoutRounding = false };
            Window = new AnimationTestWindow() { Width = 500, Height = 400, Content = Root };
            Window.Show(); Layout();
        }
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public Rect Actual(Control node) => new(node.TranslatePoint(default, Root)!.Value, node.Bounds.Size);
        public void Dispose() => Window.Close();
    }

    [Fact]
    public Task TargetPass_IsIndependentAndPublishesFullConstraints() => OnUi(() =>
    {
        var probe = new Probe();
        LookaheadMeasure.Measure(probe, new Size(300, 200));
        probe.Arrange(new Rect(probe.DesiredSize));
        var bounds = probe.Bounds;
        var desired = probe.DesiredSize;
        var builder = new LookaheadLayoutBuilder();
        var constraints = LookaheadConstraints.Fixed(new Size(120, 100));
        Assert.Equal(new Size(120, 100), builder.Measure(probe, constraints));
        builder.Place(probe, new Rect(10, 20, 200, 200));
        var snapshot = builder.Build(1, new Size(200, 200));
        Assert.Equal(constraints, snapshot.Results[probe].FullMeasureConstraints);
        Assert.Equal(constraints.MaxSize, snapshot.Results[probe].MeasureConstraints);
        Assert.Equal(new Rect(10, 20, 120, 100), snapshot.Results[probe].Bounds);
        Assert.Equal(bounds, probe.Bounds);
        Assert.Equal(desired, probe.DesiredSize);
        Assert.Equal(1, probe.ActualMeasures);
        Assert.Equal(1, probe.TargetMeasures);
        LookaheadMeasure.Measure(probe, constraints);
        probe.Arrange(new Rect(10, 20, 200, 200));
        Assert.Equal(snapshot.Results[probe].Bounds, probe.Bounds);
    });

    [Fact]
    public Task MinimumOnlyChanges_InvalidateNativeCache_AndNativeModeClearsThem() => OnUi(() =>
    {
        var probe = new Probe();
        var first = new LookaheadConstraints(100, 300, 80, 200);
        var second = new LookaheadConstraints(180, 300, 120, 200);
        Assert.Equal(new Size(100, 80), LookaheadMeasure.Measure(probe, first));
        LookaheadMeasure.Measure(probe, first);
        Assert.Equal(1, probe.ActualMeasures);
        Assert.Equal(new Size(180, 120), LookaheadMeasure.Measure(probe, second));
        Assert.Equal(2, probe.ActualMeasures);
        Assert.Equal(second, probe.LastFull);
        Assert.Equal(probe.Natural, LookaheadMeasure.Measure(probe, second.MaxSize));
        Assert.Equal(3, probe.ActualMeasures);
        Assert.Null(LookaheadMeasure.ActualConstraints(probe));
        Assert.Equal(new Size(100, 80), LookaheadMeasure.Measure(probe, first));
        Assert.Equal(4, probe.ActualMeasures);
    });

    [Fact]
    public Task FullConstraintsWinLocalPreferences_WithoutChangingProperties_AndNativeRestoresThem() => OnUi(() =>
    {
        var probe = new Probe { Width = 300, MinHeight = 150, MaxHeight = 100 };
        var propertyChanges = new List<AvaloniaProperty>();
        probe.PropertyChanged += (_, e) => propertyChanges.Add(e.Property);
        var fixedSize = LookaheadConstraints.Fixed(new Size(120, 80));
        LookaheadMeasure.Measure(probe, fixedSize);
        probe.Arrange(new Rect(fixedSize.MaxSize));
        Assert.Equal(fixedSize, probe.LastFull);
        Assert.Equal(fixedSize.MaxSize, probe.Bounds.Size);
        Assert.Equal(300, probe.Width);
        Assert.DoesNotContain(Layoutable.WidthProperty, propertyChanges);
        Assert.DoesNotContain(Layoutable.MinHeightProperty, propertyChanges);
        LookaheadMeasure.Measure(probe, fixedSize.MaxSize); // identical native cache key, different mode
        probe.Arrange(new Rect(fixedSize.MaxSize));
        Assert.Equal(new Size(300, 150), probe.LastMaximum);
        Assert.Equal(new Size(300, 150), probe.Bounds.Size);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task BoxConsumesOrPropagatesMinima_InBothPasses(bool propagate) => OnUi(() =>
    {
        var probe = new Probe();
        var box = new LookaheadBox { PropagateMinConstraints = propagate, UseLayoutRounding = false, Children = { probe } };
        var constraints = LookaheadConstraints.Fixed(new Size(200, 140));
        var host = new ConstraintHost { Child = box, Constraints = constraints };
        using var f = new Fixture(host);
        Assert.Equal(propagate ? constraints : constraints.Loosen(), probe.LastFull);
        Assert.Equal(probe.LastFull, probe.LastTarget);
        Assert.Equal(constraints.MaxSize, box.DesiredSize);
        Assert.Equal(constraints.MaxSize, box.Bounds.Size);
        Assert.Equal(f.Root.Snapshot!.Results[probe].Bounds, f.Actual(probe));
        box.PropagateMinConstraints = !propagate; f.Layout();
        Assert.Equal(!propagate ? constraints : constraints.Loosen(), probe.LastFull);
    });

    [Theory]
    [InlineData(Orientation.Vertical)]
    [InlineData(Orientation.Horizontal)]
    public Task StackConsumesMinimaAndUnboundsStackingAxis_InBothPasses(Orientation orientation) => OnUi(() =>
    {
        var probe = new Probe();
        var stack = new LookaheadStackPanel { Orientation = orientation, Children = { probe }, UseLayoutRounding = false };
        using var f = new Fixture(new ConstraintHost { Child = stack, Constraints = LookaheadConstraints.Fixed(new Size(200, 140)) });
        var expected = orientation == Orientation.Vertical ? new LookaheadConstraints(0, 200, 0, double.PositiveInfinity)
            : new LookaheadConstraints(0, double.PositiveInfinity, 0, 140);
        Assert.Equal(expected, probe.LastFull);
        Assert.Equal(expected, probe.LastTarget);
        Assert.Equal(new Size(200, 140), stack.Bounds.Size);
        Assert.Equal(f.Root.Snapshot!.Results[probe].Bounds, f.Actual(probe));
    });

    private sealed class MeasuredText : LookaheadTextBlock
    {
        public Size ActualMaximum { get; private set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            ActualMaximum = availableSize;
            return base.MeasureOverride(availableSize);
        }
    }

    [Fact]
    public Task TextRemeasuresUnderFullConstraint_NotOnlyResizedDuringArrange() => OnUi(() =>
    {
        var text = new MeasuredText
        {
            Text = "Actual text must wrap at the constrained width even with a larger local Width.",
            Width = 300, TextWrapping = TextWrapping.Wrap, FontSize = 16, UseLayoutRounding = false
        };
        var host = new ConstraintHost { Child = text, Constraints = LookaheadConstraints.Fixed(new Size(120, 180)) };
        using var f = new Fixture(host);
        Assert.Equal(new Size(120, 180), text.ActualMaximum);
        Assert.Equal(new Size(120, 180), text.Bounds.Size);
        Assert.Equal(300, text.Width);
        var lines = text.TextLayout.TextLines.Count;
        host.Constraints = LookaheadConstraints.Fixed(new Size(240, 180)); f.Layout();
        Assert.Equal(new Size(240, 180), text.ActualMaximum);
        Assert.True(text.TextLayout.TextLines.Count < lines);
        Assert.Equal(f.Root.Snapshot!.Results[text].Bounds, f.Actual(text));
        host.Constraints = null; f.Layout();
        Assert.Equal(300, text.ActualMaximum.Width);
        Assert.Equal(300, text.Bounds.Width);
        Assert.Null(f.Root.Snapshot!.Results[text].FullMeasureConstraints);
    });

    [Fact]
    public Task MarginAlignmentAndFractionalFixedSize_UseIdenticalTargetAndActualGeometry() => OnUi(() =>
    {
        var probe = new Probe
        {
            Margin = new Thickness(3.5, 4.5), HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom, UseLayoutRounding = true
        };
        var constraints = LookaheadConstraints.Fixed(new Size(150.25, 100.25));
        using var f = new Fixture(new ConstraintHost { Child = probe, Constraints = constraints });
        Assert.Equal(constraints.MaxSize, probe.DesiredSize);
        Assert.Equal(f.Root.Snapshot!.Results[probe].Bounds, f.Actual(probe));
        Assert.Equal(probe.LastTarget, probe.LastFull);
        probe.InvalidateMeasure(); f.Layout(); // Layout manager direct remeasure must keep the four-bound request.
        Assert.Equal(f.Root.Snapshot.Results[probe].Bounds, f.Actual(probe));
    });

    [Fact]
    public Task LeavingRootAndChangingParent_ClearConstraintState() => OnUi(() =>
    {
        var probe = new Probe { Width = 300 };
        var host = new ConstraintHost { Child = probe, Constraints = LookaheadConstraints.Fixed(new Size(120, 100)) };
        using var f = new Fixture(host);
        Assert.Equal(120, probe.Bounds.Width);
        host.Child = null; f.Layout();
        var native = new Border { Child = probe };
        f.Window.Content = native; f.Layout();
        Assert.Equal(300, probe.LastMaximum.Width);
        Assert.Equal(300, probe.Bounds.Width);
        Assert.Null(LookaheadMeasure.ActualConstraints(probe));
    });

    [Fact]
    public Task LegacyFallbackIsExplicit_AndCannotSilentlyDropMinimumConstraints() => OnUi(() =>
    {
        var leaf = new Border { Width = 50, Height = 30 };
        var full = LookaheadConstraints.Fixed(new Size(100, 80));
        Assert.Throws<NotSupportedException>(() => LookaheadMeasure.Measure(leaf, full));
        Assert.Throws<NotSupportedException>(() => LookaheadMeasure.MeasureChild(leaf, full));
        Assert.Throws<NotSupportedException>(() => new LookaheadLayoutBuilder().Measure(leaf, full));
        Assert.Throws<NotSupportedException>(() => new LookaheadLayoutBuilder().MeasureChild(leaf, full));
        var builder = new LookaheadLayoutBuilder();
        Assert.Equal(builder.MeasureChild(leaf, full.Loosen()), LookaheadMeasure.MeasureChild(leaf, full.Loosen()));
        Assert.Null(builder.Build(1, full.MaxSize).Results[leaf].FullMeasureConstraints);
    });

    [Fact]
    public Task ExceptionsAndMissingBridge_DoNotLeaveValidCacheOrLeakedConstraints() => OnUi(() =>
    {
        var probe = new Probe { Throw = true };
        var constraints = LookaheadConstraints.Fixed(new Size(100, 80));
        Assert.Throws<InvalidOperationException>(() => LookaheadMeasure.Measure(probe, constraints));
        Assert.False(probe.IsMeasureValid);
        Assert.Null(LookaheadMeasure.ActualConstraints(probe));
        probe.Throw = false;
        Assert.Equal(constraints.MaxSize, LookaheadMeasure.Measure(probe, constraints));
        Assert.Equal(2, probe.ActualMeasures);
        var broken = new Probe { Bridge = false };
        Assert.Throws<InvalidOperationException>(() => LookaheadMeasure.Measure(broken, constraints));
        Assert.False(broken.IsMeasureValid);
        Assert.Null(LookaheadMeasure.ActualConstraints(broken));
    });

    [Fact]
    public Task HiddenAndNonFiniteMeasurement_DoNotInventValidGeometry() => OnUi(() =>
    {
        var constraints = LookaheadConstraints.Fixed(new Size(100, 80));
        var probe = new Probe { IsVisible = false };
        Assert.Equal(default, LookaheadMeasure.Measure(probe, constraints));
        Assert.Equal(default, new LookaheadLayoutBuilder().Measure(probe, constraints));
        probe.IsVisible = true;
        probe.Natural = new Size(double.PositiveInfinity, 10);
        Assert.Throws<InvalidOperationException>(() => LookaheadMeasure.Measure(probe, constraints));
        Assert.Throws<InvalidOperationException>(() => new LookaheadLayoutBuilder().Measure(probe, constraints));
        probe.Natural = new Size(20, 10);
        Assert.Equal(constraints.MaxSize, LookaheadMeasure.Measure(probe, constraints));
    });

    [Fact]
    public Task ActualMinimumSamples_DoNotDirtyTargetSnapshot() => OnUi(() =>
    {
        var probe = new Probe();
        var target = new LookaheadConstraints(200, 300, 140, 200);
        var host = new ConstraintHost { Child = probe, Constraints = target };
        using var f = new Fixture(host);
        var snapshot = f.Root.Snapshot!;
        var predictions = probe.TargetMeasures;
        host.SetApproach(new LookaheadConstraints(100, 300, 80, 200)); f.Layout();
        Assert.Equal(new Size(100, 80), probe.DesiredSize);
        host.SetApproach(new LookaheadConstraints(150, 300, 100, 200)); f.Layout();
        Assert.Equal(new Size(150, 100), probe.DesiredSize);
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.Equal(predictions, probe.TargetMeasures);
        Assert.Equal(target, snapshot.Results[probe].FullMeasureConstraints);
        host.SetApproach(target); f.Layout();
        Assert.Equal(snapshot.Results[probe].Bounds, f.Actual(probe));
    });

    [Fact]
    public Task DirectFrameworkRemeasureFailureAndArrangeFailure_LeaveInvalidCaches() => OnUi(() =>
    {
        var probe = new Probe();
        var constraints = LookaheadConstraints.Fixed(new Size(100, 80));
        LookaheadMeasure.Measure(probe, constraints);
        probe.Throw = true;
        probe.InvalidateMeasure();
        Assert.Throws<InvalidOperationException>(() => probe.Measure(constraints.MaxSize));
        Assert.False(probe.IsMeasureValid);
        probe.Throw = false;
        LookaheadMeasure.Measure(probe, constraints);
        probe.Arrange(new Rect(constraints.MaxSize));
        probe.BadArrange = true;
        probe.InvalidateArrange();
        Assert.Throws<InvalidOperationException>(() => probe.Arrange(new Rect(constraints.MaxSize)));
        Assert.False(probe.IsArrangeValid);
        probe.BadArrange = false;
        probe.Arrange(new Rect(constraints.MaxSize));
        Assert.Equal(constraints.MaxSize, probe.Bounds.Size);
    });
}

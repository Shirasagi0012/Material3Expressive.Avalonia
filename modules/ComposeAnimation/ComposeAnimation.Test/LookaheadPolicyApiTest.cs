using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ComposeAnimation.Test;

public class LookaheadPolicyApiTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    [Fact]
    public Task SinglePolicy_DrivesTargetAndApproach_AndTryGetTargetIsApproachOnly() => OnUi(() =>
    {
        var leaf = new PolicyLeaf(new Size(40, 20));
        var panel = new PolicyPanel(leaf);
        using var fixture = new Fixture(new LookaheadRoot
        {
            Width = 200,
            Height = 120,
            Child = panel,
            UseLayoutRounding = false
        });

        Assert.Contains(LookaheadLayoutPhase.Target, panel.Phases);
        Assert.Contains(LookaheadLayoutPhase.Approach, panel.Phases);
        Assert.Contains(LookaheadLayoutPhase.Target, leaf.Phases);
        Assert.Contains(LookaheadLayoutPhase.Approach, leaf.Phases);
        Assert.False(panel.TargetQueryByPhase[LookaheadLayoutPhase.Target]);
        Assert.True(panel.TargetQueryByPhase[LookaheadLayoutPhase.Approach]);
        Assert.Equal(panel.Bounds, leaf.Bounds);
        Assert.NotNull(Lookahead.GetSnapshot(panel));
    });

    [Fact]
    public void TargetPolicy_AppliesFullConstraintsAndMarginsOnce_AndKeepsOnlyLastChildMeasure()
    {
        var leaf = new PolicyLeaf(new Size(5, 5))
        {
            Margin = new Thickness(5, 3),
            UseLayoutRounding = false
        };
        var panel = new PolicyPanel(leaf)
        {
            Margin = new Thickness(7, 4),
            UseLayoutRounding = false,
            FirstChildConstraints = LookaheadConstraints.AtMost(new Size(30, 20)),
            ChildConstraints = LookaheadConstraints.Fixed(new Size(80, 40)),
            MeasureChildTwice = true
        };
        var builder = new LookaheadLayoutBuilder();
        var incoming = LookaheadConstraints.Fixed(new Size(120, 80));

        Assert.Equal(incoming.MaxSize, builder.Measure(panel, incoming));
        builder.Place(panel, new Rect(incoming.MaxSize));
        var snapshot = builder.Build(1, incoming.MaxSize);

        Assert.Equal(LookaheadConstraints.Fixed(new Size(106, 72)), panel.LastTargetConstraints);
        Assert.Equal(LookaheadConstraints.Fixed(new Size(70, 34)), leaf.LastTargetConstraints);
        Assert.Equal(LookaheadConstraints.Fixed(new Size(70, 34)),
            snapshot.Results[leaf].FullMeasureConstraints);
        Assert.Equal(new Size(80, 40), snapshot.Results[leaf].DesiredSize);
        Assert.Equal(new Rect(7, 4, 106, 72), snapshot.Results[panel].Bounds);
    }

    [Fact]
    public Task FirstFullConstraintMeasure_RegistersTheAutomaticEntryBeforeCapturingState() => OnUi(() =>
    {
        var panel = new PolicyPanel(new PolicyLeaf(new Size(10, 10)))
        {
            Margin = new Thickness(6, 4),
            UseLayoutRounding = false
        };

        Assert.Equal(new Size(120, 80),
            LookaheadMeasure.Measure(panel, LookaheadConstraints.Fixed(new Size(120, 80))));
        Assert.Equal(LookaheadConstraints.Fixed(new Size(108, 72)), panel.LastTargetConstraints);
        Assert.Equal(LookaheadConstraints.Fixed(new Size(108, 72)),
            LookaheadMeasure.ActualConstraints(panel));
        LookaheadLayout.Forget(panel);
    });

    [Fact]
    public Task ManualPolicy_FirstAndPostForgetFullMeasuresKeepTheirMinima() => OnUi(() =>
    {
        var control = new ManualPolicyControl { UseLayoutRounding = false };

        Assert.Equal(new Size(90, 55),
            LookaheadMeasure.Measure(control, LookaheadConstraints.Fixed(new Size(90, 55))));
        Assert.Equal(LookaheadConstraints.Fixed(new Size(90, 55)),
            LookaheadMeasure.ActualConstraints(control));

        LookaheadLayout.Forget(control);
        Assert.Equal(new Size(70, 45),
            LookaheadMeasure.Measure(control, LookaheadConstraints.Fixed(new Size(70, 45))));
        Assert.Equal(LookaheadConstraints.Fixed(new Size(70, 45)),
            LookaheadMeasure.ActualConstraints(control));
        LookaheadLayout.Forget(control);
    });

    [Fact]
    public Task NestedLocalSlotsMarginsAndNonzeroEntryOrigin_MatchActualGeometry() => OnUi(() =>
    {
        var leaf = new PolicyLeaf(new Size(30, 18))
        {
            Margin = new Thickness(3, 4, 5, 6),
            UseLayoutRounding = false
        };
        var inner = new OffsetPanel(leaf, new Vector(7, 9)) { UseLayoutRounding = false };
        var outer = new OffsetPanel(inner, new Vector(5, 8))
        {
            Margin = new Thickness(11, 13, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        var nativeParent = new Grid { Width = 260, Height = 180, Children = { outer } };
        using var fixture = new Fixture(nativeParent);

        var snapshot = Lookahead.GetSnapshot(outer)!;
        var actual = new Rect(leaf.TranslatePoint(default, nativeParent)!.Value, leaf.Bounds.Size);
        Assert.Equal(snapshot.Results[leaf].Bounds, actual);
        Assert.True(actual.X > outer.Margin.Left);
        Assert.True(actual.Y > outer.Margin.Top);
    });

    [Fact]
    public void MeasuredButUnarrangedChild_IsExcludedFromTheTargetSnapshot()
    {
        var leaf = new PolicyLeaf(new Size(20, 10));
        var panel = new PolicyPanel(leaf) { SkipArrange = true };
        var builder = new LookaheadLayoutBuilder();
        builder.Measure(panel, LookaheadConstraints.AtMost(new Size(100, 80)));
        builder.Place(panel, new Rect(new Size(100, 80)));
        var snapshot = builder.Build(1, new Size(100, 80));

        Assert.True(snapshot.Results.ContainsKey(panel));
        Assert.False(snapshot.Results.ContainsKey(leaf));
        Assert.Contains(LookaheadLayoutPhase.Target, leaf.Phases);
    }

    [Fact]
    public void RepeatedBuilderPlacement_RecomputesSelectionWithoutDestroyingCandidateMeasurements()
    {
        var child = new Border { Width = 20, Height = 10 };
        var panel = new ResponsivePlacementPanel(child);
        var builder = new LookaheadLayoutBuilder();
        builder.Measure(panel, LookaheadConstraints.AtMost(new Size(100, 80)));

        builder.Place(panel, new Rect(new Size(30, 40)));
        Assert.False(builder.Build(1, new Size(30, 40)).Results.ContainsKey(child));

        builder.Place(panel, new Rect(new Size(100, 80)));
        var selected = builder.Build(2, new Size(100, 80));
        Assert.True(selected.Results.ContainsKey(child));
        Assert.Equal(new Rect(40, 35, 20, 10), selected.Results[child].Bounds);
    }

    [Fact]
    public Task LocalEntry_ReusesOneCandidateWhenLaterSlotSelectsPreviouslyUnarrangedChild() => OnUi(() =>
    {
        var child = new Border { Width = 20, Height = 10 };
        var panel = new ResponsivePlacementPanel(child) { UseLayoutRounding = false };
        var parent = new Grid { Children = { panel } };
        panel.Measure(new Size(100, 80));

        panel.Arrange(new Rect(new Size(30, 40)));
        var first = Lookahead.GetSnapshot(panel)!;
        Assert.False(first.Results.ContainsKey(child));

        panel.Arrange(new Rect(new Size(100, 80)));
        var second = Lookahead.GetSnapshot(panel)!;
        Assert.True(second.Version > first.Version);
        Assert.True(second.Results.ContainsKey(child));
        Assert.Same(parent, second.CoordinateReference);
    });

    [Fact]
    public Task UnselectedAnimatedChild_UsesInternalCandidateMeasureWithoutPublishingFakeBounds() => OnUi(() =>
    {
        var animated = new AnimatedBounds
        {
            TargetSize = new Size(20, 10),
            UseLayoutRounding = false
        };
        var panel = new ResponsivePlacementPanel(animated) { UseLayoutRounding = false };
        var host = new SlotHost(panel) { SlotWidth = 30 };
        using var fixture = new Fixture(host);

        var first = Lookahead.GetSnapshot(panel)!;
        Assert.False(first.Results.ContainsKey(animated));
        Assert.Null(Lookahead.GetSnapshot(animated));
        Assert.Null(animated.TargetBounds);
        Assert.False(animated.IsRunning);

        host.SlotWidth = 100;
        host.InvalidateArrange();
        fixture.Layout();

        var second = Lookahead.GetSnapshot(panel)!;
        Assert.True(second.Version > first.Version);
        Assert.True(second.Results.ContainsKey(animated));
        Assert.NotNull(animated.TargetBounds);
    });

    [Fact]
    public Task CompatibilityRoot_UnselectedAnimatedChildAlsoUsesOnlyInternalCandidateMeasure() => OnUi(() =>
    {
        var animated = new AnimatedBounds
        {
            TargetSize = new Size(20, 10),
            UseLayoutRounding = false
        };
        var panel = new ResponsivePlacementPanel(animated) { UseLayoutRounding = false };
        var root = new LookaheadRoot
        {
            Width = 30,
            Height = 80,
            Child = panel,
            UseLayoutRounding = false
        };
        using var fixture = new Fixture(root);

        var first = root.Snapshot!;
        Assert.False(first.Results.ContainsKey(animated));
        Assert.Null(Lookahead.GetSnapshot(animated));
        Assert.Null(animated.TargetBounds);

        root.Width = 100;
        fixture.Layout();

        Assert.True(root.Snapshot!.Version > first.Version);
        Assert.True(root.Snapshot.Results.ContainsKey(animated));
        Assert.NotNull(animated.TargetBounds);
    });

    [Fact]
    public void StaleAndCrossGenerationPlaceables_AreRejected()
    {
        var leaf = new PolicyLeaf(new Size(20, 10));
        var stale = new PolicyPanel(leaf)
        {
            MeasureChildTwice = true,
            ArrangeFirstMeasurement = true
        };
        var builder = new LookaheadLayoutBuilder();
        builder.Measure(stale, LookaheadConstraints.AtMost(new Size(100, 80)));
        Assert.Throws<InvalidOperationException>(() => builder.Place(stale, new Rect(new Size(100, 80))));

        var cross = new PolicyPanel(new PolicyLeaf(new Size(20, 10))) { SaveCurrentHandle = true };
        var first = new LookaheadLayoutBuilder();
        first.Measure(cross, LookaheadConstraints.AtMost(new Size(100, 80)));
        first.Place(cross, new Rect(new Size(100, 80)));
        cross.SaveCurrentHandle = false;
        cross.ArrangeSavedHandle = true;
        var second = new LookaheadLayoutBuilder();
        second.Measure(cross, LookaheadConstraints.AtMost(new Size(100, 80)));
        Assert.Throws<InvalidOperationException>(() => second.Place(cross, new Rect(new Size(100, 80))));
    }

    [Fact]
    public void SequentialRemeasure_RemovesDescendantsUsedOnlyByTheIntermediateBranch()
    {
        var narrow = new PolicyLeaf(new Size(10, 10));
        var wide = new PolicyLeaf(new Size(20, 10));
        var switching = new SwitchingPanel(narrow, wide);
        var outer = new PolicyPanel(switching)
        {
            MeasureChildTwice = true,
            FirstChildConstraints = LookaheadConstraints.AtMost(new Size(30, 40)),
            ChildConstraints = LookaheadConstraints.AtMost(new Size(80, 40))
        };
        var builder = new LookaheadLayoutBuilder();
        builder.Measure(outer, LookaheadConstraints.AtMost(new Size(100, 80)));
        builder.Place(outer, new Rect(new Size(100, 80)));
        var snapshot = builder.Build(1, new Size(100, 80));

        Assert.False(snapshot.Results.ContainsKey(narrow));
        Assert.True(snapshot.Results.ContainsKey(wide));
        Assert.True(snapshot.Results.ContainsKey(switching));
    }

    [Fact]
    public void PlacementRejectsDuplicateArrangeAndCapturedMeasureContext()
    {
        var leaf = new PolicyLeaf(new Size(20, 10));
        var duplicate = new PolicyPanel(leaf) { ArrangeTwice = true };
        var builder = new LookaheadLayoutBuilder();
        builder.Measure(duplicate, LookaheadConstraints.AtMost(new Size(100, 80)));
        Assert.Throws<InvalidOperationException>(() => builder.Place(duplicate, new Rect(new Size(100, 80))));

        var lateMeasure = new PolicyPanel(new PolicyLeaf(new Size(20, 10))) { MeasureDuringPlacement = true };
        builder = new LookaheadLayoutBuilder();
        builder.Measure(lateMeasure, LookaheadConstraints.AtMost(new Size(100, 80)));
        Assert.Throws<InvalidOperationException>(() => builder.Place(lateMeasure, new Rect(new Size(100, 80))));

        var mutation = new PolicyPanel(new PolicyLeaf(new Size(20, 10))) { MutateTreeDuringPlacement = true };
        builder = new LookaheadLayoutBuilder();
        builder.Measure(mutation, LookaheadConstraints.AtMost(new Size(100, 80)));
        Assert.Throws<InvalidOperationException>(() => builder.Place(mutation, new Rect(new Size(100, 80))));
    }

    [Fact]
    public Task CrossPhasePlaceable_IsRejected() => OnUi(() =>
    {
        var leaf = new PolicyLeaf(new Size(20, 10));
        var panel = new PolicyPanel(leaf) { SaveTargetHandle = true };
        var target = new LookaheadLayoutBuilder();
        target.Measure(panel, LookaheadConstraints.AtMost(new Size(100, 80)));
        target.Place(panel, new Rect(new Size(100, 80)));

        panel.ArrangeSavedHandle = true;
        LookaheadPolicyRuntime.MeasureActual(panel, panel, LookaheadLayoutPhase.Native,
            LookaheadConstraints.AtMost(new Size(100, 80)));
        Assert.Throws<InvalidOperationException>(() =>
            LookaheadPolicyRuntime.ArrangeActual(panel, new Size(100, 80)));
    });

    [Fact]
    public void EscapedPlacementContext_IsRejectedByTheNextPlacementToken()
    {
        var panel = new PolicyPanel(new PolicyLeaf(new Size(20, 10))) { SavePlacementContext = true };
        var builder = new LookaheadLayoutBuilder();
        builder.Measure(panel, LookaheadConstraints.AtMost(new Size(100, 80)));
        builder.Place(panel, new Rect(new Size(100, 80)));

        panel.UseSavedPlacementContext = true;
        Assert.Throws<InvalidOperationException>(() =>
            builder.Place(panel, new Rect(new Size(100, 80))));
    }

    [Fact]
    public void NativeFallback_UsesTheSamePolicyAndAcceptsOrdinaryChildren()
    {
        var ordinary = new Border { Width = 35, Height = 18, Margin = new Thickness(2) };
        var panel = new PolicyPanel(ordinary);

        var desired = panel.InvokeNativeMeasure(new Size(100, 80));
        panel.InvokeNativeArrange(desired);

        Assert.Equal(new Size(39, 22), desired);
        Assert.Equal([LookaheadLayoutPhase.Native], panel.Phases);
        Assert.Equal(new Rect(2, 2, 35, 18), ordinary.Bounds);
    }

    [Fact]
    public Task PositionOnlyArrange_ReusesTheApproachMeasurement() => OnUi(() =>
    {
        var leaf = new PolicyLeaf(new Size(40, 20));
        var panel = new PolicyPanel(leaf);
        using var fixture = new Fixture(new Grid { Width = 200, Height = 120, Children = { panel } });
        var approachMeasures = panel.Phases.Count(value => value == LookaheadLayoutPhase.Approach);
        var approachPlacements = panel.PlacementPhases.Count(value => value == LookaheadLayoutPhase.Approach);

        panel.InvalidateArrange();
        fixture.Layout();

        Assert.Equal(approachMeasures, panel.Phases.Count(value => value == LookaheadLayoutPhase.Approach));
        Assert.True(panel.PlacementPhases.Count(value => value == LookaheadLayoutPhase.Approach) > approachPlacements);
    });

    [Fact]
    public Task LowLevelBridge_SupportsControlsWithAnExistingBaseClass() => OnUi(() =>
    {
        var control = new ManualPolicyControl
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        using var fixture = new Fixture(new Grid { Width = 100, Height = 80, Children = { control } });

        Assert.Equal(new Size(25, 15), control.Bounds.Size);
        Assert.Contains(LookaheadLayoutPhase.Target, control.Phases);
        Assert.Contains(LookaheadLayoutPhase.Approach, control.Phases);
        Assert.NotNull(Lookahead.GetSnapshot(control));
    });

    [Fact]
    public Task LowLevelBridge_RunsApproachWhenCompatibilityRootUsesSizeMeasurement() => OnUi(() =>
    {
        var control = new ManualPolicyControl { UseLayoutRounding = false };
        var root = new LookaheadRoot
        {
            Width = 180,
            Height = 100,
            Child = control,
            UseLayoutRounding = false
        };
        using var fixture = new Fixture(root);

        Assert.Contains(LookaheadLayoutPhase.Target, control.Phases);
        Assert.Contains(LookaheadLayoutPhase.Approach, control.Phases);
        Assert.DoesNotContain(LookaheadLayoutPhase.Native, control.Phases);
        Assert.Equal(root.Snapshot!.Results[control].Bounds, control.Bounds);
    });

    private sealed class PolicyLeaf(Size natural) : LookaheadControl
    {
        internal List<LookaheadLayoutPhase> Phases { get; } = [];
        internal LookaheadConstraints LastTargetConstraints { get; private set; }

        protected override LookaheadMeasureResult MeasureLayout(
            LookaheadMeasureContext context, LookaheadConstraints constraints)
        {
            Phases.Add(context.Phase);
            if (context.Phase == LookaheadLayoutPhase.Target) LastTargetConstraints = constraints;
            return context.Layout(constraints.Constrain(natural));
        }
    }

    private sealed class PolicyPanel : LookaheadPanel
    {
        private readonly Control child;
        private LookaheadPlaceable? _saved;
        private LookaheadPlacementContext? _savedPlacement;
        internal List<LookaheadLayoutPhase> Phases { get; } = [];
        internal List<LookaheadLayoutPhase> PlacementPhases { get; } = [];
        internal Dictionary<LookaheadLayoutPhase, bool> TargetQueryByPhase { get; } = [];
        internal LookaheadConstraints LastTargetConstraints { get; private set; }
        internal LookaheadConstraints FirstChildConstraints { get; set; } = LookaheadConstraints.Unbounded;
        internal LookaheadConstraints ChildConstraints { get; set; } = LookaheadConstraints.Unbounded;
        internal bool MeasureChildTwice { get; set; }
        internal bool ArrangeFirstMeasurement { get; set; }
        internal bool ArrangeTwice { get; set; }
        internal bool MeasureDuringPlacement { get; set; }
        internal bool SaveCurrentHandle { get; set; }
        internal bool SaveTargetHandle { get; set; }
        internal bool ArrangeSavedHandle { get; set; }
        internal bool MutateTreeDuringPlacement { get; set; }
        internal bool SavePlacementContext { get; set; }
        internal bool UseSavedPlacementContext { get; set; }
        internal bool SkipArrange { get; set; }

        internal PolicyPanel(Control child)
        {
            this.child = child;
            Children.Add(child);
        }

        protected override LookaheadMeasureResult MeasureLayout(
            LookaheadMeasureContext context, LookaheadConstraints constraints)
        {
            Phases.Add(context.Phase);
            if (context.Phase == LookaheadLayoutPhase.Target) LastTargetConstraints = constraints;
            TargetQueryByPhase[context.Phase] = context.TryGetTarget(child, out _);
            LookaheadPlaceable? first = null;
            if (MeasureChildTwice) first = context.Measure(child, FirstChildConstraints);
            var current = context.Measure(child,
                ChildConstraints == LookaheadConstraints.Unbounded ? constraints.Loosen() : ChildConstraints);
            if (SaveCurrentHandle) _saved = current;
            if (SaveTargetHandle && context.Phase == LookaheadLayoutPhase.Target) _saved = current;
            var chosen = ArrangeSavedHandle ? _saved! : ArrangeFirstMeasurement ? first! : current;
            var size = constraints.Constrain(current.DesiredSize);
            return context.Layout(size, placement =>
            {
                PlacementPhases.Add(placement.Phase);
                if (MeasureDuringPlacement) context.Measure(child, constraints);
                if (MutateTreeDuringPlacement) Children.Clear();
                if (SavePlacementContext && _savedPlacement is null) _savedPlacement = placement;
                if (!SkipArrange)
                    (UseSavedPlacementContext ? _savedPlacement! : placement)
                        .Arrange(chosen, new Rect(placement.FinalSize));
                if (ArrangeTwice) placement.Arrange(chosen, new Rect(placement.FinalSize));
            });
        }

        internal Size InvokeNativeMeasure(Size available) => base.MeasureOverride(available);
        internal Size InvokeNativeArrange(Size finalSize) => base.ArrangeOverride(finalSize);
    }

    private sealed class OffsetPanel : LookaheadPanel
    {
        private readonly Control _child;
        private readonly Vector _offset;

        internal OffsetPanel(Control child, Vector offset)
        {
            _child = child;
            _offset = offset;
            Children.Add(child);
        }

        protected override LookaheadMeasureResult MeasureLayout(
            LookaheadMeasureContext context, LookaheadConstraints constraints)
        {
            var placeable = context.Measure(_child, constraints.Loosen());
            var measured = new Size(placeable.DesiredSize.Width + _offset.X,
                placeable.DesiredSize.Height + _offset.Y);
            return context.Layout(constraints.Constrain(measured), placement =>
                placement.Arrange(placeable,
                    new Rect(new Point(_offset.X, _offset.Y), placeable.DesiredSize)));
        }
    }

    private sealed class ResponsivePlacementPanel : LookaheadPanel
    {
        private readonly Control _child;

        internal ResponsivePlacementPanel(Control child)
        {
            _child = child;
            Children.Add(child);
        }

        protected override LookaheadMeasureResult MeasureLayout(
            LookaheadMeasureContext context, LookaheadConstraints constraints)
        {
            var placeable = context.Measure(_child, constraints.Loosen());
            return context.Layout(constraints.Constrain(new Size(100, 80)), placement =>
            {
                if (placement.FinalSize.Width >= 50)
                    placement.Arrange(placeable, new Rect(placement.FinalSize));
            });
        }
    }

    private sealed class SlotHost : Decorator
    {
        internal SlotHost(Control child) => Child = child;
        internal double SlotWidth { get; set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            Child?.Measure(new Size(100, 80));
            return new Size(120, 90);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child?.Arrange(new Rect(new Size(SlotWidth, 80)));
            return finalSize;
        }
    }

    private sealed class SwitchingPanel : LookaheadPanel
    {
        private readonly Control _narrow;
        private readonly Control _wide;

        internal SwitchingPanel(Control narrow, Control wide)
        {
            _narrow = narrow;
            _wide = wide;
            Children.Add(narrow);
            Children.Add(wide);
        }

        protected override LookaheadMeasureResult MeasureLayout(
            LookaheadMeasureContext context, LookaheadConstraints constraints)
        {
            var selected = constraints.MaxWidth < 50 ? _narrow : _wide;
            var placeable = context.Measure(selected, constraints.Loosen());
            return context.Layout(constraints.Constrain(placeable.DesiredSize),
                placement => placement.Arrange(placeable, new Rect(placement.FinalSize)));
        }
    }

    private sealed class ManualPolicyControl : Control, ILookaheadLayoutPolicy
    {
        internal List<LookaheadLayoutPhase> Phases { get; } = [];
        IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => [];

        public LookaheadMeasureResult MeasureLayout(
            LookaheadMeasureContext context, LookaheadConstraints constraints)
        {
            Phases.Add(context.Phase);
            return context.Layout(constraints.Constrain(new Size(25, 15)));
        }

        protected override Size MeasureCore(Size availableSize)
            => LookaheadLayout.TryMeasureCore(this, availableSize, out var desired)
                ? desired : base.MeasureCore(availableSize);

        protected override void ArrangeCore(Rect finalRect)
        {
            if (LookaheadLayout.TryArrangeCore(this, finalRect, out var bounds)) Bounds = bounds;
            else base.ArrangeCore(finalRect);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            LookaheadLayout.Forget(this);
            base.OnDetachedFromVisualTree(e);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly Window _window;

        internal Fixture(Control content)
        {
            _window = new AnimationTestWindow() { Width = 300, Height = 220, Content = content };
            _window.Show();
            Layout();
        }

        internal void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            _window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            _window.UpdateLayout();
        }

        public void Dispose() => _window.Close();
    }
}

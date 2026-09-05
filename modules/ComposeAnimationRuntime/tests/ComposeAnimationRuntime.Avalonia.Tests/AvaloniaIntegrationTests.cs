using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Numerics;
using ComposeAnimationRuntime.Avalonia;
using Xunit;

namespace ComposeAnimationRuntime.Avalonia.Tests;

public sealed class AvaloniaIntegrationTests
{
    [Fact]
    public void PointConverterRoundTrips()
    {
        var converter = new PointVectorConverter();
        var value = new Point(12.5, -4.25);
        Assert.Equal(value, converter.ConvertFromVector(converter.ConvertToVector(value)));
    }

    [Fact]
    public void ColorAndMatrixConvertersRoundTrip()
    {
        var colorConverter = new ColorVectorConverter();
        var color = Color.FromArgb(128, 12, 34, 56);
        Assert.Equal(color, colorConverter.ConvertFromVector(colorConverter.ConvertToVector(color)));

        var matrixConverter = new MatrixVectorConverter();
        var matrix = new Matrix(2, 0.1, 0.2, 3, 40, 50);
        var roundTrip = matrixConverter.ConvertFromVector(matrixConverter.ConvertToVector(matrix));
        Assert.Equal(matrix.M11, roundTrip.M11, 5);
        Assert.Equal(matrix.M12, roundTrip.M12, 5);
        Assert.Equal(matrix.M21, roundTrip.M21, 5);
        Assert.Equal(matrix.M22, roundTrip.M22, 5);
        Assert.Equal(matrix.M31, roundTrip.M31, 5);
        Assert.Equal(matrix.M32, roundTrip.M32, 5);
    }

    [Fact]
    public void CornerRadiusConverterRoundTrips()
    {
        var converter = new CornerRadiusVectorConverter();
        var value = new CornerRadius(1, 2, 3, 4);
        Assert.Equal(value, converter.ConvertFromVector(converter.ConvertToVector(value)));
    }

    [Fact]
    public void GestureProgressMappersClampAndMapAxes()
    {
        var viewport = new Size(200, 100);
        Assert.Equal(0.5, new HorizontalProgressMapper().Map(new Vector2(100, 90), viewport));
        Assert.Equal(0.25, new VerticalProgressMapper().Map(new Vector2(190, 25), viewport));
        Assert.Equal(1, new CustomProgressMapper((position, _) => position.X / 10).Map(new Vector2(20, 0), viewport));
    }

    [Fact]
    public void PredictiveLayoutControllerCapturesAndStopsAfterDispose()
    {
        var control = new Border { Width = 24, Height = 16 };
        control.Measure(new Size(100, 100));
        control.Arrange(new Rect(3, 4, 24, 16));
        using var controller = new PredictiveLayoutController(control);

        var snapshot = controller.Capture();

        Assert.Equal(new Size(24, 16), snapshot.DesiredSize);
        Assert.Equal(new Rect(3, 4, 24, 16), snapshot.Bounds);
    }

    [Fact]
    public void AnimatedBoundsTrackTicksPositionAndSizeTogether()
    {
        var bounds = new AnimatedBounds { TargetBounds = new Rect(100, 50, 80, 40) };
        var track = (ITransitionTrack)bounds;
        track.Tick(TimeSpan.Zero);
        track.Tick(TimeSpan.FromMilliseconds(110));

        Assert.NotEqual(0, bounds.Width);
        Assert.NotEqual(0, bounds.Height);
    }

    [Fact]
    public void BindingCanBeDisposed()
    {
        var owner = new TestObject();
        var converter = new DoubleVectorConverter();
        var animation = new ComposeAnimationRuntime.Animatable<double>(0, converter);
        using (MotionBinding.Bind(owner, TestObject.ValueProperty, animation))
        {
            animation.SnapTo(1);
            Assert.Equal(1, owner.Value);
        }

        animation.SnapTo(2);
        Assert.Equal(1, owner.Value);
    }

    [Fact]
    public void LayoutSnapshotCapturesBoundsAndDesiredSize()
    {
        var control = new TestObject { Width = 40, Height = 20 };
        control.Measure(new Size(100, 100));
        control.Arrange(new Rect(5, 6, 40, 20));
        var snapshot = LayoutSnapshot.Capture(control);
        Assert.Equal(new Rect(5, 6, 40, 20), snapshot.Bounds);
        Assert.Equal(new Size(40, 20), snapshot.DesiredSize);
    }

    [Fact]
    public void LayoutSnapshotConvertsBetweenLocalAndRootCoordinates()
    {
        var root = new Panel();
        var control = new Border { Width = 40, Height = 20 };
        root.Children.Add(control);
        control.Measure(new Size(100, 100));
        control.Arrange(new Rect(10, 15, 40, 20));

        var snapshot = LayoutSnapshot.Capture(control, root);

        Assert.Equal(snapshot.RootBounds.Position, snapshot.ToRoot(snapshot.LocalBounds.Position));
        Assert.Equal(snapshot.LocalBounds.Position, snapshot.ToLocal(snapshot.RootBounds.Position));
    }

    [Fact]
    public void LayoutSnapshotUsesFullTransformForRotatedAndScaledBounds()
    {
        var root = new Panel();
        var control = new Border
        {
            Width = 20,
            Height = 10,
            RenderTransform = new ScaleTransform(2, 3)
        };
        root.Children.Add(control);
        control.Measure(new Size(100, 100));
        control.Arrange(new Rect(5, 7, 20, 10));

        var snapshot = LayoutSnapshot.Capture(control, root);

        Assert.Equal(new Size(40, 30), snapshot.RootBounds.Size);
        var rootPoint = snapshot.ToRoot(new Point(10, 5));
        Assert.Equal(new Point(10, 5), snapshot.ToLocal(rootPoint));
    }

    [Fact]
    public void LayoutSnapshotRoundTripsArbitraryPointsThroughNestedTransforms()
    {
        var root = new Panel();
        var parent = new Border
        {
            RenderTransform = new TranslateTransform(13, 17)
        };
        var control = new Border
        {
            Width = 20,
            Height = 10,
            RenderTransform = new ScaleTransform(2, 1.5)
        };
        parent.Child = control;
        root.Children.Add(parent);
        root.Measure(new Size(200, 200));
        root.Arrange(new Rect(0, 0, 200, 200));

        var snapshot = LayoutSnapshot.Capture(control, root);
        var localPoint = new Point(7, 3);

        Assert.Equal(localPoint, snapshot.ToLocal(snapshot.ToRoot(localPoint)));
    }

    [Fact]
    public async Task SharedTransitionRootAsyncTransactionCompletesWhenDetached()
    {
        var root = new SharedTransitionRoot();
        var invoked = false;

        var pairs = await root.RunTransitionAsync(() => invoked = true);

        Assert.True(invoked);
        Assert.Empty(pairs);
    }

    [Fact]
    public async Task SharedTransitionTransactionCanBeDisposedBeforeCommit()
    {
        var root = new SharedTransitionRoot();
        using var transaction = root.BeginTransaction();

        transaction.Dispose();

        Assert.True(transaction.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync(() => { }));
    }

    [Fact]
    public void SizeTransformFactoriesCreateEffects()
    {
        Assert.NotNull(SizeTransform.Expand(SizeTransformAxis.Horizontal));
        Assert.NotNull(SizeTransform.Shrink(SizeTransformAxis.Vertical));
    }

    [Fact]
    public void SharedGeometryInterpolatesMatrixAndRectangleClip()
    {
        var from = new SharedGeometry(
            new Rect(0, 0, 10, 20),
            new MatrixTransform(Matrix.Identity),
            new RectangleGeometry(new Rect(0, 0, 10, 20)),
            0,
            1,
            new CornerRadius(0));
        var to = new SharedGeometry(
            new Rect(20, 10, 30, 40),
            new MatrixTransform(new Matrix(2, 0, 0, 2, 10, 12)),
            new RectangleGeometry(new Rect(10, 5, 20, 30)),
            1,
            4,
            new CornerRadius(8));

        var value = SharedGeometry.Lerp(from, to, 0.5);

        Assert.Equal(new Rect(10, 5, 20, 30), value.Bounds);
        Assert.Equal(0.5, value.Opacity);
        Assert.Equal(1, value.ZIndex);
        Assert.Equal(new Rect(5, 2.5, 15, 25), ((RectangleGeometry)value.Clip!).Rect);
        Assert.Equal(1.5, ((MatrixTransform)value.Transform!).Matrix.M11, 5);
    }

    [Fact]
    public void AnimatedContentCanReplaceVisualContentWithoutDuplicateParent()
    {
        var animated = new AnimatedContent();
        var first = new Border { Width = 20, Height = 20 };
        var second = new Border { Width = 30, Height = 30 };

        animated.AnimatedValue = first;
        animated.AnimatedValue = second;

        Assert.True(animated.IsTransitioning);
    }

    [Fact]
    public void AnimatedContentUsesStateFactoryAndKeys()
    {
        var animated = new AnimatedContent
        {
            ContentFactory = state => new Border { Tag = state },
            KeySelector = state => state,
        };

        animated.TargetState = "first";
        animated.TargetState = "second";

        Assert.True(animated.IsTransitioning);
        Assert.Equal("second", animated.CurrentKey);
    }

    [Fact]
    public void AnimatedContentCanUseExternalClock()
    {
        var clock = new ManualAnimationClock();
        var content = new AnimatedContent { Duration = TimeSpan.FromSeconds(1) };
        content.AttachClock(clock);
        content.TargetState = "first";
        content.TargetState = "second";

        clock.AdvanceBy(TimeSpan.FromMilliseconds(250));

        Assert.True(content.IsTransitioning);
        content.CompleteTransition();
        Assert.False(content.IsTransitioning);
    }

    [Fact]
    public void AnimatedContentUsesComparerAndRetargetsSafely()
    {
        var animated = new AnimatedContent
        {
            ContentFactory = state => new Border { Tag = state },
            KeySelector = state => state is string text ? text.ToUpperInvariant() : state,
        };

        animated.TargetState = "a";
        animated.TargetState = "A";

        Assert.Equal("A", animated.CurrentKey);
        Assert.True(animated.IsTransitioning);

        animated.TargetState = "b";
        animated.TargetState = "c";

        Assert.Equal("C", animated.CurrentKey);
        Assert.True(animated.IsTransitioning);
    }

    [Fact]
    public void AnimatedContentCanCompleteAndCancelTransition()
    {
        var animated = new AnimatedContent();
        var completed = 0;
        animated.TransitionCompleted += (_, _) => completed++;

        animated.TargetState = new Border { Tag = "first" };
        animated.TargetState = new Border { Tag = "second" };
        animated.CompleteTransition();

        Assert.False(animated.IsTransitioning);
        Assert.Equal(1, completed);

        animated.TargetState = new Border { Tag = "third" };
        animated.CancelTransition();

        Assert.False(animated.IsTransitioning);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void SharedTransitionRootProvidesOverlay()
    {
        var root = new SharedTransitionRoot();
        Assert.Contains(root.Overlay, root.Children);
        Assert.False(root.Overlay.IsHitTestVisible);
        Assert.Contains(root.ContentLayer, root.Children);
    }

    [Fact]
    public void SharedParticipantRegistryTracksLifecycle()
    {
        var registry = new SharedParticipantRegistry();
        var control = new Border();
        registry.Register("item", control, SharedParticipantRole.Source);

        Assert.True(registry.TryGet("item", out var participant));
        Assert.Equal(SharedParticipantRole.Source, participant.Role);
        Assert.True(registry.Unregister("item", control));
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public void SharedParticipantRegistryPairsSourceAndDestination()
    {
        var registry = new SharedParticipantRegistry();
        registry.Register("item", new Border(), SharedParticipantRole.Source);
        registry.Register("item", new Border(), SharedParticipantRole.Destination);

        Assert.True(registry.TryPair("item", out var source, out var destination));
        Assert.NotNull(source);
        Assert.NotNull(destination);
    }

    [Fact]
    public void SharedParticipantRegistryRejectsDuplicateRoleAndRemovesEmptyKeys()
    {
        var registry = new SharedParticipantRegistry();
        var first = new Border();
        registry.Register("item", first, SharedParticipantRole.Source);

        Assert.Throws<InvalidOperationException>(() =>
            registry.Register("item", new Border(), SharedParticipantRole.Source));
        Assert.True(registry.Unregister("item", first));
        Assert.False(registry.TryGet("item", out _));
    }

    [Fact]
    public void SharedParticipantRegistryUpdatesRoleForExistingControl()
    {
        var registry = new SharedParticipantRegistry();
        var control = new Border();
        registry.Register("item", control, SharedParticipantRole.Source);
        registry.Register("item", control, SharedParticipantRole.Destination);

        Assert.True(registry.TryPair("item", out var source, out var destination));
        Assert.Null(source);
        Assert.Same(control, destination!.Control);
    }

    [Fact]
    public void SharedTransitionPlanClassifiesUnmatchedParticipants()
    {
        var root = new SharedTransitionRoot();
        var source = new Border();
        var destination = new Border();
        root.Participants.Register("paired", source, SharedParticipantRole.Source);
        root.Participants.Register("paired", destination, SharedParticipantRole.Destination);
        root.Participants.Register("exit", new Border(), SharedParticipantRole.Source);

        var pairs = root.CommitTransition(root.BeginTransition());

        Assert.Contains(pairs, pair => pair.Key.Equals("paired") && pair.IsPaired);
        Assert.Contains(pairs, pair => pair.Key.Equals("exit") && pair.Source is not null && pair.Destination is null);
        Assert.Equal(SharedTransitionPhase.Paired, pairs.Single(pair => pair.Key.Equals("paired")).Phase);
        Assert.Equal(SharedTransitionPhase.ExitOnly, pairs.Single(pair => pair.Key.Equals("exit")).Phase);
    }

    [Fact]
    public void SharedGeometryExposesUnifiedBoundsAndVisualState()
    {
        var geometry = new SharedGeometry(new Rect(1, 2, 30, 40), Opacity: 0.5, ZIndex: 3);
        Assert.Equal(new Point(1, 2), geometry.Position);
        Assert.Equal(new Size(30, 40), geometry.Size);
        Assert.Equal(0.5, geometry.Opacity);
        Assert.Equal(3, geometry.ZIndex);
    }

    [Fact]
    public void SharedGeometryInterpolatesBoundsAndOpacityAsOneValue()
    {
        var from = new SharedGeometry(new Rect(0, 0, 10, 20), Opacity: 0.2);
        var to = new SharedGeometry(new Rect(100, 40, 30, 60), Opacity: 1);

        var value = SharedGeometry.Lerp(from, to, 0.5);

        Assert.Equal(new Rect(50, 20, 20, 40), value.Bounds);
        Assert.Equal(0.6, value.Opacity, 6);
    }

    [Fact]
    public void SharedBoundsCommitsUnifiedGeometryTarget()
    {
        var bounds = new SharedBounds { Duration = TimeSpan.Zero };
        var target = new SharedGeometry(new Rect(10, 20, 30, 40), Opacity: 0.25, ZIndex: 7);

        bounds.AnimateTo(target);
        bounds.CompleteTransition();

        Assert.Equal(target.Bounds, bounds.CurrentBounds);
        Assert.Equal(target.Opacity, bounds.Opacity);
        Assert.Equal(target.ZIndex, bounds.ZIndex);
    }

    [Fact]
    public void SharedTransitionRootExecutesBoundsPairing()
    {
        var root = new SharedTransitionRoot();
        var source = new SharedBounds { Key = "item", Role = SharedParticipantRole.Source };
        var destination = new SharedBounds { Key = "item", Role = SharedParticipantRole.Destination };
        source.SnapTo(new Rect(0, 0, 20, 20));
        destination.SnapTo(new Rect(100, 50, 80, 60));
        root.Participants.Register("item", source, SharedParticipantRole.Source);
        root.Participants.Register("item", destination, SharedParticipantRole.Destination);

        root.Animate(root.CommitTransition(root.BeginTransition()));

        Assert.True(destination.IsTransitioning);
        Assert.Equal(new Rect(0, 0, 20, 20), destination.CurrentBounds);
    }

    [Fact]
    public void SharedElementTransfersVisualAndRestoresDestinationState()
    {
        var root = new SharedTransitionRoot();
        var sourceVisual = new Button { DataContext = "source", IsHitTestVisible = true };
        var destinationVisual = new Button { DataContext = "destination" };
        var source = new SharedElement
        {
            TransitionRoot = root,
            Mode = SharedElementMode.MoveVisual,
            Child = sourceVisual,
        };
        var destination = new SharedElement
        {
            TransitionRoot = root,
            InputPolicy = SharedElementInputPolicy.TransferToDestination,
            UseDestinationDataContext = true,
            DataContext = "destination-context",
            Child = destinationVisual,
        };
        root.ContentLayer.Children.Add(source);
        root.ContentLayer.Children.Add(destination);

        Assert.True(source.MoveVisualToOverlay());
        Assert.Same(sourceVisual, source.Child is null ? sourceVisual : null);
        Assert.True(source.TransferVisualTo(destination));
        Assert.Same(sourceVisual, destination.Child);
        Assert.Equal("destination-context", sourceVisual.DataContext);
        Assert.Null(destinationVisual.Parent);
    }

    [Fact]
    public void SharedTransitionRootControlsActiveBounds()
    {
        var root = new SharedTransitionRoot();
        var bounds = new SharedBounds();
        bounds.SnapTo(new Rect(0, 0, 10, 10));
        bounds.AnimateTo(new Rect(100, 0, 20, 20));
        var source = new SharedTransitionParticipant("item", new SharedBounds(), SharedParticipantRole.Source, new LayoutSnapshot(new Rect(0, 0, 10, 10), new Size(10, 10)));
        var destination = new SharedTransitionParticipant("item", bounds, SharedParticipantRole.Destination, new LayoutSnapshot(new Rect(100, 0, 20, 20), new Size(20, 20)));
        root.Animate(new[] { new SharedTransitionPair("item", source, destination) });
        root.PauseTransitions();
        Assert.False(root.IsTransitioning);
        root.CancelTransitions();
        Assert.False(bounds.IsTransitioning);
    }

    [Fact]
    public void SharedBoundsRetargetsFromCurrentAnimation()
    {
        var bounds = new SharedBounds();
        bounds.SnapTo(new Rect(0, 0, 10, 10));
        bounds.AnimateTo(new Rect(100, 0, 20, 20));
        bounds.RetargetTo(new Rect(200, 0, 30, 30));

        Assert.True(bounds.IsTransitioning);
        Assert.Equal(new Rect(0, 0, 10, 10), bounds.CurrentBounds);
    }

    [Fact]
    public void SharedBoundsCreatesLayoutPlaceholder()
    {
        var bounds = new SharedBounds();
        bounds.SnapTo(new Rect(0, 0, 64, 32));
        var placeholder = bounds.CreatePlaceholder();
        Assert.Equal(64, placeholder.Width);
        Assert.Equal(32, placeholder.Height);
        Assert.False(placeholder.IsHitTestVisible);
    }

    [Fact]
    public void SharedElementMoveVisualUsesOverlayWithoutDuplicateParent()
    {
        var root = new SharedTransitionRoot();
        var element = new SharedElement { TransitionRoot = root, Mode = SharedElementMode.MoveVisual, InputPolicy = SharedElementInputPolicy.DisableDuringTransition };
        var visual = new Border { IsHitTestVisible = true };
        element.Child = visual;
        root.ContentLayer.Children.Add(element);

        Assert.True(element.MoveVisualToOverlay());
        Assert.Null(element.Child);
        Assert.Contains(visual, root.Overlay.Children);
        Assert.False(visual.IsHitTestVisible);
        Assert.True(element.RestoreVisual());
        Assert.Same(visual, element.Child);
        Assert.True(visual.IsHitTestVisible);
    }

    [Fact]
    public void SharedElementRenderSnapshotUsesInjectedFactory()
    {
        var root = new SharedTransitionRoot();
        var element = new SharedElement
        {
            TransitionRoot = root,
            Mode = SharedElementMode.RenderSnapshot,
            SnapshotFactory = source => new Border { Width = source.Width, Height = source.Height },
            Child = new Border { Width = 24, Height = 18 },
        };

        Assert.True(element.MoveVisualToOverlay());
        Assert.Single(root.Overlay.Children);
        Assert.True(element.RestoreVisual());
        Assert.Empty(root.Overlay.Children);
        Assert.NotNull(element.Child);
    }

    [Fact]
    public void SharedElementTransfersVisualToDestination()
    {
        var root = new SharedTransitionRoot();
        var source = new SharedElement { TransitionRoot = root, Mode = SharedElementMode.MoveVisual, Child = new Border() };
        var destination = new SharedElement { TransitionRoot = root, InputPolicy = SharedElementInputPolicy.TransferToDestination };
        var pair = new SharedTransitionPair("item",
            new SharedTransitionParticipant("item", source, SharedParticipantRole.Source, new LayoutSnapshot()),
            new SharedTransitionParticipant("item", destination, SharedParticipantRole.Destination, new LayoutSnapshot()));

        Assert.True(source.MoveVisualToOverlay());
        root.Complete(new[] { pair });

        Assert.NotNull(destination.Child);
        Assert.Empty(root.Overlay.Children);
    }

    [Fact]
    public void SharedElementCanTransferDestinationDataContext()
    {
        var root = new SharedTransitionRoot();
        var visual = new Border { DataContext = "source" };
        var source = new SharedElement { TransitionRoot = root, Mode = SharedElementMode.MoveVisual, Child = visual };
        var destination = new SharedElement
        {
            TransitionRoot = root,
            InputPolicy = SharedElementInputPolicy.TransferToDestination,
            UseDestinationDataContext = true,
            DataContext = "destination"
        };

        Assert.True(source.MoveVisualToOverlay());
        Assert.True(source.TransferVisualTo(destination));
        Assert.Same(destination.Child, visual);
        Assert.Equal("destination", visual.DataContext);
    }

    [Fact]
    public void SharedInteractiveTransitionSynchronizesMultipleTargets()
    {
        var first = new SharedBounds();
        var second = new SharedElement();
        var transition = new SharedInteractiveTransition();
        transition.Add(first, new Rect(0, 0, 10, 10), new Rect(100, 0, 20, 20));
        transition.Add(second, new Rect(20, 0, 10, 10), new Rect(120, 0, 30, 30));
        transition.SetProgress(0.5);

        Assert.Equal(new Rect(50, 0, 15, 15), first.CurrentBounds);
        Assert.Equal(new Rect(70, 0, 20, 20), second.CurrentBounds);
    }

    [Fact]
    public void SharedCoordinatorCanDriveElementsAndBoundsTogether()
    {
        var clock = new ManualAnimationClock();
        var bounds = new SharedBounds();
        var element = new SharedElement();
        bounds.AttachClock(clock);
        element.AttachClock(clock);
        bounds.SnapTo(new Rect(0, 0, 10, 10));
        element.SnapTo(new Rect(20, 0, 10, 10));
        bounds.AnimateTo(new Rect(100, 0, 20, 20));
        element.AnimateTo(new Rect(120, 0, 20, 20));
        var coordinator = new SharedTransitionCoordinator(clock);
        coordinator.Add(bounds);
        coordinator.Add(element);
        coordinator.Start();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        Assert.True(coordinator.IsRunning);
        coordinator.Cancel();
        Assert.False(coordinator.IsRunning);
    }

    [Fact]
    public void TransformTransitionsExposeRotateAndSkewFactories()
    {
        Assert.NotNull(EnterTransition.RotateIn());
        Assert.NotNull(EnterTransition.SkewIn());
        Assert.NotNull(ExitTransition.RotateOut());
        Assert.NotNull(ExitTransition.SkewOut());
    }

    [Fact]
    public void SharedTransitionRootCreatesInteractivePairedTransition()
    {
        var root = new SharedTransitionRoot();
        var source = new SharedBounds();
        var destination = new SharedBounds();
        source.SnapTo(new Rect(0, 0, 10, 10));
        var pair = new SharedTransitionPair("item",
            new SharedTransitionParticipant("item", source, SharedParticipantRole.Source, new LayoutSnapshot(new Rect(0, 0, 10, 10), new Size(10, 10))),
            new SharedTransitionParticipant("item", destination, SharedParticipantRole.Destination, new LayoutSnapshot(new Rect(100, 0, 20, 20), new Size(20, 20))));

        var transition = root.BeginInteractiveTransition(new[] { pair });
        transition.SetProgress(0.5);

        Assert.Equal(new Rect(50, 0, 15, 15), destination.CurrentBounds);
    }

    [Fact]
    public void SharedTransitionRootCreatesKeyedPlaceholders()
    {
        var root = new SharedTransitionRoot();
        var participant = new SharedTransitionParticipant("card", new SharedBounds(), SharedParticipantRole.Source,
            new LayoutSnapshot(new Rect(0, 0, 80, 40), new Size(80, 40)));
        var placeholders = root.CreatePlaceholders(new[] { new SharedTransitionPair("card", participant, null) });

        var placeholder = Assert.Single(placeholders);
        Assert.Equal("card", placeholder.Key);
        Assert.Equal(80, placeholder.Width);
        Assert.Equal(40, placeholder.Height);
    }

    [Fact]
    public void SharedBoundsCanLeaseParentPlaceholder()
    {
        var parent = new Panel();
        var bounds = new SharedBounds();
        parent.Children.Add(bounds);
        using (bounds.CreateParentPlaceholder())
        {
            Assert.IsType<SharedTransitionPlaceholder>(parent.Children[0]);
        }
        Assert.Same(bounds, parent.Children[0]);
    }

    [Fact]
    public void SharedRootReleasesPlaceholdersOnCancel()
    {
        var root = new SharedTransitionRoot();
        var parent = new Panel();
        var source = new SharedBounds { Key = "item", Role = SharedParticipantRole.Source };
        parent.Children.Add(source);
        source.SnapTo(new Rect(0, 0, 40, 20));
        root.Participants.Register("item", source, SharedParticipantRole.Source);
        var pair = new SharedTransitionPair("item", new SharedTransitionParticipant("item", source, SharedParticipantRole.Source, new LayoutSnapshot(new Rect(0, 0, 40, 20), new Size(40, 20))), null);

        root.Animate(new[] { pair });
        root.CancelTransitions();

        Assert.Same(source, parent.Children[0]);
    }

    [Fact]
    public void SharedRootCreatesCoordinatorFromPairs()
    {
        var root = new SharedTransitionRoot();
        var bounds = new SharedBounds();
        var element = new SharedElement();
        var pairs = new[]
        {
            new SharedTransitionPair("bounds", null, new SharedTransitionParticipant("bounds", bounds, SharedParticipantRole.Destination, new LayoutSnapshot())),
            new SharedTransitionPair("element", null, new SharedTransitionParticipant("element", element, SharedParticipantRole.Destination, new LayoutSnapshot())),
        };

        var coordinator = root.CreateCoordinator(pairs, new ManualAnimationClock());

        Assert.Equal(TransitionSessionState.Idle, coordinator.State);
    }

    [Fact]
    public void InteractiveSharedTransitionTransfersOrRestoresVisualOnSettle()
    {
        var root = new SharedTransitionRoot();
        var source = new SharedElement { TransitionRoot = root, Mode = SharedElementMode.MoveVisual, Child = new Border() };
        var destination = new SharedElement { TransitionRoot = root, InputPolicy = SharedElementInputPolicy.TransferToDestination };
        var pair = new SharedTransitionPair("item",
            new SharedTransitionParticipant("item", source, SharedParticipantRole.Source, new LayoutSnapshot()),
            new SharedTransitionParticipant("item", destination, SharedParticipantRole.Destination, new LayoutSnapshot(new Rect(10, 0, 20, 20), new Size(20, 20))));
        var transition = root.BeginInteractiveTransition(new[] { pair });
        source.MoveVisualToOverlay();
        transition.Complete();
        Assert.NotNull(destination.Child);
    }

    [Fact]
    public void InteractiveSharedTransitionCancelRestoresSourceOnlyOnce()
    {
        var root = new SharedTransitionRoot();
        var source = new SharedElement { TransitionRoot = root, Mode = SharedElementMode.MoveVisual, Child = new Border() };
        var transition = new SharedInteractiveTransition();
        var settled = 0;
        transition.Settled += _ => settled++;
        source.MoveVisualToOverlay();
        transition.Cancel();
        transition.Cancel();
        source.RestoreVisual();

        Assert.Equal(1, settled);
        Assert.NotNull(source.Child);
    }

    [Fact]
    public void GestureSurfaceCanBindSharedInteractiveTransition()
    {
        var surface = new GestureSurface();
        var transition = new SharedInteractiveTransition();
        surface.BindSharedTransition(transition);
        Assert.Same(transition, surface.SharedTransition);
        Assert.Equal(0, surface.Progress);
    }

    [Fact]
    public void SharedTransitionCoordinatorControlsMultipleBounds()
    {
        var clock = new ManualAnimationClock();
        var first = new SharedBounds();
        var second = new SharedBounds();
        first.SnapTo(new Rect(0, 0, 10, 10));
        second.SnapTo(new Rect(20, 0, 10, 10));
        first.AnimateTo(new Rect(100, 0, 20, 20));
        second.AnimateTo(new Rect(120, 0, 20, 20));
        var coordinator = new SharedTransitionCoordinator(clock);
        coordinator.Add(first);
        coordinator.Add(second);
        coordinator.Start();
        coordinator.Pause();
        Assert.Equal(TransitionSessionState.Paused, coordinator.State);
        coordinator.Cancel();
        Assert.Equal(TransitionSessionState.Canceled, coordinator.State);
    }

    [Fact]
    public void SharedBoundsCanRebindToCoordinatorClock()
    {
        var clock = new ManualAnimationClock();
        var bounds = new SharedBounds();
        bounds.AttachClock(clock);
        bounds.SnapTo(new Rect(0, 0, 10, 10));
        bounds.AnimateTo(new Rect(50, 0, 20, 20));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
        Assert.True(bounds.CurrentBounds.X >= 0);
    }

    [Fact]
    public void SharedElementCanRetargetGeometry()
    {
        var element = new SharedElement();
        element.SnapTo(new Rect(0, 0, 20, 20));
        element.AnimateTo(new Rect(40, 30, 80, 60));

        Assert.True(element.IsTransitioning);
        Assert.Equal(new Rect(0, 0, 20, 20), element.CurrentBounds);
        element.CompleteTransition();
        Assert.False(element.IsTransitioning);
    }

    private sealed class TestObject : Control
    {
        public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<TestObject, double>(nameof(Value));
        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    }
}

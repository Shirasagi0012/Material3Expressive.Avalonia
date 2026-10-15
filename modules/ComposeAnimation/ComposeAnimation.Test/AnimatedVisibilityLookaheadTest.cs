using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatedVisibilityLookaheadTest
{
    private static TweenSpec<T> Linear<T>(int duration = 100)
        => new(TimeSpan.FromMilliseconds(duration), easing: new LinearEasing());

    private sealed class Fixture : IDisposable
    {
        internal Transition<bool> Parent { get; }
        internal AnimatedVisibility Visibility { get; }
        internal LookaheadBox Content { get; } = new() { Width = 200, Height = 200 };
        internal LookaheadBox Header { get; } = new() { Width = 200, Height = 40 };
        internal LookaheadRoot Root { get; }
        internal AnimationTestWindow Window { get; }

        internal Fixture(bool visible = true, Transition<bool>? parent = null)
        {
            Parent = parent ?? new Transition<bool>(visible);
            Visibility = new AnimatedVisibility
            {
                Source = VisibilitySource.From(Parent, value => value),
                Enter = new ExpandVertically { ExpandFrom = VerticalAlignment.Top, Spec = Linear<Size>() },
                Exit = new ShrinkVertically { ShrinkTowards = VerticalAlignment.Top, Spec = Linear<Size>() },
                Content = Content,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            Root = new LookaheadRoot
            {
                Child = new LookaheadStackPanel { Children = { Visibility, Header } }
            };
            Window = new AnimationTestWindow(new ManualFrameSource()) { Width = 500, Height = 400, Content = Root };
            Window.Show();
            Layout();
            if (Visibility.IsContentPresent) Frame(0);
        }

        internal EnterExitLayout LayoutControl => Visibility.GetVisualDescendants().OfType<EnterExitLayout>().First();
        internal void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Assert.Null(Window.AnimationRoot.Diagnostics.LastError);
        }
        internal void Frame(int milliseconds)
        {
            Parent.OnFrame(TimeSpan.FromMilliseconds(milliseconds));
            Layout();
        }
        public void Dispose()
        {
            Window.Close();
            Parent.Dispose();
        }
    }

    [Fact]
    public Task Collapse_PredictsFinalHeaderImmediately_WhileActualHeaderKeepsMoving() => OnUi(() =>
    {
        using var fixture = new Fixture();
        Assert.Equal(200, fixture.Header.Bounds.Y);
        fixture.Parent.UpdateTarget(false);
        fixture.Layout();
        var snapshot = Assert.IsType<LookaheadLayoutSnapshot>(fixture.Root.Snapshot);
        Assert.Equal(default, snapshot.Results[fixture.Visibility].DesiredSize);
        Assert.Equal(0, snapshot.Results[fixture.Header].Bounds.Y);
        Assert.Equal(new Size(200, 200), snapshot.Results[fixture.Content].Bounds.Size);
        Assert.True(fixture.Visibility.IsContentPresent);
        Assert.Equal(200, fixture.Header.Bounds.Y);

        fixture.Frame(16);
        fixture.Frame(66);
        Assert.InRange(fixture.Header.Bounds.Y, 99, 101);
        Assert.Same(snapshot, fixture.Root.Snapshot);
        Assert.Equal(0, snapshot.Results[fixture.Header].Bounds.Y);
        fixture.Frame(116);
        Assert.False(fixture.Visibility.IsContentPresent);
        Assert.Equal(0, fixture.Header.Bounds.Y);
    });

    [Fact]
    public Task HiddenTarget_DropsOwnMarginAndMinimum_ButRetainsMeasuredDescendants() => OnUi(() =>
    {
        using var fixture = new Fixture();
        fixture.Visibility.Margin = new Thickness(12);
        fixture.Visibility.MinHeight = 300;
        fixture.Layout();
        fixture.Parent.UpdateTarget(false);
        fixture.Layout();
        var snapshot = fixture.Root.Snapshot!;
        Assert.Equal(default, snapshot.Results[fixture.Visibility].DesiredSize);
        Assert.Equal(default, snapshot.Results[fixture.Visibility].Bounds.Size);
        Assert.Equal(0, snapshot.Results[fixture.Header].Bounds.Y);
        Assert.True(snapshot.Results.ContainsKey(fixture.Content));
        Assert.True(fixture.Visibility.DesiredSize.Height >= 300);
    });

    [Fact]
    public Task Reentry_RebuildsFullTarget_AfterAnActualZeroFootprintMeasurement() => OnUi(() =>
    {
        using var fixture = new Fixture(visible: false);
        Assert.False(fixture.Visibility.IsContentPresent);
        Assert.Equal(0, fixture.Header.Bounds.Y);
        fixture.Parent.UpdateTarget(true);
        fixture.Layout();
        Assert.Equal(200, fixture.Root.Snapshot!.Results[fixture.Header].Bounds.Y);
        Assert.True(fixture.Visibility.IsContentPresent);
        fixture.Frame(16);
        fixture.Frame(66);
        Assert.InRange(fixture.Header.Bounds.Y, 99, 101);
    });

    [Fact]
    public Task TargetPreparation_RealizesTemplateOnce_WithoutCreatingAnimationChannels() => OnUi(() =>
    {
        using var fixture = new Fixture(visible: false);
        var builds = 0;
        fixture.Visibility.Content = "model";
        fixture.Visibility.ContentTemplate = new FuncDataTemplate<string>((_, _) =>
        {
            builds++;
            return new LookaheadBox { Width = 100, Height = 80 };
        });
        fixture.Layout();
        Assert.Equal(0, builds);
        var observedTarget = false;
        fixture.Root.TargetLayoutUpdated += (_, _) =>
        {
            if (fixture.Visibility.Scope is not { } scope) return;
            observedTarget = true;
            Assert.Equal(1, builds);
            Assert.Empty(scope.Transition.Animations);
            Assert.False(scope.IsInitialLayoutReady);
        };
        fixture.Parent.UpdateTarget(true);
        fixture.Layout();
        Assert.True(observedTarget);
        Assert.Equal(1, builds);
        Assert.NotEmpty(fixture.Visibility.Scope!.Transition.Animations);
    });

    [Fact]
    public Task TemplateReplacement_PreservesMotionChannelsValuesAndScope() => OnUi(() =>
    {
        using var fixture = new Fixture();
        fixture.Visibility.Content = "model";
        fixture.Visibility.ContentTemplate = new FuncDataTemplate<string>((_, _) => new LookaheadBox { Width = 200, Height = 200 });
        fixture.Layout();
        fixture.Frame(1);
        fixture.Parent.UpdateTarget(false);
        fixture.Layout();
        fixture.Frame(16);
        fixture.Frame(66);
        var scope = fixture.Visibility.Scope;
        var motion = fixture.LayoutControl.Motion;
        var channels = scope!.Transition.Animations.ToArray();
        var height = fixture.Visibility.DesiredSize.Height;
        fixture.Visibility.Template = new FuncControlTemplate<AnimatedVisibility>((owner, names) =>
        {
            var presenter = new EnterExitContentPresenter
            {
                Name = "PART_ContentPresenter", Content = owner.Content, ContentTemplate = owner.ContentTemplate
            };
            var layout = new EnterExitLayout { Name = "PART_EnterExitLayout" };
            layout.Surface.Child = presenter;
            names.Register(presenter.Name, presenter);
            names.Register(layout.Name, layout);
            return layout;
        });
        fixture.Layout();
        Assert.Same(scope, fixture.Visibility.Scope);
        Assert.Same(motion, fixture.LayoutControl.Motion);
        Assert.Equal(channels, scope.Transition.Animations);
        Assert.Equal(height, fixture.Visibility.DesiredSize.Height);
        fixture.Frame(116);
        Assert.False(fixture.Visibility.IsContentPresent);
    });

    [Fact]
    public Task DeferredVisibleTarget_PreparesContent_AndCancellationExitsIt() => OnUi(() =>
    {
        var state = new DeferredTransitionState<bool>(false);
        using var fixture = new Fixture(visible: false, new DeferredTransition<bool>(state));
        state.Defer(true);
        fixture.Layout();
        Assert.True(fixture.Visibility.IsContentPresent);
        Assert.False(fixture.Visibility.TargetVisible);
        Assert.Equal(EnterExitState.PreEnter, fixture.Visibility.Scope!.Transition.TargetState);
        Assert.Equal(0, fixture.Root.Snapshot!.Results[fixture.Header].Bounds.Y);
        state.AnimateTo(false);
        fixture.Layout();
        fixture.Frame(16);
        fixture.Frame(116);
        Assert.False(fixture.Visibility.IsContentPresent);
    });

    [Fact]
    public Task DeferredVisibleTarget_CommitKeepsThePreparedScopeAndEntersNormally() => OnUi(() =>
    {
        var state = new DeferredTransitionState<bool>(false);
        using var fixture = new Fixture(visible: false, new DeferredTransition<bool>(state));
        state.Defer(true);
        fixture.Layout();
        var scope = fixture.Visibility.Scope;
        state.AnimateTo(true);
        fixture.Layout();
        Assert.Same(scope, fixture.Visibility.Scope);
        Assert.Equal(200, fixture.Root.Snapshot!.Results[fixture.Header].Bounds.Y);
        fixture.Frame(16);
        fixture.Frame(66);
        Assert.InRange(fixture.Header.Bounds.Y, 99, 101);
        fixture.Frame(116);
        Assert.True(fixture.Visibility.IsIdle);
    });

    [Fact]
    public Task ZeroDurationExit_RetainsTargetDescendantsUntilPreparationIsReleased() => OnUi(() =>
    {
        using var fixture = new Fixture();
        using var preparation = fixture.Visibility.Scope!.BeginPreparation();
        fixture.Parent.DurationScale = 0;
        fixture.Parent.UpdateTarget(false);
        fixture.Layout();
        fixture.Frame(16);
        Assert.True(fixture.Visibility.IsContentPresent);
        Assert.Equal(0, fixture.Root.Snapshot!.Results[fixture.Header].Bounds.Y);
        Assert.True(fixture.Root.Snapshot.Results.ContainsKey(fixture.Content));
        preparation.Dispose();
        fixture.Frame(32);
        Assert.False(fixture.Visibility.IsContentPresent);
        Assert.False(fixture.Root.Snapshot!.Results.ContainsKey(fixture.Content));
        LookaheadMeasure.Measure(fixture.Visibility, LookaheadConstraints.Fixed(new Size(200, 300)));
        Assert.Equal(default, fixture.Visibility.DesiredSize);
    });

    [Fact]
    public Task ReplacingExitAfterReversal_RetainsOldSizeEffectAtItsNeutralTarget() => OnUi(() =>
    {
        using var fixture = new Fixture();
        fixture.Visibility.Enter = EnterTransition.None;
        fixture.Parent.UpdateTarget(false);
        fixture.Layout();
        fixture.Frame(16);
        fixture.Frame(66);
        fixture.Parent.UpdateTarget(true);
        fixture.Layout();
        fixture.Visibility.Exit = new SlideOutHorizontally { Spec = Linear<Vector>() };
        fixture.Parent.UpdateTarget(false);
        fixture.Layout();
        Assert.True(fixture.LayoutControl.Motion!.ChangesSize);
        var size = fixture.Visibility.Scope!.Transition.Animations.Single(animation => animation.Label.EndsWith(" size"));
        Assert.Equal(new Size(200, 200), Assert.IsType<Transition<EnterExitState>.TransitionAnimationState<Size>>(size).TargetValue);
    });
}

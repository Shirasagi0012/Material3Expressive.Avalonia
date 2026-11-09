using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public sealed class AnimatedVisibilityTargetCommitTest
{
    private sealed class Fixture : IDisposable
    {
        internal ManualFrameSource Frames { get; } = new();
        internal AnimatedVisibility Host { get; } = new()
        {
            Visible = false, TargetCommitPhase = AnimationCommitPhase.AfterFrame,
            Content = new Border { Width = 80, Height = 40 },
            Enter = new FadeIn { Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()) },
            Exit = new FadeOut { Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()) }
        };
        internal AnimationTestWindow Window { get; }
        internal Grid Root { get; } = new();
        internal Fixture()
        {
            Root.Children.Add(Host);
            Window = new AnimationTestWindow(Frames) { Width = 240, Height = 120, Content = Root };
            Window.Show(); Layout();
        }
        internal void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        internal void Step(int milliseconds) { Frames.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds)); Layout(); }
        internal double Opacity => Host.GetVisualDescendants().OfType<EnterExitLayout>().Single().Surface.Opacity;
        public void Dispose() => Window.Close();
    }

    [Fact]
    public Task LocalTargetCoalescesAndStartsSamplingAfterItsCommitFrame() => OnUi(() =>
    {
        using var f = new Fixture();
        f.Host.Visible = true; f.Host.Visible = false; f.Host.Visible = true;
        Assert.False(f.Host.TargetVisible); Assert.False(f.Host.IsIdle); Assert.Null(f.Host.Scope);
        Assert.Equal(1, f.Frames.PendingFrameCount);
        f.Step(0);
        Assert.True(f.Host.TargetVisible); Assert.Equal(0, f.Opacity);
        f.Step(50);
        Assert.Equal(0, f.Opacity);
        f.Step(50);
        Assert.Equal(0.5, f.Opacity, 8);
    });

    [Fact]
    public Task ReentrantEffectBelongsToTheFollowingCommitBatch() => OnUi(() =>
    {
        using var f = new Fixture();
        using var effect = f.Window.AnimationRoot.ScheduleFrameCommit(() => f.Host.Visible = false);
        f.Host.Visible = true;
        f.Step(0);
        Assert.False(f.Host.Visible);
        Assert.True(f.Host.TargetVisible);
        Assert.False(f.Host.IsIdle);
        f.Step(16);
        Assert.False(f.Host.TargetVisible);
    });

    [Fact]
    public Task DetachCancelsPendingTargetAndExternalSourceKeepsItsOwnTiming() => OnUi(() =>
    {
        using var f = new Fixture();
        f.Host.Visible = true;
        f.Root.Children.Remove(f.Host); f.Layout();
        Assert.Equal(0, f.Frames.PendingFrameCount);
        Assert.Null(f.Host.Scope);
        f.Root.Children.Add(f.Host); f.Layout();
        Assert.True(f.Host.TargetVisible);
        var state = new MutableTransitionState<bool>(false);
        f.Host.Source = VisibilitySource.From(state); f.Layout();
        f.Host.Visible = false; f.Host.Visible = true;
        Assert.False(f.Host.TargetVisible);
        state.TargetState = true;
        Assert.True(f.Host.TargetVisible);
        f.Host.Source = null;
        f.Host.Visible = false;
        Assert.True(f.Host.TargetVisible);
        f.Host.TargetCommitPhase = AnimationCommitPhase.BeforeFrame;
        Assert.False(f.Host.TargetVisible);
    });
}

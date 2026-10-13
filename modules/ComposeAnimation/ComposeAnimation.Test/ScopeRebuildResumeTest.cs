using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

/// <summary>D17: components re-run their kept state in the rebuilt scope instead of restarting.</summary>
public sealed class ScopeRebuildResumeTest
{
    private static TweenSpec<double> Linear(int ms) => new(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing());

    internal sealed class Stage : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public Decorator Left { get; } = new();
        public Decorator Right { get; } = new();
        public AnimationTestWindow Window { get; }
        public Stage(Control content)
        {
            Left.Child = content;
            Window = new AnimationTestWindow(Frames) { Width = 400, Height = 400, Content = new StackPanel { Children = { Left, Right } } };
            Window.Show();
            Layout();
            AnimationScope.Get(Left); AnimationScope.Get(Right);
            AnimationScope.Get(content);
        }
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        public void Step(int ms) { Layout(); Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() => Window.Close();
    }

    private static AnimatedVisibility Visibility() => new()
    {
        Visible = false,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Content = new Border { Width = 50, Height = 40, Background = Brushes.Red },
        Enter = new FadeIn { Spec = Linear(100) },
        Exit = new FadeOut { Spec = Linear(100) },
    };

    private static double Opacity(AnimatedVisibility host)
        => host.GetVisualDescendants().OfType<VisibilityLayer>().First().Surface.Opacity;

    [Fact]
    public Task AnimatedVisibility_ReparentedMidEnter_ContinuesFromStoredPlayTimeAtTheDrainFrame() => OnUi(() =>
    {
        var host = Visibility();
        using var stage = new Stage(host);
        host.Visible = true;
        stage.Step(0);
        stage.Step(16); stage.Step(16); stage.Step(16);
        var scope = host.Scope;
        var before = Opacity(host);
        Assert.InRange(before, 0.2, 0.8);
        var oldMotion = AnimationScope.Get(host);

        stage.Left.Child = null;
        stage.Right.Child = host;
        stage.Step(16); // the drain runs in this frame's before-frame hook; the run joins the batch
        Assert.NotSame(oldMotion, AnimationScope.Get(host));
        Assert.Same(scope, host.Scope); // kept, not reset
        Assert.Equal(before, Opacity(host), 8); // sampled at the drain frame from the stored play time
        stage.Step(16);
        Assert.Equal(before + 0.16, Opacity(host), 6); // no skipped frame
        for (var i = 0; i < 20 && !host.IsIdle; i++) stage.Step(16);
        Assert.True(host.IsIdle);
        Assert.Equal(1, Opacity(host));
    });

    [Fact]
    public Task AnimatedVisibility_DetachedWithoutReturn_IsReleasedAtTheDrain() => OnUi(() =>
    {
        var host = Visibility();
        using var stage = new Stage(host);
        host.Visible = true;
        stage.Step(0); stage.Step(16);
        Assert.NotNull(host.Scope);
        stage.Left.Child = null;
        Assert.NotNull(host.Scope); // released at the drain, not on detach
        Dispatcher.UIThread.RunJobs(); // no frame: the fallback post drains
        Assert.Null(host.Scope);
        Assert.False(host.IsContentPresent);
        Assert.Equal(0, stage.Frames.PendingFrameCount);
    });

    [Fact]
    public Task AnimatedVisibility_DetachedWithoutReturn_IsReleasedAtTheDrainFrame() => OnUi(() =>
    {
        var host = Visibility();
        using var stage = new Stage(host);
        host.Visible = true;
        stage.Step(0); stage.Step(16);
        stage.Left.Child = null;
        stage.Frames.AdvanceBy(TimeSpan.FromMilliseconds(16)); // the before-frame hook drains
        Dispatcher.UIThread.RunJobs();
        Assert.Null(host.Scope);
        Assert.Equal(0, stage.Frames.PendingFrameCount);
    });

    [Fact]
    public Task AnimatedContent_ReparentedMidSwitch_ContinuesFromStoredPlayTime() => OnUi(() =>
    {
        var enter = Linear(100);
        var host = new AnimatedContent
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Content = "A",
            ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 30, Height = 10 }),
            ContentTransform = new ContentTransform(new FadeIn { Spec = enter }, new FadeOut { Spec = Linear(100) }).Using(null),
        };
        using var stage = new Stage(host);
        stage.Step(0);
        host.Content = "B";
        stage.Step(0);
        stage.Step(16); stage.Step(16); stage.Step(16);
        double Incoming() => host.GetVisualDescendants().OfType<ContentSlot>().First(s => Equals(s.State, "B")).Layer.Surface.Opacity;
        var before = Incoming();
        Assert.InRange(before, 0.2, 0.8);

        stage.Left.Child = null;
        stage.Right.Child = host;
        stage.Step(16);
        Assert.Equal(before, Incoming(), 8);
        stage.Step(16);
        Assert.Equal(before + 0.16, Incoming(), 6);
    });

    [Fact]
    public Task ReentrantGetInScopeChanged_RunningAgain_LeavesOneLiveRun() => OnUi(() =>
    {
        var control = new Border();
        using var stage = new Stage(control);
        var value = Animatable.Create(0d);
        var runs = new List<Task>();
        void Start(AnimationScope scope) => runs.Add(value.AnimateToAsync(scope, 100, Linear(1000)));
        Start(AnimationScope.Get(control));
        AnimationScope.AddScopeChangedHandler(control, (_, e) =>
        {
            if (e.NewScope is null) return;
            Start(e.NewScope);
            Start(AnimationScope.Get(control)); // re-entrant Get, runs again: takes over
        });
        stage.Step(0); stage.Step(100);
        var before = value.Value;
        Assert.Equal(10, before, 6);
        stage.Left.Child = null; stage.Right.Child = control;
        stage.Step(16);
        stage.Step(16);
        Assert.Equal(3, runs.Count);
        Assert.All(runs.Take(2), r => Assert.True(r.IsCanceled));
        Assert.False(runs[2].IsCompleted);
        Assert.True(value.IsRunning);
        // value continued from where it stopped (no jump back to 0)
        Assert.True(value.Value > before);
        value.Stop();
    });
}

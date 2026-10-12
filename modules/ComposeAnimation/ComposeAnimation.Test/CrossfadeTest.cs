using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class CrossfadeTest
{
    private static TweenSpec<double> Linear(int milliseconds)
        => new(TimeSpan.FromMilliseconds(milliseconds), easing: new LinearEasing());
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Clock { get; } = new();
        public Crossfade Host { get; }
        public Window Window { get; }
        public Fixture(Crossfade host)
        {
            Host = host;

            Window = new AnimationTestWindow(Clock) { Width = 400, Height = 400, Content = host };
            Window.Show();
            Layout();
        }
        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }
        public void Step(int milliseconds)
        {
            Layout();
            Assert.True(SpinWait.SpinUntil(() =>
            {
                Layout();
                return Host.IsIdle || Clock.PendingFrameCount > 0;
            }, TimeSpan.FromSeconds(5)));
            Clock.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds));
            Layout();
        }
        public void Finish()
        {
            for (var i = 0; i < 200 && (!Host.IsIdle || Clock.PendingFrameCount > 0); i++) Step(16);
            Assert.True(Host.IsIdle);
            Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(Host)!).AnimationRoot.Diagnostics.LastError);
        }
        public void Dispose() => Window.Close();
    }

    private static Crossfade Host() => new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Content = "A",
        ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 40, Height = 20 }),
        Spec = Linear(300)
    };

    private static IReadOnlyList<ContentSlot> Slots(Crossfade host)
        => host.GetVisualDescendants().OfType<ContentSlot>().ToList();

    [Fact]
    public Task Reverse_UsesCrossfadeSpecForBothSlotsInsteadOfVisibilityFallback() => OnUi(() =>
    {
        var host = Host();
        var spec = new SpringSpec<double>(stiffness: 1600, visibilityThreshold: 0.01);
        host.Spec = spec;
        using var fixture = new Fixture(host);
        fixture.Finish();
        host.Content = "B";
        fixture.Step(16); fixture.Step(48);
        host.Content = "A"; fixture.Layout();
        var slots = Slots(host);
        Assert.Equal(2, slots.Count);
        foreach (var slot in slots)
        {
            var alpha = (Transition<EnterExitState>.TransitionAnimationState<double>)
                slot.Scope!.Transition.Animations.Single(a => a.Label.EndsWith(" alpha"));
            Assert.Same(spec, alpha.AnimationSpec);
            var expected = new TargetBasedAnimation<double>(spec, new DoubleConverter(), alpha.Value,
                slot.IsTarget ? 1 : 0, alpha.Velocity);
            var currentTime = slot.Scope.Transition.PlayTime;
            Assert.Equal(expected.GetValue(TimeSpan.FromMilliseconds(16)),
                alpha.Animation.GetValue(currentTime + TimeSpan.FromMilliseconds(16)), 8);
        }
        fixture.Finish(); Assert.Single(Slots(host));
    });

    [Fact]
    public Task Midpoint_Linear300ms_BothAboutHalf() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        f.Finish();
        f.Host.Content = "B";
        f.Step(16);
        f.Step(150);
        Assert.Equal(2, f.Host.VisibleSlotCount);
        var outgoing = Slots(f.Host).First(s => Equals(s.State, "A"));
        var incoming = Slots(f.Host).First(s => Equals(s.State, "B"));
        Assert.InRange(outgoing.Layer.Surface.Opacity, 0.4, 0.6);
        Assert.InRange(incoming.Layer.Surface.Opacity, 0.4, 0.6);
        f.Finish();
    });

    [Fact]
    public Task Size_MaxThenSnap() => OnUi(() =>
    {
        var host = Host();
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) =>
            s == "A" ? new Border { Width = 100, Height = 80 } : new Border { Width = 40, Height = 20 });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Step(16);
        f.Step(16);
        Assert.Equal(2, host.VisibleSlotCount);
        Assert.Equal(100, host.DesiredSize.Width);
        f.Finish();
        Assert.Equal(40, host.DesiredSize.Width);
    });

    [Fact]
    public Task Spec_ReplacesBothEnterAndExit() => OnUi(() =>
    {
        var host = Host();
        host.Spec = Linear(80);
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Step(16);
        f.Step(40);
        var outgoing = Slots(host).First(s => Equals(s.State, "A"));
        Assert.InRange(outgoing.Layer.Surface.Opacity, 0.4, 0.6);
        f.Finish();
    });

    [Fact]
    public Task InheritedContentTransform_HasNoEffect() => OnUi(() =>
    {
        var host = Host();
        host.ContentTransform = (new FadeIn { Spec = new TweenSpec<double>(TimeSpan.FromMilliseconds(220), TimeSpan.FromMilliseconds(90), new LinearEasing()) })
            .TogetherWith(new FadeOut { Spec = Linear(90) });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Step(16);
        f.Step(150);
        var incoming = Slots(host).First(s => Equals(s.State, "B"));
        Assert.InRange(incoming.Layer.Surface.Opacity, 0.4, 0.6);
        f.Finish();
    });

    [Fact]
    public void CreateTransform_UsingNull_DoesNotMutateFadeThroughFactory()
    {
        var fade = ContentTransform.CreateFadeThrough();
        var cross = CrossfadeTransforms.CreateTransform(Linear(300));
        Assert.Null(cross.SizeTransform);
        Assert.NotNull(fade.SizeTransform);
    }
}

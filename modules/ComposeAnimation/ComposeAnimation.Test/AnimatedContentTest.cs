using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatedContentTest
{
    private static TweenSpec<T> Tween<T>(int milliseconds = 100, int delay = 0)
        => new(TimeSpan.FromMilliseconds(milliseconds), TimeSpan.FromMilliseconds(delay), new LinearEasing());
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private static ContentTransform LinearFade(int enterMs = 220, int enterDelay = 90, int exitMs = 90,
        SizeTransform? size = null)
    {
        var enter = Tween<double>(enterMs, enterDelay);
        var transform = (new FadeIn { Spec = enter } + new ScaleIn { InitialScale = 0.92, Spec = enter })
            .TogetherWith(new FadeOut { Spec = Tween<double>(exitMs) });
        return size is null ? transform : transform.Using(size);
    }

    private sealed class Fixture : IDisposable
    {
        public ManualAnimationFrameClock Clock { get; } = new();
        public AnimatedContent Host { get; }
        public Window Window { get; }
        public Fixture(AnimatedContent host)
        {
            Host = host;

            Window = new AnimationTestWindow(Clock) { Width = 500, Height = 500, Content = host };
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
            }, TimeSpan.FromSeconds(5)), "The animation loop did not request its next frame.");
            Clock.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds));
            Layout();
        }
        public void Finish()
        {
            for (var i = 0; i < 200 && (!Host.IsIdle || Clock.PendingFrameCount > 0); i++) Step(16);
            Assert.True(Host.IsIdle,
                $"Target={Host.TargetContent}, slots={Host.VisibleSlotCount}, idle={Host.IsIdle}, error={((AnimationTestWindow)TopLevel.GetTopLevel(Host)!).AnimationRoot.LastError}");
            Assert.Null(((AnimationTestWindow)TopLevel.GetTopLevel(Host)!).AnimationRoot.LastError);
        }
        public void Dispose() => Window.Close();
    }

    private static AnimatedContent Host(object? content = null) => new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Content = content ?? new Border { Width = 100, Height = 80, Background = Brushes.Red },
        ContentTransform = LinearFade()
    };

    private static IReadOnlyList<ContentSlot> Slots(AnimatedContent host)
        => host.GetVisualDescendants().OfType<ContentSlot>().ToList();
    private static Border SizeBox(AnimatedContent host)
        => host.GetVisualDescendants().OfType<Border>().First(x => x.Name == "PART_ContentPanel");

    [Fact]
    public Task InitiallyIdle_SingleSlotNoAnimation() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        f.Finish();
        Assert.Equal(1, f.Host.VisibleSlotCount);
        Assert.Single(Slots(f.Host));
        Assert.Equal(1, Slots(f.Host)[0].Layer.Surface.Opacity);
    });

    [Fact]
    public Task FadeThrough_At16ms_OutgoingStillPresentAndFading() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 40, Height = 20 });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Step(16);
        f.Step(16);
        Assert.Equal(2, host.VisibleSlotCount);
        var outgoing = Slots(host).First(s => Equals(s.State, "A"));
        Assert.InRange(outgoing.Layer.Surface.Opacity, 0.01, 0.99);
    });

    [Fact]
    public Task FadeThrough_At90ms_OutgoingGoneIncomingStillZero() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 40, Height = 20 });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Step(16);
        f.Step(90);
        Assert.Equal(1, host.VisibleSlotCount);
        Assert.Equal("B", Slots(host)[0].State);
        Assert.InRange(Slots(host)[0].Layer.Surface.Opacity, 0, 0.05);
    });

    [Fact]
    public Task FadeThrough_DefaultEasing_SmokeFinishes() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTransform = null;
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s });
        using var f = new Fixture(host);
        host.Content = "B";
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
        Assert.Equal("B", host.TargetContent);
    });

    [Fact]
    public Task SizeTransform_AnimatesContainer_NotMaxOfChildren() => OnUi(() =>
    {
        var host = Host();
        host.Content = new Border { Width = 100, Height = 80, Background = Brushes.Red };
        host.ContentTransform = LinearFade(exitMs: 200, size: new SizeTransform
        {
            Spec = Tween<Size>(200)
        });
        using var f = new Fixture(host);
        f.Finish();
        Assert.Equal(100, host.DesiredSize.Width);
        host.Content = new Border { Width = 40, Height = 40, Background = Brushes.Blue };
        f.Step(16);
        f.Step(100);
        Assert.InRange(host.DesiredSize.Width, 65, 75);
        Assert.True(host.DesiredSize.Width < 100);
        f.Finish();
        Assert.Equal(40, host.DesiredSize.Width);
    });

    [Fact]
    public Task SizeTransform_Clip_OnStretchedFixedHost() => OnUi(() =>
    {
        var host = Host();
        host.Width = 200;
        host.Height = 200;
        host.HorizontalAlignment = HorizontalAlignment.Stretch;
        host.VerticalAlignment = VerticalAlignment.Stretch;
        host.Content = new Border { Width = 100, Height = 80 };
        host.ContentTransform = LinearFade(exitMs: 200, size: new SizeTransform { Spec = Tween<Size>(200) });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = new Border { Width = 40, Height = 40 };
        f.Step(16);
        f.Step(100);
        var box = SizeBox(host);
        Assert.True(box.ClipToBounds);
        Assert.InRange(box.Bounds.Width, 65, 75);
        Assert.Equal(200, host.Bounds.Width);
        f.Finish();
    });

    [Fact]
    public Task Background_FollowsPartContentPanelBorder_NotViewport() => OnUi(() =>
    {
        var host = Host();
        host.Width = 200;
        host.Height = 200;
        host.Background = Brushes.Green;
        host.Content = new Border { Width = 100, Height = 80 };
        using var f = new Fixture(host);
        f.Finish();
        var box = SizeBox(host);
        Assert.Equal(Brushes.Green, box.Background);
        Assert.InRange(box.Bounds.Width, 99, 101);
        Assert.Equal(200, host.Bounds.Width);
    });

    [Fact]
    public Task SizeTransform_Null_MaxThenSnap() => OnUi(() =>
    {
        var host = Host();
        host.Content = new Border { Width = 100, Height = 80 };
        host.ContentTransform = LinearFade(exitMs: 200).Using(null);
        using var f = new Fixture(host);
        f.Finish();
        host.Content = new Border { Width = 40, Height = 20 };
        f.Step(16);
        f.Step(16);
        Assert.Equal(2, host.VisibleSlotCount);
        Assert.Equal(100, host.DesiredSize.Width);
        f.Finish();
        Assert.Equal(40, host.DesiredSize.Width);
    });

    [Fact]
    public Task Interrupt_AToBToA_ReusesSlotAndMotion() => OnUi(() =>
    {
        var created = 0;
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 40, Height = 20 });
        host.SlotCreated += (_, e) => { if (Equals(e.Content, "A")) created++; };
        using var f = new Fixture(host);
        f.Finish();
        var original = host.TargetScope;
        host.Content = "B";
        f.Step(16);
        f.Step(16);
        host.Content = "A";
        f.Layout();
        Assert.Equal(1, created);
        Assert.Same(original, Slots(host).First(s => Equals(s.State, "A")).Scope);
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
        Assert.Equal("A", host.TargetContent);
    });

    [Fact]
    public Task TransitionSpec_AToBThenC_OutgoingBUsesBToCExit() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 40, Height = 20 });
        host.TransitionSpec = scope =>
        {
            var exitMs = Equals(scope.InitialState, "B") && Equals(scope.TargetState, "C") ? 1000 : 80;
            return new FadeIn { Spec = Tween<double>(80) }.TogetherWith(new FadeOut { Spec = Tween<double>(exitMs) })
                .Using(null);
        };
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Step(16);
        f.Step(80);
        host.Content = "C";
        f.Layout();
        var states = string.Join(",", Slots(host).Select(s => s.State));
        Assert.True(Slots(host).Any(s => Equals(s.State, "B")), states);
        var b = Slots(host).First(s => Equals(s.State, "B"));
        Assert.InRange(b.Layer.Surface.Opacity, 0.8, 1);
        f.Step(16);
        f.Step(16);
        Assert.True(b.Layer.Surface.Opacity > 0.9, "B→C must use the 1000ms exit, not the 80ms A→B exit.");
        f.Finish();
    });

    [Fact]
    public Task ControlAsContent_ParentImmediatelyAfterSet_AndAfterAToB() => OnUi(() =>
    {
        var a = new Border { Width = 40, Height = 20, Name = "A" };
        var b = new Border { Width = 40, Height = 20, Name = "B" };
        var host = Host(a);
        host.ContentTransform = LinearFade().Using(null);
        using var f = new Fixture(host);
        Assert.IsType<ContentPresenter>(a.Parent);
        Assert.NotSame(host, a.Parent);
        host.Content = b;
        Assert.IsType<ContentPresenter>(a.Parent);
        Assert.IsType<ContentPresenter>(b.Parent);
        Assert.NotSame(a.Parent, b.Parent);
        f.Finish();
        Assert.Same(b, ((ContentPresenter)b.Parent!).Content);
    });

    [Fact]
    public Task ControlAsContent_OutgoingKeepsSameInstance() => OnUi(() =>
    {
        var a = new Border { Width = 40, Height = 20 };
        var host = Host(a);
        using var f = new Fixture(host);
        host.Content = new Border { Width = 30, Height = 10 };
        f.Step(16);
        f.Step(16);
        Assert.Contains(Slots(host), s => ReferenceEquals(s.Presenter.Content, a));
        f.Finish();
    });

    [Fact]
    public Task Template_InstantiatesTwice_FrozenDataContext() => OnUi(() =>
    {
        var built = new List<string?>();
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) =>
        {
            built.Add(s);
            return new TextBlock { Text = s };
        });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Layout();
        var names = built.Where(x => x is not null).ToList();
        Assert.Equal(2, names.Count);
        Assert.Contains("A", names);
        Assert.Contains("B", names);
        var outgoing = Slots(host).First(s => Equals(s.State, "A"));
        Assert.Equal("A", outgoing.Presenter.DataContext);
        f.Finish();
    });

    [Fact]
    public Task Template_NotBuiltForUnusedStates() => OnUi(() =>
    {
        var built = 0;
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) =>
        {
            built++;
            return new TextBlock { Text = s };
        });
        using var f = new Fixture(host);
        f.Finish();
        Assert.Equal(1, built);
    });

    [Fact]
    public Task ContentAlignment_TopStart_SmallContentNotStretched_On200WideStretchHost() => OnUi(() =>
    {
        var inner = new Border { Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Left };
        var host = Host(inner);
        host.Width = 200;
        host.Height = 200;
        host.HorizontalAlignment = HorizontalAlignment.Stretch;
        host.ContentTransform = LinearFade().Using(null);
        using var f = new Fixture(host);
        f.Finish();
        var slot = Slots(host)[0];
        Assert.InRange(slot.Bounds.Width, 9, 11);
        Assert.Equal(HorizontalAlignment.Left, slot.Presenter.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, slot.Presenter.VerticalContentAlignment);
    });

    [Fact]
    public Task ContentSlot_PresenterAlignmentIsLeftTop() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        var presenter = Slots(f.Host)[0].Presenter;
        Assert.Equal(HorizontalAlignment.Left, presenter.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, presenter.VerticalContentAlignment);
    });

    [Fact]
    public Task ExitNone_OutgoingAndIncoming_SameFrame() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 20, Height = 10 });
        host.ContentTransform = new ContentTransform(EnterTransition.None, ExitTransition.None, 0f, null);
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Layout();
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
        Assert.Equal("B", Slots(host)[0].State);
    });

    [Fact]
    public Task Preparation_IncomingNone_DoesNotCompleteBeforeLayout() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 20, Height = 10 });
        host.ContentTransform = new ContentTransform(EnterTransition.None, new FadeOut { Spec = Tween<double>(200) }, 0f, null);
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        Assert.Equal(2, host.VisibleSlotCount);
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
    });

    [Fact]
    public Task ReplaceTemplate_MissingPanel_Throws() => OnUi(() =>
    {
        var host = Host();
        using var f = new Fixture(host);
        var error = Assert.Throws<InvalidOperationException>(() =>
        {
            host.Template = new FuncControlTemplate<AnimatedContent>((_, _) => new Panel());
            host.ApplyTemplate();
            host.Content = new Border();
        });
        Assert.Contains("PART_ContentPanel", error.Message);
    });

    [Fact]
    public Task ReplaceTemplate_ExtraPartContentPresenter_Throws() => OnUi(() =>
    {
        var host = Host();
        using var f = new Fixture(host);
        Assert.Throws<InvalidOperationException>(() =>
        {
            host.Template = new FuncControlTemplate<AnimatedContent>((_, names) =>
            {
                var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
                names.Register(presenter.Name, presenter);
                var panel = new ContentSlotPanel();
                var box = new Border { Name = "PART_ContentPanel", Child = panel };
                names.Register(box.Name, box);
                return box;
            });
            host.ApplyTemplate();
        });
    });

    [Fact]
    public Task ExpandIn_DoesNotFeedInFlightSizeToSizeTransform() => OnUi(() =>
    {
        var host = Host();
        host.Content = new Border { Width = 100, Height = 80 };
        host.ContentTransform = (new ExpandIn { Spec = Tween<Size>(200) } + new FadeIn { Spec = Tween<double>(200) })
            .TogetherWith(new FadeOut { Spec = Tween<double>(200) })
            .Using(new SizeTransform { Spec = Tween<Size>(200) });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = new Border { Width = 40, Height = 40 };
        f.Step(16);
        f.Step(16);
        var incoming = Slots(host).First(s => s.IsTarget);
        Assert.Equal(40, incoming.DesiredSize.Width);
        Assert.Equal(40, incoming.DesiredSize.Height);
        f.Finish();
    });

    [Fact]
    public Task ExpandIn_ClipsIncomingToLayoutSize_InsideFullSlot() => OnUi(() =>
    {
        var host = Host();
        host.Content = new Border { Width = 100, Height = 80 };
        host.ContentTransform = (new ExpandIn { Spec = Tween<Size>(200) } + new FadeIn { Spec = Tween<double>(200) })
            .TogetherWith(new FadeOut { Spec = Tween<double>(200) })
            .Using(new SizeTransform { Clip = false, Spec = Tween<Size>(200) });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = new Border { Width = 80, Height = 80 };
        f.Step(16);
        f.Step(100);
        var incoming = Slots(host).First(s => s.IsTarget);
        Assert.Equal(80, incoming.DesiredSize.Width);
        var layer = incoming.Layer;
        Assert.True(layer.Motion!.ChangesSize);
        Assert.True(layer.ClipToBounds);
        Assert.True(layer.Bounds.Width < incoming.Bounds.Width + 1);
        f.Finish();
    });

    [Fact]
    public Task SizeTransform_LastSizeContinuityOnInterrupt() => OnUi(() =>
    {
        var host = Host();
        host.Content = new Border { Width = 100, Height = 80 };
        host.ContentTransform = LinearFade(exitMs: 400, size: new SizeTransform { Spec = Tween<Size>(400) });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = new Border { Width = 40, Height = 40 };
        f.Step(16);
        f.Step(200);
        var mid = host.DesiredSize.Width;
        Assert.InRange(mid, 55, 85);
        host.Content = new Border { Width = 100, Height = 80 };
        f.Step(16);
        f.Step(16);
        Assert.True(host.DesiredSize.Width < 96, $"expected lastSize continuity, mid={mid}, now={host.DesiredSize.Width}");
        Assert.True(host.DesiredSize.Width > 50);
        f.Finish();
        Assert.Equal(100, host.DesiredSize.Width);
    });

    private sealed record Keyed(int Id, int Width);
    private sealed class BuildTemplate(Func<object?, Control> build) : IDataTemplate
    {
        public bool Match(object? data) => data is Keyed;
        public Control? Build(object? data) => data is null ? null : build(data);
    }

    [Fact]
    public Task ContentKey_SameKey_SnapsSize_NoParentSegment() => OnUi(() =>
    {
        var host = Host(new Keyed(1, 100));
        host.ContentKeySelector = s => s is Keyed k ? $"id:{k.Id}" : s;
        host.ContentTemplate = new BuildTemplate(s =>
        {
            var k = (Keyed)s!;
            return new Border { Width = k.Width, Height = 20 };
        });
        host.ContentTransform = LinearFade(exitMs: 400, size: new SizeTransform { Spec = Tween<Size>(400) });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = new Keyed(1, 40);
        f.Layout();
        Assert.Equal(1, host.VisibleSlotCount);
        Assert.True(Equals(host.Connection!.Transition.CurrentState, host.Connection.Transition.TargetState));
        f.Layout();
        Assert.True(host.IsIdle);
    });

    [Fact]
    public Task KeepUntilTransitionsFinished_WaitsForParentIdleIncludingSize() => OnUi(() =>
    {
        var host = Host();
        host.Content = new Border { Width = 100, Height = 80 };
        host.ContentTransform = new FadeIn { Spec = Tween<double>(200) }
            .TogetherWith(new FadeOut { Spec = Tween<double>(20) } + ExitTransition.KeepUntilTransitionsFinished)
            .Using(new SizeTransform { Spec = Tween<Size>(200) });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = new Border { Width = 40, Height = 40 };
        f.Step(16);
        f.Step(50);
        Assert.Equal(2, host.VisibleSlotCount);
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
    });

    [Fact]
    public Task ZIndex_TargetPlacedLast_RoundedInt() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 20, Height = 10 });
        host.TransitionSpec = _ =>
        {
            var t = new FadeIn { Spec = Tween<double>(200) }
                .TogetherWith(new FadeOut { Spec = Tween<double>(200) }).Using(null);
            t.TargetContentZIndex = 2.4f;
            return t;
        };
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Layout();
        var target = Slots(host).First(s => s.IsTarget);
        Assert.Equal(2, target.ZIndex);
        Assert.Same(target, Slots(host)[^1]);
        f.Finish();
    });

    [Fact]
    public Task ZIndex_EqualRoundedInts_TargetLastWins() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 20, Height = 10 });
        host.TransitionSpec = scope =>
        {
            var t = new FadeIn { Spec = Tween<double>(200) }.TogetherWith(new FadeOut { Spec = Tween<double>(200) }).Using(null);
            t.TargetContentZIndex = Equals(scope.TargetState, "B") ? 2.4f : 2.1f;
            return t;
        };
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Layout();
        Assert.Equal(2, Slots(host).First(s => Equals(s.State, "A")).ZIndex);
        Assert.Equal(2, Slots(host).First(s => Equals(s.State, "B")).ZIndex);
        Assert.Equal("B", Slots(host)[^1].State);
        f.Finish();
    });

    [Fact]
    public Task Source_ParentTransition_WaitsForSlots() => OnUi(() =>
    {
        var clock = new ManualAnimationFrameClock();
        using var parent = new Transition<string>("A", clock, "Page");
        _ = parent.RunAsync();
        var host = new AnimatedContent
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Source = ContentSource.From(parent),
            ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 30, Height = 10 }),
            ContentTransform = LinearFade().Using(null)
        };

        var window = new AnimationTestWindow(clock) { Width = 400, Height = 400, Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        parent.UpdateTarget("B");
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        window.UpdateLayout();
        Assert.True(host.VisibleSlotCount >= 1);
        Assert.True(parent.IsRunning);
        for (var i = 0; i < 40 && host.VisibleSlotCount > 1; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
        }
        Assert.Equal(1, host.VisibleSlotCount);
        window.Close();
    });

    [Fact]
    public Task Seeking_KeepsOutgoingAndIncoming() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 20, Height = 10 });
        host.ContentTransform = LinearFade(exitMs: 200).Using(null);
        using var f = new Fixture(host);
        f.Finish();
        host.Connection!.Transition.SetPlaytimeAfterInitialAndTargetStateEstablished("A", "B", TimeSpan.FromMilliseconds(40));
        f.Layout();
        Assert.Equal(2, host.VisibleSlotCount);
        Assert.False(host.IsIdle);
        Assert.Contains(Slots(host), s => Equals(s.State, "A"));
        Assert.Contains(Slots(host), s => Equals(s.State, "B"));
    });

    [Fact]
    public Task SeekCore_AppliesExitBeforeChildSeek_LinearMidpoint() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 20, Height = 10 });
        host.ContentTransform = new FadeIn { Spec = Tween<double>(90) }
            .TogetherWith(new FadeOut { Spec = Tween<double>(90) }).Using(null);
        using var f = new Fixture(host);
        f.Finish();
        host.Connection!.Transition.SetPlaytimeAfterInitialAndTargetStateEstablished("A", "B", TimeSpan.FromMilliseconds(45));
        f.Layout();
        var outgoing = Slots(host).First(s => Equals(s.State, "A"));
        Assert.InRange(outgoing.Layer.Surface.Opacity, 0.4, 0.6);
    });

    [Fact]
    public Task NativeIsVisibleFalse_DropsAllSlots() => OnUi(() =>
    {
        using var f = new Fixture(Host());
        f.Finish();
        f.Host.IsVisible = false;
        f.Layout();
        Assert.Equal(0, f.Host.VisibleSlotCount);
        Assert.True(f.Host.IsIdle);
    });

    [Fact]
    public Task Detach_ReleasesAll_ReattachFresh() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s });
        using var f = new Fixture(host);
        var first = host.TargetScope;
        f.Window.Content = null;
        f.Layout();
        Assert.Equal(0, host.VisibleSlotCount);
        Assert.Null(host.TargetScope);
        f.Window.Content = host;
        f.Layout();
        f.Finish();
        Assert.NotSame(first, host.TargetScope);
        Assert.Equal(1, host.VisibleSlotCount);
    });

    [Fact]
    public Task AnimateEnterExit_InsideSlot_FindsSlotScopeNotHost() => OnUi(() =>
    {
        AnimateEnterExit? child = null;
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) =>
        {
            child = new AnimateEnterExit
            {
                Exit = new FadeOut { Spec = Tween<double>(300) },
                Child = new TextBlock { Text = s, Width = 20, Height = 10 }
            };
            return child;
        });
        using var f = new Fixture(host);
        f.Finish();
        Assert.NotNull(child);
        host.Content = "B";
        f.Step(16);
        f.Step(50);
        Assert.True(host.VisibleSlotCount >= 1);
        f.Finish();
    });

    [Fact]
    public Task ContentTemplate_Change_DoesNotRebuildOutgoing() => OnUi(() =>
    {
        var built = new List<string>();
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) =>
        {
            built.Add("t1:" + s);
            return new TextBlock { Text = s, Width = 20, Height = 10 };
        });
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Layout();
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) =>
        {
            built.Add("t2:" + s);
            return new TextBlock { Text = s, Width = 20, Height = 10 };
        });
        f.Layout();
        Assert.Contains("t1:A", built);
        Assert.DoesNotContain("t2:A", built);
        Assert.Contains("t2:B", built);
        f.Finish();
    });

    [Fact]
    public Task RapidInterrupt_AbcdThenIdle_SingleSlot() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s, Width = 10, Height = 10 });
        host.ContentTransform = LinearFade().Using(null);
        using var f = new Fixture(host);
        foreach (var x in new[] { "B", "C", "D" })
        {
            host.Content = x;
            f.Layout();
        }
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
        Assert.Equal("D", host.TargetContent);
    });

    [Fact]
    public Task Error_ReportsViaAnimatedLastError() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s });
        using var f = new Fixture(host);
        host.TransitionSpec = _ => throw new InvalidOperationException("boom");
        Assert.Throws<InvalidOperationException>(() => host.Content = "B");
        Assert.IsType<InvalidOperationException>(((AnimationTestWindow)TopLevel.GetTopLevel(host)!).AnimationRoot.LastError);
    });

    [Fact]
    public Task Xaml_ParsesContentTransform() => OnUi(() =>
    {
        var host = AvaloniaRuntimeXamlLoader.Parse<AnimatedContent>("""
            <ca:AnimatedContent xmlns="https://github.com/avaloniaui"
                xmlns:ca="https://github.com/Shirasagi0012/ComposeAnimation">
                <ca:AnimatedContent.ContentTransform>
                    <ca:ContentTransform TargetContentZIndex="1">
                        <ca:ContentTransform.Enter><ca:FadeIn /></ca:ContentTransform.Enter>
                        <ca:ContentTransform.Exit><ca:FadeOut /></ca:ContentTransform.Exit>
                    </ca:ContentTransform>
                </ca:AnimatedContent.ContentTransform>
                <Border Width="10" Height="10" />
            </ca:AnimatedContent>
            """);
        Assert.NotNull(host.ContentTransform);
        Assert.Equal(1f, host.ContentTransform!.TargetContentZIndex);
        using var f = new Fixture(host);
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
    });

    [Fact]
    public Task ContentAlignment_Center() => OnUi(() =>
    {
        var host = Host(new Border { Width = 20, Height = 20 });
        host.Width = 200;
        host.Height = 200;
        host.HorizontalAlignment = HorizontalAlignment.Stretch;
        host.VerticalAlignment = VerticalAlignment.Stretch;
        host.ContentAlignment = VisibilityAlignment.Center;
        host.ContentTransform = LinearFade().Using(null);
        using var f = new Fixture(host);
        f.Finish();
        var box = SizeBox(host);
        Assert.InRange(box.Bounds.X, 80, 100);
        Assert.InRange(box.Bounds.Y, 80, 100);
    });

    [Fact]
    public Task SlideIntoContainer_MovesIncomingFromTheRight() => OnUi(() =>
    {
        var host = Host("A");
        host.ContentTemplate = new FuncDataTemplate<string>((s, _) => new Border { Width = 80, Height = 40 });
        host.TransitionSpec = scope =>
            scope.SlideIntoContainer(ContentSlideDirection.Left, Tween<Vector>(200))
                .TogetherWith(scope.SlideOutOfContainer(ContentSlideDirection.Left, Tween<Vector>(200)))
                .Using(null);
        using var f = new Fixture(host);
        f.Finish();
        host.Content = "B";
        f.Step(16);
        f.Step(16);
        var incoming = Slots(host).First(s => s.IsTarget);
        var transform = Assert.IsType<MatrixTransform>(incoming.Layer.Surface.RenderTransform);
        Assert.True(transform.Matrix.M31 > 10, $"incoming should start from the right, offset={transform.Matrix.M31}");
        f.Finish();
        Assert.Equal(1, host.VisibleSlotCount);
    });
}

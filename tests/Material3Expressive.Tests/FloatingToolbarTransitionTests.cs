using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class FloatingToolbarTransitionTests
{
    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void DetachedFactorySupportsEnterExitReverseAndCleanup(bool horizontal, bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Width = 80, Height = 40 };
        var host = new AnimatedVisibility { Visible = false, Content = content, TargetCommitPhase = AnimationCommitPhase.AfterFrame,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        Configure(host, horizontal, new MaterialSpring(0.6, 800));
        var window = new MotionTestWindow { Width = 240, Height = 120, Content = new Grid { Children = { host } } };
        window.Show(); window.UpdateLayout();
        Assert.Equal(default, host.DesiredSize);
        host.Visible = true; Advance(clock, window, 5);
        var entering = horizontal ? host.DesiredSize.Width : host.DesiredSize.Height;
        Assert.InRange(entering, 0.1, horizontal ? 79.9 : 39.9);
        Assert.Equal(Math.Round(entering), entering);
        Advance(clock, window, 75);
        Assert.Equal(new Size(80, 40), host.DesiredSize);
        host.Visible = false; Advance(clock, window, 4);
        Assert.Contains(content, host.GetVisualDescendants());
        host.Visible = true; Advance(clock, window, 80);
        Assert.Equal(new Size(80, 40), host.DesiredSize);
        Configure(host, horizontal, new MaterialSpring(1, 400));
        host.Visible = false; Advance(clock, window, 80);
        Assert.Equal(default, host.DesiredSize);
        host.Visible = true; Advance(clock, window, 4);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Null(MotionTestClockScope.Error(host));
    }

    [AvaloniaFact]
    public void ExplicitSpringUpdatesAffectTheActualSizeAnimation()
    {
        using var clock = new MotionTestClockScope();
        var slow = new AnimatedVisibility { Visible = false, TargetCommitPhase = AnimationCommitPhase.AfterFrame, Content = new Border { Width = 80, Height = 40 } };
        var fast = new AnimatedVisibility { Visible = false, TargetCommitPhase = AnimationCommitPhase.AfterFrame, Content = new Border { Width = 80, Height = 40 } };
        Configure(slow, true, new MaterialSpring(1, 100));
        Configure(fast, true, new MaterialSpring(1, 1000));
        var window = new MotionTestWindow { Width = 240, Height = 120,
            Content = new StackPanel { Children = { slow, fast } } };
        window.Show(); window.UpdateLayout();
        slow.Visible = fast.Visible = true; Advance(clock, window, 5);
        Assert.True(fast.DesiredSize.Width > slow.DesiredSize.Width);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static void Configure(AnimatedVisibility host, bool horizontal, MaterialSpring spring)
    {
        host.Enter = horizontal
            ? FloatingToolbarDefaults.HorizontalEnterTransition(host, VisibilityHorizontalAlignment.Start, spring)
            : FloatingToolbarDefaults.VerticalEnterTransition(host, VerticalAlignment.Bottom, spring);
        host.Exit = horizontal
            ? FloatingToolbarDefaults.HorizontalExitTransition(host, VisibilityHorizontalAlignment.End, spring)
            : FloatingToolbarDefaults.VerticalExitTransition(host, VerticalAlignment.Top, spring);
    }

    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var i = 0; i < frames; i++)
        { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.TestSupport;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarScrollTests
{
    [AvaloniaFact]
    public void ExplicitDispatch_ConsumesTopThenBodyThenBottom()
    {
        var top = new TopAppBarState(-64, -24);
        var bottom = new BottomAppBarState(-80);
        var scope = new AppBarScrollScope
        {
            TopScrollBehavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(top),
            BottomScrollBehavior = BottomAppBarDefaults.ExitAlwaysScrollBehavior(bottom),
        };
        Vector bodyInput = default;
        Vector Body(Vector delta) => bodyInput = delta;

        Assert.Equal(new Vector(0, -50), scope.DispatchScroll(new Vector(0, -50), Body));
        Assert.Equal(-64, top.HeightOffset);
        Assert.Equal(default, bodyInput);
        Assert.Equal(0, bottom.HeightOffset);
        scope.DispatchScroll(new Vector(0, -50), Body);
        Assert.Equal(new Vector(0, -50), bodyInput);
        Assert.Equal(-50, bottom.HeightOffset);
        Assert.Equal(-50, top.ContentOffset);
        scope.DispatchScroll(new Vector(0, 50), Body);
        Assert.Equal(-14, top.HeightOffset);
        Assert.Equal(default, bodyInput);
        Assert.Equal(-50, top.ContentOffset);
    }

    [AvaloniaFact]
    public void ExplicitDispatch_NestedScopesConsumeOnceInAncestorOrder()
    {
        var top = new TopAppBarState(-64, -24);
        var bottom = new BottomAppBarState(-80);
        var inner = new AppBarScrollScope
        {
            Child = new Border(),
            BottomScrollBehavior = BottomAppBarDefaults.ExitAlwaysScrollBehavior(bottom),
        };
        var outer = new AppBarScrollScope
        {
            Child = inner,
            TopScrollBehavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(top),
        };
        using var clock = new MotionTestClockScope();
        var window = Show(outer);
        try
        {
            Vector bodyInput = default;
            Vector Body(Vector delta) => bodyInput = delta;
            inner.DispatchScroll(new Vector(0, -50), Body);
            Assert.Equal(-64, top.HeightOffset);
            Assert.Equal(default, bodyInput);
            inner.DispatchScroll(new Vector(0, -50), Body);
            Assert.Equal(new Vector(0, -50), bodyInput);
            Assert.Equal(-50, bottom.HeightOffset);
            Assert.Equal(-50, top.ContentOffset);
        }
        finally { window.Close(); }
    }

    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add(new MaterialTheme());
        window.Content = control;
        window.Show();
        window.UpdateLayout();
        using (window.CaptureRenderedFrame()) { }
        return window;
    }

    [AvaloniaFact]
    public async Task RawTouch_BarDrag_Snaps_AndNewPressCancelsSnap()
    {
        var state = new TopAppBarState();
        var behavior = TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(state);
        var bar = new MediumTopAppBar { ScrollBehavior = behavior };
        var scope = new AppBarScrollScope { TopScrollBehavior = behavior, Child = new Scaffold { TopBar = bar } };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        using var touch = new TouchDevice();
        void Send(ulong time, RawPointerEventType type, double y) => window.PlatformImpl!.Input!(new RawTouchEventArgs(touch, time, (IInputRoot)window.PlatformImpl!.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!, type, new Point(200, y), RawInputModifiers.None, 1));
        try
        {
            using (window.CaptureRenderedFrame()) { } Send(100, RawPointerEventType.TouchBegin, 90); Send(116, RawPointerEventType.TouchUpdate, 58);
            Assert.True(state.HeightOffset == -14, $"offset={state.HeightOffset}; limit={state.HeightOffsetLimit}; bounds={bar.Bounds}; hit={window.InputHitTest(new Point(200, 90))}"); // Compose draggable's 18dp touch slop
            Send(200, RawPointerEventType.TouchEnd, 58); // pause removes release velocity; snap remains
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); Assert.True(state.HeightOffset > -14);
            Send(216, RawPointerEventType.TouchBegin, 50); var interrupted = state.HeightOffset;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(100)); window.UpdateLayout(); Assert.Equal(interrupted, state.HeightOffset);
            Send(232, RawPointerEventType.TouchCancel, 50);
            Assert.Null(MotionTestClockScope.Error(bar));
            await Task.CompletedTask;
        }
        finally { window.Close(); }
    }
}

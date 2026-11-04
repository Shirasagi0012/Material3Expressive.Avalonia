using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ScrimTests
{
    [AvaloniaFact]
    public void NullColorOmitsLayoutAndActionButTransparentColorStillAcceptsClicks()
    {
        using var clock = new MotionTestClockScope();
        var scrim = new Scrim { Color = null }; var clicks = 0; scrim.Click += (_, _) => clicks++;
        var window = DialogTests.Show(scrim);
        try
        {
            Assert.Equal(default, scrim.DesiredSize); Click(window, new Point(20, 20)); Assert.Equal(0, clicks);
            scrim.Color = Colors.Transparent; scrim.Alpha = 0; window.UpdateLayout();
            Assert.Equal(window.ClientSize, scrim.DesiredSize); Click(window, new Point(20, 20)); Assert.Equal(1, clicks);
            Assert.False(scrim.Focusable);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void GestureMayTravelInsideButLeavingBoundsCancelsPermanently(bool leave)
    {
        using var clock = new MotionTestClockScope(); var scrim = new Scrim(); var clicks = 0; scrim.Click += (_, _) => clicks++;
        var window = DialogTests.Show(new Border { Padding = new Thickness(20), Child = scrim });
        try
        {
            window.MouseDown(new Point(30, 30), MouseButton.Left);
            window.MouseMove(leave ? new Point(5, 5) : new Point(300, 200));
            window.MouseMove(new Point(40, 40)); window.MouseUp(new Point(40, 40), MouseButton.Left);
            Assert.Equal(leave ? 0 : 1, clicks);
            Click(window, new Point(40, 40)); Assert.Equal(leave ? 1 : 2, clicks);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void DisablingActionDuringPressCancelsAndPeerTracksSemantics()
    {
        using var clock = new MotionTestClockScope(); var scrim = new Scrim(); var clicks = 0; scrim.Click += (_, _) => clicks++;
        AutomationProperties.SetName(scrim, "Close sheet");
        var window = DialogTests.Show(scrim);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(scrim)!;
            Assert.Equal("Close sheet", peer.GetName()); Assert.True(peer.IsControlElement());
            window.MouseDown(new Point(30, 30), MouseButton.Left); scrim.IsDismissEnabled = false;
            scrim.IsDismissEnabled = true; window.MouseUp(new Point(30, 30), MouseButton.Left); Assert.Equal(0, clicks);
            ((IInvokeProvider)peer).Invoke(); Assert.Equal(1, clicks);
            scrim.IsDismissEnabled = false; Assert.False(peer.IsControlElement());
            ((IInvokeProvider)peer).Invoke(); Click(window, new Point(30, 30)); Assert.Equal(1, clicks);
            scrim.IsDismissEnabled = true; scrim.Color = null; Assert.False(peer.IsContentElement());
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void DecorativeScrimDoesNotInterceptBackgroundPointerInput()
    {
        using var clock = new MotionTestClockScope(); var scrim = new Scrim { IsDismissEnabled = false };
        var button = new Button { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch };
        var clicks = 0; button.Click += (_, _) => clicks++;
        var window = DialogTests.Show(new Grid { Children = { button, scrim } });
        try
        {
            Assert.False(scrim.IsHitTestVisible);
            Click(window, button.Bounds.Center); Assert.Equal(1, clicks);
        }
        finally { window.Close(); }
    }
    private static void Click(Window window, Point point)
    { window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); }
}

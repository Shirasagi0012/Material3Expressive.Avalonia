using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class SwitchDisplayTests
{
    [AvaloniaFact]
    public void ParentOwnsSelection_WithoutMinimumFocusOrToggleAutomation()
    {
        using var clock = new MotionTestClockScope();
        var display = new SwitchDisplay();
        var row = new ToggleableSurface { Content = display, Width = 140, Height = 64 };
        MaterialInteractiveSize.SetMinimum(row, 100);
        display.Bind(SwitchDisplay.IsCheckedProperty, new Binding(nameof(ToggleableSurface.IsChecked)) { Source = row });
        var requests = 0;
        row.CheckedChangeRequested += (_, e) => { requests++; row.IsChecked = e.Value; };
        var window = DialogTests.Show(row);
        Assert.Equal(new Size(52, 32), display.Bounds.Size);
        Assert.False(display.Focus()); Assert.False(display.IsHitTestVisible);
        Assert.False(ControlAutomationPeer.CreatePeerForElement(display) is IToggleProvider);
        var center = display.TranslatePoint(new Point(26, 16), window)!.Value;
        window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
        Assert.Equal(1, requests); Assert.True(display.IsChecked);
        Assert.False(MaterialInteraction.GetIsInteractive(display));
    }

    [AvaloniaFact]
    public void SharedSourceDrivesThumbAndRipple_AndReattachMovesCollector()
    {
        using var clock = new MotionTestClockScope();
        var first = new MaterialInteractionSource(); var second = new MaterialInteractionSource();
        var display = new SwitchDisplay { InteractionSource = first };
        var root = new Grid { Children = { display } }; var window = DialogTests.Show(root);
        var thumb = display.GetVisualDescendants().OfType<SwitchThumbLayout>().Single();
        void Settle() { for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); } }
        Settle(); Assert.Equal(16, thumb.PaintedSize);
        var press = new MaterialInteractionEvent.Press(new Point(26, 16));
        first.Emit(press); Settle(); Assert.Equal(28, thumb.PaintedSize);
        Assert.Equal(1, MaterialInteraction.GetOrCreate(display).RippleCount);
        display.InteractionSource = second; Settle();
        Assert.Equal(0, MaterialInteraction.GetOrCreate(display).RippleCount);
        first.Emit(new MaterialInteractionEvent.Release(press)); Settle(); Assert.Equal(16, thumb.PaintedSize);
        second.Emit(press); Settle(); Assert.Equal(16, thumb.PaintedSize);
        root.Children.Remove(display); root.Children.Add(display); Settle();
        second.Emit(press); Settle(); Assert.Equal(28, thumb.PaintedSize);
        display.IsEnabled = false; Settle(); Assert.Equal(28, thumb.PaintedSize);
        second.Emit(new MaterialInteractionEvent.Release(press)); Settle(); Assert.Equal(16, thumb.PaintedSize);
        display.IsChecked = true; Settle(); Assert.Equal(24, thumb.PaintedSize); Assert.Equal(24, thumb.PaintedOffset);
        root.Children.Clear(); Settle(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void RequiredTrackOverflowsTightConstraints_AndContentChangesThumbSize()
    {
        using var clock = new MotionTestClockScope();
        var display = new SwitchDisplay { Width = 12, Height = 12 };
        var window = DialogTests.Show(display);
        var track = display.GetVisualDescendants().OfType<SwitchTrack>().Single();
        var thumb = display.GetVisualDescendants().OfType<SwitchThumbLayout>().Single();
        Assert.Equal(new Size(52, 32), track.Bounds.Size);
        Assert.Equal(new Point(-20, -10), track.TranslatePoint(default, display));
        Assert.Equal(16, thumb.PaintedSize);
        display.ThumbIcon = new Border { Width = 8, Height = 8 };
        window.UpdateLayout(); Assert.Equal(24, thumb.PaintedSize);
        display.ThumbIcon = null; window.UpdateLayout(); Assert.Equal(16, thumb.PaintedSize);
    }
}

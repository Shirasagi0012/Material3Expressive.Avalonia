using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class TooltipAnchorTests
{
    [AvaloniaTheory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void OwnRenderTransformDoesNotMovePopup_WhileAncestorTransformAndLayoutDo(bool animated, bool rtl, bool ancestor)
    {
        using var clock = new MotionTestClockScope();
        var marker = new Border { Width = 64, Height = 8 };
        var tip = new PlainTooltip { Content = marker };
        ContentControl owner = animated
            ? new TooltipBox { Tooltip = tip, State = new(initialIsVisible: true, mutatorMutex: new()), EnableUserInput = false }
            : new TooltipLabel { Label = tip, IsPersistent = true };
        owner.Content = new Border { Width = 24, Height = 24 };
        owner.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var parent = new Grid { Children = { owner } };
        Canvas.SetLeft(parent, 100); Canvas.SetTop(parent, 100);
        var window = DialogTests.Show(new Canvas { Children = { parent } });
        var initial = Origin(tip, window);
        var transformed = ancestor ? (Control)parent : owner;
        var translation = new TranslateTransform(20, 12);
        transformed.RenderTransform = translation;
        Flush(window);
        Assert.Equal(initial + (ancestor ? new Vector(20, 12) : default), Origin(tip, window));
        translation.X = 28; Flush(window);
        Assert.Equal(initial + (ancestor ? new Vector(28, 12) : default), Origin(tip, window));
        translation.X = 20; Flush(window);
        marker.Width = 100; Flush(window);
        Assert.Equal(initial + (ancestor ? new Vector(2, 12) : new Vector(-18, 0)), Origin(tip, window));
        Canvas.SetTop(parent, 120); Flush(window);
        var afterPlacement = Origin(tip, window);
        // The outer Box follows parent coordinates, excluding only the anchor modifier.
        Assert.Equal(initial + (ancestor ? new Vector(2, 32) : new Vector(-18, 20)), afterPlacement);
        transformed.RenderTransform = null; Flush(window);
        Assert.Equal(initial + new Vector(-18, 20), Origin(tip, window));
        if (owner is TooltipLabel label)
        {
            label.IsPersistent = false; Flush(window);
            label.IsPersistent = true;
        }
        else
        {
            var box = (TooltipBox)owner;
            box.State = new(initialIsVisible: false, mutatorMutex: new()); Flush(window);
            box.State = new(initialIsVisible: true, mutatorMutex: new());
        }
        Flush(window);
        Assert.Equal(initial + new Vector(-18, 20), Origin(tip, window));
        window.Close(); Flush(window);
        Assert.Equal(0, clock.PendingFrameCount);
    }

    static Point Origin(Control control, Window window)
    {
        var left = control.TranslatePoint(default, window)!.Value;
        var right = control.TranslatePoint(new Point(control.Bounds.Width, 0), window)!.Value;
        return new Point(Math.Min(left.X, right.X), left.Y);
    }

    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

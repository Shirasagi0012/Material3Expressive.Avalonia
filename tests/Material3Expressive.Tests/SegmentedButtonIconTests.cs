using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class SegmentedButtonIconTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultIconHasNoInteractionSpaceAndFollowsSizeAndForeground(bool active)
    {
        using var clock = new MotionTestClockScope();
        var icon = new SegmentedButtonIcon { IsActive = active };
        var window = Show(icon);
        Assert.Equal(active ? new Size(18, 18) : default, icon.DesiredSize);
        Assert.False(icon.Focusable);
        Assert.Equal(Brushes.Blue, icon.Foreground);
        window.Resources[MaterialTokens.Comp.SegmentedButton.IconSize] = 24d;
        window.UpdateLayout();
        Assert.Equal(active ? new Size(24, 24) : default, icon.DesiredSize);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SingleSlotEntersAndExitsWithoutReplacingCallerContent()
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Width = 26, Height = 14 };
        var icon = new SegmentedButtonIcon { ActiveContent = content };
        var window = Show(icon);
        Assert.Equal(default, icon.DesiredSize);
        icon.IsActive = true;
        Advance(clock, window, 60);
        Assert.Equal(new Size(26, 14), icon.DesiredSize);
        Assert.Contains(content, icon.GetVisualDescendants());
        icon.IsActive = false;
        Advance(clock, window, 2);
        Assert.Equal(default, icon.DesiredSize);
        Assert.IsType<AnimatedVisibility>(icon.Child);
        icon.IsActive = true;
        Advance(clock, window, 3);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SingleSlotUsesTheSamePhysicalScaleOriginInBothDirections()
    {
        using var clock = new MotionTestClockScope();
        var ltrContent = new Border { Width = 26, Height = 14 };
        var rtlContent = new Border { Width = 26, Height = 14 };
        var ltr = new SegmentedButtonIcon { ActiveContent = ltrContent };
        var rtl = new SegmentedButtonIcon { ActiveContent = rtlContent, FlowDirection = FlowDirection.RightToLeft,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(ltr);
        ((Grid)window.Content!).Children.Add(rtl); window.UpdateLayout();
        ltr.IsActive = rtl.IsActive = true;
        Advance(clock, window, 4);
        var ltrBox = new Rect(ltrContent.Bounds.Size).TransformToAABB(ltrContent.TransformToVisual(window)!.Value);
        var rtlBox = new Rect(rtlContent.Bounds.Size).TransformToAABB(rtlContent.TransformToVisual(window)!.Value);
        // The RTL branch includes two inverse native mirror matrices; their multiplication
        // can leave floating-point roundoff even when the physical geometry is identical.
        Assert.Equal(ltrBox.X, rtlBox.X, 12);
        Assert.Equal(ltrBox.Y, rtlBox.Y, 12);
        Assert.Equal(ltrBox.Width, rtlBox.Width, 12);
        Assert.Equal(ltrBox.Height, rtlBox.Height, 12);
        Assert.Equal(FlowDirection.RightToLeft, rtlContent.FlowDirection);
        Assert.InRange(ltrBox.Width, 0.1, 25.9);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void TwoSlotsCrossfadeAndReleaseBothControlsOnReplacementAndDetach()
    {
        using var clock = new MotionTestClockScope();
        var active = new Border { Width = 26, Height = 14 };
        var inactive = new Border { Width = 12, Height = 20 };
        var icon = new SegmentedButtonIcon { ActiveContent = active, InactiveContent = inactive };
        var window = Show(icon);
        var crossfade = Assert.IsType<Crossfade>(icon.Child);
        Assert.Equal(new Size(12, 20), icon.DesiredSize);
        icon.IsActive = true; Advance(clock, window, 4);
        Assert.Contains(active, icon.GetVisualDescendants());
        Assert.Contains(inactive, icon.GetVisualDescendants());
        icon.IsActive = false; Advance(clock, window, 3);
        icon.IsActive = true; Advance(clock, window, 60);
        Assert.Equal(new Size(26, 14), icon.DesiredSize);
        var replacement = new Border { Width = 31, Height = 9 };
        icon.ActiveContent = replacement; window.UpdateLayout();
        Assert.Same(crossfade, icon.Child);
        Assert.Null(active.GetVisualParent());
        Assert.Equal(new Size(31, 9), icon.DesiredSize);
        var root = (Grid)window.Content!;
        root.Children.Remove(icon); window.UpdateLayout();
        Assert.Null(replacement.GetVisualParent());
        root.Children.Add(icon); window.UpdateLayout();
        Assert.Contains(replacement, icon.GetVisualDescendants());
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void TemplateOnlySlotSelectsCrossfadeWithoutContentObjects()
    {
        using var clock = new MotionTestClockScope();
        var icon = new SegmentedButtonIcon
        {
            ActiveContentTemplate = new FuncDataTemplate<object?>((_, _) => new Border { Width = 30, Height = 10 }),
            InactiveContentTemplate = new FuncDataTemplate<object?>((_, _) => new Border { Width = 14, Height = 16 }),
        };
        var window = Show(icon);
        Assert.IsType<Crossfade>(icon.Child);
        Assert.Equal(new Size(14, 16), icon.DesiredSize);
        icon.IsActive = true; Advance(clock, window, 60);
        Assert.Equal(new Size(30, 10), icon.DesiredSize);
        icon.InactiveContentTemplate = null; window.UpdateLayout();
        Assert.IsType<AnimatedVisibility>(icon.Child);
        Assert.Equal(new Size(30, 10), icon.DesiredSize);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static Window Show(SegmentedButtonIcon icon)
    {
        icon.HorizontalAlignment = HorizontalAlignment.Left;
        icon.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 240, Height = 120 };
        window.Styles.Add(new MaterialTheme());
        Avalonia.Controls.Documents.TextElement.SetForeground(window, Brushes.Blue);
        window.Content = new Grid { Children = { icon } };
        window.Show(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
        }
    }
}

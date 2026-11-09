using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarOverflowIndicatorTests
{
    [AvaloniaFact]
    public void RealButtonTogglesCurrentStateAndHonorsDisabledInputAndAncestor()
    {
        using var clock = new MotionTestClockScope();
        var initial = new AppBarMenuState();
        var indicator = new AppBarOverflowIndicator { MenuState = initial, Description = "More actions" };
        var window = DialogTests.Show(indicator);
        var button = indicator.GetVisualDescendants().OfType<IconButton>().Single();
        var tooltip = indicator.GetVisualDescendants().OfType<TooltipBox>().Single();
        try
        {
            Assert.Equal(new Size(48, 48), button.Bounds.Size);
            Assert.Equal("More actions", AutomationProperties.GetName(button));
            Assert.Equal("More actions", ((PlainTooltip)tooltip.Tooltip!).Content);
            button.Focus(NavigationMethod.Tab);
            PressSpace(window); Assert.True(initial.IsShowing);
            PressSpace(window); Assert.False(initial.IsShowing);
            var replacement = new AppBarMenuState(); replacement.Show();
            indicator.MenuState = replacement;
            PressSpace(window); Assert.False(replacement.IsShowing); Assert.False(initial.IsShowing);
            indicator.IsEnabled = false;
            PressSpace(window); Assert.False(replacement.IsShowing);
            Assert.True(tooltip.EnableUserInput);
            Assert.True(indicator.IsEffectivelyEnabled);
            Assert.True(tooltip.IsEffectivelyEnabled);
            Assert.False(button.IsEffectivelyEnabled);
            window.Content = null;
            Assert.False(tooltip.State.IsVisible);
            window.Content = indicator; window.UpdateLayout();
            indicator.IsEnabled = true;
            indicator.GetVisualDescendants().OfType<IconButton>().Single().Focus(NavigationMethod.Tab);
            PressSpace(window); Assert.True(replacement.IsShowing);
            window.Content = null;
            var disabledParent = new Border { Child = indicator, IsEnabled = false };
            window.Content = disabledParent; window.UpdateLayout();
            Assert.False(indicator.IsEffectivelyEnabled);
            Assert.False(button.IsEffectivelyEnabled);
            disabledParent.IsEnabled = true;
            Assert.True(indicator.IsEffectivelyEnabled);
            Assert.True(button.IsEffectivelyEnabled);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DisabledButtonAllowsTooltipHoverWithoutActivatingMenu()
    {
        using var clock = new MotionTestClockScope();
        var indicator = new AppBarOverflowIndicator { MenuState = new(), IsEnabled = false };
        var window = DialogTests.Show(indicator);
        try
        {
            var button = indicator.GetVisualDescendants().OfType<IconButton>().Single();
            var tooltip = indicator.GetVisualDescendants().OfType<TooltipBox>().Single();
            var center = button.TranslatePoint(new Point(24, 24), window)!.Value;
            window.MouseMove(new Point(500, 280)); window.MouseMove(center);
            Frames(clock, window, 35);
            Assert.True(tooltip.State.IsVisible);
            window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
            Assert.False(indicator.MenuState!.IsShowing);
            window.MouseMove(new Point(500, 280)); Frames(clock, window, 35);
            Assert.False(tooltip.State.IsVisible);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ColorsShapeDescriptionAndSharedSourceFollowReplacementAndClearing()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var shape = new CutCornerShape(6);
        var indicator = new AppBarOverflowIndicator
        {
            MenuState = new(), Shape = shape, InteractionSource = source,
            Foreground = new SolidColorBrush(Color.FromArgb(64, 32, 128, 112)),
            ContainerColor = Colors.Red, ContentColor = Colors.Blue,
            DisabledContainerColor = Colors.Green, DisabledContentColor = Colors.Yellow
        };
        var window = DialogTests.Show(indicator);
        try
        {
            var button = indicator.GetVisualDescendants().OfType<IconButton>().Single();
            Assert.Same(shape, MaterialButton.GetShape(button));
            Assert.Same(source, button.InteractionSource);
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)button.Foreground!).Color);
            Assert.Equal(Colors.Red, ((ISolidColorBrush)button.Background!).Color);
            indicator.IsEnabled = false;
            Assert.Equal(Colors.Yellow, ((ISolidColorBrush)button.Foreground!).Color);
            Assert.Equal(Colors.Green, ((ISolidColorBrush)button.Background!).Color);
            indicator.ClearValue(AppBarOverflowIndicator.DisabledContentColorProperty);
            Assert.Equal(Color.FromArgb(97, 0, 0, 255), ((ISolidColorBrush)button.Foreground!).Color);
            indicator.ClearValue(AppBarOverflowIndicator.ContentColorProperty);
            Assert.Equal(Color.FromArgb(97, 32, 128, 112), ((ISolidColorBrush)button.Foreground!).Color);
            indicator.IsEnabled = true;
            Assert.Equal(Color.FromArgb(64, 32, 128, 112), ((ISolidColorBrush)button.Foreground!).Color);
            indicator.Description = "其他操作";
            Assert.Equal(indicator.Description, AutomationProperties.GetName(button));
            var next = new MaterialInteractionSource(); indicator.InteractionSource = next;
            Assert.Same(next, button.InteractionSource);
            indicator.Shape = null; Assert.Null(MaterialButton.GetShape(button));
            var oldButton = button;
            var template = indicator.Template;
            indicator.Template = null; window.UpdateLayout();
            indicator.Template = template; window.UpdateLayout();
            button = indicator.GetVisualDescendants().OfType<IconButton>().Single();
            Assert.NotSame(oldButton, button);
            button.Focus(NavigationMethod.Tab); PressSpace(window);
            Assert.True(indicator.MenuState!.IsShowing);
            oldButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.True(indicator.MenuState.IsShowing);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static void PressSpace(Window window)
    {
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
    }
    private static void Frames(MotionTestClockScope clock, Window window, int count)
    {
        for (var i = 0; i < count; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        }
    }
}

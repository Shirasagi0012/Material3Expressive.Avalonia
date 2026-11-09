using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class NativeMenuMotionTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void OpenCloseAndSameAnchorReopenPreserveNativePresentationUntilExitCompletes(bool baseline)
    {
        using var clock = new MotionTestClockScope();
        var flyout = new MaterialMenuFlyout { Items = { new MenuItem { Header = "Open" } } };
        var (window, anchor) = Show(flyout, baseline);
        var closed = 0; flyout.Closed += (_, _) => { Assert.False(flyout.IsPresented); closed++; };
        try
        {
            flyout.ShowAt(anchor); Flush(window);
            Assert.True(flyout.IsOpen); Assert.True(flyout.IsPresented);
            Frames(clock, window, 4);
            Assert.InRange(flyout.AppearanceAlpha, .001f, .999f);
            Assert.InRange(flyout.AppearanceScale, .8f, 1.1f);
            Frames(clock, window, 60);
            Assert.Equal(1, flyout.AppearanceAlpha); Assert.Equal(1, flyout.AppearanceScale);
            flyout.Hide();
            Assert.False(flyout.IsOpen); Assert.True(flyout.IsPresented); Assert.True(flyout.Popup.IsOpen);
            Frames(clock, window, 4);
            var alpha = flyout.AppearanceAlpha;
            Assert.InRange(alpha, .001f, .999f);
            flyout.IsOpen = true;
            Assert.True(flyout.IsOpen); Assert.True(flyout.IsPresented);
            Assert.Equal(alpha, flyout.AppearanceAlpha);
            Frames(clock, window, 60);
            Assert.Equal(1, flyout.AppearanceAlpha); Assert.Equal(0, closed);
            flyout.Hide(); Frames(clock, window, 60);
            Assert.False(flyout.IsPresented); Assert.False(flyout.Popup.IsOpen); Assert.Equal(1, closed);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData("command")] [InlineData("escape")] [InlineData("outside")] [InlineData("popup")] [InlineData("popup-property")]
    public void NativeDismissalRoutesThroughTheExitTransition(string action)
    {
        using var clock = new MotionTestClockScope();
        var command = new CountingCommand();
        var item = new MenuItem { Header = "Action", Command = command };
        var flyout = new MaterialMenuFlyout { Items = { item } };
        var (window, anchor) = Show(flyout);
        try
        {
            flyout.ShowAt(anchor); Flush(window); Frames(clock, window, 60);
            switch (action)
            {
                case "command": Click(window, item); Assert.Equal(1, command.Calls); break;
                case "escape":
                    item.Focus();
                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
                    window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
                    break;
                case "outside":
                    window.MouseMove(new Point(450, 350));
                    window.MouseDown(new Point(450, 350), MouseButton.Left);
                    window.MouseUp(new Point(450, 350), MouseButton.Left);
                    break;
                case "popup": flyout.Popup.Close(); break;
                case "popup-property": flyout.Popup.IsOpen = false; break;
            }
            Flush(window);
            Assert.False(flyout.IsOpen); Assert.True(flyout.IsPresented);
            Frames(clock, window, 4); Assert.InRange(flyout.AppearanceAlpha, .001f, .999f);
            Frames(clock, window, 60); Assert.False(flyout.IsPresented);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ClosingCanBeCancelledAndCheckableItemsKeepTheirNativeOwnership()
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Toggle", ToggleType = MenuItemToggleType.CheckBox, StaysOpenOnClick = true };
        var flyout = new MaterialMenuFlyout { Items = { item } };
        var (window, anchor) = Show(flyout);
        EventHandler<CancelEventArgs> cancel = (_, e) => e.Cancel = true;
        try
        {
            flyout.ShowAt(anchor); Flush(window); Frames(clock, window, 60);
            Click(window, item); Assert.True(item.IsChecked); Assert.True(flyout.IsOpen);
            flyout.Closing += cancel;
            flyout.Hide(); Assert.True(flyout.IsOpen); Assert.True(flyout.IsPresented);
            flyout.Popup.Close(); Assert.True(flyout.IsOpen); Assert.True(flyout.Popup.IsOpen);
            Frames(clock, window, 8); Assert.Equal(1, flyout.AppearanceAlpha);
            flyout.Closing -= cancel;
            flyout.Hide(); Frames(clock, window, 60); Assert.False(flyout.IsPresented);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void EarlyCloseThenReopenAndDifferentAnchorCancelObsoleteRemoval()
    {
        using var clock = new MotionTestClockScope();
        var flyout = new MaterialMenuFlyout { Items = { new MenuItem { Header = "Open" } } };
        var (window, anchor) = Show(flyout);
        try
        {
            flyout.ShowAt(anchor); Flush(window); Frames(clock, window, 4);
            flyout.Hide(); Frames(clock, window, 2);
            flyout.ShowAt(anchor); Frames(clock, window, 60);
            Assert.True(flyout.IsPresented); Assert.True(flyout.IsOpen); Assert.Equal(1, flyout.AppearanceAlpha);
            var replacement = new Border { Width = 24, Height = 24, Background = Brushes.Gray, Margin = new Thickness(300, 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            ((Grid)window.Content!).Children.Add(replacement); Flush(window);
            flyout.Hide(); Frames(clock, window, 2);
            flyout.ShowAt(replacement); Flush(window);
            Assert.Same(replacement, flyout.Target);
            Frames(clock, window, 60); Assert.True(flyout.IsPresented); Assert.Equal(1, flyout.AppearanceAlpha);
            window.Content = null; Flush(window);
            Assert.False(flyout.IsPresented); Assert.False(flyout.IsOpen);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    private static void Frames(MotionTestClockScope clock, Window window, int count)
    {
        for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
    private static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Rect(control.Bounds.Size).Center, window)!.Value;
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
    }
    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
    }
    private static (MotionTestWindow Window, Control Anchor) Show(MaterialMenuFlyout flyout, bool baseline = false)
    {
        var window = new MotionTestWindow { Width = 480, Height = 400 };
        window.Template = new FuncControlTemplate<Window>((owner, scope) =>
        {
            var content = new Avalonia.Controls.Presenters.ContentPresenter { Name = "PART_ContentPresenter" };
            content.Bind(Avalonia.Controls.Presenters.ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = content };
            scope.Register(content.Name, content); scope.Register(layers.Name, layers); return layers;
        });
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        var anchor = new Border { Width = 80, Height = 32, Margin = new Thickness(20), Background = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        window.Content = new Grid { Children = { anchor } }; window.Show(); Flush(window);
        flyout.Popup.ShouldUseOverlayLayer = true;
        if (baseline) flyout.FlyoutPresenterTheme = (ControlTheme)window.FindResource("MaterialBaselineMenuTheme")!;
        return (window, anchor);
    }
    private sealed class CountingCommand : ICommand
    {
        public int Calls { get; private set; }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Calls++;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}

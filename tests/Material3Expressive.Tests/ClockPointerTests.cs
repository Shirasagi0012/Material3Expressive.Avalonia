using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Material3Expressive.Foundation.Interaction;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using ComposeAnimation;
using Material3Expressive.Components;
using Xunit;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Tests;

public sealed class ClockPointerTests
{
    [AvaloniaFact]
    public void HourTapAnimatesBeforeItsDelay_ThenSwitchesToMinute()
    {
        using var host = new Host();
        host.Input("down", new(229, 128)); host.Input("up", new(229, 128));
        Assert.Equal(new TimeOnly(3, 0), host.Picker.SelectedTime);
        Assert.Equal(-MathF.PI / 2, host.Dial.CurrentAngle);
        host.Advance(160);
        Assert.Equal(TimeSelection.Hour, host.Picker.Selection);
        Assert.InRange(host.Dial.CurrentAngle, -MathF.PI / 2 + .01f, 0);
        host.Advance(1000);
        Assert.Equal(TimeSelection.Minute, host.Picker.Selection);
        Assert.InRange(MathF.Abs(host.Dial.CurrentAngle + MathF.PI / 2), 0, .001f);
    }

    [AvaloniaFact]
    public void DragPreservesContinuousAngle_AndUpDoesNotApplyAnotherPosition()
    {
        using var host = new Host(TimeSelection.Minute);
        host.Input("down", new(229, 128));
        host.Input("move", new(228, 140));
        Assert.Equal(16, host.Picker.SelectedTime.Minute);
        var angle = host.Dial.CurrentAngle;
        Assert.InRange(angle - MathF.Tau, .11f, .13f);
        Assert.True(Math.Abs(angle - MathF.Tau / 60) > .005); // It is not the quantized minute.
        host.Input("up", new(128, 229));
        Assert.Equal(16, host.Picker.SelectedTime.Minute);
        host.Advance(1000);
        Assert.InRange(Math.Abs(host.Dial.CurrentAngle - MathF.Tau - MathF.Tau / 60), 0, .00001);
    }

    [AvaloniaFact]
    public void TouchSlopIsRemovedFromTheFirstDrag_AndCaptureLossDoesNotAdvance()
    {
        using var host = new Host();
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        void Event(Point point, bool down)
        {
            var scene = host.Dial.TranslatePoint(point, host.Window)!.Value;
            if (down) host.Dial.RaiseEvent(new PointerPressedEventArgs(host.Dial, pointer, host.Window, scene, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), 0));
            else host.Dial.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, host.Dial, pointer, host.Window, scene, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), 0));
            host.Draw();
        }
        Event(new(229, 128), true); Event(new(229, 145), false);
        Assert.Equal(0, host.Picker.SelectedTime.Hour);
        Event(new(229, 148), false);
        Assert.Equal(3, host.Picker.SelectedTime.Hour);
        Assert.InRange(host.Dial.CurrentAngle - MathF.Tau, .019f, .021f); // 2 px past the 18 DIP touch slop.
        pointer.Capture(null); host.Advance(1000);
        Assert.Equal(TimeSelection.Hour, host.Picker.Selection);
        Assert.InRange(host.Dial.CurrentAngle - MathF.Tau, .019f, .021f);
    }

    [AvaloniaFact]
    public void FieldTransitionRejectsPointerTimeMutation_AndDetachCancelsTapDelay()
    {
        using var host = new Host();
        host.Picker.SelectedTime = new(0, 30);
        host.Picker.Selection = TimeSelection.Minute;
        host.Advance(16); // ClockDialNode updates its selection on the composition frame.
        host.Input("down", new(229, 128)); host.Input("up", new(229, 128));
        Assert.Equal(30, host.Picker.SelectedTime.Minute);
        host.Advance(1000);
        host.Picker.Selection = TimeSelection.Hour; host.Advance(2000);
        host.Input("down", new(229, 128)); host.Input("up", new(229, 128));
        host.Window.Content = null; host.Draw(); host.Advance(3000);
        Assert.Equal(TimeSelection.Hour, host.Picker.Selection);
        Assert.Equal(0, host.Clock.PendingFrameCount);
        Assert.Equal(0, host.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public void DragChanges24HourRings_AndAncestorDisableCancelsCapture()
    {
        using var host = new Host();
        host.Input("down", new(229, 128)); host.Input("move", new(197, 128));
        Assert.Equal(15, host.Picker.SelectedTime.Hour);
        host.Input("move", new(229, 128)); Assert.Equal(3, host.Picker.SelectedTime.Hour);
        host.Picker.IsEnabled = false; host.Draw();
        host.Input("up", new(128, 229)); host.Advance(1000);
        Assert.Equal(TimeSelection.Hour, host.Picker.Selection);
        Assert.Equal(3, host.Picker.SelectedTime.Hour);
    }

    [AvaloniaFact]
    public void TouchExplorationKeepsTheHourScreenForBothTapAndDrag()
    {
        using var host = new Host();
        host.Picker.TouchExplorationEnabled = true;
        host.Input("down", new(229, 128)); host.Input("up", new(229, 128));
        host.Advance(1000);
        Assert.Equal(new TimeOnly(3, 0), host.Picker.SelectedTime);
        Assert.Equal(TimeSelection.Hour, host.Picker.Selection);
        host.Input("down", new(229, 128)); host.Input("move", new(200, 200)); host.Input("up", new(200, 200));
        host.Advance(2000);
        Assert.Equal(new TimeOnly(4, 0), host.Picker.SelectedTime);
        Assert.Equal(TimeSelection.Hour, host.Picker.Selection);
    }

    [AvaloniaFact]
    public void AccessibilityActivationSnapsAndObeysInputMode_SelectionFollowsTheAnimatedHandle()
    {
        using var host = new Host();
        var top = (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(host.Dial.Labels[0])!;
        var right = (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(host.Dial.Labels[3])!;
        Assert.True(top.IsSelected);
        host.Input("down", new(229, 128)); host.Input("up", new(229, 128));
        Assert.Equal(3, host.Picker.SelectedTime.Hour);
        Assert.True(top.IsSelected); Assert.False(right.IsSelected); // Discrete time changes before the handle arrives.
        host.Picker.TouchExplorationEnabled = true;
        host.Advance(1000); // The already-started gesture retains its captured auto-switch policy.
        Assert.Equal(TimeSelection.Minute, host.Picker.Selection);
        host.Picker.Selection = TimeSelection.Hour; host.Advance(2000);
        MaterialInputMode.For(host.Dial)!.ObserveFocusNavigation(NavigationMethod.Tab);
        var bottom = (IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(host.Dial.Labels[6])!;
        bottom.Invoke(); host.Draw();
        Assert.Equal(6, host.Picker.SelectedTime.Hour);
        host.Advance(3000); Assert.Equal(TimeSelection.Hour, host.Picker.Selection);
        host.Picker.TouchExplorationEnabled = false;
        MaterialInputMode.For(host.Dial)!.ObserveFocusNavigation(NavigationMethod.Pointer);
        ((IInvokeProvider)right).Invoke(); host.Draw();
        Assert.Equal(3, host.Picker.SelectedTime.Hour); Assert.True(right.IsSelected);
        host.Advance(3200); Assert.Equal(TimeSelection.Minute, host.Picker.Selection);
    }

    [AvaloniaFact]
    public void ReversingCrossfadeKeepsOutgoingMinutesAndFixedInnerHours()
    {
        using var host = new Host(TimeSelection.Minute);
        host.Picker.Selection = TimeSelection.Hour; host.Draw(); host.Advance(64);
        var outgoing = host.Dial.GetVisualDescendants().OfType<Panel>()
            .Single(panel => panel.Tag is TimeSelection.Minute);
        var labels = outgoing.Children.OfType<Button>().ToArray();
        Assert.Equal(24, labels.Length);
        Assert.Equal(new[] { "0", "5", "10", "3" }, labels.Take(4).Select(label => (string)label.Content!));
        Assert.Equal(Enumerable.Range(12, 12).Select(n => n.ToString()), labels.Skip(12).Select(label => (string)label.Content!));
        Assert.All(labels, label => Assert.False(label.Focusable));
        host.Picker.Selection = TimeSelection.Minute; host.Draw(); host.Advance(1000);
        Assert.Equal(12, host.Dial.GetVisualDescendants().OfType<Button>().Count());
        Assert.All(host.Dial.Labels, label => Assert.True(label.IsHitTestVisible));
    }

    [AvaloniaTheory]
    [InlineData(false, false, false)] [InlineData(false, true, false)]
    [InlineData(true, false, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(false, true, true)]
    [InlineData(true, false, true)] [InlineData(true, true, true)]
    public void PeriodToggleIsControlledAndMorphsUsingTheExistingToggleTheme(bool vibrant, bool horizontal, bool rtl)
    {
        using var host = new Host(clock24: false);
        host.Picker.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        host.Picker.Variant = vibrant ? TimePickerVariant.Vibrant : TimePickerVariant.Standard;
        host.Picker.Layout = horizontal ? TimePickerLayout.Horizontal : TimePickerLayout.Vertical;
        host.Draw();
        var hour = host.Picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HourSelector");
        Assert.Equal(vibrant ? new Thickness(2) : new Thickness(0), hour.BorderThickness);
        var periods = host.Picker.GetVisualDescendants().OfType<TimePeriodButton>().ToArray();
        Assert.Equal(2, periods.Length);
        Assert.True(periods[0].IsChecked); Assert.False(periods[1].IsChecked);
        Assert.Equal(FontWeight.Bold, periods[0].FontWeight);
        var pm = periods[1];
        var surface = pm.GetVisualDescendants().OfType<MaterialSurface>().Single();
        Assert.True(surface.EffectiveCornerRadius.TopLeft > 12);
        var provider = (IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(pm)!;
        provider.Invoke(); host.Draw(); host.Advance(640);
        Assert.Equal(12, host.Picker.SelectedTime.Hour);
        Assert.True(pm.IsChecked); Assert.False(periods[0].IsChecked);
        Assert.Equal(FontWeight.Bold, pm.FontWeight);
        Assert.Equal(new CornerRadius(12), surface.EffectiveCornerRadius);
        provider.Invoke(); host.Draw(); host.Advance(1280);
        Assert.True(pm.IsChecked); Assert.Equal(12, host.Picker.SelectedTime.Hour);
        ((ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(periods[0])!).Select(); host.Draw();
        Assert.Equal(0, host.Picker.SelectedTime.Hour); Assert.True(periods[0].IsChecked);
        var gap = horizontal ? pm.Bounds.X - periods[0].Bounds.Right : pm.Bounds.Y - periods[0].Bounds.Bottom;
        Assert.Equal(vibrant ? horizontal ? 16d : 8d : 4d, gap);
    }

    [AvaloniaFact]
    public void SelectorKeyUpDoesNotActivateTheNewlyFocusedLabelAndCancelTheFieldTransition()
    {
        using var host = new Host(selectedTime: new(11, 55));
        var hour = host.Picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HourSelector");
        hour.Focus(NavigationMethod.Tab); host.Draw();
        var time = 0;
        void Key(Key key)
        {
            host.Window.KeyPress(key, 0, PhysicalKey.None, null);
            host.Window.KeyRelease(key, 0, PhysicalKey.None, null);
            host.Draw(); host.Advance(time += 16);
        }
        Key(Avalonia.Input.Key.Enter); Key(Avalonia.Input.Key.Right); Key(Avalonia.Input.Key.Space);
        Key(Avalonia.Input.Key.Tab); Key(Avalonia.Input.Key.Enter);
        Key(Avalonia.Input.Key.Right); Key(Avalonia.Input.Key.Enter);
        Assert.Equal(55, host.Picker.SelectedTime.Minute);
        host.Advance(1000);
        Key(Avalonia.Input.Key.Enter);
        Assert.Equal(0, host.Picker.SelectedTime.Minute);
    }

    [AvaloniaFact]
    public async Task ScopeReplacementBeforeSelectionCommitDoesNotLeaveTheDialStuck()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { Is24Hour = true, SelectedTime = new(0, 30), Layout = TimePickerLayout.Vertical };
        var window = DialogTests.Show(picker);
        try
        {
            var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
            picker.Selection = TimeSelection.Minute;
            var old = AnimationScope.Get(dial);
            await old.Diagnostics.EndAsync();
            Assert.NotSame(old, AnimationScope.Get(dial));
            clock.SendFrame(clock.Now);
            for (var i = 0; i < 64; i++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
            Assert.InRange(Math.Abs(MathF.Sin(dial.CurrentAngle) - 1), 0, .00001);
            Assert.Equal(12, dial.GetVisualDescendants().OfType<Button>().Count());
            Assert.Null(AnimationScope.Get(dial).Diagnostics.LastError);
            window.Content = null; Dispatcher.UIThread.RunJobs();
            window.Content = picker; Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            Assert.Equal(12, dial.Labels.Count);
            Assert.InRange(Math.Abs(MathF.Sin(dial.CurrentAngle) - 1), 0, .00001);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private sealed class Host : IDisposable
    {
        internal ManualFrameSource Clock { get; } = new(Dispatcher.UIThread.VerifyAccess);
        internal ManualTimeProvider Time { get; } = new(Dispatcher.UIThread.VerifyAccess);
        internal AnimationTestWindow Window { get; }
        internal TimePicker Picker { get; }
        internal ClockFace Dial { get; }
        private int _frame;
        internal Host(TimeSelection selection = TimeSelection.Hour, bool clock24 = true, TimeOnly? selectedTime = null)
        {
            Picker = new() { Is24Hour = clock24, SelectedTime = selectedTime ?? new(0, 0), Selection = selection, Layout = TimePickerLayout.Vertical };
            Window = new(Clock, Time) { Width = 360, Height = 480 };
            Window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
            Window.Content = Picker;
            Window.Show(); Draw();
            Dial = Picker.GetVisualDescendants().OfType<ClockFace>().Single();
            Clock.SendFrame(TimeSpan.Zero); Draw();
        }
        internal void Input(string action, Point point)
        {
            var scene = Dial.TranslatePoint(point, Window)!.Value;
            if (action == "down") Window.MouseDown(scene, MouseButton.Left);
            else if (action == "up") Window.MouseUp(scene, MouseButton.Left);
            else Window.MouseMove(scene);
            Draw();
        }
        internal void Draw()
        {
            Dispatcher.UIThread.RunJobs(); Window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        }
        internal void Advance(int ms)
        {
            while (_frame + 16 <= ms)
            {
                _frame += 16; Time.AdvanceBy(TimeSpan.FromMilliseconds(_frame) - Time.Elapsed);
                Dispatcher.UIThread.RunJobs(); Clock.SendFrame(TimeSpan.FromMilliseconds(_frame)); Draw();
            }
        }
        public void Dispose()
        {
            Window.Close(); Draw();
            Assert.Null(Window.AnimationRoot.Diagnostics.LastError);
            Assert.Equal(0, Clock.PendingFrameCount); Assert.Equal(0, Time.ActiveTimerCount);
            Clock.Dispose();
        }
    }
}

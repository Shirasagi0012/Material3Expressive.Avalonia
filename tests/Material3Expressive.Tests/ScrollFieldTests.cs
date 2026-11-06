using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using ComposeAnimation.TestSupport;
using Material3Expressive.Foundation.Input;
using System.Globalization;
using Xunit;

namespace Material3Expressive.Tests;

public class ScrollFieldTests
{
    [AvaloniaTheory]
    [InlineData(1, 0)] [InlineData(60, 17)] [InlineData(7, -1)] [InlineData(4, 99)]
    public void InitialSelectionAndPagesAreVirtualized(int count, int index)
    {
        using var clock = new MotionTestClockScope();
        var state = new ScrollFieldState(count, index);
        var field = new ScrollField { State = state, Width = 120, Height = 200 };
        var window = Show(field);
        Assert.Equal(Math.Clamp(index, 0, count - 1), state.SelectedOption);
        Assert.Equal(state.SelectedOption, state.TargetOption);
        var pages = field.GetVisualDescendants().OfType<ScrollFieldNumber>().ToArray();
        Assert.InRange(pages.Length, 3, 4);
        var selected = Assert.Single(pages, p => p.IsSelected);
        Assert.Equal(57, selected.FontSize);
        Assert.Equal(state.SelectedOption.ToString("D2"), selected.Text);
        Assert.All(pages.Where(p => !p.IsSelected), p => Assert.Equal(45, p.FontSize));
        Assert.False(state.IsScrollInProgress);
        window.Close();
    }
    [AvaloniaFact]
    public void InvalidCountIsRejectedAndUnmountedAssignmentClamps()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScrollFieldState(0));
        var state = new ScrollFieldState(7, 3); state.SelectedOption = 9;
        Assert.Equal(6, state.SelectedOption); state.SelectedOption = -1;
        Assert.Equal(0, state.SelectedOption);
        Assert.Throws<InvalidOperationException>(() => { _ = state.AnimateScrollToOptionAsync(3); });
    }
    [AvaloniaFact]
    public void KeyboardUsesTargetPageAndWrapsWhileRetainingFocus()
    {
        using var clock = new MotionTestClockScope();
        var state = new ScrollFieldState(4, 3);
        var field = new ScrollField { State = state, Width = 100, Height = 200 };
        var window = Show(field); clock.SendFrame(TimeSpan.Zero); field.Focus();
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Assert.Equal(0, state.TargetOption);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Assert.Equal(1, state.TargetOption);
        Advance(clock, window, 0, 800);
        Assert.Equal(1, state.SelectedOption); Assert.False(state.IsScrollInProgress); Assert.True(field.IsFocused);
        window.Close();
    }
    [AvaloniaFact]
    public void ProgrammaticAnimationExposesTargetAndCanBeReversed()
    {
        using var clock = new MotionTestClockScope();
        var state = new ScrollFieldState(60, 20);
        var field = new ScrollField { State = state, Width = 120, Height = 200 };
        var window = Show(field); clock.SendFrame(TimeSpan.Zero);
        var first = state.AnimateScrollToOptionAsync(21);
        Assert.Equal(21, state.TargetOption); Assert.True(state.IsScrollInProgress);
        Advance(clock, window, 0, 64);
        var second = state.AnimateScrollToOptionAsync(19);
        Assert.True(first.IsCompletedSuccessfully);
        Advance(clock, window, 64, 864);
        Assert.True(second.IsCompletedSuccessfully); Assert.Equal(19, state.SelectedOption);
        window.Close();
    }
    [AvaloniaFact]
    public void FarJumpRealizesOnlyTheDestinationViewportAndCancellationRetainsPosition()
    {
        using var clock = new MotionTestClockScope();
        var state = new ScrollFieldState(1000);
        var field = new ScrollField { State = state, Width = 120, Height = 200 };
        var window = Show(field); clock.SendFrame(TimeSpan.Zero);
        using var cancel = new CancellationTokenSource();
        var task = state.AnimateScrollToOptionAsync(900, cancel.Token); Flush(window);
        Assert.InRange(state.SelectedOption, 895, 899);
        Assert.InRange(field.GetVisualDescendants().OfType<ScrollFieldNumber>().Count(), 3, 4);
        Advance(clock, window, 0, 64); cancel.Cancel(); Dispatcher.UIThread.RunJobs();
        Assert.True(task.IsCompletedSuccessfully); Assert.False(state.IsScrollInProgress);
        var option = state.SelectedOption;
        Advance(clock, window, 64, 864); Assert.Equal(option, state.SelectedOption);
        window.Close();
    }
    [AvaloniaFact]
    public void TemplateKeepsPageDataAndUsesOriginalCycleOnClick()
    {
        using var clock = new MotionTestClockScope();
        var state = new ScrollFieldState(4, 3);
        var field = new ScrollField { State = state, Width = 120, Height = 200,
            ItemTemplate = new FuncDataTemplate<ScrollFieldItem>((item, _) => new Border { Width = 32, Height = 24, Background = Brushes.Red }) };
        var window = Show(field); clock.SendFrame(TimeSpan.Zero);
        var panel = field.GetVisualDescendants().OfType<ScrollFieldPanel>().Single();
        var following = panel.Children.Cast<ClickableSurface>().Single(v => ((ScrollFieldItem)v.Content!).Index == 0);
        ControlAutomationPeer.CreatePeerForElement(following)!.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(0, state.TargetOption);
        // Clicks call animateScrollToOption(0), so the preceding cycle start is targeted.
        Assert.True(state.TargetPage < state.CurrentPage);
        Advance(clock, window, 0, 800); Assert.Equal(0, state.SelectedOption);
        window.Close();
    }
    [AvaloniaFact]
    public void NestedTemplateButtonDoesNotAlsoSelectItsPage()
    {
        using var clock = new MotionTestClockScope();
        var clicks = 0;
        var field = new ScrollField { State = new(7, 3), Width = 120, Height = 200,
            ItemTemplate = new FuncDataTemplate<ScrollFieldItem>((item, _) =>
            {
                var button = new Button { Content = item!.Index, Width = 32, Height = 24, Tag = item.Index };
                button.Click += (_, _) => clicks++;
                return button;
            }) };
        var window = Show(field); clock.SendFrame(TimeSpan.Zero);
        var button = field.GetVisualDescendants().OfType<Button>().Single(b => b.Tag is 4);
        ControlAutomationPeer.CreatePeerForElement(button)!.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks); Assert.Equal(3, field.State.TargetOption); Assert.False(field.State.IsScrollInProgress);
        window.Close();
    }

    [AvaloniaFact]
    public void AutomationExposesVirtualPageRangeAndTargetDescriptionWithoutChildNodes()
    {
        using var clock = new MotionTestClockScope();
        var field = new ScrollField { State = new(4, 3), Width = 120, Height = 200, OptionDescription = index => $"Option {index}" };
        AutomationProperties.SetName(field, "Unit");
        var window = Show(field); clock.SendFrame(TimeSpan.Zero);
        var peer = ControlAutomationPeer.CreatePeerForElement(field)!;
        var range = peer.GetProvider<IRangeValueProvider>()!;
        Assert.Equal(99999, range.Maximum); Assert.Equal(0, range.Minimum);
        Assert.Empty(peer.GetChildren()!); Assert.Equal("Option 3", peer.GetItemStatus());
        range.SetValue(range.Value + 1);
        Assert.Equal("Option 0", peer.GetItemStatus());
        Advance(clock, window, 0, 800); Assert.Equal(0, field.State.SelectedOption);
        window.Close();
    }
    [AvaloniaFact]
    public void StateReplacementAndDetachCancelOnlyOwnedAnimation()
    {
        using var clock = new MotionTestClockScope();
        var old = new ScrollFieldState(60, 20);
        var field = new ScrollField { State = old, Width = 120, Height = 200 };
        var window = Show(field); clock.SendFrame(TimeSpan.Zero);
        var task = old.AnimateScrollToOptionAsync(25); Advance(clock, window, 0, 48);
        field.State = new(5, 2); Flush(window);
        Assert.True(task.IsCompletedSuccessfully); Assert.Equal(2, field.State.SelectedOption);
        var next = field.State.AnimateScrollToOptionAsync(3); Advance(clock, window, 48, 96);
        window.Content = null; Flush(window);
        Assert.True(next.IsCompletedSuccessfully); Assert.False(field.State.IsScrollInProgress);
        window.Content = field; Flush(window); field.State.SelectedOption = 4; Flush(window);
        Assert.Equal(4, field.State.SelectedOption);
        window.Close();
    }
    [AvaloniaFact]
    public void TouchDragAppliesSlopAndCaptureCancellationSettlesWithoutChangingFocusSource()
    {
        using var clock = new MotionTestClockScope();
        var state = new ScrollFieldState(7, 3);
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var field = new ScrollField { State = state, Width = 120, Height = 200, InteractionSource = source };
        var window = Show(field); clock.SendFrame(TimeSpan.Zero);
        using (window.CaptureRenderedFrame()) { }
        using var touch = Activator.CreateInstance<TouchDevice>();
        var impl = typeof(TopLevel).GetProperty("PlatformImpl", System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(window)!;
        var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
        var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
        void Send(RawPointerEventType type, ulong time, double y)
        {
            input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs), touch, time, root,
                type, new Point(60, y), RawInputModifiers.None, 1L)!);
            Flush(window);
        }
        Send(RawPointerEventType.TouchBegin, 1000, 100);
        Send(RawPointerEventType.TouchUpdate, 1016, 90);
        Assert.False(state.IsScrollInProgress); Assert.Equal(0, state.OffsetFraction);
        Send(RawPointerEventType.TouchUpdate, 1032, 60);
        Assert.True(state.IsScrollInProgress);
        Assert.Equal(22f / state.PageSize, state.OffsetFraction);
        Assert.False(state.AnimateScrollToOptionAsync(5).GetAwaiter().GetResult());
        Assert.False(state.ScrollToOptionAsync(5).GetAwaiter().GetResult());
        Assert.True(state.IsScrollInProgress); // Default mutations cannot cancel UserInput.
        state.SelectedOption = 0;
        Send(RawPointerEventType.TouchUpdate, 1040, 50);
        Assert.True(state.IsScrollInProgress); Assert.Equal(0, state.SelectedOption);
        Assert.Equal(10f / state.PageSize, state.OffsetFraction);
        Send(RawPointerEventType.TouchCancel, 1048, 60);
        Advance(clock, window, 0, 800);
        Assert.False(state.IsScrollInProgress); Assert.Equal(0, state.OffsetFraction);
        Assert.DoesNotContain(events, e => e is MaterialInteractionEvent.DragStart);
        // Applications may navigate away in response to the first scrolling notification.
        IPointer? pointer = null;
        field.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer,
            Avalonia.Interactivity.RoutingStrategies.Tunnel);
        state.PropertyChanged += (_, e) =>
        {
            if (e.Property == ScrollFieldState.IsScrollInProgressProperty && state.IsScrollInProgress)
                window.Content = null;
        };
        Send(RawPointerEventType.TouchBegin, 2000, 100);
        Send(RawPointerEventType.TouchUpdate, 2016, 60);
        Assert.NotNull(pointer); Assert.NotSame(field, pointer.Captured);
        Assert.False(state.IsScrollInProgress); Assert.Equal(0, state.OffsetFraction);
        window.Close();
    }

    [AvaloniaFact]
    public void WheelSmoothsAndSnapsAndStateReplacementCancelsPendingTimeout()
    {
        using var frames = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(frames, time) { Width = 300, Height = 260 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        var field = new ScrollField { State = new(60, 12), Width = 120, Height = 200,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        window.Content = field; window.Show(); Flush(window);
        using (window.CaptureRenderedFrame()) { }
        frames.SendFrame(TimeSpan.Zero); Flush(window);
        window.MouseWheel(new Point(60, 100), new Vector(0, -12), RawInputModifiers.None);
        Assert.True(field.State.IsScrollInProgress);
        Assert.False(field.State.AnimateScrollToOptionAsync(20).GetAwaiter().GetResult());
        Assert.InRange(field.State.OffsetFraction * field.State.PageSize, 5.9f, 6.1f);
        AdvanceWheel(100);
        Assert.False(field.State.IsScrollInProgress); Assert.Equal(0, field.State.OffsetFraction);
        Assert.NotEqual(12, field.State.SelectedOption);
        window.MouseWheel(new Point(60, 100), new Vector(0, -.1), RawInputModifiers.None);
        Assert.True(field.State.IsScrollInProgress); Assert.True(time.ActiveTimerCount > 0);
        var old = field.State; field.State = new(5, 2); Flush(window);
        AdvanceWheel(20);
        Assert.False(old.IsScrollInProgress); Assert.Equal(2, field.State.SelectedOption);
        Assert.Equal(0, time.ActiveTimerCount);
        window.MouseWheel(new Point(60, 100), new Vector(0, -.1), RawInputModifiers.None);
        field.State.SelectedOption = 0;
        // Do not flush the canceled receiver before accepting the next wheel event.
        window.MouseWheel(new Point(60, 100), new Vector(0, -12), RawInputModifiers.None);
        AdvanceWheel(100);
        Assert.NotEqual(0, field.State.SelectedOption); Assert.False(field.State.IsScrollInProgress);
        window.Close(); Flush(window);
        void AdvanceWheel(int count)
        {
            for (var i = 0; i < count; i++)
            {
                time.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
                frames.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
            }
        }
    }

    [AvaloniaFact]
    public void NumberItemLocalizesDigitsAndReversesItsSelectionAnimation()
    {
        using var clock = new MotionTestClockScope();
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
            var number = new ScrollFieldNumber { Index = 7, SelectedContentColor = Colors.Red, UnselectedContentColor = Colors.Blue };
            var window = Show(number); clock.SendFrame(TimeSpan.Zero);
            Assert.Equal("٠٧", number.Text); Assert.Equal(45, number.FontSize);
            number.IsSelected = true; Advance(clock, window, 0, 80);
            Assert.True(number.FontSize > 45);
            number.IsSelected = false; Advance(clock, window, 80, 880);
            Assert.Equal(45, number.FontSize); Assert.Equal(Colors.Blue, ((ISolidColorBrush)number.Foreground!).Color);
            number.Index = 2026; Assert.Equal("٢٠٢٦", number.Text);
            window.Content = null; Flush(window); number.IsSelected = true;
            window.Content = number; Flush(window); Assert.Equal(57, number.FontSize);
            window.Close();
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void DifferentialVelocityUsesImpulseAndDiscardsOldGestureHistory()
    {
        var tracker = new DifferentialVelocityTracker();
        tracker.Add(1000, 10); Assert.Equal(0, tracker.Calculate());
        tracker.Add(1010, 10); Assert.Equal(1000, tracker.Calculate(), 3);
        tracker.Add(1020, 10); Assert.Equal(1000, tracker.Calculate(), 3);
        tracker.Add(1200, -10); Assert.Equal(0, tracker.Calculate());
        tracker.Add(1210, -10); Assert.Equal(-1000, tracker.Calculate(), 3);
    }

    [AvaloniaFact]
    public void FocusSourceReplacementPairsEventsAndDoesNotMoveSelection()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var field = new ScrollField { State = new(5, 2), Width = 120, Height = 200, InteractionSource = source };
        var window = Show(field); field.Focus(); field.InteractionSource = new();
        var focus = Assert.IsType<MaterialInteractionEvent.Focus>(events[0]);
        Assert.Same(focus, Assert.IsType<MaterialInteractionEvent.Unfocus>(events[1]).Start);
        Assert.Equal(2, field.State.SelectedOption); window.Close();
    }
    private static MotionTestWindow Show(Control field)
    {
        var window = new MotionTestWindow { Width = 400, Height = 300 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        field.HorizontalAlignment = HorizontalAlignment.Left; field.VerticalAlignment = VerticalAlignment.Top;
        window.Content = field; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Advance(MotionTestClockScope clock, Window window, int from, int to)
    {
        for (var time = from + 16; time <= to; time += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(time)); Flush(window); }
    }
}

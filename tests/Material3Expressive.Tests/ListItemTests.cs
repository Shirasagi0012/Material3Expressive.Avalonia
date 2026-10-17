using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class ListItemTests
{
    [AvaloniaTheory]
    [InlineData("chip")] [InlineData("fab")] [InlineData("slider")] [InlineData("range")]
    [InlineData("segmented")]
    public void InteractiveEnvironmentUpdatesSharedConsumers(string kind)
    {
        using var clock = new MotionTestClockScope();
        Control child = kind switch
        {
            "chip" => new AssistChip(),
            "fab" => new FloatingActionButton { Size = FloatingActionButtonSize.Small },
            "slider" => new Slider(),
            "range" => new RangeSlider(),
            _ => new SingleChoiceSegmentedButtonRow { ItemsSource = new[] { new SegmentedButton { Content = "A" }, new SegmentedButton { Content = "B" } } },
        };
        var root = new StackPanel { Children = { child } };
        MaterialInteractiveSize.SetMinimum(root, 80);
        var window = new MotionTestWindow { Width = 360, Height = 240 };
        window.Styles.Add(new MaterialTheme()); window.Content = root; window.Show(); window.UpdateLayout();
        try
        {
            Assert.Equal(80, child.Bounds.Height);
            MaterialInteractiveSize.SetMinimum(root, 20); window.UpdateLayout();
            Assert.True(child.Bounds.Height < 80, $"{kind}: {child.Bounds}");
            MaterialInteractiveSize.SetMinimum(root, null); window.UpdateLayout();
            Assert.True(child.Bounds.Height >= 48, $"{kind}: {child.Bounds}");
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void XamlSlotsShapesBindingAndHelpTextWorkTogether()
    {
        using var clock = new MotionTestClockScope();
        var root = (StackPanel)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/ListItem.axaml"));
        var item = (ListItem)root.Children[0];
        var selected = (SegmentedListItem)root.Children[1];
        var window = new MotionTestWindow { Width = 320, Height = 300 };
        window.Styles.Add(new MaterialTheme()); window.Content = root; window.Show(); window.UpdateLayout();
        try
        {
            Assert.Equal(88, item.Bounds.Height);
            Assert.Equal(new CornerRadius(2), item.Surface!.EffectiveCornerRadius);
            Assert.Equal(new CornerRadius(24), item.Shapes!.PressedShape);
            Assert.Equal("More actions", ControlAutomationPeer.CreatePeerForElement(item)!.GetHelpText());
            Assert.Equal(5, item.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                .Count(p => p.Name?.StartsWith("PART_") == true && p.Child is Border));
            Assert.True(selected.IsChecked);
            Click(selected, window); Assert.True(selected.IsChecked);
            ((CheckBox)root.Children[2]).IsChecked = false;
            Assert.False(selected.IsChecked);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ReattachAndTemplateReplacementReleaseOldAnimationScopes()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var item = new ListItem { InteractionMode = ListItemInteractionMode.Click, InteractionSource = source };
        var window = Show(item);
        try
        {
            source.Emit(new MaterialInteractionEvent.DragStart());
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            var oldSurface = item.Surface;
            var template = item.Template;
            item.Template = null; item.ApplyTemplate();
            item.Template = template; item.ApplyTemplate(); window.UpdateLayout();
            Assert.NotSame(oldSurface, item.Surface);
            var root = (Grid)window.Content!;
            root.Children.Remove(item);
            Assert.Equal(0, clock.PendingFrameCount);
            root.Children.Add(item); window.UpdateLayout();
            var start = new MaterialInteractionEvent.DragStart(); source.Emit(start);
            for (var i = 0; i < 80; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(item.DraggedElevation, item.AnimatedElevation);
            source.Emit(new MaterialInteractionEvent.DragStop(start));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SideInteractiveSizeUpdatesAndNativeContainerMinimaSurvive()
    {
        using var clock = new MotionTestClockScope();
        var checkbox = new CheckBox(); var toggle = new ToggleSwitch();
        var item = new ListItem { LeadingContent = checkbox, TrailingContent = toggle };
        var window = Show(item);
        try
        {
            Assert.Equal(24, checkbox.Bounds.Height); Assert.Equal(32, toggle.Bounds.Height);
            MaterialInteractiveSize.SetMinimum(item, 100); window.UpdateLayout();
            Assert.Equal(72, checkbox.Bounds.Height); Assert.Equal(72, toggle.Bounds.Width);
            Assert.Equal(72, toggle.Bounds.Height);
            item.Padding = new Thickness(8, 10, 20, 10); window.UpdateLayout();
            Assert.Equal(80, MaterialInteractiveSize.GetMinimum(checkbox));
            Assert.Equal(68, MaterialInteractiveSize.GetMinimum(toggle));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false, 56)] [InlineData(true, false, 72)] [InlineData(false, true, 72)] [InlineData(true, true, 88)]
    public void SlotsSelectDefaultMinimumHeight(bool overline, bool supporting, double height)
    {
        using var clock = new MotionTestClockScope();
        var item = new ListItem { Content = new Border { Width = 70, Height = 24 },
            OverlineContent = overline ? new Border { Width = 40, Height = 16 } : null,
            SupportingContent = supporting ? new Border { Width = 60, Height = 20 } : null };
        var window = Show(item);
        try { Assert.Equal(height, item.Bounds.Height); Assert.Equal(320, item.Bounds.Width); }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void MultilineSupportingAndLargeLeadingIncreaseHeight()
    {
        using var clock = new MotionTestClockScope();
        var item = new ListItem { Content = "Headline", SupportingContent = new TextBlock { Text = "First\nSecond" } };
        var window = Show(item);
        try
        {
            Assert.True(item.Bounds.Height >= 88);
            item.LeadingContent = new Border { Width = 64, Height = 100 };
            window.UpdateLayout();
            Assert.True(item.Bounds.Height >= 120);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SelectionIsControlledAndEnterActivatesOnRelease()
    {
        using var clock = new MotionTestClockScope();
        var item = new ListItem { InteractionMode = ListItemInteractionMode.MultipleSelection, Content = "Item" };
        var requests = new List<bool>(); item.CheckedChangeRequested += (_, e) => requests.Add(e.Value);
        var window = Show(item);
        try
        {
            Click(item, window); Assert.Equal([true], requests); Assert.False(item.IsChecked);
            item.IsChecked = true; item.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Single(requests);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal([true, false], requests); Assert.True(item.IsChecked);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DisabledRowRetainsNestedButtonInput()
    {
        using var clock = new MotionTestClockScope();
        var child = new Button { Content = "Help" };
        var item = new ListItem { InteractionMode = ListItemInteractionMode.Click, IsInteractionEnabled = false, TrailingContent = child, Content = "Item" };
        var childClicks = 0; var rowClicks = 0;
        child.Click += (_, _) => childClicks++;
        item.Click += (_, e) => { if (ReferenceEquals(e.Source, item)) rowClicks++; };
        var window = Show(item);
        try
        {
            Assert.True(child.IsEffectivelyEnabled); Click(child, window); Assert.Equal(1, childClicks); Assert.Equal(0, rowClicks);
            item.IsInteractionEnabled = true; window.UpdateLayout(); Click(child, window);
            Assert.Equal(2, childClicks); Assert.Equal(0, rowClicks);
            Assert.Equal(0, MaterialInteraction.GetOrCreate(item).RippleCount);
            item.IsEnabled = false; Assert.False(child.IsEffectivelyEnabled);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PairedExternalDragsRetainStateUntilLastEnd()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var item = new ListItem { InteractionMode = ListItemInteractionMode.Click, InteractionSource = source };
        var window = Show(item);
        try
        {
            var first = new MaterialInteractionEvent.DragStart(); var second = new MaterialInteractionEvent.DragStart();
            source.Emit(first); source.Emit(second); source.Emit(new MaterialInteractionEvent.DragStop(first));
            Assert.True(item.Interactions.HasFlag(InteractionState.Dragged));
            for (var i = 0; i < 80; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(item.DraggedElevation, item.AnimatedElevation); Assert.Equal(1, item.ZIndex);
            source.Emit(new MaterialInteractionEvent.DragCancel(second));
            Assert.False(item.Interactions.HasFlag(InteractionState.Dragged));
            for (var i = 0; i < 80; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(item.Elevation, item.AnimatedElevation); Assert.Equal(0, item.ZIndex);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(0, 1, 16, 16)] [InlineData(0, 3, 16, 4)] [InlineData(1, 3, 4, 4)] [InlineData(2, 3, 4, 16)]
    public void GroupPositionsOnlyOverrideBaseShape(int index, int count, double top, double bottom)
    {
        using var clock = new MotionTestClockScope();
        var item = new SegmentedListItem { GroupIndex = index, GroupCount = count };
        var window = Show(item);
        try
        {
            Assert.Equal(new CornerRadius(top, top, bottom, bottom), item.ResolveShapes().Shape);
            Assert.Equal(new CornerRadius(12), item.ResolveShapes().HoveredShape);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LeadingCheckboxConsumesReducedInteractiveEnvironment()
    {
        using var clock = new MotionTestClockScope();
        var checkbox = new CheckBox(); var item = new ListItem { LeadingContent = checkbox };
        var window = Show(item);
        try
        {
            Assert.Equal(24, checkbox.Bounds.Width); Assert.Equal(24, checkbox.Bounds.Height);
            Assert.Equal(20, MaterialInteractiveSize.GetMinimum(checkbox));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LongPressUsesOrdinaryTimeAndSuppressesClickUntilNextGesture()
    {
        using var frames = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var time = new ManualTimeProvider(Dispatcher.UIThread.VerifyAccess);
        var item = new ListItem { InteractionMode = ListItemInteractionMode.Click, Content = "Hold" };
        var longClicks = 0; var clicks = 0;
        item.LongClick += (_, _) => longClicks++;
        item.Click += (_, _) => clicks++;
        var window = new AnimationTestWindow(frames, time) { Width = 320, Height = 240 };
        window.Styles.Add(new MaterialTheme()); window.Content = new Grid { Children = { item } }; window.Show(); window.UpdateLayout();
        try
        {
            var p = new Point(160, 28);
            window.MouseMove(p); window.MouseDown(p, MouseButton.Left);
            Dispatcher.UIThread.RunJobs(); Assert.True(item.IsPressed, $"bounds={item.Bounds}; hit={window.InputHitTest(p)}; enabled={item.RowEnabled}"); Assert.Equal(1, time.ActiveTimerCount);
            time.AdvanceBy(TimeSpan.FromMilliseconds(499)); Dispatcher.UIThread.RunJobs(); Assert.Equal(0, longClicks);
            time.AdvanceBy(TimeSpan.FromMilliseconds(1)); Dispatcher.UIThread.RunJobs(); Assert.Equal(1, longClicks);
            window.MouseUp(p, MouseButton.Left); Assert.Equal(0, clicks);
            Click(item, window); Assert.Equal(1, clicks);
            window.MouseDown(p, MouseButton.Left); window.MouseMove(new Point(160, 100), RawInputModifiers.LeftMouseButton);
            time.AdvanceBy(TimeSpan.FromSeconds(1)); Dispatcher.UIThread.RunJobs();
            window.MouseUp(new Point(160, 100), MouseButton.Left); Assert.Equal(1, longClicks);
            item.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(Key.A, RawInputModifiers.None, PhysicalKey.None, null);
            time.AdvanceBy(TimeSpan.FromMilliseconds(500)); Dispatcher.UIThread.RunJobs();
            Assert.Equal(3, longClicks);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.True(item.Interactions.HasFlag(InteractionState.Pressed));
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.False(item.Interactions.HasFlag(InteractionState.Pressed));
            Assert.Equal(1, clicks);
        }
        finally { window.Close(); Dispatcher.UIThread.RunJobs(); }
        Assert.Equal(0, time.ActiveTimerCount); Assert.Equal(0, frames.PendingFrameCount);
        Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public void ExternalInteractionsSurviveDisabledInputAndPreserveArrivalOrder()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var item = new ListItem { InteractionMode = ListItemInteractionMode.Click, InteractionSource = source };
        var window = Show(item);
        try
        {
            var hover1 = new MaterialInteractionEvent.HoverEnter(); var hover2 = new MaterialInteractionEvent.HoverEnter();
            var focus = new MaterialInteractionEvent.Focus(); var drag = new MaterialInteractionEvent.DragStart();
            source.Emit(hover1); source.Emit(focus); source.Emit(hover2);
            var controller = MaterialInteraction.GetOrCreate(item);
            Assert.Equal(InteractionState.Hovered, controller.LastInteraction);
            source.Emit(new MaterialInteractionEvent.HoverExit(hover2)); Assert.Equal(InteractionState.Focused, controller.LastInteraction);
            source.Emit(drag); item.IsInteractionEnabled = false;
            Assert.True(item.Interactions.HasFlag(InteractionState.Dragged));
            for (var i = 0; i < 80; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(item.DraggedElevation, item.AnimatedElevation);
            Assert.Equal(item.DisabledContainerColor, item.AnimatedContainerColor);
            item.InteractionSource = new(); Assert.Equal(InteractionState.None, item.Interactions);
            source.Emit(new MaterialInteractionEvent.HoverEnter()); Assert.Equal(InteractionState.None, item.Interactions);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void PointerCancellationEmitsMatchingCancelAndDoesNotClickOnReentry()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var item = new ListItem { InteractionMode = ListItemInteractionMode.Click, InteractionSource = source };
        var clicks = 0; item.Click += (_, _) => clicks++;
        var window = Show(item);
        try
        {
            var p = new Point(160,28); window.MouseMove(p);window.MouseDown(p,MouseButton.Left);
            window.MouseMove(new Point(160,100),RawInputModifiers.LeftMouseButton);
            window.MouseMove(p,RawInputModifiers.LeftMouseButton);window.MouseUp(p,MouseButton.Left);
            Assert.Equal(0,clicks);
            Assert.Same(Assert.Single(events.OfType<MaterialInteractionEvent.Press>()),Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
            Assert.Empty(events.OfType<MaterialInteractionEvent.Release>());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AutomationUsesControlledSelectionAndModeSpecificRoles()
    {
        using var clock = new MotionTestClockScope();
        var item = new ListItem { InteractionMode = ListItemInteractionMode.MultipleSelection };
        var requests = 0; item.CheckedChangeRequested += (_, _) => requests++;
        var window = Show(item);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Equal(AutomationControlType.CheckBox,peer.GetAutomationControlType());
            var toggle=Assert.IsAssignableFrom<IToggleProvider>(peer.GetProvider<IToggleProvider>());
            toggle.Toggle();Assert.Equal(1,requests);Assert.False(item.IsChecked);
            item.IsInteractionEnabled=false;toggle.Toggle();Assert.Equal(1,requests);Assert.False(peer.IsEnabled());
            item.InteractionMode=ListItemInteractionMode.SingleSelection;
            Assert.Equal(AutomationControlType.RadioButton,peer.GetAutomationControlType());
            Assert.Null(peer.GetProvider<IToggleProvider>());
            item.InteractionMode=ListItemInteractionMode.Click;
            var clicks = 0; item.Click += (_, _) => clicks++;
            var invoke = peer.GetProvider<IInvokeProvider>()!;
            Assert.Throws<Avalonia.Automation.ElementNotEnabledException>(() => invoke.Invoke());
            Assert.Equal(0, clicks);
            item.IsInteractionEnabled = true;
            invoke.Invoke(); Dispatcher.UIThread.RunJobs(); Assert.Equal(1, clicks);
            item.InteractionMode=ListItemInteractionMode.None;Assert.Null(peer.GetProvider<IInvokeProvider>());
        }
        finally { window.Close(); }
    }

    static Window Show(ListItem item)
    {
        var window = new MotionTestWindow { Width = 320, Height = 240 };
        window.Styles.Add(new MaterialTheme()); window.Content = new Grid { Children = { item } };
        window.Show(); window.UpdateLayout(); return window;
    }
    static void Click(Control control, Window window)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left);
        Assert.True(((Button)control).IsPressed, $"Not pressed: {control.Bounds}, hit={window.InputHitTest(point)}");
        window.MouseUp(point, MouseButton.Left);
    }
}

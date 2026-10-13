using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class SwitchTests
{
    [AvaloniaTheory]
    [InlineData(false, false, 16, 8)]
    [InlineData(true, false, 24, 24)]
    [InlineData(false, true, 24, 4)]
    [InlineData(true, true, 24, 24)]
    public void Template_UsesNativeOwnerAndMaterialGeometry(bool state, bool icon, double size, double offset)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { IsChecked = state };
        if (icon) SwitchAppearance.SetThumbContent(box, new Border { Width = 16, Height = 16 });
        var window = Show(box);
        Assert.Equal(new Size(52, 48), box.Bounds.Size);
        Assert.Equal(size, Thumb(box).PaintedSize); Assert.Equal(offset, Thumb(box).PaintedOffset);
        var surface = box.GetVisualDescendants().OfType<MaterialSurface>().Single();
        Assert.False(Material3Expressive.Foundation.Interaction.MaterialInteraction.GetOrCreate(box).IsRippleBounded);
        Assert.Equal(20, Material3Expressive.Foundation.Interaction.MaterialInteraction.GetOrCreate(box).RippleRadius);
        Assert.All(surface.GetVisualAncestors().Prepend(surface).OfType<Control>().Where(c => c != window),
            control => Assert.False(control.ClipToBounds, control.GetType().Name));
        Assert.Null(box.OnContent); Assert.Null(box.OffContent);
        var peer = ControlAutomationPeer.CreatePeerForElement(box)!;
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer);
        toggle.Toggle(); Assert.Equal(!state, box.IsChecked);
        box.IsEnabled = false; Assert.ThrowsAny<Exception>(() => toggle.Toggle());
        window.Close();
    }

    [AvaloniaFact]
    public void Click_PreservesBindingAndUpdatesStateBeforeCommand()
    {
        using var clock = new MotionTestClockScope();
        var source = new SwitchState(); var box = new ToggleSwitch();
        box.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(SwitchState.Value)) { Source = source });
        var observed = new List<bool?>(); box.Click += (_, _) => observed.Add(box.IsChecked);
        var commands = new List<bool?>(); box.Command = new ObserveCommand(() => commands.Add(box.IsChecked));
        var window = Show(box);
        Click(window, box); Assert.True(source.Value); Assert.Equal(new bool?[] { true }, observed);
        source.Value = false; Assert.False(box.IsChecked);
        Click(window, box); Assert.True(source.Value);
        Assert.Equal(new bool?[] { true, true }, commands); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter, true)]
    [InlineData(Key.Space, false)]
    public void Keyboard_PreservesNativeActivation(Key key, bool onDown)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch(); var window = Show(box); var clicks = 0;
        box.Click += (_, _) => clicks++; box.Focus(NavigationMethod.Tab);
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(onDown ? 1 : 0, clicks); Assert.Equal(onDown, box.IsChecked);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(1, clicks); Assert.True(box.IsChecked); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false, FlowDirection.LeftToRight, 16)]
    [InlineData(true, FlowDirection.LeftToRight, 16)]
    [InlineData(false, FlowDirection.RightToLeft, 16)]
    [InlineData(true, FlowDirection.RightToLeft, 16)]
    [InlineData(false, FlowDirection.LeftToRight, 6)]
    [InlineData(true, FlowDirection.LeftToRight, 6)]
    [InlineData(false, FlowDirection.RightToLeft, 6)]
    [InlineData(true, FlowDirection.RightToLeft, 6)]
    public void Drag_UsesNativeThresholdAndStateWriteback(bool state, FlowDirection direction, double distance)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { IsChecked = state, FlowDirection = direction }; var window = Show(box);
        var moving = Moving(box);
        Assert.Equal(state ? 20 : 0, Canvas.GetLeft(moving));
        var start = moving.TranslatePoint(new Point(moving.Bounds.Width / 2, moving.Bounds.Height / 2), window)!.Value;
        var sign = (state ? -1 : 1) * (direction == FlowDirection.RightToLeft ? -1 : 1);
        var end = start + new Vector(sign * distance, 0);
        window.MouseDown(start, MouseButton.Left); window.MouseMove(end);
        Assert.Contains(":dragging", box.Classes);
        window.MouseUp(end, MouseButton.Left);
        Assert.Equal(distance > 10 ? !state : state, box.IsChecked);
        Assert.DoesNotContain(":dragging", box.Classes); window.Close();
    }

    public static IEnumerable<object[]> DragHandoffs()
    {
        foreach (var state in new[] { false, true })
        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        foreach (var distance in new[] { 6d, 16d, 20d })
        foreach (var warm in new[] { false, true })
            yield return [state, direction, distance, warm, 1d];
        foreach (var state in new[] { false, true })
        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            yield return [state, direction, 20d, true, 10d];
    }

    [AvaloniaTheory]
    [MemberData(nameof(DragHandoffs))]
    public void DragRelease_ContinuesFromTheDisplayedPose(bool state, FlowDirection direction,
        double distance, bool warm, double durationScale)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { IsChecked = state, FlowDirection = direction }; var window = Show(box);
        if (warm)
        {
            box.IsChecked = !state; clock.SendFrame(clock.Now); window.UpdateLayout(); Advance(800);
            box.IsChecked = state; clock.SendFrame(clock.Now); window.UpdateLayout(); Advance(800);
        }
        AnimationScope.SetDurationScale(window, durationScale);
        var clicks = 0; box.Click += (_, _) => clicks++;
        var moving = Moving(box);
        var start = moving.TranslatePoint(new Point(moving.Bounds.Width / 2, moving.Bounds.Height / 2), window)!.Value;
        var sign = (state ? -1 : 1) * (direction == FlowDirection.RightToLeft ? -1 : 1);
        var end = start + new Vector(sign * distance, 0);
        window.MouseDown(start, MouseButton.Left); window.UpdateLayout();
        clock.SendFrame(clock.Now); window.UpdateLayout();
        Assert.Equal(28, Thumb(box).PaintedSize);
        window.MouseMove(end); window.UpdateLayout();
        var releaseCenter = Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2;
        window.MouseUp(end, MouseButton.Left); window.UpdateLayout();
        Assert.Equal(distance > 10 ? !state : state, box.IsChecked);
        Assert.Equal(28, Thumb(box).PaintedSize);
        Assert.Equal(releaseCenter, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        clock.SendFrame(clock.Now); window.UpdateLayout();
        Assert.Equal(releaseCenter, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        var targetCenter = box.IsChecked == true ? 36d : 16d;
        var pixelRounding = 1.5 / LayoutHelper.GetLayoutScale(Thumb(box));
        Advance(96 * durationScale, () =>
        {
            var center = Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2;
            // Springs may overshoot the target; they must never restart behind the release pose.
            // Independent physical-pixel truncation contributes <1px for offset and <.5px for radius.
            if (targetCenter >= releaseCenter) Assert.True(center >= releaseCenter - pixelRounding, $"{releaseCenter} -> {center}");
            if (targetCenter <= releaseCenter) Assert.True(center <= releaseCenter + pixelRounding, $"{releaseCenter} -> {center}");
        });
        Advance(800 * durationScale);
        Assert.Equal(targetCenter, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        Assert.Equal(box.IsChecked == true ? 24 : 16, Thumb(box).PaintedSize);
        Assert.Equal(0, clicks); window.Close(); Assert.Null(window.MotionRoot.Diagnostics.LastError);

        void Advance(double milliseconds, Action? check = null)
        {
            for (double time = 0; time < milliseconds; time += 16)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(Math.Min(16, milliseconds - time)));
                window.UpdateLayout(); check?.Invoke();
            }
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelledDrag_ReturnsFromItsCurrentPoseAndCanDragAgain(bool state)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { IsChecked = state }; var window = Show(box);
        Drag(16);
        var releaseCenter = Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2;
        window.MouseMove(new Point(1, 1)); window.UpdateLayout();
        Assert.Equal(releaseCenter, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        clock.SendFrame(clock.Now); window.UpdateLayout(); Advance(800);
        window.MouseUp(new Point(1, 1), MouseButton.Left);
        Assert.Equal(state, box.IsChecked);
        Assert.Equal(state ? 36 : 16, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        Assert.Equal(state ? 20 : 0, Canvas.GetLeft(Moving(box)));
        var end = Drag(20); window.MouseUp(end, MouseButton.Left); window.UpdateLayout();
        clock.SendFrame(clock.Now); window.UpdateLayout(); Advance(800);
        Assert.Equal(!state, box.IsChecked);
        Assert.Equal(!state ? 36 : 16, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        window.Close(); Assert.Null(window.MotionRoot.Diagnostics.LastError);

        Point Drag(double distance)
        {
            var moving = Moving(box);
            var start = moving.TranslatePoint(new Point(moving.Bounds.Width / 2, moving.Bounds.Height / 2), window)!.Value;
            var end = start + new Vector((state ? -1 : 1) * distance, 0);
            window.MouseDown(start, MouseButton.Left); window.UpdateLayout();
            clock.SendFrame(clock.Now); window.UpdateLayout();
            window.MouseMove(end); window.UpdateLayout(); return end;
        }
        void Advance(int milliseconds)
        {
            for (var time = 0; time < milliseconds; time += 16)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
            }
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisablingDuringDrag_HandsOffAndUnloadingCancelsPendingWork(bool unload)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch(); var window = Show(box);
        var moving = Moving(box);
        var start = moving.TranslatePoint(new Point(moving.Bounds.Width / 2, moving.Bounds.Height / 2), window)!.Value;
        var end = start + new Vector(16, 0);
        window.MouseDown(start, MouseButton.Left); window.UpdateLayout();
        window.MouseMove(end); window.UpdateLayout();
        box.IsEnabled = false; window.UpdateLayout();
        Assert.Equal(32, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        if (unload) { window.Content = null; window.UpdateLayout(); }
        clock.SendFrame(clock.Now); window.UpdateLayout();
        for (var time = 0; time < 800; time += 16)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
        }
        if (!unload) Assert.Equal(16, Thumb(box).PaintedOffset + Thumb(box).PaintedSize / 2);
        Assert.False(box.IsChecked); window.Close();
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveTaskCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public void DisabledAndCancelledPress_DoNotActivate()
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch(); var window = Show(box);
        var center = Center(window, box);
        window.MouseDown(center, MouseButton.Left); window.MouseMove(new Point(1, 1)); window.MouseUp(new Point(1, 1), MouseButton.Left);
        Assert.False(box.IsChecked);
        box.IsEnabled = false; Click(window, box); Assert.False(box.IsChecked); window.Close();
    }

    [AvaloniaFact]
    public void ResizingACapturedThumb_KeepsHoverUntilThePointerLeavesTheOwner()
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch(); var window = Show(box);
        var controller = Material3Expressive.Foundation.Interaction.MaterialInteraction.GetOrCreate(box);
        var point = Center(window, box);
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.UpdateLayout();
        window.MouseMove(point);
        Assert.True(controller.Interaction.HasFlag(Material3Expressive.Foundation.Interaction.InteractionState.Hovered));
        window.MouseMove(new Point(1, 1));
        // Leaving cancels hover/press, while Core Desktop retains the native pointer focus.
        Assert.True(box.IsFocused);
        Assert.Equal(Material3Expressive.Foundation.Interaction.InteractionState.Focused, controller.Interaction);
        window.MouseUp(new Point(1, 1), MouseButton.Left);
        Assert.False(box.IsChecked);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(0, controller.StateLayerOpacity);
        window.FocusManager.Focus(null);
        Assert.Equal(Material3Expressive.Foundation.Interaction.InteractionState.None, controller.Interaction);
        window.Close();
    }

    public static IEnumerable<object[]> RippleCenters()
    {
        foreach (var state in new[] { false, true })
        foreach (var warm in new[] { false, true })
        foreach (var icon in new[] { false, true })
        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            yield return [state, warm, icon, direction, 1d];
        yield return [false, true, false, FlowDirection.LeftToRight, 10d];
    }

    [AvaloniaTheory]
    [MemberData(nameof(RippleCenters))]
    public void RippleAndHover_RemainCenteredDuringPressDragAndRelease(bool state, bool warm, bool icon,
        FlowDirection direction, double durationScale)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { IsChecked = state, FlowDirection = direction };
        if (icon) SwitchAppearance.SetThumbContent(box, new Border { Width = 16, Height = 16 });
        var window = Show(box);
        if (warm)
        {
            box.IsChecked = !state; clock.SendFrame(clock.Now); window.UpdateLayout(); Advance(800);
            box.IsChecked = state; clock.SendFrame(clock.Now); window.UpdateLayout(); Advance(800);
        }
        AnimationScope.SetDurationScale(window, durationScale);
        var controller = MaterialInteraction.GetOrCreate(box);
        var moving = Moving(box);
        var start = moving.TranslatePoint(new Point(moving.Bounds.Width / 2, moving.Bounds.Height / 2), window)!.Value;
        window.MouseMove(start); window.MouseDown(start, MouseButton.Left); window.UpdateLayout();
        clock.SendFrame(clock.Now); window.UpdateLayout(); Advance(240 * durationScale);
        Assert.Equal(28, Thumb(box).PaintedSize); AssertCentered();
        var sign = (state ? -1 : 1) * (direction == FlowDirection.RightToLeft ? -1 : 1);
        var end = start + new Vector(sign * 16, 0);
        window.MouseMove(end); window.UpdateLayout(); AssertCentered();
        window.MouseUp(end, MouseButton.Left); window.UpdateLayout(); AssertCentered();
        clock.SendFrame(clock.Now); window.UpdateLayout(); AssertCentered();
        Advance(32 * durationScale); AssertCentered();
        Advance(64 * durationScale); AssertCentered();
        Advance(800 * durationScale); Assert.Equal(0, controller.RippleCount);
        window.Close(); Assert.Null(window.MotionRoot.Diagnostics.LastError);

        void AssertCentered()
        {
            Assert.Equal(1, controller.RippleCount);
            var surface = box.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var drawing = new DrawingGroup();
            using (var context = drawing.Open()) controller.DrawLayers(context, Brushes.Black, new CornerRadius(0), surface.Bounds.Size);
            // Observe actual feedback drawing, including the live press wave and hover layer.
            Assert.Equal(new Rect(surface.Bounds.Size).Center, drawing.GetBounds().Center);
        }
        void Advance(double milliseconds)
        {
            for (double time = 0; time < milliseconds; time += 16)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(Math.Min(16, milliseconds - time)));
                window.UpdateLayout();
            }
        }
    }

    [AvaloniaFact]
    public void TokensAndExplicitColors_UpdateRealPaintWithoutColorAnimation()
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { IsChecked = true }; var window = Show(box);
        window.Resources[MaterialTokens.Comp.Switch.CheckedTrackColor] = Colors.Red;
        Assert.Equal(Colors.Red, Track(box).PaintedTrack);
        var colors = new SwitchColors(Colors.White, Colors.Blue, Colors.Black, Colors.Red,
            Colors.Gray, Colors.Green, Colors.Black, Colors.Yellow, Colors.Blue, Colors.Red, Colors.Green,
            Colors.Yellow, Colors.Black, Colors.Gray, Colors.White, Colors.Blue);
        SwitchAppearance.SetColors(box, colors); box.IsEnabled = false;
        Assert.Equal(Colors.Red, Track(box).PaintedTrack); Assert.Equal(Colors.Blue, Track(box).PaintedThumb);
        box.IsChecked = false;
        Assert.Equal(Colors.Gray, Track(box).PaintedTrack); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void NativeContentAndStateLabels_AreOutsideThumb(FlowDirection direction)
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { Content = new Border { Width = 32, Height = 16 },
            OffContent = new Border { Width = 12, Height = 12 }, OnContent = new Border { Width = 20, Height = 12 }, FlowDirection = direction };
        var window = Show(box);
        Assert.Equal(new Size(104, 48), box.Bounds.Size);
        Assert.Same(box.OffContent, box.OffContentPresenter!.Child);
        Assert.True(box.OffContentPresenter.IsVisible); Assert.False(box.OnContentPresenter!.IsVisible);
        box.IsChecked = true; window.UpdateLayout();
        Assert.Equal(new Size(112, 48), box.Bounds.Size);
        Assert.True(box.OnContentPresenter.IsVisible); Assert.False(box.OffContentPresenter.IsVisible); window.Close();
    }

    [AvaloniaFact]
    public void ThumbContentTemplate_InheritsDataContextAndIconColorAndCanBeReplaced()
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch { DataContext = new object() };
        SwitchAppearance.SetThumbContent(box, new object());
        SwitchAppearance.SetThumbContentTemplate(box, new FuncDataTemplate<object>((data, _) => new Border { Width = 16, Height = 16 }));
        var window = Show(box);
        var presenter = box.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_ThumbContentPresenter");
        Assert.NotNull(presenter.Child); Assert.Equal(new Size(16, 16), presenter.Child.Bounds.Size);
        Assert.Equal(Track(box).PaintedIcon, Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Foreground).Color);
        Assert.Equal(Track(box).PaintedIcon,
            Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Child.GetValue(TextElement.ForegroundProperty)).Color);
        var replacement = new Border { Width = 12, Height = 12 };
        SwitchAppearance.SetThumbContentTemplate(box, null); SwitchAppearance.SetThumbContent(box, replacement); window.UpdateLayout();
        Assert.Same(replacement, presenter.Child); Assert.Same(box.DataContext, replacement.DataContext); window.Close();
    }

    [AvaloniaFact]
    public void Animation_RestartsAfterIdleAndTemplateReattachmentReleasesWork()
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch(); var window = Show(box);
        box.IsChecked = true; clock.SendFrame(clock.Now); window.UpdateLayout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(48)); window.UpdateLayout();
        Assert.InRange(Thumb(box).PaintedOffset, 8.25, 26);
        clock.AdvanceBy(TimeSpan.FromSeconds(2)); window.UpdateLayout();
        Assert.Equal(24, Thumb(box).PaintedOffset);
        Assert.Equal(0, clock.PendingFrameCount);
        clock.AdvanceBy(TimeSpan.FromSeconds(3));
        box.IsChecked = false; clock.SendFrame(clock.Now); window.UpdateLayout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
        var template = box.Template;
        box.Template = new FuncControlTemplate<ToggleSwitch>((_, names) =>
        {
            var panel = new Panel { Name = "PART_MovingKnobs" }; names.Register("PART_MovingKnobs", panel); return panel;
        });
        window.UpdateLayout();
        box.Template = template; window.UpdateLayout(); Assert.Equal(16, Thumb(box).PaintedSize);
        var root = window.Content; window.Content = null; window.UpdateLayout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount); Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveTaskCount);
        window.Content = root; window.UpdateLayout(); Assert.NotNull(Thumb(box));
        window.Close(); Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    static SwitchThumbLayout Thumb(ToggleSwitch box) => box.GetVisualDescendants().OfType<SwitchThumbLayout>().Single();
    static SwitchTrack Track(ToggleSwitch box) => box.GetVisualDescendants().OfType<SwitchTrack>().Single();
    static Control Moving(ToggleSwitch box) => (Control)box.GetVisualDescendants().Single(v => v.Name == "PART_MovingKnobs");
    static Point Center(Window window, ToggleSwitch box) => box.TranslatePoint(new Point(box.Bounds.Width / 2, box.Bounds.Height / 2), window)!.Value;
    static void Click(Window window, ToggleSwitch box) { var point = Center(window, box); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); }
    static MotionTestWindow Show(ToggleSwitch box)
    {
        box.HorizontalAlignment = HorizontalAlignment.Center; box.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 240, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { box } }; window.Show(); window.UpdateLayout(); return window;
    }
    sealed class SwitchState : INotifyPropertyChanged
    {
        bool? value = false;
        public bool? Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    sealed class ObserveCommand(Action execute) : System.Windows.Input.ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}

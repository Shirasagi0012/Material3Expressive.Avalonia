using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class RadioButtonTests
{
    [AvaloniaFact]
    public void Theme_UsesNativeRadioSelectionAndAutomation()
    {
        using var clock = new MotionTestClockScope();
        var first = new RadioButton { GroupName = "choices", IsChecked = true };
        var second = new RadioButton { GroupName = "choices" };
        var window = Show(new StackPanel { Children = { first, second } });
        Assert.Equal(new Size(48, 48), first.Bounds.Size);
        Assert.Equal(new Size(20, 20), Part(first).Bounds.Size);
        var peer = ControlAutomationPeer.CreatePeerForElement(second)!;
        Assert.Equal(AutomationControlType.RadioButton, peer.GetAutomationControlType());
        var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
        selection.Select(); Assert.True(second.IsChecked); Assert.False(first.IsChecked);
        Click(window, second); Assert.True(second.IsChecked);
        second.IsEnabled = false;
        Assert.ThrowsAny<Exception>(() => selection.Select());
        window.Close();
    }

    [AvaloniaFact]
    public void Click_PreservesTwoWayBindingAndPublishesSelectionBeforeCommand()
    {
        using var clock = new MotionTestClockScope();
        var source = new SelectionState();
        var radio = new RadioButton();
        radio.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(SelectionState.Value)) { Source = source });
        var observed = new List<bool?>();
        radio.Click += (_, _) => observed.Add(radio.IsChecked);
        var window = Show(radio);
        Click(window, radio); Assert.True(source.Value); Assert.Equal(new bool?[] { true }, observed);
        Click(window, radio); Assert.True(radio.IsChecked); Assert.Equal(2, observed.Count);
        source.Value = false; Assert.False(radio.IsChecked);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter, true)]
    [InlineData(Key.Space, false)]
    public void Keyboard_PreservesApprovedNativeActivation(Key key, bool onDown)
    {
        using var clock = new MotionTestClockScope();
        var radio = new RadioButton(); var window = Show(radio); var clicks = 0;
        radio.Click += (_, _) => clicks++;
        radio.Focus(NavigationMethod.Tab);
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(onDown ? 1 : 0, clicks); Assert.Equal(onDown, radio.IsChecked);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(1, clicks); Assert.True(radio.IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void NamedGroupsSpanParents_ImplicitGroupsStayAmongSiblings()
    {
        using var clock = new MotionTestClockScope();
        var a = new RadioButton { GroupName = "named", IsChecked = true };
        var b = new RadioButton { GroupName = "named" };
        var c = new RadioButton { IsChecked = true };
        var d = new RadioButton();
        var independent = new RadioButton { IsChecked = true };
        var window = Show(new StackPanel { Children = {
            new StackPanel { Children = { a, c, d } }, new StackPanel { Children = { b, independent } } } });
        Click(window, b); Assert.False(a.IsChecked); Assert.True(b.IsChecked);
        Click(window, d); Assert.False(c.IsChecked); Assert.True(d.IsChecked); Assert.True(independent.IsChecked);
        b.GroupName = "different"; a.IsChecked = true; Assert.True(b.IsChecked);
        window.Close();
        var otherWindow = Show(new RadioButton { GroupName = "named", IsChecked = true });
        Assert.True(a.IsChecked); otherWindow.Close();
    }

    [AvaloniaFact]
    public void DisabledAndCancelledPress_DoNotSelect()
    {
        using var clock = new MotionTestClockScope();
        var radio = new RadioButton(); var window = Show(radio); var clicks = 0;
        radio.Click += (_, _) => clicks++;
        var center = Center(window, radio);
        window.MouseDown(center, MouseButton.Left); window.MouseMove(new Point(1, 1)); window.MouseUp(new Point(1, 1), MouseButton.Left);
        Assert.False(radio.IsChecked); Assert.Equal(0, clicks);
        radio.IsEnabled = false; Click(window, radio);
        Assert.False(radio.IsChecked); Assert.Equal(0, clicks);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void ContentAndTemplate_StayOutsideTheIndicatorAndMirrorInRtl(FlowDirection direction)
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Width = 32, Height = 16 };
        var radio = new RadioButton { Content = content, FlowDirection = direction };
        var window = Show(radio);
        Assert.Equal(new Size(88, 48), radio.Bounds.Size);
        Assert.Equal(new Size(20, 20), Part(radio).Bounds.Size);
        var iconX = Part(radio).TranslatePoint(default, window)!.Value.X;
        var contentX = content.TranslatePoint(default, window)!.Value.X;
        Assert.Equal(direction == FlowDirection.LeftToRight, iconX < contentX);
        radio.Content = "template-data";
        radio.ContentTemplate = new FuncDataTemplate<string>((_, _) => new Border { Width = 40, Height = 16 });
        window.UpdateLayout(); Assert.Equal(new Size(96, 48), radio.Bounds.Size);
        window.Close();
    }

    [AvaloniaFact]
    public void ColorsAndDisabledSelection_KeepIndependentAnimationRules()
    {
        using var clock = new MotionTestClockScope();
        var radio = new RadioButton(); var window = Show(radio);
        var colors = new RadioButtonColors(Colors.Blue, Colors.Red, Colors.Green, Colors.Gray);
        RadioButtonAppearance.SetColors(radio, colors);
        clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(Colors.Red, Part(radio).PaintedColor);
        radio.IsEnabled = false; radio.IsChecked = true;
        clock.SendFrame(clock.Now);
        Assert.Equal(Colors.Green, Part(radio).PaintedColor);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
        Assert.InRange(Part(radio).DotRadius, 0.1f, 5.99f);
        clock.AdvanceBy(TimeSpan.FromSeconds(2)); Assert.Equal(6, Part(radio).DotRadius);
        radio.IsEnabled = true; clock.SendFrame(clock.Now);
        Assert.Equal(Colors.Blue, Part(radio).PaintedColor);
        RadioButtonAppearance.SetColors(radio, null);
        window.Resources[MaterialTokens.Comp.RadioButton.SelectedColor] = Colors.Teal;
        clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(Colors.Teal, Part(radio).PaintedColor);
        window.Close(); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void Disable_StopsInFlightColorAnimationWhenTheDisabledTargetIsUnchanged()
    {
        using var clock = new MotionTestClockScope();
        var radio = new RadioButton { IsChecked = true }; var window = Show(radio);
        RadioButtonAppearance.SetColors(radio, new(Colors.Blue, Colors.Gray, Colors.Blue, Colors.Gray));
        var origin = clock.Now; clock.SendFrame(origin);
        clock.SendFrame(origin + TimeSpan.FromMilliseconds(16));
        clock.SendFrame(origin + TimeSpan.FromMilliseconds(32));
        Assert.NotEqual(Colors.Blue, Part(radio).PaintedColor);
        Assert.True(clock.PendingFrameCount > 0);
        // The target color is already Blue; removing animateColorAsState must still snap and stop.
        radio.IsEnabled = false; clock.SendFrame(origin + TimeSpan.FromMilliseconds(33));
        Assert.Equal(Colors.Blue, Part(radio).PaintedColor);
        // The shared interaction controller commits the disabled state-layer cleanup on its next frame.
        clock.SendFrame(origin + TimeSpan.FromMilliseconds(49));
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close(); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void ReversedDot_UsesMotionSpringThresholdUntilTheTrailingBounceCompletes()
    {
        using var clock = new MotionTestClockScope();
        var radio = new RadioButton(); var window = Show(radio);
        var origin = clock.Now;
        radio.IsChecked = true; clock.SendFrame(origin + TimeSpan.FromMilliseconds(1));
        for (var time = 16; time <= 64; time += 16) clock.SendFrame(origin + TimeSpan.FromMilliseconds(time));
        radio.IsChecked = false; clock.SendFrame(origin + TimeSpan.FromMilliseconds(65));
        for (var time = 80; time <= 400; time += 16) clock.SendFrame(origin + TimeSpan.FromMilliseconds(time));
        // Fixed motionScheme's explicit spring uses the 0.01 threshold, rather than dpDefaultSpring's 0.1.
        // Keep the trailing bounce in animation state even when it has no positive painted radius.
        Assert.InRange(Part(radio).DotRadius, 0.01f, 0.02f);
        clock.SendFrame(origin + TimeSpan.FromMilliseconds(800)); Assert.Equal(0, Part(radio).DotRadius);
        window.Close(); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(10)]
    public void Deselection_DotDoesNotReappearAfterItDisappears(int durationScale)
    {
        using var clock = new MotionTestClockScope();
        var radio = new RadioButton { IsChecked = true }; var window = Show(radio);
        window.Background = Brushes.White;
        Animated.SetDurationScale(window, durationScale);
        var center = Center(window, radio);
        // The central 10 DIP square contains the dot, but cannot touch the 9 DIP outer ring.
        bool DotIsPainted()
        {
            using var frame = window.CaptureRenderedFrame()!;
            using var buffer = frame.Lock();
            var row = new byte[buffer.RowBytes];
            for (var y = (int)center.Y - 5; y < (int)center.Y + 5; y++)
            {
                Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, row.Length);
                for (var x = (int)center.X - 5; x < (int)center.X + 5; x++)
                    if (row[x * 4] != 255 || row[x * 4 + 1] != 255 || row[x * 4 + 2] != 255) return true;
            }
            return false;
        }
        Assert.True(DotIsPainted());
        var origin = clock.Now;
        radio.IsChecked = false;
        clock.SendFrame(origin + TimeSpan.FromMilliseconds(durationScale));
        var disappeared = false;
        for (var time = 16; time <= 800; time += 16)
        {
            clock.SendFrame(origin + TimeSpan.FromMilliseconds(time * durationScale));
            if (!DotIsPainted()) disappeared = true;
            else Assert.False(disappeared, $"Dot reappeared at animation time {time} ms (duration scale {durationScale}).");
        }
        Assert.True(disappeared); Assert.Equal(0, Part(radio).DotRadius);
        window.Close(); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void ReversalIdleRestartAndTemplateReplacement_ReleaseAnimationWork()
    {
        using var clock = new MotionTestClockScope();
        var radio = new RadioButton(); var window = Show(radio);
        radio.IsChecked = true; clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
        Assert.InRange(Part(radio).DotRadius, 0.1f, 5.99f);
        radio.IsChecked = false; clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(2));
        Assert.Equal(0, Part(radio).DotRadius); Assert.Equal(0, clock.PendingFrameCount);
        clock.AdvanceBy(TimeSpan.FromSeconds(3));
        radio.IsChecked = true; clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
        Assert.InRange(Part(radio).DotRadius, 0.1f, 5.99f);
        var template = radio.Template;
        radio.Template = new FuncControlTemplate<RadioButton>((_, _) => new Border()); window.UpdateLayout();
        Assert.Empty(radio.GetVisualDescendants().OfType<RadioButtonIndicator>());
        radio.Template = template; window.UpdateLayout(); Assert.Equal(6, Part(radio).DotRadius);
        var root = window.Content; window.Content = null; window.UpdateLayout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.ActiveChildCount); Assert.Equal(0, window.MotionRoot.ActiveTaskCount);
        window.Content = root; window.UpdateLayout(); Assert.Equal(6, Part(radio).DotRadius);
        window.Close(); Assert.Null(window.MotionRoot.LastError);
    }

    static RadioButtonIndicator Part(RadioButton radio) => radio.GetVisualDescendants().OfType<RadioButtonIndicator>().Single();
    static Point Center(Window window, RadioButton radio) => radio.TranslatePoint(new Point(radio.Bounds.Width / 2, radio.Bounds.Height / 2), window)!.Value;
    static void Click(Window window, RadioButton radio) { var point = Center(window, radio); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); }
    static MotionTestWindow Show(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 240, Height = 320 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
    sealed class SelectionState : INotifyPropertyChanged
    {
        bool? value = false;
        public bool? Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

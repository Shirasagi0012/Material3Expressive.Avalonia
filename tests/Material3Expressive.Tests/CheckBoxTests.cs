using System.ComponentModel;
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
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class CheckBoxTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Theme_UsesNativeStateAndAutomation(bool threeState)
    {
        using var clock = new MotionTestClockScope();
        var box = new CheckBox { IsThreeState = threeState };
        var window = Show(box);
        var peer = ControlAutomationPeer.CreatePeerForElement(box)!;
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer);
        Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
        Assert.Equal(new Size(48, 48), box.Bounds.Size);
        Assert.Equal(new Size(20, 20), Part(box).Bounds.Size);
        toggle.Toggle(); Assert.True(box.IsChecked);
        toggle.Toggle(); Assert.Equal(threeState ? (bool?)null : false, box.IsChecked);
        toggle.Toggle(); Assert.Equal(threeState ? false : true, box.IsChecked);
        box.IsEnabled = false;
        Assert.ThrowsAny<Exception>(() => toggle.Toggle());
        window.Close();
    }

    [AvaloniaFact]
    public void Click_PreservesTwoWayBindingAndPublishesStateBeforeCommand()
    {
        using var clock = new MotionTestClockScope();
        var source = new CheckState();
        var box = new CheckBox();
        box.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(CheckState.Value)) { Source = source });
        var observed = new List<bool?>();
        box.Click += (_, _) => observed.Add(box.IsChecked);
        var window = Show(box);
        Click(window, box);
        Assert.True(source.Value); Assert.Equal(new bool?[] { true }, observed);
        source.Value = false;
        Assert.False(box.IsChecked);
        Click(window, box); Assert.True(source.Value);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter, true)]
    [InlineData(Key.Space, false)]
    public void Keyboard_PreservesApprovedNativeActivation(Key key, bool onDown)
    {
        using var clock = new MotionTestClockScope();
        var box = new CheckBox(); var window = Show(box); var clicks = 0;
        box.Click += (_, _) => clicks++;
        box.Focus(NavigationMethod.Tab);
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(onDown ? 1 : 0, clicks); Assert.Equal(onDown, box.IsChecked);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(1, clicks); Assert.True(box.IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void DisabledAndCancelledPress_DoNotChangeState()
    {
        using var clock = new MotionTestClockScope();
        var box = new CheckBox(); var window = Show(box); var clicks = 0;
        box.Click += (_, _) => clicks++;
        var point = Center(window, box);
        window.MouseDown(point, MouseButton.Left); window.MouseMove(new Point(1, 1)); window.MouseUp(new Point(1, 1), MouseButton.Left);
        Assert.False(box.IsChecked); Assert.Equal(0, clicks);
        box.IsEnabled = false; Click(window, box);
        Assert.False(box.IsChecked); Assert.Equal(0, clicks);
        window.Close();
    }

    [AvaloniaFact]
    public void CustomColorsAndTokens_UpdateThroughTheActualTemplate()
    {
        using var clock = new MotionTestClockScope();
        var box = new CheckBox { IsChecked = true }; var window = Show(box);
        // Avalonia Colors.Transparent is transparent white; Compose's default is transparent black.
        Assert.Equal(Color.FromArgb(0, 0, 0, 0), Part(box).UncheckedBoxColor);
        Assert.Equal(Color.FromArgb(0, 0, 0, 0), Part(box).UncheckedCheckmarkColor);
        window.Resources[MaterialTokens.Comp.CheckBox.CheckedBoxColor] = Colors.Red;
        clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(Colors.Red, Part(box).PaintedBox);
        var colors = new CheckBoxColors(Colors.White, Colors.Transparent, Colors.Blue, Colors.Transparent,
            Colors.Gray, Colors.Transparent, Colors.Green, Colors.Blue, Colors.Black, Colors.Gray, Colors.Gray, Colors.Green, Colors.Yellow);
        CheckBoxAppearance.SetColors(box, colors);
        box.IsEnabled = false; box.IsChecked = null;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(Colors.Green, Part(box).PaintedBox);
        // Styling fix is off at the fixed baseline: the enabled checkmark color is retained.
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(Colors.White, Part(box).PaintedCheckmark);
        box.IsEnabled = true; clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(Colors.Blue, Part(box).PaintedBox);
        window.Close();
    }

    [AvaloniaFact]
    public void ShapeTransition_RestartsAfterIdleAndReleasesItsFrameWaiter()
    {
        using var clock = new MotionTestClockScope();
        var box = new CheckBox(); var window = Show(box);
        Assert.Equal(0, Part(box).DrawFraction);
        box.IsChecked = true;
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
        Assert.InRange(Part(box).DrawFraction, 0.01f, 0.99f);
        clock.AdvanceBy(TimeSpan.FromSeconds(2));
        Assert.Equal(1, Part(box).DrawFraction);
        Assert.Equal(0, clock.PendingFrameCount);
        clock.AdvanceBy(TimeSpan.FromSeconds(3));
        box.IsChecked = null;
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
        Assert.InRange(Part(box).CenterGravitation, 0.01f, 0.99f);
        clock.AdvanceBy(TimeSpan.FromSeconds(2));
        Assert.Equal(1, Part(box).CenterGravitation);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void NativeContent_UsesPresenterAndStaysOutsideTheCanvas(FlowDirection direction)
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Width = 32, Height = 16 };
        var box = new CheckBox { Content = content, FlowDirection = direction };
        var window = Show(box);
        Assert.Equal(new Size(88, 48), box.Bounds.Size);
        Assert.Equal(new Size(20, 20), Part(box).Bounds.Size);
        Assert.Equal(new Size(32, 16), content.Bounds.Size);
        window.Close();
    }

    [AvaloniaFact]
    public void TemplateReplacementAndReattachment_ReleaseAnimationWork()
    {
        using var clock = new MotionTestClockScope();
        var box = new CheckBox(); var window = Show(box);
        box.IsChecked = true; clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var template = box.Template;
        box.Template = new FuncControlTemplate<CheckBox>((_, _) => new Border()); window.UpdateLayout();
        Assert.Empty(box.GetVisualDescendants().OfType<CheckBoxIndicator>());
        box.Template = template; window.UpdateLayout();
        Assert.Equal(new Size(20, 20), Part(box).Bounds.Size);
        var root = window.Content;
        window.Content = null; window.UpdateLayout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveTaskCount);
        window.Content = root; window.UpdateLayout();
        Assert.NotNull(Part(box));
        window.Close();
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    static CheckBoxIndicator Part(CheckBox box) => box.GetVisualDescendants().OfType<CheckBoxIndicator>().Single();
    static Point Center(Window window, CheckBox box) => box.TranslatePoint(new Point(box.Bounds.Width / 2, box.Bounds.Height / 2), window)!.Value;
    static void Click(Window window, CheckBox box) { var point = Center(window, box); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); }
    static MotionTestWindow Show(CheckBox box)
    {
        box.HorizontalAlignment = HorizontalAlignment.Center; box.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 240, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { box } };
        window.Show(); window.UpdateLayout(); return window;
    }
    sealed class CheckState : INotifyPropertyChanged
    {
        bool? value = false;
        public bool? Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

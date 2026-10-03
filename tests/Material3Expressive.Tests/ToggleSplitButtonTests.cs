using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using System.ComponentModel;
using Split = Material3Expressive.Components.SplitButton;

namespace Material3Expressive.Tests;

public class ToggleSplitButtonTests
{
    [AvaloniaTheory]
    [InlineData("xsmall", 32, 12, 8)]
    [InlineData("small", 40, 12, 6)]
    [InlineData("medium", 56, 16, 12)]
    [InlineData("large", 96, 28, 16)]
    [InlineData("xlarge", 136, 28, 16)]
    public void ToggleSizes_UseActualTemplateAndThreeShapes(string size, double height, double selected, double pressed)
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleButton { Content = new Border { Width = 32, Height = 16 }, IsChecked = true };
        if (size != "small") toggle.Classes.Add(size);
        var window = Show(toggle);
        Assert.Equal(height, Surface(toggle).Bounds.Height);
        Assert.Equal(selected, ToggleButtonAppearance.GetCheckedCornerRadius(toggle).TopLeft);
        Assert.Equal(pressed, MaterialButton.GetPressedCornerRadius(toggle)!.Value.TopLeft);
        var peer = ControlAutomationPeer.CreatePeerForElement(toggle)!;
        Assert.IsAssignableFrom<IToggleProvider>(peer).Toggle();
        Assert.False(toggle.IsChecked);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter, true)]
    [InlineData(Key.Space, false)]
    public void NativeToggleInput_PublishesStateBeforeClick(Key key, bool onDown)
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleButton { Content = "Toggle" };
        var window = Show(toggle); var states = new List<bool?>();
        toggle.Click += (_, _) => states.Add(toggle.IsChecked);
        toggle.Focus(NavigationMethod.Tab);
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(onDown ? 1 : 0, states.Count);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(new bool?[] { true }, states);
        toggle.IsEnabled = false;
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Single(states);
        window.Close();
    }

    [AvaloniaFact]
    public void SplitButtons_AreIndependentAndUseTrailingToggle()
    {
        using var clock = new MotionTestClockScope();
        var first = new Button { Content = new Border { Width = 32, Height = 16 } };
        var second = new ToggleButton { Content = new Border { Width = 22, Height = 22 } };
        var split = new Split { LeadingButton = first, TrailingButton = second };
        var window = Show(split);
        first.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonLeadingTheme")!;
        second.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!;
        window.UpdateLayout();
        Assert.Equal(Surface(first).Bounds.Height, Surface(second).Bounds.Height);
        Assert.Equal(40, Surface(first).Bounds.Height);
        Assert.Equal(48, split.Bounds.Height);
        var clicks = 0; first.Click += (_, _) => clicks++;
        Click(window, second); Assert.True(second.IsChecked); Assert.Equal(0, clicks);
        Click(window, first); Assert.Equal(1, clicks); Assert.True(second.IsChecked);
        first.IsEnabled = false; Click(window, second); Assert.False(second.IsChecked);
        // Replacing a slot must detach its prior logical child and retain independent parenting.
        split.TrailingButton = new Button(); window.UpdateLayout();
        Assert.Null(second.Parent);
        window.Close();
    }

    [AvaloniaFact]
    public void ToggleContent_IconColorsAndLifecycleStayInProductTemplate()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleButton { Content = new Border { Width = 32, Height = 16 } };
        ToggleButtonAppearance.SetIcon(toggle, new Border { Background = Brushes.White });
        ToggleButtonAppearance.SetColors(toggle, new(Colors.Red, Colors.Blue, Colors.Gray, Colors.Black, Colors.Green, Colors.White));
        var window = Show(toggle);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)toggle.Background!).Color);
        Assert.Equal(20, toggle.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().Single(p => p.Name == "PART_IconPresenter").Bounds.Width);
        toggle.IsChecked = true; Assert.Equal(Colors.Green, ((ISolidColorBrush)toggle.Background!).Color);
        toggle.Theme = (ControlTheme)window.FindResource("MaterialOutlinedToggleButtonTheme")!; window.UpdateLayout();
        var root = window.Content;
        window.Content = null;
        window.Content = root; window.UpdateLayout();
        window.Close();
    }

    [AvaloniaFact]
    public void NativeToggleBinding_UsesTwoWayStateAndNullableExtension()
    {
        using var clock = new MotionTestClockScope();
        var state = new ToggleState();
        var toggle = new ToggleButton { Content = "Toggle", IsThreeState = true };
        toggle.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ToggleState.Checked)) { Source = state });
        var window = Show(toggle);
        Click(window, toggle); Assert.True(state.Checked);
        state.Checked = null; Assert.Null(toggle.IsChecked);
        var uncheckedColor = ((ISolidColorBrush)toggle.Background!).Color;
        state.Checked = false; Assert.Equal(uncheckedColor, ((ISolidColorBrush)toggle.Background!).Color);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void OutlinedToggle_ReattachmentCreatesANewBorderAnimationLifetime()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleButton { Content = new Border { Width = 32, Height = 16 } };
        var window = Show(toggle);
        toggle.Theme = (ControlTheme)window.FindResource("MaterialOutlinedToggleButtonTheme")!;
        window.UpdateLayout();
        var root = window.Content;
        window.Content = null;
        Assert.Equal(0, clock.PendingFrameCount);
        window.Content = root; window.UpdateLayout();
        var outline = Surface(toggle).GetVisualChildren().OfType<SurfaceOutline>().Single();
        Assert.NotEqual(0, ((ISolidColorBrush)outline.BorderBrush!).Color.A);
        toggle.IsChecked = true;
        clock.SendFrame(clock.Now);
        for (var frame = 0; frame < 25; frame++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, ((ISolidColorBrush)outline.BorderBrush!).Color.A);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SplitInactiveShapeChange_DoesNotCancelTheSelectedAnimation()
    {
        using var clock = new MotionTestClockScope();
        var trailing = new ToggleButton { Content = new Border { Width = 22, Height = 22 } };
        var split = new Split { LeadingButton = new Button(), TrailingButton = trailing };
        var window = Show(split);
        split.LeadingButton.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonLeadingTheme")!;
        trailing.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!;
        window.UpdateLayout();
        trailing.IsChecked = true;
        clock.SendFrame(clock.Now);
        for (var frame = 0; frame < 4; frame++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        trailing.CornerRadius = new CornerRadius(8);
        for (var frame = 0; frame < 25; frame++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(new CornerRadius(20), Surface(trailing).EffectiveCornerRadius);
        trailing.IsChecked = false;
        for (var frame = 0; frame < 30; frame++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(new CornerRadius(8), Surface(trailing).EffectiveCornerRadius);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SplitConstrainedWidth_PreservesTrailingOverflowAndOpticalOffset()
    {
        using var clock = new MotionTestClockScope();
        var trailing = new ToggleButton { Content = new Border { Height = 22 } };
        var split = new Split { Width = 24, LeadingButton = new Button(), TrailingButton = trailing };
        var window = Show(split);
        split.LeadingButton.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonLeadingTheme")!;
        trailing.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!;
        window.UpdateLayout(); clock.SendFrame(TimeSpan.Zero); window.UpdateLayout();
        Assert.False(split.ClipToBounds); Assert.False(trailing.ClipToBounds);
        Assert.Equal(24, Surface(trailing).Bounds.Width);
        var content = (Border)trailing.Content!;
        Assert.Equal(11, content.TranslatePoint(default, Surface(trailing))!.Value.X);
        window.Close();
    }

    sealed class ToggleState : INotifyPropertyChanged
    {
        bool? value = false;
        public bool? Checked { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new(nameof(Checked))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    static MaterialSurface Surface(Button button) => button.GetVisualDescendants().OfType<MaterialSurface>().Single();
    static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
    }
    static MotionTestWindow Show(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 420, Height = 220 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
}

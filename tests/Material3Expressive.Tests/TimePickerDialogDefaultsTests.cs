using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class TimePickerDialogDefaultsTests
{
    [AvaloniaFact]
    public void TitleTracksModeCultureAndThemeWithoutOverwritingExplicitText()
    {
        var title = new TimePickerDialogTitle { Culture = CultureInfo.GetCultureInfo("en") };
        var window = DialogTests.Show(title);
        try
        {
            Assert.Equal("Select Time", title.Text); Assert.Equal(12, title.FontSize);
            Assert.Equal(new Thickness(0, 0, 0, 20), title.Padding);
            title.DisplayMode = TimePickerDisplayMode.Input; Assert.Equal("Enter Time", title.Text);
            title.Culture = CultureInfo.GetCultureInfo("fr"); Assert.Equal(DateTimeStrings.Get("TimeInputDialogTitle", title.Culture), title.Text);
            title.DisplayMode = TimePickerDisplayMode.Scroll; Assert.Equal(DateTimeStrings.Get("TimeScrollDialogTitle", title.Culture), title.Text);
            window.Resources[MaterialTokens.Comp.TimePickerDialog.TitlePadding] = new Thickness(0, 0, 0, 24);
            Assert.Equal(24, title.Padding.Bottom);
            title.Text = "Application title"; title.DisplayMode = TimePickerDisplayMode.Picker;
            Assert.Equal("Application title", title.Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void ButtonRequestsModeThroughNativeClickAndCommandAndUsesIndependentTooltip(bool scroll)
    {
        using var clock = new MotionTestClockScope();
        TimePickerDisplayModeToggle toggle = scroll ? new TimeScrollDisplayModeToggle() : new TimePickerDisplayModeToggle();
        toggle.Culture = CultureInfo.GetCultureInfo("en");
        var command = new TestCommand(); toggle.Command = command; toggle.CommandParameter = "mode";
        var window = DialogTests.Show(toggle);
        var clicks = 0; toggle.Click += (_, _) => clicks++;
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            var button = toggle.GetVisualDescendants().OfType<IconButton>().Single();
            var tooltip = toggle.GetVisualDescendants().OfType<TooltipBox>().Single();
            Assert.Equal(TooltipAnchorPosition.Above, tooltip.AnchorPosition);
            Assert.Equal("Switch to text input mode", AutomationProperties.GetName(button));
            Assert.Same(TimePickerModeIcons.Keyboard, toggle.IconGeometry);
            var icon = button.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
            Assert.Same(toggle.IconGeometry, icon.Data); Assert.NotNull(icon.Fill); Assert.True(icon.IsEffectivelyVisible);
            button.Focus(NavigationMethod.Tab); Frames(20); Assert.True(tooltip.State.IsVisible);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Frames(2);
            Assert.Equal(1, clicks); Assert.Equal("mode", command.LastParameter);
            Assert.Equal(scroll ? TimePickerDisplayMode.Scroll : TimePickerDisplayMode.Picker, toggle.DisplayMode);
            toggle.DisplayMode = TimePickerDisplayMode.Input;
            Assert.Equal(scroll ? "Switch to scroll mode" : "Switch to clock mode", AutomationProperties.GetName(button));
            Assert.Same(scroll ? TimePickerModeIcons.SwipeVertical : TimePickerModeIcons.Schedule, toggle.IconGeometry);
            Assert.Equal(AutomationProperties.GetName(button), ((PlainTooltip)tooltip.Tooltip!).Content);
            command.Enabled = false; command.Notify(); Assert.False(button.IsEffectivelyEnabled);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Assert.Equal(1, clicks);
            window.Content = null; Assert.False(tooltip.State.IsVisible);
            window.Content = toggle; window.UpdateLayout(); Assert.Equal(TimePickerDisplayMode.Input, toggle.DisplayMode);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private sealed class TestCommand : ICommand
    {
        public bool Enabled = true;
        public object? LastParameter;
        public event EventHandler? CanExecuteChanged;
        public void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) => LastParameter = parameter;
    }
}

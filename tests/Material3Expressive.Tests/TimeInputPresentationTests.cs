using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class TimeInputPresentationTests
{
    [AvaloniaTheory]
    [InlineData("zh-CN", "小时值必须介于 1 到 12 间")]
    [InlineData("pt-PT", "A hora tem de ser 1-12")]
    [InlineData("id-ID", "Jam harus antara 1—12")]
    public void RegionalErrorIsPresentedByTheRealEditorAndLiveRegion(string cultureName, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
        try
        {
            var picker = new TimeInput { Is24Hour = false, SelectedTime = new(2, 30) };
            var window = Show(picker);
            try
            {
                var editor = Part<TextBox>(picker, "HourInput");
                editor.Text = "99"; Flush(window);
                var label = Part<TextBlock>(picker, "HourSupportingText");
                Assert.Equal(expected, label.Text);
                Assert.Equal(expected, MaterialTextField.GetErrorMessage(editor));
                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(label));
            }
            finally { window.Close(); }
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [AvaloniaTheory]
    [InlineData(TimeSelection.Hour)]
    [InlineData(TimeSelection.Minute)]
    public void OnlySelectedFieldIsAnEditor_AndReceivesInitialFocus(TimeSelection selection)
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Selection = selection };
        var window = Show(picker);
        try
        {
            var hour = Part<TextBox>(picker, "HourInput");
            var minute = Part<TextBox>(picker, "MinuteInput");
            Assert.Equal(selection == TimeSelection.Hour, hour.IsVisible);
            Assert.Equal(selection == TimeSelection.Minute, minute.IsVisible);
            var editor = selection == TimeSelection.Hour ? hour : minute;
            Assert.True(editor.IsFocused);
            Assert.Equal(editor.SelectionStart, editor.SelectionEnd);
            var selector = Part<Button>(picker, selection == TimeSelection.Hour ? "MinuteInputSelector" : "HourInputSelector");
            Assert.True(selector.IsVisible);
            var point = selector.TranslatePoint(new Point(selector.Bounds.Width / 2, selector.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.False(editor.IsVisible);
            Assert.True((selection == TimeSelection.Hour ? minute : hour).IsFocused);
            Assert.Equal(new TimeOnly(2, 30), picker.SelectedTime);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SupportingTextFollowsVariantValidityAndHourFormat(bool vibrant, bool clock24)
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Is24Hour = clock24,
            Variant = vibrant ? TimePickerVariant.Vibrant : TimePickerVariant.Standard };
        var window = Show(picker);
        try
        {
            var hour = Part<TextBox>(picker, "HourInput");
            var label = Part<TextBlock>(picker, "HourSupportingText");
            Assert.Equal(!vibrant, label.IsVisible);
            Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(label));
            hour.Text = "99"; Flush(window);
            Assert.False(picker.IsInputValid);
            Assert.True(label.IsVisible);
            Assert.Equal(DateTimeStrings.Get(clock24 ? "TimePicker24HourError" : "TimePickerHourError"), label.Text);
            Assert.Equal(label.Text, MaterialTextField.GetErrorMessage(hour));
            Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(label));
            Assert.True(label.Bounds.Height >= label.LineHeight * 2);
            Assert.Equal(new TimeOnly(2, 30), picker.SelectedTime);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            Assert.Equal(TimeSelection.Hour, picker.Selection);
            picker.Selection = TimeSelection.Minute; Flush(window);
            Assert.Equal("99", Part<Button>(picker, "HourInputSelector").Content);
            picker.Selection = TimeSelection.Hour; Flush(window);
            Assert.Equal("99", hour.Text);
            hour.Text = "01"; Flush(window);
            Assert.True(picker.IsInputValid);
            Assert.Equal(!vibrant, label.IsVisible);
            Assert.Equal(AutomationLiveSetting.Off, AutomationProperties.GetLiveSetting(label));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, "2", TimeSelection.Minute)]
    [InlineData(true, "2", TimeSelection.Hour)]
    [InlineData(false, "11", TimeSelection.Minute)]
    [InlineData(true, "11", TimeSelection.Hour)]
    public void TouchExplorationSuppressesAutomaticAdvancement(bool exploration, string digits, TimeSelection expected)
    {
        var picker = new TimeInput { SelectedTime = new(1, 30), Is24Hour = false, TouchExplorationEnabled = exploration };
        var window = Show(picker);
        try
        {
            var hour = Part<TextBox>(picker, "HourInput");
            hour.SelectAll(); window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            window.KeyTextInput(digits); Flush(window);
            Assert.Equal(expected, picker.Selection);
            Assert.Equal(int.Parse(digits, CultureInfo.InvariantCulture), picker.SelectedTime.Hour);
            if (exploration)
            {
                window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
                Assert.Equal(TimeSelection.Minute, picker.Selection);
                Assert.True(Part<TextBox>(picker, "MinuteInput").IsFocused);
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void QueuedSelectionFocusCannotStealFocusAfterDetachment()
    {
        var picker = new TimeInput();
        var window = Show(picker);
        try
        {
            picker.Selection = TimeSelection.Minute;
            var replacement = new TextBox();
            window.Content = replacement; window.UpdateLayout(); replacement.Focus(); Flush(window);
            Assert.True(replacement.IsFocused);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TimeSelectorsExposeControlledRadioSelectionToAutomation(bool dial)
    {
        Control picker = dial ? new Material3Expressive.Components.TimePicker { SelectedTime = new(2, 30) }
            : new TimeInput { SelectedTime = new(2, 30) };
        var window = Show(picker);
        try
        {
            var button = Part<Button>(picker, dial ? "MinuteSelector" : "MinuteInputSelector");
            var peer = ControlAutomationPeer.CreatePeerForElement(button)!;
            Assert.Equal(AutomationControlType.RadioButton, peer.GetAutomationControlType());
            var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
            Assert.False(selection.IsSelected);
            selection.Select(); Flush(window);
            Assert.Equal(TimeSelection.Minute, picker.GetValue(Material3Expressive.Components.TimePicker.SelectionProperty));
            Assert.True(selection.IsSelected);
            selection.RemoveFromSelection(); Flush(window);
            Assert.True(selection.IsSelected);
            Assert.Equal(new TimeOnly(2, 30), picker.GetValue(Material3Expressive.Components.TimePicker.SelectedTimeProperty));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ErrorCaretStillFollowsTheLocalPrimaryColor(bool vibrant)
    {
        var picker = new TimeInput { Variant = vibrant ? TimePickerVariant.Vibrant : TimePickerVariant.Standard };
        var key = Tokens.MaterialTokens.Sys.Color.Primary;
        picker.Resources[key] = Colors.Magenta;
        var window = Show(picker);
        try
        {
            var field = Part<TextBox>(picker, "HourInput");
            field.Text = "99"; Flush(window);
            Assert.False(picker.IsInputValid);
            var brush = Assert.IsType<LinearGradientBrush>(field.CaretBrush);
            Assert.All(brush.GradientStops.Where(s => s.Color.A > 0), s => Assert.Equal(Colors.Magenta, s.Color));
            picker.Resources[key] = Colors.Lime; Flush(window);
            brush = Assert.IsType<LinearGradientBrush>(field.CaretBrush);
            Assert.All(brush.GradientStops.Where(s => s.Color.A > 0), s => Assert.Equal(Colors.Lime, s.Color));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SupportingTextReservesTwoLinesWithAutomaticLineHeight()
    {
        var picker = new TimeInput();
        var window = Show(picker);
        try
        {
            var label = Part<TextBlock>(picker, "HourSupportingText");
            label.LineHeight = double.NaN; Flush(window);
            Assert.True(label.Bounds.Height >= label.FontSize * 2);
            label.FontSize = 24; Flush(window);
            Assert.True(label.Bounds.Height >= 48);
        }
        finally { window.Close(); }
    }

    private static T Part<T>(Control picker, string name) where T : Control =>
        picker.GetVisualDescendants().OfType<T>().Single(c => c.Name == "PART_" + name);
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 800, Height = 800 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}

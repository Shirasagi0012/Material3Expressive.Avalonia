using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class TimeInputEditingTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypingAtStartReplacesTheExistingPairWithOneDigit(bool minute)
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Is24Hour = false,
            Selection = minute ? TimeSelection.Minute : TimeSelection.Hour };
        var window = Show(picker);
        try
        {
            var field = Editor(picker);
            field.SelectionStart = field.SelectionEnd = 0;
            window.KeyTextInput("1"); Flush(window);
            Assert.Equal("1", field.Text);
            Assert.Equal(1, field.SelectionStart);
            Assert.Equal(1, field.SelectionEnd);
            Assert.Equal(minute ? new TimeOnly(2, 1) : new TimeOnly(1, 30), picker.SelectedTime);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("007", "0", 7, true)]
    [InlineData("0001", "0", 1, true)]
    [InlineData("-1", "-1", 30, false)]
    [InlineData("75", "75", 30, false)]
    [InlineData("100", "30", 30, true)]
    [InlineData("hello", "30", 30, true)]
    [InlineData("١", "١", 1, true)]
    [InlineData("１２", "１２", 12, true)]
    public void TextReplacementUsesTheUpstreamParseThenReductionOrder(string input, string text, int value, bool valid)
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Selection = TimeSelection.Minute };
        var window = Show(picker);
        try
        {
            var field = Editor(picker);
            field.SelectAll(); window.KeyTextInput(input); Flush(window);
            Assert.Equal(text, field.Text);
            Assert.Equal(value, picker.SelectedTime.Minute);
            Assert.Equal(valid, picker.IsInputValid);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InvalidPmSelectorUsesHourInputWhileTheEditorRetainsItsDraft()
    {
        var picker = new TimeInput { SelectedTime = new(14, 30), Is24Hour = false };
        var window = Show(picker);
        try
        {
            var field = Editor(picker); field.Text = "99"; Flush(window);
            picker.Selection = TimeSelection.Minute; Flush(window);
            var selector = picker.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "PART_HourInputSelector");
            Assert.Equal("111", selector.Content);
            Assert.Equal(new TimeOnly(14, 30), picker.SelectedTime);
            picker.Selection = TimeSelection.Hour; Flush(window);
            Assert.Equal("99", field.Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("02", 2)]
    [InlineData("１２", 0)]
    public void DigitAtEndOfCompleteHourMovesToMinuteBeforeTextInput(string digits, int expectedHour)
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Is24Hour = false, TouchExplorationEnabled = true };
        var window = Show(picker);
        try
        {
            var hour = Editor(picker); hour.Text = digits; Flush(window);
            hour.SelectionStart = hour.SelectionEnd = 2;
            window.KeyPress(Key.D5, RawInputModifiers.None, PhysicalKey.Digit5, "5");
            window.KeyTextInput("5"); Flush(window);
            Assert.Equal(TimeSelection.Minute, picker.Selection);
            Assert.Equal("5", Editor(picker).Text);
            Assert.Equal(new TimeOnly(expectedHour, 5), picker.SelectedTime);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BackspaceAtMinuteStartDoesNotChangeFieldOrTime()
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Selection = TimeSelection.Minute };
        var window = Show(picker);
        try
        {
            var field = Editor(picker); field.SelectionStart = field.SelectionEnd = 0;
            window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            Assert.Equal(TimeSelection.Minute, picker.Selection);
            Assert.Equal("30", field.Text);
            Assert.Equal(new TimeOnly(2, 30), picker.SelectedTime);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RejectedReplacementRetainsThePreviousSelection()
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Selection = TimeSelection.Minute };
        var window = Show(picker);
        try
        {
            var field = Editor(picker); field.SelectAll();
            window.KeyTextInput("100"); Flush(window);
            Assert.Equal("30", field.Text);
            Assert.Equal(0, field.SelectionStart);
            Assert.Equal(2, field.SelectionEnd);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExternalHourUpdatePreservesTheUnchangedMinuteDraftAndSelection()
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Is24Hour = true };
        var window = Show(picker);
        try
        {
            var hour = Editor(picker); hour.Text = "99"; Flush(window);
            picker.Selection = TimeSelection.Minute; Flush(window);
            var minute = Editor(picker); minute.Text = "75"; Flush(window);
            minute.SelectionStart = 0; minute.SelectionEnd = 1;
            picker.SelectedTime = new(5, 30); Flush(window);
            Assert.Equal("05", hour.Text);
            Assert.Equal("75", minute.Text);
            Assert.Equal(0, minute.SelectionStart); Assert.Equal(1, minute.SelectionEnd);
            Assert.False(picker.IsInputValid);
            Assert.False(MaterialTextField.GetIsError(hour));
            Assert.Empty(MaterialTextField.GetErrorMessage(hour)!);
            picker.SelectedTime = new(5, 45); Flush(window);
            Assert.Equal("45", minute.Text);
            Assert.True(picker.IsInputValid);
            Assert.Equal(0, minute.SelectionStart); Assert.Equal(0, minute.SelectionEnd);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InvalidNegativeSelectorUsesMinimumWidthIncludingTheSign()
    {
        var picker = new TimeInput { SelectedTime = new(2, 30), Selection = TimeSelection.Minute };
        var window = Show(picker);
        try
        {
            Editor(picker).Text = "-1"; Flush(window);
            picker.Selection = TimeSelection.Hour; Flush(window);
            var selector = picker.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "PART_MinuteInputSelector");
            Assert.Equal("-1", selector.Content);
        }
        finally { window.Close(); }
    }

    private static TextBox Editor(Control picker) => picker.GetVisualDescendants().OfType<TextBox>().Single(x => x.IsEffectivelyVisible);
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 800, Height = 800 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}

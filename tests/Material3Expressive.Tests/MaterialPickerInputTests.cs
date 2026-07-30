// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// DateInput.kt:59-250,282-359 and TimePicker.kt:302-332,950-1180,1498-1665.

using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialPickerInputTests
{
    [AvaloniaFact]
    public void DateInputAndCalendarShareCommittedState()
    {
        var state = new MaterialDatePickerState(new DateTime(2024, 7, 4));
        var input = DateInput(state);
        var picker = new MaterialDatePicker { State = state };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { input, picker } });

        Assert.Equal("07/04/2024", input.Text);
        Assert.Equal(state.SelectedDate, picker.SelectedDate);

        input.Text = "03/14/2025";

        Assert.True(input.CommitEdit());
        Assert.Equal(new DateTime(2025, 3, 14), state.SelectedDate);
        Assert.Equal(new DateTime(2025, 3, 14), picker.SelectedDate);
        Assert.Equal(new DateTime(2025, 3, 1), picker.DisplayMonth);
    }

    [AvaloniaFact]
    public void DateInputRejectsPatternRangeAndImpossibleDatesWithoutChangingSelection()
    {
        var original = new DateTime(2024, 7, 4);
        var input = DateInput(new MaterialDatePickerState(original));
        input.MinimumDate = new DateTime(2020, 1, 1);
        input.MaximumDate = new DateTime(2030, 12, 31);
        ThemeTestHelper.ShowWindow(input);

        input.Text = "02/30/2025";
        Assert.False(input.CommitEdit());
        Assert.False(input.IsInputValid);
        Assert.True(input.IsError);
        Assert.Contains("MM/dd/yyyy", input.ValidationError);
        Assert.Equal(original, input.SelectedDate);

        input.Text = "03/14/2035";
        Assert.False(input.CommitEdit());
        Assert.Contains("between", input.ValidationError);
        Assert.Equal(original, input.SelectedDate);
    }

    [AvaloniaFact]
    public void DateEscapeCancelsAndLeavingTheNativeEditorCommits()
    {
        var original = new DateTime(2024, 7, 4);
        var input = DateInput(new MaterialDatePickerState(original));
        var next = new Button { Content = "Next" };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { input, next } });

        Assert.True(input.FocusInput());
        input.Text = "03/14/2025";
        EditorOf(input).RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Escape
        });

        Assert.Equal(original, input.SelectedDate);
        Assert.Equal("07/04/2024", input.Text);

        input.Text = "03/14/2025";
        Assert.True(next.Focus());
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new DateTime(2025, 3, 14), input.SelectedDate);
    }

    [AvaloniaFact]
    public void DateStateRestoresModeAndReplacementDetachesTheOldInstance()
    {
        var oldState = new MaterialDatePickerState(new DateTime(2024, 7, 4));
        var restored = new MaterialDatePickerState(
            new DateTime(2030, 8, 9),
            new DateTime(2031, 2, 20),
            DatePickerDisplayMode.Input);
        var input = DateInput(oldState);

        input.State = restored;
        oldState.SelectedDate = new DateTime(2001, 1, 1);

        Assert.Same(restored, input.State);
        Assert.Equal(new DateTime(2030, 8, 9), input.SelectedDate);
        Assert.Equal(new DateTime(2031, 2, 1), restored.DisplayMonth);
        Assert.Equal(DatePickerDisplayMode.Input, restored.DisplayMode);

        input.ResetState();
        Assert.NotSame(restored, input.State);
        Assert.Null(input.SelectedDate);
    }

    [AvaloniaFact]
    public void ApplicationStateChangesSupersedeAnActiveDraft()
    {
        var state = new MaterialDatePickerState(new DateTime(2024, 7, 4));
        var input = DateInput(state);
        ThemeTestHelper.ShowWindow(input);

        Assert.True(input.FocusInput());
        input.Text = "03/14/2025";
        state.SelectedDate = new DateTime(2030, 8, 9);
        input.CancelEdit();

        Assert.Equal(new DateTime(2030, 8, 9), input.SelectedDate);
        Assert.Equal("08/09/2030", input.Text);
    }

    [AvaloniaFact]
    public void DateRangeCommitsAtomicallyAndRejectsReversedEndpoints()
    {
        var originalStart = new DateTime(2024, 7, 4);
        var originalEnd = new DateTime(2024, 7, 8);
        var state = new MaterialDateRangePickerState(originalStart, originalEnd);
        var input = new MaterialDateRangeInput
        {
            State = state,
            Culture = CultureInfo.GetCultureInfo("en-US"),
            DateFormat = "MM/dd/yyyy"
        };
        ThemeTestHelper.ShowWindow(input);

        input.StartText = "03/14/2025";
        input.EndText = "03/20/2025";
        Assert.True(input.CommitEdit());
        Assert.Equal(new DateTime(2025, 3, 14), state.StartDate);
        Assert.Equal(new DateTime(2025, 3, 20), state.EndDate);

        input.StartText = "04/01/2025";
        input.EndText = "03/20/2025";
        Assert.False(input.CommitEdit());
        Assert.Contains("must not be after", input.ValidationError);
        Assert.Equal(new DateTime(2025, 3, 14), state.StartDate);
        Assert.Equal(new DateTime(2025, 3, 20), state.EndDate);
    }

    [AvaloniaFact]
    public void EscapeCancelsDateDraftAndContinuesToDismissItsDialog()
    {
        var original = new DateTime(2024, 7, 4);
        var input = DateInput(new MaterialDatePickerState(original));
        var dialog = new MaterialDialog
        {
            IsOpen = true,
            Content = input
        };
        ThemeTestHelper.ShowWindow(dialog);

        Assert.True(input.FocusInput());
        input.Text = "03/14/2025";
        EditorOf(input).RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Escape
        });

        Assert.False(dialog.IsOpen);
        Assert.Equal(original, input.SelectedDate);
    }

    [AvaloniaFact]
    public void TimeInputAndDialShareCommittedState()
    {
        var state = new MaterialTimePickerState(new TimeSpan(9, 30, 0));
        var input = new MaterialTimeInput { State = state };
        var picker = new MaterialTimePicker { State = state };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { input, picker } });

        input.HourText = "21";
        input.MinuteText = "45";

        Assert.True(input.CommitEdit());
        Assert.Equal(new TimeSpan(21, 45, 0), state.Time);
        Assert.Equal(state.Time, picker.Time);
    }

    [AvaloniaFact]
    public void TwelveHourInputConvertsPeriodsAndRejectsInvalidFields()
    {
        var state = new MaterialTimePickerState(new TimeSpan(9, 30, 0), is24Hour: false);
        var input = new MaterialTimeInput { State = state };
        ThemeTestHelper.ShowWindow(input);

        input.HourText = "12";
        input.MinuteText = "05";
        input.Period = TimePickerPeriod.Pm;

        Assert.True(input.CommitEdit());
        Assert.Equal(new TimeSpan(12, 5, 0), state.Time);

        input.HourText = "13";
        input.MinuteText = "72";

        Assert.False(input.CommitEdit());
        Assert.False(input.IsInputValid);
        Assert.Contains("01–12", input.ValidationError);
        Assert.Equal(new TimeSpan(12, 5, 0), state.Time);
    }

    [AvaloniaFact]
    public void TimeEscapeCancelsAndEnterCommitsThroughTheTemplatedEditor()
    {
        var original = new TimeSpan(9, 30, 0);
        var input = new MaterialTimeInput
        {
            State = new MaterialTimePickerState(original)
        };
        ThemeTestHelper.ShowWindow(input);
        Assert.True(input.FocusInput());

        input.HourText = "21";
        input.MinuteText = "45";
        HourEditorOf(input).RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Escape
        });

        Assert.Equal(original, input.Time);
        Assert.Equal("09", input.HourText);
        Assert.Equal("30", input.MinuteText);

        input.HourText = "21";
        input.MinuteText = "45";
        HourEditorOf(input).RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Enter
        });

        Assert.Equal(new TimeSpan(21, 45, 0), input.Time);
    }

    [AvaloniaFact]
    public void TimeStateRestoresFormatSelectionAndResetCreatesFreshState()
    {
        var oldState = new MaterialTimePickerState(new TimeSpan(9, 30, 0));
        var restored = new MaterialTimePickerState(
            new TimeSpan(17, 5, 0),
            is24Hour: false,
            field: TimePickerField.Minute);
        var input = new MaterialTimeInput { State = oldState };

        input.State = restored;
        oldState.Time = new TimeSpan(1, 1, 0);

        Assert.Equal(new TimeSpan(17, 5, 0), input.Time);
        Assert.False(input.Use24HourClock);
        Assert.Equal(TimePickerField.Minute, restored.Field);

        input.ResetState();
        Assert.NotSame(restored, input.State);
        Assert.Equal(TimeSpan.Zero, input.Time);
        Assert.True(input.Use24HourClock);
    }

    private static MaterialDateInput DateInput(MaterialDatePickerState state) =>
        new()
        {
            State = state,
            Culture = CultureInfo.GetCultureInfo("en-US"),
            DateFormat = "MM/dd/yyyy"
        };

    private static TextBox EditorOf(MaterialDateInput input) =>
        Assert.Single(input.GetVisualDescendants().OfType<TextBox>());

    private static TextBox HourEditorOf(MaterialTimeInput input) =>
        input.GetVisualDescendants()
            .OfType<MaterialTextField>()
            .Single(field => field.Label == "Hour")
            .GetVisualDescendants()
            .OfType<TextBox>()
            .Single();
}

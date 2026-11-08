using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Catalog;

public partial class PickerDialogDemo : Window
{
    private DateOnly? _date = DateOnly.FromDateTime(DateTime.Today);
    private DateRange _range;
    private TimeOnly _time = new(14, 30);
    private bool _timeInput;
    private bool _vibrantTimeInput;

    public PickerDialogDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        _range = new(_date, _date?.AddDays(7));
        ShowCommitted();
    }

    private void OnOpenDate(object? sender, RoutedEventArgs e)
    {
        DraftDate.SelectedDate = _date;
        DateDialog.IsOpen = true;
    }

    private void OnOpenRange(object? sender, RoutedEventArgs e)
    {
        DraftRange.SelectedRange = _range;
        RangeDialog.IsOpen = true;
    }

    private void OnOpenTime(object? sender, RoutedEventArgs e)
    {
        DraftDial.SelectedTime = _time;
        TimeDialog.IsOpen = true;
    }

    private void OnOpenVibrantTime(object? sender, RoutedEventArgs e)
    {
        VibrantDraftDial.SelectedTime = _time;
        VibrantTimeDialog.IsOpen = true;
    }

    private void OnConfirmVibrantTime(object? sender, RoutedEventArgs e)
    {
        _time = VibrantDraftDial.SelectedTime;
        VibrantTimeDialog.IsOpen = false;
        ShowCommitted();
    }

    private void OnCancelVibrantTime(object? sender, RoutedEventArgs e) => VibrantTimeDialog.IsOpen = false;

    private void OnToggleVibrantTimeMode(object? sender, RoutedEventArgs e)
    {
        _vibrantTimeInput = !_vibrantTimeInput;
        VibrantDraftDial.IsVisible = !_vibrantTimeInput;
        VibrantDraftInput.IsVisible = _vibrantTimeInput;
        VibrantTimeModeButton.DisplayMode = _vibrantTimeInput ? TimePickerDisplayMode.Input : TimePickerDisplayMode.Picker;
    }

    private void OnConfirmDate(object? sender, RoutedEventArgs e)
    {
        _date = DraftDate.SelectedDate;
        DateDialog.IsOpen = false;
        ShowCommitted();
    }

    private void OnConfirmRange(object? sender, RoutedEventArgs e)
    {
        _range = DraftRange.SelectedRange;
        RangeDialog.IsOpen = false;
        ShowCommitted();
    }

    private void OnConfirmTime(object? sender, RoutedEventArgs e)
    {
        _time = DraftDial.SelectedTime;
        TimeDialog.IsOpen = false;
        ShowCommitted();
    }

    private void OnCancelDate(object? sender, RoutedEventArgs e) => DateDialog.IsOpen = false;
    private void OnCancelRange(object? sender, RoutedEventArgs e) => RangeDialog.IsOpen = false;
    private void OnCancelTime(object? sender, RoutedEventArgs e) => TimeDialog.IsOpen = false;

    private void OnToggleTimeMode(object? sender, RoutedEventArgs e)
    {
        _timeInput = !_timeInput;
        DraftDial.IsVisible = !_timeInput;
        DraftInput.IsVisible = _timeInput;
        TimeModeButton.DisplayMode = _timeInput ? TimePickerDisplayMode.Input : TimePickerDisplayMode.Picker;
        TimeTitle.DisplayMode = TimeModeButton.DisplayMode;
    }

    private void OnDismissRequested(object? sender, DismissRequestedEventArgs e)
        => ((Dialog)sender!).IsOpen = false;

    private void ShowCommitted()
    {
        var range = _range.Start is { } start
            ? _range.End is { } end ? $"{start:d} – {end:d}" : $"{start:d} –"
            : "none";
        Result.Text = $"Date: {_date:d}{Environment.NewLine}Range: {range}{Environment.NewLine}Time: {_time:t}";
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class DateTimePickerDemo : Window
{
    public DateTimePickerDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        Date.SelectedDate=DateOnly.FromDateTime(DateTime.Today);
        Range.SelectedRange=new(Date.SelectedDate,Date.SelectedDate?.AddDays(7));
        Dial.SelectedTime=new(14,30);
    }
    private void ToggleTheme(object? sender,RoutedEventArgs e)=>RequestedThemeVariant=ActualThemeVariant==ThemeVariant.Dark?ThemeVariant.Light:ThemeVariant.Dark;
    private void ToggleDirection(object? sender,RoutedEventArgs e)=>FlowDirection=FlowDirection==FlowDirection.LeftToRight?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
    private void ToggleClock(object? sender,RoutedEventArgs e)=>Dial.Is24Hour=!Dial.Is24Hour;
}

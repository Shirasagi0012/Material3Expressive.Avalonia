using System;
using Avalonia.Controls;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class NumericUpDownDemo : Window
{
    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    public NumericUpDownDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
    }

    private void OnWeekdaySpin(object? sender, SpinEventArgs e)
    {
        if (sender is not ButtonSpinner spinner) return;
        var index = Array.IndexOf(Weekdays, spinner.Content as string);
        index = (index + (e.Direction == SpinDirection.Increase ? 1 : -1) + Weekdays.Length) % Weekdays.Length;
        spinner.Content = Weekdays[index];
    }
}

using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class ButtonGroupDemo : Window
{
    public ButtonGroupDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }

    // The sample writes the clicked index back and ignores the unchecked value, so the current item stays selected.
    private void OnSingleSelect(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton selected || selected.Parent is not Panel panel)
            return;

        foreach (var child in panel.Children)
        {
            if (child is ToggleButton button)
                button.IsChecked = ReferenceEquals(button, selected);
        }
    }
}

public sealed class CheckedFillConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1d : 0d;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

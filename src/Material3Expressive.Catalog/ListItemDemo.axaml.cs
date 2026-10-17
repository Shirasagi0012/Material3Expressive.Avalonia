using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class ListItemDemo : Window
{
    public ListItemDemo() { AnimationScope.Initialize(this); InitializeComponent(); }
    private void OnClick(object? sender, RoutedEventArgs e) => Status.Text = "Clicked";
    private void OnLongClick(object? sender, EventArgs e) => Status.Text = "Long clicked";
    private void OnHelp(object? sender, RoutedEventArgs e) => Status.Text = "Nested help remains available";
    private void OnCheckedChange(object? sender, ListItemCheckedChangeEventArgs e)
    {
        if (sender is ListItem item) item.IsChecked = e.Value;
        Status.Text = $"Requested checked: {e.Value}";
    }
}

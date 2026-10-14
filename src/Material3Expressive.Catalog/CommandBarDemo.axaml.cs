using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class CommandBarDemo : Window
{
    public CommandBarDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }

    private void OnAction(object? sender, RoutedEventArgs e)
    {
        Status.Text = sender switch
        {
            CommandBarToggleButton toggle => $"{toggle.Label}: {toggle.IsChecked}",
            CommandBarButton command => $"Executed: {command.Label}",
            _ => Status.Text
        };
    }
}

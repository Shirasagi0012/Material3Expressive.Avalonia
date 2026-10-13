using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class RepeatButtonDemo : Window
{
    private int _count;

    public RepeatButtonDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }

    private void OnIncrease(object? sender, RoutedEventArgs e) => CountText.Text = (++_count).ToString();
    private void OnDecrease(object? sender, RoutedEventArgs e) => CountText.Text = (--_count).ToString();
    private void OnReset(object? sender, RoutedEventArgs e) => CountText.Text = (_count = 0).ToString();
}

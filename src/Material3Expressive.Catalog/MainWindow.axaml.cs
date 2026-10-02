using ComposeAnimation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3Expressive.Components;
using System.Linq;

namespace Material3Expressive.Catalog;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        // Animated.SetDurationScale(this, 4);
    }

    private void OnDestinationClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in DemoNavigationRail.Items.OfType<NavigationRailItem>())
            item.IsSelected = ReferenceEquals(item, sender);
    }
}

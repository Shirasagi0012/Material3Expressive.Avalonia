using ComposeAnimation;
using Avalonia.Controls;

namespace Material3Expressive.Catalog;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        Animated.SetDurationScale(this, 4);
    }
}

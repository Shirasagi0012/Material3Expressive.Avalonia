using Avalonia.Controls;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class LoadingIndicatorDemo : Window
{
    public LoadingIndicatorDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
    }
}

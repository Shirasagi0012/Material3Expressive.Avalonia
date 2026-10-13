using Avalonia.Controls;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class LoadingIndicatorDemo : Window
{
    public LoadingIndicatorDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }
}

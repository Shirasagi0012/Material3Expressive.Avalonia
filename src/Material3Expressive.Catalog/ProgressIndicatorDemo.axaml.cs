using Avalonia.Controls;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class ProgressIndicatorDemo : Window
{
    public ProgressIndicatorDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }
}

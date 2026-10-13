using Avalonia.Controls;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class ScrollBarDemo : Window
{
    public ScrollBarDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }
}

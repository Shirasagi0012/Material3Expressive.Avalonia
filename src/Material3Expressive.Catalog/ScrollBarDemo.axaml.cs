using Avalonia.Controls;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class ScrollBarDemo : Window
{
    public ScrollBarDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
    }
}

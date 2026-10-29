using Avalonia.Controls;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class InsetFocusRingDemo : Window
{
    public InsetFocusRingDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }
}

using Avalonia.Controls;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class FloatingToolbarDemo : Window
{
    public FloatingToolbarDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        ScrollingToolbar.ScrollBehavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(FloatingToolbarExitDirection.Bottom);
    }
}

using Avalonia.Controls;
using Avalonia.Styling;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class GroupBoxDemo : Window
{
    public GroupBoxDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        DarkToggle.IsCheckedChanged += (_, _) =>
            RequestedThemeVariant = DarkToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class ExpanderDemo : Window
{
    public ExpanderDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
        DarkToggle.IsCheckedChanged += (_, _) =>
            RequestedThemeVariant = DarkToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
        RtlToggle.IsCheckedChanged += (_, _) =>
            Root.FlowDirection = RtlToggle.IsChecked == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }
}

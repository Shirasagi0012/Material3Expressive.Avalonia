using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class SurfaceDemo : Window
{
    private int _opens;
    public SurfaceDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        CutRtl.IsCheckedChanged += (_, _) => CutExamples.FlowDirection = CutRtl.IsChecked == true
            ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
    }
    private void OnOpen(object? sender, RoutedEventArgs e) => Status.Text = $"Opened {_opens += 1} times";
    private void OnSelect(object? sender, RoutedEventArgs e)
    {
        foreach (var control in Views.Children)
            if (control is SelectableSurface surface) surface.IsSelected = ReferenceEquals(sender, surface);
    }
    private void OnToggle(object? sender, SurfaceCheckedChangeEventArgs e) => ((ToggleableSurface)sender!).IsChecked = e.Value;
}

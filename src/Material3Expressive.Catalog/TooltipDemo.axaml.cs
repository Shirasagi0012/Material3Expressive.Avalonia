using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class TooltipDemo : Window
{
    public TooltipDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
    }
    private async void OnShow(object? sender, RoutedEventArgs e)
    {
        try { await Programmatic.State.ShowAsync(); }
        catch (OperationCanceledException) { }
    }
}

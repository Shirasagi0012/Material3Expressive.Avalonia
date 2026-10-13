using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class TabRowDemo : Window
{
    public TabRowDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }
    private void OnTabClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Tab tab || tab.FindAncestorOfType<TabRow>() is not { } row) return;
        row.SelectedIndex = row.Items.IndexOf(tab);
        foreach (var item in row.Items.OfType<Tab>()) item.IsSelected = ReferenceEquals(item, tab);
    }
}

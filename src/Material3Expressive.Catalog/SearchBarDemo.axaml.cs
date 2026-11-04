using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class SearchBarDemo : Window
{
    private static readonly string[] Components = ["Button", "Card", "DatePicker", "DockedSearchBar", "ListItem", "NavigationRail", "Slider", "TextField", "TimeScroll"];
    private bool _selecting;
    public SearchBarDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        UpdateResults();
    }
    private void QueryChanged(object? sender, TextChangedEventArgs e)
    {
        if (Results is null || _selecting) return;
        UpdateResults(); Search.IsExpanded = true;
    }
    private void UpdateResults()
    {
        Results.Children.Clear();
        foreach (var name in Components.Where(name => name.Contains(Query.Text ?? "", StringComparison.OrdinalIgnoreCase)))
        {
            var button = new Button { Content = name, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            button.Click += (_, _) => Select(name);
            Results.Children.Add(button);
        }
        if (Results.Children.Count == 0) Results.Children.Add(new TextBlock { Text = "没有匹配的组件", Margin = new(16) });
    }
    private void Select(string name)
    {
        _selecting = true;
        try { Query.Text = name; Status.Text = $"已选择：{name}"; Search.IsExpanded = false; }
        finally { _selecting = false; }
    }
    private void SearchSubmitted(object? sender, RoutedEventArgs e) => Select(Query.Text ?? "");
    private void OpenSearch(object? sender, RoutedEventArgs e) => Search.IsExpanded = true;
    private void CollapseSearch(object? sender, RoutedEventArgs e) => Search.IsExpanded = false;
    private void ToggleExpanded(object? sender, RoutedEventArgs e) => Search.IsExpanded = !Search.IsExpanded;
    private void ToggleTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) => FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

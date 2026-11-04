using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class ExpandedSearchBarDemo : Window
{
    private static readonly string[] Names = ["Button", "Card", "DatePicker", "ExpandedDockedSearchBar", "ListItem", "NavigationRail", "SearchBar", "Slider", "TextField", "TimeScroll"];
    private SearchBarState State => (SearchBarState)Resources["SearchState"]!;
    public ExpandedSearchBarDemo()
    {
        AnimationScope.Initialize(this); InitializeComponent(); UpdateResults();
    }
    private void OpenSearch(object? sender, RoutedEventArgs e) => _ = State.AnimateToExpandedAsync();
    private void QueryChanged(object? sender, TextChangedEventArgs e) { if (Results is not null) UpdateResults(); }
    private void UpdateResults()
    {
        Results.Children.Clear();
        foreach (var name in Names.Where(name => name.Contains(Query.Text ?? "", StringComparison.OrdinalIgnoreCase)))
        {
            var button = new Button { Content = name, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += (_, _) => Select(name); Results.Children.Add(button);
        }
        if (Results.Children.Count == 0) Results.Children.Add(new TextBlock { Text = "没有匹配的组件", Margin = new(16) });
    }
    private void Select(string name)
    {
        Status.Text = $"已选择：{name}"; SearchAnchor.Content = name; _ = State.AnimateToCollapsedAsync();
    }
    private void SearchSubmitted(object? sender, RoutedEventArgs e) => Select(Query.Text ?? "");
    private void ToggleTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) => FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

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

public partial class SearchAppBarDemo : Window
{
    private readonly SearchBarScrollBehavior _scroll = new();
    private SearchBarState State => (SearchBarState)Resources["SearchState"]!;
    public SearchAppBarDemo()
    {
        AnimationScope.Initialize(this); InitializeComponent(); Bar.ScrollBehavior = _scroll; UpdateResults();
        foreach (var i in Enumerable.Range(1, 24))
            Examples.Items.Add(new TextBlock { Text = $"组件示例 {i:00} · 滚动正文查看搜索顶栏联动", Margin = new(0, 12) });
    }
    private void OpenSearch(object? sender, RoutedEventArgs e) => _ = State.AnimateToExpandedAsync();
    private void QueryChanged(object? sender, TextChangedEventArgs e) { if (Results is not null) UpdateResults(); }
    private void UpdateResults()
    {
        Results.Children.Clear();
        foreach (var name in new[] { "AppBarWithSearch", "Button", "Card", "DatePicker", "SearchBar", "Slider", "TextField" }.Where(n => n.Contains(Query.Text ?? "", StringComparison.OrdinalIgnoreCase)))
        {
            var button = new Button { Content = name, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += (_, _) => Select(name); Results.Children.Add(button);
        }
    }
    private void Select(string name) { Status.Text = $"已选择：{name}"; _ = State.AnimateToCollapsedAsync(); }
    private void SearchSubmitted(object? sender, RoutedEventArgs e) => Select(Query.Text ?? "");
    private void NavigationClicked(object? sender, RoutedEventArgs e) => Status.Text = "导航操作";
    private void ToggleTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) => FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    private void ScrollUp(object? sender, RoutedEventArgs e) { _scroll.OnPreScroll(-16); _scroll.OnPostScroll(-8, 0); }
    private void ScrollDown(object? sender, RoutedEventArgs e) { _scroll.OnPreScroll(16); _scroll.OnPostScroll(8, 0); }
    private void ReleaseScroll(object? sender, RoutedEventArgs e) => _ = _scroll.OnPostFlingAsync(0);
    private void ResetScroll(object? sender, RoutedEventArgs e) { _scroll.State.ScrollOffset = 0; _scroll.State.ContentOffset = 0; }
    private void ScrollEnabledChanged(object? sender, RoutedEventArgs e) => _scroll.CanScroll = ((CheckBox)sender!).IsChecked != true;
    private void InsetsChanged(object? sender, RoutedEventArgs e)
    {
        if (Bar is not null) Bar.WindowInsets = ((CheckBox)sender!).IsChecked == true ? new(16, 8, 24, 12) : default;
    }
    private void TransparencyChanged(object? sender, RoutedEventArgs e)
    {
        if (Bar is null) return;
        if (((CheckBox)sender!).IsChecked == true) Bar.Background = Brushes.Transparent;
        else Bar.ClearValue(BackgroundProperty);
    }
}

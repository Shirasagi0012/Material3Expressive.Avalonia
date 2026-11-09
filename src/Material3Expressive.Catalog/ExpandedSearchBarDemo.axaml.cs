using Material3Expressive.Foundation.Rendering;
using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Catalog;

internal enum SearchPresentation { Docked, Gap, FullScreen, Contained }

public partial class ExpandedSearchBarDemo : Window
{
    private static readonly string[] Names = ["Button", "Card", "DatePicker", "ExpandedDockedSearchBar", "ListItem", "NavigationRail", "SearchBar", "Slider", "TextField", "TimeScroll"];
    private SearchBarState State => (SearchBarState)Resources["SearchState"]!;
    public ExpandedSearchBarDemo() : this(SearchPresentation.Docked) { }
    internal ExpandedSearchBarDemo(SearchPresentation presentation)
    {
        AnimationScope.Initialize(this); InitializeComponent();
        if (presentation != SearchPresentation.Docked)
        {
            ExpandedDockedSearchBar popup = presentation switch
            {
                SearchPresentation.Gap => new ExpandedDockedSearchBarWithGap(),
                SearchPresentation.FullScreen => new ExpandedFullScreenSearchBar(),
                _ => new ExpandedFullScreenContainedSearchBar()
            };
            if (presentation == SearchPresentation.Contained)
            {
                Resources["SearchState"] = SearchBarState.CreateContained();
                CollapsedSearch.State = SearchAnchor.State = Query.State = State;
                Query.Classes.Add("contained");
            }
            if (presentation == SearchPresentation.Gap)
            {
                Resources["SearchState"] = SearchBarState.CreateWithGap();
                CollapsedSearch.State = SearchAnchor.State = Query.State = State;
            }
            Title = presentation == SearchPresentation.Gap ? "Search popup with gap" : $"{presentation} search";
            Heading.Text = $"SearchBar + {popup.GetType().Name}";
            if (popup is ExpandedFullScreenSearchBar)
            {
                Query.HorizontalAlignment = HorizontalAlignment.Stretch;
                Query.VerticalAlignment = VerticalAlignment.Stretch;
                Description.Text = "搜索覆盖客户区。Escape、输入框返回按钮或选择结果后收起；输入和结果使用独立实例。";
            }
            var input = SearchPopup.InputField;
            var content = SearchPopup.Content;
            SearchPopup.InputField = null; SearchPopup.Content = null;
            Root.Children.Remove(SearchPopup);
            popup.State = State; popup.InputField = input; popup.Content = content; Root.Children.Add(popup);
        }
        AllowEscape.IsEnabled = presentation is SearchPresentation.FullScreen or SearchPresentation.Contained;
        UpdateResults();
    }
    private void CloseSearch(object? sender, RoutedEventArgs e) => _ = State.AnimateToCollapsedAsync();
    private void DismissalPolicyChanged(object? sender, RoutedEventArgs e)
    {
        if (Root is null || AllowEscape is null || AllowOutside is null) return;
        foreach (var popup in Root.Children.OfType<ExpandedDockedSearchBar>())
        {
            if (popup is ExpandedFullScreenSearchBar fullScreen) fullScreen.DismissOnEscape = AllowEscape.IsChecked == true;
            popup.DismissOnPointerOutside = AllowOutside.IsChecked == true;
        }
    }
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
        Status.Text = $"已选择：{name}"; Query.Text = name; _ = State.AnimateToCollapsedAsync();
    }
    private void SearchSubmitted(object? sender, RoutedEventArgs e) => Select(Query.Text ?? "");
    private void ToggleShape(object? sender, RoutedEventArgs e)
    {
        MaterialShape? shape = CollapsedSearch.Shape is null ? new CutCornerShape { TopStart = new(20, Avalonia.RelativeUnit.Absolute), BottomEnd = new(20, Avalonia.RelativeUnit.Absolute) } : null;
        CollapsedSearch.Shape = SearchAnchor.Shape = Query.Shape = shape;
        foreach (var popup in Root.Children.OfType<ExpandedDockedSearchBar>())
        {
            popup.Shape = shape;
            if (popup is ExpandedDockedSearchBarWithGap gap)
                gap.DropdownShape = shape is null ? null : new CutCornerShape(12);
        }
        Status.Text = shape is null ? "已恢复默认形状" : "已设置切角；打开搜索可查看展开形状与焦点环";
    }
    private void ToggleFocusFeedback(object? sender, RoutedEventArgs e) => MaterialInteraction.SetFocusFeedback(Root,
        MaterialInteraction.GetFocusFeedback(Root) == FocusFeedback.InsetRing ? FocusFeedback.Opacity : FocusFeedback.InsetRing);
    private void ToggleTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) => FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

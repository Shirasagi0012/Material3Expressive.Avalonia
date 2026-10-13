using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Catalog.Framework;

public partial class CatalogWindow : Window
{
    private readonly List<ListBox> _lists = [];
    private readonly List<Expander> _groups = [];
    private DemoDescriptor? _selected;
    private bool _selecting;

    internal IFrameSource? AnimationClock { get; }
    public CatalogWindow() : this(null) { }
    internal CatalogWindow(IFrameSource? clock)
    {
        AnimationClock = clock;
        AnimationScope.Initialize(this, clock);
        InitializeComponent();
        foreach (var page in DemoRegistry.Pages)
        {
            var list = new ListBox { ItemsSource = page.Demos.Select(demo => demo.Title).ToArray() };
            list.Classes.Add("catalog-nav");
            _lists.Add(list);
            var group = new Expander { Header = page.Title, Content = list,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            group.Classes.Add("nav-group");
            _groups.Add(group);
            Navigation.Children.Add(group);
            list.SelectionChanged += (_, _) =>
            {
                if (!_selecting && list.SelectedIndex >= 0) ShowDemo(page.Demos[list.SelectedIndex]);
            };
        }
        var themes = new MenuFlyout();
        foreach (var (title, variant) in new[] { ("跟随系统", ThemeVariant.Default), ("浅色", ThemeVariant.Light), ("深色", ThemeVariant.Dark) })
        {
            var item = new MenuItem { Header = title };
            item.Click += (_, _) => RequestedThemeVariant = variant;
            themes.Items.Add(item);
        }
        ThemeButton.Flyout = themes;
        ShowDemo(DemoRegistry.Pages[0].Demos[0]);
    }

    public DemoFrame? CurrentFrame => DemoHost.Content as DemoFrame;

    public void ShowDemo(DemoDescriptor descriptor)
    {
        if (_selected == descriptor) return;
        _selected = descriptor;
        _selecting = true;
        for (var i = 0; i < _lists.Count; i++)
        {
            _lists[i].SelectedIndex = DemoRegistry.Pages[i].Demos.ToList().IndexOf(descriptor);
            _groups[i].IsExpanded = _lists[i].SelectedIndex >= 0;
        }
        _selecting = false;
        SectionTitle.Text = descriptor.Page;
        DemoHost.Content = new DemoFrame(descriptor, DemoContext.Ambient);
        DemoScroll.Offset = default;
        var tools = new MenuFlyout();
        foreach (var action in descriptor.Actions)
        {
            var item = new MenuItem { Header = action.Name };
            item.Click += async (_, _) =>
            {
                var frame = CurrentFrame;
                if (frame is null) return;
                ToolsButton.IsEnabled = false;
                try { await frame.RunActionAsync(action); }
                catch (OperationCanceledException) { }
                catch (Exception error)
                {
                    var dialog = new Window { Title = "动作失败", Width = 520, Height = 220 };
                    AnimationScope.Initialize(dialog);
                    dialog.Content = new TextBox { Text = error.GetBaseException().Message, IsReadOnly = true,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(24) };
                    await dialog.ShowDialog(this);
                }
                finally { ToolsButton.IsEnabled = true; }
            };
            tools.Items.Add(item);
        }
        ToolsButton.Flyout = tools;
    }
}

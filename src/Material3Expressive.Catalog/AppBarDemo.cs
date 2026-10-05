using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using MaterialSymbols;
using System.Linq;

namespace Material3Expressive.Catalog;

public sealed class AppBarDemo : Window
{
    private readonly Scaffold _screen = new();
    private readonly AppBarScrollScope _scope = new();
    private readonly TopAppBarState _top = new();
    private readonly BottomAppBarState _bottom = new();
    private readonly ScrollViewer _bodyScroll = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
    };
    private readonly ComboBox _variant = new() { ItemsSource = new[] { "TopAppBar", "CenterAlignedTopAppBar", "MediumTopAppBar", "LargeTopAppBar", "MediumFlexibleTopAppBar", "LargeFlexibleTopAppBar", "TwoRowsTopAppBar" }, SelectedIndex = 4 };
    private readonly ComboBox _behavior = new() { ItemsSource = new[] { "pinned", "enterAlways", "exitUntilCollapsed" }, SelectedIndex = 2 };
    private readonly CheckBox _flexible = new() { Content = "Flexible bottom" };
    private readonly CheckBox _custom = new() { Content = "Custom bottom content" };
    private readonly CheckBox _rtl = new() { Content = "RTL" };
    private readonly CheckBox _nested = new() { Content = "Nested scrolling" };
    public AppBarDemo()
    {
        Title = "AppBar"; Width = 820; Height = 680;
        VisualAnimationScope.Initialize(this);
        _scope.Child = _screen;
        var body = new Border { Child = _bodyScroll };
        body.Bind(Border.WidthProperty, new Avalonia.Data.Binding("Bounds.Width") { Source = _screen });
        body.Bind(Border.HeightProperty, new Avalonia.Data.Binding("Bounds.Height") { Source = _screen });
        body.Bind(Border.PaddingProperty, _screen.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, _screen.GetObservable(Scaffold.ContentPaddingProperty));
        _screen.Content = body;
        var options = new WrapPanel { Margin = new Thickness(12), Children = { _variant, _behavior, _flexible, _custom, _rtl, _nested } };
        Content = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Children = { options, _scope } };
        Grid.SetRow(_scope, 1);
        _variant.SelectionChanged += (_, _) => UpdateBars(); _behavior.SelectionChanged += (_, _) => UpdateBars();
        _flexible.IsCheckedChanged += (_, _) => UpdateBottom(); _custom.IsCheckedChanged += (_, _) => UpdateBottom();
        _rtl.IsCheckedChanged += (_, _) => _scope.FlowDirection = _rtl.IsChecked == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        _nested.IsCheckedChanged += (_, _) => UpdateContent();
        UpdateContent();
        UpdateBars();
    }
    private void UpdateContent()
    {
        _scope.Interrupt(); _bodyScroll.Offset = default;
        _top.HeightOffset = _top.ContentOffset = 0;
        _bottom.HeightOffset = _bottom.ContentOffset = 0;
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        content.Children.Add(new TextBlock
        {
            Text = "Scroll the content or drag a bar. Actions and FAB remain ordinary controls.",
            TextWrapping = TextWrapping.Wrap,
        });
        // The default scene has one physical viewport, allowing the bars and body to share
        // compositor scrolling. Keep the existing nested-input demo available explicitly.
        if (_nested.IsChecked == true)
        {
            content.Children.Add(new ListBox { Height = 160,
                ItemsSource = Enumerable.Range(1, 60).Select(n => $"Embedded list item {n}") });
            content.Children.Add(new TextBox { Height = 120, AcceptsReturn = true,
                Text = string.Join('\n', Enumerable.Range(1, 40).Select(n => $"Embedded text line {n}")) });
        }
        for (var index = 1; index <= 10; index++)
            content.Children.Add(new Border
            {
                Height = 120, Padding = new Thickness(24), BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray,
                Child = new TextBlock { Text = $"Scrollable content {index:00}", VerticalAlignment = VerticalAlignment.Center },
            });
        _bodyScroll.Content = content;
    }
    private static IconButton Action(MaterialSymbolCodepoint symbol, string name)
    {
        var button = new IconButton { Symbol = symbol };
        Avalonia.Automation.AutomationProperties.SetName(button, name); return button;
    }
    private void UpdateBars()
    {
        _scope.Interrupt(); _top.HeightOffset = 0; _top.ContentOffset = 0;
        TopAppBar bar = _variant.SelectedIndex switch { 1 => new CenterAlignedTopAppBar(), 2 => new MediumTopAppBar(), 3 => new LargeTopAppBar(),
            4 => new MediumFlexibleTopAppBar(), 5 => new LargeFlexibleTopAppBar(), 6 => new TwoRowsTopAppBar(), _ => new TopAppBar() };
        bar.Title = "Material 3 Expressive";
        if (_variant.SelectedIndex >= 4) bar.Subtitle = "Independent expanded and collapsed titles";
        bar.NavigationIcon = Action(MaterialSymbolCodepoint.Menu, "Menu");
        bar.Items.Add(Action(MaterialSymbolCodepoint.Search, "Search")); bar.Items.Add(Action(MaterialSymbolCodepoint.MoreVert, "More actions"));
        bar.ScrollBehavior = _behavior.SelectedIndex switch { 0 => TopAppBarDefaults.PinnedScrollBehavior(_top, isScrollingContentAtStart: () => _bodyScroll.Offset.Y == 0),
            1 => TopAppBarDefaults.EnterAlwaysScrollBehavior(_top, isScrollingContentAtStart: () => _bodyScroll.Offset.Y == 0), _ => TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(_top) };
        _scope.TopScrollBehavior = bar.ScrollBehavior; _screen.TopBar = bar; UpdateBottom();
    }
    private void UpdateBottom()
    {
        _scope.Interrupt(); _bottom.HeightOffset = 0; _bottom.ContentOffset = 0;
        BottomAppBar bar = _flexible.IsChecked == true ? new FlexibleBottomAppBar() : new BottomAppBar();
        bar.Items.Add(Action(MaterialSymbolCodepoint.Favorite, "Favorite")); bar.Items.Add(Action(MaterialSymbolCodepoint.Search, "Search"));
        if (bar is not FlexibleBottomAppBar) bar.FloatingActionButton = new FloatingActionButton { Content = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Add, Width = 24, Height = 24 } };
        if (_custom.IsChecked == true) bar.CustomContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center,
            Children = { new TextBlock { Text = "Custom content replaces actions and FAB" }, new Button { Content = "Action" } } };
        bar.ScrollBehavior = BottomAppBarDefaults.ExitAlwaysScrollBehavior(_bottom);
        _scope.BottomScrollBehavior = bar.ScrollBehavior; _screen.BottomBar = bar;
    }
}

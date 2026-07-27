using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialSymbols;
using MaterialSymbols.Avalonia;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Static navigation surfaces and selected destinations.</summary>
internal static class NavigationCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        AppBars(VisualTheme.Light),
        AppBars(VisualTheme.Dark),
        TabsAndDestinations(VisualTheme.Light),
        TabsAndDestinations(VisualTheme.Dark),
        Drawers(VisualTheme.Light),
        Drawers(VisualTheme.Dark)
    ];

    private static VisualCase AppBars(VisualTheme theme) =>
        new(
            $"navigation/app-bar/all-sizes/{ThemeName(theme)}",
            new PixelSize(620, 470),
            theme,
            _ => new StackPanel
            {
                Width = 520,
                Spacing = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new MaterialAppBar
                    {
                        Size = AppBarSize.Small,
                        Title = "Inbox",
                        NavigationIcon = Symbol(MaterialSymbolCodepoint.ArrowBack),
                        Actions = Symbol(MaterialSymbolCodepoint.MoreVert)
                    },
                    new MaterialAppBar
                    {
                        Size = AppBarSize.Medium,
                        Title = "Settings",
                        NavigationIcon = Symbol(MaterialSymbolCodepoint.ArrowBack)
                    },
                    new MaterialAppBar
                    {
                        Size = AppBarSize.Large,
                        Title = "Photographs",
                        Subtitle = "384 items",
                        NavigationIcon = Symbol(MaterialSymbolCodepoint.ArrowBack),
                        IsScrolled = true
                    }
                }
            },
            ComparisonProfile.Exact,
            ["full", "navigation", "app-bar"]);

    private static MaterialSymbol Symbol(MaterialSymbolCodepoint codepoint) =>
        new() { Symbol = codepoint };

    private static VisualCase TabsAndDestinations(VisualTheme theme) =>
        new(
            $"navigation/tabs-bar-rail/selected/{ThemeName(theme)}",
            new PixelSize(760, 430),
            theme,
            _ =>
            {
                var primaryTabs = Tabs(TabVariant.Primary, 0, "Overview", "Details", "Reviews");
                var secondaryTabs = Tabs(TabVariant.Secondary, 1, "All", "Unread", "Flagged");
                var bar = Navigation(NavigationVariant.Bar, 0, "Home", "Saved", "Inbox");
                bar.Width = 430;
                bar.Height = 80;
                var rail = Navigation(NavigationVariant.Rail, 1, "Home", "Saved", "Inbox");
                rail.Width = 96;
                rail.Height = 280;

                return CaseLayout.Horizontal(
                    new StackPanel
                    {
                        Width = 520,
                        Spacing = 14,
                        Children = { primaryTabs, secondaryTabs, bar }
                    },
                    rail);
            },
            ComparisonProfile.Exact,
            ["smoke", "full", "navigation", "tabs"]);

    private static VisualCase Drawers(VisualTheme theme) =>
        new(
            $"navigation/drawer/all-variants/open/{ThemeName(theme)}",
            new PixelSize(820, 360),
            theme,
            context => CaseLayout.Horizontal(
                DrawerHost(DrawerVariant.Modal, context),
                DrawerHost(DrawerVariant.Standard, context)),
            ComparisonProfile.Shadow,
            ["full", "navigation", "drawer", "overlay"]);

    private static MaterialTabs Tabs(
        TabVariant variant,
        int selected,
        params string[] labels)
    {
        var tabs = new MaterialTabs
        {
            Variant = variant,
            SelectedIndex = selected
        };
        foreach (var label in labels)
            tabs.Items.Add(new MaterialTab { Content = label });
        return tabs;
    }

    private static MaterialNavigationBar Navigation(
        NavigationVariant variant,
        int selected,
        params string[] labels)
    {
        var navigation = new MaterialNavigationBar
        {
            Variant = variant,
            SelectedIndex = selected
        };
        foreach (var label in labels)
        {
            navigation.Items.Add(new MaterialNavigationItem
            {
                Icon = new Border { Width = 24, Height = 24 },
                Content = label
            });
        }
        return navigation;
    }

    private static Border DrawerHost(DrawerVariant variant, VisualCaseContext context)
    {
        var drawerContent = new StackPanel
        {
            Margin = new Thickness(12, 22),
            Spacing = 4,
            Children =
            {
                CaseLayout.Label(variant.ToString(), context, 15),
                Destination("Inbox", true),
                Destination("Starred", false),
                Destination("Archive", false)
            }
        };
        var host = new Grid
        {
            Width = 350,
            Height = 280,
            ClipToBounds = true,
            Children =
            {
                new Border
                {
                    Padding = new Thickness(20),
                    Child = CaseLayout.Label("Content behind drawer", context)
                },
                new MaterialNavigationDrawer
                {
                    Variant = variant,
                    IsOpen = true,
                    Content = drawerContent
                }
            }
        };
        return new Border
        {
            Width = 350,
            Height = 280,
            ClipToBounds = true,
            Child = host
        };
    }

    private static MaterialNavigationItem Destination(string label, bool selected) =>
        new()
        {
            Icon = new Border { Width = 24, Height = 24 },
            Content = label,
            IsChecked = selected
        };

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}

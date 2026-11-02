using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class AppBarDensityTokenTests
{
    [AvaloniaFact]
    public void TopTitleInsets_UpdateMeasuredAndArrangedTitle()
    {
        using var clock = new MotionTestClockScope();
        var title = new Border { Width = 20, Height = 10 };
        var bar = new TopAppBar { Title = title };
        var window = Show(bar);
        try
        {
            var row = bar.GetVisualDescendants().OfType<AppBarTopRow>().First();
            var titleLayout = row.Title;
            Assert.Equal(28, titleLayout.DesiredSize.Width);
            Assert.Equal(16, title.TranslatePoint(default, bar)!.Value.X);
            bar.Resources[MaterialTokens.Comp.AppBar.TitleStartInset] = 30d;
            bar.Resources[MaterialTokens.Comp.AppBar.TitleHorizontalPadding] = 6d;
            bar.Resources[MaterialTokens.Comp.AppBar.NavigationIconPadding] = new Thickness(10, 0, 0, 0);
            bar.Resources[MaterialTokens.Comp.AppBar.ActionPadding] = new Thickness(0, 0, 14, 0);
            window.UpdateLayout();
            Assert.Equal(32, titleLayout.DesiredSize.Width);
            Assert.Equal(36, title.TranslatePoint(default, bar)!.Value.X);
            Assert.Equal(10, row.Children[0].DesiredSize.Width);
            Assert.Equal(14, row.Children[2].DesiredSize.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("medium", 24)]
    [InlineData("mediumFlexible", 24)]
    [InlineData("large", 28)]
    [InlineData("largeFlexible", 28)]
    [InlineData("twoRows", 0)]
    public void TitleBottomTokens_PreserveVariantSelectionAndUpdatePlacement(string variant, double original)
    {
        using var clock = new MotionTestClockScope();
        TopAppBar bar = variant switch
        {
            "medium" => new MediumTopAppBar(),
            "mediumFlexible" => new MediumFlexibleTopAppBar(),
            "large" => new LargeTopAppBar(),
            "largeFlexible" => new LargeFlexibleTopAppBar(),
            _ => new TwoRowsTopAppBar()
        };
        bar.Title = "Title";
        bar.TitleTemplate = new FuncDataTemplate<string>((_, _) =>
        {
            var title = new Border { Width = 20, Height = 10 };
            TextBlock.SetBaselineOffset(title, 10);
            return title;
        });
        var window = Show(bar);
        try
        {
            var expanded = bar.GetVisualDescendants().OfType<AppBarTopRow>().Last();
            var before = expanded.Title.Bounds.Y;
            Assert.Equal(original, bar.TitleBottomPadding);
            bar.Resources[MaterialTokens.Comp.AppBar.MediumTitleBottomPadding] = 32d;
            bar.Resources[MaterialTokens.Comp.AppBar.LargeTitleBottomPadding] = 36d;
            window.UpdateLayout();
            Assert.Equal(original == 0 ? 0 : original + 8, bar.TitleBottomPadding);
            Assert.Equal(original == 0 ? before : before - 8, expanded.Title.Bounds.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BottomFabSpacing_UpdatesActionConstraintAndFabPlacement()
    {
        using var clock = new MotionTestClockScope();
        var bar = new BottomAppBar { FloatingActionButton = new Border { Width = 56, Height = 56 } };
        var window = Show(bar);
        try
        {
            var row = bar.GetVisualDescendants().OfType<AppBarBottomRow>().Single();
            var actions = row.Children[0];
            var fab = row.Children[1];
            var width = actions.Bounds.Width;
            var x = fab.Bounds.X;
            Assert.Equal(12, fab.Bounds.Y);
            bar.Resources[MaterialTokens.Comp.AppBar.BottomFabHorizontalSpacing] = 20d;
            bar.Resources[MaterialTokens.Comp.AppBar.BottomFabVerticalOffset] = 14d;
            window.UpdateLayout();
            Assert.Equal(width - 8, actions.Bounds.Width);
            Assert.Equal(x - 8, fab.Bounds.X);
            Assert.Equal(18, fab.Bounds.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ScaffoldFabSpacing_UpdatesHorizontalAndBottomOffsets()
    {
        using var clock = new MotionTestClockScope();
        var fab = new Border { Width = 56, Height = 56 };
        var scaffold = new Scaffold { FloatingActionButton = fab };
        var window = Show(scaffold);
        try
        {
            Assert.Equal(new Point(228, 168), fab.TranslatePoint(default, scaffold));
            scaffold.Resources[MaterialTokens.Comp.Scaffold.FabSpacing] = 24d;
            window.UpdateLayout();
            Assert.Equal(new Point(220, 160), fab.TranslatePoint(default, scaffold));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void FloatingToolbarFabTokens_UpdateSlotEndpointsAndReservedSize(bool horizontal, bool expanded)
    {
        using var clock = new MotionTestClockScope();
        FloatingToolbar toolbar = horizontal ? new HorizontalFloatingToolbar() : new VerticalFloatingToolbar();
        toolbar.Content = new Border { Width = 40, Height = 40 };
        toolbar.FloatingActionButton = new Border();
        toolbar.Expanded = expanded;
        toolbar.HorizontalAlignment = HorizontalAlignment.Center;
        toolbar.VerticalAlignment = VerticalAlignment.Center;
        var window = Show(toolbar);
        try
        {
            var layout = toolbar.GetVisualDescendants().OfType<FloatingToolbarLayout>().Single();
            var fab = layout.Children[1];
            var beforeMain = horizontal ? toolbar.Bounds.Width : toolbar.Bounds.Height;
            Assert.Equal(new Size(expanded ? 56 : 80, expanded ? 56 : 80), fab.Bounds.Size);
            Assert.Equal(80, horizontal ? toolbar.Bounds.Height : toolbar.Bounds.Width);
            toolbar.Resources[MaterialTokens.Comp.FloatingToolbar.ExpandedFabSize] = 48d;
            toolbar.Resources[MaterialTokens.Comp.FloatingToolbar.CollapsedFabSize] = 90d;
            toolbar.Resources[MaterialTokens.Comp.FloatingToolbar.FabBetweenSpace] = 12d;
            window.UpdateLayout();
            Assert.Equal(new Size(expanded ? 48 : 90, expanded ? 48 : 90), fab.Bounds.Size);
            Assert.Equal(beforeMain - 4, horizontal ? toolbar.Bounds.Width : toolbar.Bounds.Height);
            Assert.Equal(90, horizontal ? toolbar.Bounds.Height : toolbar.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OrdinaryToolbar_DoesNotConsumeFabSlotsOrReplaceCallerPadding()
    {
        using var clock = new MotionTestClockScope();
        var toolbar = new HorizontalFloatingToolbar
        {
            Content = new Border { Width = 40, Height = 40 },
            Padding = new Thickness(3, 5),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var window = Show(toolbar);
        try
        {
            var before = toolbar.Bounds.Size;
            toolbar.Resources[MaterialTokens.Comp.FloatingToolbar.ExpandedFabSize] = 120d;
            toolbar.Resources[MaterialTokens.Comp.FloatingToolbar.CollapsedFabSize] = 160d;
            toolbar.Resources[MaterialTokens.Comp.FloatingToolbar.FabBetweenSpace] = 40d;
            window.UpdateLayout();
            Assert.Equal(before, toolbar.Bounds.Size);
            Assert.Equal(new Thickness(3, 5), toolbar.Padding);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } };
        window.Show();
        window.UpdateLayout();
        return window;
    }
}

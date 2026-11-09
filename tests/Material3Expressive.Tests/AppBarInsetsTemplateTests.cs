using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarInsetsTemplateTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void ConsumptionProjectionPreservesRealizedTemplateRootAcrossMeasure(bool twoRows)
    {
        using var clock = new MotionTestClockScope();
        MaterialWindowInsetsPadding? title = null, navigation = null;
        var titleCreations = 0; var navigationCreations = 0;
        TopAppBar bar = twoRows ? new MediumFlexibleTopAppBar() : new TopAppBar();
        bar.WindowInsets = new Thickness(13, 7, 5, 11);
        bar.Title = "title"; bar.CollapsedTitle = new Border { Width = 20, Height = 10 };
        bar.TitleTemplate = new FuncDataTemplate<string>((_, _) =>
        {
            titleCreations++;
            return title = new MaterialWindowInsetsPadding { Insets = new Thickness(30), Child = new Border { Width = 20, Height = 10 } };
        });
        bar.NavigationIcon = "navigation";
        bar.NavigationIconTemplate = new FuncDataTemplate<string>((_, _) =>
        {
            navigationCreations++;
            return navigation = new MaterialWindowInsetsPadding { Insets = new Thickness(30), Child = new Border { Width = 20, Height = 20 } };
        });
        var window = new MotionTestWindow { Width = 400, Height = 200 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme()); window.Content = bar; window.Show(); Flush(window);
        MaterialWindowInsetsPadding PresentedTitle() => (MaterialWindowInsetsPadding)bar.GetVisualDescendants()
            .OfType<AppBarTopRow>().Single(row => row.Lower == twoRows).Title.Children.OfType<ContentPresenter>().First().Child!;
        try
        {
            var originalTitle = PresentedTitle();
            var originalNavigation = Assert.IsType<MaterialWindowInsetsPadding>(navigation);
            var originalTitleCreations = titleCreations; var originalNavigationCreations = navigationCreations;
            for (var i = 0; i < 3; i++) { bar.InvalidateMeasure(); Flush(window); }
            bar.WindowInsets = new Thickness(15, 9, 7, 13); Flush(window);
            title = PresentedTitle();
            Assert.Same(originalTitle, title); Assert.Same(originalNavigation, navigation);
            Assert.Equal(originalTitleCreations, titleCreations); Assert.Equal(originalNavigationCreations, navigationCreations);
            Assert.Equal(twoRows ? new Thickness(15, 30, 23, 30) : new Thickness(15, 21, 23, 17), title!.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(new Thickness(15, 21, 23, 17), navigation!.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.True(title.Bounds.Width > 0); Assert.True(title.Child!.Bounds.Height > 0);
            bar.TitleTemplate = new FuncDataTemplate<string>((_, _) => title = new MaterialWindowInsetsPadding
                { Insets = new Thickness(30), Child = new Border { Width = 31, Height = 12 } });
            Flush(window);
            title = PresentedTitle();
            Assert.NotSame(originalTitle, title);
            Assert.Equal(twoRows ? new Thickness(15, 30, 23, 30) : new Thickness(15, 21, 23, 17), title!.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
    }

    private static void Flush(Window window)
    { for (var i = 0; i < 3; i++) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); } }
}

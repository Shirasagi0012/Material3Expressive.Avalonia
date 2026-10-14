using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationSelectionAnimationTests
{
    [AvaloniaFact]
    public void BarAndRail_KeepMatchingSelectionTracks_DuringReversalAndDisable()
    {
        using var clock = new MotionTestClockScope();
        var barItem = new NavigationBarItem { Label = "Bar", Icon = new Border { Width = 24, Height = 24 } };
        var railItem = new NavigationRailItem { Label = "Rail", Icon = new Border { Width = 24, Height = 24 } };
        var window = new MotionTestWindow
        {
            Width = 300,
            Height = 240
        };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new StackPanel { Children = { barItem, railItem } };

        try
        {
            window.Show();
            window.UpdateLayout();
            var barLayout = barItem.GetVisualDescendants().OfType<NavigationBarItemLayout>().Single();
            var railLayout = railItem.GetVisualDescendants().OfType<NavigationRailItemLayout>().Single();

            AssertMatchingFrames(1);
            barItem.IsSelected = railItem.IsSelected = true;
            AssertMatchingFrames(5);
            Assert.InRange(barLayout.AlphaProgress, 0.001f, 0.999f);

            // Reverse before settling, then change only color targets while selection is running.
            barItem.IsSelected = railItem.IsSelected = false;
            AssertMatchingFrames(3);
            barItem.IsEnabled = railItem.IsEnabled = false;
            AssertMatchingFrames(100);
            Assert.Equal(0, barLayout.SizeProgress);
            Assert.Equal(0, barLayout.AlphaProgress);
            Assert.Equal(barItem.DisabledIconColor, ColorOf(barItem, "PART_Icon"));
            Assert.Equal(barItem.DisabledTextColor, ColorOf(barItem, "PART_Label"));

            barItem.IsEnabled = railItem.IsEnabled = true;
            barItem.IsSelected = railItem.IsSelected = true;
            AssertMatchingFrames(100);
            Assert.Equal(1, barLayout.SizeProgress);
            Assert.Equal(1, barLayout.AlphaProgress);
            Assert.Equal(barItem.SelectedIconColor, ColorOf(barItem, "PART_Icon"));
            Assert.Equal(barItem.SelectedTextColor, ColorOf(barItem, "PART_Label"));
            Assert.Null(window.MotionRoot.Diagnostics.LastError);

            void AssertMatchingFrames(int count)
            {
                for (var frame = 0; frame < count; frame++)
                {
                    clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                    window.UpdateLayout();
                    Assert.Equal(barLayout.AlphaProgress, railLayout.AlphaProgress);
                    Assert.Equal(barLayout.SizeProgress, railLayout.SizeProgress);
                    Assert.Equal(ColorOf(barItem, "PART_Icon"), ColorOf(railItem, "PART_Icon"));
                    Assert.Equal(ColorOf(barItem, "PART_Label"), ColorOf(railItem, "PART_Label"));
                }
            }
        }
        finally
        {
            window.Close();
        }

        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static Color ColorOf(Control item, string partName)
    {
        var presenter = item.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Single(control => control.Name == partName);
        return Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Foreground).Color;
    }
}

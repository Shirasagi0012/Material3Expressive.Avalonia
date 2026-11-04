using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class TimeScrollTests
{
    [AvaloniaTheory]
    [InlineData(false, 0, 12)]
    [InlineData(false, 12, 12)]
    [InlineData(true, 0, 0)]
    [InlineData(true, 23, 23)]
    public void InitialItemsUseDisplayTypographyAndScrollFieldPalette(bool clock24, int hour, int display)
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimeScroll { Is24Hour = clock24, SelectedTime = new(hour, 30) };
        var window = Show(picker);
        var panel = Hour(picker);
        Assert.Equal((double)(200f / 3f), panel.ItemHeight);
        Assert.Equal(display, panel.SelectedValue);
        Assert.All(panel.Children.Cast<ListBoxItem>(), item => {
            Assert.Equal(item.IsSelected ? 57 : 45, item.FontSize);
            Assert.Equal(item.IsSelected ? FontWeight.Medium : FontWeight.Normal, item.FontWeight);
            Assert.Equal(item.IsSelected ? panel.SelectedColor : panel.UnselectedColor, ((ISolidColorBrush)item.Foreground!).Color);
            Assert.NotEqual(Brushes.Transparent, item.Foreground);
        });
        Assert.Equal(new TimeOnly(hour, 30), picker.SelectedTime);
        window.Close();
    }

    [AvaloniaFact]
    public void AdjacentSelectionAndReversalKeepMotionWithEachPageInsteadOfEachContainer()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimeScroll { Is24Hour = true, SelectedTime = new(8, 30) };
        var window = Show(picker);
        var panel = Hour(picker);
        clock.SendFrame(TimeSpan.Zero);
        picker.SelectedTime = new(9, 30); Flush(window);
        Assert.Equal(57, Item(panel, 8).FontSize);
        Assert.Equal(45, Item(panel, 9).FontSize);
        for (var ms = 16; ms <= 64; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
        var oldSize = Item(panel, 8).FontSize;
        var newSize = Item(panel, 9).FontSize;
        Assert.InRange(oldSize, 45.01, 56.99);
        Assert.InRange(newSize, 45.01, 56.99);
        picker.SelectedTime = new(8, 30); Flush(window);
        Assert.Equal(oldSize, Item(panel, 8).FontSize);
        Assert.Equal(newSize, Item(panel, 9).FontSize);
        for (var ms = 80; ms <= 800; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
        Assert.Equal(57, Item(panel, 8).FontSize);
        Assert.Equal(45, Item(panel, 9).FontSize);
        window.Close();
    }

    [AvaloniaFact]
    public void NativeMinuteWheelVisitsEveryMinuteInBothDirectionsAndPreservesHour()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimeScroll { Is24Hour = true, SelectedTime = new(23, 0) };
        var window = Show(picker);
        var panel = picker.GetVisualDescendants().OfType<TimeScrollPanel>().Single(p => p.Name == "PART_MinuteScroll");
        for (var step = 1; step <= 60; step++)
        {
            panel.ScrollDown(); Flush(window);
            Assert.Equal(new TimeOnly(23, step % 60), picker.SelectedTime);
        }
        for (var step = 1; step <= 60; step++)
        {
            panel.ScrollUp(); Flush(window);
            Assert.Equal(new TimeOnly(23, (60 - step) % 60), picker.SelectedTime);
        }
        window.Close();
    }

    [AvaloniaFact]
    public void ProgrammaticTimeChangesKeepAllMinuteValuesAfterResourceRemeasure()
    {
        var picker = new TimeScroll { Is24Hour = true };
        var window = Show(picker);
        foreach (var height in new[] { (double)(200f / 3f), 72d, 67.25d })
        {
            picker.Resources[Comp.TimeScroll.ItemHeight] = height; Flush(window);
            for (var minute = 0; minute < 60; minute++)
            {
                picker.SelectedTime = new(23, minute); Flush(window);
                Assert.Equal(new TimeOnly(23, minute), picker.SelectedTime);
                Assert.Equal(minute, picker.GetVisualDescendants().OfType<TimeScrollPanel>().Single(p => p.Name == "PART_MinuteScroll").SelectedValue);
            }
        }
        window.Close();
    }

    [AvaloniaFact]
    public void NativeLoopBoundaryKeepsTheOutgoingPageAnimation()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimeScroll { Is24Hour = true, SelectedTime = new(0, 30) };
        var window = Show(picker);
        var panel = Hour(picker);
        clock.SendFrame(TimeSpan.Zero);
        panel.ScrollUp(); Flush(window);
        Assert.Equal(23, picker.SelectedTime.Hour);
        Assert.Equal(57, Item(panel, 0).FontSize);
        Assert.Equal(45, Item(panel, 23).FontSize);
        for (var ms = 16; ms <= 64; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
        var zero = Item(panel, 0).FontSize;
        Assert.InRange(zero, 45.01, 56.99);
        panel.ScrollDown(); Flush(window);
        Assert.Equal(0, picker.SelectedTime.Hour);
        Assert.Equal(zero, Item(panel, 0).FontSize);
        window.Close();
    }

    [AvaloniaFact]
    public void RoleAndColorResourcesRetargetExistingPagesAndDetachReleasesAnimationContributions()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimeScroll { Is24Hour = true, SelectedTime = new(8, 30) };
        var window = Show(picker);
        var panel = Hour(picker);
        var selected = Item(panel, 8);
        selected.FontSize = 31; // The animation contribution must leave this underlying value intact.
        Assert.Equal(57, selected.FontSize);
        picker.Resources[Sys.TypeScale.DisplayLargeEmphasized.Style] = new TextStyle {
            FontSize = 60, FontWeight = FontWeight.Bold, LineHeight = 68, LetterSpacing = 1
        };
        picker.Resources[Sys.Color.OnSurface] = Colors.Red;
        Flush(window);
        Assert.Same(panel, Hour(picker));
        Assert.Equal(60, selected.FontSize);
        Assert.Equal(FontWeight.Bold, selected.FontWeight);
        clock.SendFrame(TimeSpan.Zero);
        for (var ms = 16; ms <= 800; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
        Assert.Equal(Colors.Red, ((ISolidColorBrush)selected.Foreground!).Color);
        picker.SelectedTime = new(9, 30); Flush(window);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        window.Content = null; Flush(window);
        Assert.Equal(31, selected.FontSize);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Content = picker; Flush(window);
        Assert.Equal(60, Item(Hour(picker), 9).FontSize);
        window.Close();
    }

    [AvaloniaFact]
    public void ItemHeightAndContainerShapeResourcesPreserveSelectedTime()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimeScroll { Is24Hour = false, SelectedTime = new(20, 47) };
        var window = Show(picker);
        picker.Resources[Comp.TimeScroll.ItemHeight] = 72d;
        picker.Resources[Sys.Shape.Large] = new CornerRadius(22);
        Flush(window);
        Assert.Equal(new TimeOnly(20, 47), picker.SelectedTime);
        Assert.All(picker.GetVisualDescendants().OfType<TimeScrollPanel>(), panel => Assert.Equal(72, panel.ItemHeight));
        Assert.All(picker.GetVisualDescendants().OfType<MaterialSurface>().Where(s => s.Classes.Contains("time-scroll-surface")),
            surface => Assert.Equal(new CornerRadius(22), surface.CornerRadius));
        window.Close();
    }

    [AvaloniaFact]
    public void MissingMotionWaitsForConfigurationAndDoesNotReplaceNativeWheelSelection()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimeScroll { Is24Hour = true, SelectedTime = new(8, 30) };
        var window = Show(picker);
        var panel = Hour(picker);
        var spec = panel.SelectionAnimation;
        panel.SelectionAnimation = null;
        panel.ScrollDown(); Flush(window);
        Assert.Equal(9, picker.SelectedTime.Hour);
        for (var ms = 0; ms <= 160; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
        Assert.Equal(45, Item(panel, 9).FontSize);
        panel.SelectionAnimation = spec;
        for (var ms = 176; ms <= 976; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
        Assert.Equal(57, Item(panel, 9).FontSize);
        window.Close();
    }

    private static TimeScrollPanel Hour(TimeScroll picker) => picker.GetVisualDescendants().OfType<TimeScrollPanel>().Single(p => p.Name == "PART_HourScroll");
    private static ListBoxItem Item(TimeScrollPanel panel, int value) => panel.Children.Cast<ListBoxItem>().Single(i => Equals(i.Tag, value));
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 700, Height = 500 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        control.HorizontalAlignment = HorizontalAlignment.Left; control.VerticalAlignment = VerticalAlignment.Top;
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}

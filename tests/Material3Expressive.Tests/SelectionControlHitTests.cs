using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Xunit;

namespace Material3Expressive.Tests;

public class SelectionControlHitTests
{
    [AvaloniaTheory]
    [InlineData(false, false, FlowDirection.LeftToRight)]
    [InlineData(false, false, FlowDirection.RightToLeft)]
    [InlineData(false, true, FlowDirection.LeftToRight)]
    [InlineData(false, true, FlowDirection.RightToLeft)]
    [InlineData(true, false, FlowDirection.LeftToRight)]
    [InlineData(true, false, FlowDirection.RightToLeft)]
    [InlineData(true, true, FlowDirection.LeftToRight)]
    [InlineData(true, true, FlowDirection.RightToLeft)]
    public void ContentRow_ClicksIncludeIndicatorSlotGapAndPadding(bool radio, bool template, FlowDirection direction)
    {
        using var clock = new MotionTestClockScope();
        ToggleButton control = radio ? new RadioButton() : new CheckBox();
        control.Content = "Option";
        if (template)
            control.ContentTemplate = new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text });
        control.FlowDirection = direction;
        control.Width = 200;
        control.Padding = new Thickness(8, 4, 12, 6);
        var window = Show(control);
        var clicks = 0;
        control.Click += (_, _) => clicks++;

        // The indicator surface occupies x=12..36. Content starts at x=56 in LTR.
        // Exercise empty indicator space, both parts of the gap, and trailing content padding.
        Point[] points = [new(2, 2), new(44, 24), new(52, 24), new(198, 46)];
        foreach (var local in points)
        {
            var mirrored = direction == FlowDirection.LeftToRight ? local : new Point(control.Bounds.Width - local.X, local.Y);
            control.IsChecked = false;
            var before = clicks;
            Click(mirrored);
            Assert.True(control.IsChecked);
            Assert.Equal(before + 1, clicks);

            control.IsEnabled = false;
            Click(mirrored);
            Assert.Equal(before + 1, clicks);
            control.IsEnabled = true;
        }

        // A press in the gap still follows native cancellation and release-outside behavior.
        control.IsChecked = false;
        var gapX = direction == FlowDirection.LeftToRight ? 52 : control.Bounds.Width - 52;
        var gap = control.TranslatePoint(new Point(gapX, 24), window)!.Value;
        window.MouseDown(gap, MouseButton.Left);
        window.MouseMove(new Point(1, 1));
        window.MouseUp(new Point(1, 1), MouseButton.Left);
        Assert.False(control.IsChecked);
        Assert.Equal(points.Length, clicks);
        window.Close();

        void Click(Point local)
        {
            var point = control.TranslatePoint(local, window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentChanges_UpdateRowHitAreaWithoutExpandingTheStandaloneIndicator(bool radio)
    {
        using var clock = new MotionTestClockScope();
        ToggleButton control = radio ? new RadioButton() : new CheckBox();
        var window = Show(control);
        var clicks = 0;
        control.Click += (_, _) => clicks++;

        ClickCorner();
        Assert.Equal(0, clicks);
        control.Content = "Option";
        window.UpdateLayout();
        ClickCorner();
        Assert.Equal(1, clicks);
        control.Content = null;
        window.UpdateLayout();
        ClickCorner();
        Assert.Equal(1, clicks);
        window.Close();

        void ClickCorner()
        {
            var point = control.TranslatePoint(new Point(2, 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
        }
    }

    static MotionTestWindow Show(ToggleButton control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 300, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } };
        window.Show();
        window.UpdateLayout();
        return window;
    }
}

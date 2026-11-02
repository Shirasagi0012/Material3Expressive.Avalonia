using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using Xunit;
using DatePicker = Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Tests;

public class DateDensityTokenTests
{
    [AvaloniaFact]
    public void GridDefinitionsFollowTokensIndependentlyOfButtonOverrides()
    {
        var picker = new DatePicker { MinYear = 2024, MaxYear = 2024 };
        var size = new MaterialTokenKey<double>("test.date-mode-size");
        picker.Resources[MaterialTokens.Comp.DatePickerModal.ModeToggleSize] = new TokenRef(size);
        picker.Resources[size] = 48d;
        var window = Show(picker);
        try
        {
            var toggle = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_ModeToggle");
            var column = Assert.IsType<Grid>(toggle.Parent).ColumnDefinitions[1];
            var style = new Style(selector => selector.OfType<Button>().Class("custom-mode")) {
                Setters = { new Setter(Layoutable.MinWidthProperty, 90d) }
            };
            toggle.Styles.Add(style);
            toggle.Classes.Add("custom-mode");
            Flush(window);
            Assert.Equal(90, toggle.MinWidth);
            Assert.Equal(48, column.Width.Value);
            toggle.MinWidth = 100;
            picker.Resources[size] = 60d;
            Flush(window);
            Assert.Equal(100, toggle.MinWidth);
            Assert.Equal(60, column.Width.Value);
            Assert.Equal(60, toggle.Height);

            var arrow = picker.GetVisualDescendants().OfType<IconButton>().First();
            var arrowColumn = Assert.IsType<Grid>(arrow.Parent).ColumnDefinitions[Grid.GetColumn(arrow)];
            arrow.Width = 80;
            Flush(window);
            Assert.Equal(80, arrow.Width);
            Assert.Equal(48, arrowColumn.Width.Value);
            picker.Resources[MaterialTokens.Comp.DatePickerModal.NavigationButtonSize] = 44d;
            Flush(window);
            Assert.Equal(44, arrow.Width);
            Assert.Equal(44, arrowColumn.Width.Value);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InputTokenAliasUpdatesExistingFieldAndPreservesDraftAndSelection()
    {
        var picker = new DatePicker {
            DisplayMode = DatePickerDisplayMode.Input,
            Culture = CultureInfo.GetCultureInfo("en-US")
        };
        var width = new MaterialTokenKey<double>("test.date-input-width");
        picker.Resources[MaterialTokens.Comp.DatePickerModal.SingleInputWidth] = new TokenRef(width);
        picker.Resources[width] = 280d;
        var window = Show(picker);
        try
        {
            TextBox input = picker.GetVisualDescendants().OfType<TextBox>().Single(b => b.Name == "PART_DateInput");
            Assert.Equal(280d, input.Width);
            input.Text = "022";
            input.SelectionStart = 1;
            input.SelectionEnd = 2;
            string? draft = input.Text;
            int caret = input.CaretIndex;
            picker.Resources[width] = 264d;
            picker.Resources[MaterialTokens.Comp.DatePickerModal.InputMargin] = new Thickness(20, 18, 20, 14);
            Flush(window);

            Assert.Same(input, picker.GetVisualDescendants().OfType<TextBox>().Single(b => b.Name == "PART_DateInput"));
            Assert.Equal(264d, input.Width);
            Assert.Equal(draft, input.Text);
            Assert.Equal(caret, input.CaretIndex);
            Assert.Equal(1, input.SelectionStart);
            Assert.Equal(2, input.SelectionEnd);
            Assert.Null(picker.SelectedDate);
            Assert.Equal(new Thickness(20, 18, 20, 14), Assert.IsType<StackPanel>(input.Parent).Margin);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DayGridKeepsSevenColumnsSixRowsAndUsesIndependentCellDimensions()
    {
        var panel = new DateMonthPanel(new DateOnly(2024, 9, 1), CultureInfo.GetCultureInfo("en-US"),
            _ => true, _ => { }, _ => { });
        panel.Measure(Size.Infinity);
        Assert.Equal(new Size(336, 288), panel.DesiredSize);
        panel.DayCellWidth = 60;
        panel.DayCellHeight = 56;
        panel.Measure(Size.Infinity);
        Assert.Equal(new Size(420, 336), panel.DesiredSize);
        panel.Arrange(new Rect(0, 0, 436, 336));
        Button[] days = panel.GetVisualChildren().OfType<Button>().ToArray();
        Assert.Equal(new Rect(2, 0, 60, 56), days[0].Bounds);
        Assert.Equal(new Rect(374, 0, 60, 56), days[6].Bounds);
        Assert.Equal(new Rect(2, 56, 60, 56), days[7].Bounds);
        panel.RangeBrush = Brushes.Red;
        panel.RangeIndicatorHeight = 38;
        panel.RangeIndicatorTopOffset = 7;
        panel.UpdateSelection(new DateRange(new DateOnly(2024, 9, 2), new DateOnly(2024, 9, 10)));
        var drawing = new DrawingGroup();
        using (DrawingContext context = drawing.Open()) panel.Render(context);
        GeometryDrawing[] ranges = drawing.Children.OfType<GeometryDrawing>()
            .Where(d => d.Brush == Brushes.Red).ToArray();
        Assert.Equal(2, ranges.Length);
        Assert.Equal(new Rect(94, 7, 342, 38), ranges[0].Geometry!.Bounds);
        Assert.Equal(new Rect(2, 63, 154, 38), ranges[1].Geometry!.Bounds);
    }

    [AvaloniaFact]
    public void CalendarOverridesUpdateExistingGridNavigationAndWeekdayRow()
    {
        var picker = new DatePicker { MinYear = 2024, MaxYear = 2024, DisplayedMonth = new DateOnly(2024, 9, 1) };
        var window = Show(picker);
        try
        {
            DateMonthPanel panel = picker.GetVisualDescendants().OfType<DateMonthPanel>().Single();
            picker.Resources[MaterialTokens.Comp.DatePickerModal.DayCellWidth] = 50d;
            picker.Resources[MaterialTokens.Comp.DatePickerModal.DayCellHeight] = 54d;
            picker.Resources[MaterialTokens.Comp.DatePickerModal.NavigationButtonSize] = 44d;
            picker.Resources[MaterialTokens.Comp.DatePickerModal.WeekdayRowHeight] = 42d;
            picker.Resources[MaterialTokens.Comp.DatePickerModal.ModeToggleSize] = 46d;
            Flush(window);
            Assert.Same(panel, picker.GetVisualDescendants().OfType<DateMonthPanel>().Single());
            Assert.Equal(324d, panel.Bounds.Height);
            Assert.All(panel.GetVisualChildren().OfType<Button>(), day => {
                Assert.Equal(50d, day.Bounds.Width);
                Assert.Equal(54d, day.Bounds.Height);
            });
            Assert.Equal(42d, picker.GetVisualDescendants().OfType<UniformGrid>().Single(g => g.Columns == 7).Height);
            Assert.All(picker.GetVisualDescendants().OfType<IconButton>(), arrow => {
                Assert.Equal(44d, arrow.Width);
                Assert.Equal(44d, Assert.IsType<Grid>(arrow.Parent).ColumnDefinitions[Grid.GetColumn(arrow)].Width.Value);
            });
            Button toggle = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_ModeToggle");
            Assert.Equal(46d, toggle.Height);
            Assert.Equal(46d, Assert.IsType<Grid>(toggle.Parent).ColumnDefinitions[1].Width.Value);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 700, Height = 700 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        window.Content = control;
        window.Show();
        Flush(window);
        return window;
    }

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}

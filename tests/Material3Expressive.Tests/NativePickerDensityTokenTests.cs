using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class NativePickerDensityTokenTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void PresenterLiveTokensReachRowsWheelsAndRepeatButtons(bool time)
    {
        Control presenter = time ? new TimePickerPresenter { UseSeconds = true } : new DatePickerPresenter();
        var height = new MaterialTokenKey<double>("test.native.action-height");
        presenter.Resources[MaterialTokens.Comp.NativePicker.ActionRowHeight] = new TokenRef(height);
        presenter.Resources[height] = 64d;
        var window = Show(presenter);
        try
        {
            var root = presenter.GetVisualDescendants().OfType<Components.NativePickerPresenterGrid>().Single();
            var wheels = presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().ToArray();
            Assert.NotEmpty(wheels);
            presenter.Resources[height] = 72d;
            presenter.Resources[MaterialTokens.Comp.NativePicker.WheelItemHeight] = 52d;
            presenter.Resources[MaterialTokens.Comp.NativePicker.WheelColumnSpacing] = 10d;
            presenter.Resources[MaterialTokens.Comp.NativePicker.RepeatButtonHeight] = 28d;
            presenter.Resources[MaterialTokens.Comp.NativePicker.RepeatButtonMinimumHeight] = 26d;
            Flush(window);
            Assert.Same(root, presenter.GetVisualDescendants().OfType<Components.NativePickerPresenterGrid>().Single());
            Assert.Equal(72d, root.RowDefinitions[1].Height.Value);
            Assert.All(wheels, wheel => Assert.Equal(52d, wheel.ItemHeight));
            Assert.All(presenter.GetVisualDescendants().OfType<Border>().Where(b => b.Name?.EndsWith("Spacer") == true),
                spacer => Assert.Equal(10d, spacer.Width));
            Assert.All(presenter.GetVisualDescendants().OfType<RepeatButton>(), button => {
                Assert.Equal(28d, button.Height);
                Assert.Equal(26d, button.MinHeight);
            });
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CalendarLiveTokensKeepSixDateRowsAndDerivedButtonThemeOverrides()
    {
        var calendar = new Calendar { DisplayDate = new DateTime(2024, 2, 1) };
        var window = Show(calendar, "MaterialNativeCalendarTheme");
        try
        {
            var month = calendar.GetVisualDescendants().OfType<Components.NativeCalendarMonthGrid>().Single();
            calendar.Resources[MaterialTokens.Comp.NativeCalendar.WeekdayRowHeight] = 44d;
            calendar.Resources[MaterialTokens.Comp.NativeCalendar.DayRowHeight] = 54d;
            calendar.Resources[MaterialTokens.Comp.NativeCalendar.NavigationColumnWidth] = 60d;
            calendar.Resources[MaterialTokens.Comp.NativeCalendar.HeaderHeight] = 56d;
            calendar.Resources[MaterialTokens.Comp.NativePicker.FieldMinimumHeight] = 80d;
            calendar.Resources[MaterialTokens.Comp.NativePicker.FieldPadding] = new Thickness(30);
            Flush(window);
            Assert.Equal(7, month.RowDefinitions.Count);
            Assert.Equal(7, month.ColumnDefinitions.Count);
            Assert.Equal(44d, month.RowDefinitions[0].Height.Value);
            Assert.Equal(368d, month.Bounds.Height);
            Assert.All(month.RowDefinitions.Skip(1), row => Assert.Equal(54d, row.Height.Value));
            var header = calendar.GetVisualDescendants().OfType<Components.NativeCalendarHeaderGrid>().Single();
            Assert.Equal(60d, header.ColumnDefinitions[0].Width.Value);
            Assert.Equal(60d, header.ColumnDefinitions[2].Width.Value);
            Assert.Equal(56d, calendar.GetVisualDescendants().OfType<Components.NativeCalendarItemGrid>().Single().RowDefinitions[0].Height.Value);
            Assert.All(calendar.GetVisualDescendants().OfType<CalendarDayButton>(), day => {
                Assert.Equal(40d, day.MinHeight);
                Assert.Equal(new Thickness(0), day.Padding);
            });
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeFieldTokensPreserveLocalMinimumWidthAndPadding()
    {
        var picker = new Avalonia.Controls.DatePicker { MinWidth = 350 };
        var window = Show(picker);
        try
        {
            var field = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_FlyoutButton");
            field.Padding = new Thickness(7);
            picker.Resources[MaterialTokens.Comp.NativeDatePicker.MinimumWidth] = 400d;
            picker.Resources[MaterialTokens.Comp.NativePicker.FieldPadding] = new Thickness(20);
            picker.Resources[MaterialTokens.Comp.NativePicker.FieldMinimumHeight] = 62d;
            Flush(window);
            Assert.Equal(350d, picker.MinWidth);
            Assert.Equal(new Thickness(7), field.Padding);
            Assert.Equal(62d, field.MinHeight);
            field.ClearValue(Avalonia.Controls.Primitives.TemplatedControl.PaddingProperty);
            picker.ClearValue(Layoutable.MinWidthProperty);
            Flush(window);
            Assert.Equal(400d, picker.MinWidth);
            Assert.Equal(new Thickness(20), field.Padding);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control control, string? theme = null)
    {
        var window = new MotionTestWindow { Width = 700, Height = 700 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        window.Content = control;
        if (theme is not null)
            control.Theme = (ControlTheme)window.FindResource(theme)!;
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

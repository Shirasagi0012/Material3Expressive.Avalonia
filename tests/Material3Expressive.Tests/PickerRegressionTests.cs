using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using Xunit;
using DatePicker = Material3Expressive.Components.DatePicker;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class PickerRegressionTests
{
    [AvaloniaTheory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    public void TimeInput_RendersDigitsAtTheCenterWhileEditing(bool vibrant, bool toggle, bool is24Hour)
    {
        var picker = new TimeInput { SelectedTime = new(6, 48), Is24Hour = is24Hour,
            Toggle = toggle ? new Button { Content = "Input" } : null,
            Variant = vibrant ? TimePickerVariant.Vibrant : TimePickerVariant.Standard };
        var window = Show(picker);
        try
        {
            window.RequestedThemeVariant = ThemeVariant.Dark;
            foreach (var field in picker.GetVisualDescendants().OfType<TextBox>())
            {
                // Isolate text ink from selection, caret and outline pixels.
                field.Foreground = Brushes.White;
                field.BorderBrush = field.CaretBrush = field.SelectionBrush = Brushes.Transparent;
                foreach (var focused in new[] { false, true })
                {
                    if (focused)
                    {
                        field.Focus();
                        field.SelectionStart = field.SelectionEnd = field.CaretIndex = 2;
                        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.None, null);
                    }
                    Draw(window);
                    using var frame = window.CaptureRenderedFrame()!;
                    var origin = field.TranslatePoint(default, window)!.Value;
                    var left = double.PositiveInfinity;
                    var right = double.NegativeInfinity;
                    for (var y = 4; y < field.Bounds.Height - 4; y++)
                    for (var x = 4; x < field.Bounds.Width - 4; x++)
                    {
                        var color = PixelAt(frame, origin + new Vector(x, y));
                        if (color.R > 240 && color.G > 240 && color.B > 240)
                        { left = Math.Min(left, x); right = Math.Max(right, x); }
                    }
                    Assert.True(double.IsFinite(left));
                    Assert.True(Math.Abs((left + right + 1) / 2 - field.Bounds.Width / 2) < 2,
                        $"{field.Name} (focused={focused}): rendered digits span {left}..{right} in a {field.Bounds.Width} DIP field.");
                }
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TimeInput_UsesItsTypographyRoleInTheNativeEditor(bool vibrant)
    {
        var picker = new TimeInput { SelectedTime = new(4, 9),
            Variant = vibrant ? TimePickerVariant.Vibrant : TimePickerVariant.Standard };
        var window = Show(picker);
        try
        {
            var role = vibrant ? TypographyRole.DisplayLarge : TypographyRole.DisplayMedium;
            var expected = Typography.GetStyle(picker, role);
            var hour = picker.GetVisualDescendants().OfType<TextBox>().Single(b => b.Name == "PART_HourInput");
            var presenter = hour.GetVisualDescendants().OfType<TextPresenter>().Single();
            Assert.Equal(expected.FontSize, hour.FontSize);
            Assert.Equal(expected.FontSize, presenter.FontSize);
            Assert.Equal(expected.FontWeight, presenter.FontWeight);
            Assert.Equal(TextAlignment.Center, presenter.TextAlignment);
            var line = presenter.TextLayout.TextLines[0];
            var textCenter = presenter.TranslatePoint(new Point(line.Start + line.Width / 2, 0), hour)!.Value.X;
            // Native TextPresenter rounds its allocated text width up to a whole DIP.
            Assert.InRange(Math.Abs(textCenter - hour.Bounds.Width / 2), 0, .5);

            var sizeKey = vibrant ? Sys.TypeScale.DisplayLarge.Size : Sys.TypeScale.DisplayMedium.Size;
            window.Resources[sizeKey] = 40d;
            Draw(window);
            Assert.Equal(40, presenter.FontSize);
            hour.FontSize = 32;
            window.Resources[sizeKey] = 42d;
            Draw(window);
            Assert.Equal(32, presenter.FontSize);
            hour.ClearValue(TextBox.FontSizeProperty);
            Draw(window);
            Assert.Equal(42, presenter.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("en-US", false)]
    [InlineData("de-DE", false)]
    [InlineData("zh-CN", false)]
    [InlineData("en-US", true)]
    [InlineData("de-DE", true)]
    public void DateInput_DisplaysCultureDelimitersForSingleAndRange(string cultureName, bool range)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var date = new DateOnly(2024, 2, 29);
        TemplatedControl picker = range ? new DateRangePicker { SelectedRange = new(date, date.AddDays(1)) }
            : new DatePicker { SelectedDate = date };
        picker.SetValue(DatePicker.CultureProperty, culture);
        picker.SetValue(DatePicker.DisplayModeProperty, DatePickerDisplayMode.Input);
        var window = Show(picker);
        try
        {
            var fields = picker.GetVisualDescendants().OfType<TextBox>().ToArray();
            var pattern = DateEntryController.InputPattern(culture);
            Assert.Equal(range ? 2 : 1, fields.Length);
            Assert.Equal(date.ToString(pattern, culture), fields[0].Text);
            if (range) Assert.Equal(date.AddDays(1).ToString(pattern, culture), fields[1].Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DateInput_TypingAndDeletingAcrossDelimitersPreservesNativeEditing()
    {
        var picker = new DatePicker { DisplayMode = DatePickerDisplayMode.Input, Culture = CultureInfo.GetCultureInfo("en-US") };
        var window = Show(picker);
        try
        {
            var field = picker.GetVisualDescendants().OfType<TextBox>().Single();
            field.Focus();
            var entered = "";
            foreach (var digit in "02292024")
            {
                entered += digit;
                window.KeyTextInput(digit.ToString());
                Draw(window);
                Assert.Equal(entered, new string(field.Text!.Take(field.CaretIndex).Where(char.IsDigit).ToArray()));
            }
            Assert.Equal("02/29/2024", field.Text);
            Assert.Equal(new DateOnly(2024, 2, 29), picker.SelectedDate);
            Assert.True(field.CanUndo);
            field.SelectAll();
            window.KeyTextInput("03/01/2024");
            Draw(window);
            Assert.Equal("03/01/2024", field.Text);
            Assert.Equal(new DateOnly(2024, 3, 1), picker.SelectedDate);
            Assert.True(field.CanUndo);
            field.Undo();
            Draw(window);
            Assert.Equal("02/29/2024", field.Text);

            field.CaretIndex = field.SelectionStart = field.SelectionEnd = 3;
            window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.None, null);
            Draw(window);
            Assert.Equal("02/92/024", field.Text);
            Assert.Equal(1, field.CaretIndex);
            Assert.Null(picker.SelectedDate);
            field.Text = "02292024";
            Draw(window);
            field.CaretIndex = field.SelectionStart = field.SelectionEnd = 2;
            window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.None, null);
            Draw(window);
            Assert.Equal("02/92/024", field.Text);
            Assert.Equal(2, field.Text!.Take(field.CaretIndex).Count(char.IsDigit));
            window.KeyTextInput("2");
            Draw(window);
            Assert.Equal("02/29/2024", field.Text);
            Assert.Equal(new DateOnly(2024, 2, 29), picker.SelectedDate);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void YearSelection_OverlaysCalendarWithoutChangingDialogHeight()
    {
        using var clock = new MotionTestClockScope();
        var picker = new DatePicker { SelectedDate = new(2024, 2, 29), DisplayedMonth = new(2024, 2, 1),
            MinYear = 2023, MaxYear = 2025, Culture = CultureInfo.GetCultureInfo("en-US") };
        var dialog = new DatePickerDialog { Content = picker, ConfirmButton = new Button { Content = "OK" },
            IsOpen = true, AnimateTransition = false };
        var window = Show(new Grid { Children = { dialog } });
        try
        {
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Draw(window);
            var card = window.GetVisualDescendants().OfType<MaterialSurface>()
                .Single(c => c.Name == "PART_Container" && c.TemplatedParent is DatePickerDialog);
            var height = card.Bounds.Height;
            var month = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "February 2024"));
            var point = month.TranslatePoint(new Point(month.Bounds.Width / 2, month.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            for (var i = 0; i < 32; i++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                Draw(window);
                Assert.Equal(height, card.Bounds.Height);
            }
            var year = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Tag, 2025));
            point = year.TranslatePoint(new Point(year.Bounds.Width / 2, year.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            for (var i = 0; i < 32; i++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                Draw(window);
                Assert.Equal(height, card.Bounds.Height);
            }
            Assert.Equal(new DateOnly(2025, 2, 1), picker.DisplayedMonth);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void YearSelection_DialogContainerBackgroundMasksMonthAndTracksChanges(bool dark)
    {
        using var clock = new MotionTestClockScope();
        var date = new DateOnly(2049, 6, 17);
        var picker = new DatePicker { SelectedDate = date, DisplayedMonth = new(2049, 6, 1),
            MinYear = 2040, MaxYear = 2060, Culture = CultureInfo.GetCultureInfo("en-US") };
        var dialog = new DatePickerDialog { Content = picker, ConfirmButton = new Button { Content = "OK" },
            IsOpen = true, AnimateTransition = false };
        // Match the Catalog/README contract: the picker and its year overlay use the dialog color.
        picker.Bind(TemplatedControl.BackgroundProperty, new Binding(nameof(DatePickerDialog.Background)) { Source = dialog });
        var window = Show(new Grid { Children = { dialog } });
        try
        {
            window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromSeconds(1)); Draw(window);
            var card = window.GetVisualDescendants().OfType<MaterialSurface>()
                .Single(c => c.Name == "PART_Container" && c.TemplatedParent is DatePickerDialog);
            var height = card.Bounds.Height;
            var day = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Tag, date));
            // Inside the selected-day circle, away from its text and the year grid's labels.
            var sample = day.TranslatePoint(new Point(day.Bounds.Width / 2 + 10, day.Bounds.Height / 2 + 10), window)!.Value;
            var containerColor = Assert.IsAssignableFrom<ISolidColorBrush>(dialog.Background).Color;
            using (var calendarFrame = window.CaptureRenderedFrame()!)
                Assert.NotEqual(containerColor, PixelAt(calendarFrame, sample));

            var month = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "June 2049"));
            var point = month.TranslatePoint(new Point(month.Bounds.Width / 2, month.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            for (var i = 0; i < 32; i++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Draw(window);
                Assert.Equal(height, card.Bounds.Height);
            }
            var years = picker.GetVisualDescendants().OfType<AnimatedVisibility>().Single();
            Assert.True(years.Visible);
            using (var yearFrame = window.CaptureRenderedFrame()!)
            {
                Assert.Equal(containerColor, PixelAt(yearFrame, sample));
                var directory = Path.GetFullPath("../../../../../artifacts/picker-regressions", AppContext.BaseDirectory);
                Directory.CreateDirectory(directory);
                yearFrame.Save(Path.Combine(directory, $"year-overlay.{(dark ? "dark" : "light")}.png"), PngBitmapEncoderOptions.Default);
            }

            dialog.Background = new SolidColorBrush(Colors.DarkSlateBlue);
            Draw(window);
            using var changedFrame = window.CaptureRenderedFrame()!;
            Assert.Equal(Colors.DarkSlateBlue, PixelAt(changedFrame, sample));
            Assert.Equal(height, card.Bounds.Height);
        }
        finally { window.Close(); }
    }

    private static Color PixelAt(WriteableBitmap frame, Point point)
    {
        using var buffer = frame.Lock();
        var x = (int)point.X; var y = (int)point.Y;
        var bytes = new byte[4];
        Marshal.Copy(buffer.Address + y * buffer.RowBytes + x * 4, bytes, 0, bytes.Length);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2])
            : Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
    }

    [AvaloniaFact]
    public void CalendarGapsAndEmptyCellsAcceptMonthWheelNavigation()
    {
        var picker = new DatePicker { Width = 400, DisplayedMonth = new(2024, 2, 1), Culture = CultureInfo.GetCultureInfo("en-US") };
        var window = Show(picker);
        try
        {
            Draw(window);
            var panel = picker.GetVisualDescendants().OfType<DateMonthPanel>().Single();
            var first = panel.GetVisualChildren().OfType<Button>().First();
            var next = panel.GetVisualChildren().OfType<Button>().Skip(1).First();
            var gap = new Point((first.Bounds.Right + next.Bounds.Left) / 2, first.Bounds.Center.Y);
            window.MouseWheel(panel.TranslatePoint(gap, window)!.Value, new Vector(0, -1));
            Draw(window);
            Assert.Equal(new DateOnly(2024, 3, 1), picker.DisplayedMonth);
            panel = picker.GetVisualDescendants().OfType<DateMonthPanel>().Single();
            window.MouseWheel(panel.TranslatePoint(new Point(24, 24), window)!.Value, new Vector(0, 1));
            Draw(window);
            Assert.Equal(new DateOnly(2024, 2, 1), picker.DisplayedMonth);
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
        window.Content = control; window.Show(); Draw(window);
        return window;
    }

    private static void Draw(Window window)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
    }
}

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class NativeDateTimePickerTests
{
    [AvaloniaFact]
    public void CalendarDatePicker_OpensCalendar_AndWritesNativeSelectedDate()
    {
        var picker = new CalendarDatePicker {SelectedDate=new DateTime(2024,2,10),DisplayDate=new DateTime(2024,2,1)};
        INameScope? scope=null;
        picker.TemplateApplied+=(_,e)=>scope=e.NameScope;
        var window=Show(picker);
        try
        {
            var text=scope!.Get<TextBox>("PART_TextBox");
            Assert.Contains("2024",text.Text);
            Invoke(scope!.Get<Button>("PART_Button"));window.UpdateLayout();
            Assert.True(scope!.Get<Popup>("PART_Popup").IsOpen);
            var calendar=scope!.Get<Calendar>("PART_Calendar");
            Assert.NotEmpty(calendar.GetVisualDescendants().OfType<CalendarDayButton>());
            calendar.SelectedDate=new DateTime(2024,2,29);window.UpdateLayout();
            Assert.Equal(new DateTime(2024,2,29),picker.SelectedDate);
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public void DatePicker_StagesThenCommits_AndPreservesHiddenColumns()
    {
        var original = new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero);
        var picker = new DatePicker { SelectedDate = original, DayVisible = false };
        INameScope? scope = null;
        picker.TemplateApplied += (_, e) => scope = e.NameScope;
        var window = Show(picker);
        try
        {
            Invoke(Part<Button>(picker, "PART_FlyoutButton")); window.UpdateLayout();
            var popup = scope!.Get<Popup>("PART_Popup");
            Assert.True(popup.IsOpen);
            var presenter = Assert.IsType<DatePickerPresenter>(popup.Child);
            Assert.False(Part<Panel>(presenter, "PART_DayHost").IsVisible);
            presenter.Date = original.AddYears(1);
            Assert.Equal(original, picker.SelectedDate);
            Invoke(Part<Button>(presenter, "PART_DismissButton"));
            Assert.False(popup.IsOpen); Assert.Equal(original, picker.SelectedDate);
            Invoke(Part<Button>(picker, "PART_FlyoutButton")); window.UpdateLayout();
            Assert.Equal(original, presenter.Date);
            presenter.Date = original.AddYears(1);
            Invoke(Part<Button>(presenter, "PART_AcceptButton"));
            Assert.False(popup.IsOpen); Assert.Equal(original.AddYears(1), picker.SelectedDate);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("12HourClock", true)]
    [InlineData("24HourClock", false)]
    public void TimePicker_PreservesSecondsStepsAndCommit(string clock, bool periodVisible)
    {
        var original = new TimeSpan(14, 30, 15);
        var picker = new TimePicker { SelectedTime = original, UseSeconds = true,
            MinuteIncrement = 5, SecondIncrement = 5, ClockIdentifier = clock };
        INameScope? scope = null;
        picker.TemplateApplied += (_, e) => scope = e.NameScope;
        var window = Show(picker);
        try
        {
            Invoke(Part<Button>(picker, "PART_FlyoutButton")); window.UpdateLayout();
            var popup = scope!.Get<Popup>("PART_Popup");
            Assert.True(popup.IsOpen);
            var presenter = Assert.IsType<TimePickerPresenter>(popup.Child);
            Assert.True(Part<Panel>(presenter, "PART_SecondHost").IsVisible);
            Assert.Equal(periodVisible, Part<Panel>(presenter, "PART_PeriodHost").IsVisible);
            Assert.Equal(5, presenter.MinuteIncrement); Assert.Equal(5, presenter.SecondIncrement);
            presenter.Time = new TimeSpan(16, 45, 20);
            Assert.Equal(original, picker.SelectedTime);
            Invoke(Part<Button>(presenter, "PART_AcceptButton"));
            Assert.False(popup.IsOpen); Assert.Equal(new TimeSpan(16, 45, 20), picker.SelectedTime);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 640, Height = 600 };
        control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        control.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        window.Styles.Add(new FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = control; window.Show(); window.UpdateLayout(); return window;
    }

    private static T Part<T>(Control root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);

    private static void Invoke(Button button) =>
        Assert.IsAssignableFrom<IInvokeProvider>(ControlAutomationPeer.CreatePeerForElement(button)).Invoke();
}



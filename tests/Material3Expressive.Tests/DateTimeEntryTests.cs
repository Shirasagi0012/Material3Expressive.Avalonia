using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Xunit;
using DatePicker = Material3Expressive.Components.DatePicker;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Tests;

public class DateTimeEntryTests
{
    [AvaloniaFact]
    public void DateSelection_PreservesBinding_RespectsBlackout_AndChangesMonth()
    {
        var source = new DatePicker { SelectedDate = new(2024, 2, 15) };
        var picker = new DatePicker { DisplayedMonth = new(2024, 2, 1), Culture = CultureInfo.GetCultureInfo("en-US"),
            IsDateSelectable = d => d.Day != 16 };
        picker.Bind(DatePicker.SelectedDateProperty, new Binding(nameof(DatePicker.SelectedDate)) { Source=source, Mode=BindingMode.TwoWay });
        var window=Show(picker);
        try
        {
            Assert.False(Day(picker, new(2024,2,16)).IsEnabled);
            Invoke(Day(picker,new(2024,2,29))); Flush(window);
            Assert.Equal(new DateOnly(2024,2,29),source.SelectedDate);
            source.SelectedDate=new(2024,2,28); Flush(window);
            Assert.Contains("selected",Day(picker,new(2024,2,28)).Classes);
            Invoke(picker.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Change to next month")); Flush(window);
            Assert.Equal(new DateOnly(2024,3,1),picker.DisplayedMonth);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RangeSelection_Completes_AndRestartsOnEarlierOrThirdDate()
    {
        var picker=new DateRangePicker { MinYear=2024,MaxYear=2024,DisplayedMonth=new(2024,1,1),Culture=CultureInfo.GetCultureInfo("en-US") };
        var window=Show(picker);
        try
        {
            Invoke(Day(picker,new(2024,1,10))); Flush(window);
            Assert.Equal(new DateRange(new(2024,1,10),null),picker.SelectedRange);
            Invoke(Day(picker,new(2024,1,5))); Flush(window);
            Assert.Equal(new DateRange(new(2024,1,5),null),picker.SelectedRange);
            Invoke(Day(picker,new(2024,1,20))); Flush(window);
            Assert.Equal(new DateRange(new(2024,1,5),new(2024,1,20)),picker.SelectedRange);
            Invoke(Day(picker,new(2024,1,25))); Flush(window);
            Assert.Equal(new DateRange(new(2024,1,25),null),picker.SelectedRange);
        }
        finally {window.Close();}
    }

    [AvaloniaFact]
    public void DateInput_RejectsInvalidLeapDay_AndKeepsEditableText()
    {
        var picker=new DatePicker {DisplayMode=DatePickerDisplayMode.Input,Culture=CultureInfo.GetCultureInfo("en-US")};
        var window=Show(picker);
        try
        {
            var field=picker.GetVisualDescendants().OfType<TextBox>().Single(box => box.Name=="PART_DateInput");
            field.Text="02292023"; Flush(window);
            Assert.Null(picker.SelectedDate); Assert.True(MaterialTextField.GetIsError(field));
            Assert.Contains("2023",field.Text);
            field.Text="02292024"; Flush(window);
            Assert.Equal(new DateOnly(2024,2,29),picker.SelectedDate); Assert.False(MaterialTextField.GetIsError(field));
            Invoke(picker.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="PART_ModeToggle")); Flush(window);
            Assert.Equal(DatePickerDisplayMode.Picker,picker.DisplayMode);
            Assert.Contains("selected",Day(picker,new(2024,2,29)).Classes);
        }
        finally {window.Close();}
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void TimeInput_InvalidDraftDoesNotPublish_ValidFieldsAndPeriodsDo(bool clock24)
    {
        var picker=new TimeInput {SelectedTime=new(14,30),Is24Hour=clock24};
        var window=Show(picker);
        try
        {
            var hour=Part<TextBox>(picker,"PART_HourInput"); var minute=Part<TextBox>(picker,"PART_MinuteInput");
            minute.Text="75"; Flush(window);
            Assert.False(picker.IsInputValid); Assert.Equal(new TimeOnly(14,30),picker.SelectedTime);
            hour.Focus(); Flush(window); Assert.Equal("75",minute.Text); Assert.False(picker.IsInputValid);
            minute.Text="45"; Flush(window);
            Assert.True(picker.IsInputValid); Assert.Equal(new TimeOnly(14,45),picker.SelectedTime);
            hour.Text=clock24?"23":"11"; Flush(window);
            Assert.Equal(new TimeOnly(23,45),picker.SelectedTime);
            if(!clock24)
            {
                Invoke(picker.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"AM"))); Flush(window);
                Assert.Equal(new TimeOnly(11,45),picker.SelectedTime);
            }
            picker.SelectedTime=new(1,2); Flush(window);
            Assert.Equal("01",hour.Text); Assert.Equal("02",minute.Text);
        }
        finally {window.Close();}
    }

    [AvaloniaFact]
    public async Task Dial_TapAdvancesToMinute_KeyboardWraps_AndDetachCancelsMotion()
    {
        using var clock=new MotionTestClockScope();
        var picker=new TimePicker {SelectedTime=new(0,0),Is24Hour=true,Layout=TimePickerLayout.Vertical};
        var window=Show(picker);
        var dial=picker.GetVisualDescendants().OfType<ClockFace>().Single();
        var point=dial.TranslatePoint(new Point(229,128),window)!.Value;
        window.MouseDown(point,MouseButton.Left); window.MouseUp(point,MouseButton.Left); Flush(window);
        Assert.Equal(3,picker.SelectedTime.Hour);
        await Task.Delay(150); clock.AdvanceBy(TimeSpan.FromMilliseconds(150)); Flush(window);
        Assert.Equal(TimeSelection.Minute,picker.Selection);
        dial.Focus();window.KeyPress(Key.Left,Avalonia.Input.RawInputModifiers.None,PhysicalKey.None,null); Flush(window);
        Assert.Equal(59,picker.SelectedTime.Minute);
        window.Content=null; clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Dial_ScopeRebuild_ReconnectsSelectorAndReachesItsTarget()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { SelectedTime = new(0, 0), Is24Hour = true, Layout = TimePickerLayout.Vertical };
        var window = Show(picker);
        try
        {
            var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
            float Angle() => (float)typeof(ClockFace).GetField("_position",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(dial)!;
            picker.SelectedTime = new(3, 0);
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(32));
            var before = Angle();
            Assert.InRange(before, -MathF.PI / 2 + .01f, -.01f);

            var old = AnimationScope.Get(dial);
            await old.Diagnostics.EndAsync();
            var replacement = AnimationScope.Get(dial);
            Assert.NotSame(old, replacement);
            Assert.Equal(before, Angle());
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.InRange(MathF.Abs(Angle()), 0, .001f);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task Dial_DetachCancelsPendingAdvanceToMinute()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { SelectedTime = new(0, 0), Is24Hour = true, Layout = TimePickerLayout.Vertical };
        var window = Show(picker);
        try
        {
            var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
            var point = dial.TranslatePoint(new Point(229, 128), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Assert.Equal(3, picker.SelectedTime.Hour);
            window.Content = null;
            Flush(window);
            await Task.Delay(150);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
            Assert.Equal(TimeSelection.Hour, picker.Selection);
            Assert.Equal(0, clock.PendingFrameCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Dial_HourLabels_ShowZeroOnThe24HourOuterRing()
    {
        var picker = new TimePicker { SelectedTime = new(0, 0), Is24Hour = true, Selection = TimeSelection.Hour };
        var window = Show(picker);
        try
        {
            var labels = picker.GetVisualDescendants().OfType<ClockFace>().Single()
                .GetVisualDescendants().OfType<Button>().Select(button => button.Content?.ToString()).ToArray();
            Assert.Equal("0", labels[0]);
            Assert.Equal("12", labels[12]);
            Assert.Contains("23", labels);
            picker.Is24Hour = false; Flush(window);
            labels = picker.GetVisualDescendants().OfType<ClockFace>().Single()
                .GetVisualDescendants().OfType<Button>().Select(button => button.Content?.ToString()).ToArray();
            Assert.Equal(12, labels.Length);
            Assert.Equal("12", labels[0]);
            Assert.DoesNotContain("0", labels);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(true,23,23)]
    [InlineData(false,12,0)]
    public void Scroll_CyclicSelectionAndExternalValueStaySynchronized(bool clock24,int displayHour,int hour)
    {
        var picker=new TimeScroll {Is24Hour=clock24,SelectedTime=new(0,30)};
        var window=Show(picker);
        try
        {
            var field=Part<DateTimePickerPanel>(picker,"PART_HourScroll");
            Assert.True(field.ShouldLoop);
            field.SelectedValue=displayHour; Flush(window);
            Assert.Equal(hour,picker.SelectedTime.Hour);
            picker.SelectedTime=new(5,10);Flush(window);
            Assert.Equal(5,field.SelectedValue); Assert.Equal(10,Part<DateTimePickerPanel>(picker,"PART_MinuteScroll").SelectedValue);
        }
        finally {window.Close();}
    }

    private static Button Day(Control owner,DateOnly date)=>owner.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Tag,date));
    private static T Part<T>(Control root,string name) where T:Control=>root.GetVisualDescendants().OfType<T>().Single(c=>c.Name==name);
    private static void Invoke(Button button)=>Assert.IsAssignableFrom<IInvokeProvider>(ControlAutomationPeer.CreatePeerForElement(button)).Invoke();
    private static void Flush(Window window){Dispatcher.UIThread.RunJobs();window.UpdateLayout();}
    private static MotionTestWindow Show(Control control)
    {
        var window=new MotionTestWindow {Width=700,Height=700};
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        control.HorizontalAlignment=HorizontalAlignment.Left; control.VerticalAlignment=VerticalAlignment.Top;
        window.Content=control;window.Show();Flush(window);return window;
    }
}

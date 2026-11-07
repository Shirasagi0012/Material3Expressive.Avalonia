using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using DatePicker=Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Tests;

public class DateKeyboardTests
{
    [AvaloniaFact]
    public void SelectingYear_CancelsAnUnfinishedMonthAnimation()
    {
        using var clock = new MotionTestClockScope();
        var picker = new DatePicker { MinYear = 2023, MaxYear = 2025, DisplayedMonth = new(2024, 2, 1),
            Culture = CultureInfo.GetCultureInfo("en-US") };
        var window = DialogTests.Show(picker);
        try
        {
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(); }
            var strip = picker.GetVisualDescendants().OfType<DateMonthStrip>().Single();
            clock.Track(strip.AnimateToAsync(new(2024, 3, 1)));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush();
            Assert.True(strip.IsScrollInProgress);
            Invoke(picker.GetVisualDescendants().OfType<Button>().Single(b =>
                AutomationProperties.GetName(b) == "Switch to selecting a year"));
            Flush();
            var targetMonth = picker.DisplayedMonth!.Value.Month;
            Invoke(picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Tag, 2025)));
            for (var i = 0; i < 60; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(); }
            Assert.False(strip.IsScrollInProgress);
            Assert.Equal(new DateOnly(2025, targetMonth, 1), picker.DisplayedMonth);
            Assert.Equal(0, strip.ScrollOffset);
        }
        finally { window.Close(); }
        static void Invoke(Button button) => Assert.IsAssignableFrom<IInvokeProvider>(ControlAutomationPeer.CreatePeerForElement(button)).Invoke();
        void Flush() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CalendarTabs_UseEnabledEdges_AndHorizontalKeysSkipDisabledDays(bool rtl)
    {
        using var clock=new MotionTestClockScope();
        var picker=new DatePicker {MinYear=2024,MaxYear=2024,DisplayedMonth=new(2024,2,1),Culture=CultureInfo.GetCultureInfo("en-US"),
            FlowDirection=rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight,IsDateSelectable=date=>date.Day is not (1 or 3 or 29)};
        var after=new Button {Content="After dates"};
        var window=DialogTests.Show(new StackPanel {Children={picker,after}});
        try
        {
            var next=picker.GetVisualDescendants().OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Change to next month");
            Day(2).Focus();Key(Avalonia.Input.Key.Tab,RawInputModifiers.Shift);Assert.Same(next,window.FocusManager!.GetFocusedElement());
            Day(2).Focus();Key(rtl?Avalonia.Input.Key.Left:Avalonia.Input.Key.Right);Assert.Same(Day(4),window.FocusManager!.GetFocusedElement());
            Day(28).Focus();Key(Avalonia.Input.Key.Tab);Assert.Same(after,window.FocusManager!.GetFocusedElement());
            Day(28).Focus();Key(rtl?Avalonia.Input.Key.Left:Avalonia.Input.Key.Right);
            for(var i=0;i<70;i++){clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush();}
            Assert.Equal(new DateOnly(2024,3,1),picker.DisplayedMonth);
            Assert.Equal(new DateOnly(2024,3,2),(window.FocusManager!.GetFocusedElement() as Button)?.Tag);
        }
        finally{window.Close();}
        Button Day(int day)=>picker.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Tag,new DateOnly(2024,2,day)));
        void Key(Key key,RawInputModifiers modifiers=RawInputModifiers.None)
        {window.KeyPress(key,modifiers,PhysicalKey.None,null);window.KeyRelease(key,modifiers,PhysicalKey.None,null);Flush();}
        void Flush(){Dispatcher.UIThread.RunJobs();window.UpdateLayout();Dispatcher.UIThread.RunJobs();}
    }
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void YearPicker_FocusesDisplayedYear_TraversesGrid_AndTabsOut(bool rtl)
    {
        using var clock=new MotionTestClockScope();
        var picker=new DatePicker { MinYear=2023,MaxYear=2025,DisplayedMonth=new(2024,2,1),Culture=CultureInfo.GetCultureInfo("en-US"),
            FlowDirection=rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight };
        var after=new Button {Content="After dates"};
        var window=DialogTests.Show(new StackPanel {Children={picker,after}});
        try
        {
            var month=picker.GetVisualDescendants().OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Switch to selecting a year");
            Assert.IsAssignableFrom<IInvokeProvider>(ControlAutomationPeer.CreatePeerForElement(month)).Invoke();Flush();
            for(var i=0;i<40;i++){clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush();}
            Assert.Equal(2024,FocusedYear());
            Key(rtl?Avalonia.Input.Key.Left:Avalonia.Input.Key.Right);Assert.Equal(2025,FocusedYear());
            Key(Avalonia.Input.Key.Tab,RawInputModifiers.Shift);Assert.Same(month,window.FocusManager!.GetFocusedElement());
            Key(Avalonia.Input.Key.Tab);Assert.Equal(2024,FocusedYear());
            Key(Avalonia.Input.Key.Tab);Assert.Same(after,window.FocusManager!.GetFocusedElement());
            Key(Avalonia.Input.Key.Tab,RawInputModifiers.Shift);Assert.Equal(2024,FocusedYear());
            Key(rtl?Avalonia.Input.Key.Left:Avalonia.Input.Key.Right);Key(Avalonia.Input.Key.Enter);
            for(var i=0;i<40;i++){clock.AdvanceBy(TimeSpan.FromMilliseconds(16));Flush();}
            Assert.Equal(new DateOnly(2025,2,1),picker.DisplayedMonth);
        }
        finally{window.Close();}
        int? FocusedYear()=>(window.FocusManager!.GetFocusedElement() as Button)?.Tag as int?;
        void Key(Key key,RawInputModifiers modifiers=RawInputModifiers.None)
        {window.KeyPress(key,modifiers,PhysicalKey.None,null);window.KeyRelease(key,modifiers,PhysicalKey.None,null);Flush();}
        void Flush(){Dispatcher.UIThread.RunJobs();window.UpdateLayout();Dispatcher.UIThread.RunJobs();}
    }
}

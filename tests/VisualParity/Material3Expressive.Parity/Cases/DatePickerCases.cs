using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Headless;
using Material3Expressive.Components;
using DatePicker = Material3Expressive.Components.DatePicker;

namespace Material3Expressive.Parity;

public sealed record DatePickerConfig(bool Range=false,bool Input=false,bool Selected=true,bool Dark=false,bool Rtl=false,string Culture="en-US",double Width=400,string Action="none");

[ParitySuite("date-picker")]
public sealed class DatePickerCases
{
    public static IEnumerable<CaseData<DatePickerConfig>> StaticConfigurations()
    {
        foreach(var range in new[]{false,true})
        foreach(var input in new[]{false,true})
        foreach(var selected in new[]{false,true})
        foreach(var dark in new[]{false,true})
            yield return Data($"static.{(range?"range":"single")}.{(input?"input":"calendar")}.{(selected?"selected":"empty")}.{(dark?"dark":"light")}",new(range,input,selected,dark));
        foreach(var range in new[]{false,true})
        {
            yield return Data($"static.rtl.{range}",new(range,Rtl:true));
            yield return Data($"static.german-input.{range}",new(range,Input:true,Culture:"de-DE"));
            yield return Data($"static.constrained.{range}",new(range,Width:320));
        }
    }
    public static IEnumerable<CaseData<DatePickerConfig>> Changes()
    {
        foreach(var range in new[]{false,true})
            yield return Data($"dynamic.mode-and-selection.{range}",new(range,Action:"change"));
        foreach(var dark in new[]{false,true})
            yield return Data($"dynamic.year-selection.{(dark?"dark":"light")}",new(Dark:dark,Action:"years"));
    }
    private static CaseData<DatePickerConfig> Data(string id,DatePickerConfig c)=>new(id.ToLowerInvariant(),id,c);
    [ParityScenario("static",Data=nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer)=>Capture(writer,false);
    [ParityScenario("changes",Data=nameof(Changes))]
    public Task Change(CaptureWriter writer)=>Capture(writer,true);
    internal static async Task Capture(CaptureWriter writer,bool dynamic)
    {
        var c=writer.Metadata.ReadConfig<DatePickerConfig>();
        await using var host=new AvaloniaHost(writer,440,680);
        CaseDefaults.SetThemeColors(host,c.Dark);
        host.Window.FlowDirection=c.Rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
        TemplatedControl picker=c.Range?new DateRangePicker():new DatePicker();
        picker.SetValue(DatePicker.CultureProperty,CultureInfo.GetCultureInfo(c.Culture));
        picker.SetValue(DatePicker.MinYearProperty,2023);picker.SetValue(DatePicker.MaxYearProperty,2025);
        picker.SetValue(DatePicker.DisplayedMonthProperty,new DateOnly(2024,2,1));
        picker.SetValue(DatePicker.DisplayModeProperty,c.Input?DatePickerDisplayMode.Input:DatePickerDisplayMode.Picker);
        if(c.Selected)SetSelection(new(2024,2,10),new(2024,2,20));
        var container=new Grid {Width=c.Width,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new(16),Children={picker}};
        var after=new Border {Width=40,Height=24,Focusable=true,Background=Brushes.Blue,HorizontalAlignment=HorizontalAlignment.Left};
        if(c.Action=="year-keys")
        {
            container.Children.Clear();container.Children.Add(new StackPanel {Children={picker,after}});
        }
        host.Mount(new Grid {Children={container}});host.Frame(0);
        void SetSelection(DateOnly? start,DateOnly? end)
        {
            if(picker is DateRangePicker range)range.SelectedRange=new(start,end);
            else ((DatePicker)picker).SelectedDate=start;
        }
        void Sample(string id,bool image=true)
        {
            var selected=picker is DateRangePicker range?range.SelectedRange:new DateRange(((DatePicker)picker).SelectedDate,null);
            var keyboard=c.Action=="year-keys"?new[]{
                Measurement.Numeric("focused-year",(host.Window.FocusManager!.GetFocusedElement() as Button)?.Tag is int year?year:-1,"year",definition:"date-picker.focused-year"),
                Measurement.Flag("after-focused",after.IsFocused,definition:"date-picker.after-focused")}:Array.Empty<Measurement>();
            writer.Submit(host.Snapshot(id,id,[new ProbePoint("picker","date-picker",null,
                AvaloniaMeasurements.Geometry(picker,host.Window).Where(m=>m.Name is "box" or "size").Concat([
                    Measurement.Numeric("month", picker.GetValue(DatePicker.DisplayedMonthProperty)?.DayNumber ?? -1,"day",definition:"date-picker.displayed-month-day-number"),
                    Measurement.Numeric("start",selected.Start?.DayNumber??-1,"day",definition:"date-picker.selected-start-day-number"),
                    Measurement.Numeric("end",selected.End?.DayNumber??-1,"day",definition:"date-picker.selected-end-day-number")]).Concat(keyboard).ToArray())],image));
        }
        Sample("initial");
        if(c.Action=="year-keys")
        {
            for(long t=16000;t<=512000;t+=16000)host.Frame(t);
            var month=picker.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"February 2024"));
            var point=month.TranslatePoint(new Point(month.Bounds.Width/2,month.Bounds.Height/2),host.Window)!.Value;
            host.AdvanceTo(600000);host.Input("down",point);host.Frame(600000);
            host.AdvanceTo(616000);host.Input("up",point);host.Frame(616000);
            for(long t=632000;t<=1112000;t+=16000)host.Frame(t);
            Sample("years-open");
            host.AdvanceTo(1152000);
            var selectedYear=picker.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Tag,2024));
            if(!selectedYear.Focus())throw new InvalidOperationException("Selected year could not receive focus.");
            writer.Record(1152000,"input","Focus on selected year button");
            host.Draw();host.Frame(1152000);Sample("year-focused");
            void Key(long time,Key key,RawInputModifiers modifiers=RawInputModifiers.None)
            {
                host.AdvanceTo(time);host.Window.KeyPress(key,modifiers,PhysicalKey.None,null);host.Window.KeyRelease(key,modifiers,PhysicalKey.None,null);host.Draw();host.Frame(time);Sample($"key-{time}");
            }
            Key(1200000,c.Rtl?Avalonia.Input.Key.Left:Avalonia.Input.Key.Right);
            Key(1300000,Avalonia.Input.Key.Tab,RawInputModifiers.Shift);
            Key(1400000,Avalonia.Input.Key.Tab);Key(1500000,Avalonia.Input.Key.Tab);
            Key(1600000,Avalonia.Input.Key.Tab,RawInputModifiers.Shift);
            Key(1700000,c.Rtl?Avalonia.Input.Key.Left:Avalonia.Input.Key.Right);
            Key(1800000,Avalonia.Input.Key.Enter);
            for(long t=1816000;t<=2312000;t+=16000)host.Frame(t);
            Sample("year-selected");
        }
        else         if(c.Action.StartsWith("month-"))
        {
            for(long t=16000;t<=512000;t+=16000) host.Frame(t);
            Sample("calendar-ready");
            using var touch = Activator.CreateInstance<TouchDevice>();
            var impl = typeof(TopLevel).GetProperty("PlatformImpl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Window)!;
            var root = (IInputRoot)impl.GetType().GetProperty("InputRoot")!.GetValue(impl)!;
            var input = (Action<RawInputEventArgs>)impl.GetType().GetProperty("Input")!.GetValue(impl)!;
            // Avalonia mirrors local coordinates in RTL; local (0,0) can be the physical
            // right edge. Raw touch input uses the same physical left/top origin as Core.
            var cornerA = picker.TranslatePoint(default,host.Window)!.Value;
            var cornerB = picker.TranslatePoint(new Point(picker.Bounds.Width,picker.Bounds.Height),host.Window)!.Value;
            var origin = new Point(Math.Min(cornerA.X,cornerB.X),Math.Min(cornerA.Y,cornerB.Y));
            for(long t=0;t<=640000;t+=16000)
            {
                host.AdvanceTo(600000+t);
                if(c.Action.Contains("touch"))
                {
                    var distance = t switch { 0=>0,16000=>10,32000=>36,48000=>80,64000=>120,_=>-1 };
                    var release = c.Action.EndsWith("slow") ? 144000 : 80000;
                    if(distance>=0 || t==release)
                    {
                        if(t==release)distance=120;
                        var point=origin+new Vector(200+(c.Rtl?distance:-distance),300);
                        var type=t==0?RawPointerEventType.TouchBegin:t==release?RawPointerEventType.TouchEnd:RawPointerEventType.TouchUpdate;
                        var hit=host.Window.InputHitTest(point);
                        input((RawTouchEventArgs)Activator.CreateInstance(typeof(RawTouchEventArgs),touch,(ulong)(1600+t/1000),root,type,point,RawInputModifiers.None,1L)!);
                        var strip=picker.GetVisualDescendants().OfType<DateMonthStrip>().Single();
                        writer.Record(600000+t,"touch-input",$"{type}; hit={hit?.GetType().Name}; offset={strip.ScrollOffset}; scrolling={strip.IsScrollInProgress}");
                    }
                }
                else if(t==0)
                {
                    var name=c.Action=="month-next"?"Change to next month":"Change to previous month";
                    var button=picker.GetVisualDescendants().OfType<Button>().Single(b=>AutomationProperties.GetName(b)==name);
                    var point=button.TranslatePoint(new Point(button.Bounds.Width/2,button.Bounds.Height/2),host.Window)!.Value;
                    host.Input("down",point);host.Input("up",point);
                }
                host.Frame(600000+t);
                if(t is 0 or 32000 or 64000 or 96000 or 128000 or 256000 or 512000 or 640000)Sample($"month-{t}");
            }
        }
        else if(c.Action == "years")
        {
            // Let the entry's initial content animation settle before hit-testing navigation.
            for(long t=16000;t<=512000;t+=16000) host.Frame(t);
            Sample("calendar-ready");
            var month = picker.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "February 2024"));
            var point = month.TranslatePoint(new Point(month.Bounds.Width / 2, month.Bounds.Height / 2), host.Window)!.Value;
            host.AdvanceTo(600000); host.Input("down",point); host.Input("up",point); host.Draw(); host.Frame(600000); Sample("year-start");
            for(long t=616000;t<=1112000;t+=16000)
            {
                host.Frame(t);
                if(t is 664000 or 856000) Sample($"year-enter-{t}");
            }
            Sample("year-settled");
            host.AdvanceTo(1200000); host.Input("down",point); host.Input("up",point); host.Draw(); host.Frame(1200000); Sample("calendar-start");
            for(long t=1216000;t<=1712000;t+=16000)
            {
                host.Frame(t);
                if(t is 1264000 or 1456000) Sample($"year-exit-{t}");
            }
            Sample("calendar-settled"); host.AdvanceTo(1800000); Sample("idle",false);
        }
        else if(dynamic)
        {
            host.AdvanceTo(1000);picker.SetCurrentValue(DatePicker.DisplayModeProperty,DatePickerDisplayMode.Input);writer.Record(1000,"mutation","input-mode");host.Draw();host.Frame(1000);Sample("input-start");
            for(long t=16000;t<=512000;t+=16000)host.Frame(t);
            Sample("input-settled");
            host.AdvanceTo(600000);SetSelection(new(2024,2,29),null);writer.Record(600000,"mutation","leap-day");host.Draw();host.Frame(600000);Sample("leap-day");
            host.AdvanceTo(700000);picker.SetCurrentValue(DatePicker.DisplayModeProperty,DatePickerDisplayMode.Picker);writer.Record(700000,"mutation","calendar-mode");host.Draw();host.Frame(700000);Sample("calendar-start");
            for(long t=716000;t<=1212000;t+=16000)host.Frame(t);
            Sample("calendar-settled");host.AdvanceTo(1300000);Sample("idle",false);
        }
    }
}

[ParitySuite("date-month-scroll")]
public sealed class DateMonthScrollCases
{
    public static IEnumerable<CaseData<DatePickerConfig>> Configurations()
    {
        foreach(var rtl in new[]{false,true})
        foreach(var action in new[]{"next","previous","touch-fast","touch-slow"})
            yield return new($"{action}.{(rtl?"rtl":"ltr")}",action,new(Rtl:rtl,Action:$"month-{action}"));
    }
    [ParityScenario("scroll",Data=nameof(Configurations))]
    public Task Scroll(CaptureWriter writer)=>DatePickerCases.Capture(writer,true);
}

[ParitySuite("date-keyboard")]
public sealed class DateKeyboardCases
{
    public static IEnumerable<CaseData<DatePickerConfig>> Configurations()
    {
        foreach(var rtl in new[]{false,true})
            yield return new($"year-grid.{(rtl?"rtl":"ltr")}","Year grid keyboard",new(Rtl:rtl,Action:"year-keys"));
    }
    [ParityScenario("keys",Data=nameof(Configurations))]
    public Task Keys(CaptureWriter writer)=>DatePickerCases.Capture(writer,true);
}

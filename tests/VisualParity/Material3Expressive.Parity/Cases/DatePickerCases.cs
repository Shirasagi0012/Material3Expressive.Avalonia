using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
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
    private static async Task Capture(CaptureWriter writer,bool dynamic)
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
        host.Mount(new Grid {Children={container}});host.Frame(0);
        void SetSelection(DateOnly? start,DateOnly? end)
        {
            if(picker is DateRangePicker range)range.SelectedRange=new(start,end);
            else ((DatePicker)picker).SelectedDate=start;
        }
        void Sample(string id,bool image=true)
        {
            var selected=picker is DateRangePicker range?range.SelectedRange:new DateRange(((DatePicker)picker).SelectedDate,null);
            writer.Submit(host.Snapshot(id,id,[new ProbePoint("picker","date-picker",null,
                AvaloniaMeasurements.Geometry(picker,host.Window).Where(m=>m.Name is "box" or "size").Concat([
                    Measurement.Numeric("start",selected.Start?.DayNumber??-1,"day",definition:"date-picker.selected-start-day-number"),
                    Measurement.Numeric("end",selected.End?.DayNumber??-1,"day",definition:"date-picker.selected-end-day-number")]).ToArray())],image));
        }
        Sample("initial");
        if(c.Action == "years")
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

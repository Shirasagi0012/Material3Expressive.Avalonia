using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Parity;

public sealed record TimePickerConfig(string Kind="dial",bool Vibrant=false,bool Horizontal=false,bool Clock24=false,bool Dark=false,bool Rtl=false,bool Toggle=false,string Action="none");

[ParitySuite("time-picker")]
public sealed class TimePickerCases
{
    public static IEnumerable<CaseData<TimePickerConfig>> StaticConfigurations()
    {
        foreach(var kind in new[]{"dial","input","scroll"})
        foreach(var vibrant in kind=="scroll"?new[]{true}:new[]{false,true})
        foreach(var horizontal in kind=="dial"?new[]{false,true}:new[]{false})
        foreach(var clock24 in new[]{false,true})
        foreach(var dark in new[]{false,true})
            yield return Data($"static.{kind}.{(vibrant?"vibrant":"standard")}.{(horizontal?"horizontal":"vertical")}.{(clock24?"24h":"12h")}.{(dark?"dark":"light")}",new(kind,vibrant,horizontal,clock24,dark));
        foreach(var kind in new[]{"dial","input","scroll"})
            yield return Data($"static.rtl.{kind}",new(kind,Vibrant:true,Rtl:true));
        foreach(var kind in new[]{"input","scroll"})
            yield return Data($"static.uncontained.{kind}",new(kind,Vibrant:true,Toggle:true));
    }
    public static IEnumerable<CaseData<TimePickerConfig>> Changes()
    {
        foreach(var kind in new[]{"dial","input","scroll"})
            yield return Data($"dynamic.selection.{kind}",new(kind,Vibrant:kind=="scroll",Clock24:true,Action:"change"));
    }
    static CaseData<TimePickerConfig> Data(string id,TimePickerConfig c)=>new(id,id,c);
    [ParityScenario("static",Data=nameof(StaticConfigurations))] public Task Static(CaptureWriter w)=>Capture(w,false);
    [ParityScenario("changes",Data=nameof(Changes))] public Task Change(CaptureWriter w)=>Capture(w,true);
    static async Task Capture(CaptureWriter writer,bool dynamic)
    {
        var c=writer.Metadata.ReadConfig<TimePickerConfig>();
        await using var host=new AvaloniaHost(writer,640,520);
        CaseDefaults.SetThemeColors(host,c.Dark);
        host.Window.FlowDirection=c.Rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
        TemplatedControl picker=c.Kind switch {"input"=>new TimeInput(),"scroll"=>new TimeScroll(),_=>new TimePicker()};
        picker.SetValue(TimePicker.SelectedTimeProperty,new TimeOnly(14,30));picker.SetValue(TimePicker.Is24HourProperty,c.Clock24);
        picker.SetValue(TimePicker.VariantProperty,c.Vibrant?TimePickerVariant.Vibrant:TimePickerVariant.Standard);
        picker.SetValue(TimePicker.LayoutProperty,c.Horizontal?TimePickerLayout.Horizontal:TimePickerLayout.Vertical);
        if(c.Toggle)picker.SetValue(TimePicker.ToggleProperty,new Border {Width=48,Height=48,Background=Brushes.Magenta});
        picker.Margin=new(16);picker.HorizontalAlignment=HorizontalAlignment.Left;picker.VerticalAlignment=VerticalAlignment.Top;
        host.Mount(new Grid {Children={picker}});host.Frame(0);
        void Sample(string id,bool image=true)
        {
            var time=picker.GetValue(TimePicker.SelectedTimeProperty);
            writer.Submit(host.Snapshot(id,id,[new ProbePoint("picker","time-picker",null,
                AvaloniaMeasurements.Geometry(picker,host.Window).Where(m=>m.Name is "box" or "size").Concat([
                    Measurement.Numeric("hour",time.Hour,"hour",definition:"time-picker.selected-hour"),
                    Measurement.Numeric("minute",time.Minute,"minute",definition:"time-picker.selected-minute")]).ToArray())],image));
        }
        Sample("initial");
        if(dynamic)
        {
            host.AdvanceTo(1000);picker.SetCurrentValue(TimePicker.SelectedTimeProperty,new TimeOnly(23,59));writer.Record(1000,"mutation","time");host.Draw();host.Frame(1000);Sample("time-start");
            for(long t=16000;t<=512000;t+=16000)host.Frame(t);Sample("time-settled");
            host.AdvanceTo(600000);picker.SetCurrentValue(TimePicker.SelectionProperty,TimeSelection.Minute);writer.Record(600000,"mutation","minute-selection");host.Draw();host.Frame(600000);Sample("minute-start");
            for(long t=616000;t<=1112000;t+=16000)host.Frame(t);Sample("minute-settled");host.AdvanceTo(1200000);Sample("idle",false);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record DateMonthMotionConfig(string Action="next",bool Rtl=false,double Width=336);

[ParitySuite("date-month-motion")]
public sealed class DateMonthMotionCases
{
    public static IEnumerable<CaseData<DateMonthMotionConfig>> Configurations()
    {
        foreach(var rtl in new[]{false,true})
        foreach(var width in new[]{336,375.5})
        foreach(var action in new[]{"next","previous","reverse","snap-low","snap-fast","snap-back"})
            yield return new($"{action}.{(rtl?"rtl":"ltr")}.{width.ToString(System.Globalization.CultureInfo.InvariantCulture)}",action,new(action,rtl,width));
    }
    [ParityScenario("motion",Data=nameof(Configurations))]
    public async Task Motion(CaptureWriter writer)
    {
        var c=writer.Metadata.ReadConfig<DateMonthMotionConfig>();
        await using var host=new AvaloniaHost(writer,420,160);
        var strip=new DateMonthStrip(new(2023,1,1),new(2025,12,1),new(2024,2,1),
            month=>new Border { Background=new SolidColorBrush(month.Month%2==0?Colors.Red:Colors.Blue) },
            _=>{},()=>new SpringSpec<float>(1,1600,.01f)) {
            Width=c.Width,Height=112,Margin=new(16),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,
            FlowDirection=c.Rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight
        };
        host.Mount(new Grid {Children={strip}});host.Frame(0);
        void Sample(string id)=>writer.Submit(host.Snapshot(id,id,[new ProbePoint("months","date-month-strip",null,[
            ..AvaloniaMeasurements.Geometry(strip,host.Window).Where(m=>m.Name is "box" or "size"),
            Measurement.Numeric("index",(strip.FirstVisibleMonth.Year-2023)*12+strip.FirstVisibleMonth.Month-1,"item",definition:"date-month.first-visible-index"),
            Measurement.Numeric("offset",strip.ScrollOffset,"dip",definition:"date-month.first-visible-offset")])],true));
        Sample("initial");
        host.AdvanceTo(16000);
        if(c.Action.StartsWith("snap"))
        {
            strip.Consume(MathF.Round((float)(c.Width*.3*4)));host.Draw();
            _=strip.SettleAsync(c.Action=="snap-fast"?600*4:c.Action=="snap-back"?-600*4:0);
        }
        else _=strip.AnimateToAsync(new(2024,c.Action=="previous"?1:3,1));
        for(long t=16000;t<=816000;t+=16000)
        {
            host.AdvanceTo(t);
            if(t==96000 && c.Action=="reverse")_=strip.AnimateToAsync(new(2024,1,1));
            host.Frame(t);
            if(t<=256000 || t is 400000 or 816000)Sample($"time-{t}");
        }
    }
}

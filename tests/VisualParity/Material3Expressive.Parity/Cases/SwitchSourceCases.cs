using Avalonia;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

[ParitySuite("switch-source")]
public sealed class SwitchSourceCases
{
    public static IEnumerable<CaseData<SwitchConfig>> Configurations()
    {
        foreach (var state in new[] { "off", "on" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "overlap", "unmatched", "replace", "disable" })
        {
            var id = $"{state}.{(rtl ? "rtl" : "ltr")}.{action}";
            yield return new(id, id, new(State: state, Rtl: rtl, Action: "source-" + action));
        }
    }
    [ParityScenario("source", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config=writer.Metadata.ReadConfig<SwitchConfig>();
        await using var host=new AvaloniaHost(writer,160,128);
        var scene=new SwitchCases.SwitchScene(host,config);
        var source=new MaterialInteractionSource(); SwitchAppearance.SetInteractionSource(scene.Switch,source);
        host.Mount(CaseDefaults.Center(scene.Switch)); host.Frame(-1000); host.Frame(0);
        void Sample(string id)=>writer.Submit(host.Snapshot(id,id,scene.Measure()));
        Sample("initial"); long next=16000;
        void Advance(long time){while(next<=time){host.Frame(next);next+=16000;}host.AdvanceTo(time);}
        void At(long time){Advance(time);host.Draw();Sample($"time-{time}");}
        void Change(long time,string id,Action action){Advance(time);action();writer.Record(time,"source",id);host.Draw();host.Frame(time);Sample(id);}
        var first=new MaterialInteractionEvent.Press(new Point(26,24));
        var second=new MaterialInteractionEvent.Press(new Point(26,24));
        Change(1000,"press",()=>source.Emit(first)); At(16000); At(64000);
        switch(config.Action)
        {
            case "source-overlap":
                Change(65000,"second-press",()=>source.Emit(second));At(80000);
                Change(129000,"first-release",()=>source.Emit(new MaterialInteractionEvent.Release(first)));At(160000);
                Change(193000,"last-cancel",()=>source.Emit(new MaterialInteractionEvent.CancelPress(second)));break;
            case "source-unmatched":
                Change(65000,"unmatched-release",()=>source.Emit(new MaterialInteractionEvent.Release(second)));At(80000);
                Change(129000,"negative-count",()=>source.Emit(new MaterialInteractionEvent.CancelPress(first)));At(160000);
                Change(193000,"count-zero",()=>source.Emit(second));At(208000);
                Change(257000,"count-positive",()=>source.Emit(first));break;
            case "source-replace":
                var old=source;
                Change(65000,"replace",()=>SwitchAppearance.SetInteractionSource(scene.Switch,source=new()));At(80000);
                Change(129000,"old-release",()=>old.Emit(new MaterialInteractionEvent.Release(first)));At(160000);
                Change(193000,"new-press",()=>source.Emit(second));At(208000);
                Change(257000,"old-press",()=>old.Emit(first));break;
            case "source-disable":
                Change(65000,"disable",()=>scene.Switch.IsEnabled=false);At(80000);
                Change(129000,"release",()=>source.Emit(new MaterialInteractionEvent.Release(first)));At(160000);
                Change(193000,"enable",()=>scene.Switch.IsEnabled=true);break;
        }
        At(272000);At(320000);At(400000);At(800000);
    }
}

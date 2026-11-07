namespace Material3Expressive.Parity;

[ParitySuite("tooltip-shape")]
public sealed class TooltipShapeCases
{
    public static IEnumerable<CaseData<TooltipBoxConfig>> Configurations()
    {
        foreach(var rich in new[]{false,true})
        foreach(var position in new[]{"Above","Below","Start","End"})
        foreach(var rtl in new[]{false,true})
        foreach(var shape in new[]{"cubic","notch","direction"})
            yield return new($"{shape}.{(rich?"rich":"plain")}.{position.ToLowerInvariant()}.{(rtl?"rtl":"ltr")}",shape,
                new(Position:position,Rtl:rtl,Rich:rich,Elevation:6,CaretShape:shape));
        foreach(var rich in new[]{false,true})
        foreach(var rtl in new[]{false,true})
        foreach(var shape in new[]{"arc","hole","empty"})
            yield return new($"{shape}.{(rich?"rich":"plain")}.{(rtl?"rtl":"ltr")}",shape,new(Rich:rich,Rtl:rtl,Elevation:6,CaretShape:shape));
        foreach(var rich in new[]{false,true})
        foreach(var shape in new[]{"cubic","notch","direction"})
            yield return new($"resize.{shape}.{(rich?"rich":"plain")}",shape,new(Rich:rich,Elevation:6,CaretShape:shape,Action:"resize"));
    }
    [ParityScenario("shape",Data=nameof(Configurations))]
    public Task Shape(CaptureWriter writer)=>TooltipBoxCases.Capture(writer,writer.Metadata.ReadConfig<TooltipBoxConfig>().Action!="none");
}

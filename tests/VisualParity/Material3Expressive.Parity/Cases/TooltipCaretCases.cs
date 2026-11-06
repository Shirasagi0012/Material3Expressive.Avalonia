using Material3Expressive.Components;

namespace Material3Expressive.Parity;

[ParitySuite("tooltip-caret")]
public sealed class TooltipCaretCases
{
    public static IEnumerable<CaseData<TooltipBoxConfig>> Configurations()
    {
        foreach (var rich in new[] { false, true })
        foreach (var position in Enum.GetNames<TooltipAnchorPosition>())
        foreach (var rtl in new[] { false, true })
        foreach (var point in new[] { (112, 80), (0, 0), (232, 168) })
            yield return Data($"static.{(rich ? "rich" : "plain")}.{position.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{point.Item1}-{point.Item2}",
                new(Position: position, X: point.Item1, Y: point.Item2, Rtl: rtl, Rich: rich, Elevation: rich ? 3 : 0, Caret: true));
        foreach (var rich in new[] { false, true })
        {
            var kind = rich ? "rich" : "plain";
            yield return Data($"custom-size.{kind}", new(Rich: rich, Caret: true, CaretWidth: 28, CaretHeight: 12));
            yield return Data($"dark.{kind}", new(Rich: rich, Caret: true, Dark: true, Elevation: 6));
            yield return Data($"wide.{kind}", new(Rich: rich, Caret: true, ContentWidth: 300));
            foreach (var action in new[] { "resize", "show", "interrupt" })
                yield return Data($"dynamic.{kind}.{action}", new(Rich: rich, Caret: true, Action: action, Elevation: rich ? 3 : 0));
        }
    }
    private static CaseData<TooltipBoxConfig> Data(string id, TooltipBoxConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("caret", Data = nameof(Configurations))]
    public Task Capture(CaptureWriter writer) => TooltipBoxCases.Capture(writer,
        writer.Metadata.ReadConfig<TooltipBoxConfig>().Action != "none");
}

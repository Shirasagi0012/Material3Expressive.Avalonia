namespace Material3Expressive.Parity;

[ParitySuite("bottom-sheet-semantics")]
public sealed class BottomSheetSemanticsCases
{
    public static IEnumerable<CaseData<BottomSheetConfig>> Configurations()
    {
        foreach (var kind in new[] { "plain", "modal", "scaffold" })
        foreach (var action in new[] { "expand", "collapse", "dismiss", "dismiss-disabled", "dismiss-veto" })
            yield return new($"dynamic.{kind}.{action}", "Invoke actual accessibility action", new(Initial: action == "collapse" ? "Expanded" : "PartiallyExpanded",
                Action: "semantics-" + action, Gestures: action != "dismiss-disabled", Veto: action == "dismiss-veto", Modal: kind == "modal", Scaffold: kind == "scaffold"));
    }
    [ParityScenario("behavior", Data = nameof(Configurations))]
    public Task Behavior(CaptureWriter writer) => new BottomSheetCases().Behavior(writer);
}

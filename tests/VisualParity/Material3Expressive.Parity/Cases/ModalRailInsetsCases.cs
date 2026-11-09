namespace Material3Expressive.Parity;

[ParitySuite("modal-rail-insets")]
public sealed class ModalRailInsetsCases
{
    public static IEnumerable<CaseData<ModalWideNavigationRailConfig>> StaticCases()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var own in new[] { false, true })
            yield return new($"static.scope.{(expanded ? "modal" : "inline")}.{(own ? "own" : "ancestor")}",
                "Dialog uses a new consumed-insets root", new(Expanded: expanded, Header: true, Insets: true,
                    ParentInsets: true, OwnInsets: own, NestedInsets: true, ObserveInsets: true));
        foreach (var hide in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var own in new[] { "ancestor", "padding", "insets" })
            yield return new($"static.expanded.{(hide ? "hidden" : "retained")}.{(rtl ? "rtl" : "ltr")}.{own}",
                "Dialog replays caller modifiers in a fresh layout root", new(Hide: hide, Expanded: true, Header: true, Rtl: rtl,
                    Insets: true, Width: 220, ParentInsets: true, OwnInsets: own == "insets", OwnPadding: own == "padding", NestedInsets: true, ObserveInsets: true));
        foreach (var rtl in new[] { false, true })
        foreach (var width in new double?[] { null, 220 })
            yield return new($"static.inline.{(rtl ? "rtl" : "ltr")}.{(width is null ? "natural" : "width")}",
                "Inline scope retains declaration ancestors", new(Header: true, Rtl: rtl, Width: width, Insets: true,
                    ParentInsets: true, OwnPadding: true, NestedInsets: true, ObserveInsets: true));
    }
    public static IEnumerable<CaseData<ModalWideNavigationRailConfig>> DynamicCases()
    {
        foreach (var hide in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.reopen.{(hide ? "hidden" : "retained")}.{(rtl ? "rtl" : "ltr")}",
                "Insets follow actual layout roots across collapse and reopen", new(Hide: hide, Header: true, Rtl: rtl,
                    Insets: true, ParentInsets: true, OwnPadding: rtl, NestedInsets: true, ObserveInsets: true, Action: "reopen"));
        foreach (var mode in new[] { "inline", "modal", "hidden" })
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.change.{mode}.{(rtl ? "rtl" : "ltr")}", "Live physical insets and logical consumption changes",
                new(Hide: mode == "hidden", Expanded: mode != "inline", Header: true, Rtl: rtl, Insets: true,
                    ParentInsets: true, OwnPadding: true, NestedInsets: true, ObserveInsets: true, Action: "insets-change"));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new ModalWideNavigationRailCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicCases))]
    public Task Behavior(CaptureWriter writer) => new ModalWideNavigationRailCases().Behavior(writer);
}

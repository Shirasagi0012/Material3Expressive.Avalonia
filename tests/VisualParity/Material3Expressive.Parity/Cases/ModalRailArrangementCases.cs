namespace Material3Expressive.Parity;

[ParitySuite("modal-rail-arrangement")]
public sealed class ModalRailArrangementCases
{
    public static IEnumerable<CaseData<ModalWideNavigationRailConfig>> StaticCases()
    {
        foreach (var hide in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var arrangement in new[] { "custom-inset", "custom-reverse" })
            yield return new($"static.{hide}.{rtl}.{arrangement}".ToLowerInvariant(), "Custom arrangement transferred to modal rail",
                new(Hide: hide, Expanded: true, Header: true, Rtl: rtl, Arrangement: arrangement));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new ModalWideNavigationRailCases().Static(writer);
}

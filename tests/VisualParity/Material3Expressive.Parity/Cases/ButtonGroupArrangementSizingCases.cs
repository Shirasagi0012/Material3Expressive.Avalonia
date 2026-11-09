namespace Material3Expressive.Parity;

// Additional natural-width coverage: fixed-width overflow cases remain in ButtonGroupCases,
// including the pinned Compose constraint exception. Do not erase that evidence.
[ParitySuite("button-group-arrangement-sizing")]
public sealed class ButtonGroupArrangementSizingCases
{
    public static IEnumerable<CaseData<ButtonGroupConfig>> Configurations() => ButtonGroupCases.StaticConfigurations()
        .Where(c => c.Id.StartsWith("static.arrangement.", StringComparison.Ordinal))
        .Select(c => c with { Config = c.Config with { FixedWidth = false } });
    public static IEnumerable<CaseData<ButtonGroupConfig>> ExistingConfigurations() => ButtonGroupCases.StaticConfigurations()
        .Where(c => !c.Id.StartsWith("static.arrangement.", StringComparison.Ordinal));
    [ParityScenario("existing", Data = nameof(ExistingConfigurations))]
    public Task Existing(CaptureWriter writer) => new ButtonGroupCases().Static(writer);
    [ParityScenario("static", Data = nameof(Configurations))]
    public Task Static(CaptureWriter writer) => new ButtonGroupCases().Static(writer);
}

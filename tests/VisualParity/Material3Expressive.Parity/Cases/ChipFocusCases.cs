namespace Material3Expressive.Parity;

[ParitySuite("chip-focus")]
public sealed class ChipFocusCases
{
    public static IEnumerable<CaseData<FocusRingConfig>> Configurations()
    {
        foreach (var kind in new[] { "assist-chip", "elevated-assist-chip", "suggestion-chip", "elevated-suggestion-chip", "filter-chip", "elevated-filter-chip", "input-chip" })
        foreach (var morph in kind.Contains("filter") || kind == "input-chip" ? new[] { false, true } : new[] { false })
        {
            var variant = $"{kind}.{(morph ? "morph" : "fixed")}";
            foreach (var action in new[] { "focus", "reverse", "disable", "focus-mode", "source-replace", "detach" })
                yield return new($"dynamic.{variant}.{action}", "Chip focus ring and lifecycle", new(Kind: kind, Action: action, Morph: morph));
            yield return new($"dynamic.{variant}.dark", "Chip dark focus ring", new(Kind: kind, Dark: true, Morph: morph));
            if (morph)
            {
                yield return new($"dynamic.{variant}.selected", "Selected chip shape and focus ring", new(Kind: kind, Checked: true, Morph: true));
                yield return new($"dynamic.{variant}.press-to-ring", "Ring mode during chip shape animation", new(Kind: kind, Action: "press-to-ring", Morph: true));
            }
        }
    }

    [ParityScenario("behavior", Data = nameof(Configurations))]
    public Task Behavior(CaptureWriter writer) => new FocusRingCases().Behavior(writer);
}

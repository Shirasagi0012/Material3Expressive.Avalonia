namespace Material3Expressive.Parity;

[ParitySuite("surface-focus")]
public sealed class SurfaceFocusCases
{
    public static IEnumerable<CaseData<FocusRingConfig>> Configurations()
    {
        foreach (var kind in new[] { "click-surface", "select-surface", "toggle-surface" })
        {
            foreach (var action in new[] { "focus", "reverse", "disable", "focus-mode", "source-replace", "detach", "press-to-ring", "press-to-opacity" })
                yield return new($"dynamic.{kind}.{action}", "Interactive surface focus indication lifecycle", new(Kind: kind, Action: action));
            yield return new($"dynamic.{kind}.dark", "Surface dark focus ring", new(Kind: kind, Dark: true));
            yield return new($"dynamic.{kind}.custom", "Surface custom ring colors and metrics", new(Kind: kind, Custom: true));
        }
    }

    [ParityScenario("behavior", Data = nameof(Configurations))]
    public Task Behavior(CaptureWriter writer) => new FocusRingCases().Behavior(writer);
}

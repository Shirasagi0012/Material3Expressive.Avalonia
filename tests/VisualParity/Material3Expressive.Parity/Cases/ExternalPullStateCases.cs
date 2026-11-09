namespace Material3Expressive.Parity;

[ParitySuite("external-pull-state")]
public sealed class ExternalPullStateCases
{
    public static IEnumerable<CaseData<PullToRefreshConfig>> Cases()
    {
        foreach (var kind in new[] { "circular", "loading", "custom" })
        foreach (var action in new[] { "progress", "state", "refresh", "reverse", "conflation", "external-replace" })
            yield return new($"{kind}.{action}", "External observable state, replacement and refresh transitions",
                new(kind, .8, Action: action, ExternalState: true));
    }
    [ParityScenario("indicator", Data = nameof(Cases))]
    public Task Capture(CaptureWriter writer) => new PullToRefreshCases().Capture(writer);
}

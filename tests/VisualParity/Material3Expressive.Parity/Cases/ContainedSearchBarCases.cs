namespace Material3Expressive.Parity;

[ParitySuite("contained-search-bar")]
public sealed class ContainedSearchBarCases
{
    public static IEnumerable<CaseData<ExpandedSearchBarConfig>> Configurations() =>
        FullScreenSearchBarCases.Configurations().Select(c => c with { Config = c.Config with { Contained = true } });

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public Task Capture(CaptureWriter writer) => new ExpandedSearchBarCases().Capture(writer);
}

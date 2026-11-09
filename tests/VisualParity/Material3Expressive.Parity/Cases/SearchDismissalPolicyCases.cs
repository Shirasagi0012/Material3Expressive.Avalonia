namespace Material3Expressive.Parity;

[ParitySuite("search-dismissal-policy")]
public sealed class SearchDismissalPolicyCases
{
    public static IEnumerable<CaseData<ExpandedSearchBarConfig>> Configurations()
    {
        foreach (var variant in new[] { "docked", "gap", "full-screen", "contained" })
        foreach (var policy in new[] { "enabled", "disabled", "enable-later" })
            yield return new($"escape.{variant}.{policy}", "real Escape with configurable popup or dialog dismissal",
                new(Action: "escape", Gap: variant == "gap", FullScreen: variant == "full-screen", Contained: variant == "contained",
                    DismissOnEscape: policy == "enabled", EnableDismissalAfterAttempt: policy == "enable-later"));
        foreach (var variant in new[] { "docked", "gap-no-scrim", "gap-scrim" })
        foreach (var policy in new[] { "enabled", "disabled", "enable-later" })
            yield return new($"outside.{variant}.{policy}", "outside popup presses remain separate from the internal clickable scrim",
                new(Action: "outside", Gap: variant != "docked", NoScrim: variant == "gap-no-scrim",
                    DismissOnPointerOutside: policy == "enabled", EnableDismissalAfterAttempt: policy == "enable-later"));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public Task Capture(CaptureWriter writer) => new ExpandedSearchBarCases().Capture(writer);
}

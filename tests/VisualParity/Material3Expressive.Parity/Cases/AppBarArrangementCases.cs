namespace Material3Expressive.Parity;

[ParitySuite("appbar-arrangement")]
public sealed class AppBarArrangementCases
{
    public static IEnumerable<CaseData<AppBarConfig>> StaticCases()
    {
        foreach (var arrangement in new[] { "spaced-center", "spaced-end", "absolute.left", "absolute.right", "absolute.center",
                     "absolute.spacebetween", "absolute.spacearound", "absolute.spaceevenly", "absolute-spaced", "custom" })
        foreach (var rtl in new[] { false, true })
        foreach (var width in new[] { 133d, 361d })
            yield return new($"static.{arrangement}.{(rtl ? "rtl" : "ltr")}.{width}", "Arbitrary bottom bar arrangements",
                new("flexibleBottom", Arrangement: arrangement, Rtl: rtl, Width: width, Actions: 3, Spacing: 7.25));
        foreach (var count in new[] { 0, 1 })
        foreach (var rtl in new[] { false, true })
        foreach (var arrangement in new[] { "spaceBetween", "spaced-center", "absolute.spacebetween", "custom" })
            yield return new($"static.count.{count}.{arrangement.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}", "Empty and singleton rows",
                new("flexibleBottom", Arrangement: arrangement, Rtl: rtl, Actions: count));
        foreach (var rtl in new[] { false, true })
            yield return new($"static.overlap.{(rtl ? "rtl" : "ltr")}", "Negative spacing with end alignment",
                new("flexibleBottom", Arrangement: "spaced-end", Rtl: rtl, Actions: 4, Width: 180, Spacing: -8));
    }
    public static IEnumerable<CaseData<AppBarConfig>> DynamicCases()
    {
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.{(rtl ? "rtl" : "ltr")}", "Live arrangement and direction changes",
                new("flexibleBottom", Arrangement: "spaced-center", Rtl: rtl, Actions: 3, Action: "arrangement"));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new AppBarCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicCases))]
    public Task Behavior(CaptureWriter writer) => new AppBarCases().Behavior(writer);
}

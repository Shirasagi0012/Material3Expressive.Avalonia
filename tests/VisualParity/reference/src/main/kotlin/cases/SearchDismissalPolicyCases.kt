package parity

@ParitySuite("search-dismissal-policy")
class SearchDismissalPolicyCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) = ExpandedSearchBarCases().capture(writer)
}

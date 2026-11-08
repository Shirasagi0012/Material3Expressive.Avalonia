package parity

@ParitySuite("search-shape")
class SearchShapeCases {
    @ParityScenario("legacy") suspend fun legacy(writer:CaptureWriter) = DockedSearchBarCases().capture(writer)
    @ParityScenario("presentation") suspend fun presentation(writer:CaptureWriter) = ExpandedSearchBarCases().capture(writer)
    @ParityScenario("ring") suspend fun ring(writer:CaptureWriter) = SearchInputFocusRingCases().capture(writer)
}

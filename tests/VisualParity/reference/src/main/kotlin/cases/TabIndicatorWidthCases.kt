package parity
@ParitySuite("tab-indicator-width")
class TabIndicatorWidthCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = TabRowCases().static(writer)
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) = TabRowCases().behavior(writer)
}

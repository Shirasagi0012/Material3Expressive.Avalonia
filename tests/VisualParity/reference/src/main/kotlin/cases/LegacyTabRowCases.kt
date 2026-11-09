package parity

@ParitySuite("legacy-tab-row")
class LegacyTabRowCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=TabRowCases().static(writer)
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter)=TabRowCases().behavior(writer)
}

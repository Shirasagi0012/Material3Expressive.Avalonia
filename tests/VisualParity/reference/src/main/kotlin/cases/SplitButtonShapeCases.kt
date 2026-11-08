package parity

@ParitySuite("split-button-shape")
class SplitButtonShapeCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = SplitButtonCases().static(writer)
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) = SplitButtonCases().behavior(writer)
}

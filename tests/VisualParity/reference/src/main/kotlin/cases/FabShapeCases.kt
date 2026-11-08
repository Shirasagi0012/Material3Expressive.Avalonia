package parity

@ParitySuite("fab-shape")
class FabShapeCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = FloatingActionButtonCases().static(writer)
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) = FloatingActionButtonCases().behavior(writer)
}

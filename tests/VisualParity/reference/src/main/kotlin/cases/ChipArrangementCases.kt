package parity

@ParitySuite("chip-arrangement")
class ChipArrangementCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = ChipCases().static(writer)
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter) = ChipCases().behavior(writer)
}

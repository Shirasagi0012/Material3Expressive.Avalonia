package parity

@ParitySuite("chip-focus")
class ChipFocusCases {
    @ParityScenario("behavior") suspend fun behavior(writer: CaptureWriter) = FocusRingCases().behavior(writer)
}

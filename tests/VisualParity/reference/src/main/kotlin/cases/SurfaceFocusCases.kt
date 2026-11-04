package parity

@ParitySuite("surface-focus")
class SurfaceFocusCases {
    @ParityScenario("behavior") suspend fun behavior(writer: CaptureWriter) = FocusRingCases().behavior(writer)
}

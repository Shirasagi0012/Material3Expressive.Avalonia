package parity

@ParitySuite("combo-box-selection")
class ComboBoxSelectionCases {
    @ParityScenario("static") suspend fun static(writer: CaptureWriter) = MenuCases().capture(writer, false)
    @ParityScenario("dynamic") suspend fun dynamic(writer: CaptureWriter) = MenuCases().capture(writer, true)
}

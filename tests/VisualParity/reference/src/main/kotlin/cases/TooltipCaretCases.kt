package parity

@ParitySuite("tooltip-caret")
class TooltipCaretCases {
    @ParityScenario("caret") suspend fun capture(writer: CaptureWriter) =
        TooltipBoxCases().capture(writer, writer.readConfig<TooltipBoxConfig>().action != "none")
}

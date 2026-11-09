package parity
@ParitySuite("button-group-arrangement-sizing")
class ButtonGroupArrangementSizingCases {
    @ParityScenario("existing")
    suspend fun existing(writer:CaptureWriter) = ButtonGroupCases().static(writer)
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = ButtonGroupCases().static(writer)
}

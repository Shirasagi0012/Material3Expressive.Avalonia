package parity

@ParitySuite("tooltip-shape")
class TooltipShapeCases {
    @ParityScenario("shape") suspend fun shape(writer:CaptureWriter)=TooltipBoxCases().capture(writer,writer.readConfig<TooltipBoxConfig>().action!="none")
}

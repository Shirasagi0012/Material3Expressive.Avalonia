package parity

@ParitySuite("toolbar-shape")
class ToolbarShapeCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = FloatingToolbarCases().static(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) = FloatingToolbarCases().dynamic(writer)
}

package parity

@ParitySuite("icon-button-content")
class IconButtonContentCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = MaterialShapeCases().static(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) = MaterialShapeCases().dynamic(writer)
}

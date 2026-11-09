package parity
@ParitySuite("native-menu-shapes")
class NativeMenuShapeCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = MenuCases().static(writer)
    @ParityScenario("dynamic")
    suspend fun dynamic(writer:CaptureWriter) = MenuCases().dynamic(writer)
}

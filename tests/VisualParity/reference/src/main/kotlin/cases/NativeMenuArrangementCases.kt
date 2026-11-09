package parity
@ParitySuite("native-menu-arrangement")
class NativeMenuArrangementCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = MenuArrangementCases().static(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) = MenuArrangementCases().dynamic(writer)
}

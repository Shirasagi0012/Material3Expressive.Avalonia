package parity

@ParitySuite("appbar-arrangement")
class AppBarArrangementCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = AppBarCases().static(writer)
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter) = AppBarCases().behavior(writer)
}

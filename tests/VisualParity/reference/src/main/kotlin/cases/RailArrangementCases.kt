package parity

@ParitySuite("rail-arrangement")
class RailArrangementCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=WideNavigationRailCases().static(writer)
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter)=WideNavigationRailCases().behavior(writer)
}

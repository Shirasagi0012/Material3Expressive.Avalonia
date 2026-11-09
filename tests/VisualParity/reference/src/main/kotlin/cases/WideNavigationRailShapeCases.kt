package parity
@ParitySuite("wide-navigation-rail-shapes")
class WideNavigationRailShapeCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = WideNavigationRailCases().static(writer)
    @ParityScenario("dynamic")
    suspend fun dynamic(writer:CaptureWriter) = WideNavigationRailCases().behavior(writer)
}

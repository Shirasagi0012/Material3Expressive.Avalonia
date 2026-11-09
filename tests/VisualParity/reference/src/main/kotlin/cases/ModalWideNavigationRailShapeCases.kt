package parity
@ParitySuite("modal-wide-navigation-rail-shapes")
class ModalWideNavigationRailShapeCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = ModalWideNavigationRailCases().static(writer)
    @ParityScenario("dynamic")
    suspend fun dynamic(writer:CaptureWriter) = ModalWideNavigationRailCases().behavior(writer)
}

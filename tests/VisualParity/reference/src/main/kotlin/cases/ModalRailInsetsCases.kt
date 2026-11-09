package parity

@ParitySuite("modal-rail-insets")
class ModalRailInsetsCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=ModalWideNavigationRailCases().static(writer)
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter)=ModalWideNavigationRailCases().behavior(writer)
}

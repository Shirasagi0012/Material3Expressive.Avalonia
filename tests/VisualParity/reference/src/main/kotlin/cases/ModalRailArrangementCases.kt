package parity

@ParitySuite("modal-rail-arrangement")
class ModalRailArrangementCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=ModalWideNavigationRailCases().static(writer)
}

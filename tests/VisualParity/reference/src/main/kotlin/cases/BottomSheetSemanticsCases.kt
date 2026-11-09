package parity

@ParitySuite("bottom-sheet-semantics")
class BottomSheetSemanticsCases {
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) = BottomSheetCases().behavior(writer)
}

package parity

@ParitySuite("rounded-shape")
class RoundedShapeCases {
    @ParityScenario("list") suspend fun list(writer:CaptureWriter) = ListItemCases().behavior(writer)
    @ParityScenario("menu") suspend fun menu(writer:CaptureWriter) = DropdownMenuItemCases().dynamic(writer)
    @ParityScenario("split") suspend fun split(writer:CaptureWriter) = SplitButtonCases().behavior(writer)
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = MaterialShapeCases().static(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) = MaterialShapeCases().dynamic(writer)
}

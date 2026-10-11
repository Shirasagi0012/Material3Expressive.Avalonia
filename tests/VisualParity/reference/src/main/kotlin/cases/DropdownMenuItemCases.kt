package parity

@ParitySuite("dropdown-menu-item")
class DropdownMenuItemCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=MenuCases().capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=MenuCases().capture(writer,true)
}

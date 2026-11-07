package parity

@ParitySuite("precision-menu-item")
class PrecisionMenuItemCases {
    @ParityScenario("sizing") suspend fun sizing(writer:CaptureWriter)=
        MenuCases().capture(writer,writer.readConfig<MenuConfig>().action!="none")
}

@ParitySuite("dropdown-menu-item")
class DropdownMenuItemCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=MenuCases().capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=MenuCases().capture(writer,true)
}

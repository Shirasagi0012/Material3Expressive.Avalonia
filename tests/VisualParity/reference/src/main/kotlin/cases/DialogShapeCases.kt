package parity

@ParitySuite("dialog-shape")
class DialogShapeCases {
    @ParityScenario("alert") suspend fun alert(writer:CaptureWriter) =
        if(writer.readConfig<AlertDialogConfig>().action=="none") AlertDialogCases().static(writer) else AlertDialogCases().dynamic(writer)
    @ParityScenario("picker") suspend fun picker(writer:CaptureWriter) =
        if(writer.readConfig<PickerDialogConfig>().action=="none") TimePickerDialogCases().static(writer) else TimePickerDialogCases().dynamic(writer)
}

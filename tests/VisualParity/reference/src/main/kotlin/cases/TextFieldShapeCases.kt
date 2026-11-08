package parity

@ParitySuite("text-field-shape")
class TextFieldShapeCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=TextFieldCases().static(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=TextFieldCases().changes(writer)
    @ParityScenario("secure") suspend fun secure(writer:CaptureWriter)=SecureTextFieldCases().static(writer)
}

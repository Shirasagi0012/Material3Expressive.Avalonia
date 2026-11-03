package parity

// A template-only Avalonia slot represents the same non-null Compose content lambda.
@ParitySuite("floating-toolbar-template")
class FloatingToolbarTemplateCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=FloatingToolbarCases().static(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=FloatingToolbarCases().dynamic(writer)
}

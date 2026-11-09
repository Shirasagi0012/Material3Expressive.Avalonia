package parity

@ParitySuite("external-pull-state")
class ExternalPullStateCases {
    @ParityScenario("indicator") suspend fun capture(writer:CaptureWriter)=PullToRefreshCases().capture(writer)
}

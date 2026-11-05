@file:OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class, kotlinx.coroutines.InternalCoroutinesApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.runtime.*
import kotlinx.coroutines.*
import kotlinx.coroutines.test.*
import kotlinx.serialization.json.JsonObject
import java.io.File

// Run against the real FrameRecomposer and its effect dispatcher, independently of any component.
// No additional test dependency is needed by the locked producer.
fun main() {
    ReferenceTimeoutClock.initialize()
    verifyNativeShadowCurves()
    val writer = CaptureWriter(CaseMetadata(4, "host.clock", "Host clock", "host", "clock", JsonObject(emptyMap())),
        File("unused-input"), File("unused-output"), File("."))
    val events = mutableListOf<String>()
    var previous: ComposeHost? = null
    repeat(2) {
        val host = ComposeHost(writer, 16, 16)
        host.use {
            lateinit var effects: CoroutineScope
            host.mount { effects = rememberCoroutineScope() }
            host.frame(0)
            var backgroundExpired = false
            val backgroundTimeout = ReferenceTimeoutClock.invokeOnTimeout(100_000,
                Runnable { backgroundExpired = true }, Dispatchers.IO)
            fun record(name: String) { events += "$name@${host.scheduler.currentTime}" }
            effects.launch {
                delay(50); record("delay")
                delay(50); record("second-delay")
            }
            val timeout = effects.launch {
                try { withTimeout(100) { awaitCancellation() } }
                catch (_: TimeoutCancellationException) { record("timeout") }
            }
            val cancelled = effects.launch {
                try { withTimeout(75) { awaitCancellation() } }
                finally { record("cancelled") }
            }
            effects.launch { withTimeout(200) { delay(25) }; record("completed") }
            effects.launch { withFrameNanos { record("frame") } }
            host.draw()
            host.snapshot("passive", "passive", emptyList(), image = false)
            check(events.isEmpty()) { "Draw/Snapshot advanced time: $events" }
            host.advanceTo(49_999)
            check(events == listOf("completed@25")) { "Early delay/timeout: $events" }
            cancelled.cancel()
            host.scheduler.runCurrent()
            host.advanceTo(99_999)
            check(events == listOf("completed@25", "cancelled@49", "delay@50")) { "Cancellation/order: $events" }
            check(!timeout.isCompleted && host.frameTimeUs == 0L)
            host.advanceTo(100_000)
            check(timeout.isCompleted) { "FrameRecomposer withTimeout did not use the host scheduler: $events" }
            check(events == listOf("completed@25", "cancelled@49", "delay@50", "timeout@100", "second-delay@100")) {
                "Same-deadline registration order: $events"
            }
            host.frame(100_000)
            check(events.last() == "frame@100") { "Timeout and frame clocks were coupled: $events" }
            host.advanceTo(250_000)
            check(events.size == 6) { "Cancelled/completed timeout fired again: $events" }
            // A large virtual jump must not drive Skiko/IO wall-clock tasks.
            host.advanceTo(200_250_000)
            check(!backgroundExpired) { "Background timeout was captured by the component clock." }
            backgroundTimeout.dispose()
            effects.launch {
                try { withTimeout(100) { awaitCancellation() } }
                finally { record("dispose") }
            }
        }
        check(events.last() == "dispose@200250") { "Host close did not cancel composition work: $events" }
        val afterClose = events.toList()
        host.scheduler.advanceUntilIdle()
        previous?.scheduler?.advanceUntilIdle()
        check(events == afterClose) { "Closed host timers leaked into the next host: $events" }
        check(writer.errors.isEmpty()) { writer.errors.joinToString("\n") }
        previous = host
        events.clear()
        // Background registration remains valid between cases, with no active Compose host.
        ReferenceTimeoutClock.invokeOnTimeout(100_000, Runnable { error("Disposed timeout fired") }, Dispatchers.IO).dispose()
    }
    println("ComposeHost clock tests passed: delay, timeout, cancellation, passive reads, frame order, disposal, sequential hosts.")
}

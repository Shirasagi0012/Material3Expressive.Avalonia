@file:OptIn(kotlinx.coroutines.InternalCoroutinesApi::class, kotlinx.coroutines.ExperimentalCoroutinesApi::class)
@file:Suppress("INVISIBLE_REFERENCE", "INVISIBLE_MEMBER")
package parity

import kotlin.coroutines.ContinuationInterceptor
import kotlin.coroutines.CoroutineContext
import kotlinx.coroutines.*
import kotlinx.coroutines.test.*

// Pinned coroutines 1.9.0: Delay's default timeout uses DefaultDelay. Fixed Core's
// FlushCoroutineDispatcher inherits it, although its ordinary delay uses the host scope.
// Select the test scheduler only for Compose; Skiko's Dispatchers.IO FrameWatcher and
// other background work must keep DefaultExecutor, including between sequential hosts.
internal object ReferenceTimeoutClock : CoroutineDispatcher(), Delay {
    private var dispatcher: TestDispatcher? = null

    fun initialize() {
        // Must precede Skia loading and runBlocking: DefaultDelay is initialized once.
        System.setProperty("kotlinx.coroutines.main.delay", "true")
        Dispatchers.setMain(this)
    }

    fun attach(value: TestDispatcher) {
        check(dispatcher == null) { "Compose reference hosts must run sequentially." }
        dispatcher = value
    }

    fun detach() { dispatcher = null }

    private fun clock(context: CoroutineContext): Delay {
        val interceptor = context[ContinuationInterceptor]
        return if (interceptor?.javaClass?.name == "androidx.compose.ui.platform.FlushCoroutineDispatcher" ||
            interceptor === Dispatchers.Main || interceptor === this)
            checkNotNull(dispatcher) { "Compose timeout scheduled outside its host lifetime." }
        else DefaultExecutor
    }

    override fun scheduleResumeAfterDelay(timeMillis: Long, continuation: CancellableContinuation<Unit>) =
        clock(continuation.context).scheduleResumeAfterDelay(timeMillis, continuation)

    override fun invokeOnTimeout(timeMillis: Long, block: Runnable, context: CoroutineContext): DisposableHandle =
        clock(context).invokeOnTimeout(timeMillis, block, context)

    override fun isDispatchNeeded(context: CoroutineContext) = checkNotNull(dispatcher).isDispatchNeeded(context)
    override fun dispatch(context: CoroutineContext, block: Runnable) = checkNotNull(dispatcher).dispatch(context, block)
}

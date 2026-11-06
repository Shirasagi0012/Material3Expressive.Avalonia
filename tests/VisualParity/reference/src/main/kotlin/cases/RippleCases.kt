@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.collection.ScatterMap
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.util.lerp
import kotlinx.serialization.Serializable

@Serializable
data class RippleConfig(val width: Double = 97.0, val height: Double = 53.0, val action: String = "hold")

@ParitySuite("ripple")
class RippleCases {
    @ParityScenario("press") suspend fun capture(writer: CaptureWriter) {
        val config = writer.readConfig<RippleConfig>()
        ComposeHost(writer, 280, 160).use { host ->
            val source = MutableInteractionSource()
            val probe = Probe()
            var attached by mutableStateOf(true)
            host.mount {
                MaterialTheme {
                    Box(Modifier.fillMaxSize().background(Color.White), contentAlignment = Alignment.Center) {
                        if (attached) Surface(onClick = {}, modifier = probe.modifier().size(config.width.toFloat().dp, config.height.toFloat().dp),
                            color = Color.White, contentColor = Color.Black, interactionSource = source) {}
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            var next = 16000L
            fun advance(time: Long) { while (next <= time) { host.frame(next); next += 16000 }; host.advanceTo(time) }
            fun sample(id: String) {
                val ripples = mutableListOf<Any>()
                if (attached) {
                    val node = checkNotNull(field(checkNotNull(probe.coordinates), "layoutNode"))
                    val visited = java.util.Collections.newSetFromMap(java.util.IdentityHashMap<Any, Boolean>())
                    fun visit(current: Any?) {
                        if (current == null || !visited.add(current)) return
                        if (current.javaClass.simpleName == "CommonRippleNode") {
                            (field(current, "ripples") as ScatterMap<*, *>).forEach { _, value -> ripples.add(checkNotNull(value)) }
                        }
                        visit(runCatching { field(current, "delegate") }.getOrNull())
                        visit(runCatching { field(current, "child") }.getOrNull())
                    }
                    visit(field(checkNotNull(field(node, "nodes")), "head"))
                }
                // Map iteration is unspecified. Press origins identify the two overlapping waves.
                ripples.sortBy { (field(it, "origin") as Offset).x }
                fun value(ripple: Any, name: String): Float {
                    val animation = checkNotNull(field(ripple, name))
                    return (animation.javaClass.methods.single { it.name == "getValue" && it.parameterCount == 0 }.invoke(animation) as Number).toFloat()
                }
                val points = mutableListOf(ProbePoint("state", "state", null, listOf(number("count", ripples.size.toDouble(), "count"))))
                for ((index, ripple) in ripples.withIndex()) {
                    fun state(name: String) = (field(ripple, name + "\u0024delegate") as State<*>).value
                    val alpha = if (state("finishRequested") == true && state("finishedFadingIn") == false) 1f else value(ripple, "animatedAlpha")
                    val radius = (field(ripple, "radius") as Number).toFloat()
                    val start = (field(ripple, "startRadius") as Number).toFloat()
                    val origin = field(ripple, "origin") as Offset
                    val center = field(ripple, "targetCenter") as Offset
                    val progress = value(ripple, "animatedCenterPercent")
                    val density = ComposeHost.RenderScale.toFloat()
                    points.add(ProbePoint("ripple-$index", "ripple", null, listOf(
                        number("alpha", alpha.toDouble(), "ratio", category = "motion"),
                        number("radius", (lerp(start, radius, value(ripple, "animatedRadiusPercent")) / density).toDouble(), "dip", category = "motion"),
                        number("targetRadius", (radius / density).toDouble(), "dip"),
                        number("centerX", (lerp(origin.x, center.x, progress) / density).toDouble(), "dip", category = "motion"),
                        number("centerY", (lerp(origin.y, center.y, progress) / density).toDouble(), "dip", category = "motion"))))
                }
                writer.submit(host.snapshot(id, id, points, image = id != "time-32000"))
            }
            fun change(time: Long, id: String, action: () -> Unit) {
                advance(time); action(); writer.record(time, "interaction", id); host.scheduler.runCurrent(); host.draw(); sample(id)
            }
            fun times(vararg values: Long) { for (time in values) { advance(time); host.draw(); sample("time-$time") } }
            val press = PressInteraction.Press(Offset(7.25f, 11.5f) * ComposeHost.RenderScale.toFloat())
            sample("initial")
            change(1000, "press") { source.tryEmit(press) }
            if (config.action == "quick") change(5000, "release") { source.tryEmit(PressInteraction.Release(press)) }
            times(16000, 32000, 64000)
            if (config.action == "cancel") change(65000, "cancel") { source.tryEmit(PressInteraction.Cancel(press)) }
            if (config.action == "detach") change(65000, "detach") { attached = false }
            var second: PressInteraction.Press? = null
            if (config.action == "overlap") change(65000, "second") {
                second = PressInteraction.Press(Offset(31.75f, 19.25f) * ComposeHost.RenderScale.toFloat()); source.tryEmit(second!!)
            }
            times(80000, 160000, 240000)
            if (config.action in listOf("hold", "overlap")) change(241000, "release") { source.tryEmit(PressInteraction.Release(second ?: press)) }
            times(256000, 320000, 400000, 640000)
        }
    }
}

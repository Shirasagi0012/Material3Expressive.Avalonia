@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class ButtonGroupSourceConfig(val action: String, val rtl: Boolean)

@ParitySuite("button-group-source")
class ButtonGroupSourceCases {
    @ParityScenario("source")
    suspend fun capture(writer: CaptureWriter) {
        val config = writer.readConfig<ButtonGroupSourceConfig>()
        ComposeHost(writer, 360, 128).use { host ->
            var source by mutableStateOf(MutableInteractionSource())
            var attached by mutableStateOf(true)
            val group = Probe()
            val items = List(3) { Probe() }
            val neighbours = List(3) { MutableInteractionSource() }
            host.mount {
                MaterialTheme(colorScheme = lightColorScheme(), motionScheme = MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if (config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White), contentAlignment = AbsoluteAlignment.TopLeft) {
                            if (attached) ButtonGroup(
                                modifier = Modifier.absoluteOffset(32.dp, 32.dp).then(group.modifier()),
                                expandedRatio = .5f,
                                overflowIndicator = { Box(Modifier.size(32.dp, 40.dp)) }
                            ) {
                                repeat(3) { index ->
                                    customItem(buttonGroupContent = {
                                        Box(Modifier.animateWidth(if (index == 1) source else neighbours[index], 24.dp)
                                            .then(items[index].modifier()).size(80.dp, 40.dp)
                                            .background(listOf(Color.Red, Color.Blue, Color(0xff008000))[index]))
                                    }, menuContent = {})
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            var next = 16000L
            fun advance(time: Long) {
                while (next <= time) { host.frame(next); next += 16000 }
                host.advanceTo(time)
            }
            fun sample(id: String) {
                val points = mutableListOf<ProbePoint>()
                if (attached) {
                    points += ProbePoint("group", "container", null, group.geometry().filter { it.name in setOf("box", "size") })
                    items.forEachIndexed { index, probe ->
                        points += ProbePoint("item-$index", "item", "group", probe.geometry().filter { it.name in setOf("box", "size") })
                    }
                }
                writer.submit(host.snapshot(id, id, points, image = id != "time-32000"))
            }
            fun change(time: Long, id: String, action: () -> Unit) {
                advance(time); action(); writer.record(time, "source", id); host.draw(); host.frame(time); sample(id)
            }
            fun times(vararg times: Long) {
                times.forEach { advance(it); host.draw(); sample("time-$it") }
            }
            val press = PressInteraction.Press(Offset.Zero)
            val second = PressInteraction.Press(Offset.Zero)
            sample("initial")
            change(1000, "press") { check(source.tryEmit(press)) }
            if (config.action == "quick") {
                change(2000, "release") { check(source.tryEmit(PressInteraction.Release(press))) }
                times(16000, 32000, 64000, 96000, 160000, 400000, 800000)
                return
            }
            times(16000, 32000, 64000)
            when (config.action) {
                "multiple" -> {
                    change(65000, "second") { check(source.tryEmit(second)) }
                    change(81000, "first-release") { check(source.tryEmit(PressInteraction.Release(press))) }
                    change(97000, "unmatched") { check(source.tryEmit(PressInteraction.Cancel(press))) }
                    times(160000, 400000)
                    change(401000, "last-cancel") { check(source.tryEmit(PressInteraction.Cancel(second))) }
                    times(416000, 480000, 640000, 1000000)
                }
                "repress" -> {
                    change(65000, "release") { check(source.tryEmit(PressInteraction.Release(press))) }
                    times(96000, 160000)
                    change(161000, "repress") { check(source.tryEmit(second)) }
                    times(176000, 240000, 400000)
                    change(401000, "cancel") { check(source.tryEmit(PressInteraction.Cancel(second))) }
                    times(480000, 640000, 1000000)
                }
                "replace", "replace-release" -> {
                    val old = source
                    change(65000, "replace") { source = MutableInteractionSource() }
                    change(81000, "old-release") { check(old.tryEmit(PressInteraction.Release(press))) }
                    times(160000)
                    change(161000, "first-new-event") { check(source.tryEmit(if (config.action == "replace") second else HoverInteraction.Enter())) }
                    times(176000, 240000, 400000, 800000, 1200000)
                    if (config.action == "replace") change(1201000, "release") { check(source.tryEmit(PressInteraction.Release(second))) }
                    times(1216000, 1400000, 1800000)
                }
                "detach" -> {
                    change(65000, "detach") { attached = false }
                    change(81000, "detached-press") { check(source.tryEmit(second)) }
                    change(97000, "reattach") { attached = true }
                    times(160000)
                    change(161000, "new-press") { check(source.tryEmit(press)) }
                    times(240000, 400000)
                    change(401000, "cancel") { check(source.tryEmit(PressInteraction.Cancel(press))) }
                    times(480000, 640000, 1000000)
                }
            }
        }
    }
}

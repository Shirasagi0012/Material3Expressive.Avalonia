@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class ElevationCommitConfig(val kind: String, val action: String)

@ParitySuite("elevation-commit")
class ElevationCommitCases {
    @ParityScenario("change") suspend fun capture(writer: CaptureWriter) {
        val config = writer.readConfig<ElevationCommitConfig>()
        ComposeHost(writer, 240, 160).use { host ->
            val source = MutableInteractionSource()
            val probe = Probe()
            var enabled by mutableStateOf(true)
            var step by mutableIntStateOf(0)
            host.mount {
                MaterialTheme(colorScheme = lightColorScheme(), motionScheme = MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White), contentAlignment = Alignment.Center) {
                        val modifier = probe.modifier().size(104.dp, 48.dp)
                        val hover = if (step > 0) 7.25f.dp else 5.dp
                        val drag = if (step > 1) 12.5f.dp else 9.dp
                        val disabled = if (step > 1) 4.75f.dp else 1.dp
                        val content: @Composable () -> Unit = { Box(Modifier.size(32.dp, 16.dp).background(Color.Black)) }
                        if (config.kind == "chip") InputChip(selected = false, onClick = {}, label = content,
                            modifier = modifier, enabled = enabled, shape = RoundedCornerShape(8.dp),
                            colors = InputChipDefaults.inputChipColors(containerColor = Color.White, disabledContainerColor = Color.White),
                            elevation = InputChipDefaults.inputChipElevation(elevation = 2.dp, pressedElevation = 3.dp,
                                focusedElevation = 4.dp, hoveredElevation = hover, draggedElevation = drag, disabledElevation = disabled),
                            interactionSource = source)
                        else if (config.kind == "card") Card(onClick = {}, modifier = modifier, enabled = enabled, shape = RoundedCornerShape(8.dp),
                            colors = CardDefaults.cardColors(containerColor = Color.White, disabledContainerColor = Color.White),
                            elevation = CardDefaults.cardElevation(defaultElevation = 2.dp, pressedElevation = 3.dp,
                                focusedElevation = 4.dp, hoveredElevation = hover, draggedElevation = drag, disabledElevation = disabled),
                            interactionSource = source) { content() }
                        else Button(onClick = {}, modifier = modifier, enabled = enabled, shape = RoundedCornerShape(8.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black,
                                disabledContainerColor = Color.White, disabledContentColor = Color.Black),
                            elevation = ButtonDefaults.buttonElevation(defaultElevation = 2.dp, pressedElevation = drag,
                                focusedElevation = 4.dp, hoveredElevation = hover, disabledElevation = disabled),
                            contentPadding = PaddingValues(0.dp), interactionSource = source) { content() }
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            var next = 16000L
            fun advance(time: Long) { while (next <= time) { host.frame(next); next += 16000 }; host.advanceTo(time) }
            fun sample(id: String) {
                var coordinator: Any? = probe.coordinates
                var elevation = 0.0
                while (coordinator != null) {
                    field(coordinator, "layer")?.let { layer ->
                        val graphics = checkNotNull(field(layer, "graphicsLayer"))
                        elevation = maxOf(elevation, (graphics.javaClass.methods.single { it.name == "getShadowElevation" && it.parameterCount == 0 }.invoke(graphics) as Number).toDouble())
                    }
                    coordinator = field(coordinator, "wrapped")
                }
                writer.submit(host.snapshot(id, id, listOf(ProbePoint("container", "container", null,
                    listOf(number("shadowElevation", elevation / ComposeHost.RenderScale, "dip", category = "motion", definition = "surface.shadow-elevation")))),
                    image = id != "time-32000"))
            }
            fun change(time: Long, id: String, action: () -> Unit) {
                advance(time); action(); writer.record(time, "mutation", id); host.scheduler.runCurrent(); host.draw(); sample(id)
            }
            fun times(vararg values: Long) { for (time in values) { advance(time); host.draw(); sample("time-$time") } }
            val active: Interaction = if (config.kind == "button") PressInteraction.Press(Offset(52f, 24f) * ComposeHost.RenderScale.toFloat()) else DragInteraction.Start()
            sample("initial"); change(1000, "active") { source.tryEmit(active) }; times(16000, 32000, 64000)
            when (config.action) {
                "configuration" -> {
                    change(65000, "unused-hover") { step = 1 }; times(80000, 96000)
                    change(97000, "active-drag") { step = 2 }; times(112000, 160000, 240000)
                }
                "disable" -> {
                    change(65000, "disable") { enabled = false }; times(80000)
                    change(81000, "disabled-config") { step = 2 }; times(96000)
                    change(97000, "enable") { enabled = true }; times(112000, 160000, 240000)
                }
                else -> {
                    change(65000, "disable") { enabled = false }
                    change(66000, "config") { step = 2 }
                    change(67000, "enable") { enabled = true }
                    times(80000, 96000, 112000, 160000, 240000)
                }
            }
            change(241000, "stop") { source.tryEmit(if (active is PressInteraction.Press) PressInteraction.Release(active) else DragInteraction.Stop(active as DragInteraction.Start)) }
            times(256000, 320000, 400000, 640000)
        }
    }
}

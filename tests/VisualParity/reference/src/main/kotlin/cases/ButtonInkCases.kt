@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable data class ButtonInkConfig(val style: String)

@ParitySuite("button-ink")
class ButtonInkCases {
    @ParityScenario("color") suspend fun capture(writer: CaptureWriter) {
        val config = writer.readConfig<ButtonInkConfig>()
        ComposeHost(writer, 240, 160).use { host ->
            val source = MutableInteractionSource()
            var enabled by mutableStateOf(true)
            var overrideColor by mutableStateOf(Color(0xFF803050))
            var painted = Color.Unspecified
            host.mount {
                MaterialTheme(colorScheme = lightColorScheme(), motionScheme = MotionScheme.expressive()) {
                    val colors = when(config.style) {
                        "Elevated" -> ButtonDefaults.elevatedButtonColors()
                        "Tonal" -> ButtonDefaults.filledTonalButtonColors()
                        "Outlined" -> ButtonDefaults.outlinedButtonColors()
                        "Text" -> ButtonDefaults.textButtonColors()
                        else -> ButtonDefaults.buttonColors()
                    }.let { if(overrideColor == Color.Unspecified) it else it.copy(contentColor = overrideColor, disabledContentColor = overrideColor) }
                    val elevation = when(config.style) {
                        "Elevated" -> ButtonDefaults.elevatedButtonElevation()
                        "Tonal" -> ButtonDefaults.filledTonalButtonElevation()
                        "Outlined", "Text" -> null
                        else -> ButtonDefaults.buttonElevation()
                    }
                    Box(Modifier.fillMaxSize().background(Color.White), contentAlignment = Alignment.Center) {
                        Button(onClick = {}, modifier = Modifier.size(104.dp, 48.dp), enabled = enabled,
                            shape = RoundedCornerShape(8.dp), colors = colors, elevation = elevation,
                            border = if(config.style == "Outlined") ButtonDefaults.outlinedButtonBorder(enabled) else null,
                            contentPadding = PaddingValues(0.dp), interactionSource = source) {
                            val ink = LocalContentColor.current
                            Canvas(Modifier.size(32.dp, 16.dp)) { painted = ink; drawRect(ink) }
                        }
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            var next = 16000L
            fun advance(time: Long) { while(next <= time) { host.frame(next); next += 16000 }; host.advanceTo(time) }
            fun sample(id: String) {
                val argb = painted.toArgb()
                writer.submit(host.snapshot(id, id, listOf(ProbePoint("content", "content", null,
                    listOf(vector("color", "color", listOf((argb ushr 16 and 255).toDouble(), (argb ushr 8 and 255).toDouble(),
                        (argb and 255).toDouble(), (argb ushr 24).toDouble()), "srgba8", "none", "properties", "button.content.painted-solid-srgba8"))))))
            }
            fun change(time: Long, id: String, action: () -> Unit) { advance(time); action(); writer.record(time, "mutation", id); host.draw(); sample(id) }
            fun at(time: Long) { advance(time); host.draw(); sample("time-$time") }
            val hover = HoverInteraction.Enter()
            val press = PressInteraction.Press(Offset(52f, 24f) * ComposeHost.RenderScale.toFloat())
            sample("initial"); change(1000, "hover") { source.tryEmit(hover) }; at(16000); at(64000)
            change(65000, "press") { source.tryEmit(press) }; at(80000); at(128000)
            change(129000, "replace-color") { overrideColor = Color(0x804080C0) }; at(144000); at(192000)
            change(193000, "clear-color") { overrideColor = Color.Unspecified }; at(208000); at(256000)
            change(257000, "disable") { enabled = false }; at(272000); at(320000)
            change(321000, "enable") { enabled = true }; at(336000)
            change(337000, "release") { source.tryEmit(PressInteraction.Release(press)); source.tryEmit(HoverInteraction.Exit(hover)) }
            at(352000); at(480000); at(640000)
        }
    }
}

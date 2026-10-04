@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.MutatorMutex
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import androidx.compose.ui.window.PopupPositionProvider
import kotlinx.serialization.Serializable

@Serializable
data class NativeToolTipConfig(val contentWidth: Double = 64.0, val contentHeight: Double = 16.0,
    val dark: Boolean = false, val rtl: Boolean = false, val custom: Boolean = false)

@ParitySuite("native-tooltip")
class NativeToolTipCases {
    @ParityScenario("static")
    suspend fun capture(writer: CaptureWriter) {
        val config = writer.readConfig<NativeToolTipConfig>()
        ComposeHost(writer, 400, 240).use { host ->
            val scene = Scene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial", "Static native ToolTip surface", scene.measure()))
        }
    }

    private class Scene(val config: NativeToolTipConfig) {
        val tooltip = Probe(); val content = Probe(); var painted = Color.Transparent
        // The real TooltipBox supplies TooltipScope to the upstream PlainTooltip.
        // Centering isolates its static surface from intentionally native placement.
        val position = object: PopupPositionProvider {
            override fun calculatePosition(anchorBounds: IntRect, windowSize: IntSize,
                layoutDirection: LayoutDirection, popupContentSize: IntSize) =
                IntOffset((windowSize.width - popupContentSize.width) / 2, (windowSize.height - popupContentSize.height) / 2)
        }

        @Composable fun Content() {
            MaterialTheme(colorScheme = if (config.dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if (config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                        val state = rememberTooltipState(initialIsVisible = true, mutatorMutex = remember { MutatorMutex() })
                        TooltipBox(positionProvider = position, state = state, enableUserInput = false, tooltip = {
                            PlainTooltip(modifier = tooltip.modifier(),
                                containerColor = if (config.custom) Color(0xFFE0F0E8) else TooltipDefaults.plainTooltipContainerColor,
                                contentColor = if (config.custom) Color.Blue else TooltipDefaults.plainTooltipContentColor,
                                shape = if (config.custom) RoundedCornerShape(12.dp) else TooltipDefaults.plainTooltipContainerShape) {
                                val foreground = LocalContentColor.current
                                Canvas(content.modifier().size(config.contentWidth.toFloat().dp, config.contentHeight.toFloat().dp)) {
                                    painted = foreground; drawRect(foreground)
                                }
                            }
                        }) { Box(Modifier.size(1.dp)) }
                    }
                }
            }
        }

        fun measure(): List<ProbePoint> {
            fun geometry(probe: Probe): List<Measurement> {
                val c = checkNotNull(probe.coordinates)
                val origin = c.localToWindow(Offset.Zero) / ComposeHost.RenderScale.toFloat()
                return listOf(
                    vector("box", "rect", listOf(origin.x.toDouble(), origin.y.toDouble(),
                        c.size.width.toDouble() / ComposeHost.RenderScale, c.size.height.toDouble() / ComposeHost.RenderScale),
                        "dip", "scene", "geometry", "layout.box.aabb.unclipped", "LayoutCoordinates.size + localToWindow / density"),
                    vector("size", "point", listOf(c.size.width.toDouble() / ComposeHost.RenderScale, c.size.height.toDouble() / ComposeHost.RenderScale),
                        "dip", "local", "geometry", "layout.box.local-size", "LayoutCoordinates.size / density", listOf("width", "height")))
            }
            val argb = painted.toArgb()
            return listOf(ProbePoint("tooltip", "tooltip", null, geometry(tooltip)),
                ProbePoint("content", "content", "tooltip", geometry(content) + vector("paint", "color",
                    listOf((argb ushr 16 and 255).toDouble(), (argb ushr 8 and 255).toDouble(), (argb and 255).toDouble(), (argb ushr 24).toDouble()),
                    "srgba8", "none", "properties", "tooltip.content.painted-solid-srgba8", "Canvas actual inherited LocalContentColor.toArgb()")))
        }
    }
}

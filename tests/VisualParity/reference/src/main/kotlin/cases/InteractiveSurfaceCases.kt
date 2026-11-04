@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.AbsoluteRoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class InteractiveSurfaceConfig(val kind: String = "click", val shape: String = "rectangle", val state: Boolean = false,
    val enabled: Boolean = true, val dark: Boolean = false, val rtl: Boolean = false, val width: Double? = 120.0,
    val height: Double? = 64.0, val contentWidth: Double = 32.0, val contentHeight: Double = 16.0,
    val minimum: Double = 48.0, val tonal: Double = 0.0, val shadow: Double = 0.0, val border: Boolean = false,
    val color: String = "surface", val action: String = "none")

@ParitySuite("interactive-surface")
class InteractiveSurfaceCases {
    @ParityScenario("static") suspend fun static(writer: CaptureWriter) = capture(writer, false)
    @ParityScenario("dynamic") suspend fun dynamic(writer: CaptureWriter) = capture(writer, true)

    private suspend fun capture(writer: CaptureWriter, dynamic: Boolean) {
        val c = writer.readConfig<InteractiveSurfaceConfig>()
        ComposeHost(writer, 240, 160).use { host ->
            var source by mutableStateOf(MutableInteractionSource())
            var attached by mutableStateOf(true)
            var enabled by mutableStateOf(c.enabled)
            var value by mutableStateOf(c.state)
            var clicks = 0
            var requests = 0
            val surface = Probe()
            val marker = Probe()
            val requester = FocusRequester()
            host.mount {
                val scheme = if (c.dark) darkColorScheme(surface = Color(0xff141218), surfaceTint = Color(0xffd0bcff))
                    else lightColorScheme(surface = Color(0xfffef7ff), surfaceTint = Color(0xff6750a4))
                MaterialTheme(colorScheme = scheme, motionScheme = MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if (c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                        LocalMinimumInteractiveComponentSize provides c.minimum.toFloat().dp) {
                        Box(Modifier.fillMaxSize().background(Color.White), contentAlignment = Alignment.Center) {
                            if (attached) {
                                var modifier = surface.modifier().focusRequester(requester)
                                c.width?.let { modifier = modifier.width(it.toFloat().dp) }
                                c.height?.let { modifier = modifier.height(it.toFloat().dp) }
                                val shape = when (c.shape) {
                                    "round" -> AbsoluteRoundedCornerShape(16.dp)
                                    "asymmetric" -> AbsoluteRoundedCornerShape(24.dp, 4.dp, 12.dp, 0.dp)
                                    else -> AbsoluteRoundedCornerShape(0.dp)
                                }
                                val color = if (c.color == "primary") scheme.primary else scheme.surface
                                val border = if (c.border) BorderStroke(1.25.dp, Color.Red) else null
                                val content: @Composable () -> Unit = {
                                    val foreground = LocalContentColor.current
                                    Canvas(Modifier.size(c.contentWidth.toFloat().dp, c.contentHeight.toFloat().dp).then(marker.modifier())) {
                                        val mark = Size(minOf(12.dp.toPx(), size.width), minOf(8.dp.toPx(), size.height))
                                        drawRect(foreground, Offset((size.width - mark.width) / 2, (size.height - mark.height) / 2), mark)
                                    }
                                }
                                when (c.kind) {
                                    "select" -> Surface(selected = value, onClick = { clicks++; value = true }, modifier = modifier,
                                        enabled = enabled, shape = shape, color = color, tonalElevation = c.tonal.toFloat().dp,
                                        shadowElevation = c.shadow.toFloat().dp, border = border, interactionSource = source, content = content)
                                    "toggle" -> Surface(checked = value, onCheckedChange = { requests++; clicks++; value = it }, modifier = modifier,
                                        enabled = enabled, shape = shape, color = color, tonalElevation = c.tonal.toFloat().dp,
                                        shadowElevation = c.shadow.toFloat().dp, border = border, interactionSource = source, content = content)
                                    else -> Surface(onClick = { clicks++ }, modifier = modifier, enabled = enabled, shape = shape, color = color,
                                        tonalElevation = c.tonal.toFloat().dp, shadowElevation = c.shadow.toFloat().dp,
                                        border = border, interactionSource = source, content = content)
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            var next = 16000L
            fun advance(time: Long) { while (next <= time) { host.frame(next); next += 16000 }; host.advanceTo(time) }
            fun sample(id: String) {
                val points = mutableListOf(ProbePoint("state", "state", null, listOf(
                    number("clicks", clicks.toDouble(), "count", definition = "surface.activation-count"),
                    number("requests", requests.toDouble(), "count", definition = "surface.toggle-request-count"),
                    flag("value", if (c.kind == "click") false else value, definition = "surface.controlled-state"))))
                if (attached) {
                    points += ProbePoint("surface", "container", null, surface.geometry().filter { it.name in setOf("box", "size") })
                    points += ProbePoint("content", "content", "surface", marker.geometry().filter { it.name in setOf("box", "size") })
                }
                writer.submit(host.snapshot(id, id, points, image = id != "time-32000"))
            }
            fun change(time: Long, id: String, action: () -> Unit) {
                advance(time); action(); writer.record(time, "mutation", id); host.draw(); host.frame(time); sample(id)
            }
            fun times(vararg times: Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun input(time: Long, operation: String, position: Offset) { advance(time); host.input(operation, position); host.draw() }
            val center = Offset(120f, 80f)
            val outside = Offset(8f, 8f)
            sample("initial"); if (!dynamic) return
            if(c.action in setOf("keys","keys-reverse","key-source")) {
                change(1000,"focus") { requester.requestFocus() };times(16000,160000)
                fun keyChange(time:Long,id:String,key:Key,down:Boolean) = change(time,id) {
                    host.scene.sendKeyEvent(KeyEvent(key,if(down) KeyEventType.KeyDown else KeyEventType.KeyUp));host.scheduler.runCurrent()
                }
                keyChange(161000,"space-down",Key.Spacebar,true);times(176000)
                if(c.action=="key-source") {
                    change(177000,"replace") { source=MutableInteractionSource() }
                    keyChange(193000,"cancelled-key-up",Key.Spacebar,false)
                    keyChange(209000,"new-down",Key.Spacebar,true);times(224000)
                    keyChange(241000,"new-up",Key.Spacebar,false)
                } else {
                    keyChange(177000,"enter-down",Key.Enter,true)
                    keyChange(193000,"space-repeat",Key.Spacebar,true);times(208000,240000)
                    keyChange(241000,"first-up",if(c.action=="keys-reverse") Key.Enter else Key.Spacebar,false);times(256000,320000)
                    keyChange(321000,"second-up",if(c.action=="keys-reverse") Key.Spacebar else Key.Enter,false)
                    keyChange(337000,"unmatched-up",Key.Spacebar,false)
                }
                times(400000,800000);return
            }
            if (c.action in setOf("space", "enter")) {
                change(1000, "focus") { requester.requestFocus() }; times(16000, 64000, 160000)
                val key = if (c.action == "space") Key.Spacebar else Key.Enter
                change(161000, "key-down") { host.scene.sendKeyEvent(KeyEvent(key, KeyEventType.KeyDown)); host.scheduler.runCurrent() }
                times(176000, 240000)
                change(241000, "key-up") { host.scene.sendKeyEvent(KeyEvent(key, KeyEventType.KeyUp)); host.scheduler.runCurrent() }
                times(256000, 400000, 800000); return
            }
            if (c.action in setOf("source", "replace", "detach")) {
                val first = PressInteraction.Press(Offset(40f, 24f) * ComposeHost.RenderScale.toFloat())
                val second = PressInteraction.Press(Offset(80f, 40f) * ComposeHost.RenderScale.toFloat())
                change(1000, "first") { check(source.tryEmit(first)) }; times(16000, 32000, 64000)
                when (c.action) {
                    "source" -> {
                        change(65000, "second") { check(source.tryEmit(second)) }
                        change(81000, "release-first") { check(source.tryEmit(PressInteraction.Release(first))) }
                        times(96000, 160000)
                        change(161000, "cancel-second") { check(source.tryEmit(PressInteraction.Cancel(second))) }
                    }
                    "replace" -> {
                        val old = source
                        change(65000, "replace") { source = MutableInteractionSource() }
                        change(81000, "old-release") { check(old.tryEmit(PressInteraction.Release(first))) }
                        change(97000, "new-press") { check(source.tryEmit(second)) }; times(112000, 160000)
                        change(161000, "new-release") { check(source.tryEmit(PressInteraction.Release(second))) }
                    }
                    else -> {
                        change(65000, "detach") { attached = false }
                        change(81000, "detached-press") { check(source.tryEmit(second)) }
                        change(97000, "reattach") { attached = true }; times(112000, 160000)
                    }
                }
                times(176000, 240000, 400000, 800000); return
            }
            input(1000, "move", center); input(2000, "down", center); sample("press")
            if (c.action == "quick") {
                input(5000, "up", center); sample("release"); times(16000, 80000, 240000, 800000); return
            }
            times(16000, 32000, 64000)
            if (c.action == "cancel") input(65000, "move", outside)
            if (c.action == "disable") change(65000, "disable") { enabled = false }
            times(80000, 160000)
            input(161000, "up", if (c.action == "cancel") outside else center); sample("release")
            times(176000, 240000, 400000, 800000)
        }
    }
}

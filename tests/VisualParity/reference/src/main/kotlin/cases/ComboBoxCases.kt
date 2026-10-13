@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.input.TextFieldLineLimits
import androidx.compose.foundation.text.input.rememberTextFieldState
import androidx.compose.material3.*
import androidx.compose.material3.internal.Icons
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.FocusManager
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class ComboBoxConfig(val outlined: Boolean = false, val slots: Boolean = false, val label: Boolean = true,
    val enabled: Boolean = true, val error: Boolean = false, val dark: Boolean = false, val rtl: Boolean = false,
    val width: Double? = null, val height: Double? = null, val labelWidth: Double = 40.0, val labelHeight: Double = 12.0,
    val items: Int = 0, val selected: Int = -1, val action: String = "none")

@ParitySuite("combo-box")
class ComboBoxCases {
    @ParityScenario("static")
    suspend fun static(writer: CaptureWriter) {
        val config = writer.readConfig<ComboBoxConfig>()
        val menu = config.items > 0
        ComposeHost(writer, if (menu) 560 else 400, if (menu) 480 else 200).use { host ->
            val scene = FieldScene(config)
            host.mount { scene.Content() }
            host.frame(-1000)
            host.frame(0)
            if (menu) {
                var next = 16000L
                while (next <= 800000L) { host.frame(next); next += 16000 }
                host.advanceTo(800000)
            }
            writer.submit(host.snapshot("initial", "initial", scene.measure()))
        }
    }

    @ParityScenario("changes")
    suspend fun changes(writer: CaptureWriter) {
        val config = writer.readConfig<ComboBoxConfig>()
        ComposeHost(writer, 400, 200).use { host ->
            val scene = FieldScene(config)
            host.mount { scene.Content() }
            host.frame(-1000)
            host.frame(0)
            fun sample(id: String) = writer.submit(host.snapshot(id, id, scene.measure(), image = config.action == "error" || config.action == "theme"))
            var next = 16000L
            fun advance(time: Long) { while (next <= time) { host.frame(next); next += 16000 }; host.advanceTo(time) }
            fun change(time: Long, id: String, action: () -> Unit) {
                advance(time); action(); writer.record(time, "mutation", id); host.draw(); host.frame(time); sample(id)
            }
            sample("initial")
            change(1000, "start") {
                when (config.action) {
                    "error" -> scene.error = true
                    "theme" -> scene.dark = true
                    else -> scene.requester.requestFocus()
                }
            }
            listOf(16000L, 32000L, 64000L).forEach { advance(it); sample("time-$it") }
            if (config.action == "reverse") change(65000, "reverse") { scene.focusManager.clearFocus() }
            if (config.action == "disable") change(65000, "disable") { scene.enabled = false }
            listOf(96000L, 160000L, 240000L, 400000L, 800000L).forEach { advance(it); sample("time-$it") }
        }
    }

    private class FieldScene(val config: ComboBoxConfig) {
        val field = Probe()
        val probes = linkedMapOf<String, Probe>()
        val itemProbes = List(config.items) { Probe() }
        val requester = FocusRequester()
        lateinit var focusManager: FocusManager
        var enabled by mutableStateOf(config.enabled)
        var error by mutableStateOf(config.error)
        var dark by mutableStateOf(config.dark)
        var expanded by mutableStateOf(config.items > 0)
        var progress = 0f

        @Composable fun Slot(name: String, width: Double, height: Double) {
            val probe = remember(name) { Probe() }
            SideEffect { probes[name] = probe }
            DisposableEffect(probe) { onDispose { if (probes[name] === probe) probes.remove(name) } }
            Box(probe.modifier().size(width.toFloat().dp, height.toFloat().dp).background(LocalContentColor.current))
        }

        @Composable fun Content() {
            focusManager = LocalFocusManager.current
            MaterialTheme(colorScheme = if (dark) darkColorScheme() else lightColorScheme(), motionScheme = MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if (config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White), contentAlignment = if (config.items > 0) Alignment.TopCenter else Alignment.Center) {
                        Box(Modifier.padding(top = if (config.items > 0) 32.dp else 0.dp)) {
                            // Field colors follow TextField, which is the accepted palette. The menu is the
                            // read-only sample: standard group container, standalone shape, selectable rows.
                            ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = it }) {
                                var modifier = field.modifier().menuAnchor(ExposedDropdownMenuAnchorType.PrimaryNotEditable).focusRequester(requester)
                                if (config.width != null) modifier = modifier.width(config.width.toFloat().dp)
                                if (config.height != null) modifier = modifier.height(config.height.toFloat().dp)
                                val position = if (config.outlined) TextFieldLabelPosition.Cutout() else TextFieldLabelPosition.Inside()
                                val label: (@Composable TextFieldLabelScope.() -> Unit)? = if (config.label) {
                                    { progress = labelMinimizedProgress; Slot("label", config.labelWidth, config.labelHeight) }
                                } else null
                                val leading: (@Composable () -> Unit)? = if (config.slots) ({ Slot("leading", 24.0, 24.0) }) else null
                                val supporting: (@Composable () -> Unit)? = if (config.slots) ({ Slot("supporting", 60.0, 12.0) }) else null
                                val state = rememberTextFieldState()
                                val trailing: @Composable () -> Unit = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) }
                                if (config.outlined) OutlinedTextField(state = state, modifier = modifier, enabled = enabled, readOnly = true, isError = error,
                                    labelPosition = position, label = label, leadingIcon = leading, trailingIcon = trailing, supportingText = supporting,
                                    lineLimits = TextFieldLineLimits.SingleLine)
                                else TextField(state = state, modifier = modifier, enabled = enabled, readOnly = true, isError = error,
                                    labelPosition = position, label = label, leadingIcon = leading, trailingIcon = trailing, supportingText = supporting,
                                    lineLimits = TextFieldLineLimits.SingleLine)
                                if (config.items > 0) ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false },
                                    containerColor = MenuDefaults.groupStandardContainerColor, shape = MenuDefaults.standaloneGroupShape) {
                                    repeat(config.items) { index ->
                                        SelectableDropdownMenuItem(selected = index == config.selected, onClick = {},
                                            shapes = MenuDefaults.itemShape(index, config.items),
                                            text = { Box(itemProbes[index].modifier().size(48.dp, 16.dp).background(LocalContentColor.current)) },
                                            selectedLeadingIcon = { Icon(Icons.Filled.Check, null, Modifier.size(MenuDefaults.LeadingIconSize)) },
                                            contentPadding = ExposedDropdownMenuDefaults.ItemContentPadding)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        fun measure(): List<ProbePoint> {
            val presented = itemProbes.count { it.coordinates?.isAttached == true }
            if (config.items > 0 && presented != config.items) error("Expected ${config.items} menu markers, found $presented.")
            val result = mutableListOf(
                ProbePoint("field", "field", null, field.geometry().filter { it.name == "box" || it.name == "size" }),
                ProbePoint("state", "state", null, listOf(
                    flag("expanded", expanded, definition = "combo-box.menu-expanded"),
                    number("items", presented.toDouble(), "count", definition = "combo-box.menu-items-presented"))))
            probes.forEach { (name, probe) -> result.add(ProbePoint(name, name, "field", probe.geometry().filter { it.name == "box" || it.name == "size" })) }
            if (config.label) result.add(ProbePoint("motion", "motion", "field", listOf(
                number("labelProgress", progress.toDouble(), "ratio", category = "motion", definition = "combo-box.label-minimized-progress"))))
            itemProbes.forEachIndexed { index, probe ->
                result.add(ProbePoint("item-$index", "content", "field", probe.geometry().filter { it.name == "box" || it.name == "size" }))
            }
            return result
        }
    }
}

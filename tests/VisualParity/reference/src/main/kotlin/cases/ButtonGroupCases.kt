@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
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
data class ButtonGroupConfig(
    val rtl: Boolean = false, val space: Int = 400, val fixedWidth: Boolean = false,
    val arrangement: String = "SpacedBy", val weights: String = "none", val alignment: String = "Top",
    val content: String = "blocks", val action: String = "none", val pressIndex: Int = 1,
    val ratio: Double = .15, val compression: Double = 24.0, val checked: Boolean = false, val spacingAlignment: String = "Left"
)

@ParitySuite("button-group")
class ButtonGroupCases {
    @ParityScenario("static")
    suspend fun static(writer: CaptureWriter) = capture(writer, false)

    @ParityScenario("dynamic")
    suspend fun dynamic(writer: CaptureWriter) = capture(writer, true)

    private suspend fun capture(writer: CaptureWriter, dynamic: Boolean) {
        val config = writer.readConfig<ButtonGroupConfig>()
        ComposeHost(writer, 420, 240).use { host ->
            val group = Probe()
            val indicator = Probe()
            val items = List(3) { Probe() }
            val contentMarkers = List(3) { Probe() }
            val menuItems = List(3) { Probe() }
            var menuState: ButtonGroupMenuState? = null
            var space by mutableStateOf(config.space)
            var enabled by mutableStateOf(true)
            var clicks = 0
            val checked = mutableStateListOf(config.checked, config.checked, config.checked)

            host.mount {
                MaterialTheme(colorScheme = lightColorScheme(), motionScheme = MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if (config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White), contentAlignment = AbsoluteAlignment.TopLeft) {
                            val arrangement = when (config.arrangement) {
                                "Start" -> Arrangement.Start
                                "Center" -> Arrangement.Center
                                "End" -> Arrangement.End
                                "SpaceBetween" -> Arrangement.SpaceBetween
                                "SpaceAround" -> Arrangement.SpaceAround
                                "SpaceEvenly" -> Arrangement.SpaceEvenly
                                else -> Arrangement.spacedBy(12.dp, when (config.spacingAlignment) {
                                    "Center" -> Alignment.CenterHorizontally
                                    "Right" -> Alignment.End
                                    else -> Alignment.Start
                                })
                            }
                            val alignment = when (config.alignment) {
                                "Center" -> Alignment.CenterVertically
                                "Bottom" -> Alignment.Bottom
                                else -> Alignment.Top
                            }
                            val sizeModifier = if (config.fixedWidth) Modifier.width(space.dp) else Modifier.widthIn(max = space.dp)
                            ButtonGroup(
                                modifier = Modifier.absoluteOffset(32.dp, 24.dp).then(sizeModifier).then(group.modifier()),
                                expandedRatio = config.ratio.toFloat(),
                                horizontalArrangement = arrangement,
                                verticalAlignment = alignment,
                                overflowIndicator = { state ->
                                    menuState = state
                                    Box(indicator.modifier().size(32.dp, 40.dp).background(Color(0xff008000))
                                        .clickable(interactionSource = null, indication = null) {
                                            if (state.isShowing) state.dismiss() else state.show()
                                        })
                                }
                            ) {
                                listOf(40 to 24, 56 to 32, 32 to 40).forEachIndexed { index, size ->
                                    customItem(
                                        buttonGroupContent = {
                                            val interaction = remember { MutableInteractionSource() }
                                            var modifier: Modifier = Modifier
                                            if (config.weights == "all" || config.weights == "mixed" && index != 1)
                                                modifier = modifier.weight((index + 1).toFloat())
                                            if (index == 1 && config.alignment != "Top")
                                                modifier = modifier.align(Alignment.Top)
                                            if (config.content != "blocks")
                                                modifier = modifier.animateWidth(interaction, config.compression.toFloat().dp)
                                            modifier = modifier.then(items[index].modifier())

                                            if (config.content == "blocks") {
                                                Box(modifier.size(size.first.dp, size.second.dp).background(Color.Blue))
                                            } else {
                                                val content: @Composable RowScope.() -> Unit = {
                                                    Box(Modifier.size(listOf(32, 48, 24)[index].dp, 16.dp)
                                                        .then(contentMarkers[index].modifier()).background(LocalContentColor.current))
                                                }
                                                // Observe the custom-item allocation, rather than the Button's
                                                // uncoerced minimum-interactive node when a neighbour shrinks below 48 dp.
                                                Box(modifier, propagateMinConstraints = true) {
                                                if (config.content == "connected") {
                                                    val shapes = when (index) {
                                                        0 -> ButtonGroupDefaults.connectedLeadingButtonShapes()
                                                        2 -> ButtonGroupDefaults.connectedTrailingButtonShapes()
                                                        else -> ButtonGroupDefaults.connectedMiddleButtonShapes()
                                                    }
                                                    ToggleButton(checked = checked[index], onCheckedChange = { checked[index] = it; clicks++ },
                                                        enabled = enabled || index != config.pressIndex,
                                                        shapes = shapes, contentPadding = PaddingValues(16.dp, 10.dp),
                                                        interactionSource = interaction, content = content)
                                                } else {
                                                    Button(onClick = { clicks++ },
                                                        enabled = enabled || index != config.pressIndex,
                                                        contentPadding = PaddingValues(16.dp, 10.dp),
                                                        interactionSource = interaction, content = content)
                                                }
                                                }
                                            }
                                        },
                                        menuContent = { state ->
                                            DropdownMenuItem(modifier = menuItems[index].modifier(),
                                                text = { Box(Modifier.size(48.dp, 16.dp).background(LocalContentColor.current)) },
                                                onClick = { clicks++; state.dismiss() })
                                        }
                                    )
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000)
            host.frame(0)

            var nextFrame = 16000L
            fun advance(time: Long) {
                while (nextFrame <= time) {
                    host.frame(nextFrame)
                    nextFrame += 16000
                }
                host.advanceTo(time)
            }

            fun sample(id: String) {
                val points = mutableListOf(
                    ProbePoint("group", "container", null, group.geometry().filter { it.name == "box" || it.name == "size" }),
                    ProbePoint("state", "state", null, listOf(
                        flag("expanded", checkNotNull(menuState).isShowing, definition = "button-group.menu.showing"),
                        number("clicks", clicks.toDouble(), "count", definition = "button-group.clicks")))
                )
                if (config.action !in listOf("menu", "outside", "reverse", "resize")) {
                    items.forEachIndexed { index, probe ->
                        if (probe.coordinates != null) {
                            points.add(ProbePoint("item-$index", "item", "group", probe.geometry().filter { it.name == "box" || it.name == "size" }))
                            if (config.content != "blocks")
                                points.add(ProbePoint("content-$index", "content", "item-$index", contentMarkers[index].geometry()
                                    .filter { it.name == "box" || it.name == "size" }))
                        }
                    }
                }
                writer.submit(host.snapshot(id, id, points, image = id != "time-32000"))
            }

            fun times(vararg times: Long) {
                times.forEach { time -> advance(time); host.draw(); sample("time-$time") }
            }

            fun mutate(time: Long, id: String, action: () -> Unit) {
                advance(time)
                action()
                writer.record(time, "mutation", id)
                host.draw()
                host.frame(time)
                sample(id)
            }

            fun center(probe: Probe): Offset {
                val coordinates = checkNotNull(probe.coordinates)
                return coordinates.localToWindow(Offset(coordinates.size.width / 2f, coordinates.size.height / 2f)) / ComposeHost.RenderScale.toFloat()
            }

            fun click(probe: Probe) {
                val point = center(probe)
                host.input("move", point)
                host.input("down", point)
                host.input("up", point)
            }

            sample("initial")
            if (!dynamic) return
            if (config.action == "resize") {
                mutate(1000, "all-visible") { space = 400 }
                mutate(2000, "none-visible") { space = 20 }
                mutate(3000, "restore") { space = config.space }
                return
            }
            if (config.content == "blocks") {
                mutate(1000, "open") { click(indicator) }
                times(16000, 32000, 64000)
                if (config.action == "reverse") {
                    mutate(65000, "close") { checkNotNull(menuState).dismiss() }
                    times(80000, 96000)
                    mutate(97000, "reopen") { checkNotNull(menuState).show() }
                    times(112000, 224000, 800000)
                } else {
                    times(224000, 800000)
                    mutate(801000, "select-or-dismiss") {
                        if (config.action == "outside") {
                            host.input("move", Offset(8f, 8f))
                            host.input("down", Offset(8f, 8f))
                            host.input("up", Offset(8f, 8f))
                        } else click(menuItems.last())
                    }
                    times(816000, 960000, 1200000)
                }
                return
            }

            val pressCenter = center(items[config.pressIndex])
            fun input(time: Long, operation: String, point: Offset) {
                advance(time)
                host.input(operation, point)
                host.draw()
            }
            input(1000, "move", pressCenter)
            input(2000, "down", pressCenter)
            sample("press")
            if (config.action == "quick") {
                input(5000, "up", pressCenter)
                sample("release")
                times(16000, 32000, 64000, 80000, 160000, 400000, 800000)
            } else {
                times(16000, 32000, 64000)
                if (config.action == "cancel") input(65000, "move", Offset(415f, 235f))
                if (config.action == "disable") mutate(65000, "disabled") { enabled = false }
                input(81000, "up", if (config.action == "cancel") Offset(415f, 235f) else pressCenter)
                sample("release")
                times(96000)
                if (config.action == "repress") {
                    input(97000, "down", pressCenter)
                    sample("repress")
                    times(112000, 160000)
                    input(161000, "up", pressCenter)
                    sample("second-release")
                }
                times(240000, 400000, 800000)
            }
        }
    }
}

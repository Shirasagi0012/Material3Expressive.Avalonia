@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class MenuSourceConfig(val kind:String="baseline",val action:String="press")

@ParitySuite("menu-source")
class MenuInteractionSourceCases {
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<MenuSourceConfig>()
        ComposeHost(writer,240,128).use { host ->
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            var next=16000L
            fun advance(t:Long) { while(next<=t) { host.frame(next);next+=16000 };host.advanceTo(t) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun times(vararg ts:Long) { ts.forEach { advance(it);host.draw();sample("time-$it") } }
            fun mutate(t:Long,id:String,sampleNow:Boolean=true,action:()->Unit) {
                advance(t);action();writer.record(t,"mutation",id);host.draw();host.frame(t);if(sampleNow)sample(id)
            }
            var source=checkNotNull(scene.source)
            // Avalonia's source positions are owner-local; Compose's indication is inside
            // the expressive item's 4dp horizontal padding and 2dp minimum-touch inset.
            // Use the same scene point, including the 44dp surface inside the 48dp target.
            val position=when(c.kind) {
                "baseline", "native-baseline" -> Offset(40f,24f)
                "native-expressive" -> Offset(40f,22f) // Native owner Bounds excludes horizontal Margin.
                else -> Offset(36f,22f)
            }
            val scaledPosition=position*ComposeHost.RenderScale.toFloat()
            val press=PressInteraction.Press(scaledPosition)
            val second=PressInteraction.Press(scaledPosition)
            val hover=HoverInteraction.Enter();val drag=DragInteraction.Start()
            sample("initial")
            if(c.action=="hover-drag") {
                mutate(1000,"hover"){source.tryEmit(hover)};times(16000,64000)
                mutate(65000,"drag"){source.tryEmit(drag)};times(80000,160000)
                mutate(161000,"drag-stop"){source.tryEmit(DragInteraction.Stop(drag))};times(176000,240000)
                mutate(241000,"hover-exit"){source.tryEmit(HoverInteraction.Exit(hover))};times(256000,400000)
                return
            }
            if(c.action=="null-native") {
                mutate(1000,"local-source"){scene.source=null}
                mutate(17000,"native-down"){host.input("move",Offset(120f,64f));host.input("down",Offset(120f,64f))};times(32000,64000)
                mutate(65000,"native-up"){host.input("up",Offset(120f,64f))};times(80000,240000,800000)
                return
            }
            mutate(1000,"press"){source.tryEmit(press)};times(16000,64000)
            when(c.action) {
                "overlap" -> {
                    mutate(65000,"second-press"){source.tryEmit(second)}
                    mutate(66000,"first-release"){source.tryEmit(PressInteraction.Release(press))};times(80000,160000,240000)
                    mutate(241000,"second-cancel"){source.tryEmit(PressInteraction.Cancel(second))}
                }
                "replace" -> {
                    val old=source;source=MutableInteractionSource();source.tryEmit(second)
                    mutate(65000,"replace"){scene.source=source}
                    mutate(66000,"old-release"){old.tryEmit(PressInteraction.Release(press))};times(80000,160000)
                    mutate(161000,"new-press"){source.tryEmit(press)};times(176000,240000)
                    mutate(241000,"new-release"){source.tryEmit(PressInteraction.Release(press))}
                }
                "disable" -> {
                    mutate(65000,"disable"){scene.enabled=false};times(80000,160000)
                    mutate(161000,"external-release"){source.tryEmit(PressInteraction.Release(press))}
                    mutate(177000,"disabled-press"){source.tryEmit(second)};times(192000,240000)
                    mutate(241000,"enable"){scene.enabled=true}
                    mutate(242000,"release"){source.tryEmit(PressInteraction.Release(second))}
                }
                "reattach" -> {
                    mutate(65000,"detach",false){scene.mounted=false}
                    mutate(66000,"unmounted-press",false){source.tryEmit(second)}
                    mutate(81000,"reattach"){scene.mounted=true};times(96000,160000)
                    mutate(161000,"old-release"){source.tryEmit(PressInteraction.Release(second))}
                    mutate(177000,"new-press"){source.tryEmit(press)};times(192000,240000)
                    mutate(241000,"release"){source.tryEmit(PressInteraction.Release(press))}
                }
                else -> {
                    times(160000,240000)
                    mutate(241000,"release"){source.tryEmit(PressInteraction.Release(press))}
                }
            }
            times(256000,320000,400000,800000)
        }
    }
    private class Scene(val c:MenuSourceConfig) {
        var source:MutableInteractionSource? by mutableStateOf(MutableInteractionSource())
        var enabled by mutableStateOf(true)
        var mounted by mutableStateOf(true)
        val selected = c.kind == "checkable" || c.kind == "selectable"
        var clicks=0
        val probe=Probe()
        @Composable fun Content() {
            MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalContentColor provides MaterialTheme.colorScheme.onSurface) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        if(mounted) {
                            val content: @Composable () -> Unit = { Box(Modifier.size(32.dp,16.dp).background(Color.Blue)) }
                            val modifier = probe.modifier().width(180.dp)
                            when(c.kind) {
                                "expressive" -> DropdownMenuItem(onClick={clicks++},text=content,shape=MenuDefaults.itemShapes().shape,modifier=modifier,enabled=enabled,interactionSource=source)
                                "native-expressive" -> SelectableDropdownMenuItem(selected=false,onClick={clicks++},text=content,shapes=MenuDefaults.itemShapes(),modifier=modifier,enabled=enabled,interactionSource=source)
                                "checkable" -> CheckableDropdownMenuItem(checked=selected,onCheckedChange={clicks++},text=content,shapes=MenuDefaults.itemShapes(),modifier=modifier,enabled=enabled,interactionSource=source)
                                "selectable" -> SelectableDropdownMenuItem(selected=selected,onClick={clicks++},text=content,shapes=MenuDefaults.itemShapes(),modifier=modifier,enabled=enabled,interactionSource=source)
                                else -> DropdownMenuItem(text=content,onClick={clicks++},modifier=modifier,enabled=enabled,interactionSource=source)
                            }
                        }
                    }
                }
            }
        }
        fun measure()=listOf(ProbePoint("control",c.kind,null,
            probe.geometry().filter { it.name in listOf("box","size") }+listOf(
                number("state",if(selected) 1.0 else 0.0,"enum",definition="menu-source.state"),
                flag("enabled",enabled,definition="menu-source.enabled"),
                number("clicks",clicks.toDouble(),"count",definition="menu-source.clicks"))))
    }
}

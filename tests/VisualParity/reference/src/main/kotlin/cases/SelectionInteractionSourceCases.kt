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
import androidx.compose.ui.state.ToggleableState
import kotlinx.serialization.Serializable

@Serializable
data class SelectionSourceConfig(val kind:String="checkbox",val action:String="press")

@ParitySuite("selection-source")
class SelectionInteractionSourceCases {
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<SelectionSourceConfig>()
        ComposeHost(writer,160,128).use { host ->
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            var next=16000L
            fun advance(t:Long) { while(next<=t) { host.frame(next);next+=16000 };host.advanceTo(t) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun times(vararg ts:Long) { ts.forEach { advance(it);host.draw();sample("time-$it") } }
            fun mutate(t:Long,id:String,sampleNow:Boolean=true,action:()->Unit) {
                advance(t);action();writer.record(t,"mutation",id);host.draw();host.frame(t);if(sampleNow)sample(id)
            }
            var source=checkNotNull(scene.source)
            val press=PressInteraction.Press(Offset(12f,12f)*ComposeHost.RenderScale.toFloat())
            val second=PressInteraction.Press(Offset(12f,12f)*ComposeHost.RenderScale.toFloat())
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
                mutate(17000,"native-down"){host.input("move",Offset(80f,64f));host.input("down",Offset(80f,64f))};times(32000,64000)
                mutate(65000,"native-up"){host.input("up",Offset(80f,64f))};times(80000,240000,800000)
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
    private class Scene(val c:SelectionSourceConfig) {
        var source:MutableInteractionSource? by mutableStateOf(MutableInteractionSource())
        var enabled by mutableStateOf(true)
        var mounted by mutableStateOf(true)
        var state by mutableStateOf(if(c.kind=="tri") ToggleableState.Indeterminate else ToggleableState.Off)
        var clicks=0
        val probe=Probe()
        @Composable fun Content() {
            MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalContentColor provides MaterialTheme.colorScheme.onSurface) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        if(mounted) when(c.kind) {
                            "radio" -> RadioButton(state==ToggleableState.On,{clicks++;state=ToggleableState.On},probe.modifier(),enabled,interactionSource=source)
                            "tri" -> TriStateCheckbox(state,{clicks++;state=if(state==ToggleableState.On) ToggleableState.Off else ToggleableState.On},probe.modifier(),enabled,interactionSource=source)
                            else -> Checkbox(state==ToggleableState.On,{clicks++;state=ToggleableState(it)},probe.modifier(),enabled,interactionSource=source)
                        }
                    }
                }
            }
        }
        fun measure()=listOf(ProbePoint("control",c.kind,null,
            probe.geometry().filter { it.name in listOf("box","size") }+listOf(
                number("state",when(state){ToggleableState.On->1.0;ToggleableState.Indeterminate->2.0;else->0.0},"enum",definition="selection-source.state"),
                flag("enabled",enabled,definition="selection-source.enabled"),
                number("clicks",clicks.toDouble(),"count",definition="selection-source.clicks"))))
    }
}

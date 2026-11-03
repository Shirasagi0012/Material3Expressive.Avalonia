@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.platform.WindowInfo
import androidx.compose.ui.unit.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class SwipeToDismissConfig(val initial:String="Settled",val startToEnd:Boolean=true,val endToStart:Boolean=true,
    val rtl:Boolean=false,val gestures:Boolean=true,val width:Double=240.0,val autoSize:Boolean=false,val action:String="none")

@ParitySuite("swipe-to-dismiss")
class SwipeToDismissBoxCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val c=writer.readConfig<SwipeToDismissConfig>()
        ComposeHost(writer,520,160,desktopContext()).use { host ->
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<SwipeToDismissConfig>()
        ComposeHost(writer,520,160,desktopContext()).use { host ->
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(t:Long) { while(nextFrame<=t) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(t) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun times(vararg ts:Long) { ts.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(t:Long,id:String,sampleNow:Boolean=true,action:()->Unit) { advance(t); action(); writer.record(t,"mutation",id); host.scheduler.runCurrent(); host.draw(); if(sampleNow) sample(id) }
            fun input(t:Long,action:String,p:Offset) { advance(t); host.input(action,p); host.draw() }
            sample("initial")
            if(c.action in listOf("dismiss","dismiss-end","reset","reverse","resize","remove-anchor","drag-interrupt")) {
                mutate(1000,"state-change") { scene.scope.launch {
                    if(c.action=="reset") scene.state.reset() else scene.state.dismiss(if(c.action=="dismiss-end") SwipeToDismissBoxValue.EndToStart else SwipeToDismissBoxValue.StartToEnd)
                } }
                times(16000,64000,96000)
                if(c.action=="reverse") mutate(97000,"reverse") { scene.scope.launch { scene.state.dismiss(SwipeToDismissBoxValue.EndToStart) } }
                if(c.action=="resize") mutate(97000,"resize",false) { scene.width=280.0 }
                if(c.action=="remove-anchor") mutate(97000,"remove-anchor",false) { scene.startToEnd=false }
                if(c.action=="drag-interrupt") {
                    input(98000,"down",Offset(220f,64f)); sample("interrupt-down")
                    input(114000,"move",Offset(150f,64f)); sample("interrupt-move")
                    input(170000,"up",Offset(150f,64f)); sample("interrupt-up")
                }
                times(192000,320000,416000,640000)
            } else if(c.action=="snap") {
                mutate(1000,"snap-dismiss") { scene.scope.launch { scene.state.snapTo(SwipeToDismissBoxValue.EndToStart) } }; times(16000)
                mutate(17000,"snap-reset") { scene.scope.launch { scene.state.snapTo(SwipeToDismissBoxValue.Settled) } }; times(32000)
            } else {
                val origin=Offset(if(c.rtl) 460f else 60f,64f); val sign=if(c.rtl) -1 else 1
                input(1000,"move",origin); input(2000,"down",origin); sample("drag-down")
                input(18000,"move",origin+Offset(32f*sign,0f)); sample("drag-start")
                val distance=if(c.action=="drag-return") 70f else 170f
                var end=origin+Offset(distance*sign,0f)
                input(34000,"move",end); sample("drag-move")
                if(c.action=="disable-during-drag") mutate(35000,"disable",false) { scene.gestures=false }
                if(c.action=="drag-reverse") { end=origin+Offset(-35f*sign,0f); input(50000,"move",end); sample("drag-reverse") }
                input(if(c.action=="drag-fast") 35000 else 100000,"up",end); sample("drag-up")
                times(112000,160000,256000,416000,640000)
            }
        }
    }

    private class Scene(val c:SwipeToDismissConfig) {
        lateinit var state:SwipeToDismissBoxState
        lateinit var scope:CoroutineScope
        var width by mutableStateOf(c.width)
        var startToEnd by mutableStateOf(c.startToEnd)
        var gestures by mutableStateOf(c.gestures)
        var dismissals=0
        var lastDismissal="none"
        val container=Probe(); val foreground=Probe(); val background=Probe()
        @Composable fun Content() {
            MaterialTheme {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    state=rememberSwipeToDismissBoxState(SwipeToDismissBoxValue.valueOf(c.initial))
                    scope=rememberCoroutineScope()
                    Box(Modifier.fillMaxSize().background(Color.White).padding(32.dp)) {
                        SwipeToDismissBox(state=state,
                            modifier=container.modifier().then(if(c.autoSize) Modifier else Modifier.size(width.toFloat().dp,64.dp)),
                            enableDismissFromStartToEnd=startToEnd,enableDismissFromEndToStart=c.endToStart,gesturesEnabled=gestures,
                            onDismiss={ dismissals++; lastDismissal=it.name },
                            backgroundContent={ Box(background.modifier().fillMaxSize().background(Color(0xFF993344))) }) {
                            Box(foreground.modifier().then(if(c.autoSize) Modifier.size(96.dp,40.dp) else Modifier.fillMaxSize()).background(Color(0xFF336699))) {
                                Box(Modifier.align(Alignment.CenterStart).size(24.dp).background(Color(0xFFFFD166)))
                            }
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe)=probe.geometry().filter { it.name in listOf("box","size") }
            return listOf(
                ProbePoint("container","container",null,geometry(container)+listOf(
                    text("currentValue",state.currentValue.name,definition="swipe.current-value"),
                    text("targetValue",state.targetValue.name,definition="swipe.target-value"),
                    text("settledValue",state.settledValue.name,definition="swipe.settled-value"),
                    text("dismissDirection",state.dismissDirection.name,definition="swipe.dismiss-direction"),
                    number("offset",state.requireOffset().toDouble()/ComposeHost.RenderScale,"dip","local","motion","swipe.offset","SwipeToDismissBoxState.requireOffset / density"),
                    number("progress",state.progress.toDouble(),"fraction",definition="swipe.progress"),
                    number("dismissals",dismissals.toDouble(),"count",definition="swipe.dismissals"),
                    text("lastDismissal",lastDismissal,definition="swipe.last-dismissal"))),
                ProbePoint("foreground","foreground","container",geometry(foreground)),
                ProbePoint("background","background","container",geometry(background)))
        }
    }
    companion object {
        private fun desktopContext():PlatformContext {
            val empty=PlatformContext.Empty()
            return object:PlatformContext by empty {
                override val windowInfo=object:WindowInfo by empty.windowInfo {
                    override val containerSize=IntSize(520*ComposeHost.RenderScale,160*ComposeHost.RenderScale)
                    override val isWindowFocused=true
                }
                override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                    override val touchSlop=18f*ComposeHost.RenderScale
                }
            }
        }
    }
}

@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class SnackbarHostConfig(val action:String,val rtl:Boolean=false,val overlap:Boolean=false)

@ParitySuite("snackbar-host") class SnackbarHostCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) {
        val config=writer.readConfig<SnackbarHostConfig>()
        ComposeHost(writer,192,128).use {host ->
            val scene=Scene(config,host.scope)
            host.mount {scene.Content()}; host.frame(-1000);host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long) {while(nextFrame<=time) {host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun change(time:Long,id:String,action:()->Unit) {advance(time);action();writer.record(time,"mutation",id);host.draw();sample(id)}
            fun times(vararg times:Long) {times.forEach {advance(it);host.draw();sample("time-$it")}}
            sample("initial")
            try {
                change(1000,"show-first") {scene.show("first")}
                if(config.action=="interrupt") {
                    times(16000,32000,64000)
                    change(65000,"dismiss-early") {scene.state.currentSnackbarData!!.dismiss()}
                    times(80000,96000)
                    change(97000,"show-second") {scene.show("second")}
                    times(112000,160000,224000,400000,640000)
                } else {
                    if(config.action in setOf("action-queue","cancel-current","cancel-queued")) change(2000,"queue-second") {scene.show("second")}
                    if(config.action=="cancel-queued") change(3000,"cancel-queued") {scene.jobs[1].cancel()}
                    times(16000,32000,64000,112000,224000,400000)
                    when(config.action) {
                        "action-queue" -> {
                            change(401000,"down") {host.input("move",Offset(76f,40f));host.input("down",Offset(76f,40f))}
                            change(402000,"action") {host.input("up",Offset(76f,40f))}
                        }
                        "cancel-current" -> change(401000,"cancel-current") {scene.jobs[0].cancel()}
                        "state" -> change(401000,"replace-state") {scene.state=SnackbarHostState();scene.show("second")}
                        "template" -> change(401000,"replace-template") {scene.width=80}
                        // Replace the content to actually dispose/recreate the Host at these times.
                        // Toggling Compose state twice before a frame would never detach it.
                        "detach" -> {
                            change(401000,"detach") {host.mount {scene.Content(showHost=false)}}
                            change(402000,"reattach") {host.mount {scene.Content()}}
                        }
                        else -> change(401000,"dismiss") {scene.state.currentSnackbarData!!.dismiss()}
                    }
                    times(416000,448000,512000,640000,800000)
                }
            } finally {scene.jobs.forEach {it.cancel()};host.draw()}
        }
    }
    private class Scene(val config:SnackbarHostConfig,val scope:CoroutineScope) {
        var state by mutableStateOf(SnackbarHostState())
        var width by mutableStateOf(120)
        val jobs=mutableListOf<Job>()
        val layers=linkedMapOf<SnackbarData,Probe>()
        var completed=0
        fun show(message:String) {jobs+=scope.launch {state.showSnackbar(message,"action",duration=SnackbarDuration.Indefinite);completed++}}
        @Composable fun Content(showHost:Boolean=true) {
            MaterialTheme(motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                      CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        if(showHost) SnackbarHost(state,Modifier.absoluteOffset(16.dp,16.dp)) {data ->
                            val probe=remember(data) {Probe()}
                            DisposableEffect(data) {layers[data]=probe;onDispose {layers.remove(data)}}
                            Canvas(probe.modifier().size(width.dp,48.dp).pointerInput(data) {detectTapGestures {data.performAction()}}) {
                                drawRect(if(data.visuals.message=="second")Color.Blue else Color.Red)
                                if(config.overlap) drawRect(Color.Blue,Offset((size.width-40.dp.toPx())/2,12.dp.toPx()),androidx.compose.ui.geometry.Size(40.dp.toPx(),24.dp.toPx()))
                            }
                        }
                      }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> = listOf(ProbePoint("host","snackbar-host",null,listOf(
            text("current",state.currentSnackbarData?.visuals?.message ?: "none",definition="snackbar-host.current-message"),
            number("layers",layers.size.toDouble(),"count",definition="snackbar-host.retained-layer-count"),
            number("completed",completed.toDouble(),"count",definition="snackbar-host.completed-request-count"),
            number("cancelled",jobs.count {it.isCancelled}.toDouble(),"count",definition="snackbar-host.cancelled-request-count"))))+
            layers.map {(data,probe) ->
                val coordinator=generateSequence(checkNotNull(probe.coordinates) as Any) {field(it,"wrappedBy")}
                    .first {field(it,"layer")!=null}
                ProbePoint(data.visuals.message,"snackbar-item","host",probe.geometry().filter {it.name=="size"}+listOf(
                    number("alpha",(field(coordinator,"lastLayerAlpha") as Float).toDouble(),"ratio",category="motion"),
                    number("scale",(field(checkNotNull(field(coordinator,"layerPositionalProperties")),"scaleX") as Float).toDouble(),"ratio",category="motion")))
            }
    }
}

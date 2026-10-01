package parity

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.*
import kotlinx.coroutines.launch
import kotlinx.serialization.json.*

@ParitySuite("motion")
class MotionCases {
    @ParityScenario("interrupted")
    suspend fun motionInterrupted(w:CaptureWriter) {
        w.readConfig<EmptyConfig>()
        ComposeHost(w).use { h -> val p=Probe(); val value=Animatable(0f); var animationScope:kotlinx.coroutines.CoroutineScope?=null
            h.mount { animationScope=rememberCoroutineScope()
                Box(Modifier.fillMaxSize().background(Color.White)) {
                    Box(Modifier.offset(x=(24+value.value).dp,y=64.dp).size(18.dp).then(p.modifier()).background(Color(0xff6750a4)))
                } }
            h.frame(-2000); h.frame(-1000)
            fun target(valueTo:Float) { w.record(h.timeUs,"target",valueTo.toString(),buildJsonObject { put("value",valueTo);put("unit","dip") }); animationScope!!.launch { value.animateTo(valueTo,tween(100,easing=LinearEasing)) } }
            fun sample(id:String,image:Boolean) { w.submit(h.snapshot(id,"interrupted-tween",listOf(ProbePoint("marker","container",null,p.geometry()+listOf(
                number("value",value.value.toDouble(),"dip",category="motion"),number("velocity",value.velocity.toDouble(),"dip/s",category="motion"),flag("running",value.isRunning,"motion")))),image)) }
            target(100f)
            for(time in listOf(0L,16000L,40000L)) { h.frame(time); sample("forward-$time",time!=16000L) }
            target(-20f)
            for(time in listOf(56000L,80000L,140000L,160000L)) { h.frame(time); sample("reverse-$time",time!=80000L) }
        }
    }
}

@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.animation.core.*
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import kotlinx.serialization.Serializable

@Serializable data class TweenConfig(val a:Double,val b:Double,val c:Double,val d:Double,val duration:Int=125,val delay:Int=0)

@ParitySuite("animation-tween")
class TweenCases {
    @ParityScenario("sample") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TweenConfig>()
        val easing=CubicBezierEasing(c.a.toFloat(),c.b.toFloat(),c.c.toFloat(),c.d.toFloat())
        val animation=TargetBasedAnimation(tween<Float>(c.duration,c.delay,easing),Float.VectorConverter,2f,40f,3f)
        var value by mutableFloatStateOf(2f)
        ComposeHost(writer,240,96).use { host ->
            host.mount {
                Canvas(Modifier.fillMaxSize()) {
                    drawRect(Color.White)
                    drawRect(Color.Blue,Offset((24f+value*3f)*density,36f*density),Size(12f*density,24f*density))
                }
            }
            for(time in listOf(0L,1,1000,16000,17000,18000,32000,48000,64000,96000,124999,125000,141999,142000,160000)) {
                host.advanceTo(time)
                value=animation.getValueFromNanos(time*1000)
                host.draw()
                writer.submit(host.snapshot("time-$time","time-$time",listOf(ProbePoint("animation","tween",null,listOf(
                    number("value",value.toDouble(),category="motion",definition="tween.value",source="TargetBasedAnimation.getValueFromNanos"),
                    number("velocity",animation.getVelocityFromNanos(time*1000).toDouble(),"dip/s",category="motion",definition="tween.velocity",source="TargetBasedAnimation.getVelocityFromNanos"),
                    number("easing",easing.transform(time.toFloat()/125000f).toDouble(),"fraction",category="motion",definition="tween.easing",source="CubicBezierEasing.transform"),
                    flag("finished",animation.isFinishedFromNanos(time*1000),definition="tween.finished"))))))
            }
        }
    }
}

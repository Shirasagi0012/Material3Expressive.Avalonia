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

@Serializable data class SpringConfig(val damping:Double,val stiffness:Double,val initial:Float=2f,val target:Float=40f,val velocity:Float=3f,val threshold:Float=.01f)

@ParitySuite("animation-spring")
class SpringCases {
    @ParityScenario("sample") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SpringConfig>()
        val animation=TargetBasedAnimation(spring<Float>(c.damping.toFloat(),c.stiffness.toFloat(),c.threshold),Float.VectorConverter,c.initial,c.target,c.velocity)
        var value by mutableFloatStateOf(c.initial)
        ComposeHost(writer,240,96).use { host ->
            host.mount {
                Canvas(Modifier.fillMaxSize()) {
                    val fraction=if(c.initial==c.target) 1f else (value-c.initial)/(c.target-c.initial)
                    drawRect(Color.White)
                    drawRect(Color.Blue,Offset((24f+fraction*160f)*density,36f*density),Size(12f*density,24f*density))
                }
            }
            for(time in listOf(0L,1,999,1000,1001,15999,16000,16001,32000,64000,96000,160000,320000,640000,2000000)) {
                host.advanceTo(time); value=animation.getValueFromNanos(time*1000); host.draw()
                writer.submit(host.snapshot("time-$time","time-$time",listOf(ProbePoint("animation","spring",null,listOf(
                    number("value",value.toDouble(),category="motion",definition="spring.value"),
                    number("velocity",animation.getVelocityFromNanos(time*1000).toDouble(),"dip/s",category="motion",definition="spring.velocity"),
                    number("duration",animation.durationNanos/1000000.0,"ms",category="motion",definition="spring.duration"),
                    flag("finished",animation.isFinishedFromNanos(time*1000),definition="spring.finished"))))))
            }
        }
    }
}

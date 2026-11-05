@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.*
import androidx.compose.foundation.layout.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.*
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.unit.IntOffset
import kotlin.math.roundToInt
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.*

@Serializable data class PointerVelocityConfig(val positions:List<Float>,val times:List<Long>,val release:Long,val moveContent:Boolean=false)

@ParitySuite("pointer-velocity")
class PointerVelocityCases {
    @ParityScenario("drag") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<PointerVelocityConfig>()
        var delta by mutableFloatStateOf(0f);var velocity=0f;var stops=0
        val empty=PlatformContext.Empty()
        val context=object:PlatformContext by empty {
            override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
        ComposeHost(writer,120,160,context).use { host ->
            host.mount {
                Box(Modifier.fillMaxSize().then(if(c.moveContent)Modifier.offset { IntOffset(0,(delta*ComposeHost.RenderScale).roundToInt()) }else Modifier)
                    .background(Color.Black).pointerInput(Unit) {
                        awaitPointerEventScope { while(true) {
                            val event=awaitPointerEvent(PointerEventPass.Initial)
                            if(event.type==PointerEventType.Move) event.changes.forEach {
                                writer.record(host.timeUs,"pointer-observation","move",buildJsonObject {
                                    put("timestampMs",it.uptimeMillis);put("sceneY",it.originalEventPosition.y/ComposeHost.RenderScale);put("localY",it.position.y/ComposeHost.RenderScale)
                                })
                            }
                        } }
                    }.draggable(
                    orientation=Orientation.Vertical,
                    state=rememberDraggableState { delta+=it/ComposeHost.RenderScale },
                    onDragStopped={velocity=it/ComposeHost.RenderScale;stops++}))
            };host.frame(0)
            c.times.forEachIndexed { i,time ->
                host.advanceTo(time);host.input(if(i==0)"down" else "move",Offset(60f,c.positions[i]))
                if(c.moveContent)host.draw()
            }
            host.advanceTo(c.release);host.input("up",Offset(60f,c.positions.last()))
            writer.submit(host.snapshot("release","release",listOf(ProbePoint("gesture","vertical-drag",null,
                listOf(number("delta",delta.toDouble(),"dip"),number("velocity",velocity.toDouble(),"dip/s"),
                    number("stops",stops.toDouble(),"count")))),image=false))
        }
    }
}

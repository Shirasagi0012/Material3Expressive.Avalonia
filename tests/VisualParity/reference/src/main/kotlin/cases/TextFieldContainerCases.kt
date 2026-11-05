@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class,androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class,androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable data class TextFieldContainerConfig(val outlined:Boolean,val action:String,val slow:Boolean=false,val shape:String="default")

@ParitySuite("text-field-container")
class TextFieldContainerCases {
    @ParityScenario("container") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TextFieldContainerConfig>()
        ComposeHost(writer,320,120).use { host ->
            val probe=Probe()
            var source by mutableStateOf(MutableInteractionSource())
            var enabled by mutableStateOf(true);var error by mutableStateOf(false);var dark by mutableStateOf(false)
            val base=MotionScheme.expressive()
            val motion=if(c.slow)object:MotionScheme by base {
                override fun<T> fastSpatialSpec():FiniteAnimationSpec<T> = spring(.5f,100f)
                override fun<T> fastEffectsSpec():FiniteAnimationSpec<T> = spring(.7f,100f)
            } else base
            host.mount {
                MaterialTheme(colorScheme=if(dark)darkColorScheme()else lightColorScheme(),motionScheme=motion) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val modifier=probe.modifier().size(280.dp,80.dp)
                        val shape=when(c.shape){
                            "square"->RoundedCornerShape(0.dp)
                            "small"->RoundedCornerShape(.1.dp)
                            "asymmetric"->RoundedCornerShape(.1.dp,4.dp,12.dp,0.dp)
                            else->OutlinedTextFieldDefaults.shape
                        }
                        if(c.outlined)OutlinedTextFieldDefaults.Container(enabled,error,source,modifier,shape=shape)
                        else TextFieldDefaults.Container(enabled,error,source,modifier)
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("container","text-field-container",null,
                probe.geometry().filter { it.name=="box"||it.name=="size" }))))
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            val first=FocusInteraction.Focus();val second=FocusInteraction.Focus()
            sample("initial");change(1000,"focus"){source.tryEmit(first)}
            at(16000);at(32000);at(64000)
            change(65000,"change"){
                when(c.action){
                    "reverse"->source.tryEmit(FocusInteraction.Unfocus(first))
                    "error"->error=true
                    "disable"->enabled=false
                    "replace"->source=MutableInteractionSource()
                    "theme"->dark=true
                }
            }
            at(80000);at(96000)
            if(c.action=="reverse")change(97000,"refocus"){source.tryEmit(second)}
            if(c.action=="replace")change(97000,"new-source-event"){source.tryEmit(HoverInteraction.Enter())}
            at(112000);at(160000);at(240000);at(400000);at(800000)
            change(801000,"end"){
                if(c.action=="disable")enabled=true
                source.tryEmit(FocusInteraction.Unfocus(if(c.action=="reverse")second else first))
            }
            at(864000);at(960000);at(1200000);at(2000000)
        }
    }
}

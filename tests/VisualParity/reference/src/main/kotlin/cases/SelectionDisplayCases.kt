@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.PressInteraction
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class SelectionDisplayConfig(val kind:String="checkbox",val state:String="off",val enabled:Boolean=true,
    val dark:Boolean=false,val rtl:Boolean=false,val width:Double?=null,val height:Double?=null,
    val action:String="none",val target:String="on",val icon:Boolean=false)

@ParitySuite("selection-display")
class SelectionDisplayCases {
    @ParityScenario("display") suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<SelectionDisplayConfig>()
        ComposeHost(writer,160,128).use { host ->
            var state by mutableStateOf(config.state)
            var enabled by mutableStateOf(config.enabled)
            val probe=Probe()
            val source=MutableInteractionSource()
            host.mount {
                MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            var modifier=probe.modifier()
                            config.width?.let { modifier=modifier.width(it.toFloat().dp) }
                            config.height?.let { modifier=modifier.height(it.toFloat().dp) }
                            if(config.kind=="checkbox") TriStateCheckbox(
                                state=when(state){"on"->ToggleableState.On;"off"->ToggleableState.Off;else->ToggleableState.Indeterminate},
                                onClick=null,modifier=modifier,enabled=enabled)
                            else if(config.kind=="radio") RadioButton(selected=state=="on",onClick=null,modifier=modifier,enabled=enabled)
                            else Switch(checked=state=="on",onCheckedChange=null,modifier=modifier,enabled=enabled,
                                interactionSource=source,thumbContent=if(config.icon){{Box(Modifier.size(8.dp).background(Color.Red))}}else null)
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("indicator","indicator",null,
                probe.geometry().filter{it.name=="box"||it.name=="size"}+text("state",state,definition="selection.display.state")))))
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            fun times(vararg values:Long){values.forEach{advance(it);host.draw();sample("time-$it")}}
            sample("initial")
            if(config.action=="none")return
            if(config.action=="source") {
                val press=PressInteraction.Press(Offset(26f,16f)*ComposeHost.RenderScale.toFloat())
                change(1000,"press"){source.tryEmit(press)};times(16000,64000)
                change(65000,"release"){source.tryEmit(PressInteraction.Release(press))}
                times(80000,112000,160000,240000,400000,800000);return
            }
            if(config.action=="input") {
                val coordinates=checkNotNull(probe.coordinates)
                val center=coordinates.localToWindow(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
                change(1000,"press"){host.input("move",center);host.input("down",center)}
                times(16000,64000)
                change(65000,"release"){host.input("up",center)};times(80000,400000);return
            }
            change(1000,"change"){if(config.action=="disable")enabled=false else state=config.target}
            times(16000,32000,64000)
            if(config.action=="reverse")change(65000,"reverse"){state=config.state}
            if(config.action=="disable")change(65000,"enable"){enabled=true}
            times(80000,112000,160000,240000,400000,800000)
        }
    }
}

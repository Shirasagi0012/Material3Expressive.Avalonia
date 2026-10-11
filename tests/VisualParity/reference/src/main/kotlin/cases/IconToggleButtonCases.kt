@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class IconToggleConfig(val selected:Boolean=false,val enabled:Boolean=true,val dark:Boolean=false,val action:String="none")
@ParitySuite("icon-toggle-button")
class IconToggleButtonCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<IconToggleConfig>()
        ComposeHost(writer,120,100).use {host ->
            var selected by mutableStateOf(config.selected);var enabled by mutableStateOf(config.enabled);var requests=0
            val button=Probe()
            host.mount {
                MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalContentColor provides Color(0xff245a80)) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            IconToggleButton(checked=selected,onCheckedChange={requests++;if(config.action!="ignore")selected=it},enabled=enabled,modifier=button.modifier()) {
                                val color=LocalContentColor.current
                                Canvas(Modifier.size(24.dp)){drawRect(color,Offset(4.dp.toPx(),4.dp.toPx()),Size(size.width-8.dp.toPx(),size.height-8.dp.toPx()))}
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("button","container",null,button.geometry().filter{it.name=="box"||it.name=="size"}),
                ProbePoint("state","state",null,listOf(flag("checked",selected,definition="icon-toggle.checked"),number("requests",requests.toDouble(),"count",definition="icon-toggle.requests"))))))
            sample("initial");if(!dynamic)return
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"input",id);host.draw();host.frame(time);sample(id)}
            val center=Offset(60f,50f)
            change(1000,"down"){host.input("move",center);host.input("down",center)}
            advance(64000);host.draw();sample("pressed")
            if(config.action=="disable")change(65000,"disabled"){enabled=false}
            change(80000,"up"){host.input("up",center)}
            listOf(96000L,160000L,320000L,800000L).forEach{advance(it);host.draw();sample("time-$it")}
            change(801000,"leave"){host.input("move",Offset(4f,4f))}
            advance(1200000);host.draw();sample("late")
        }
    }
}

@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class ProgressStopIndicatorConfig(val mode:String="standalone",val cap:String="round",val rtl:Boolean=false,
    val width:Double=120.0,val height:Double=20.0,val stop:Double=8.0,val bypass:Boolean=false,val dynamic:Boolean=false)

@ParitySuite("progress-stop-indicator")
class ProgressStopIndicatorCases {
    @ParityScenario("capture")
    suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<ProgressStopIndicatorConfig>()
        var width by mutableStateOf(c.width);var height by mutableStateOf(c.height);var stop by mutableStateOf(c.stop)
        var rtl by mutableStateOf(c.rtl);var color by mutableStateOf(Color.Black);var opacity by mutableStateOf(1f)
        var cap by mutableStateOf(when(c.cap){"flat"->StrokeCap.Butt;"square"->StrokeCap.Square;else->StrokeCap.Round})
        var attached by mutableStateOf(true);var callback by mutableStateOf("composite")
        val probe=Probe()
        ComposeHost(writer,240,120).use { host ->
            host.mount {
                MaterialTheme {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            if(attached) {
                                val modifier=probe.modifier().size(width.dp,height.dp).graphicsLayer { alpha=opacity }
                                if(c.mode=="standalone") Canvas(modifier) { ProgressIndicatorDefaults.drawStopIndicator(this,stop.dp,color,cap) }
                                else if(callback=="default") LinearProgressIndicator({.75f},modifier,Color.Blue,Color(0xFFCCCCCC),cap)
                                else LinearProgressIndicator({.75f},modifier,Color.Blue,Color(0xFFCCCCCC),cap) {
                                    if(callback!="none") {
                                        ProgressIndicatorDefaults.drawStopIndicator(this,stop.dp,color,cap)
                                        ProgressIndicatorDefaults.drawStopIndicator(this,(stop/2).dp,Color.Red,StrokeCap.Round)
                                    }
                                }
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("owner","owner",null,probe.geometry().filter{it.name=="box"||it.name=="size"}))))
            sample("initial");if(!c.dynamic)return
            fun change(time:Long,id:String,action:()->Unit){host.advanceTo(time);action();host.draw();host.frame(time);sample(id)}
            change(1000,"clamped-size"){height=6.0;stop=14.0}
            change(17000,"direction"){rtl=!c.rtl}
            change(33000,"color-opacity"){color=Color(0x804488CC);opacity=.5f}
            change(49000,"shape-size"){height=28.0;width=80.0;stop=10.0;cap=StrokeCap.Square}
            change(65000,"detach-reattach"){attached=false;host.draw();attached=true}
            if(c.mode=="callback") {
                change(81000,"no-stop"){callback="none"}
                change(97000,"default-stop"){callback="default"}
                change(113000,"restore-composite"){callback="composite"}
            }
        }
    }
}

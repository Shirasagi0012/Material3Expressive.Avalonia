@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.material3.internal.Icons
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class SnackbarConfig(val hasAction:Boolean=true,val hasDismiss:Boolean=false,val newLine:Boolean=false,
    val dark:Boolean=false,val rtl:Boolean=false,val buttons:Boolean=false,val contentWidth:Double=64.0,val contentHeight:Double=12.0,
    val dismissWidth:Double=24.0,val hostWidth:Int=720,val width:Double?=null,val height:Double?=null,
    val minimumWidth:Double=0.0,val colors:String="default",val mutation:String="none")

@ParitySuite("snackbar") class SnackbarCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<SnackbarConfig>()
        ComposeHost(writer,config.hostWidth,240).use {host ->
            val s=Scene(config);host.mount {s.Content()};host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure()))
            sample("initial");if(!dynamic) return
            var nextFrame=16000L
            fun advance(time:Long) {while(nextFrame<=time) {host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit) {advance(time);action();writer.record(time,"mutation",id);host.frame(time);host.draw();sample(id)}
            fun times(vararg times:Long) {times.forEach {advance(it);host.draw();sample("time-$it")}}
            when(config.mutation) {
                "slots" -> {change(1000,"dismiss-only") {s.hasAction=false;s.hasDismiss=true};change(2000,"both") {s.hasAction=true};change(3000,"none") {s.hasAction=false;s.hasDismiss=false}}
                "new-line" -> {change(1000,"new-line") {s.newLine=true};change(2000,"row") {s.newLine=false}}
                "resize" -> {change(1000,"wide") {s.width=400.0};change(2000,"tall") {s.width=20.0;s.height=80.0}}
                "colors" -> {change(1000,"custom") {s.colors="custom"};change(2000,"default") {s.colors="default"}}
                "theme" -> {change(1000,"dark") {s.dark=true};change(2000,"light") {s.dark=false}}
                "action","dismiss" -> {
                    val p=checkNotNull(s.probes[config.mutation]?.coordinates)
                    val point=p.localToWindow(Offset(p.size.width/2f,p.size.height/2f))/ComposeHost.RenderScale.toFloat()
                    change(1000,"down") {host.input("move",point);host.input("down",point)}
                    times(16000,32000,64000,112000)
                    change(113000,"up") {host.input("up",point)};times(128000,176000,320000,640000)
                }
            }
        }
    }
    private class Scene(val config:SnackbarConfig) {
        var hasAction by mutableStateOf(config.hasAction);var hasDismiss by mutableStateOf(config.hasDismiss)
        var newLine by mutableStateOf(config.newLine);var dark by mutableStateOf(config.dark)
        var width by mutableStateOf(config.contentWidth);var height by mutableStateOf(config.contentHeight)
        var colors by mutableStateOf(config.colors)
        val probes=linkedMapOf("snackbar" to Probe(),"content" to Probe(),"action" to Probe(),"dismiss" to Probe())
        val painted=mutableMapOf<String,Color>();var actions=0;var dismissals=0
        @Composable fun marker(name:String,width:Double,height:Double) {
            val color=LocalContentColor.current
            Canvas(checkNotNull(probes[name]).modifier().size(width.toFloat().dp,height.toFloat().dp)) {painted[name]=color;drawRect(color)}
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=checkNotNull(probes["snackbar"]).modifier()
                        if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                        if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                        if(config.minimumWidth>0) modifier=modifier.widthIn(min=config.minimumWidth.toFloat().dp)
                        val background=when(colors) {"custom" -> Color(0xFFE0F0E8);"surface" -> Color(0xFFFEF7FF);"transparent" -> Color.Transparent;else -> SnackbarDefaults.color}
                        val actionColor=if(colors=="custom") Color.Blue else SnackbarDefaults.actionContentColor
                        val dismissColor=if(colors=="custom") Color(0xFF00FF00) else SnackbarDefaults.dismissActionContentColor
                        val actionSlot:(@Composable ()->Unit)?=if(!hasAction) null else if(config.buttons) {
                            {TextButton(onClick={actions++},colors=ButtonDefaults.textButtonColors(contentColor=actionColor)) {marker("action",40.0,16.0)}}
                        } else {{marker("action",80.0,24.0)}}
                        val dismissSlot:(@Composable ()->Unit)?=if(!hasDismiss) null else if(config.buttons) {
                            {IconButton(onClick={dismissals++},modifier=checkNotNull(probes["dismiss"]).modifier(),colors=IconButtonDefaults.iconButtonColors(contentColor=dismissColor)) {Icon(Icons.Filled.Close,null)}}
                        } else {{marker("dismiss",config.dismissWidth,24.0)}}
                        Snackbar(modifier=modifier,action=actionSlot,dismissAction=dismissSlot,actionOnNewLine=newLine,
                            containerColor=background,contentColor=if(colors=="custom") Color.Red else SnackbarDefaults.contentColor,
                            actionContentColor=actionColor,dismissActionContentColor=dismissColor,
                            shape=if(colors=="custom") RoundedCornerShape(12.dp) else SnackbarDefaults.shape) {marker("content",width,height)}
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            val result=mutableListOf(ProbePoint("snackbar","snackbar",null,geometry("snackbar")+listOf(
                number("actions",actions.toDouble(),"count",definition="snackbar.action-callback-count"),
                number("dismissals",dismissals.toDouble(),"count",definition="snackbar.dismiss-callback-count"))))
            fun add(name:String,color:Boolean=true) {
                val measurements=geometry(name).toMutableList()
                if(color) {val argb=checkNotNull(painted[name]).toArgb();measurements+=vector("paint","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","snackbar.slot.painted-solid-srgba8","Canvas actual inherited LocalContentColor")}
                result+=ProbePoint(name,name,"snackbar",measurements)
            }
            add("content");if(hasAction) add("action");if(hasDismiss) add("dismiss",!config.buttons)
            return result
        }
        fun geometry(name:String):List<Measurement> {
            val c=checkNotNull(probes[name]?.coordinates);val p=c.localToWindow(Offset.Zero)/ComposeHost.RenderScale.toFloat()
            return listOf(vector("box","rect",listOf(p.x.toDouble(),p.y.toDouble(),c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),"dip","scene","geometry","layout.box.aabb.unclipped","LayoutCoordinates.size + localToWindow / density"),
                vector("size","point",listOf(c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),"dip","local","geometry","layout.box.local-size","LayoutCoordinates.size / density",listOf("width","height")))
        }
    }
}

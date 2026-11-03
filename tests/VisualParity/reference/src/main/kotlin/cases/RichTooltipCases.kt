@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.MutatorMutex
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.layout.FirstBaseline
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import androidx.compose.ui.window.PopupPositionProvider
import kotlinx.serialization.Serializable

@Serializable
data class RichTooltipConfig(val title:Boolean=true,val action:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,
    val contentWidth:Double=64.0,val contentHeight:Double=8.0,val width:Double?=null,val height:Double?=null,
    val maximum:Double=320.0,val baseline:Double?=null,val colors:String="default",val mutation:String="none")

@ParitySuite("rich-tooltip")
class RichTooltipCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<RichTooltipConfig>()
        ComposeHost(writer,480,300).use { host ->
            val s=Scene(config);host.mount {s.Content()};host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure()))
            sample("initial");if(!dynamic) return
            fun change(time:Long,id:String,action:()->Unit) {
                host.advanceTo(time);action();writer.record(time,"mutation",id);host.frame(time);sample(id)
            }
            when(config.mutation) {
                "slots" -> {
                    change(1000,"text-only") {s.title=false;s.action=false}
                    change(2000,"action") {s.action=true}
                    change(3000,"all") {s.title=true}
                }
                "theme" -> {change(1000,"dark") {s.dark=true};change(2000,"light") {s.dark=false}}
                "colors" -> {change(1000,"custom") {s.colors="custom"};change(2000,"default") {s.colors="default"}}
                "content" -> {change(1000,"wide") {s.width=400.0};change(2000,"tall") {s.width=8.0;s.height=64.0}}
            }
        }
    }
    private class Scene(val config:RichTooltipConfig) {
        var title by mutableStateOf(config.title);var action by mutableStateOf(config.action)
        var dark by mutableStateOf(config.dark);var colors by mutableStateOf(config.colors)
        var width by mutableStateOf(config.contentWidth);var height by mutableStateOf(config.contentHeight)
        val probes=linkedMapOf("tooltip" to Probe(),"title" to Probe(),"content" to Probe(),"action" to Probe())
        val painted=mutableMapOf<String,Color>()
        val position=object:PopupPositionProvider {
            override fun calculatePosition(anchorBounds:IntRect,windowSize:IntSize,layoutDirection:LayoutDirection,popupContentSize:IntSize)=
                IntOffset((windowSize.width-popupContentSize.width)/2,(windowSize.height-popupContentSize.height)/2)
        }
        @Composable fun marker(name:String,width:Double,height:Double) {
            val foreground=LocalContentColor.current
            val modifier=checkNotNull(probes[name]).modifier().size(width.toFloat().dp,height.toFloat().dp)
            if(config.baseline!=null && name!="action") {
                Layout(content={Canvas(Modifier.fillMaxSize()) {painted[name]=foreground;drawRect(foreground)}},modifier=modifier) {measurables,constraints ->
                    val child=measurables.single().measure(constraints)
                    layout(child.width,child.height,mapOf(FirstBaseline to config.baseline.toFloat().dp.roundToPx())) {child.place(0,0)}
                }
            } else Canvas(modifier) {painted[name]=foreground;drawRect(foreground)}
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                        val state=rememberTooltipState(initialIsVisible=true,mutatorMutex=remember {MutatorMutex()})
                        TooltipBox(positionProvider=position,state=state,enableUserInput=false,tooltip={
                            var modifier=checkNotNull(probes["tooltip"]).modifier()
                            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                            val background=when(colors) {"custom" -> Color(0xFFE0F0E8);"surface" -> MaterialTheme.colorScheme.surface;else -> MaterialTheme.colorScheme.surfaceContainer}
                            val palette=if(colors=="custom") TooltipDefaults.richTooltipColors(containerColor=background,contentColor=Color.Blue,
                                titleContentColor=Color.Red,actionContentColor=Color(0xFF008000)) else TooltipDefaults.richTooltipColors(containerColor=background)
                            val titleSlot:(@Composable ()->Unit)?=if(title) {{marker("title",32.0,10.0)}} else null
                            val actionSlot:(@Composable ()->Unit)?=if(action) {{marker("action",24.0,12.0)}} else null
                            RichTooltip(modifier=modifier,title=titleSlot,action=actionSlot,maxWidth=config.maximum.toFloat().dp,
                                colors=palette,shape=if(colors=="custom") RoundedCornerShape(20.dp) else TooltipDefaults.richTooltipContainerShape,
                                tonalElevation=if(colors=="surface") 6.dp else 0.dp,shadowElevation=if(colors=="flat") 0.dp else 3.dp) {
                                marker("content",width,height)
                            }
                        }) {Box(Modifier.size(1.dp))}
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe):List<Measurement> {
                val c=checkNotNull(probe.coordinates);val origin=c.localToWindow(Offset.Zero)/ComposeHost.RenderScale.toFloat()
                return listOf(vector("box","rect",listOf(origin.x.toDouble(),origin.y.toDouble(),c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),
                    "dip","scene","geometry","layout.box.aabb.unclipped","LayoutCoordinates.size + localToWindow / density"),
                    vector("size","point",listOf(c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),"dip","local","geometry","layout.box.local-size","LayoutCoordinates.size / density",listOf("width","height")))
            }
            return listOf("tooltip","title","content","action").filter {it!="title" || title}.filter {it!="action" || action}.map {name ->
                val fields=geometry(checkNotNull(probes[name])).toMutableList()
                if(name!="tooltip") {
                    val argb=(painted[name] ?: Color.Transparent).toArgb()
                    fields+=vector("paint","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                        "srgba8","none","properties","rich-tooltip.slot.painted-solid-srgba8","Canvas actual inherited LocalContentColor.toArgb()")
                }
                ProbePoint(name,name,if(name=="tooltip") null else "tooltip",fields)
            }
        }
    }
}

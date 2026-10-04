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
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import androidx.compose.ui.window.PopupPositionProvider
import kotlinx.serialization.Serializable

@Serializable
data class PlainTooltipConfig(val contentWidth:Double=24.0,val contentHeight:Double=8.0,
    val dark:Boolean=false,val rtl:Boolean=false,val colors:String="default",val width:Double?=null,
    val height:Double?=null,val maximum:Double=200.0,val minimum:Double=0.0,val action:String="none")

@ParitySuite("plain-tooltip")
class PlainTooltipCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<PlainTooltipConfig>()
        ComposeHost(writer,400,240).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            sample("initial")
            if(!dynamic) return
            fun change(time:Long,id:String,action:()->Unit) {
                host.advanceTo(time);action();writer.record(time,"mutation",id);host.frame(time);sample(id)
            }
            when(config.action) {
                "theme" -> {change(1000,"dark") {scene.dark=true};change(2000,"light") {scene.dark=false}}
                "content" -> {
                    change(1000,"wide") {scene.width=300.0}
                    change(2000,"tall") {scene.width=8.0;scene.height=48.0}
                    change(3000,"empty") {scene.width=0.0;scene.height=0.0}
                }
                "colors" -> {change(1000,"custom") {scene.colors="custom"};change(2000,"default") {scene.colors="default"}}
            }
        }
    }
    private class Scene(val config:PlainTooltipConfig) {
        var dark by mutableStateOf(config.dark)
        var width by mutableStateOf(config.contentWidth)
        var height by mutableStateOf(config.contentHeight)
        var colors by mutableStateOf(config.colors)
        val tooltip=Probe();val content=Probe();var painted=Color.Transparent
        // PlainTooltip is a TooltipScope extension. Its real TooltipBox supplies that
        // scope here; the fixture provider centers the isolated surface in the host.
        val position=object:PopupPositionProvider {
            override fun calculatePosition(anchorBounds:IntRect,windowSize:IntSize,layoutDirection:LayoutDirection,popupContentSize:IntSize)=
                IntOffset((windowSize.width-popupContentSize.width)/2,(windowSize.height-popupContentSize.height)/2)
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                        val state=rememberTooltipState(initialIsVisible=true,mutatorMutex=remember {MutatorMutex()})
                        TooltipBox(positionProvider=position,state=state,enableUserInput=false,tooltip={
                            var modifier=tooltip.modifier()
                            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                            if(config.minimum>0) modifier=modifier.widthIn(min=config.minimum.toFloat().dp)
                            val background=when(colors) {"custom" -> Color(0xFFE0F0E8);"surface" -> MaterialTheme.colorScheme.surface;else -> TooltipDefaults.plainTooltipContainerColor}
                            PlainTooltip(modifier=modifier,maxWidth=config.maximum.toFloat().dp,
                                containerColor=background,
                                contentColor=if(colors=="custom") Color.Blue else TooltipDefaults.plainTooltipContentColor,
                                shape=if(colors=="custom") RoundedCornerShape(12.dp) else TooltipDefaults.plainTooltipContainerShape,
                                tonalElevation=if(colors=="surface") 6.dp else 0.dp,
                                shadowElevation=if(colors=="elevated") 6.dp else 0.dp) {
                                val foreground=LocalContentColor.current
                                Canvas(content.modifier().size(width.toFloat().dp,height.toFloat().dp)) {painted=foreground;drawRect(foreground)}
                            }
                        }) {Box(Modifier.size(1.dp))}
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe):List<Measurement> {
                val c=checkNotNull(probe.coordinates)
                val origin=c.localToWindow(Offset.Zero)/ComposeHost.RenderScale.toFloat()
                return listOf(vector("box","rect",listOf(origin.x.toDouble(),origin.y.toDouble(),c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),
                    "dip","scene","geometry","layout.box.aabb.unclipped","LayoutCoordinates.size + localToWindow / density"),
                    vector("size","point",listOf(c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),"dip","local","geometry","layout.box.local-size","LayoutCoordinates.size / density",listOf("width","height")))
            }
            val argb=painted.toArgb()
            return listOf(ProbePoint("tooltip","tooltip",null,geometry(tooltip)),
                ProbePoint("content","content","tooltip",geometry(content)+vector("paint","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","plain-tooltip.content.painted-solid-srgba8","Canvas actual inherited LocalContentColor.toArgb()")))
        }
    }
}

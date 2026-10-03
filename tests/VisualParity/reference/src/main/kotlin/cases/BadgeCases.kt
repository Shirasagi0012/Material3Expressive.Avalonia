@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class BadgeConfig(val host:String="badge", val content:String="small", val dark:Boolean=false, val rtl:Boolean=false,
    val colors:String="default", val width:Double?=null, val height:Double?=null, val boxWidth:Double?=null, val boxHeight:Double?=null,
    val boxMinWidth:Double=0.0, val boxMinHeight:Double=0.0, val maxWidth:Double?=null, val maxHeight:Double?=null,
    val anchorWidth:Double=24.0, val anchorHeight:Double=24.0, val action:String="none", val start:Boolean=false)

@ParitySuite("badge")
class BadgeCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,200,160).use { host ->
            val scene=Scene(writer.readConfig<BadgeConfig>())
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("dynamic")
    suspend fun dynamic(writer:CaptureWriter) {
        val config=writer.readConfig<BadgeConfig>()
        ComposeHost(writer,200,160).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun mutate(time:Long,id:String,change:()->Unit) {
                host.advanceTo(time); change(); writer.record(time,"mutation",id); host.frame(time); sample(id)
            }
            sample("initial")
            when(config.action) {
                "content" -> {
                    mutate(1000,"dot") { scene.content="dot" }
                    mutate(2000,"wide") { scene.content="wide" }
                    mutate(3000,"empty") { scene.content="empty" }
                    mutate(4000,"small") { scene.content="small" }
                }
                "theme" -> {
                    mutate(1000,"dark") { scene.dark=true }; mutate(2000,"light") { scene.dark=false }
                }
                "colors" -> {
                    mutate(1000,"custom") { scene.colors="custom" }
                    mutate(2000,"primary") { scene.colors="primary" }
                    mutate(3000,"ambient") { scene.colors="custom"; scene.ambient=Color(0xFF008000) }
                }
                "rtl" -> {
                    mutate(1000,"rtl") { scene.rtl=true }; mutate(2000,"ltr") { scene.rtl=false }
                }
                "resize" -> {
                    mutate(1000,"larger") { scene.anchorWidth=40.0; scene.anchorHeight=36.0 }
                    mutate(2000,"smaller") { scene.anchorWidth=16.0; scene.anchorHeight=16.0 }
                }
                "bounds" -> {
                    mutate(1000,"narrow") { scene.itemWidth=60.0 }; mutate(2000,"wide") { scene.itemWidth=96.0 }
                }
                "input","interactive" -> {
                    val point=checkNotNull(scene.anchor.coordinates).localToRoot(Offset(20f,5f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
                    host.advanceTo(1000); host.input("move",point); host.input("down",point); host.draw(); sample("down")
                    host.advanceTo(2000); host.input("up",point); host.draw(); sample("up")
                }
            }
        }
    }

    private class Scene(val config:BadgeConfig) {
        var content by mutableStateOf(config.content)
        var dark by mutableStateOf(config.dark)
        var rtl by mutableStateOf(config.rtl)
        var colors by mutableStateOf(config.colors)
        var ambient by mutableStateOf(Color.Magenta)
        var anchorWidth by mutableStateOf(config.anchorWidth)
        var anchorHeight by mutableStateOf(config.anchorHeight)
        var clicks=0
        var badgeClicks=0
        var itemWidth by mutableStateOf(80.0)
        val badge=Probe(); val box=Probe(); val anchor=Probe(); val item=Probe()
        val contents=listOf(Probe(),Probe())
        val painted=mutableMapOf<Int,Color>()
        var contentCount=0

        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                    LocalContentColor provides ambient) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        when(config.host) {
                            "badge" -> BadgeContent()
                            "box" -> BoxContent()
                            "rail" -> NavigationRailItem(true,{},icon={ BoxContent() },modifier=item.modifier())
                            "bar" -> Row(Modifier.width(itemWidth.toFloat().dp)) { NavigationBarItem(true,{},icon={ BoxContent() },modifier=item.modifier()) }
                            "short" -> ShortNavigationBarItem(true,{},icon={ BoxContent() },modifier=item.modifier().width(80.dp),
                                label=if(config.start) ({ Canvas(Modifier.size(32.dp,16.dp)) { drawRect(Color.Blue) } }) else null,
                                iconPosition=if(config.start) NavigationItemIconPosition.Start else NavigationItemIconPosition.Top)
                            "wide" -> WideNavigationRailItem(true,{},icon={ BoxContent() },modifier=item.modifier(),railExpanded=config.start,
                                label=if(config.start) ({ Canvas(Modifier.size(32.dp,16.dp)) { drawRect(Color.Blue) } }) else null,
                                iconPosition=if(config.start) NavigationItemIconPosition.Start else NavigationItemIconPosition.Top)
                            else -> error(config.host)
                        }
                    }
                }
            }
        }
        @Composable fun BoxContent() {
            var modifier=box.modifier()
            if(config.boxWidth!=null) modifier=modifier.width(config.boxWidth.toFloat().dp)
            if(config.boxHeight!=null) modifier=modifier.height(config.boxHeight.toFloat().dp)
            if(config.boxMinWidth>0 || config.boxMinHeight>0) modifier=modifier.sizeIn(minWidth=config.boxMinWidth.toFloat().dp,minHeight=config.boxMinHeight.toFloat().dp)
            BadgedBox(badge={ BadgeContent() },modifier=modifier) {
                Canvas(anchor.modifier().size(anchorWidth.toFloat().dp,anchorHeight.toFloat().dp)
                    .then(if(config.action in listOf("input","interactive")) Modifier.clickable(interactionSource=null,indication=null) { clicks++ } else Modifier)) { drawRect(Color.Blue) }
            }
        }
        @Composable fun BadgeContent() {
            var modifier=badge.modifier()
            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
            if(config.maxWidth!=null || config.maxHeight!=null) modifier=modifier.sizeIn(
                maxWidth=config.maxWidth?.toFloat()?.dp ?: Dp.Infinity,maxHeight=config.maxHeight?.toFloat()?.dp ?: Dp.Infinity)
            val container=when(colors) { "custom" -> Color(0x408060A0); "primary" -> MaterialTheme.colorScheme.primary; "explicit" -> Color(0xFF000080); else -> BadgeDefaults.containerColor }
            val foreground=if(colors=="explicit") Color(0xFF00FF00) else contentColorFor(container)
            contentCount=when(content) { "dot","empty" -> 0; "row" -> 2; else -> 1 }
            val slot:(@Composable RowScope.()->Unit)?=if(content=="dot") null else ({
                when(content) {
                    "empty" -> Unit
                    "small" -> Marker(0,4.0,8.0)
                    "wide" -> Marker(0,64.0,8.0)
                    "tall" -> Marker(0,8.0,48.0)
                    "row" -> { Marker(0,4.0,8.0); Marker(1,12.0,4.0) }
                    else -> error(content)
                }
            })
            Badge(modifier,container,foreground,slot)
        }
        @Composable fun Marker(index:Int,width:Double,height:Double) {
            val color=LocalContentColor.current
            Canvas(contents[index].modifier().size(width.toFloat().dp,height.toFloat().dp)
                .then(if(config.action=="interactive") Modifier.clickable(interactionSource=null,indication=null) { badgeClicks++ } else Modifier)) {
                painted[index]=color; drawRect(color)
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe)=probe.geometry().filter { it.name=="box" || it.name=="size" }
            val points=mutableListOf<ProbePoint>()
            if(config.host!="badge") {
                points+=ProbePoint("box","box",null,geometry(box)); points+=ProbePoint("anchor","anchor","box",geometry(anchor))
                if(config.host!="box") points+=ProbePoint("item","item",null,geometry(item))
            }
            points+=ProbePoint("badge","badge",if(config.host=="badge") null else "box",geometry(badge))
            repeat(contentCount) { index ->
                val argb=checkNotNull(painted[index]).toArgb()
                points+=ProbePoint("content-$index","content","badge",geometry(contents[index])+vector("paint","color",
                    listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","badge.content.painted-solid-srgba8","Canvas draw actual inherited LocalContentColor.toArgb()"))
            }
            if(config.action in listOf("input","interactive")) points+=ProbePoint("input","input",null,listOf(
                number("clicks",clicks.toDouble(),"count",definition="badge.anchor.click-count"),
                number("badgeClicks",badgeClicks.toDouble(),"count",definition="badge.content.click-count")))
            return points
        }
    }
}

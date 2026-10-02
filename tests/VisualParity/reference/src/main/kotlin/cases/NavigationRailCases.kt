@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.*
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class NavigationRailConfig(val labels:String="always", val selected:Boolean=false, val enabled:Boolean=true,
    val dark:Boolean=false, val header:Boolean=false, val rtl:Boolean=false, val insets:Boolean=false, val standalone:Boolean=false,
    val width:Double?=null, val height:Double?=null, val iconWidth:Double=24.0, val iconHeight:Double=24.0,
    val labelWidth:Double=32.0, val labelHeight:Double=16.0, val action:String="none", val customColors:Boolean=false, val standardMotion:Boolean=false,
    val container:String="default")

@ParitySuite("navigation-rail")
class NavigationRailCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<NavigationRailConfig>()
        ComposeHost(writer,240,320).use { host ->
            val scene=RailScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<NavigationRailConfig>()
        ComposeHost(writer,240,320).use { host ->
            val scene=RailScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            var nextFrame=16000L
            fun advance(time:Long) {
                while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }
                host.advanceTo(time)
            }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            // Same explicit event frame and continuing 16ms grid as Avalonia. This samples
            // applied selectable semantics, rather than stale pre-recomposition semantics.
            fun event(time:Long,action:String,position:Offset) { advance(time); host.input(action,position); host.draw(); host.frame(time) }
            fun mutate(time:Long,description:String,action:()->Unit) { advance(time); action(); writer.record(time,"mutation",description); host.draw(); host.frame(time); sample(description) }
            val center=Offset(56f,48f); val outside=Offset(220f,300f)
            sample("initial")
            when(config.action) {
                "select", "deselect", "reverse" -> {
                    mutate(1000,"selection-change") { scene.selected=!scene.selected }; times(16000,64000)
                    if(config.action=="reverse") mutate(65000,"selection-reverse") { scene.selected=false }
                    times(96000,160000,400000,800000)
                }
                "theme" -> {
                    mutate(1000,"dark-theme") { scene.dark=true }; times(16000,64000,160000,400000)
                    mutate(401000,"light-theme") { scene.dark=false }; times(416000,480000,800000)
                }
                "hover" -> {
                    event(1000,"move",center); sample("hover"); times(16000,80000)
                    event(81000,"move",outside); sample("exit"); times(96000,240000,400000)
                }
                "space", "enter" -> {
                    advance(1000); scene.requester.requestFocus(); host.draw(); sample("focus"); times(16000,80000)
                    val key=if(config.action=="space") Key.Spacebar else Key.Enter
                    advance(81000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); host.frame(81000); sample("key-down"); times(96000,160000)
                    advance(161000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); host.frame(161000); sample("key-up"); times(176000,400000,800000)
                }
                else -> {
                    event(1000,"move",center); event(2000,"down",center); sample("press")
                    if(config.action in listOf("quick","disabled")) {
                        event(5000,"up",center); sample("early-release"); times(16000,80000,240000,400000,800000)
                    } else {
                        times(16000,64000)
                        if(config.action=="disable") mutate(65000,"disabled-during-press") { scene.enabled=false }
                        if(config.action=="cancel") event(65000,"move",outside)
                        times(80000,160000,240000)
                        event(241000,"up",if(config.action=="cancel") outside else center); sample("release"); times(256000,400000,800000)
                    }
                }
            }
        }
    }

    private class RailScene(val config:NavigationRailConfig) {
        var selected by mutableStateOf(config.selected)
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var clicks=0
        val item=Probe(); val rail=Probe(); val icon=Probe(); val label=Probe(); val header=Probe()
        val painted=mutableMapOf<String,Color>()
        val requester=FocusRequester()

        @Composable
        fun Content() {
            // MaterialTheme's stock default is Standard. The product's MotionTokenSource
            // defaults to Expressive; explicitly pair the same scheme on both ends.
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),
                motionScheme=if(config.standardMotion) MotionScheme.standard() else MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                    LocalContentColor provides if(config.container!="default") Color(0xFF008080) else LocalContentColor.current) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(start=16.dp,top=16.dp)) {
                        if(config.standalone) Item() else NavigationRail(
                            modifier=rail.modifier().height(288.dp),
                            header=if(config.header) ({ if(config.container!="default") Marker(header,48.0,40.0,false,"header") else Box(Modifier.size(48.dp,40.dp).background(Color.Red)) }) else null,
                            containerColor=when(config.container) { "primary" -> MaterialTheme.colorScheme.primary; "ambient", "override" -> Color.Magenta; else -> NavigationRailDefaults.ContainerColor },
                            contentColor=if(config.container=="override") Color.Cyan else contentColorFor(when(config.container) { "primary" -> MaterialTheme.colorScheme.primary; "ambient" -> Color.Magenta; else -> NavigationRailDefaults.ContainerColor }),
                            windowInsets=if(config.insets) WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp) else NavigationRailDefaults.windowInsets
                        ) {
                            Item()
                            NavigationRailItem(false,{},icon={ Marker(null,24.0,24.0,true,null) })
                        }
                    }
                }
            }
        }

        @Composable
        fun Item() {
            var modifier=item.modifier().focusRequester(requester)
            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
            val colors=if(config.customColors) NavigationRailItemDefaults.colors(
                selectedIconColor=Color(0xFF126E60),selectedTextColor=Color(0xFF895210),indicatorColor=Color(0xFFB2E6D6),
                unselectedIconColor=Color(0xFF365A80),unselectedTextColor=Color(0xFF456789),
                disabledIconColor=Color(0x50365A80),disabledTextColor=Color(0x70456789)
            ) else NavigationRailItemDefaults.colors()
            NavigationRailItem(selected,{ clicks++; selected=true },
                icon={ Marker(icon,config.iconWidth,config.iconHeight,true,"icon") },
                modifier=modifier, enabled=enabled,
                label=if(config.labels=="none") null else ({ Marker(label,config.labelWidth,config.labelHeight,false,"label") }),
                alwaysShowLabel=config.labels=="always",colors=colors)
        }

        @Composable
        fun Marker(probe:Probe?,width:Double,height:Double,isIcon:Boolean,id:String?) {
            val color=LocalContentColor.current
            // A holder records the consumed content environment without triggering recomposition.
            // It remains readable while the label's graphics layer has alpha zero.
            if(id!=null) painted[id]=color
            Canvas((probe?.modifier() ?: Modifier).size(width.toFloat().dp,height.toFloat().dp)) {
                if(isIcon) {
                    drawRect(color,size=Size(size.width/3,size.height))
                    drawRect(color,topLeft=Offset(size.width/3,0f),size=Size(size.width*2/3,size.height/3))
                } else drawRect(color)
            }
        }

        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe)=probe.geometry().filter { it.name=="box" || it.name=="size" }
            val points=mutableListOf<ProbePoint>()
            if(!config.standalone) points+=ProbePoint("rail","container",null,geometry(rail))
            val layoutNode=checkNotNull(field(checkNotNull(item.coordinates),"layoutNode"))
            val semantics=checkNotNull(field(layoutNode,"_semanticsConfiguration")) as SemanticsConfiguration
            points+=ProbePoint("item","navigation-item",if(config.standalone) null else "rail",geometry(item)+listOf(
                flag("selected",semantics[SemanticsProperties.Selected],definition="navigation-item.selected",source="cached selectable SemanticsConfiguration.Selected"),
                flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="navigation-item.enabled",source="cached selectable SemanticsConfiguration.Disabled"),
                number("clicks",clicks.toDouble(),"count",definition="navigation-item.click-callback-count")))
            fun marker(id:String,probe:Probe,parent:String="item") {
                val argb=checkNotNull(painted[id]).toArgb()
                points+=ProbePoint(id,id,parent,geometry(probe)+vector("color","color",
                    listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","navigation-item.content-inherited-srgba8","Canvas consumed LocalContentColor.toArgb()"))
            }
            marker("icon",icon)
            if(config.container!="default") marker("header",header,"rail")
            if(config.labels=="always" || config.action=="none" && config.labels!="none" && selected) marker("label",label)
            return points
        }
    }
}

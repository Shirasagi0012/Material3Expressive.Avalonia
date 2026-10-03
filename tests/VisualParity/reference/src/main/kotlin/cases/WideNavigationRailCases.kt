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
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class WideNavigationRailConfig(val expanded:Boolean=false, val arrangement:String="top", val header:Boolean=false,
    val selected:Boolean=true, val enabled:Boolean=true, val label:Boolean=true, val standalone:Boolean=false, val dark:Boolean=false,
    val rtl:Boolean=false, val insets:Boolean=false, val width:Double?=null, val height:Double?=null,
    val iconWidth:Double=24.0, val iconHeight:Double=24.0, val labelWidth:Double=32.0, val labelHeight:Double=16.0,
    val headerWidth:Double=48.0, val headerHeight:Double=40.0, val empty:Boolean=false, val customPadding:Boolean=false,
    val customColors:Boolean=false, val iconPosition:String="auto", val action:String="none", val standardMotion:Boolean=false,
    val indicatorPadding:String="default", val secondLabelWidth:Double=48.0, val singleItem:Boolean=false)

@ParitySuite("wide-navigation-rail")
class WideNavigationRailCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<WideNavigationRailConfig>()
        ComposeHost(writer,520,360).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<WideNavigationRailConfig>()
        ComposeHost(writer,520,360).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            var nextFrame=16000L
            fun advance(time:Long) {
                while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time)
            }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,description:String,action:()->Unit) {
                advance(time); action(); writer.record(time,"mutation",description); host.draw(); host.frame(time); sample(description)
            }
            val itemBox=checkNotNull(scene.item.coordinates)
            val center=itemBox.localToRoot(Offset(itemBox.size.width/2f,itemBox.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(500f,340f)
            fun event(time:Long,action:String,position:Offset) { advance(time); host.input(action,position); host.draw(); host.frame(time) }
            sample("initial")
            when(config.action) {
                "expand","collapse","reverse","snap" -> {
                    mutate(1000,"expansion-change") { scene.scope.launch {
                        if(config.action=="snap") scene.state.snapTo(WideNavigationRailValue.Expanded) else scene.state.toggle()
                    } }
                    times(16000,64000,96000)
                    if(config.action=="reverse") mutate(97000,"expansion-reverse") { scene.scope.launch { scene.state.toggle() } }
                    times(160000,240000,400000,800000)
                }
                "select","deselect" -> {
                    mutate(1000,"selection-change") { scene.selected=!scene.selected }; times(16000,64000,160000,400000,800000)
                }
                "theme" -> {
                    mutate(1000,"dark-theme") { scene.dark=true }; times(16000,64000,160000,400000)
                }
                "hover" -> {
                    event(1000,"move",center); sample("hover"); times(16000,80000)
                    event(81000,"move",outside); sample("exit"); times(96000,240000,400000)
                }
                "hover-area" -> {
                    val label=checkNotNull(scene.label.coordinates)
                    val local=if(config.expanded) Offset(-4f*ComposeHost.RenderScale,label.size.height/2f) else Offset(label.size.width/2f,-2f*ComposeHost.RenderScale)
                    val gap=label.localToRoot(local)/ComposeHost.RenderScale.toFloat()
                    event(1000,"move",gap); sample("gap-hover"); times(16000,80000)
                    event(81000,"move",center); sample("icon-hover"); times(96000)
                    val edge=itemBox.localToRoot(Offset(ComposeHost.RenderScale.toFloat(),ComposeHost.RenderScale.toFloat()))/ComposeHost.RenderScale.toFloat()
                    event(97000,"move",edge); sample("edge-hover"); times(160000)
                    event(161000,"move",outside); sample("exit"); times(176000,400000)
                }
                "space","enter" -> {
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
                        if(config.action=="press-expand") mutate(65000,"expand-during-press") { scene.scope.launch { scene.state.expand() } }
                        if(config.action=="cancel") event(65000,"move",outside)
                        times(80000,160000,240000)
                        event(241000,"up",if(config.action=="cancel") outside else center); sample("release"); times(256000,400000,800000)
                    }
                }
            }
        }
    }

    private class Scene(val config:WideNavigationRailConfig) {
        var selected by mutableStateOf(config.selected)
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var clicks=0
        lateinit var state:WideNavigationRailState
        lateinit var scope:CoroutineScope
        val requester=FocusRequester()
        val item=Probe(); val rail=Probe(); val icon=Probe(); val label=Probe()
        val painted=mutableMapOf<String,Color>()

        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=if(config.standardMotion) MotionScheme.standard() else MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    state=rememberWideNavigationRailState(if(config.expanded) WideNavigationRailValue.Expanded else WideNavigationRailValue.Collapsed)
                    scope=rememberCoroutineScope()
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        if(config.standalone) Item() else {
                            var modifier=rail.modifier()
                            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                            WideNavigationRail(modifier=modifier,state=state,
                                header=if(config.header) ({ Marker(null,config.headerWidth,config.headerHeight,false,null) }) else null,
                                arrangement=arrange(config.arrangement),
                                windowInsets=if(config.insets) WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp) else WideNavigationRailDefaults.windowInsets,
                                contentPadding=if(config.customPadding) PaddingValues(start=7.dp,top=13.dp,end=11.dp,bottom=5.dp) else WideNavigationRailDefaults.ContentPadding) {
                                if(!config.empty) {
                                    Item()
                                    if(!config.singleItem) {
                                        WideNavigationRailItem(false,{},icon={ Marker(null,24.0,24.0,true,null) },
                                            label={ Marker(null,config.secondLabelWidth,16.0,false,null) },railExpanded=state.targetValue==WideNavigationRailValue.Expanded)
                                        WideNavigationRailItem(false,{},icon={ Marker(null,24.0,24.0,true,null) },label=null,
                                            railExpanded=state.targetValue==WideNavigationRailValue.Expanded)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        @Composable fun Item() {
            var modifier=item.modifier().focusRequester(requester)
            if(config.standalone && config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
            if(config.standalone && config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
            val expanded=if(config.standalone) config.expanded else state.targetValue==WideNavigationRailValue.Expanded
            val position=when(config.iconPosition) { "top"->NavigationItemIconPosition.Top; "start"->NavigationItemIconPosition.Start; else->WideNavigationRailItemDefaults.iconPositionFor(expanded) }
            val colors=if(config.customColors) WideNavigationRailItemDefaults.colors(
                selectedIconColor=Color(0xFF126E60),selectedTextColorTopIconPosition=Color(0xFF895210),
                selectedTextColorStartIconPosition=Color(0xFF246893),selectedIndicatorColor=Color(0xFFB2E6D6),
                unselectedIconColor=Color(0xFF365A80),unselectedTextColor=Color(0xFF456789),
                disabledIconColor=Color(0x50365A80),disabledTextColor=Color(0x70456789)
            ) else WideNavigationRailItemDefaults.colors()
            WideNavigationRailItem(selected,{ clicks++ },icon={ Marker(icon,config.iconWidth,config.iconHeight,true,"icon") },
                label=if(config.label) ({ Marker(label,config.labelWidth,config.labelHeight,false,"label") }) else null,
                railExpanded=expanded,iconPosition=position,modifier=modifier,enabled=enabled,colors=colors,
                indicatorPadding=when(config.indicatorPadding) {
                    "fixed"->PaddingValues(start=9.dp,top=3.dp,end=17.dp,bottom=7.dp)
                    "dynamic"->WideNavigationRailItemDefaults.indicatorPadding(expanded,
                        collapsedPadding=PaddingValues(start=9.dp,top=3.dp,end=17.dp,bottom=7.dp),
                        expandedPadding=PaddingValues(start=19.dp,top=13.dp,end=23.dp,bottom=17.dp))
                    else->WideNavigationRailItemDefaults.indicatorPadding(expanded)
                })
        }
        @Composable fun Marker(probe:Probe?,width:Double,height:Double,isIcon:Boolean,id:String?) {
            val color=LocalContentColor.current
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
            if(!config.standalone) points+=ProbePoint("rail","container",null,geometry(rail)+listOf(
                text("targetValue",state.targetValue.name,definition="wide-navigation-rail.state.target-value"),
                text("currentValue",state.currentValue.name,definition="wide-navigation-rail.state.current-value"),
                flag("isAnimating",state.isAnimating,definition="wide-navigation-rail.state.is-animating")))
            if(config.empty) return points
            val node=checkNotNull(field(checkNotNull(item.coordinates),"layoutNode"))
            val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
            points+=ProbePoint("item","navigation-item",if(config.standalone) null else "rail",geometry(item)+listOf(
                flag("selected",semantics[SemanticsProperties.Selected],definition="navigation-item.selected",source="cached selectable SemanticsConfiguration.Selected"),
                flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="navigation-item.enabled",source="cached selectable SemanticsConfiguration.Disabled"),
                number("clicks",clicks.toDouble(),"count",definition="navigation-item.click-callback-count")))
            fun marker(id:String,probe:Probe) {
                val argb=checkNotNull(painted[id]).toArgb()
                points+=ProbePoint(id,id,"item",geometry(probe)+vector("color","color",
                    listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","navigation-item.content-inherited-srgba8","Canvas consumed LocalContentColor.toArgb()"))
            }
            marker("icon",icon); if(config.label) marker("label",label)
            return points
        }
    }
    companion object {
        private fun arrange(value:String):Arrangement.Vertical=when(value) {
            "center"->Arrangement.Center; "bottom"->Arrangement.Bottom; "space-between"->Arrangement.SpaceBetween;
            "space-around"->Arrangement.SpaceAround; "space-evenly"->Arrangement.SpaceEvenly;
            "spaced-top"->Arrangement.spacedBy(8.dp); "spaced-center"->Arrangement.spacedBy(8.dp,Alignment.CenterVertically);
            "spaced-bottom"->Arrangement.spacedBy(8.dp,Alignment.Bottom); "overlap"->Arrangement.spacedBy((-12).dp);
            "spaced-zero-center"->Arrangement.spacedBy(0.dp,Alignment.CenterVertically);
            else->Arrangement.Top
        }
    }
}


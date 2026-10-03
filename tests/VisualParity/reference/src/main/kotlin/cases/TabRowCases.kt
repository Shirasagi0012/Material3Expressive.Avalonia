@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.ScrollState
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.*
import androidx.compose.ui.input.key.*
import androidx.compose.ui.layout.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class TabRowConfig(val variant:String="primary",val scrollable:Boolean=false,val content:String="label",val native:Boolean=false,
    val rtl:Boolean=false,val dark:Boolean=false,val enabled:Boolean=true,val customColors:Boolean=false,val customSlots:Boolean=false,
    val noSlots:Boolean=false,val count:Int=3,val selected:Int=0,val width:Double=352.0,val height:Double?=null,
    val edgePadding:Double=52.0,val minTabWidth:Double=90.0,val labelHeight:Double=16.0,val iconSize:Double=24.0,val action:String="none")

@ParitySuite("tab-row")
class TabRowCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<TabRowConfig>()
        ComposeHost(writer,384,180).use { host ->
            val scene=TabScene(config); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<TabRowConfig>()
        ComposeHost(writer,384,180).use { host ->
            val scene=TabScene(config); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,action:()->Unit) { advance(time); action(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            val target=checkNotNull(scene.tabs[minOf(1,config.count-1)].coordinates)
            val center=target.localToRoot(Offset(target.size.width/2f,target.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(380f,175f)
            fun input(time:Long,action:String,position:Offset) { advance(time); host.input(action,position); host.draw(); host.frame(time) }
            sample("initial")
            when(config.action) {
                "select", "reverse" -> {
                    mutate(1000,"select-last") { scene.selected=config.count-1 }; times(16000,64000)
                    if(config.action=="reverse") mutate(65000,"select-first") { scene.selected=0 }
                    times(96000,160000,400000,1000000)
                }
                "resize" -> {
                    mutate(1000,"narrower") { scene.width=241.25 }; times(16000,64000,160000)
                    mutate(161000,"restore") { scene.width=config.width }; times(176000,400000,1000000)
                }
                "initial-scroll" -> times(16000,64000,160000,400000,1000000)
                "theme" -> {
                    mutate(1000,"dark") { scene.dark=true }; times(16000,64000,400000)
                    mutate(401000,"light") { scene.dark=false }; times(416000,480000,1000000)
                }
                "colors" -> {
                    mutate(1000,"change-colors") { scene.active=Color(0xFF126E60); scene.inactive=Color(0xFF895210) }
                    times(16000,64000,400000,1000000)
                }
                "content" -> {
                    mutate(1000,"grow-label") { scene.firstLabelWidth=90.0 }; times(16000,64000,400000,1000000)
                }
                "space", "enter", "directional" -> {
                    advance(1000); scene.requester.requestFocus(); host.draw(); sample("focus"); times(16000,80000)
                    val key=when(config.action) { "space" -> Key.Spacebar; "directional" -> Key.DirectionRight; else -> Key.Enter }
                    advance(81000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); host.frame(81000); sample("key-down"); times(96000,160000)
                    advance(161000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); host.frame(161000); sample("key-up"); times(176000,400000,1000000)
                }
                "hover" -> {
                    input(1000,"move",center); sample("hover"); times(16000,80000)
                    input(81000,"move",outside); sample("exit"); times(96000,240000,400000)
                }
                else -> {
                    input(1000,"move",center); input(2000,"down",center); sample("press")
                    if(config.action in listOf("quick","disabled")) {
                        input(5000,"up",center); sample("early-release"); times(16000,80000,240000,400000,1000000)
                    } else {
                        times(16000,64000)
                        if(config.action=="disable") mutate(65000,"disabled") { scene.enabled=false }
                        if(config.action=="cancel") input(65000,"move",outside)
                        times(80000,160000,240000)
                        input(241000,"up",if(config.action=="cancel") outside else center); sample("release"); times(256000,400000,1000000)
                    }
                }
            }
            advance(1100000); host.draw(); sample("passive-late",false)
        }
    }
    private class TabScene(val config:TabRowConfig) {
        var selected by mutableStateOf(config.selected)
        var width by mutableStateOf(config.width)
        var dark by mutableStateOf(config.dark)
        var enabled by mutableStateOf(config.enabled)
        var firstLabelWidth by mutableStateOf(32.0)
        var active by mutableStateOf(Color(0xFF6750A4))
        var inactive by mutableStateOf(Color(0xFF49454F))
        var clicks=0
        val row=Probe(); val tabs=List(config.count) { Probe() }; val labels=List(config.count) { Probe() }; val icons=List(config.count) { Probe() }
        val indicator=Probe(); val divider=Probe(); val requester=FocusRequester()
        val painted=mutableMapOf<String,Color>()
        var scope:TabIndicatorScope?=null
        var scrollState:ScrollState?=null
        @Composable
        fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(start=16.dp,end=16.dp,top=16.dp)) { Row() }
                }
            }
        }
        @Composable
        fun Row() {
            var modifier=row.modifier().width(width.toFloat().dp)
            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
            val primary=config.variant=="primary"
            val indicatorContent:@Composable TabIndicatorScope.()->Unit = {
                scope=this
                if(!config.noSlots && config.count>0) {
                    val offset=Modifier.tabIndicatorOffset(selected,matchContentSize=primary).then(indicator.modifier())
                    if(config.customSlots) Box(offset.fillMaxWidth().height(5.dp).background(Color.Cyan))
                    else if(primary) TabRowDefaults.PrimaryIndicator(offset,width=Dp.Unspecified)
                    else TabRowDefaults.SecondaryIndicator(offset)
                }
            }
            val dividerContent:@Composable ()->Unit = {
                if(!config.noSlots) {
                    if(config.customSlots) Box(divider.modifier().fillMaxWidth().height(2.dp).background(Color.Magenta))
                    else HorizontalDivider(divider.modifier())
                }
            }
            val tabsContent:@Composable ()->Unit = { repeat(config.count) { TabContent(it) } }
            if(config.scrollable) {
                val state=rememberScrollState(); scrollState=state
                if(primary) PrimaryScrollableTabRow(selected,modifier,edgePadding=config.edgePadding.toFloat().dp,minTabWidth=config.minTabWidth.toFloat().dp,
                    scrollState=state,indicator=indicatorContent,divider=dividerContent,tabs=tabsContent)
                else SecondaryScrollableTabRow(selected,modifier,edgePadding=config.edgePadding.toFloat().dp,minTabWidth=config.minTabWidth.toFloat().dp,
                    scrollState=state,indicator=indicatorContent,divider=dividerContent,tabs=tabsContent)
            } else {
                if(primary) PrimaryTabRow(selected,modifier,indicator=indicatorContent,divider=dividerContent,tabs=tabsContent)
                else SecondaryTabRow(selected,modifier,indicator=indicatorContent,divider=dividerContent,tabs=tabsContent)
            }
        }
        @Composable
        fun TabContent(index:Int) {
            var modifier=tabs[index].modifier()
            if(index==minOf(1,config.count-1)) modifier=modifier.focusRequester(requester)
            val labelWidth=if(index==0) firstLabelWidth else 32.0+index*16
            val label:@Composable ()->Unit={ Marker(labels[index],labelWidth,config.labelHeight,false,"label-$index",true) }
            val icon:@Composable ()->Unit={ Marker(icons[index],config.iconSize,config.iconSize,true,"icon-$index",false) }
            val color=if(config.customColors) active else LocalContentColor.current
            val unselected=if(config.customColors) inactive else color
            val click:()->Unit={ clicks++; selected=index }
            val itemEnabled=if(index==minOf(1,config.count-1)) enabled else config.enabled
            when(config.content) {
                "custom" -> Tab(selected==index,click,modifier,itemEnabled,color,unselected) { label() }
                "leading" -> LeadingIconTab(selected==index,click,label,icon,modifier,itemEnabled,color,unselected)
                else -> Tab(selected==index,click,modifier,itemEnabled,if(config.content=="icon") null else label,
                    if(config.content in listOf("icon","stacked")) icon else null,color,unselected)
            }
        }
        @Composable
        fun Marker(probe:Probe,width:Double,height:Double,isIcon:Boolean,id:String,baseline:Boolean) {
            val color=LocalContentColor.current; painted[id]=color
            val modifier=probe.modifier().size(width.toFloat().dp,height.toFloat().dp)
            Canvas(if(!baseline) modifier else modifier.layout { measurable,constraints ->
                val child=measurable.measure(constraints)
                layout(child.width,child.height,mapOf(FirstBaseline to 12.dp.roundToPx(),LastBaseline to 12.dp.roundToPx())) { child.placeRelative(0,0) }
            }) {
                if(isIcon) { drawRect(color,size=Size(size.width/3,size.height)); drawRect(color,topLeft=Offset(size.width/3,0f),size=Size(size.width*2/3,size.height/3)) }
                else drawRect(color)
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe)=probe.geometry().filter { it.name=="box" || it.name=="size" }
            val points=mutableListOf(ProbePoint("row","container",null,geometry(row)))
            points+=ProbePoint("selection","state","row",listOf(number("index",selected.toDouble(),"index",definition="tab-row.selected-index"),
                number("scroll",(scrollState?.value ?: 0).toDouble()/ComposeHost.RenderScale,"dip",category="motion",definition="tab-row.scroll-start-offset")))
            @Suppress("UNCHECKED_CAST")
            val positions=(checkNotNull(field(checkNotNull(scope),"tabPositions")) as State<List<TabPosition>>).value
            tabs.forEachIndexed { index,probe ->
                val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
                val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
                points+=ProbePoint("tab-$index","tab","row",geometry(probe)+listOf(
                    flag("selected",semantics[SemanticsProperties.Selected],definition="tab.selected"),
                    flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="tab.enabled"),
                    number("left",positions[index].left.value.toDouble(),definition="tab-position.start"),
                    number("width",positions[index].width.value.toDouble(),definition="tab-position.width"),
                    number("contentWidth",positions[index].contentWidth.value.toDouble(),definition="tab-position.content-width")))
                fun marker(id:String,probe:Probe) {
                    val argb=checkNotNull(painted[id]).toArgb()
                    points+=ProbePoint(id,"content","tab-$index",geometry(probe)+vector("color","color",listOf((argb ushr 16 and 255).toDouble(),
                        (argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),"srgba8","none","properties","tab.content-srgba8","Canvas LocalContentColor"))
                }
                if(config.content!="icon") marker("label-$index",labels[index])
                if(config.content in listOf("icon","stacked","leading")) marker("icon-$index",icons[index])
            }
            if(!config.native) points+=ProbePoint("activation","state","row",listOf(number("clicks",clicks.toDouble(),"count",definition="tab.click-callback-count")))
            if(config.count>0 && !config.noSlots) points+=ProbePoint("indicator","indicator","row",geometry(indicator))
            if(!config.noSlots) points+=ProbePoint("divider","divider","row",geometry(divider))
            return points
        }
    }
}

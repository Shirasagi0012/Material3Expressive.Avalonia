@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.focusable
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.*
import androidx.compose.ui.input.key.*
import androidx.compose.ui.layout.findRootCoordinates
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.platform.WindowInfo
import androidx.compose.ui.unit.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class ModalWideNavigationRailConfig(val hide:Boolean=false,val expanded:Boolean=false,val header:Boolean=false,
    val arrangement:String="top",val rtl:Boolean=false,val dark:Boolean=false,val insets:Boolean=false,val empty:Boolean=false,
    val customColors:Boolean=false,val customPadding:Boolean=false,val headerTopPadding:Double=0.0,val labelWidth:Double=32.0,
    val width:Double?=null,val action:String="none")

@ParitySuite("modal-wide-navigation-rail")
class ModalWideNavigationRailCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<ModalWideNavigationRailConfig>()
        ComposeHost(writer,520,360,desktopContext()).use { host ->
            val scene=Scene(config,host)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<ModalWideNavigationRailConfig>()
        ComposeHost(writer,520,360,desktopContext()).use { host ->
            val scene=Scene(config,host)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,action:()->Unit) { advance(time); action(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            fun input(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw(); host.frame(time) }
            if(config.action.startsWith("escape")) {
                host.scene.sendKeyEvent(KeyEvent(Key.Tab,KeyEventType.KeyDown)); host.scene.sendKeyEvent(KeyEvent(Key.Tab,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw()
            }
            sample("initial")
            when(config.action) {
                "expand","collapse","reverse" -> {
                    mutate(1000,"expansion-change") { scene.scope.launch { scene.state.toggle() } }
                    times(16000,32000,64000,96000)
                    if(config.action=="reverse") mutate(97000,"expansion-reverse") { scene.scope.launch { scene.state.toggle() } }
                    times(160000,240000,400000,800000,1200000)
                }
                "theme" -> { mutate(1000,"dark-theme") { scene.dark=true }; times(16000,64000,160000,400000) }
                "escape","escape-no-back" -> {
                    mutate(1000,"key-down") { host.scene.sendKeyEvent(KeyEvent(Key.Escape,KeyEventType.KeyDown)); host.scheduler.runCurrent() }
                    times(16000,64000)
                    mutate(65000,"key-up") { host.scene.sendKeyEvent(KeyEvent(Key.Escape,KeyEventType.KeyUp)); host.scheduler.runCurrent() }
                    times(80000,160000,400000,800000)
                }
                "scrim" -> {
                    input(1000,"move",Offset(500f,340f)); input(2000,"down",Offset(500f,340f)); sample("scrim-down")
                    input(5000,"up",Offset(500f,340f)); sample("scrim-up"); times(16000,64000,160000,400000,800000,1200000)
                }
                else -> {
                    val direction=if(config.rtl) -1f else 1f
                    val origin=Offset(if(config.rtl) 410f else 110f,300f)
                    val distance=if(config.action=="drag-return") 60f else 160f
                    input(1000,"move",origin); input(2000,"down",origin); sample("drag-down")
                    input(18000,"move",origin+Offset(-40f*direction,0f)); sample("drag-start")
                    input(34000,"move",origin+Offset(-distance*direction,0f)); sample("drag-move")
                    if(config.action=="fling-return") input(50000,"move",origin+Offset(-120f*direction,0f))
                    val release=if(config.action.startsWith("fling")) 51000L else 90000L
                    val end=origin+Offset(-(if(config.action=="fling-return") 120f else distance)*direction,0f)
                    input(release,"up",end); sample("drag-up"); times(96000,160000,240000,400000,800000,1200000)
                }
            }
        }
    }
    private class Scene(val config:ModalWideNavigationRailConfig,val host:ComposeHost) {
        var dark by mutableStateOf(config.dark)
        lateinit var state:WideNavigationRailState
        lateinit var scope:CoroutineScope
        val page=Probe(); val item=Probe(); val icon=Probe(); val label=Probe()
        val painted=mutableMapOf<String,Color>()
        var clicks=0
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    state=rememberWideNavigationRailState(if(config.expanded) WideNavigationRailValue.Expanded else WideNavigationRailValue.Collapsed)
                    scope=rememberCoroutineScope()
                    Row(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        var modifier:Modifier=Modifier
                        if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                        ModalWideNavigationRail(modifier=modifier,state=state,hideOnCollapse=config.hide,
                            header=if(config.header) ({ Marker(null,48.0,40.0,false,null) }) else null,
                            expandedHeaderTopPadding=config.headerTopPadding.toFloat().dp,
                            arrangement=arrange(config.arrangement),
                            windowInsets=if(config.insets) WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp) else WideNavigationRailDefaults.windowInsets,
                            contentPadding=if(config.customPadding) PaddingValues(start=7.dp,top=13.dp,end=11.dp,bottom=5.dp) else WideNavigationRailDefaults.ContentPadding,
                            expandedProperties=ModalWideNavigationRailProperties(shouldDismissOnBackPress=config.action!="escape-no-back"),
                            colors=if(config.customColors) WideNavigationRailDefaults.colors(containerColor=Color(0xFFE0F0E8),contentColor=Color(0xFF126E60),
                                modalContainerColor=Color(0xFFD6E8F6),modalContentColor=Color(0xFF246893),modalScrimColor=Color(0x66365A80)) else WideNavigationRailDefaults.colors(),
                            collapsedShape=if(config.customColors) RoundedCornerShape(8.dp) else WideNavigationRailDefaults.modalCollapsedShape,
                            expandedShape=if(config.customColors) RoundedCornerShape(28.dp) else WideNavigationRailDefaults.modalExpandedShape) {
                            if(!config.empty) {
                                WideNavigationRailItem(true,{clicks++},icon={Marker(icon,24.0,24.0,true,"icon")},
                                    label={Marker(label,config.labelWidth,16.0,false,"label")},railExpanded=state.targetValue==WideNavigationRailValue.Expanded,modifier=item.modifier())
                                WideNavigationRailItem(false,{},icon={Marker(null,24.0,24.0,true,null)},label={Marker(null,48.0,16.0,false,null)},railExpanded=state.targetValue==WideNavigationRailValue.Expanded)
                                WideNavigationRailItem(false,{},icon={Marker(null,24.0,24.0,true,null)},label=null,railExpanded=state.targetValue==WideNavigationRailValue.Expanded)
                            }
                        }
                        Canvas(page.modifier().weight(1f).fillMaxHeight()) { drawRect(Color(0xFFF0F0F0)) }
                    }
                }
            }
        }
        @Composable fun Marker(probe:Probe?,width:Double,height:Double,isIcon:Boolean,id:String?) {
            val color=LocalContentColor.current
            if(id!=null) painted[id]=color
            val focus=if(height==40.0 && config.action.startsWith("escape")) Modifier.focusable() else Modifier
            Canvas((probe?.modifier() ?: Modifier).then(focus).size(width.toFloat().dp,height.toFloat().dp)) {
                if(isIcon) {
                    drawRect(color,size=Size(size.width/3,size.height))
                    drawRect(color,topLeft=Offset(size.width/3,0f),size=Size(size.width*2/3,size.height/3))
                } else drawRect(color)
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe):List<Measurement> {
                val c=checkNotNull(probe.coordinates)
                val root=c.findRootCoordinates()
                // NodeCoordinator.localToRoot uses RectManager's cached offsets.
                // During minimum-size coercion they can lag the drawn placement.
                // Walk the public coordinate chain instead, without cached rects.
                val origin=root.localPositionOf(c,Offset.Zero)/ComposeHost.RenderScale.toFloat()
                return listOf(vector("box","rect",listOf(origin.x.toDouble(),origin.y.toDouble(),c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),
                    "dip","scene","geometry","layout.box.aabb.unclipped","derived: LayoutCoordinates.size + root.localPositionOf / density"),
                    vector("size","point",listOf(c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),
                    "dip","local","geometry","layout.box.local-size","derived: LayoutCoordinates.size / density",listOf("width","height")))
            }
            val points=mutableListOf(ProbePoint("page","page",null,geometry(page)+listOf(
                text("targetValue",state.targetValue.name,definition="wide-navigation-rail.state.target-value"),
                text("currentValue",state.currentValue.name,definition="wide-navigation-rail.state.current-value"),
                flag("isAnimating",state.isAnimating,definition="wide-navigation-rail.state.is-animating"))))
            if(config.empty || item.coordinates?.isAttached!=true) return points
            points+=ProbePoint("item","navigation-item",null,geometry(item)+number("clicks",clicks.toDouble(),"count",definition="navigation-item.click-callback-count"))
            fun marker(id:String,probe:Probe) {
                val argb=checkNotNull(painted[id]).toArgb()
                points+=ProbePoint(id,id,"item",geometry(probe)+vector("color","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","navigation-item.content-inherited-srgba8","Canvas consumed LocalContentColor.toArgb()"))
            }
            marker("icon",icon); marker("label",label); return points
        }
    }
    companion object {
        private fun desktopContext():PlatformContext {
            // Supply the window environment normally provided by ComposeSceneMediator:
            // full client pixels, window focus, and density-scaled 18dp touch slop.
            // Dialog's scene layers use the same platform context as the root owner.
            val empty:PlatformContext=PlatformContext.Empty()
            return object:PlatformContext by empty {
                override val windowInfo=object:WindowInfo by empty.windowInfo {
                    override val containerSize=IntSize(520*ComposeHost.RenderScale,360*ComposeHost.RenderScale)
                    override val isWindowFocused=true
                }
                override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                    override val touchSlop=18f*ComposeHost.RenderScale
                }
            }
        }
        private fun arrange(value:String):Arrangement.Vertical=when(value) {
            "center"->Arrangement.Center; "bottom"->Arrangement.Bottom; "space-between"->Arrangement.SpaceBetween;
            "space-around"->Arrangement.SpaceAround; "space-evenly"->Arrangement.SpaceEvenly;
            "spaced-center"->Arrangement.spacedBy(8.dp,Alignment.CenterVertically); else->Arrangement.Top
        }
    }
}

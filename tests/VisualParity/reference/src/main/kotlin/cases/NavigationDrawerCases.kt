@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.platform.WindowInfo
import androidx.compose.ui.unit.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class NavigationDrawerConfig(val kind:String="modal",val open:Boolean=true,val statefulSheet:Boolean=true,
    val selected:Boolean=true,val icon:Boolean=true,val badge:Boolean=true,val rtl:Boolean=false,val dark:Boolean=false,
    val insets:Boolean=false,val empty:Boolean=false,val custom:Boolean=false,val width:Double?=null,
    val hostWidth:Int=520,val gestures:Boolean=true,val veto:Boolean=false,val action:String="none")

@ParitySuite("navigation-drawer")
class NavigationDrawerCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val c=writer.readConfig<NavigationDrawerConfig>()
        ComposeHost(writer,c.hostWidth,320,desktopContext(c.hostWidth)).use { host ->
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<NavigationDrawerConfig>()
        ComposeHost(writer,c.hostWidth,320,desktopContext(c.hostWidth)).use { host ->
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            if(c.action in listOf("escape","keyboard")) {
                host.scene.sendKeyEvent(KeyEvent(Key.Tab,KeyEventType.KeyDown)); host.scene.sendKeyEvent(KeyEvent(Key.Tab,KeyEventType.KeyUp)); host.scheduler.runCurrent()
            }
            host.draw()
            var nextFrame=16000L
            fun advance(t:Long) { while(nextFrame<=t) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(t) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun times(vararg ts:Long) { ts.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(t:Long,id:String,sampleNow:Boolean=true,action:()->Unit) { advance(t); action(); writer.record(t,"mutation",id); host.scheduler.runCurrent(); host.draw(); if(sampleNow) sample(id) }
            fun input(t:Long,action:String,p:Offset) { advance(t); host.input(action,p); host.draw() }
            sample("initial")
            when(c.action) {
                "open","close","reverse","reopen","veto-open","veto-close" -> {
                    mutate(1000,"state-change") { scene.scope.launch { if(scene.state.targetValue==DrawerValue.Closed) scene.state.open() else scene.state.close() } }
                    times(16000,32000,64000,96000)
                    if(c.action=="reverse") mutate(97000,"reverse") { scene.scope.launch { scene.state.close() } }
                    times(160000,256000,400000,800000,1200000)
                    if(c.action=="reopen") {
                        mutate(1201000,"close") { scene.scope.launch { scene.state.close() } }; times(1232000,1264000,1600000)
                        mutate(1601000,"reopen") { scene.scope.launch { scene.state.open() } }; times(1616000,1664000,2000000,2800000)
                    }
                }
                "snap" -> { mutate(1000,"snap-open") { scene.scope.launch { scene.state.snapTo(DrawerValue.Open) } }; times(16000)
                    mutate(17000,"snap-close") { scene.scope.launch { scene.state.snapTo(DrawerValue.Closed) } }; times(32000) }
                // Observe configuration-dependent paint/layout after the recomposition frame.
                "theme" -> { mutate(1000,"dark-theme",false) { scene.dark=true }; times(16000,64000) }
                "selection" -> { mutate(1000,"selection-change",false) { scene.selected=!scene.selected }; times(16000,64000) }
                "resize" -> { mutate(1000,"sheet-width",false) { scene.width=260.0 }; times(16000,64000) }
                "escape","keyboard" -> {
                    val key=if(c.action=="escape") Key.Escape else Key.Enter
                    mutate(1000,"key-down") { host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)) }; times(16000)
                    mutate(17000,"key-up") { host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)) }; times(32000,64000,160000,400000,800000)
                }
                "scrim" -> {
                    val p=Offset(if(c.rtl) 20f else 500f,280f)
                    input(1000,"move",p); input(2000,"down",p); sample("scrim-down")
                    input(5000,"up",p); sample("scrim-up"); times(16000,64000,160000,400000,800000)
                }
                "press","cancel","hover" -> {
                    val box=checkNotNull(scene.item.geometry().first { it.name=="box" }.numbers)
                    val p=Offset(box[0].toFloat()+80f,box[1].toFloat()+28f)
                    input(1000,"move",p); sample("hover")
                    if(c.action!="hover") {
                        input(2000,"down",p); sample("down"); times(16000,64000,160000)
                        if(c.action=="cancel") input(161000,"move",Offset(500f,310f))
                        input(162000,"up",if(c.action=="cancel") Offset(500f,310f) else p); sample("up")
                    } else input(2000,"move",Offset(500f,310f))
                    times(192000,320000,640000)
                }
                else -> {
                    val sign=if(c.rtl) -1f else 1f; val opening=c.action=="drag-open"
                    val origin=Offset(if(c.rtl) 410f else 110f,250f); val distance=if(c.action=="drag-return") 60f else 240f
                    input(1000,"move",origin); input(2000,"down",origin); sample("drag-down")
                    input(18000,"move",origin+Offset((if(opening) 1 else -1)*40*sign,0f)); sample("drag-start")
                    val end=origin+Offset((if(opening) 1 else -1)*distance*sign,0f)
                    input(34000,"move",end); sample("drag-move")
                    input(if(c.action=="drag-fast") 35000 else 90000,"up",end); sample("drag-up")
                    times(96000,160000,256000,400000,800000)
                }
            }
        }
    }
    private class Scene(val c:NavigationDrawerConfig) {
        lateinit var state:DrawerState; lateinit var scope:CoroutineScope
        var dark by mutableStateOf(c.dark); var selected by mutableStateOf(c.selected); var width by mutableStateOf(c.width)
        var clicks=0; var focused=false
        val page=Probe(); val sheet=Probe(); val item=Probe(); val icon=Probe(); val label=Probe(); val badge=Probe()
        val painted=mutableMapOf<String,Color>()
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    state=rememberDrawerState(if(c.open) DrawerValue.Open else DrawerValue.Closed) { !c.veto }
                    scope=rememberCoroutineScope()
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        val drawer:@Composable ()->Unit={ Sheet() }
                        val content:@Composable ()->Unit={ Canvas(page.modifier().fillMaxSize()) { drawRect(Color(0xFFF0F0F0)) } }
                        when(c.kind) {
                            "modal" -> ModalNavigationDrawer(drawer,drawerState=state,gesturesEnabled=c.gestures,
                                scrimColor=if(c.custom) Color(0x66365A80) else DrawerDefaults.scrimColor,content=content)
                            "dismissible" -> DismissibleNavigationDrawer(drawer,drawerState=state,gesturesEnabled=c.gestures,content=content)
                            else -> PermanentNavigationDrawer(drawer,content=content)
                        }
                    }
                }
            }
        }
        @Composable fun Sheet() {
            val modifier=sheet.modifier().then(if(width!=null) Modifier.width(width!!.toFloat().dp) else Modifier)
            val insets=if(c.insets) WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp) else DrawerDefaults.windowInsets
            val shape=if(c.custom) RoundedCornerShape(12.dp) else if(c.kind=="modal") DrawerDefaults.shape else androidx.compose.ui.graphics.RectangleShape
            val background=if(c.custom) Color(0xFFD6E8F6) else if(c.kind=="modal") DrawerDefaults.modalContainerColor else DrawerDefaults.standardContainerColor
            val foreground=if(c.custom) Color(0xFF246893) else contentColorFor(background)
            val elevation=if(c.custom) 6.dp else 0.dp
            val content:@Composable ColumnScope.()->Unit={
                if(!c.empty) Column(Modifier.padding(horizontal=12.dp,vertical=16.dp)) {
                    Marker(null,48.0,32.0,null)
                    NavigationDrawerItem(label={Marker(label,48.0,16.0,"label")},selected=selected,onClick={clicks++},modifier=item.modifier().onFocusChanged { focused=it.isFocused },
                        icon=if(c.icon) ({Marker(icon,24.0,24.0,"icon")}) else null,
                        badge=if(c.badge) ({Marker(badge,20.0,12.0,"badge")}) else null,
                        colors=if(c.custom) NavigationDrawerItemDefaults.colors(selectedContainerColor=Color(0xFFE0F0E8),
                            selectedIconColor=Color(0xFF126E60),selectedTextColor=Color(0xFF345678),selectedBadgeColor=Color(0xFF864321)) else NavigationDrawerItemDefaults.colors())
                }
            }
            when(c.kind) {
                "modal" -> if(c.statefulSheet) ModalDrawerSheet(state,modifier,shape,background,foreground,elevation,insets,content)
                    else ModalDrawerSheet(modifier,shape,background,foreground,elevation,insets,content)
                "dismissible" -> if(c.statefulSheet) DismissibleDrawerSheet(state,modifier,shape,background,foreground,elevation,insets,content)
                    else DismissibleDrawerSheet(modifier,shape,background,foreground,elevation,insets,content)
                else -> PermanentDrawerSheet(modifier,shape,background,foreground,elevation,insets,content)
            }
        }
        @Composable fun Marker(probe:Probe?,width:Double,height:Double,id:String?) {
            val color=LocalContentColor.current; if(id!=null) painted[id]=color
            Canvas((probe?.modifier() ?: Modifier).size(width.toFloat().dp,height.toFloat().dp)) { drawRect(color) }
        }
        fun measure():List<ProbePoint> {
            fun geometry(p:Probe)=p.geometry().filter { it.name in listOf("box","size") }
            val facts=geometry(page).toMutableList()
            if(c.kind!="permanent") facts+=listOf(text("currentValue",state.currentValue.name,definition="drawer.state.settled-value"),
                text("targetValue",state.targetValue.name,definition="drawer.state.target-value"),flag("isAnimationRunning",state.isAnimationRunning,definition="drawer.state.is-animation-running"),
                number("offset",state.currentOffset.toDouble()/ComposeHost.RenderScale,"dip","local","motion","drawer.state.offset","DrawerState.currentOffset / density"))
            val points=mutableListOf(ProbePoint("page","page",null,facts))
            val visible=c.kind=="permanent" || sheet.coordinates?.let { state.currentOffset > -it.size.width.toFloat() } == true
            if(visible) {
                points+=ProbePoint("sheet","sheet",null,geometry(sheet))
                if(!c.empty) {
                    points+=ProbePoint("item","navigation-item","sheet",geometry(item)+listOf(flag("selected",selected,definition="drawer-item.selected"),flag("focused",focused,definition="drawer-item.focused"),number("clicks",clicks.toDouble(),"count",definition="drawer-item.clicks")))
                    fun marker(id:String,p:Probe) {
                        val argb=checkNotNull(painted[id]).toArgb()
                        points+=ProbePoint(id,id,"item",geometry(p)+vector("color","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                            "srgba8","none","properties","drawer-item.inherited-color","Canvas consumed LocalContentColor.toArgb()"))
                    }
                    marker("label",label); if(c.icon) marker("icon",icon); if(c.badge) marker("badge",badge)
                }
            }
            return points
        }
    }
    companion object {
        private fun desktopContext(width:Int):PlatformContext {
            val empty=PlatformContext.Empty()
            return object:PlatformContext by empty {
                override val windowInfo=object:WindowInfo by empty.windowInfo {
                    override val containerSize=IntSize(width*ComposeHost.RenderScale,320*ComposeHost.RenderScale)
                    override val isWindowFocused=true
                }
                override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                    override val touchSlop=18f*ComposeHost.RenderScale
                }
            }
        }
    }
}

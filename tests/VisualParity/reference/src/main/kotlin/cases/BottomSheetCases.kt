@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.AnchoredDraggableDefaults
import androidx.compose.foundation.gestures.FlingBehavior
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.input.key.*
import androidx.compose.ui.navigationevent.BackNavigationEventInput
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class BottomSheetConfig(val initial:String="Expanded",val contentHeight:Double=320.0,val handle:Boolean=true,
    val partial:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,val insets:Boolean=false,val gestures:Boolean=true,
    val veto:Boolean=false,val maxWidth:Double=640.0,val action:String="none",val scaffold:Boolean=false,val peek:Double=56.0,
    val modal:Boolean=false,val outside:Boolean=true,val scrim:Boolean=true)

@ParitySuite("bottom-sheet")
class BottomSheetCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = withEnglishLocale {
        val c=writer.readConfig<BottomSheetConfig>()
        ComposeHost(writer,720,480,desktopContext()).use { host ->
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) = withEnglishLocale {
        val c=writer.readConfig<BottomSheetConfig>()
        val context=desktopContext()
        ComposeHost(writer,720,480,context).use { host ->
            val navigationInput=BackNavigationEventInput()
            val dispatcher=context.architectureComponentsOwner.navigationEventDispatcherOwner.navigationEventDispatcher
            dispatcher.addInput(navigationInput)
            try {
            val scene=Scene(c); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(t:Long) { while(nextFrame<=t) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(t) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            fun times(vararg ts:Long) { ts.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(t:Long,id:String,sampleNow:Boolean=true,action:()->Unit) { advance(t); action(); writer.record(t,"mutation",id); host.scheduler.runCurrent(); host.draw(); if(sampleNow) sample(id) }
            fun input(t:Long,action:String,p:Offset) { advance(t); host.input(action,p); host.draw() }
            sample("initial")
            if(c.action=="auto-show") times(16000,64000,96000,192000,416000,800000)
            else if(c.action.startsWith("outside")) {
                input(1000,"move",Offset(10f,10f)); input(2000,"down",Offset(10f,10f))
                input(18000,"up",Offset(10f,10f)); times(32000,96000,192000,416000,800000)
            } else if(c.action=="zero-peek-hidden") {
                mutate(1000,"settle-hidden",false) { scene.scope.launch { scene.state.anchoredDrag(scene.fling,0f) } }; times(16000)
                mutate(17000,"restore-peek",false) { scene.peek=56.0 }; times(32000)
                mutate(33000,"show") { scene.scope.launch { scene.state.show() } }; times(48000,96000,192000,416000,800000)
            } else if(c.action.startsWith("drag") || c.action=="disable-during-drag") {
                val origin=Offset(300f,if(c.initial=="Expanded") 200f else 320f)
                input(1000,"move",origin); input(2000,"down",origin); sample("drag-down")
                val sign=if(c.action=="drag-up") -1 else 1
                input(18000,"move",origin+Offset(0f,32f*sign)); sample("drag-start")
                val end=origin+Offset(0f,(if(c.action=="drag-return") 40f else 150f)*sign)
                input(34000,"move",end); sample("drag-move")
                if(c.action=="disable-during-drag") mutate(35000,"disable",false) { scene.gestures=false }
                input(if(c.action.endsWith("fast")) 35000 else 100000,"up",end); sample("drag-up")
                times(112000,160000,256000,416000,800000)
            } else if(c.action.startsWith("escape")) {
                fun key(type:KeyEventType) { val event=KeyEvent(Key.Escape,type); if(!host.scene.sendKeyEvent(event)) navigationInput.onKeyEvent(event); host.scheduler.runCurrent() }
                mutate(1000,"escape-down") { key(KeyEventType.KeyDown) }
                mutate(2000,"escape-up") { key(KeyEventType.KeyUp) }
                times(16000,64000,192000,416000,800000)
            } else if(c.action.startsWith("handle")) {
                val handlePoint=Offset(360f,if(c.scaffold) 480f-c.peek.toFloat()+24f else 264f)
                input(1000,"move",handlePoint); input(2000,"down",handlePoint)
                input(18000,"up",handlePoint); times(32000,96000,192000,416000,800000)
            } else {
                mutate(1000,"state-change",c.action!="grow") {
                    if(c.action=="grow") scene.height=320.0
                    else scene.scope.launch { when(c.action) {
                        "hide","veto" -> scene.state.hide()
                        "partial","peek-change" -> scene.state.partialExpand()
                        "expand" -> scene.state.expand()
                        else -> scene.state.show()
                    } }
                }
                times(16000,64000,96000)
                if(c.action=="reverse") mutate(97000,"reverse") { scene.scope.launch { scene.state.hide() } }
                if(c.action=="resize") mutate(97000,"resize",false) { scene.height=80.0 }
                if(c.action=="peek-change") mutate(97000,"peek-change",false) { scene.peek=100.0 }
                times(192000,320000,416000,800000)
            }
            } finally { dispatcher.removeInput(navigationInput) }
        }
    }
    private class Scene(val c:BottomSheetConfig) {
        lateinit var state:SheetState; lateinit var scope:CoroutineScope; lateinit var fling:FlingBehavior
        var height by mutableStateOf(c.contentHeight); var gestures by mutableStateOf(c.gestures); var dismissals=0
        val sheet=Probe(); val content=Probe(); val body=Probe(); val top=Probe(); val snackbar=Probe()
        var peek by mutableStateOf(c.peek)
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(c.dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    val density=LocalDensity.current
                    state=remember { SheetState(enabledValues=if(c.scaffold) setOf(SheetValue.PartiallyExpanded,SheetValue.Expanded) else if(c.partial) SheetValue.entries.toSet() else setOf(SheetValue.Hidden,SheetValue.Expanded),
                        positionalThreshold={ with(density) { 56.dp.toPx() } },velocityThreshold={ with(density) { 125.dp.toPx() } },
                        initialValue=SheetValue.valueOf(c.initial),confirmValueChange={ !c.veto || it!=SheetValue.Hidden }) }
                    scope=rememberCoroutineScope()
                    fling=AnchoredDraggableDefaults.flingBehavior(state.anchoredDraggableState,
                        positionalThreshold={ with(density) { 56.dp.toPx() } },animationSpec=MaterialTheme.motionScheme.defaultSpatialSpec<Float>())
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.TopCenter) {
                        val sheetContent: @Composable ColumnScope.()->Unit = {
                            Box(content.modifier().fillMaxWidth().height(height.toFloat().dp).background(Color(0xFF336699))) {
                                Box(Modifier.align(Alignment.TopStart).size(24.dp).background(Color(0xFFFFD166)))
                            }
                        }
                        val handle: @Composable (()->Unit)? = if(c.handle) { { BottomSheetDefaults.DragHandle() } } else null
                        if(c.modal) {
                            ModalBottomSheet(onDismissRequest={dismissals++},modifier=sheet.modifier(),sheetState=state,
                                sheetMaxWidth=c.maxWidth.toFloat().dp,sheetGesturesEnabled=gestures,dragHandle=handle,
                                properties=ModalBottomSheetProperties(shouldDismissOnBackPress=c.action!="escape-disabled",shouldDismissOnClickOutside=c.outside),
                                scrimColor=if(c.scrim)BottomSheetDefaults.ScrimColor else Color.Unspecified,
                                contentWindowInsets={if(c.insets)WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp)else WindowInsets(0)},content=sheetContent)
                        } else if(c.scaffold) {
                            BottomSheetScaffold(scaffoldState=rememberBottomSheetScaffoldState(bottomSheetState=state),
                                sheetContent=sheetContent,sheetPeekHeight=peek.toFloat().dp,sheetMaxWidth=c.maxWidth.toFloat().dp,
                                sheetSwipeEnabled=gestures,sheetDragHandle=handle,
                                topBar={ Box(top.modifier().fillMaxWidth().height(56.dp).background(Color(0xFFE8DEF8))) },
                                snackbarHost={ Box(snackbar.modifier().size(120.dp,32.dp).background(Color(0xFF993344))) }) {
                                Box(body.modifier().fillMaxSize().background(Color(0xFFF3EDF7)))
                            }
                        } else BottomSheet(modifier=sheet.modifier(),state=state,onDismissRequest={dismissals++},maxWidth=c.maxWidth.toFloat().dp,
                            gesturesEnabled=gestures,backHandlerEnabled=c.action!="escape-disabled",dragHandle=handle,
                            contentWindowInsets={ if(c.insets) WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp) else WindowInsets(0) },content=sheetContent)
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe)=probe.geometry().filter { it.name in listOf("box","size") }
            val points=mutableListOf(ProbePoint("state","state",null,listOf(
                text("currentValue",state.currentValue.name,definition="sheet.current-value"),
                text("targetValue",state.targetValue.name,definition="sheet.target-value"),
                flag("visible",state.isVisible,definition="sheet.visible"),
                flag("animating",state.isAnimationRunning,definition="sheet.animating"),
                flag("hasPartial",state.hasPartiallyExpandedState,definition="sheet.has-partial"),
                number("offset",state.requireOffset().toDouble()/ComposeHost.RenderScale,"dip","local","motion","sheet.offset","SheetState.requireOffset / density"),
                number("dismissals",dismissals.toDouble(),"count",definition="sheet.dismissals"))),
                // The supplied modifier is outside draggableAnchors. Read the Surface's inner
                // coordinator to include its actual placement and graphics layers, as on Avalonia.
                ProbePoint("sheet","sheet",null,sheet.geometry(generateSequence((if(c.scaffold) checkNotNull(content.coordinates).parentLayoutCoordinates!!.parentLayoutCoordinates!! else checkNotNull(sheet.coordinates)) as Any) { field(it,"wrapped") }.last() as LayoutCoordinates)
                    .filter { it.name in listOf("box","size") }),ProbePoint("content","content","sheet",geometry(content)))
            if(c.scaffold) points+=listOf(ProbePoint("body","body",null,geometry(body)),ProbePoint("top","top",null,geometry(top)),ProbePoint("snackbar","snackbar",null,geometry(snackbar)),
                ProbePoint("padding","padding",null,listOf(number("bottom",peek,"dip",definition="sheet.body-bottom-padding"))))
            return points
        }
    }
    companion object {
        private suspend fun withEnglishLocale(block:suspend ()->Unit) {
            val previous=java.util.Locale.getDefault()
            try { java.util.Locale.setDefault(java.util.Locale.US); block() }
            finally { java.util.Locale.setDefault(previous) }
        }
        private fun desktopContext():PlatformContext {
            val empty=PlatformContext.Empty()
            return object:PlatformContext by empty {
                override val windowInfo=object:WindowInfo by empty.windowInfo {
                    override val containerSize=IntSize(720*ComposeHost.RenderScale,480*ComposeHost.RenderScale)
                    override val isWindowFocused=true
                }
                override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration { override val touchSlop=18f*ComposeHost.RenderScale }
            }
        }
    }
}

@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class,androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.ScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class BottomSheetScrollConfig(val kind:String="plain",val action:String="expand",val rtl:Boolean=false)

@ParitySuite("bottom-sheet-scroll")
class BottomSheetScrollCases {
    @ParityScenario("touch") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<BottomSheetScrollConfig>()
        val state=SheetState(enabledValues=SheetValue.entries.toSet(),
            initialValue=if(c.action=="fling-down")SheetValue.Expanded else SheetValue.PartiallyExpanded,
            positionalThreshold={56f*ComposeHost.RenderScale},velocityThreshold={125f*ComposeHost.RenderScale},confirmValueChange={true})
        val scroll=ScrollState(if(c.action=="fling-down")80*ComposeHost.RenderScale else 0)
        var present by mutableStateOf(true)
        val context=object:PlatformContext by PlatformContext.Empty() {
            override val windowInfo=object:WindowInfo by PlatformContext.Empty().windowInfo {
                override val containerSize=IntSize(240*ComposeHost.RenderScale,480*ComposeHost.RenderScale)
                override val isWindowFocused=true
            }
            override val viewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
        ComposeHost(writer,240,480,context).use { host ->
            val enabled=c.action!="disabled"
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White)) {
                            val content:@Composable ()->Unit={
                                Box(Modifier.fillMaxWidth().then(if(c.action=="auto-height")Modifier else Modifier.height(360.dp)).verticalScroll(scroll)) {
                                    Box(Modifier.fillMaxWidth().height(1200.dp).background(Color(0xffddeeff))) {
                                        Box(Modifier.padding(start=24.dp,end=24.dp,top=16.dp).fillMaxWidth().height(16.dp).background(Color(0xff305080)))
                                        Box(Modifier.padding(start=24.dp,end=24.dp,top=200.dp).fillMaxWidth().height(16.dp).background(Color(0xff804030)))
                                    }
                                }
                            }
                            if(present) when(c.kind) {
                                "modal"->ModalBottomSheet(onDismissRequest={},sheetState=state,sheetGesturesEnabled=enabled,contentWindowInsets={WindowInsets(0)}) {content()}
                                "scaffold"->BottomSheetScaffold(sheetContent={content()},scaffoldState=rememberBottomSheetScaffoldState(bottomSheetState=state),
                                    sheetPeekHeight=240.dp,sheetSwipeEnabled=enabled) { Box(Modifier.fillMaxSize().background(Color.White)) }
                                else->BottomSheet(state=state,onDismissRequest={},gesturesEnabled=enabled,contentWindowInsets={WindowInsets(0)}) {content()}
                            }
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) { writer.submit(host.snapshot(id,id,listOf(ProbePoint("sheet","bottom-sheet-scroll",null,listOf(
                number("offset",state.requireOffset()/ComposeHost.RenderScale.toDouble(),"dip"),
                number("body-offset",scroll.value/ComposeHost.RenderScale.toDouble(),"dip"),
                text("current",state.currentValue.toString()),text("target",state.targetValue.toString())))),true)) }
            sample("initial")
            val release=if(c.action.startsWith("fling"))64000L else if(c.action=="reverse")240000L else 160000L
            val d=ComposeHost.RenderScale.toFloat()
            for(time in 16000L..1600000L step 16000L) {
                host.advanceTo(time)
                val y:Float?=when {
                    time==16000L->if(c.action=="fling-down")200f else 350f
                    time==32000L->if(c.action=="fling-down")270f else if(c.action=="settle")310f else 280f
                    time==48000L && c.action!="settle"->if(c.action=="fling-down")340f else if(c.action=="fling-up")160f else 100f
                    time==80000L && c.action=="reverse"->200f
                    time==128000L && c.action=="reverse"->260f
                    time==release->if(c.action=="fling-down")340f else if(c.action=="fling-up")160f else if(c.action=="reverse")260f else if(c.action=="settle")310f else 100f
                    else->null
                }
                if(y!=null) {
                    val type=if(time==16000L)PointerEventType.Press else if(time==release)PointerEventType.Release else PointerEventType.Move
                    host.scene.sendPointerEvent(type,Offset(60*d,y*d),timeMillis=1000+time/1000,type=PointerType.Touch)
                    host.scheduler.runCurrent();writer.record(time,"input","touch $type (60,$y)")
                }
                if(time==208000L && c.action=="detach"){present=false;writer.record(time,"mutation","detach sheet")}
                host.frame(time)
                if(time in listOf(32000L,48000L,64000L,80000L,128000L,160000L,176000L,208000L,240000L,320000L,480000L,800000L,1600000L))sample("time-$time")
            }
        }
    }
}

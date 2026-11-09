@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class,androidx.compose.ui.InternalComposeUiApi::class)
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
import androidx.compose.ui.input.nestedscroll.nestedScroll
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class FloatingToolbarScrollConfig(val exit:String="bottom",val action:String="snap-away",val rtl:Boolean=false)

@ParitySuite("floating-toolbar-scroll")
class FloatingToolbarScrollCases {
    @ParityScenario("touch") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<FloatingToolbarScrollConfig>()
        val d=ComposeHost.RenderScale.toFloat()
        val state=FloatingToolbarState(initialOffsetLimit=-Float.MAX_VALUE,
            initialOffset=if(c.action=="fling-down")-60*d else 0f,initialContentOffset=0f)
        val scroll=ScrollState((if(c.action=="fling-up")840 else if(c.action=="fling-down")40 else 0)*ComposeHost.RenderScale)
        var connected by mutableStateOf(true)
        var present by mutableStateOf(true)
        val context=object:PlatformContext by PlatformContext.Empty() {
            override val viewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration {
                override val touchSlop=18f*d
            }
        }
        ComposeHost(writer,280,320,context).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        val direction=when(c.exit){"start"->FloatingToolbarExitDirection.Start;"end"->FloatingToolbarExitDirection.End;"top"->FloatingToolbarExitDirection.Top;else->FloatingToolbarExitDirection.Bottom}
                        val behavior=FloatingToolbarDefaults.exitAlwaysScrollBehavior(direction,state)
                        Box(Modifier.fillMaxSize().then(if(connected&&present)Modifier.nestedScroll(behavior) else Modifier)) {
                            Box(Modifier.fillMaxSize().verticalScroll(scroll)) {
                                Box(Modifier.fillMaxWidth().height(1200.dp).background(Color(0xffddeeff))) {
                                    Box(Modifier.padding(start=24.dp,end=24.dp,top=60.dp).fillMaxWidth().height(16.dp).background(Color(0xff305080)))
                                }
                            }
                            if(present) {
                                val alignment=when(c.exit){"start"->Alignment.BottomStart;"top"->Alignment.TopEnd;else->Alignment.BottomEnd}
                                val modifier=Modifier.align(alignment).padding(16.dp)
                                if(c.exit=="start"||c.exit=="end") VerticalFloatingToolbar(expanded=true,modifier=modifier,scrollBehavior=behavior) {
                                    Box(Modifier.size(48.dp).background(Color(0xff49454f)))
                                } else HorizontalFloatingToolbar(expanded=true,modifier=modifier,scrollBehavior=behavior) {
                                    Box(Modifier.size(48.dp).background(Color(0xff49454f)))
                                }
                            }
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) {writer.submit(host.snapshot(id,id,listOf(ProbePoint("toolbar","floating-toolbar-scroll",null,listOf(
                number("offset",state.offset/d.toDouble(),"dip"),number("limit",state.offsetLimit/d.toDouble(),"dip"),
                number("content-offset",state.contentOffset/d.toDouble(),"dip"),number("body-offset",scroll.value/d.toDouble(),"dip")))),true))}
            sample("initial")
            for(time in 16000L..1200000L step 16000L) {
                host.advanceTo(time)
                val down=c.action=="fling-down"
                val y:Float?=when {
                    time==16000L->150f
                    time==32000L->if(down)220f else if(c.action=="snap-back")110f else 70f
                    time==48000L&&c.action.startsWith("fling")->if(down)270f else 20f
                    time==64000L->if(down)270f else if(c.action.startsWith("fling"))20f else if(c.action=="snap-back")110f else 70f
                    time==112000L&&c.action=="interrupt"->100f
                    time==128000L&&c.action=="interrupt"->140f
                    time==176000L&&c.action=="interrupt"->140f
                    time==48000L&&c.action=="reverse"->110f
                    else->null
                }
                if(y!=null) {
                    val cancel=(time==64000L&&!c.action.startsWith("fling"))||time==176000L
                    val type=if(time==16000L||time==112000L)PointerEventType.Press else if(time==64000L||time==176000L)PointerEventType.Release else PointerEventType.Move
                    if(cancel)host.scene.cancelPointerInput() else host.scene.sendPointerEvent(type,Offset(140*d,y*d),timeMillis=1000+time/1000,type=PointerType.Touch)
                    host.scheduler.runCurrent();writer.record(time,"input",if(cancel)"touch cancel" else "touch $type (140,$y)")
                }
                if(time==112000L&&c.action=="disconnect"){connected=false;writer.record(time,"mutation","disconnect source")}
                if(time==112000L&&c.action=="detach"){present=false;writer.record(time,"mutation","detach toolbar")}
                host.frame(time)
                if(time in listOf(32000L,48000L,64000L,80000L,112000L,128000L,176000L,240000L,400000L,800000L,1200000L))sample("time-$time")
            }
        }
    }
}

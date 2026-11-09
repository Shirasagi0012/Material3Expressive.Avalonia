@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class,androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.material3.pulltorefresh.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class PullRefreshBoxConfig(val action:String="release",val expressive:Boolean=false,val rtl:Boolean=false)

@ParitySuite("pull-refresh-box")
class PullToRefreshBoxCases {
    @ParityScenario("touch") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<PullRefreshBoxConfig>()
        val state=PullToRefreshState()
        var refreshing by mutableStateOf(false)
        var present by mutableStateOf(true)
        var requests=0
        val desktopContext=object:PlatformContext by PlatformContext.Empty() {
            override val viewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
        val scroll=androidx.compose.foundation.ScrollState(if(c.action=="body")240*ComposeHost.RenderScale else 0)
        ComposeHost(writer,240,320,desktopContext).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White)) {
                            if(present) PullToRefreshBox(refreshing,{requests++;if(c.action=="refresh")refreshing=true},
                                Modifier.fillMaxSize(),state=state,enabled=c.action!="disabled",
                                indicator={
                                    if(c.expressive)PullToRefreshDefaults.LoadingIndicator(state,refreshing,Modifier.align(Alignment.TopCenter))
                                    else PullToRefreshDefaults.Indicator(state,refreshing,Modifier.align(Alignment.TopCenter))
                                }) {
                                Box(Modifier.fillMaxSize().verticalScroll(scroll)) {
                                    Box(Modifier.fillMaxWidth().height(1200.dp).background(Color(0xffddeeff)))
                                }
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String) {writer.submit(host.snapshot(id,id,listOf(ProbePoint("pull","pull-refresh-box",null,listOf(
                number("fraction",state.distanceFraction.toDouble(),"fraction"),
                number("body-offset",scroll.value/ComposeHost.RenderScale.toDouble(),"dip"),
                number("requests",requests.toDouble(),"count"),flag("refreshing",refreshing),flag("animating",state.isAnimating)))),true))}
            sample("initial")
            val d=ComposeHost.RenderScale.toFloat()
            for(time in 16000L..1408000L step 16000L) {
                host.advanceTo(time)
                val y=when(time){16000L->30f;32000L->100f;48000L->170f;64000L->230f
                    96000L->if(c.action=="reverse")180f else -1f
                    128000L->if(c.action=="reverse")70f else -1f
                    160000L->if(c.action=="reverse")70f else 230f
                    else->-1f}
                if(y>=0) {
                    val type=if(time==16000L)PointerEventType.Press else if(time==160000L)PointerEventType.Release else PointerEventType.Move
                    if(time==160000L && c.action=="cancel") host.scene.cancelPointerInput()
                    else host.scene.sendPointerEvent(type,Offset(60*d,y*d),timeMillis=1000+time/1000,type=PointerType.Touch)
                    host.scheduler.runCurrent();writer.record(time,"input",if(time==160000L && c.action=="cancel")"touch cancel" else "touch $type (60,$y)")
                }
                if(time==208000L && c.action=="detach"){present=false;writer.record(time,"mutation","detach box")}
                if(time==800000L && c.action=="refresh"){refreshing=false;writer.record(time,"mutation","complete refresh")}
                host.frame(time)
                if(time in listOf(32000L,48000L,64000L,96000L,128000L,160000L,176000L,208000L,240000L,400000L,640000L,800000L,960000L,1408000L))sample("time-$time")
            }
        }
    }
}

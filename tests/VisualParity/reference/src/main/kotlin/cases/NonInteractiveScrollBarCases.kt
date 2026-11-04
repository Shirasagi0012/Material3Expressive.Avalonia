@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.ScrollIndicatorState
import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.Orientation
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable
import kotlin.math.roundToInt

@Serializable
data class NonInteractiveScrollBarConfig(val horizontal:Boolean=false,val rtl:Boolean=false,val dark:Boolean=false,
    val content:Double=600.0,val viewport:Double=120.0,val offset:Double=90.0,val width:Double=160.0,val height:Double=120.0,
    val thickness:Double=4.0,val minimum:Double=24.0,val maximumFraction:Double=0.9,val mainInset:Double=2.0,val crossInset:Double=0.0,
    val thumbColor:String?=null,val trackColor:String?=null,val fade:Boolean=false,val action:String="none",val delay:Int=400)

@ParitySuite("non-interactive-scrollbar")
class NonInteractiveScrollBarCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer)

    private suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<NonInteractiveScrollBarConfig>()
        ComposeHost(writer,200,160).use { host ->
            val probe=Probe()
            val state=object:ScrollIndicatorState {
                override var contentSize by mutableIntStateOf((config.content.toFloat()*ComposeHost.RenderScale).roundToInt())
                override val viewportSize=(config.viewport.toFloat()*ComposeHost.RenderScale).roundToInt()
                override var scrollOffset by mutableIntStateOf((config.offset.toFloat()*ComposeHost.RenderScale).roundToInt())
            }
            var thickness by mutableStateOf(config.thickness)
            var attached by mutableStateOf(true)
            fun color(text:String)=Color(text.removePrefix("#").toLong(16))
            host.mount {
                MaterialTheme(colorScheme=if(config.dark) darkColorScheme() else lightColorScheme()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(start=16.dp,top=16.dp)) {
                        Box(Modifier.size(config.width.toFloat().dp,config.height.toFloat().dp).then(probe.modifier())) {
                            CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                                if(attached) Box(Modifier.fillMaxSize().nonInteractiveScrollbar(
                                    state=state,orientation=if(config.horizontal) Orientation.Horizontal else Orientation.Vertical,
                                    thumbColor=config.thumbColor?.let(::color) ?: NonInteractiveScrollbarDefaults.thumbColor,
                                    trackColor=config.trackColor?.let(::color) ?: Color.Transparent,
                                    thickness=thickness.toFloat().dp,thumbMinLength=config.minimum.toFloat().dp,
                                    thumbMaxLengthFraction=config.maximumFraction.toFloat(),isFadeEnabled=config.fade,fadeDelayMillis=config.delay,
                                    mainAxisTrackInset=config.mainInset.toFloat().dp,crossAxisTrackInset=config.crossInset.toFloat().dp))
                            }
                        }
                    }
                }
            }
            host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("viewport","viewport",null,
                probe.geometry().filter { it.name=="box" || it.name=="size" }))))
            sample("initial")
            if(config.action=="none") return
            for(ms in 16..1120 step 16) {
                host.advanceTo(ms*1000L)
                if(ms==32 || ms==160) state.scrollOffset+=40*ComposeHost.RenderScale
                if(ms==640 && config.action=="restart") state.scrollOffset+=80*ComposeHost.RenderScale
                if(ms==224 && config.action=="quick-restart") state.scrollOffset+=40*ComposeHost.RenderScale
                if(ms==320 && config.action=="configuration") thickness=8.0
                if(ms==352 && config.action=="configuration") state.scrollOffset+=40*ComposeHost.RenderScale
                if(ms==320 && config.action=="fits") { state.contentSize=100*ComposeHost.RenderScale; state.scrollOffset+=20*ComposeHost.RenderScale }
                if(ms==448 && config.action=="fits") state.contentSize=600*ComposeHost.RenderScale
                if(ms==640 && config.action=="reattach") attached=false
                if(ms==672 && config.action=="reattach") attached=true
                if(ms==704 && config.action=="reattach") state.scrollOffset+=40*ComposeHost.RenderScale
                host.draw(); host.frame(ms*1000L)
                if(ms in listOf(32,160,320,352,448,544,560,592,640,672,704,768,832,960,1120)) sample("time-$ms")
            }
        }
    }
}

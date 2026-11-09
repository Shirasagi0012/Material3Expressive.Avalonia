@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class SegmentedButtonIconConfig(val content:String="check", val active:Boolean=false, val rtl:Boolean=false,
    val constraint:Double?=null, val action:String="none")

@ParitySuite("segmented-button-icon")
class SegmentedButtonIconCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,180,100).use { host ->
            val scene=Scene(writer.readConfig<SegmentedButtonIconConfig>())
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("changes")
    suspend fun changes(writer:CaptureWriter) {
        val config=writer.readConfig<SegmentedButtonIconConfig>()
        ComposeHost(writer,180,100).use { host ->
            val scene=Scene(config); host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=id!="time-32000"))
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun change(time:Long,id:String,action:()->Unit) { advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id) }
            sample("initial")
            change(1000,"active"){scene.active=true};times(16000,32000,64000)
            when(config.action) {
                "reverse" -> {
                    change(65000,"reverse"){scene.active=false};times(80000,144000)
                    change(145000,"active-again"){scene.active=true}
                }
                "content" -> change(65000,"replace-active"){scene.replaced=true}
                "reattach" -> {
                    change(65000,"detach"){scene.mounted=false}
                    change(81000,"reattach"){scene.mounted=true}
                }
            }
            times(160000,400000);change(401000,"inactive"){scene.active=false}
            times(416000,480000,640000,800000)
        }
    }
    private class Scene(val config:SegmentedButtonIconConfig) {
        val probe=Probe()
        var active by mutableStateOf(config.active)
        var replaced by mutableStateOf(false)
        var mounted by mutableStateOf(true)
        @Composable fun Content() {
            MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalContentColor provides Color(0xFF245A80),
                    LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        Box(config.constraint?.let { Modifier.size(it.toFloat().dp) } ?: Modifier,contentAlignment=Alignment.Center) {
                            if(mounted) Box(probe.modifier()) {
                                val activeContent:@Composable ()->Unit = {
                                    if(replaced) Box(Modifier.size(32.dp,20.dp).background(LocalContentColor.current))
                                    else if(config.content=="check") SegmentedButtonDefaults.ActiveIcon()
                                    else Box(Modifier.size(24.dp,14.dp).background(LocalContentColor.current))
                                }
                                val inactiveContent:(@Composable ()->Unit)? = if(config.content=="swap")
                                    { { Box(Modifier.size(10.dp,22.dp).background(LocalContentColor.current)) } } else null
                                SegmentedButtonDefaults.Icon(active,activeContent,inactiveContent)
                            }
                        }
                    }
                }
            }
        }
        fun measure()=if(!mounted) emptyList() else listOf(ProbePoint("icon","selection-icon",null,probe.geometry().filter { it.name=="box" || it.name=="size" }))
    }
}

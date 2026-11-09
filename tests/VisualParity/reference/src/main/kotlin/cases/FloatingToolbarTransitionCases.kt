@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.FiniteAnimationSpec
import androidx.compose.animation.core.spring
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class FloatingToolbarTransitionConfig(val horizontal:Boolean=true,val alignment:String="start",
    val rtl:Boolean=false,val visible:Boolean=false,val action:String="cycle")

@ParitySuite("floating-toolbar-transition")
class FloatingToolbarTransitionCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,180,120).use { host ->
            val scene=Scene(writer.readConfig<FloatingToolbarTransitionConfig>())
            host.mount { scene.Content() };host.frame(-1000);host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("changes")
    suspend fun changes(writer:CaptureWriter) {
        val config=writer.readConfig<FloatingToolbarTransitionConfig>()
        ComposeHost(writer,180,120).use { host ->
            val scene=Scene(config);host.mount { scene.Content() };host.frame(-1000);host.frame(0)
            var next=16000L
            fun advance(time:Long) { while(next<=time) { host.frame(next);next+=16000 };host.advanceTo(time) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=id!="time-32000"))
            fun times(vararg times:Long) { times.forEach { advance(it);host.draw();sample("time-$it") } }
            fun change(time:Long,id:String,change:()->Unit) { advance(time);change();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id) }
            sample("initial");change(1000,"visible"){scene.visible=true};times(16000,32000,64000)
            when(config.action) {
                "reverse" -> {
                    change(65000,"reverse"){scene.visible=false};times(80000,144000)
                    change(145000,"visible-again"){scene.visible=true}
                }
                "content" -> change(65000,"replace"){scene.replaced=true}
                "theme" -> change(65000,"theme"){scene.slow=true}
                "reattach" -> {
                    change(65000,"detach"){scene.mounted=false}
                    change(81000,"reattach"){scene.mounted=true}
                }
            }
            times(160000,400000);change(401000,"hidden"){scene.visible=false};times(416000,480000,640000,800000)
            change(801000,"return"){scene.visible=true};times(816000,880000,1000000,1400000)
        }
    }
    private class Scene(val config:FloatingToolbarTransitionConfig) {
        val probe=Probe()
        var visible by mutableStateOf(config.visible)
        var replaced by mutableStateOf(false)
        var slow by mutableStateOf(false)
        var mounted by mutableStateOf(true)
        private val slowScheme=object:MotionScheme by MotionScheme.expressive() {
            override fun <T> fastSpatialSpec():FiniteAnimationSpec<T> = spring(dampingRatio=1f,stiffness=400f)
        }
        @Composable fun Content() {
            MaterialTheme(motionScheme=if(slow) slowScheme else MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        if(mounted) Box(probe.modifier()) {
                            val horizontal=when(config.alignment) { "start"->Alignment.Start;"end"->Alignment.End;else->Alignment.CenterHorizontally }
                            val vertical=when(config.alignment) { "start"->Alignment.Top;"end"->Alignment.Bottom;else->Alignment.CenterVertically }
                            AnimatedVisibility(visible,
                                enter=if(config.horizontal) FloatingToolbarDefaults.horizontalEnterTransition(horizontal) else FloatingToolbarDefaults.verticalEnterTransition(vertical),
                                exit=if(config.horizontal) FloatingToolbarDefaults.horizontalExitTransition(horizontal) else FloatingToolbarDefaults.verticalExitTransition(vertical)) {
                                Box(Modifier.size(if(replaced)110.dp else 80.dp,if(replaced)28.dp else 40.dp).drawBehind {
                                    drawRect(Color(0xFF205080))
                                    drawRect(Color(0xFFD06020),size=Size(8.dp.toPx(),size.height))
                                    drawRect(Color(0xFF30A080),topLeft=Offset(0f,(size.height-6.dp.toPx()).coerceAtLeast(0f)),size=Size(size.width,6.dp.toPx()))
                                })
                            }
                        }
                    }
                }
            }
        }
        fun measure()=if(!mounted) emptyList() else listOf(ProbePoint("visibility","toolbar-transition",null,probe.geometry().filter { it.name=="box"||it.name=="size" }))
    }
}

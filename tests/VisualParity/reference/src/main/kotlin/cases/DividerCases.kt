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
data class DividerConfig(val orientation:String="horizontal", val thickness:Double?=null, val color:String?=null,
    val dark:Boolean=false, val rtl:Boolean=false, val width:Double=160.0, val height:Double=120.0,
    val length:Double?=null, val crossSize:Double?=null, val inset:Boolean=false, val unbounded:Boolean=false, val action:String="none")

@ParitySuite("divider")
class DividerCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,200,160).use { host ->
            val scene=DividerScene(writer.readConfig<DividerConfig>())
            host.mount { scene.Content() }; host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("changes")
    suspend fun changes(writer:CaptureWriter) {
        val config=writer.readConfig<DividerConfig>()
        ComposeHost(writer,200,160).use { host ->
            val scene=DividerScene(config)
            host.mount { scene.Content() }; host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
            fun change(time:Long,id:String,action:()->Unit) {
                host.advanceTo(time); action(); writer.record(time,"mutation",id)
                host.draw(); host.frame(time); sample(id)
            }
            sample("initial")
            when(config.action) {
                "theme" -> {
                    change(1000,"dark") { scene.dark=true }; change(2000,"light") { scene.dark=false }
                }
                "thickness" -> {
                    change(1000,"hairline") { scene.thickness=0.0 }; change(2000,"thick") { scene.thickness=4.0 }
                    change(3000,"fractional") { scene.thickness=0.3 }; change(4000,"default") { scene.thickness=null }
                }
                "color" -> {
                    change(1000,"override") { scene.color="#70336699" }
                    change(2000,"theme-under-override") { scene.dark=true }; change(3000,"clear-override") { scene.color=null }
                }
                "resize" -> {
                    change(1000,"smaller") { scene.width=80.0; scene.height=64.0 }
                    change(2000,"restore") { scene.width=160.0; scene.height=120.0 }
                }
                "orientation" -> {
                    change(1000,"switched") { scene.orientation=if(config.orientation=="horizontal") "vertical" else "horizontal" }
                    change(2000,"restored") { scene.orientation=config.orientation }
                }
            }
            // Same event-only idle checkpoint; ExtraImages adds a passive capture in self-test.
            host.advanceTo(5000); sample("idle-layout",image=false)
        }
    }

    private class DividerScene(val config:DividerConfig) {
        val probe=Probe()
        var orientation by mutableStateOf(config.orientation)
        var thickness by mutableStateOf(config.thickness)
        var color by mutableStateOf(config.color)
        var dark by mutableStateOf(config.dark)
        var width by mutableStateOf(config.width)
        var height by mutableStateOf(config.height)

        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(start=16.dp,top=16.dp)) {
                        Box(Modifier.size(width.toFloat().dp,height.toFloat().dp)) {
                            val vertical=orientation=="vertical"
                            var modifier:Modifier=Modifier
                            // Row/Column retain a bounded remaining main-axis constraint; unlike
                            // Avalonia StackPanel they do not exercise an infinite child maximum.
                            if(config.unbounded) modifier=if(vertical)
                                modifier.wrapContentHeight(align=Alignment.Top,unbounded=true)
                            else modifier.wrapContentWidth(align=Alignment.Start,unbounded=true)
                            if(config.inset) modifier=modifier.padding(start=12.dp,top=8.dp,end=20.dp,bottom=16.dp)
                            modifier=modifier.then(probe.modifier())
                            if(config.length!=null) modifier=if(vertical) modifier.height(config.length.toFloat().dp) else modifier.width(config.length.toFloat().dp)
                            if(config.crossSize!=null) modifier=if(vertical) modifier.width(config.crossSize.toFloat().dp) else modifier.height(config.crossSize.toFloat().dp)
                            val stroke=thickness?.toFloat()?.dp ?: DividerDefaults.Thickness
                            val paint=color?.let { value ->
                                val argb=value.removePrefix("#").toLong(16)
                                Color(if(value.length==7) argb or 0xFF000000 else argb)
                            } ?: DividerDefaults.color
                            // Real fixed-baseline APIs, including their zero/negative/rounded layout behavior.
                            if(vertical) VerticalDivider(modifier,stroke,paint) else HorizontalDivider(modifier,stroke,paint)
                        }
                    }
                }
            }
        }

        fun measure()=listOf(ProbePoint("divider","divider",null,probe.geometry().filter { it.name=="box" || it.name=="size" }))
    }
}

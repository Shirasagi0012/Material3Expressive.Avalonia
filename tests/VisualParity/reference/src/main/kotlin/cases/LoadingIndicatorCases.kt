@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.AbsoluteRoundedCornerShape
import androidx.compose.foundation.shape.GenericShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import androidx.graphics.shapes.RoundedPolygon
import kotlinx.serialization.Serializable

@Serializable
data class LoadingIndicatorConfig(val contained:Boolean=false, val progress:Double=.5, val indeterminate:Boolean=false,
    val dark:Boolean=false, val rtl:Boolean=false, val polygons:String="default", val shape:String="full",
    val width:Double?=null, val height:Double?=null, val constraintWidth:Double?=null, val constraintHeight:Double?=null,
    val color:String?=null, val containerColor:String?=null, val action:String="none")

@ParitySuite("loading-indicator")
class LoadingIndicatorCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,320,160).use { host ->
            val scene=Scene(writer.readConfig<LoadingIndicatorConfig>())
            host.mount { scene.Content() }; host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("dynamic")
    suspend fun dynamic(writer:CaptureWriter) {
        val config=writer.readConfig<LoadingIndicatorConfig>()
        ComposeHost(writer,320,160).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(0)
            var frame=0L
            fun advance(time:Long) {
                if(config.action!="sparse")
                    while(frame+16000<=time) { frame+=16000; host.frame(frame) }
                host.advanceTo(time); host.draw()
            }
            fun sample(time:Long,id:String) { advance(time); writer.submit(host.snapshot(id,id,scene.measure())) }
            fun change(time:Long,id:String,action:()->Unit) {
                advance(time); action(); writer.record(time,"mutation",id); host.draw()
                // Recomposition and newly created animation waiters are two batches;
                // both hosts flush them without advancing logical time.
                host.frame(time); host.frame(time)
                writer.submit(host.snapshot(id,id,scene.measure()))
            }
            sample(0,"initial")
            when(config.action) {
                "progress" -> {
                    change(200000,"progress-85") { scene.progress=.85 }
                    change(400000,"progress-10") { scene.progress=.1 }
                    change(600000,"progress-100") { scene.progress=1.0 }
                }
                "mode" -> {
                    change(200000,"indeterminate") { scene.indeterminate=true }
                    sample(280000,"morphing"); sample(900000,"second-morph")
                    change(1000000,"determinate") { scene.indeterminate=false }
                    change(1200000,"indeterminate-again") { scene.indeterminate=true }; sample(1280000,"fresh-morph")
                }
                "polygons" -> {
                    sample(80000,"before-change"); change(100000,"two-shapes-during-spring") { scene.polygons=Scene.sequence("two") }
                    sample(180000,"retained-progress"); sample(740000,"next-morph")
                    change(1100000,"three-shapes") { scene.polygons=Scene.sequence("three") }
                    sample(1450000,"changed-sequence")
                    change(1600000,"equal-list") { scene.polygons=scene.polygons?.toList() }
                    change(1800000,"default-sequence") { scene.polygons=null }
                    sample(1880000,"default-retained-progress"); sample(2500000,"default-next-morph")
                }
                "theme" -> {
                    change(200000,"dark") { scene.dark=true }; change(400000,"override") { scene.color="#FFFF0000" }
                    change(600000,"light-under-override") { scene.dark=false }; change(800000,"clear-override") { scene.color=null }
                }
                "detach" -> {
                    sample(80000,"morphing"); change(100000,"detached") { scene.present=false }
                    sample(800000,"detached-late"); change(1000000,"reattached") { scene.present=true }
                    sample(1080000,"fresh-state"); sample(1700000,"fresh-second-morph")
                }
                "sparse" -> listOf(16000L,1000000,1100000,2000000,2100000,3500000).forEach {
                    host.frame(it); writer.submit(host.snapshot("frame-$it","sparse frame",scene.measure()))
                }
                else -> listOf(80000L,160000,256000,320000,640000,650000,656000,736000,
                    1300000,1384000,2600000,3900000,4550000,4640000,4666000,4672000,5200000,9100000).forEach { sample(it,"time-$it") }
            }
        }
    }

    private class Scene(val config:LoadingIndicatorConfig) {
        val probe=Probe()
        var progress by mutableStateOf(config.progress)
        var indeterminate by mutableStateOf(config.indeterminate)
        var dark by mutableStateOf(config.dark)
        var color by mutableStateOf(config.color)
        var polygons by mutableStateOf(sequence(config.polygons))
        var present by mutableStateOf(true)
        companion object {
            fun sequence(kind:String):List<RoundedPolygon>?=when(kind) {
                "two" -> listOf(MaterialShapes.Pill,MaterialShapes.SoftBurst)
                "three" -> listOf(MaterialShapes.Circle,MaterialShapes.Pentagon,MaterialShapes.Cookie4Sided)
                else -> null
            }
        }
        fun paint(value:String):Color {
            val argb=value.removePrefix("#").toLong(16)
            return Color(if(value.length==7) argb or 0xFF000000 else argb)
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                    if(config.constraintWidth!=null && config.constraintHeight!=null)
                        Box(Modifier.size(config.constraintWidth.toFloat().dp,config.constraintHeight.toFloat().dp)) { if(present) Indicator() }
                    else if(present) Indicator()
                }
            }
        }
        @Composable fun Indicator() {
            CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                var modifier:Modifier=probe.modifier()
                if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                val sequence=polygons ?: if(indeterminate) LoadingIndicatorDefaults.IndeterminateIndicatorPolygons
                    else LoadingIndicatorDefaults.DeterminateIndicatorPolygons
                if(config.contained) {
                    val active=color?.let(::paint) ?: LoadingIndicatorDefaults.containedIndicatorColor
                    val background=config.containerColor?.let(::paint) ?: LoadingIndicatorDefaults.containedContainerColor
                    val shape:Shape=when(config.shape) {
                        "round" -> AbsoluteRoundedCornerShape(12.dp)
                        "asymmetric" -> AbsoluteRoundedCornerShape(24.dp,4.dp,8.dp,0.dp)
                        "ellipse" -> GenericShape { size,_ -> addOval(Rect(Offset.Zero,size)) }
                        "triangle" -> GenericShape { size,direction ->
                            moveTo(if(direction==LayoutDirection.Rtl) size.width else 0f,0f)
                            lineTo(size.width,size.height); lineTo(0f,size.height); close()
                        }
                        else -> LoadingIndicatorDefaults.containerShape
                    }
                    if(indeterminate) ContainedLoadingIndicator(modifier,background,active,shape,sequence)
                    else ContainedLoadingIndicator({progress.toFloat()},modifier,background,active,shape,sequence)
                } else {
                    val active=color?.let(::paint) ?: LoadingIndicatorDefaults.indicatorColor
                    if(indeterminate) LoadingIndicator(modifier,active,sequence)
                    else LoadingIndicator({progress.toFloat()},modifier,active,sequence)
                }
            }
        }
        fun measure()=if(present) listOf(ProbePoint("indicator","indicator",null,probe.geometry().filter { it.name=="box" || it.name=="size" })) else emptyList()
    }
}

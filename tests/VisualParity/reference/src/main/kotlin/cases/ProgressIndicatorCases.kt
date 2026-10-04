@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class ProgressIndicatorConfig(val kind:String="linear", val progress:Double=.5, val indeterminate:Boolean=false,
    val dark:Boolean=false, val rtl:Boolean=false, val cap:String="round", val strokeWidth:Double?=null, val trackWidth:Double?=null,
    val gap:Double?=null, val stop:Double?=null, val amplitude:Double?=null, val wavelength:Double?=null, val speed:Double?=0.0,
    val width:Double?=null, val height:Double?=null, val constraintWidth:Double?=null, val constraintHeight:Double?=null,
    val color:String?=null, val trackColor:String?=null, val action:String="none")

@ParitySuite("progress-indicator")
class ProgressIndicatorCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,320,160).use { host ->
            val scene=Scene(writer.readConfig<ProgressIndicatorConfig>())
            host.mount { scene.Content() }; host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("dynamic")
    suspend fun dynamic(writer:CaptureWriter) {
        val config=writer.readConfig<ProgressIndicatorConfig>()
        ComposeHost(writer,320,160).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(0)
            var frame=0L
            fun advance(time:Long) {
                while(frame+16000<=time) { frame+=16000; host.frame(frame) }
                host.advanceTo(time); host.draw()
            }
            fun sample(time:Long,id:String,image:Boolean=true) {
                advance(time); writer.submit(host.snapshot(id,id,scene.measure(),image))
            }
            fun change(time:Long,id:String,action:()->Unit) {
                advance(time); action(); writer.record(time,"mutation",id); host.draw()
                // Recomposition and newly created animation waiters are separate batches.
                // Both hosts flush them at this same logical time without advancing time.
                host.frame(time); host.frame(time)
                writer.submit(host.snapshot(id,id,scene.measure()))
            }
            sample(0,"initial")
            when(config.action) {
                "progress" -> {
                    change(200000,"progress-80") { scene.progress=.8 }; sample(280000,"amplitude-increasing")
                    change(350000,"progress-98-during-amplitude") { scene.progress=.98 }; sample(600000,"amplitude-late")
                    change(800000,"progress-40") { scene.progress=.4 }; change(1000000,"progress-98") { scene.progress=.98 }
                    sample(1200000,"amplitude-decreasing")
                    change(1250000,"progress-50-during-amplitude") { scene.progress=.5 }
                    change(1600000,"progress-60") { scene.progress=.6 }
                    sample(1900000,"amplitude-increasing-again"); sample(2200000,"late",false)
                }
                "mode" -> {
                    sample(320000,"determinate-wave"); change(400000,"indeterminate") { scene.indeterminate=true }
                    sample(800000,"indeterminate-progress"); change(1000000,"determinate") { scene.indeterminate=false }
                    sample(1200000,"determinate-again")
                }
                "theme" -> {
                    change(200000,"dark") { scene.dark=true }; change(400000,"override") { scene.color="#FFFF0000" }
                    change(600000,"light-under-override") { scene.dark=false }; change(800000,"clear-override") { scene.color=null }
                }
                "speed" -> {
                    sample(320000,"moving"); change(400000,"speed-change") { scene.speed=10.0 }; sample(800000,"changed-speed")
                    change(1000000,"stopped") { scene.speed=0.0 }; sample(1200000,"stopped-late")
                    change(1400000,"restart") { scene.speed=null }; sample(1800000,"restarted")
                }
                else -> listOf(16000L,250000,650000,900000,1000000,1250000,1500000,1744000,1750000,1760000,
                    3000000,4800000,5984000,6000000,6016000,9000000,9024000).forEach { sample(it,"time-$it") }
            }
        }
    }

    private class Scene(val config:ProgressIndicatorConfig) {
        val probe=Probe()
        var progress by mutableStateOf(config.progress)
        var indeterminate by mutableStateOf(config.indeterminate)
        var dark by mutableStateOf(config.dark)
        var color by mutableStateOf(config.color)
        var speed by mutableStateOf(config.speed)
        fun paint(value:String):Color {
            val argb=value.removePrefix("#").toLong(16)
            return Color(if(value.length==7) argb or 0xFF000000 else argb)
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                    if(config.constraintWidth!=null && config.constraintHeight!=null)
                        Box(Modifier.size(config.constraintWidth.toFloat().dp,config.constraintHeight.toFloat().dp)) { Indicator() }
                    else Indicator()
                }
            }
        }
        @Composable fun Indicator() {
            CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                var modifier:Modifier=probe.modifier()
                if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                val cap=when(config.cap) { "butt" -> StrokeCap.Butt; "square" -> StrokeCap.Square; else -> StrokeCap.Round }
                val active=color?.let(::paint) ?: ProgressIndicatorDefaults.linearColor
                val track=config.trackColor?.let(::paint) ?: if(config.kind=="circular" && indeterminate)
                    ProgressIndicatorDefaults.circularIndeterminateTrackColor else ProgressIndicatorDefaults.linearTrackColor
                val gap=config.gap?.toFloat()?.dp ?: if(config.kind.startsWith("linear"))
                    ProgressIndicatorDefaults.LinearIndicatorTrackGapSize else ProgressIndicatorDefaults.CircularIndicatorTrackGapSize
                val stop=config.stop?.toFloat()?.dp ?: ProgressIndicatorDefaults.LinearTrackStopIndicatorSize
                val density=LocalDensity.current
                val stroke=Stroke(with(density) { (config.strokeWidth?.toFloat()?.dp ?: 4.dp).toPx() },cap=cap)
                val trackStroke=Stroke(with(density) { (config.trackWidth?.toFloat()?.dp ?: 4.dp).toPx() },cap=cap)
                val wavelength=config.wavelength?.toFloat()?.dp ?: if(config.kind=="circular-wavy")
                    WavyProgressIndicatorDefaults.CircularWavelength else if(indeterminate)
                    WavyProgressIndicatorDefaults.LinearIndeterminateWavelength else WavyProgressIndicatorDefaults.LinearDeterminateWavelength
                val waveSpeed=speed?.toFloat()?.dp ?: wavelength
                val amplitude:(Float)->Float={ p -> config.amplitude?.toFloat() ?: WavyProgressIndicatorDefaults.indicatorAmplitude(p) }
                // Each branch calls the actual fixed dependency's API, including its native defaults/helpers.
                when(config.kind) {
                    "linear" -> if(indeterminate) LinearProgressIndicator(modifier,active,track,cap,gap)
                        else LinearProgressIndicator({progress.toFloat()},modifier,active,track,cap,gap) {
                            ProgressIndicatorDefaults.drawStopIndicator(this,stop,active,cap)
                        }
                    "circular" -> if(indeterminate) CircularProgressIndicator(modifier,active,
                        config.strokeWidth?.toFloat()?.dp ?: ProgressIndicatorDefaults.CircularStrokeWidth,track,cap,gap)
                        else CircularProgressIndicator({progress.toFloat()},modifier,active,
                            config.strokeWidth?.toFloat()?.dp ?: ProgressIndicatorDefaults.CircularStrokeWidth,track,cap,gap)
                    "linear-wavy" -> if(indeterminate) LinearWavyProgressIndicator(modifier,active,track,stroke,trackStroke,gap,
                        config.amplitude?.toFloat() ?: 1f,wavelength,waveSpeed)
                        else LinearWavyProgressIndicator({progress.toFloat()},modifier,active,track,stroke,trackStroke,gap,stop,amplitude,wavelength,waveSpeed)
                    else -> if(indeterminate) CircularWavyProgressIndicator(modifier,active,track,stroke,trackStroke,gap,
                        config.amplitude?.toFloat() ?: 1f,wavelength,waveSpeed)
                        else CircularWavyProgressIndicator({progress.toFloat()},modifier,active,track,stroke,trackStroke,gap,amplitude,wavelength,waveSpeed)
                }
            }
        }
        fun measure()=listOf(ProbePoint("indicator","indicator",null,probe.geometry().filter { it.name=="box" || it.name=="size" }))
    }
}

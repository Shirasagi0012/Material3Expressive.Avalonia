@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.*
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class CheckBoxPathEffectConfig(val effect:String,val state:String="on",val rtl:Boolean=false,val action:String="none")

@ParitySuite("checkbox-path-effect")
class CheckBoxPathEffectCases {
    @ParityScenario("capture")
    suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<CheckBoxPathEffectConfig>()
        ComposeHost(writer,160,128).use { host ->
            var state by mutableStateOf(when(config.state) { "off" -> ToggleableState.Off; "on" -> ToggleableState.On; else -> ToggleableState.Indeterminate })
            var effect by mutableStateOf(config.effect)
            val probe=Probe()
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        val stroke=with(LocalDensity.current) {
                            val corner=PathEffect.cornerPathEffect(3.dp.toPx())
                            val dash=PathEffect.dashPathEffect(floatArrayOf(2.dp.toPx(),2.dp.toPx()),1.dp.toPx())
                            Stroke(width=2.dp.toPx(),cap=StrokeCap.Square,join=StrokeJoin.Miter,pathEffect=when(effect) {
                                "corner" -> corner; "dash" -> dash; "compose" -> PathEffect.chainPathEffect(dash,corner); else -> null
                            })
                        }
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            TriStateCheckbox(state,{},stroke,stroke,probe.modifier())
                        }
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            fun sample(id:String) {
                val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
                val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
                val actual=semantics[SemanticsProperties.ToggleableState]
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("checkbox","checkbox",null,
                    probe.geometry().filter { it.name=="box" || it.name=="size" }+number("state",when(actual) {
                        ToggleableState.Off -> 0.0; ToggleableState.On -> 1.0; ToggleableState.Indeterminate -> 2.0
                    },"enum",definition="checkbox.toggle-state")))))
            }
            var next=16000L
            fun advance(time:Long) { while(next<=time) { host.frame(next); next+=16000 }; host.advanceTo(time) }
            fun at(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun change(time:Long,id:String,action:()->Unit) { advance(time); action(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            sample("initial")
            if(config.action=="none") return
            if(config.action=="replace") {
                change(1000,"replace") { effect=if(config.effect=="corner") "dash" else "corner" }; at(16000)
                change(17000,"clear") { effect="none" }; at(32000,160000); return
            }
            change(1000,"check") { state=ToggleableState.On }; at(16000,32000,64000)
            if(config.action=="reverse") change(65000,"reverse") { state=ToggleableState.Indeterminate }
            at(96000,112000,160000,240000,400000,800000)
        }
    }
}

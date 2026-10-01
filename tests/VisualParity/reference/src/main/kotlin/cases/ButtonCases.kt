package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.geometry.Offset
import kotlinx.serialization.Serializable
import parity.CaseDefaults.Theme

@Serializable data class ButtonConfig(val enabled:Boolean, val text:String)
@ParitySuite("button")
class ButtonCases {
    class ButtonProbe(val config:ButtonConfig) {
        val allocation=Probe(); val label=Probe(); var clicks=0
        val interactions=MutableInteractionSource(); val presses=mutableSetOf<PressInteraction.Press>(); val hovers=mutableSetOf<HoverInteraction.Enter>()
        fun points()=listOf(ProbePoint("button","allocation",null,allocation.geometry()+listOf(flag("enabled",config.enabled,source="declared"),number("clicks",clicks.toDouble(),"count"))),
            label.textPoint("label","container"))+buttonMeasurements(allocation,presses.isNotEmpty(),hovers.isNotEmpty())
        @Composable fun Content() {
            LaunchedEffect(interactions) {
                interactions.interactions.collect { event -> when(event) {
                    is PressInteraction.Press -> presses.add(event)
                    is PressInteraction.Release -> presses.remove(event.press)
                    is PressInteraction.Cancel -> presses.remove(event.press)
                    is HoverInteraction.Enter -> hovers.add(event)
                    is HoverInteraction.Exit -> hovers.remove(event.enter)
                } }
            }
            Button({clicks++},ButtonDefaults.shapes(),allocation.modifier(),enabled=config.enabled,interactionSource=interactions) {
                Text(config.text,label.modifier(),onTextLayout={label.layout=it})
            }
        }
    }
    @ParityScenario("static")
    suspend fun buttonStatic(w:CaptureWriter) {
        val config=w.readConfig<ButtonConfig>()
        ComposeHost(w).use { h -> val b=ButtonProbe(config); h.mount { Theme(w) { b.Content() } }
            h.frame(-2000); h.frame(-1000); h.frame(0)
            w.submit(h.snapshot("initial",if(config.enabled) "enabled" else "disabled",b.points())) }
    }
    @ParityScenario("press")
    suspend fun buttonPressed(w:CaptureWriter) {
        val config=w.readConfig<ButtonConfig>()
        ComposeHost(w).use { h -> val b=ButtonProbe(config); h.mount { Theme(w) { b.Content() } }
            h.frame(-2000); h.frame(-1000); h.frame(0); w.submit(h.snapshot("initial",if(config.enabled) "enabled" else "disabled",b.points()))
            val box=b.allocation.geometry()[0].numbers!!; val center=Offset((box[0]+box[2]/2).toFloat(),(box[1]+box[3]/2).toFloat())
            h.input("move",center); h.input("down",center)
            for(time in listOf(16000L,32000L,80000L)) { h.frame(time); w.submit(h.snapshot("pressed-$time","pressed",b.points(),time!=32000L)) }
            h.advanceTo(88000); h.input("up",center); h.frame(96000); w.submit(h.snapshot("released","released",b.points()))
            h.frame(300000); w.submit(h.snapshot("late-300000","released",b.points()))
        }
    }
}

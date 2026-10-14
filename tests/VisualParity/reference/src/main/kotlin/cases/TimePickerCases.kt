@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class,androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
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

@Serializable data class TimePickerConfig(val kind:String="dial",val vibrant:Boolean=false,val horizontal:Boolean=false,val clock24:Boolean=false,val dark:Boolean=false,val rtl:Boolean=false,val toggle:Boolean=false,val action:String="none")

@ParitySuite("time-picker")
class TimePickerCases {
    @ParityScenario("static") suspend fun static(w:CaptureWriter)=capture(w,false)
    @ParityScenario("changes") suspend fun changes(w:CaptureWriter)=capture(w,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val c=writer.readConfig<TimePickerConfig>();val state=TimePickerState(14,30,c.clock24);val probe=Probe()
        ComposeHost(writer,640,520).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                            val layout=if(c.horizontal)TimePickerLayoutType.Horizontal else TimePickerLayoutType.Vertical
                            val shapes=TimePickerDefaults.shapes()
                            val toggle:@Composable ()->Unit={Box(Modifier.size(48.dp).background(Color.Magenta))}
                            when(c.kind) {
                                "input" -> if(c.toggle)TimeInput(state,shapes,toggle,probe.modifier()) else if(c.vibrant)TimeInput(state,shapes,probe.modifier()) else TimeInput(state,probe.modifier())
                                "scroll" -> if(c.toggle)TimeScroll(state,shapes,toggle,probe.modifier()) else TimeScroll(state,shapes,probe.modifier())
                                else -> if(c.vibrant)TimePicker(state,shapes,probe.modifier(),layoutType=layout) else TimePicker(state,probe.modifier(),layoutType=layout)
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("picker","time-picker",null,
                probe.geometry().filter{it.name=="box"||it.name=="size"}+listOf(number("hour",state.hour.toDouble(),"hour",definition="time-picker.selected-hour"),number("minute",state.minute.toDouble(),"minute",definition="time-picker.selected-minute")))),image))
            sample("initial")
            if(dynamic) {
                host.advanceTo(1000);state.hour=23;state.minute=59;writer.record(1000,"mutation","time");host.draw();host.frame(1000);sample("time-start")
                for(t in 16000L..512000L step 16000L)host.frame(t);sample("time-settled")
                host.advanceTo(600000);state.selection=TimePickerSelectionMode.Minute;writer.record(600000,"mutation","minute-selection");host.draw();host.frame(600000);sample("minute-start")
                for(t in 616000L..1112000L step 16000L)host.frame(t);sample("minute-settled");host.advanceTo(1200000);sample("idle",false)
            }
        }
    }
}

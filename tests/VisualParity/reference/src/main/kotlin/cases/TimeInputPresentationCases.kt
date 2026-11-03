@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.*
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class TimeInputPresentationConfig(val vibrant:Boolean=false,val clock24:Boolean=false,
    val dark:Boolean=false,val dynamic:Boolean=false,val error:Boolean=false,val action:String="selection")

@ParitySuite("time-input-presentation")
class TimeInputPresentationCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimeInputPresentationConfig>()
        val state=TimePickerState(2,30,c.clock24);val picker=Probe()
        ComposeHost(writer,420,280).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        if(c.vibrant)TimeInput(state,TimePickerDefaults.shapes(),picker.modifier())
                        else TimeInput(state,picker.modifier())
                    }
                }
            };host.frame(0)
            // Read the actual fixed Core layout-node semantics, including its editor action.
            fun semantics(node:Any):List<SemanticsConfiguration> {
                val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val children=node.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(node) as List<*>
                return listOfNotNull(own)+children.flatMap { semantics(checkNotNull(it)) }
            }
            fun all()=semantics(checkNotNull(field(checkNotNull(picker.coordinates),"layoutNode")))
            fun editor()=all().single { it.getOrElse(SemanticsProperties.Focused){false}&&it.contains(SemanticsProperties.EditableText) }
            fun sample(id:String) {
                val edit=editor()
                writer.submit(host.snapshot(id,id,listOf(
                    ProbePoint("picker","time-input",null,picker.geometry()+listOf(
                        text("selection",if(state.selection==TimePickerSelectionMode.Hour)"hour" else "minute",definition="time-picker.selection"),
                        number("hour",state.hour.toDouble(),"hour",definition="time-picker.selected-hour"),
                        number("minute",state.minute.toDouble(),"minute",definition="time-picker.selected-minute"),
                        flag("valid",state.isHourInputValid&&state.isMinuteInputValid,definition="time-input.valid"),
                        number("liveErrors",all().count { it.getOrElseNullable(SemanticsProperties.LiveRegion){null}==LiveRegionMode.Polite }.toDouble(),"count",definition="time-input.visible-polite-errors"))),
                    ProbePoint("editor","time-editor","picker",listOf(
                        flag("focused",edit[SemanticsProperties.Focused],definition="time-input.editor-focused"),
                        text("text",edit[SemanticsProperties.EditableText].text,definition="time-input.editor-text")))),image=!c.dynamic))
            }
            fun setText(value:String) { check(checkNotNull(editor()[SemanticsActions.SetText].action).invoke(AnnotatedString(value))) }
            var time=0L
            fun change(id:String,action:()->Unit) {
                time+=16000;host.advanceTo(time);action();writer.record(time,"mutation",id)
                host.draw();host.frame(time);sample(id)
            }
            if(c.error)change("invalid-hour"){setText("99")}else sample("initial")
            if(!c.dynamic)return
            if(c.action=="editing") {
                change("select-minute"){state.selection=TimePickerSelectionMode.Minute}
                listOf("007","0001","-1","75","100","hello","١","１２","05").forEach { value ->
                    change("replace-"+when(value){"-1"->"negative";"١"->"arabic";"１２"->"fullwidth";else->value}){setText(value)}
                }
                return
            }
            change("invalid-hour"){setText("99")}
            change("minute-selection"){state.selection=TimePickerSelectionMode.Minute}
            change("invalid-minute"){setText("75")}
            if(c.action=="external") {
                change("external-hour"){state.hour=5}
                change("hour-selection"){state.selection=TimePickerSelectionMode.Hour}
                change("external-minute"){state.minute=45}
                change("minute-selection-again"){state.selection=TimePickerSelectionMode.Minute}
                return
            }
            change("hour-draft"){state.selection=TimePickerSelectionMode.Hour}
            change("correct-hour"){setText("01")}
            change("correct-minute"){setText("05")}
            repeat(32){time+=16000;host.frame(time)};sample("settled")
        }
    }
}

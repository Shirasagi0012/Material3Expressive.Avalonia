@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class TimePickerKeyboardConfig(val clock24:Boolean=false,val rtl:Boolean=false)

@ParitySuite("time-picker-keyboard")
class TimePickerKeyboardCases {
    @ParityScenario("keyboard") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimePickerKeyboardConfig>()
        val state=TimePickerState(11,55,c.clock24);val probe=Probe();var focus:FocusManager?=null
        ComposeHost(writer,360,480).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme()) {
                    focus=LocalFocusManager.current
                    Column(Modifier.fillMaxSize().background(Color.White)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            TimePicker(state,probe.modifier().padding(16.dp),layoutType=TimePickerLayoutType.Vertical)
                        }
                        Button(onClick={}) { Text("After") }
                    }
                }
            };host.frame(0)
            fun focusedTick(node:Any):Float? {
                val semantics=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                if(semantics?.getOrElse(SemanticsProperties.Focused){false}==true)
                    return semantics.getOrElse(SemanticsProperties.TraversalIndex){0f}
                val children=node.javaClass.methods.single { it.name.startsWith("getChildren$") && it.parameterCount==0 }.invoke(node) as List<*>
                return children.firstNotNullOfOrNull { focusedTick(checkNotNull(it)) }
            }
            fun sample(id:String) {
                val index=focusedTick(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode")))?:0f
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("picker","time-picker",null,listOf(
                    number("hour",state.hour.toDouble(),"hour",definition="time-picker.selected-hour"),
                    number("minute",state.minute.toDouble(),"minute",definition="time-picker.selected-minute"),
                    number("focusedTick",index.toDouble(),"index",definition="time-picker.focused-tick.one-based-or-zero"),
                    text("selection",if(state.selection==TimePickerSelectionMode.Hour)"hour" else "minute",definition="time-picker.selection"))))))
            }
            var time=0L
            fun key(id:String,key:Key,shift:Boolean=false) {
                time+=16000;host.advanceTo(time)
                host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown,isShiftPressed=shift))
                host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp,isShiftPressed=shift))
                host.scheduler.runCurrent();host.draw();host.frame(time);sample(id)
            }
            sample("initial");checkNotNull(focus).moveFocus(FocusDirection.Next);host.draw()
            key("activate-hour",Key.Enter);key("next-ring",Key.DirectionRight);key("previous-ring",Key.DirectionUp)
            key("next-again",Key.DirectionDown);key("commit-hour",Key.Spacebar);key("leave-hour",Key.Tab)
            key("activate-minute",Key.Enter);key("minute-wrap",Key.DirectionRight);key("commit-minute",Key.Enter)
            key("back-to-minute",Key.Tab,true);key("reactivate-minute",Key.Enter);key("leave-minute",Key.Tab)
            repeat(32){time+=16000;host.frame(time)};sample("settled")
            checkNotNull(focus).clearFocus();checkNotNull(focus).moveFocus(FocusDirection.Next);checkNotNull(focus).moveFocus(FocusDirection.Next);host.draw()
            key("activate-settled",Key.Enter);key("wrap-settled",Key.DirectionRight);key("commit-settled",Key.Enter)
        }
    }
}

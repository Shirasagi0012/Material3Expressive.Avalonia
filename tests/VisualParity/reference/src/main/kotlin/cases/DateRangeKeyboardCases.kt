@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class,androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.focusable
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable
import java.util.Locale

@Serializable data class DateRangeKeyboardConfig(val rtl:Boolean=false,val toggle:Boolean=true)

@ParitySuite("date-range-keyboard")
class DateRangeKeyboardCases {
    @ParityScenario("tab") suspend fun tab(writer:CaptureWriter) {
        val c=writer.readConfig<DateRangeKeyboardConfig>()
        val previousLocale=Locale.getDefault();Locale.setDefault(Locale.US)
        try {
            val state=DateRangePickerState(locale=Locale.US,initialDisplayedMonthMillis=1706745600000L,yearRange=2024..2024)
            val probe=Probe();var before=false;var after=false
            ComposeHost(writer,440,780).use { host ->
                host.mount {
                    MaterialTheme(colorScheme=lightColorScheme()) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            Column(Modifier.fillMaxSize().background(Color.White)) {
                                Box(Modifier.size(24.dp).background(Color.Blue).onFocusChanged{before=it.isFocused}.focusable())
                                DateRangePicker(state,probe.modifier().width(400.dp).height(680.dp),showModeToggle=c.toggle)
                                Box(Modifier.size(24.dp).background(Color.Red).onFocusChanged{after=it.isFocused}.focusable())
                            }
                        }
                    }
                };host.frame(0)
                for(t in 16000L..512000L step 16000L)host.frame(t)
                fun root()=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
                fun children(node:Any)=node.javaClass.methods.single{it.name.startsWith("getChildren$")&&it.parameterCount==0}.invoke(node) as List<*>
                fun semantics(node:Any)=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                fun find(node:Any, predicate:(SemanticsConfiguration)->Boolean):SemanticsConfiguration? {
                    val own=semantics(node)
                    if(own!=null && predicate(own))return own
                    return children(node).firstNotNullOfOrNull{find(checkNotNull(it),predicate)}
                }
                fun focusDate(time:Long) {
                    host.advanceTo(time)
                    val day=checkNotNull(find(root()) { s ->
                        s.getOrNull(SemanticsActions.RequestFocus)?.action!=null &&
                            s.getOrNull(SemanticsProperties.Text)?.any{it.text.contains("February 15") }==true
                    }) { "February 15 focus semantics missing" }
                    check(day.getOrNull(SemanticsActions.RequestFocus)?.action?.invoke()==true)
                    writer.record(time,"input","focus February 15");host.scheduler.runCurrent();host.draw();host.frame(time)
                }
                fun sample(id:String) {
                    val focused=find(root()){it.getOrNull(SemanticsProperties.Focused)==true}
                    val calendar=focused?.getOrNull(SemanticsProperties.Text)?.any{it.text.contains("2024")}==true
                    writer.submit(host.snapshot(id,id,listOf(ProbePoint("focus","keyboard-focus",null,listOf(
                        flag("before",before,definition="range-keyboard.before"),flag("after",after,definition="range-keyboard.after"),
                        flag("calendar",calendar,definition="range-keyboard.calendar"),flag("header",focused!=null&&!calendar,definition="range-keyboard.header")
                    )))))
                }
                fun key(time:Long,shift:Boolean) {
                    host.advanceTo(time)
                    host.scene.sendKeyEvent(KeyEvent(Key.Tab,KeyEventType.KeyDown,isShiftPressed=shift))
                    host.scene.sendKeyEvent(KeyEvent(Key.Tab,KeyEventType.KeyUp,isShiftPressed=shift))
                    host.scheduler.runCurrent();host.draw();host.frame(time)
                }
                focusDate(600000);sample("date-focused");key(700000,false);sample("tab-exited")
                focusDate(800000);key(900000,true);sample("shift-tab-exited")
            }
        } finally { Locale.setDefault(previousLocale) }
    }
}

@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.focusable
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.input.key.*
import androidx.compose.ui.semantics.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.platform.LocalViewConfiguration
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.platform.PlatformContext
import kotlinx.serialization.Serializable
import java.util.Locale

@Serializable data class DatePickerConfig(val range:Boolean=false,val input:Boolean=false,val selected:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,val culture:String="en-US",val width:Double=400.0,val action:String="none")

@ParitySuite("date-picker")
class DatePickerCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("changes") suspend fun changes(writer:CaptureWriter)=capture(writer,true)
    suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val c=writer.readConfig<DatePickerConfig>()
        // Explicit locale avoids JVM-global state leaking between cases.
        val locale=Locale.forLanguageTag(c.culture)
        val start=if(c.selected)1707523200000L else null // 2024-02-10 UTC
        val end=if(c.selected)1708387200000L else null
        val single=DatePickerState(locale=locale,initialSelectedDateMillis=start,initialDisplayedMonthMillis=1706745600000L,yearRange=2023..2025,initialDisplayMode=if(c.input)DisplayMode.Input else DisplayMode.Picker)
        val range=DateRangePickerState(locale=locale,initialSelectedStartDateMillis=start,initialSelectedEndDateMillis=end,initialDisplayedMonthMillis=1706745600000L,yearRange=2023..2025,initialDisplayMode=if(c.input)DisplayMode.Input else DisplayMode.Picker)
        val probe=Probe()
        var afterFocused=false
        val d=ComposeHost.RenderScale.toFloat()
        val viewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration { override val touchSlop=18*d }
        ComposeHost(writer,440,680).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme()) {
                    CompositionLocalProvider(LocalViewConfiguration provides viewConfiguration, LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                            Box(Modifier.width(c.width.toFloat().dp)) {
                                if(c.action=="year-keys")Column {
                                    DatePicker(single,probe.modifier())
                                    Box(Modifier.size(40.dp,24.dp).background(Color.Blue).onFocusChanged {afterFocused=it.isFocused}.focusable())
                                } else if(c.range)DateRangePicker(range,probe.modifier()) else DatePicker(single,probe.modifier())
                            }
                        }
                    }
                }
            };host.frame(0)
            fun focusedYear(node:Any):Int? {
                val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val children=node.javaClass.methods.single {it.name.startsWith("getChildren$")&&it.parameterCount==0}.invoke(node) as List<*>
                if(own?.getOrNull(SemanticsProperties.Focused)==true) {
                    // Year semantics contain the localized navigation description, not just digits.
                    return own.getOrNull(SemanticsProperties.Text)?.firstOrNull()?.text?.let {Regex("[0-9]{4}").find(it)?.value?.toIntOrNull()}
                }
                return children.firstNotNullOfOrNull {focusedYear(checkNotNull(it))}
            }
            fun sample(id:String,image:Boolean=true) {
                fun day(millis:Long?)=if(millis==null)-1.0 else (millis/86400000+719162).toDouble()
                val a=if(c.range)range.selectedStartDateMillis else single.selectedDateMillis
                val b=if(c.range)range.selectedEndDateMillis else null
                val keyboard=if(c.action=="year-keys")listOf(
                    number("focused-year",(focusedYear(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))))?.toDouble()?:-1.0,"year",definition="date-picker.focused-year"),
                    flag("after-focused",afterFocused,definition="date-picker.after-focused"))else emptyList()
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("picker","date-picker",null,probe.geometry().filter{it.name=="box"||it.name=="size"}+listOf(
                    number("month",day(if(c.range)range.displayedMonthMillis else single.displayedMonthMillis),"day",definition="date-picker.displayed-month-day-number"),number("start",day(a),"day",definition="date-picker.selected-start-day-number"),number("end",day(b),"day",definition="date-picker.selected-end-day-number"))+keyboard)),image))
            }
            sample("initial")
            if(c.action=="year-keys") {
                for(t in 16000L..512000L step 16000L)host.frame(t)
                val origin=checkNotNull(probe.coordinates).localToRoot(Offset.Zero)/d
                val width=checkNotNull(probe.coordinates).size.width/d
                val point=origin+Offset(if(c.rtl)width-60 else 60f,148f)
                host.advanceTo(600000);host.input("down",point);host.frame(600000)
                host.advanceTo(616000);host.input("up",point);host.frame(616000)
                for(t in 632000L..1112000L step 16000L)host.frame(t)
                sample("years-open")
                // Keep the automatic-entry observation above, then explicitly focus the real year
                // through its accessibility action to exercise the grid's own key handlers.
                fun focusYear(node:Any):Boolean {
                    val semantics=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                    if(semantics?.getOrNull(SemanticsProperties.Selected)==true &&
                        semantics.getOrNull(SemanticsProperties.Text)?.any {it.text.contains("2024")}==true)
                        return semantics.getOrNull(SemanticsActions.RequestFocus)?.action?.invoke()==true
                    val children=node.javaClass.methods.single {it.name.startsWith("getChildren$")&&it.parameterCount==0}.invoke(node) as List<*>
                    return children.any {focusYear(checkNotNull(it))}
                }
                host.advanceTo(1152000)
                check(focusYear(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))))
                writer.record(1152000,"input","RequestFocus on selected year semantics")
                host.draw();host.frame(1152000);sample("year-focused")
                fun key(time:Long,key:Key,shift:Boolean=false) {
                    host.advanceTo(time);host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown,isShiftPressed=shift));host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp,isShiftPressed=shift));host.scheduler.runCurrent();host.draw();host.frame(time);sample("key-$time")
                }
                key(1200000,if(c.rtl)Key.DirectionLeft else Key.DirectionRight)
                key(1300000,Key.Tab,true);key(1400000,Key.Tab);key(1500000,Key.Tab);key(1600000,Key.Tab,true)
                key(1700000,if(c.rtl)Key.DirectionLeft else Key.DirectionRight);key(1800000,Key.Enter)
                for(t in 1816000L..2312000L step 16000L)host.frame(t)
                sample("year-selected")
            } else             if(c.action.startsWith("month-")) {
                for(t in 16000L..512000L step 16000L)host.frame(t)
                sample("calendar-ready")
                val origin=checkNotNull(probe.coordinates).localToRoot(Offset.Zero)/d
                val width=checkNotNull(probe.coordinates).size.width/d
                for(t in 0L..640000L step 16000L) {
                    host.advanceTo(600000+t)
                    if(c.action.contains("touch")) {
                        var distance=when(t){0L->0;16000L->10;32000L->36;48000L->80;64000L->120;else->-1}
                        val release=if(c.action.endsWith("slow"))144000L else 80000L
                        if(distance>=0 || t==release) {
                            if(t==release)distance=120
                            val point=(origin+Offset(200f+(if(c.rtl)distance else -distance),300f))*d
                            val type=if(t==0L)PointerEventType.Press else if(t==release)PointerEventType.Release else PointerEventType.Move
                            host.scene.sendPointerEvent(type,point,timeMillis=1600+t/1000,type=PointerType.Touch)
                            host.scheduler.runCurrent()
                        }
                    } else if(t==0L) {
                        val end=if(c.action=="month-next")36f else 84f
                        val point=origin+Offset(if(c.rtl)end else width-end,148f)
                        host.input("down",point);host.input("up",point)
                    }
                    host.frame(600000+t)
                    if(t in listOf(0L,32000L,64000L,96000L,128000L,256000L,512000L,640000L))sample("month-$t")
                }
            } else if(c.action=="years") {
                for(t in 16000L..512000L step 16000L)host.frame(t)
                sample("calendar-ready")
                // The navigation row sits below the 120-DIP header; click inside the real menu button.
                val origin=checkNotNull(probe.coordinates).localToRoot(Offset.Zero)/ComposeHost.RenderScale.toFloat()
                val point=origin+Offset(60f,148f)
                host.advanceTo(600000);host.input("down",point);host.input("up",point);host.draw();host.frame(600000);sample("year-start")
                for(t in 616000L..1112000L step 16000L) {
                    host.frame(t);if(t==664000L||t==856000L)sample("year-enter-$t")
                }
                sample("year-settled")
                host.advanceTo(1200000);host.input("down",point);host.input("up",point);host.draw();host.frame(1200000);sample("calendar-start")
                for(t in 1216000L..1712000L step 16000L) {
                    host.frame(t);if(t==1264000L||t==1456000L)sample("year-exit-$t")
                }
                sample("calendar-settled");host.advanceTo(1800000);sample("idle",false)
            } else if(dynamic) {
                host.advanceTo(1000);if(c.range)range.displayMode=DisplayMode.Input else single.displayMode=DisplayMode.Input;writer.record(1000,"mutation","input-mode");host.draw();host.frame(1000);sample("input-start")
                for(t in 16000L..512000L step 16000L)host.frame(t);sample("input-settled")
                host.advanceTo(600000);if(c.range)range.setSelection(1709164800000L,null) else single.selectedDateMillis=1709164800000L;writer.record(600000,"mutation","leap-day");host.draw();host.frame(600000);sample("leap-day")
                host.advanceTo(700000);if(c.range)range.displayMode=DisplayMode.Picker else single.displayMode=DisplayMode.Picker;writer.record(700000,"mutation","calendar-mode");host.draw();host.frame(700000);sample("calendar-start")
                for(t in 716000L..1212000L step 16000L)host.frame(t);sample("calendar-settled");host.advanceTo(1300000);sample("idle",false)
            }
        }
    }
}

@ParitySuite("date-month-scroll")
class DateMonthScrollCases {
    @ParityScenario("scroll") suspend fun scroll(writer:CaptureWriter)=DatePickerCases().capture(writer,true)
}

@ParitySuite("date-keyboard")
class DateKeyboardCases {
    @ParityScenario("keys") suspend fun keys(writer:CaptureWriter)=DatePickerCases().capture(writer,true)
}

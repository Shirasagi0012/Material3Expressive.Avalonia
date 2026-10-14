@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable
import java.util.Locale

@Serializable data class DatePickerConfig(val range:Boolean=false,val input:Boolean=false,val selected:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,val culture:String="en-US",val width:Double=400.0,val action:String="none")

@ParitySuite("date-picker")
class DatePickerCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("changes") suspend fun changes(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val c=writer.readConfig<DatePickerConfig>()
        // Explicit locale avoids JVM-global state leaking between cases.
        val locale=Locale.forLanguageTag(c.culture)
        val start=if(c.selected)1707523200000L else null // 2024-02-10 UTC
        val end=if(c.selected)1708387200000L else null
        val single=DatePickerState(locale=locale,initialSelectedDateMillis=start,initialDisplayedMonthMillis=1706745600000L,yearRange=2023..2025,initialDisplayMode=if(c.input)DisplayMode.Input else DisplayMode.Picker)
        val range=DateRangePickerState(locale=locale,initialSelectedStartDateMillis=start,initialSelectedEndDateMillis=end,initialDisplayedMonthMillis=1706745600000L,yearRange=2023..2025,initialDisplayMode=if(c.input)DisplayMode.Input else DisplayMode.Picker)
        val probe=Probe()
        ComposeHost(writer,440,680).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                            Box(Modifier.width(c.width.toFloat().dp)) {
                                if(c.range)DateRangePicker(range,probe.modifier()) else DatePicker(single,probe.modifier())
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String,image:Boolean=true) {
                fun day(millis:Long?)=if(millis==null)-1.0 else (millis/86400000+719162).toDouble()
                val a=if(c.range)range.selectedStartDateMillis else single.selectedDateMillis
                val b=if(c.range)range.selectedEndDateMillis else null
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("picker","date-picker",null,probe.geometry().filter{it.name=="box"||it.name=="size"}+listOf(
                    number("start",day(a),"day",definition="date-picker.selected-start-day-number"),number("end",day(b),"day",definition="date-picker.selected-end-day-number")))),image))
            }
            sample("initial")
            if(c.action=="years") {
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

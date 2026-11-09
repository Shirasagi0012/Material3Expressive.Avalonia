@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable
import java.util.Locale
import java.time.Instant
import java.time.ZoneOffset

@Serializable
data class DateFormatterConfig(val range:Boolean=false,val input:Boolean=false,val selected:Boolean=true,
    val nullResult:Boolean=false,val culture:String="en-US",val dynamic:Boolean=false)

@ParitySuite("date-formatter")
class DateFormatterCases {
    @ParityScenario("capture") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<DateFormatterConfig>()
        val previous=Locale.getDefault()
        val locale=Locale.forLanguageTag(c.culture)
        Locale.setDefault(locale)
        try {
            val start=if(c.selected)1707523200000L else null
            val end=if(c.selected)1708387200000L else null
            val mode=if(c.input)DisplayMode.Input else DisplayMode.Picker
            val single=DatePickerState(locale,start,1706745600000L,2024..2024,mode)
            val range=DateRangePickerState(locale,start,end,1706745600000L,2024..2024,mode)
            val probe=Probe()
            var formatter by mutableStateOf<DatePickerFormatter>(Formatter("F",c.nullResult))
            ComposeHost(writer,440,680).use { host ->
                host.mount {
                    val font=remember{CaseDefaults.font(writer)}
                    MaterialTheme(colorScheme=lightColorScheme(),typography=Typography(fontFamily=font)) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                            Box(Modifier.width(400.dp)) {
                                if(c.range)DateRangePicker(range,probe.modifier(),dateFormatter=formatter)
                                else DatePicker(single,probe.modifier(),dateFormatter=formatter)
                            }
                        }
                    }
                };host.frame(0)
                fun semantics(node:Any):List<SemanticsConfiguration> {
                    val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                    val children=node.javaClass.methods.single{it.name.startsWith("getChildren$")&&it.parameterCount==0}.invoke(node) as List<*>
                    return listOfNotNull(own)+children.flatMap{semantics(checkNotNull(it))}
                }
                fun sample(id:String) {
                    val all=semantics(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode")))
                    val headline=all.first{it.getOrNull(SemanticsProperties.LiveRegion)==LiveRegionMode.Polite && it.contains(SemanticsProperties.ContentDescription)}
                    fun day(value:Long?)=if(value==null)-1.0 else (value/86400000+719162).toDouble()
                    writer.submit(host.snapshot(id,id,listOf(ProbePoint("picker","picker",null,listOf(
                        text("headlineDescription",headline[SemanticsProperties.ContentDescription].joinToString(", "),definition="date-picker.headline-description"),
                        number("start",day(if(c.range)range.selectedStartDateMillis else single.selectedDateMillis),"day",definition="date-picker.selected-start-day-number"),
                        number("end",day(if(c.range)range.selectedEndDateMillis else null),"day",definition="date-picker.selected-end-day-number"))))))
                }
                sample("initial")
                for(time in 16000L..512000L step 16000L)host.frame(time)
                sample("settled")
                if(c.dynamic) {
                    fun change(time:Long,id:String,value:DatePickerFormatter){host.advanceTo(time);formatter=value;host.frame(time);sample(id)}
                    change(528000,"replacement",Formatter("G",false))
                    change(544000,"null-results",Formatter("G",true))
                    change(560000,"restored",Formatter("F",false))
                }
            }
        } finally {Locale.setDefault(previous)}
    }
    private class Formatter(val prefix:String,val empty:Boolean):DatePickerFormatter {
        override fun formatMonthYear(monthMillis:Long?,locale:CalendarLocale):String? =
            if(empty || monthMillis==null)null else "$prefix M:${Instant.ofEpochMilli(monthMillis).atOffset(ZoneOffset.UTC).toLocalDate().toString().take(7)}"
        override fun formatDate(dateMillis:Long?,locale:CalendarLocale,forContentDescription:Boolean):String? =
            if(empty || dateMillis==null)null else "$prefix ${if(forContentDescription)"D" else "V"}:${Instant.ofEpochMilli(dateMillis).atOffset(ZoneOffset.UTC).toLocalDate()}"
    }
}

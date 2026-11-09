@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class,androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable
import java.time.LocalDate
import java.util.Locale

@Serializable data class DateRangeNavigationConfig(val month:Int=2,val rtl:Boolean=false,val dark:Boolean=false,val culture:String="en-US")

@ParitySuite("date-range-navigation")
class DateRangeNavigationCases {
    @ParityScenario("navigate")
    suspend fun navigate(writer:CaptureWriter) {
        val c=writer.readConfig<DateRangeNavigationConfig>()
        // CalendarModel uses state.locale; Material action strings use the platform
        // locale. Supply the same requested language to both and restore the JVM host.
        val previousLocale=Locale.getDefault()
        Locale.setDefault(Locale.forLanguageTag(c.culture))
        try {
        fun millis(month:Int)=LocalDate.of(2024,month,1).toEpochDay()*86400000
        val state=DateRangePickerState(locale=Locale.forLanguageTag(c.culture), initialSelectedStartDateMillis=1707523200000L,
            initialSelectedEndDateMillis=1708387200000L,initialDisplayedMonthMillis=millis(c.month),yearRange=2024..2024)
        val probe=Probe()
        ComposeHost(writer,440,680).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                            DateRangePicker(state,probe.modifier().width(400.dp))
                        }
                    }
                }
            }; host.frame(0)
            for(t in 16000L..512000L step 16000L)host.frame(t)
            fun actions(node:Any):List<CustomAccessibilityAction>? {
                val semantics=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val own=semantics?.getOrNull(SemanticsActions.CustomActions)
                if(own?.size==2)return own
                val children=node.javaClass.methods.single{it.name.startsWith("getChildren$")&&it.parameterCount==0}.invoke(node) as List<*>
                return children.firstNotNullOfOrNull{actions(checkNotNull(it))}
            }
            fun currentActions()=checkNotNull(actions(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))))
            var accepted=false
            fun sample(id:String) {
                val actions=currentActions()
                fun day(millis:Long?)=(checkNotNull(millis)/86400000+719162).toDouble()
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("calendar","date-range-calendar",null,listOf(
                    number("month",day(state.displayedMonthMillis),"day",definition="date-picker.displayed-month-day-number"),
                    number("start",day(state.selectedStartDateMillis),"day",definition="date-picker.selected-start-day-number"),
                    number("end",day(state.selectedEndDateMillis),"day",definition="date-picker.selected-end-day-number"),
                    text("previous-action",actions[0].label,definition="date-range.previous-action-name"),
                    text("next-action",actions[1].label,definition="date-range.next-action-name"),
                    flag("accepted",accepted,definition="date-range.month-action-accepted")
                )))))
            }
            sample("initial")
            fun action(time:Long,next:Boolean) {
                host.advanceTo(time);accepted=currentActions()[if(next)1 else 0].action()
                writer.record(time,"action",if(next)"next month" else "previous month")
                host.scheduler.runCurrent();host.draw();host.frame(time);host.frame(time+16000)
                sample(if(next)"next-$time" else "previous-$time")
            }
            action(600000,false);action(800000,true);action(1000000,true)
            host.advanceTo(1200000);state.displayedMonthMillis=millis(7)
            writer.record(1200000,"mutation","displayed July without rebuilding calendar")
            host.draw();host.frame(1200000);host.frame(1216000);sample("external-month")
            action(1400000,false)
        }
        } finally { Locale.setDefault(previousLocale) }
    }
}

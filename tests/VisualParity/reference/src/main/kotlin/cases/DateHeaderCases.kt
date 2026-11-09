@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
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
import java.util.Locale
import java.time.Instant
import java.time.ZoneOffset

@Serializable data class DateHeaderConfig(val range:Boolean=false,val input:Boolean=false,val selection:String="both",
    val culture:String="en-US",val rtl:Boolean=false,val width:Int=360,val dynamic:Boolean=false,val defaultFormatter:Boolean=false)

@ParitySuite("date-header")
class DateHeaderCases {
    @ParityScenario("capture") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<DateHeaderConfig>()
        val previous=Locale.getDefault()
        var input by mutableStateOf(c.input)
        var selection by mutableStateOf(c.selection)
        var formatter by mutableStateOf<DatePickerFormatter>(Formatter("F",c.selection))
        var culture by mutableStateOf(c.culture)
        val probe=Probe()
        try {
            Locale.setDefault(Locale.forLanguageTag(c.culture))
            ComposeHost(writer,400,200).use { host ->
                host.mount {
                    val font=remember{CaseDefaults.font(writer)}
                    MaterialTheme(colorScheme=lightColorScheme(),typography=Typography(fontFamily=font)) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                            CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                                // Recreate only the content's locale read when the platform locale changes.
                                key(culture) {
                                    Column(Modifier.width(c.width.dp).then(probe.modifier()),verticalArrangement=Arrangement.spacedBy(8.dp)) {
                                        val mode=if(input)DisplayMode.Input else DisplayMode.Picker
                                        val start=if(selection=="none")null else 1707523200000L
                                        val end=if(selection=="none" || selection=="start")null else 1708387200000L
                                        val actualFormatter=if(c.defaultFormatter)DatePickerDefaults.dateFormatter() else formatter
                                        ProvideTextStyle(MaterialTheme.typography.labelLarge) {
                                            if(c.range)DateRangePickerDefaults.DateRangePickerTitle(mode)
                                            else DatePickerDefaults.DatePickerTitle(mode)
                                        }
                                        ProvideTextStyle(if(c.range)MaterialTheme.typography.titleLarge else MaterialTheme.typography.headlineMedium) {
                                            if(c.range)DateRangePickerDefaults.DateRangePickerHeadline(start,end,mode,actualFormatter)
                                            else DatePickerDefaults.DatePickerHeadline(start,mode,actualFormatter)
                                        }
                                    }
                                }
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
                    val coordinates=checkNotNull(probe.coordinates)
                    val all=semantics(checkNotNull(field(coordinates,"layoutNode")))
                    val strings=all.mapNotNull{it.getOrNull(SemanticsProperties.Text)?.joinToString(""){value->value.text}}
                    val headline=all.first{it.getOrNull(SemanticsProperties.LiveRegion)==LiveRegionMode.Polite}
                    writer.submit(host.snapshot(id,id,listOf(ProbePoint("header","date-header",null,listOf(
                        text("title",strings.first()),text("headline",strings.drop(1).joinToString("|")),
                        text("description",headline[SemanticsProperties.ContentDescription].joinToString(", ")),
                        flag("polite",true),number("width",coordinates.size.width/ComposeHost.RenderScale.toDouble(),"dip"),
                        number("height",coordinates.size.height/ComposeHost.RenderScale.toDouble(),"dip"))))))
                }
                sample("initial")
                if(c.dynamic) {
                    data class Change(val time:Long,val input:Boolean,val selection:String,val culture:String,val prefix:String)
                    for(change in listOf(Change(16000,true,"none","en-US","F"),Change(32000,false,"start","de-DE","G"),
                        Change(48000,true,"empty","de-DE","G"),Change(64000,false,"both","en-US","F"))) {
                        host.advanceTo(change.time);Locale.setDefault(Locale.forLanguageTag(change.culture));culture=change.culture
                        input=change.input;selection=change.selection;formatter=Formatter(change.prefix,change.selection)
                        host.frame(change.time);sample("time-${change.time}")
                    }
                }
            }
        } finally {Locale.setDefault(previous)}
    }
    private class Formatter(val prefix:String,val selection:String):DatePickerFormatter {
        override fun formatMonthYear(monthMillis:Long?,locale:CalendarLocale):String?=null
        override fun formatDate(dateMillis:Long?,locale:CalendarLocale,forContentDescription:Boolean):String? =
            if(dateMillis==null || selection=="null")null else if(selection=="empty")"" else
                "$prefix${if(forContentDescription)"D" else "V"}${Instant.ofEpochMilli(dateMillis).atOffset(ZoneOffset.UTC).dayOfMonth}"
    }
}

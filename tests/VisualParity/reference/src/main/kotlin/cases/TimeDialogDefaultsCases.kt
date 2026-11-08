@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class TimeDialogDefaultsConfig(val kind:String="clock",val mode:String="picker",val rtl:Boolean=false,
    val dark:Boolean=false,val culture:String="en-US",val action:String="none",val platformAmbient:Boolean=false)

@ParitySuite("time-dialog-defaults")
class TimeDialogDefaultsCases {
    @ParityScenario("content") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimeDialogDefaultsConfig>()
        val previousLocale=java.util.Locale.getDefault()
        try {
            java.util.Locale.setDefault(java.util.Locale.forLanguageTag(c.culture))
            ComposeHost(writer,320,240).use { host ->
                val probe=Probe();val focus=FocusRequester();var clicks=0
                var mode by mutableStateOf(when(c.mode){"input"->TimePickerDisplayMode.Input;"scroll"->TimePickerDisplayMode.Scroll;else->TimePickerDisplayMode.Picker})
                host.mount {
                    val family=remember { CaseDefaults.font(writer) }
                    MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive(),typography=Typography(fontFamily=family)) {
                        Box(Modifier.fillMaxSize().background(Color.White)) {
                            Box(Modifier.offset(60.dp,100.dp)) {
                                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                                    if(c.kind=="title") TimePickerDialogDefaults.Title(mode,probe.modifier().width(200.dp))
                                    else Box(probe.modifier()) {
                                        if(c.kind=="scroll") TimePickerDialogDefaults.ScrollDisplayModeToggle({clicks++},mode,Modifier.focusRequester(focus))
                                        else TimePickerDialogDefaults.DisplayModeToggle({clicks++},mode,Modifier.focusRequester(focus))
                                    }
                                }
                            }
                        }
                    }
                };host.frame(-1000);host.frame(0)
                fun semantics(node:Any):List<SemanticsConfiguration> {
                    val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                    val children=node.javaClass.methods.single {it.name.startsWith("getChildren$")&&it.parameterCount==0}.invoke(node) as List<*>
                    return listOfNotNull(own)+children.flatMap {semantics(checkNotNull(it))}
                }
                var next=16000L
                fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
                fun sample(id:String) {
                    val all=semantics(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode")))
                    val label=if(c.kind=="title")all.firstNotNullOf{it.getOrNull(SemanticsProperties.Text)?.firstOrNull()?.text}
                        else all.firstNotNullOf{it.getOrNull(SemanticsProperties.ContentDescription)?.firstOrNull()}
                    val typography=if(c.kind=="title") {
                        val results=mutableListOf<androidx.compose.ui.text.TextLayoutResult>()
                        checkNotNull(all.first{it.contains(SemanticsActions.GetTextLayoutResult)}[SemanticsActions.GetTextLayoutResult].action).invoke(results)
                        val style=results.single().layoutInput.style
                        listOf(number("fontSize",style.fontSize.value.toDouble(),"sp",category="text",definition="time-dialog-defaults.title-font-size"),
                            number("fontWeight",style.fontWeight!!.weight.toDouble(),"weight",category="text",definition="time-dialog-defaults.title-font-weight"),
                            number("lineHeight",style.lineHeight.value.toDouble(),"sp",category="text",definition="time-dialog-defaults.title-line-height"),
                            number("letterSpacing",style.letterSpacing.value.toDouble(),"sp",category="text",definition="time-dialog-defaults.title-letter-spacing"))
                    } else emptyList()
                    writer.submit(host.snapshot(id,id,listOf(ProbePoint("control","control",null,probe.geometry().filter{it.name=="box"||it.name=="size"}+listOf(
                        text("label",label,definition="time-dialog-defaults.actual-label"),number("clicks",clicks.toDouble(),"count",definition="time-dialog-defaults.click-count"))+typography))))
                }
                fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
                fun times(vararg values:Long){values.forEach{advance(it);host.draw();sample("time-$it")}}
                sample("initial")
                when(c.action) {
                    "mode"->{change(1000,"input"){mode=TimePickerDisplayMode.Input};times(16000);change(17000,"scroll"){mode=TimePickerDisplayMode.Scroll};times(32000);change(33000,"picker"){mode=TimePickerDisplayMode.Picker};times(48000)}
                    "focus"->{change(1000,"focus"){focus.requestFocus()};times(16000,64000,224000,400000);change(401000,"escape"){host.scene.sendKeyEvent(KeyEvent(Key.Escape,KeyEventType.KeyDown))};times(416000,512000,800000)}
                    "hover","reverse"->{
                        change(1000,"enter"){host.input("move",Offset(84f,124f))};times(16000,64000)
                        if(c.action=="reverse"){change(65000,"exit"){host.input("move",Offset(300f,220f))};times(80000);change(81000,"enter-again"){host.input("move",Offset(84f,124f))}}
                        times(224000,400000);change(401000,"exit-final"){host.input("move",Offset(300f,220f))};times(416000,512000,800000)
                    }
                    "press"->{change(1000,"press"){host.input("move",Offset(84f,124f));host.input("down",Offset(84f,124f))};times(16000,64000,160000);change(161000,"release"){host.input("up",Offset(84f,124f))};times(176000,400000)}
                }
            }
        } finally { java.util.Locale.setDefault(previousLocale) }
    }
}

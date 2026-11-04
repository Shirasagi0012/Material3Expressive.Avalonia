@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ColorProducer
import androidx.compose.ui.graphics.isSpecified
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable
import kotlin.math.roundToInt

@Serializable data class TimeScrollConfig(val clock24:Boolean=true,val toggle:Boolean=false,val dark:Boolean=false,
    val rtl:Boolean=false,val action:String="none")

@ParitySuite("time-scroll")
class TimeScrollCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimeScrollConfig>()
        val state=TimePickerState(8,30,c.clock24)
        val probe=Probe()
        var dark by mutableStateOf(c.dark)
        var attached by mutableStateOf(true)
        ComposeHost(writer,420,240).use { host ->
            host.mount {
                val family=remember { CaseDefaults.font(writer) }
                // Typography() resolves to the legacy 15-role constructor, which copies
                // displayLarge into displayLargeEmphasized. The font-family constructor
                // consumes the actual 30-role tokens, matching the library's type scale.
                val type=Typography(fontFamily=family)
                MaterialTheme(colorScheme=if(dark)darkColorScheme()else lightColorScheme(),typography=type,motionScheme=MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            if(attached) {
                                if(c.toggle)TimeScroll(state,TimePickerDefaults.shapes(),
                                    {Box(Modifier.size(24.dp).background(Color.Magenta))},probe.modifier())
                                else TimeScroll(state,TimePickerDefaults.shapes(),probe.modifier())
                            }
                        }
                    }
                }
            };host.frame(0)
            // Read actual Text nodes consumed by drawing, not requested selection or expected styles.
            fun itemNodes(layout:Any):List<Any> {
                val head=checkNotNull(field(checkNotNull(field(layout,"nodes")),"head"))
                val own=generateSequence(head){field(it,"child")}.filter {
                    it.javaClass.simpleName in listOf("TextAnnotatedStringNode","TextStringSimpleNode")
                }.toList()
                val children=layout.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(layout) as List<*>
                return own+children.flatMap { itemNodes(checkNotNull(it)) }
            }
            fun sample(id:String) {
                val present=probe.coordinates?.isAttached==true
                val points=mutableListOf(ProbePoint("picker","time-scroll",null,listOf(
                    flag("attached",present,definition="time-scroll.attached"),
                    number("hour",state.hour.toDouble(),"hour",definition="time-picker.selected-hour"),
                    number("minute",state.minute.toDouble(),"minute",definition="time-picker.selected-minute"))))
                if(present) {
                    points.add(ProbePoint("layout","time-scroll-layout","picker",probe.geometry()))
                    itemNodes(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))).forEach { node ->
                        val value=when(val raw=field(node,"text")){is AnnotatedString->raw.text;is String->raw;else->""}
                        if(value in listOf("08","09")) {
                            val style=field(node,"style") as TextStyle
                            val override=(field(node,"overrideColor") as ColorProducer?)?.invoke() ?: Color.Unspecified
                            val color=if(override.isSpecified)override else style.color
                            points.add(ProbePoint("hour-${value.toInt()}","scroll-item","picker",listOf(
                                number("fontSize",style.fontSize.value.toDouble(),"sp",category="text",definition="time-scroll.item.font-size"),
                                number("fontWeight",(style.fontWeight?.weight?:400).toDouble(),"weight",category="text",definition="time-scroll.item.font-weight"),
                                vector("color","color",listOf(color.red,color.green,color.blue,color.alpha).map{(it*255).roundToInt().toDouble()},"rgba8","none","properties",definition="time-scroll.item.color"))))
                        }
                    }
                }
                writer.submit(host.snapshot(id,id,points))
            }
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();sample(id)}
            fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            sample("initial")
            if(c.action=="none")return
            change(1000,"select-nine"){state.hour=9}
            at(16000,32000,64000)
            if(c.action=="reverse")change(65000,"reverse-eight"){state.hour=8}
            if(c.action=="theme")change(65000,"dark-theme"){dark=true}
            if(c.action=="detach") {
                change(65000,"detach"){attached=false};at(80000)
                change(81000,"reattach"){attached=true}
            }
            at(96000,160000,320000,640000)
        }
    }
}

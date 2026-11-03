@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import androidx.compose.ui.semantics.*
import kotlinx.serialization.Serializable

@Serializable data class TimePickerPointerConfig(val action:String="tap",val clock24:Boolean=true,val rtl:Boolean=false,val expressive:Boolean=true,val fixedInputs:Boolean=true,val vibrant:Boolean=false,val horizontal:Boolean=false)

@ParitySuite("time-picker-pointer")
class TimePickerPointerCases {
    @ParityScenario("pointer") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimePickerPointerConfig>()
        val state=TimePickerState(0,if(c.action=="field-transition")30 else 0,c.clock24)
        if(c.action.startsWith("minute"))state.selection=TimePickerSelectionMode.Minute
        val probe=Probe()
        val empty=PlatformContext.Empty()
        val desktop=object:PlatformContext by empty {
            override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
        ComposeHost(writer,if(c.horizontal)700 else 360,480,desktop).use { host ->
            host.mount {
                val family=remember { CaseDefaults.font(writer) }
                val typography=Typography().let { if(c.fixedInputs) it.copy(
                    displayLarge=it.displayLarge.copy(fontFamily=family),
                    bodyLarge=it.bodyLarge.copy(fontFamily=family),
                    titleMedium=it.titleMedium.copy(fontFamily=family)) else it }
                MaterialTheme(colorScheme=lightColorScheme(),typography=typography,motionScheme=if(c.expressive)MotionScheme.expressive()else MotionScheme.standard()) {
                    Column(Modifier.fillMaxSize().background(Color.White)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            val layout=if(c.horizontal)TimePickerLayoutType.Horizontal else TimePickerLayoutType.Vertical
                            if(c.vibrant)TimePicker(state,TimePickerDefaults.shapes(),probe.modifier().padding(16.dp),layoutType=layout)
                            else TimePicker(state,probe.modifier().padding(16.dp),layoutType=layout)
                        }
                    }
                }
            };host.frame(0)
            fun findDial(layout:Any):Any? {
                val head=checkNotNull(field(checkNotNull(field(layout,"nodes")),"head"))
                generateSequence(head){field(it,"child")}.firstOrNull { it.javaClass.simpleName=="ClockDialNode" }?.let{return it}
                val children=layout.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(layout) as List<*>
                return children.firstNotNullOfOrNull { findDial(checkNotNull(it)) }
            }
            val dial=checkNotNull(findDial(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))))
            val coordinates=checkNotNull(field(dial,"coordinator")) as LayoutCoordinates
            val analog=checkNotNull(field(dial,"state"))
            fun labels(node:Any):List<SemanticsConfiguration> {
                val semantics=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val own=if(semantics?.contains(SemanticsProperties.TraversalIndex)==true)listOf(semantics)else emptyList()
                val children=node.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(node) as List<*>
                return own+children.flatMap { labels(checkNotNull(it)) }
            }
            fun sample(id:String) {
                val labels=labels(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode")))
                val selected=labels.filter { it.getOrElse(SemanticsProperties.Selected){false} }.map { it[SemanticsProperties.TraversalIndex].toInt() }.sorted().joinToString(",")
                val angle=(analog.javaClass.methods.single{it.name=="getCurrentAngle"&&it.parameterCount==0}.invoke(analog) as Number).toDouble()
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("picker","time-picker",null,listOf(
                    number("hour",state.hour.toDouble(),"hour",definition="time-picker.selected-hour"),
                    number("minute",state.minute.toDouble(),"minute",definition="time-picker.selected-minute"),
                    text("selection",if(state.selection==TimePickerSelectionMode.Hour)"hour" else "minute",definition="time-picker.selection"),
                    number("angle",angle,"radian",category="motion",definition="time-picker.selector.current-angle"),
                    number("labelCount",labels.size.toDouble(),"count",definition="time-picker.composed-label-count"),
                    text("selectedTicks",selected,definition="time-picker.selector.selected-ticks"))))))
            }
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun input(time:Long,action:String,x:Float,y:Float){advance(time);host.input(action,coordinates.localToWindow(Offset(x,y)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat());host.draw();sample("$action-$time")}
            fun times(vararg values:Long){values.forEach{advance(it);host.draw();sample("time-$it")}}
            sample("initial")
            if(c.action=="period") {
                fun toggles(layout:Any):List<Any> {
                    val head=checkNotNull(field(checkNotNull(field(layout,"nodes")),"head"))
                    val own=generateSequence(head){field(it,"child")}.filter { it.javaClass.simpleName=="ToggleableNode" }.toList()
                    val children=layout.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(layout) as List<*>
                    return own+children.flatMap { toggles(checkNotNull(it)) }
                }
                val pm=checkNotNull(field(toggles(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))).last(),"coordinator")) as LayoutCoordinates
                fun periodInput(time:Long,action:String) {
                    advance(time);host.input(action,pm.localToWindow(Offset(pm.size.width/2f,pm.size.height/2f))/ComposeHost.RenderScale.toFloat())
                    host.draw();sample("$action-$time")
                }
                periodInput(33000,"down");times(64000);periodInput(97000,"up")
                times(112000,160000,208000,320000,640000)
                periodInput(657000,"down");periodInput(689000,"up");times(800000,1024000)
                return
            }
            if(c.action in listOf("reject","field-transition")) {advance(1000);state.hour=0;state.minute=30;state.selection=TimePickerSelectionMode.Minute;host.draw();times(16000,32000)}
            input(33000,"down",229f,128f)
            if(c.action in listOf("drag","minute-drag","rings","release-jump")) {
                input(49000,"move",if(c.action=="rings")197f else 228f,if(c.action=="rings")128f else 140f)
                times(64000)
                if(c.action=="rings")input(81000,"move",229f,128f)else input(81000,"move",200f,200f)
            }
            val stationaryRelease=c.action in listOf("drag","minute-drag")
            input(97000,"up",if(stationaryRelease)200f else 229f,if(stationaryRelease)200f else 128f);times(112000,160000)
            if(c.action=="interrupt"){input(177000,"down",128f,229f);input(193000,"up",128f,229f)}
            if(c.action=="selection-reverse") {
                advance(177000);state.selection=TimePickerSelectionMode.Minute;host.draw();sample("select-minute");times(208000)
                advance(225000);state.selection=TimePickerSelectionMode.Hour;host.draw();sample("select-hour")
            }else times(208000)
            times(320000,480000,640000,800000,1024000)
        }
    }
}

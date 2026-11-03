@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class TimeSideControlConfig(val kind:String="input",val clock24:Boolean=false,
    val rtl:Boolean=false,val dark:Boolean=false,val templateOnly:Boolean=false,val action:String="none")

@ParitySuite("time-picker-side-control")
class TimeSideControlCases {
    @ParityScenario("side-control") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimeSideControlConfig>()
        val state=TimePickerState(2,30,c.clock24)
        val picker=Probe();val mode=Probe()
        var showToggle by mutableStateOf(true)
        ComposeHost(writer,420,260).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            val shapes=TimePickerDefaults.shapes()
                            val toggle:@Composable ()->Unit={Box(Modifier.fillMaxSize(),contentAlignment=Alignment.Center) {
                                Box(mode.modifier().size(24.dp).background(Color.Magenta))
                            }}
                            if(c.kind=="input") {
                                if(showToggle)TimeInput(state,shapes,toggle,picker.modifier())
                                else TimeInput(state,shapes,picker.modifier())
                            } else {
                                if(showToggle)TimeScroll(state,shapes,toggle,picker.modifier())
                                else TimeScroll(state,shapes,picker.modifier())
                            }
                        }
                    }
                }
            };host.frame(0)
            // The fixed Core ToggleableNode shares its layout node with the actual button background.
            fun toggles(layout:Any):List<Any> {
                val head=checkNotNull(field(checkNotNull(field(layout,"nodes")),"head"))
                val own=generateSequence(head){field(it,"child")}.filter { it.javaClass.simpleName=="ToggleableNode" }.toList()
                val children=layout.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(layout) as List<*>
                return own+children.flatMap { toggles(checkNotNull(it)) }
            }
            fun periodNodes()=toggles(checkNotNull(field(checkNotNull(picker.coordinates),"layoutNode")))
            fun coordinates(node:Any)=checkNotNull(field(node,"coordinator")) as LayoutCoordinates
            fun sample(id:String) {
                val points=mutableListOf(ProbePoint("picker","time-picker",null,listOf(
                    number("hour",state.hour.toDouble(),"hour",definition="time-picker.selected-hour"),
                    number("minute",state.minute.toDouble(),"minute",definition="time-picker.selected-minute"))))
                // The requested value can precede composition. Observe the attached content;
                // a removed mode's old coordinates must never be read as a live layout.
                if(mode.coordinates?.isAttached==true)points.add(ProbePoint("mode","mode-toggle","picker",mode.geometry()))
                periodNodes().forEachIndexed { index,node ->
                    val coords=coordinates(node)
                    val semantics=checkNotNull(field(checkNotNull(field(coords,"layoutNode")),"_semanticsConfiguration")) as SemanticsConfiguration
                    points.add(ProbePoint(if(index==0)"am" else "pm","period","picker",picker.geometry(coords)+
                        flag("selected",semantics[SemanticsProperties.Selected],definition="time-picker.period.selected")))
                }
                writer.submit(host.snapshot(id,id,points))
            }
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun times(vararg values:Long){values.forEach{advance(it);host.draw();sample("time-$it")}}
            fun input(time:Long,action:String) {
                advance(time)
                val coords=coordinates(periodNodes().last())
                val point=Offset(if(c.action.endsWith("edge"))-3f*ComposeHost.RenderScale else coords.size.width/2f,coords.size.height/2f)
                val position=coords.localToWindow(point)
                if(c.action.startsWith("touch-")) {
                    host.scene.sendPointerEvent(if(action=="down")PointerEventType.Press else PointerEventType.Release,
                        position,timeMillis=1000+time/1000,type=PointerType.Touch)
                    host.scheduler.runCurrent();writer.record(time,"input","touch $action (${position.x/ComposeHost.RenderScale},${position.y/ComposeHost.RenderScale})")
                } else host.input(action,position/ComposeHost.RenderScale.toFloat())
                host.draw();sample("$action-$time")
            }
            sample("initial")
            if(c.action=="none")return
            if(c.action=="remove") {
                advance(33000);showToggle=false;host.draw();sample("removed");times(64000)
                advance(97000);showToggle=true;host.draw();sample("restored");times(112000,320000);return
            }
            input(33000,"down");times(64000);input(97000,"up");times(112000,160000,320000,640000)
            input(657000,"down");input(689000,"up");times(800000,1024000)
        }
    }
}

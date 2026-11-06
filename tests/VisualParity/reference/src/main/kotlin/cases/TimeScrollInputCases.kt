@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.focus.*
import androidx.compose.ui.input.key.*
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.*
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class TimeScrollInputConfig(val action:String="touch",val minute:Boolean=false,val clock24:Boolean=true,val rtl:Boolean=false)
@ParitySuite("time-scroll-input")
class TimeScrollInputCases {
    @ParityScenario("input") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimeScrollInputConfig>()
        val state=TimePickerState(23,59,c.clock24)
        val probe=Probe()
        val d=ComposeHost.RenderScale.toFloat()
        val viewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration { override val touchSlop=18*d }
        lateinit var focus:FocusManager
        ComposeHost(writer,420,240).use { host ->
            host.mount {
                focus=LocalFocusManager.current
                val family=remember { CaseDefaults.font(writer) }
                MaterialTheme(colorScheme=lightColorScheme(),typography=Typography(fontFamily=family),motionScheme=MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,
                            LocalViewConfiguration provides viewConfiguration) {
                            TimeScroll(state,TimePickerDefaults.shapes(),probe.modifier())
                        }
                    }
                }
            };host.frame(0)
            fun fields(node:Any):List<Any> {
                val semantics=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val own=if(semantics?.getOrNull(SemanticsProperties.ProgressBarRangeInfo)!=null)listOf(node) else emptyList()
                val children=node.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(node) as List<*>
                return own+children.flatMap{fields(checkNotNull(it))}
            }
            fun coordinates(node:Any)=field(checkNotNull(field(node,"nodes")),"innerCoordinator") as LayoutCoordinates
            fun active():Any=fields(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode")))
                .get(if(c.minute)1 else 0) // declaration order: hour, minute; RTL reverses physical position
            val coordinate=coordinates(active())
            val center=coordinate.localToRoot(Offset(coordinate.size.width/2f,coordinate.size.height/2f))
            fun sample(id:String) {
                val field=active()
                val semantics=parity.field(field,"_semanticsConfiguration") as SemanticsConfiguration
                writer.submit(host.snapshot(id,id,listOf(
                    ProbePoint("picker","time-scroll",null,listOf(
                        number("hour",state.hour.toDouble(),"hour",definition="time-picker.selected-hour"),
                        number("minute",state.minute.toDouble(),"minute",definition="time-picker.selected-minute"))),
                    ProbePoint("field","scroll-field","picker",probe.geometry(coordinates(field))+listOf(
                        number("targetPage",semantics[SemanticsProperties.ProgressBarRangeInfo].current.toDouble(),"index",definition="scroll-field.target-page"))))))
            }
            sample("initial")
            for(time in 16000L..1280000L step 16000L) {
                host.advanceTo(time)
                if(c.action.startsWith("touch")) {
                    if(time==64000L && c.action=="touch-external") { if(c.minute)state.minute=45 else state.hour=15 }
                    var distance=when(time){16000L->0f;32000L->10f;48000L->36f;64000L->60f;80000L->88f;else->-1f}
                    val release=if(c.action=="touch-slow")160000L else 96000L
                    if(distance>=0 || time==release) {
                        if(time==release)distance=88f
                        val type=if(time==16000L)PointerEventType.Press else if(time==release)PointerEventType.Release else PointerEventType.Move
                        host.scene.sendPointerEvent(type,center-Offset(0f,distance*d),timeMillis=1000+time/1000,type=PointerType.Touch)
                        host.scheduler.runCurrent();writer.record(time,"input","touch $type")
                    }
                }
                if(c.action=="wheel" && time in listOf(16000L,32000L)) {
                    host.scene.sendPointerEvent(PointerEventType.Scroll,center,scrollDelta=Offset(0f,3f),timeMillis=1000+time/1000)
                    host.scheduler.runCurrent()
                }
                if(c.action=="key" && time==16000L) {
                    focus.moveFocus(FocusDirection.Next);if(c.minute)focus.moveFocus(FocusDirection.Next)
                    host.scene.sendKeyEvent(KeyEvent(Key.DirectionDown,KeyEventType.KeyDown))
                    host.scene.sendKeyEvent(KeyEvent(Key.DirectionDown,KeyEventType.KeyUp));host.scheduler.runCurrent()
                }
                val neighbour=center/d+Offset(0f,50f)
                if(c.action=="click") {
                    if(time==16000L)host.input("move",neighbour)
                    if(time==32000L)host.input("down",neighbour)
                    if(time==80000L)host.input("up",neighbour)
                }
                host.frame(time)
                if(time<=160000L || time%160000L==0L)sample("time-$time")
            }
        }
    }
}

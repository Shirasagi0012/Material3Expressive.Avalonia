@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.focus.FocusManager
import androidx.compose.ui.focus.FocusDirection
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable
import kotlinx.coroutines.launch

@Serializable data class RangeSliderInteractionConfig(val action:String="drag",val start:Boolean=true,val rtl:Boolean=false)

@ParitySuite("range-slider-interaction")
class RangeSliderInteractionCases {
    @ParityScenario("interaction") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<RangeSliderInteractionConfig>()
        val empty=PlatformContext.Empty()
        val desktop=object:PlatformContext by empty {
            override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
        ComposeHost(writer,320,192,desktop).use { host ->
            val s=Scene(c)
            fun observe(index:Int) { val source=s.sources[index];host.scope.launch { source.interactions.collect { s.logs[index].receive(it) } } }
            observe(0);observe(1);host.mount{s.Content()};host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure()))
            var time=0L
            fun change(id:String,action:()->Unit) { time+=16000;host.advanceTo(time);action();host.draw();host.frame(time);sample(id) }
            val index=if(c.start)0 else 1
            val coordinates=checkNotNull(s.thumbs[index].coordinates)
            val point=coordinates.localToWindow(Offset(2f,22f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
            val destination=point+Offset((if(c.start)24f else -24f)*(if(c.rtl)-1 else 1),0f)
            sample("initial")
            if(c.action=="focus") {
                change("focus"){checkNotNull(s.focusManager).moveFocus(FocusDirection.Next);if(index==1)checkNotNull(s.focusManager).moveFocus(FocusDirection.Next)}
                change("other-source"){s.sources[1-index]=MutableInteractionSource();observe(1-index)}
                change("focused-source"){s.sources[index]=MutableInteractionSource();observe(index)}
                return
            }
            if(c.action=="external") {
                val press=PressInteraction.Press(Offset.Zero);val drag=DragInteraction.Start()
                change("press"){check(s.sources[index].tryEmit(press))};change("drag"){check(s.sources[index].tryEmit(drag))}
                change("release"){check(s.sources[index].tryEmit(PressInteraction.Release(press)))}
                change("cancel"){check(s.sources[index].tryEmit(DragInteraction.Cancel(drag)))}
                return
            }
            change("hover"){host.input("move",point)};change("press"){host.input("down",point)}
            if(c.action!="tap" && c.action!="release-jump")
                change("move"){host.input("move",if(c.action=="vertical-cancel")point+Offset(0f,32f)else destination)}
            if(c.action=="disable")change("disable"){s.enabled=false}
            if(c.action=="source")change("source"){s.sources[index]=MutableInteractionSource();observe(index)}
            if(c.action=="detach")change("detach"){s.attached=false}
            change("release"){host.input("up",if(c.action=="tap")point else destination)}
            change("exit"){host.input("move",Offset(4f,4f))}
        }
    }

    private class Scene(val c:RangeSliderInteractionConfig) {
        val sources=mutableStateListOf(MutableInteractionSource(),MutableInteractionSource())
        val logs=arrayOf(EventLog(),EventLog())
        var focusManager:FocusManager?=null
        var values by mutableStateOf(.25f.. .75f)
        var enabled by mutableStateOf(true);var attached by mutableStateOf(true)
        val thumbs=arrayOf(Probe(),Probe());val labels=arrayOf(Probe(),Probe())
        @Composable fun Content() {
            MaterialTheme(colorScheme=lightColorScheme()) {
                focusManager=LocalFocusManager.current
                Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft) {
                    if(attached) Box(Modifier.absoluteOffset(20.dp,96.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            RangeSlider(value=values,onValueChange={values=it},modifier=Modifier.width(280.dp),enabled=enabled,
                                startThumbInteractionSource=sources[0],endThumbInteractionSource=sources[1],
                                startThumb={Thumb(0)},endThumb={Thumb(1)})
                        }
                    }
                }
            }
        }
        @Composable fun Thumb(index:Int) {
            Label(interactionSource=sources[index],label={PlainTooltip(modifier=labels[index].modifier()) {
                Canvas(Modifier.size(32.dp,8.dp)){drawRect(Color.Blue)}
            }}) { SliderDefaults.Thumb(interactionSource=sources[index],isVertical=false,modifier=thumbs[index].modifier(),enabled=enabled) }
        }
        fun measure():List<ProbePoint> {
            val points=mutableListOf(ProbePoint("events","events",null,listOf(
                text("start",logs[0].text,definition="range-slider.interactions.paired-sequence"),
                text("end",logs[1].text,definition="range-slider.interactions.paired-sequence"))))
            if(attached) {
                points+=ProbePoint("slider","slider",null,listOf(number("start",values.start.toDouble(),"value",definition="slider.active-start"),
                    number("end",values.endInclusive.toDouble(),"value",definition="slider.active-end")))
                for(i in 0..1) {
                    val visible=labels[i].coordinates?.isAttached==true
                    points+=ProbePoint("thumb-$i","thumb",null,thumbs[i].geometry().filter{it.name=="box"||it.name=="size"}+flag("label",visible,definition="label.popup.attached"))
                    if(visible)points+=ProbePoint("label-$i","label",null,labels[i].geometry().filter{it.name=="box"||it.name=="size"})
                }
            }
            return points
        }
    }
    private class EventLog {
        val starts=mutableListOf<Interaction>();val events=mutableListOf<String>()
        val text get()=events.joinToString(",")
        fun receive(interaction:Interaction) {
            val (name,start)=when(interaction) {
                is PressInteraction.Press -> "press" to interaction
                is PressInteraction.Release -> "release" to interaction.press
                is PressInteraction.Cancel -> "cancel-press" to interaction.press
                is DragInteraction.Start -> "drag" to interaction
                is DragInteraction.Stop -> "stop" to interaction.start
                is DragInteraction.Cancel -> "cancel-drag" to interaction.start
                is FocusInteraction.Focus -> "focus" to interaction
                is FocusInteraction.Unfocus -> "unfocus" to interaction.focus
                is HoverInteraction.Enter -> "hover" to interaction
                is HoverInteraction.Exit -> "exit" to interaction.enter
                else -> error("Unknown interaction")
            }
            if(start===interaction)starts.add(start)
            events.add("$name:${starts.indexOfFirst{it===start}}")
        }
    }
}

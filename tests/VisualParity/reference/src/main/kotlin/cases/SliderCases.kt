@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class SliderConfig(val kind:String="horizontal", val steps:Int=0, val value:Float=0.5f, val end:Float=0.75f,
    val enabled:Boolean=true, val rtl:Boolean=false, val topToBottom:Boolean=true, val dark:Boolean=false,
    val centered:Boolean=false, val customCorners:Boolean=false, val stop:Boolean=true, val gap:Double=6.0,
    val length:Double=280.0, val customThumbs:Boolean=false, val customColors:Boolean=false, val hoisted:Boolean=false,
    val action:String="none",val autoLength:Boolean=false,val crossSize:Double=0.0,val minimum:Float=0f,val maximum:Float=1f)

@ParitySuite("slider")
class SliderCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) = capture(writer)
    @ParityScenario("interaction")
    suspend fun interaction(writer:CaptureWriter) = capture(writer)
    private suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SliderConfig>()
        // Desktop's ComposeSceneMediator supplies density-scaled 18dp touch slop.
        // PlatformContext.Empty has an unscaled pixel default, inappropriate at 4x.
        val empty=PlatformContext.Empty()
        val desktop=object:PlatformContext by empty {
            override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
        ComposeHost(writer,320,320,desktop).use { host ->
            val s=SliderScene(c)
            host.mount { s.Content() }; host.frame(0)
            fun sample(id:String) { writer.submit(host.snapshot(id,id,s.measure())) }
            fun event(time:Long, action:String, p:Offset) { host.advanceTo(time); host.input(action,p); host.draw(); host.frame(time) }
            var a=if(c.kind=="vertical") Offset(40f,80f) else Offset(80f,40f)
            var b=if(c.kind=="vertical") Offset(40f,230f) else Offset(230f,40f)
            sample("initial")
            if(c.action=="none") return
            if(c.action=="program" || c.action=="theme" || c.action=="corner") {
                host.advanceTo(16000)
                if(c.action=="program") { s.single.value=0.86f; s.rangeValues=0.6f..1f }
                else if(c.action=="corner") s.corner=8.0
                else s.dark=true
                host.draw(); host.frame(16000); sample("changed"); host.frame(32000); sample("late"); return
            }
            if(c.action=="keyboard") {
                host.advanceTo(1000)
                if(c.kind=="range") s.focusManager!!.moveFocus(FocusDirection.Next) else s.requester.requestFocus()
                host.draw(); host.frame(1000); sample("focus")
                listOf(if(c.kind=="vertical") Key.DirectionDown else Key.DirectionRight,Key.PageUp,Key.MoveEnd,Key.MoveHome).forEachIndexed { i,key ->
                    host.advanceTo(2000L+i*2000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); host.frame(host.timeUs); sample("key-${i}-down")
                    host.advanceTo(3000L+i*2000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); host.frame(host.timeUs); sample("key-${i}-up")
                }
                return
            }
            if(c.action=="cross-end") { val previous=a; a=b; b=previous }
            event(1000,"move",a); event(2000,"down",a); sample("pressed")
            if(c.action!="tap" && c.action!="release-jump") { event(16000,"move",if(c.action=="vertical-cancel") Offset(a.x,a.y+40) else b); sample("moved") }
            if(c.action=="disable") { host.advanceTo(17000); s.enabled=false; host.draw(); host.frame(17000); sample("disabled") }
            event(32000,"up",if(c.action=="tap") a else b); sample("released")
            host.frame(48000); sample("late")
        }
    }

    private class SliderScene(val c:SliderConfig) {
        var enabled by mutableStateOf(c.enabled)
        var dark by mutableStateOf(c.dark)
        var corner by mutableStateOf(4.0)
        var rangeValues by mutableStateOf(c.value..c.end)
        val single=SliderState(c.value,c.steps,onValueChangeFinished={ finished++ },valueRange=c.minimum..c.maximum)
        var range:RangeSliderState?=null
        var changes=0; var finished=0
        val slider=Probe(); val track=Probe(); val thumb=Probe(); val endThumb=Probe()
        val interaction=MutableInteractionSource(); val endInteraction=MutableInteractionSource()
        val feedback=mutableSetOf<Interaction>()
        private suspend fun observe(source:MutableInteractionSource) {
            source.interactions.collect { event -> when(event) {
                is PressInteraction.Press,is DragInteraction.Start,is FocusInteraction.Focus -> feedback.add(event)
                is PressInteraction.Release -> feedback.remove(event.press)
                is PressInteraction.Cancel -> feedback.remove(event.press)
                is DragInteraction.Stop -> feedback.remove(event.start)
                is DragInteraction.Cancel -> feedback.remove(event.start)
                is FocusInteraction.Unfocus -> feedback.remove(event.focus)
            } }
        }
        val requester=FocusRequester(); var focusManager:FocusManager?=null
        init { if(!c.hoisted) single.value=c.value; single.onValueChange={ changes++; single.value=it } }
        @Composable
        fun Content() {
            LaunchedEffect(interaction) { observe(interaction) }
            LaunchedEffect(endInteraction) { observe(endInteraction) }
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    focusManager=LocalFocusManager.current
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        val colors=if(c.customColors) SliderDefaults.colors(
                            thumbColor=Color(0xFF126E60),activeTrackColor=Color(0xFF126E60),activeTickColor=Color(0xFFB2E6D6),
                            inactiveTrackColor=Color(0xFFB2E6D6),inactiveTickColor=Color(0xFF126E60),
                            disabledThumbColor=Color(0x50365A80),disabledActiveTrackColor=Color(0x50365A80),
                            disabledActiveTickColor=Color(0x20456789),disabledInactiveTrackColor=Color(0x20456789),disabledInactiveTickColor=Color(0x50365A80))
                            else SliderDefaults.colors()
                        val modifier=slider.modifier()
                            .then(if(c.autoLength) Modifier else if(c.kind=="vertical") Modifier.height(c.length.toFloat().dp) else Modifier.width(c.length.toFloat().dp))
                            .then(if(c.crossSize==0.0) Modifier else if(c.kind=="vertical") Modifier.width(c.crossSize.toFloat().dp) else Modifier.height(c.crossSize.toFloat().dp))
                        if(c.kind=="range") {
                            RangeSlider(value=rangeValues,onValueChange={ changes++; rangeValues=it },modifier=modifier,enabled=enabled,
                                steps=c.steps,valueRange=c.minimum..c.maximum,onValueChangeFinished={ finished++ },startThumbInteractionSource=interaction,endThumbInteractionSource=endInteraction,
                                startThumb={ Thumb(thumb,colors,false) },endThumb={ Thumb(endThumb,colors,true) },
                                track={ state -> range=state
                                    if(c.customCorners) SliderDefaults.Track(state,trackCornerSize=corner.toFloat().dp,modifier=track.modifier(),enabled=enabled,colors=colors,
                                        drawStopIndicator=if(c.stop) ({ offset -> drawCircle(colors.trackColorFor(enabled),4.dp.toPx()/2,offset) }) else null,thumbTrackGapSize=c.gap.toFloat().dp)
                                    else SliderDefaults.Track(state,modifier=track.modifier(),enabled=enabled,colors=colors,
                                        drawStopIndicator=if(c.stop) ({ offset -> drawCircle(colors.trackColorFor(enabled),4.dp.toPx()/2,offset) }) else null,thumbTrackGapSize=c.gap.toFloat().dp)
                                })
                        } else if(c.kind=="vertical") {
                            VerticalSlider(state=single,modifier=modifier.focusRequester(requester),enabled=enabled,topToBottom=c.topToBottom,interactionSource=interaction,
                                thumb={ Thumb(thumb,colors,false) },track={ Track(it,colors) })
                        } else {
                            Slider(state=single,modifier=modifier.focusRequester(requester),enabled=enabled,interactionSource=interaction,
                                thumb={ Thumb(thumb,colors,false) },track={ Track(it,colors) })
                        }
                    }
                }
            }
        }
        @Composable
        fun Thumb(probe:Probe,colors:SliderColors,end:Boolean) {
            val size=if(c.kind=="vertical") DpSize(if(c.customThumbs) 32.dp else 44.dp,if(c.customThumbs) 12.dp else 4.dp)
                else DpSize(if(c.customThumbs) if(end) 20.dp else 12.dp else 4.dp,if(c.customThumbs) if(end) 50.dp else 32.dp else 44.dp)
            SliderDefaults.Thumb(if(end) endInteraction else interaction,isVertical=c.kind=="vertical",modifier=probe.modifier(),
                colors=colors,enabled=enabled,thumbSize=size)
        }
        @Composable
        fun Track(state:SliderState,colors:SliderColors) {
            val stop:(androidx.compose.ui.graphics.drawscope.DrawScope.(Offset)->Unit)?=if(c.stop) ({ offset -> drawCircle(colors.trackColorFor(enabled),4.dp.toPx()/2,offset) }) else null
            if(c.centered) SliderDefaults.CenteredTrack(state,modifier=track.modifier(),enabled=enabled,colors=colors,
                drawStopIndicator=stop,thumbTrackGapSize=c.gap.toFloat().dp,trackCornerSize=if(c.customCorners) corner.toFloat().dp else Dp.Unspecified)
            else if(c.customCorners || c.kind=="vertical") SliderDefaults.Track(state,trackCornerSize=if(c.customCorners) corner.toFloat().dp else Dp.Unspecified,
                modifier=track.modifier(),enabled=enabled,colors=colors,drawStopIndicator=stop,thumbTrackGapSize=c.gap.toFloat().dp)
            else SliderDefaults.Track(state,modifier=track.modifier(),enabled=enabled,colors=colors,drawStopIndicator=stop,thumbTrackGapSize=c.gap.toFloat().dp)
        }
        fun measure():List<ProbePoint> {
            val start=if(c.kind=="range") checkNotNull(range).activeRangeStart else single.value
            val end=if(c.kind=="range") checkNotNull(range).activeRangeEnd else single.value
            val points=mutableListOf(ProbePoint("slider","slider",null,slider.geometry()+listOf(
                number("value",start.toDouble(),"value",definition="slider.active-start"),
                number("end",end.toDouble(),"value",definition="slider.active-end"),
                number("changes",changes.toDouble(),"count",definition="slider.value-change-callbacks"),
                number("finished",finished.toDouble(),"count",definition="slider.value-finished-callbacks"),
                number("feedback",if(feedback.isEmpty()) 0.0 else 1.0,"boolean",definition="slider.thumb-feedback-active"))),
                ProbePoint("track","track","slider",track.geometry()),ProbePoint("thumb","thumb","slider",thumb.geometry()))
            if(c.kind=="range") points+=ProbePoint("end-thumb","thumb","slider",endThumb.geometry())
            return points
        }
    }
}
private fun SliderColors.trackColorFor(enabled:Boolean)=if(enabled) activeTrackColor else disabledActiveTrackColor

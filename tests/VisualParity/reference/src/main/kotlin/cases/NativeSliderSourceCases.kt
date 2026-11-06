@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class NativeSliderSourceConfig(val vertical:Boolean=false,val action:String="overlap")

@ParitySuite("native-slider-source")
class NativeSliderSourceCases {
    @ParityScenario("feedback") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<NativeSliderSourceConfig>();val slider=Probe();val label=Probe()
        var source by mutableStateOf(MutableInteractionSource());var enabled by mutableStateOf(true)
        val state=SliderState(.25f);state.onValueChange={state.value=it}
        ComposeHost(writer,360,360).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalRippleThemeConfiguration provides RippleDefaults.InsetFocusRingThemeConfiguration) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            Label(interactionSource=source,label={Box(label.modifier().size(40.dp,24.dp).background(Color.Blue))}) {
                                if(c.vertical) VerticalSlider(state=state,modifier=slider.modifier().size(48.dp,240.dp),topToBottom=false,enabled=enabled,interactionSource=source)
                                else Slider(state=state,modifier=slider.modifier().size(240.dp,48.dp),enabled=enabled,interactionSource=source)
                            }
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            var next=16000L
            fun advance(at:Long){while(next<=at){host.frame(next);next+=16000};host.advanceTo(at)}
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("slider","slider",null,
                slider.geometry().filter{it.name in listOf("box","size")}+listOf(
                    number("value",state.value.toDouble(),definition="slider.actual-value"),
                    flag("label",label.coordinates?.isAttached==true,definition="label.popup.attached"))))))
            fun change(at:Long,id:String,action:()->Unit){advance(at);action();host.draw();host.frame(at);sample(id)}
            val press=PressInteraction.Press(Offset.Zero);val drag=DragInteraction.Start();val focus=FocusInteraction.Focus();val old=source
            val point=if(c.vertical)Offset(180f,239f)else Offset(121f,180f)
            sample("initial");change(1000,"start"){
                when(c.action){"focus"->source.tryEmit(focus);"pointer"->{host.input("move",point);host.input("down",point)};else->source.tryEmit(press)}
            }
            listOf(16000L,64000L,160000L).forEach{advance(it);sample("time-$it")}
            change(161000,"change"){
                when(c.action){
                    "focus"->source.tryEmit(FocusInteraction.Unfocus(focus))
                    "cancel"->{source.tryEmit(PressInteraction.Cancel(press));source.tryEmit(drag)}
                    "replace"->source=MutableInteractionSource()
                    "disable"->enabled=false
                    "pointer"->host.input("move",point+if(c.vertical)Offset(0f,-24f)else Offset(24f,0f))
                    else->{source.tryEmit(drag);source.tryEmit(PressInteraction.Release(press))}
                }
            }
            listOf(176000L,240000L,320000L).forEach{advance(it);sample("time-$it")}
            change(321000,"end"){
                when(c.action){
                    "pointer"->{host.input("up",point+if(c.vertical)Offset(0f,-24f)else Offset(24f,0f));host.input("move",Offset(5f,5f))}
                    "cancel"->source.tryEmit(DragInteraction.Cancel(drag))
                    "replace"->{old.tryEmit(PressInteraction.Release(press));source.tryEmit(drag)}
                    else->{source.tryEmit(PressInteraction.Release(press));source.tryEmit(DragInteraction.Stop(drag))}
                }
            }
            listOf(336000L,400000L,640000L).forEach{advance(it);sample("time-$it")}
        }
    }
}

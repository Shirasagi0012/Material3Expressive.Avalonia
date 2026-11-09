@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("DEPRECATION")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class LegacySliderTrackConfig(val mode:String="standalone",val start:Float=.25f,val end:Float=.75f,
    val steps:Int=0,val enabled:Boolean=true,val rtl:Boolean=false,val translucent:Boolean=false,val dynamic:Boolean=false,
    val customColors:Boolean=true)

@ParitySuite("legacy-slider-track")
class LegacySliderTrackCases {
    @ParityScenario("capture")
    suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<LegacySliderTrackConfig>()
        var start by mutableFloatStateOf(c.start);var end by mutableFloatStateOf(c.end)
        var nativeValue by mutableFloatStateOf(c.end);var enabled by mutableStateOf(c.enabled)
        var rtl by mutableStateOf(c.rtl);var translucent by mutableStateOf(c.translucent)
        var ticks by mutableStateOf(if(c.steps==0)floatArrayOf() else FloatArray(c.steps+2){it.toFloat()/(c.steps+1)})
        var attached by mutableStateOf(true);var rangeSteps by mutableIntStateOf(c.steps)
        val probe=Probe()
        ComposeHost(writer,240,96).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        val colors=if(c.customColors) SliderDefaults.colors(thumbColor=Color.Black,
                            activeTrackColor=Color(if(translucent)0x804488CC else 0xFF4488CC),activeTickColor=Color(0xFFFFDD55),
                            inactiveTrackColor=Color(if(translucent)0x80775533 else 0xFF775533),inactiveTickColor=Color(0xFFDD4466),
                            disabledThumbColor=Color(0xFF999999),disabledActiveTrackColor=Color(0xFF777777),disabledActiveTickColor=Color(0xFFDDDDDD),
                            disabledInactiveTrackColor=Color(0xFFCCCCCC),disabledInactiveTickColor=Color(0xFF555555)) else SliderDefaults.colors()
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            if(attached) when(c.mode) {
                                "native" -> Slider(value=nativeValue,onValueChange={nativeValue=it},modifier=Modifier.width(180.dp),
                                    enabled=enabled,colors=colors,steps=c.steps,
                                    track={SliderDefaults.Track(SliderPositions(0f..nativeValue,ticks),probe.modifier(),colors,enabled)})
                                "range" -> RangeSlider(value=start..end,onValueChange={start=it.start;end=it.endInclusive},
                                    modifier=Modifier.width(180.dp),enabled=enabled,colors=colors,steps=rangeSteps,
                                    track={SliderDefaults.Track(SliderPositions(start..end,ticks),probe.modifier(),colors,enabled)})
                                else -> SliderDefaults.Track(SliderPositions(start..end,ticks),probe.modifier().width(180.dp),colors,enabled)
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("track","legacy-slider-track",null,
                probe.geometry().filter{it.name=="box"||it.name=="size"})),true))
            sample("initial");if(!c.dynamic)return
            fun change(time:Long,id:String,action:()->Unit){host.advanceTo(time);action();host.draw();host.frame(time);sample(id)}
            change(16000,"value"){end=1f;start=.5f;nativeValue=.5f}
            change(32000,"disabled"){enabled=false}
            change(48000,"translucent"){translucent=true;enabled=true}
            change(64000,"direction"){rtl=!c.rtl}
            change(80000,"replace-state"){start=.125f;end=.875f;rangeSteps=0;ticks=if(c.mode=="native")floatArrayOf(0f,.125f,.875f,1f) else floatArrayOf();nativeValue=.75f}
            change(96000,"remount"){attached=false;host.draw();attached=true}
        }
    }
}

@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.AbsoluteRoundedCornerShape
import androidx.compose.foundation.text.input.TextFieldState
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class SearchInputFocusRingConfig(val dark:Boolean=false,val rtl:Boolean=false,val asymmetric:Boolean=false,
    val custom:Boolean=false,val action:String="focus",val readOnly:Boolean=false)

@ParitySuite("search-input-focus-ring")
class SearchInputFocusRingCases {
    @ParityScenario("ring") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SearchInputFocusRingConfig>();val input=Probe()
        var source by mutableStateOf(MutableInteractionSource());var inset by mutableStateOf(true)
        ComposeHost(writer,420,160).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    val theme=if(!inset)RippleDefaults.OpacityFocusThemeConfiguration
                        else if(c.custom)RippleThemeConfiguration(RippleThemeConfiguration.Focus.InsetRing(1.1.dp,1.3.dp,2.4.dp,2.7.dp))
                        else RippleDefaults.InsetFocusRingThemeConfiguration
                    val local=if(c.custom)RippleConfiguration(focus=RippleConfiguration.Focus.InsetRing(Color(0x806600CC),Color(0xC000CC66)))else RippleConfiguration()
                    CompositionLocalProvider(LocalRippleThemeConfiguration provides theme,LocalRippleConfiguration provides local,
                        LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            SearchBarDefaults.InputField(textFieldState=remember{TextFieldState()},searchBarState=rememberSearchBarState(),onSearch={},
                                modifier=input.modifier().size(360.dp,56.dp),interactionSource=source,readOnly=c.readOnly,
                                shape=if(c.asymmetric)AbsoluteRoundedCornerShape(24.dp,8.dp,0.dp,16.dp)else SearchBarDefaults.inputFieldShape,
                                placeholder={Box(Modifier.size(48.dp,16.dp).background(Color.Blue))})
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            var next=16000L
            fun advance(target:Long){while(next<=target){host.frame(next);next+=16000};host.advanceTo(target)}
            fun sample(id:String){writer.submit(host.snapshot(id,id,listOf(ProbePoint("input","search-input",null,input.geometry().filter{it.name in listOf("box","size")}))))}
            fun change(at:Long,id:String,action:()->Unit){advance(at);action();host.draw();host.frame(at);sample(id)}
            val focus=FocusInteraction.Focus()
            sample("initial");change(1000,"focus"){source.tryEmit(focus)}
            listOf(16000L,32000L,64000L,96000L,160000L).forEach{advance(it);sample("time-$it")}
            change(161000,"change"){
                when(c.action){
                    "mode"->inset=false
                    "pointer"->{host.input("down",Offset(5f,5f));host.input("up",Offset(5f,5f))}
                    "replace"->source=MutableInteractionSource()
                    else->source.tryEmit(FocusInteraction.Unfocus(focus))
                }
            }
            listOf(176000L,208000L,240000L).forEach{advance(it);sample("time-$it")}
            if(c.action=="reverse")change(241000,"refocus"){source.tryEmit(FocusInteraction.Focus())}
            if(c.action=="mode")change(241000,"ring-mode"){inset=true}
            listOf(256000L,320000L,480000L,800000L).forEach{advance(it);sample("time-$it")}
        }
    }
}

@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable
import kotlin.math.roundToInt

@Serializable data class TimePickerAlphaConfig(val selectorAlpha:Float=1f,val textAlpha:Float=1f,val parentAlpha:Float=1f,val colorAlpha:Boolean=false,val dynamic:Boolean=false,val hostGroup:Boolean=false)
@ParitySuite("time-picker-alpha")
class TimePickerAlphaCases {
    @ParityScenario("render") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TimePickerAlphaConfig>(); val probe=Probe(); val state=TimePickerState(0,0,false)
        fun alpha(value:Float)=if(c.colorAlpha)(value*255).roundToInt()/255f else value
        var selector by mutableStateOf(Color.Black.copy(alpha=alpha(c.selectorAlpha)))
        var text by mutableStateOf(Color.Blue.copy(alpha=alpha(c.textAlpha)))
        var parent by mutableStateOf(c.parentAlpha)
        ComposeHost(writer,360,480).use { host ->
            host.mount {
                val family=remember { CaseDefaults.font(writer) }
                val typography=Typography().let { it.copy(displayLarge=it.displayLarge.copy(fontFamily=family),
                    bodyLarge=it.bodyLarge.copy(fontFamily=family),titleMedium=it.titleMedium.copy(fontFamily=family)) }
                MaterialTheme(colorScheme=lightColorScheme(),typography=typography) {
                    Column(Modifier.fillMaxSize().background(Color.White)) {
                        TimePicker(state,probe.modifier().padding(16.dp).alpha(parent),
                            colors=TimePickerDefaults.colors(clockDialColor=Color.White,selectorColor=selector,
                                clockDialSelectedContentColor=text,clockDialContentColor=Color.Red))
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
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("dial","clock-dial",null,
                probe.geometry(coordinates).filter { it.name=="box"||it.name=="size" }))))
            fun change(time:Long,id:String,action:()->Unit) { host.advanceTo(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id) }
            sample("initial")
            if(c.dynamic) {
                change(16000,"brush-opacity"){selector=Color.Black.copy(alpha=.5f);text=Color.Blue.copy(alpha=.5f)}
                change(32000,"brush-color"){selector=Color.Green.copy(alpha=.5f)}
                change(48000,"replacement"){selector=Color.Transparent;text=Color.Red}
                change(64000,"ancestor"){parent=.5f}
            }
        }
    }
}

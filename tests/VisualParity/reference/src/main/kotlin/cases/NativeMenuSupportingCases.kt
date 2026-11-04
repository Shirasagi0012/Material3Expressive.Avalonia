@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class NativeMenuSupportingConfig(val vibrant:Boolean=false,val selected:Boolean=false,val enabled:Boolean=true,
    val rtl:Boolean=false,val dark:Boolean=false,val templateOnly:Boolean=false,val dynamic:Boolean=false,val trailing:Boolean=false,val width:Double=200.0)

@ParitySuite("native-menu-supporting")
class NativeMenuSupportingCases {
    @ParityScenario("content") suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<NativeMenuSupportingConfig>()
        ComposeHost(writer,320,160).use { host ->
            val box=Probe(); val header=Probe(); val support=Probe(); val trailing=Probe()
            var supporting by mutableStateOf(true)
            host.mount {
                MaterialTheme(colorScheme=if(config.dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            Box(Modifier.width(config.width.dp).then(box.modifier())) {
                                val secondary:@Composable (() -> Unit)?=if(supporting) ({
                                    Box(support.modifier().size(80.dp,12.dp).background(LocalContentColor.current))
                                }) else null
                                val end:@Composable (() -> Unit)?=if(config.trailing && supporting) ({
                                    Box(trailing.modifier().size(8.dp,12.dp).background(LocalContentColor.current))
                                }) else null
                                SelectableDropdownMenuItem(selected=config.selected,onClick={},enabled=config.enabled,
                                    text={ Box(header.modifier().size(48.dp,16.dp).background(LocalContentColor.current)) },
                                    leadingIcon={ Box(Modifier.size(20.dp).background(LocalContentColor.current)) },
                                    supportingText=secondary, trailingContent=end, shapes=MenuDefaults.itemShapes(),
                                    colors=if(config.vibrant) MenuDefaults.selectableItemVibrantColors() else MenuDefaults.selectableItemColors())
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun geometry(id:String,probe:Probe)=ProbePoint(id,id,null,probe.geometry().filter { it.name=="box" || it.name=="size" })
            fun sample(id:String) {
                val points=mutableListOf(geometry("allocation",box),geometry("header",header))
                if(supporting) points.add(geometry("supporting",support))
                if(supporting && config.trailing) points.add(geometry("trailing",trailing))
                writer.submit(host.snapshot(id,id,points))
            }
            sample("initial")
            if(config.dynamic) {
                host.advanceTo(16000);supporting=false;host.draw();host.frame(16000);sample("removed")
                host.advanceTo(32000);supporting=true;host.draw();host.frame(32000);sample("restored")
            }
        }
    }
}

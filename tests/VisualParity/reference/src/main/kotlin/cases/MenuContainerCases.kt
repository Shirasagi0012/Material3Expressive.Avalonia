@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
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
data class MenuContainerConfig(val vibrant:Boolean=false,val dark:Boolean=false,val rtl:Boolean=false,val divider:Boolean=false,val width:Double=200.0,val legacySeparator:Boolean=false,val autoWidth:Boolean=false)

@ParitySuite("menu-container")
class MenuContainerCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<MenuContainerConfig>()
        val probes=listOf(Probe(),Probe())
        ComposeHost(writer,320,200).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(config.dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            DropdownMenuGroup(shapes=MenuDefaults.groupShapes(),modifier=if(config.autoWidth)Modifier.width(IntrinsicSize.Max)else Modifier.width(config.width.toFloat().dp),
                                containerColor=if(config.vibrant) MenuDefaults.groupVibrantContainerColor else MenuDefaults.groupStandardContainerColor) {
                                for(i in 0..1) {
                                    if(i==1 && config.divider) HorizontalDivider(Modifier.padding(MenuDefaults.HorizontalDividerPadding))
                                    SelectableDropdownMenuItem(selected=i==1,onClick={},text={
                                        Box(probes[i].modifier().size(32.dp,16.dp).background(LocalContentColor.current))
                                    },shapes=MenuDefaults.itemShapes(),
                                      colors=if(config.vibrant) MenuDefaults.selectableItemVibrantColors() else MenuDefaults.selectableItemColors())
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            writer.submit(host.snapshot("initial","initial",probes.mapIndexed {i,probe ->
                ProbePoint("content-$i","content",null,probe.geometry().filter {it.name=="box" || it.name=="size"})
            }))
        }
    }
}


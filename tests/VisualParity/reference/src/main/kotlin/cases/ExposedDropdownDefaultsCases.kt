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

@ParitySuite("exposed-dropdown-field")
class ExposedDropdownFieldCases {
    @ParityScenario("colors") suspend fun capture(writer:CaptureWriter)=TextFieldCases().static(writer)
}

@Serializable data class ExposedIconConfig(val expanded:Boolean=false,val rtl:Boolean=false,val size:Int=24)
@ParitySuite("exposed-dropdown-icon")
class ExposedDropdownIconCases {
    @ParityScenario("icon") suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<ExposedIconConfig>()
        ComposeHost(writer,96,96).use { host ->
            var expanded by mutableStateOf(config.expanded)
            val probe=Probe()
            host.mount {
                MaterialTheme {
                    CompositionLocalProvider(LocalContentColor provides Color.Red,LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            ExposedDropdownMenuDefaults.TrailingIcon(expanded,probe.modifier().size(config.size.dp))
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("icon","icon",null,
                probe.geometry().filter{it.name=="size"||it.name=="box"}+flag("expanded",expanded,definition="exposed.icon-expanded")))))
            sample("initial");host.advanceTo(1000);expanded=!expanded;writer.record(1000,"mutation","toggle")
            host.draw();sample("toggle");host.frame(16000);sample("next-frame")
        }
    }
}

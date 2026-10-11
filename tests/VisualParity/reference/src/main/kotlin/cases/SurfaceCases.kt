@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.AbsoluteRoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class SurfaceConfig(val shape:String="round",val layer:String="fill",val dark:Boolean=false,
    val kind:String="surface",val x:Double=130.0,val y:Double=95.0,val elevation:Double=0.0)

@ParitySuite("surface")
class SurfaceCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<SurfaceConfig>()
        val probe=Probe()
        ComposeHost(writer,360,240).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                        val modifier=Modifier.offset(config.x.toFloat().dp,config.y.toFloat().dp).then(probe.modifier()).size(100.dp,50.dp)
                        val color=if(config.dark) Color(0xFF211F26) else Color(0xFFEADDFF)
                        val shape=when(config.shape) {
                            "asymmetric-large" -> AbsoluteRoundedCornerShape(80.dp,40.dp,0.dp,0.dp)
                            "horizontal-overlap" -> AbsoluteRoundedCornerShape(80.dp,80.dp,0.dp,0.dp)
                            else -> AbsoluteRoundedCornerShape(16.dp)
                        }
                        when(config.kind) {
                            "button" -> ElevatedButton(onClick={},modifier=modifier,shape=shape,
                                colors=ButtonDefaults.elevatedButtonColors(containerColor=color),
                                elevation=ButtonDefaults.elevatedButtonElevation(defaultElevation=config.elevation.toFloat().dp),
                                contentPadding=PaddingValues(0.dp)) {}
                            "fab" -> FloatingActionButton(onClick={},modifier=modifier,shape=shape,containerColor=color,
                                elevation=FloatingActionButtonDefaults.elevation(defaultElevation=config.elevation.toFloat().dp)) {}
                            else -> Surface(modifier=modifier,shape=shape,color=color,shadowElevation=config.elevation.toFloat().dp,
                                border=if(config.layer=="border") BorderStroke(1.1.dp,Color.Red) else null) {
                                if(config.layer=="content") Box(Modifier.fillMaxSize().background(Color.Blue))
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            writer.submit(host.snapshot("initial","initial",listOf(ProbePoint("container","container",null,
                probe.geometry().filter { it.name=="box" || it.name=="size" }))))
        }
    }
}

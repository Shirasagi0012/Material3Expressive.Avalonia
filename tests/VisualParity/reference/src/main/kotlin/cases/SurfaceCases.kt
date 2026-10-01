package parity

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.*
import parity.CaseDefaults.Theme

@ParitySuite("surface")
class SurfaceCases {
    @ParityScenario("static")
    suspend fun surfaceStatic(w:CaptureWriter) {
        w.readConfig<EmptyConfig>()
        ComposeHost(w).use { h ->
            val probe=Probe()
            h.mount { Theme(w) { Surface(Modifier.size(100.dp,50.dp).then(probe.modifier()),shape=RoundedCornerShape(12.dp),color=Color(0xff6750a4)) {} } }
            h.frame(-2000); h.frame(-1000); h.frame(0)
            w.submit(h.snapshot("initial","mounted",listOf(ProbePoint("surface","container",null,probe.geometry()+
                vector("requestedColor","color",listOf(103.0,80.0,164.0,255.0),"srgba8","none","properties",source="declared")))))
        }
    }
}

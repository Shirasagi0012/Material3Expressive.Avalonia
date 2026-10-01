@file:OptIn(androidx.compose.ui.text.ExperimentalTextApi::class)
package parity

import androidx.compose.foundation.layout.*
import androidx.compose.material3.Text
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable
import parity.CaseDefaults.Theme
import parity.CaseDefaults.font

@Serializable data class TextConfig(val text:String, val stage:String)
@ParitySuite("text")
class TextCases {
    @ParityScenario("layout")
    suspend fun textLayout(w:CaptureWriter) {
        val config=w.readConfig<TextConfig>()
        ComposeHost(w).use { h -> val p=Probe(); val family=font(w)
            h.mount { Theme(w) { Text(config.text,Modifier.width(120.dp).then(p.modifier()),
                fontFamily=family,fontSize=14.sp,fontWeight=FontWeight.Normal,lineHeight=20.sp,letterSpacing=0.sp,color=Color(0xff6750a4),onTextLayout={p.layout=it}) } }
            h.frame(-2000); h.frame(-1000); h.frame(0); w.submit(h.snapshot("initial",config.stage,listOf(p.textPoint("label",null))))
        }
    }
}

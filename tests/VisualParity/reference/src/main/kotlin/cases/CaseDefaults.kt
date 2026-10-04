@file:OptIn(androidx.compose.ui.text.ExperimentalTextApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.*
import androidx.compose.ui.text.platform.Font
import java.io.File
import kotlinx.serialization.Serializable

@Serializable class EmptyConfig
object CaseDefaults {
    fun font(w:CaptureWriter)=FontFamily(listOf(400 to "Regular",500 to "Medium",700 to "Bold").map { (weight, style) ->
        Font(File(w.root,"modules/MaterialTypography.Roboto/Assets/Roboto-$style.ttf"),weight=FontWeight(weight)) })
    @Composable fun Theme(writer:CaptureWriter, content:@Composable ()->Unit) {
        val family=remember { font(writer) }
        val type=Typography().let { it.copy(labelLarge=it.labelLarge.copy(fontFamily=family)) }
        MaterialTheme(colorScheme=lightColorScheme(),typography=type) {
            Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) { content() }
        }
    }
}

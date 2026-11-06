@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.*
import androidx.compose.ui.text.TextLayoutResult
import java.util.Locale
import kotlinx.serialization.Serializable

@Serializable
data class ScrollFieldNumberConfig(val locale:String="en-US",val index:Int=7,val reverse:Boolean=false)

@ParitySuite("scroll-field-number")
class ScrollFieldNumberCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) {
        val config=writer.readConfig<ScrollFieldNumberConfig>()
        val previous=Locale.getDefault()
        try {
            Locale.setDefault(Locale.forLanguageTag(config.locale))
            ComposeHost(writer,200,120).use { host ->
                var selected by mutableStateOf(false)
                val probe=Probe()
                host.mount {
                    // Match the library's expressive motion and all 30 typography token roles.
                    // Typography() selects Core's legacy 15-role constructor (emphasized=base).
                    MaterialTheme(motionScheme=MotionScheme.expressive(),typography=Typography(fontFamily=CaseDefaults.font(writer))) {
                        Box(Modifier.fillMaxSize().background(Color.White).then(probe.modifier()),contentAlignment=Alignment.Center) {
                            ScrollFieldDefaults.Item(config.index,selected,ScrollFieldDefaults.colors(selectedContentColor=Color.Black,unselectedContentColor=Color.Blue))
                        }
                    }
                }
                host.frame(0)
                fun layouts(node:Any):List<TextLayoutResult> {
                    val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                    val result=mutableListOf<TextLayoutResult>()
                    if(own?.contains(SemanticsActions.GetTextLayoutResult)==true)own[SemanticsActions.GetTextLayoutResult].action?.invoke(result)
                    val children=node.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(node) as List<*>
                    return result+children.flatMap { layouts(checkNotNull(it)) }
                }
                fun sample(id:String) {
                    val layout=layouts(checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))).single()
                    val style=layout.layoutInput.style
                    writer.submit(host.snapshot(id,id,listOf(ProbePoint("number","number",null,listOf(
                        text("content",layout.layoutInput.text.text,"text","scroll-field-number.content"),
                        number("fontSize",style.fontSize.value.toDouble(),"sp",category="text",definition="scroll-field-number.fontSize"),
                        number("weight",(style.fontWeight?.weight?:400).toDouble(),"weight",category="text",definition="scroll-field-number.weight"))))))
                }
                sample("initial");host.advanceTo(16000);selected=true
                for(time in 16000L..640000L step 16000L) {
                    if(time==80000L && config.reverse) { host.advanceTo(time);selected=false }
                    host.frame(time)
                    if(time<=160000L || time%80000L==0L)sample("time-$time")
                }
            }
        } finally { Locale.setDefault(previous) }
    }
}

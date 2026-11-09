@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class TonalElevationConfig(val kind:String="surface",val enabled:Boolean=true,val innerEnabled:Boolean?=null,
    val dark:Boolean=false,val customColor:Boolean=false,val shadow:Double=0.0,val dynamic:Boolean=false)

@ParitySuite("tonal-elevation")
class TonalElevationCases {
    @ParityScenario("capture") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TonalElevationConfig>()
        ComposeHost(writer,240,190).use { host ->
            var enabled by mutableStateOf(c.enabled)
            var innerEnabled by mutableStateOf(c.innerEnabled)
            var outerElevation by mutableStateOf(2f)
            var actualOuter=0.0;var actualInner=0.0;var actualEnabled=true
            host.mount {
                val colors=if(c.dark) darkColorScheme(surface=Color(0xFF141218),surfaceTint=Color(0xFFD0BCFF))
                    else lightColorScheme(surface=Color(0xFFFFF7FF),surfaceTint=Color(0xFF6750A4))
                MaterialTheme(colorScheme=colors) {
                    CompositionLocalProvider(LocalTonalElevationEnabled provides enabled) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            Surface(Modifier.size(180.dp,130.dp),shape=RoundedCornerShape(16.dp),tonalElevation=outerElevation.dp) {
                                actualOuter=LocalAbsoluteTonalElevation.current.value.toDouble()
                                Box(Modifier.fillMaxSize(),contentAlignment=Alignment.Center) {
                                    CompositionLocalProvider(LocalTonalElevationEnabled provides (innerEnabled ?: LocalTonalElevationEnabled.current)) {
                                        val content:@Composable ()->Unit={
                                            actualInner=LocalAbsoluteTonalElevation.current.value.toDouble()
                                            actualEnabled=LocalTonalElevationEnabled.current
                                            Box(Modifier.fillMaxSize(),contentAlignment=Alignment.Center) {
                                                Box(Modifier.size(24.dp,12.dp).background(Color.Black))
                                            }
                                        }
                                        val modifier=Modifier.size(100.dp,60.dp)
                                        val shape=RoundedCornerShape(12.dp)
                                        val color=if(c.customColor) Color(0xFFFF7F50) else colors.surface
                                        when(c.kind) {
                                            "clickable"->Surface(onClick={},modifier=modifier,shape=shape,color=color,tonalElevation=3.dp,shadowElevation=c.shadow.toFloat().dp,content=content)
                                            "selectable"->Surface(selected=false,onClick={},modifier=modifier,shape=shape,color=color,tonalElevation=3.dp,shadowElevation=c.shadow.toFloat().dp,content=content)
                                            "toggleable"->Surface(checked=false,onCheckedChange={},modifier=modifier,shape=shape,color=color,tonalElevation=3.dp,shadowElevation=c.shadow.toFloat().dp,content=content)
                                            else->Surface(modifier=modifier,shape=shape,color=color,tonalElevation=3.dp,shadowElevation=c.shadow.toFloat().dp,content=content)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("outer","outer",null,listOf(number("absoluteElevation",actualOuter,"dip",definition="surface.absolute-tonal-elevation"))),
                ProbePoint("inner","inner","outer",listOf(number("absoluteElevation",actualInner,"dip",definition="surface.absolute-tonal-elevation"),
                    flag("enabled",actualEnabled,definition="surface.tonal-enabled"))))))
            sample("initial")
            if(!c.dynamic)return
            fun change(time:Long,id:String,action:()->Unit){host.advanceTo(time);action();writer.record(time,"mutation",id);host.frame(time);sample(id)}
            change(1000,"disabled"){enabled=false}
            change(17000,"inner-enabled"){innerEnabled=true}
            change(33000,"ancestor-elevation"){outerElevation=8f}
            change(49000,"inherit-disabled"){innerEnabled=null}
            change(65000,"enabled"){enabled=true}
            change(81000,"restore-elevation"){outerElevation=2f}
        }
    }
}

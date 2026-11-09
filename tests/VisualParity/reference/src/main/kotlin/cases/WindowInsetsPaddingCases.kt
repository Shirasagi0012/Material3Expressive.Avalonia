@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class WindowInsetsPaddingConfig(val rtl:Boolean=false,val consumed:Boolean=false,val union:Boolean=false,
    val fractional:Boolean=false,val excess:Boolean=false,val dynamic:Boolean=false)

@ParitySuite("window-insets-padding")
class WindowInsetsPaddingCases {
    @ParityScenario("capture") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<WindowInsetsPaddingConfig>()
        var rtl by mutableStateOf(c.rtl)
        var consumed by mutableStateOf(if(c.consumed)PaddingValues(start=4.dp,top=12.dp,end=8.dp,bottom=16.dp) else PaddingValues())
        var union by mutableStateOf(if(c.union)WindowInsets(left=6.dp,top=18.dp,right=10.dp,bottom=24.dp) else WindowInsets())
        var outerInsets by mutableStateOf(if(c.excess)WindowInsets(left=180.dp,top=150.dp,right=180.dp,bottom=150.dp) else if(c.fractional)
            WindowInsets(left=8.375.dp,top=24.125.dp,right=12.625.dp,bottom=16.375.dp) else WindowInsets(left=8.dp,top=24.dp,right=12.dp,bottom=16.dp))
        var innerInsets by mutableStateOf(WindowInsets(left=16.dp,top=30.dp,right=20.dp,bottom=40.dp))
        var padding:PaddingValues=PaddingValues()
        val outer=Probe();val inner=Probe();val screen=Probe();val marker=Probe()
        ComposeHost(writer,280,240).use { host ->
            host.mount {
                MaterialTheme {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).consumeWindowInsets(consumed).consumeWindowInsets(union)) {
                            Box(outer.modifier().fillMaxSize().windowInsetsPadding(outerInsets),propagateMinConstraints=true) {
                                Box(inner.modifier().fillMaxSize().windowInsetsPadding(innerInsets),propagateMinConstraints=true) {
                                    Scaffold(screen.modifier().fillMaxSize(),containerColor=Color.Cyan,
                                        contentWindowInsets=WindowInsets(left=24.dp,top=36.dp,right=32.dp,bottom=48.dp)) { value ->
                                        padding=value
                                        Box(Modifier.fillMaxSize().padding(value)) { Box(marker.modifier().fillMaxSize().background(Color.Red)) }
                                    }
                                }
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String) {
                fun point(name:String,probe:Probe)=ProbePoint(name,name,null,probe.geometry().filter{it.name=="box"||it.name=="size"})
                writer.submit(host.snapshot(id,id,listOf(point("outer",outer),point("inner",inner),point("screen",screen),point("marker",marker),
                    ProbePoint("padding","padding",null,listOf(
                        number("start",padding.calculateStartPadding(if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr).value.toDouble(),"dip",definition="insets.scaffold-start"),
                        number("top",padding.calculateTopPadding().value.toDouble(),"dip",definition="insets.scaffold-top"),
                        number("end",padding.calculateEndPadding(if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr).value.toDouble(),"dip",definition="insets.scaffold-end"),
                        number("bottom",padding.calculateBottomPadding().value.toDouble(),"dip",definition="insets.scaffold-bottom"))))))
            }
            sample("initial");if(!c.dynamic)return
            fun change(time:Long,id:String,action:()->Unit){host.advanceTo(time);action();host.draw();host.frame(time);sample(id)}
            change(1000,"ancestor"){union=WindowInsets(left=20.dp,top=4.dp,right=2.dp,bottom=50.dp)}
            change(17000,"outer"){outerInsets=WindowInsets(left=30.dp,top=20.dp,right=40.dp,bottom=10.dp)}
            change(33000,"inner"){innerInsets=WindowInsets(left=5.dp,top=8.dp,right=12.dp,bottom=16.dp)}
            change(49000,"direction"){rtl=!c.rtl}
            change(65000,"clear-consumption"){union=WindowInsets();consumed=PaddingValues()}
        }
    }
}

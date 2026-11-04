@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class ScrimConfig(val alpha:Float=1f,val color:String="default",val dismiss:Boolean=true,val dark:Boolean=false,val action:String="none")
@ParitySuite("scrim")
class ScrimCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer)
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter)=capture(writer)
    private suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<ScrimConfig>()
        var alpha by mutableStateOf(c.alpha); var color by mutableStateOf(c.color); var dismiss by mutableStateOf(c.dismiss)
        var clicks=0
        ComposeHost(writer,200,160).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(20.dp)) {
                        Box(Modifier.fillMaxSize().background(Color(0xFFFFD166))) {
                            Scrim(contentDescription="Close sheet",alpha={alpha},
                                color=when(color) { "default"->ScrimDefaults.color; "none"->Color.Unspecified; else->Color(color.removePrefix("#").toLong(16)) },
                                onClick=if(dismiss){{clicks++}}else null)
                        }
                    }
                }
            }; host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("state","state",null,
                listOf(number("clicks",clicks.toDouble(),"count",definition="scrim.clicks"))))))
            fun change(time:Long,id:String,action:()->Unit) { host.advanceTo(time); action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id) }
            sample("initial")
            when(c.action) {
                "none"->Unit
                "alpha"->{change(1000,"half"){alpha=.5f};change(2000,"zero"){alpha=0f};change(3000,"excess"){alpha=2f}}
                "color"->{change(1000,"absent"){color="none"};change(2000,"custom"){color="#70336699"};change(3000,"default"){color="default"}}
                else->{
                    change(1000,"down"){host.input("down",Offset(40f,40f))}
                    if(c.action=="disable-press")change(2000,"disabled"){dismiss=false}
                    else if(c.action!="tap")change(2000,"move"){host.input("move",if(c.action=="outside-return")Offset(190f,150f)else Offset(160f,100f))}
                    change(3000,"up"){host.input("move",Offset(40f,40f));host.input("up",Offset(40f,40f))}
                }
            }
        }
    }
}

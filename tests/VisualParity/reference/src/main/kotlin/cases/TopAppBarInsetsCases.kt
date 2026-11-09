@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
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

@Serializable data class TopAppBarInsetsConfig(val twoRows:Boolean,val rtl:Boolean=false,val consumed:Boolean=false,val nested:Boolean=true,val dynamic:Boolean=false,val search:Boolean=false)

@ParitySuite("top-app-bar-insets")
class TopAppBarInsetsCases {
    @ParityScenario("capture") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TopAppBarInsetsConfig>()
        val original=WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp)
        var insets by mutableStateOf(original)
        var consumed by mutableStateOf(if(c.consumed)WindowInsets(left=4.dp,top=3.dp,right=2.dp,bottom=6.dp) else WindowInsets())
        var rtl by mutableStateOf(c.rtl)
        val bar=Probe(); val names=if(c.twoRows)listOf("navigation","action","title","collapsed") else listOf("navigation","action","title")
        val probes=names.associateWith{Probe()}
        ComposeHost(writer,400,280).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).consumeWindowInsets(consumed)) {
                            val slot:@Composable (String,Color)->Unit = {name,color ->
                                Box(if(c.nested)Modifier.windowInsetsPadding(original) else Modifier) {
                                    Box(probes.getValue(name).modifier().size(24.dp).background(color))
                                }
                            }
                            val colors=TopAppBarDefaults.topAppBarColors(containerColor=Color.Cyan)
                            if(c.search) AppBarWithSearch(state=rememberSearchBarState(),inputField={slot("title",Color.Magenta)},
                                modifier=bar.modifier(),navigationIcon={slot("navigation",Color.Black)},actions={slot("action",Color.Red)},
                                windowInsets=insets,colors=SearchBarDefaults.appBarWithSearchColors(appBarContainerColor=Color.Cyan))
                            else if(c.twoRows) TwoRowsTopAppBar(title={expanded -> slot(if(expanded)"title" else "collapsed",if(expanded)Color.Magenta else Color.Blue)},
                                modifier=bar.modifier(),navigationIcon={slot("navigation",Color.Black)},actions={slot("action",Color.Red)},
                                windowInsets=insets,colors=colors)
                            else TopAppBar(title={slot("title",Color.Magenta)},modifier=bar.modifier(),
                                navigationIcon={slot("navigation",Color.Black)},actions={slot("action",Color.Red)},windowInsets=insets,colors=colors)
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) {
                fun point(name:String,probe:Probe)=ProbePoint(name,name,null,probe.geometry().filter{it.name=="box"||it.name=="size"})
                writer.submit(host.snapshot(id,id,listOf(point("bar",bar))+probes.map{point(it.key,it.value)}))
            }
            sample("initial");if(!c.dynamic)return
            fun change(time:Long,id:String,action:()->Unit){host.advanceTo(time);action();host.draw();host.frame(time);sample(id)}
            change(1000,"fully-consumed"){consumed=WindowInsets(left=30.dp,top=30.dp,right=30.dp,bottom=30.dp)}
            change(17000,"larger-insets"){insets=WindowInsets(left=40.dp,top=35.dp,right=32.dp,bottom=31.dp)}
            change(33000,"clear-parent"){consumed=WindowInsets()}
            change(49000,"direction"){rtl=!c.rtl}
        }
    }
}

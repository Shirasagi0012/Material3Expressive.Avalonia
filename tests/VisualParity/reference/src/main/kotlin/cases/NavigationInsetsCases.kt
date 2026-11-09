@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
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

@Serializable data class NavigationInsetsConfig(val kind:String="bar",val rtl:Boolean=false,val consumed:Boolean=false,val nested:Boolean=false,val dynamic:Boolean=false)

@ParitySuite("navigation-insets")
class NavigationInsetsCases {
    @ParityScenario("capture") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<NavigationInsetsConfig>()
        val original=WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp)
        var insets by mutableStateOf(original)
        var consumed by mutableStateOf(if(c.consumed)WindowInsets(left=4.dp,top=3.dp,right=2.dp,bottom=6.dp) else WindowInsets())
        var rtl by mutableStateOf(c.rtl)
        val container=Probe();val item=Probe();val icon=Probe()
        ComposeHost(writer,360,280).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).consumeWindowInsets(consumed)) {
                            val content:@Composable ()->Unit = {
                                Box(if(c.nested)Modifier.windowInsetsPadding(original) else Modifier) {
                                    Box(icon.modifier().size(24.dp).background(Color.Black))
                                }
                            }
                            when(c.kind) {
                                "bottom"->BottomAppBar(modifier=container.modifier(),containerColor=Color.Cyan,windowInsets=insets,
                                    actions={Box(item.modifier()){content()}})
                                "flexible"->FlexibleBottomAppBar(modifier=container.modifier(),containerColor=Color.Cyan,windowInsets=insets) {
                                    Box(item.modifier()){content()}
                                }
                                "wide","expanded"->WideNavigationRail(
                                    modifier=container.modifier(),colors=WideNavigationRailDefaults.colors(containerColor=Color.Cyan),windowInsets=insets,
                                    state=rememberWideNavigationRailState(if(c.kind=="expanded")WideNavigationRailValue.Expanded else WideNavigationRailValue.Collapsed)) {
                                    WideNavigationRailItem(false,{},icon=content,label=null,railExpanded=c.kind=="expanded",modifier=item.modifier())
                                }
                                "rail"->NavigationRail(container.modifier(),containerColor=Color.Cyan,windowInsets=insets) {
                                    NavigationRailItem(false,{},icon=content,modifier=item.modifier())
                                }
                                "short"->ShortNavigationBar(container.modifier(),containerColor=Color.Cyan,windowInsets=insets) {
                                    ShortNavigationBarItem(false,{},icon=content,label=null,modifier=item.modifier())
                                }
                                else->NavigationBar(container.modifier(),containerColor=Color.Cyan,windowInsets=insets) {
                                    NavigationBarItem(false,{},icon=content,modifier=item.modifier())
                                }
                            }
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) {
                fun point(name:String,probe:Probe)=ProbePoint(name,name,null,probe.geometry().filter{it.name=="box"||it.name=="size"})
                writer.submit(host.snapshot(id,id,listOf(point("container",container),point("item",item),point("icon",icon))))
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

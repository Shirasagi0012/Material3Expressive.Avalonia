@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
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

@Serializable
data class NavigationDrawerInsetsConfig(val kind:String="permanent",val rtl:Boolean=false,
    val consumption:String="parent",val nested:Boolean=true,val dynamic:Boolean=false)

@ParitySuite("navigation-drawer-insets")
class NavigationDrawerInsetsCases {
    @ParityScenario("capture")
    suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<NavigationDrawerInsetsConfig>()
        val original=WindowInsets(left=13.dp,top=7.dp,right=5.dp,bottom=11.dp)
        var requested by mutableStateOf(original)
        var consumed by mutableStateOf(if(c.consumption=="none")WindowInsets() else WindowInsets(left=4.dp,top=3.dp,right=2.dp,bottom=6.dp))
        var rtl by mutableStateOf(c.rtl)
        var replaced by mutableStateOf(false)
        val sheet=Probe();val content=Probe();val marker=Probe()
        ComposeHost(writer,520,320).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        val parentPadding=if(c.consumption=="mixed")PaddingValues(start=3.dp,top=2.dp,end=7.dp,bottom=1.dp) else PaddingValues(0.dp)
                        Box(Modifier.fillMaxSize().background(Color.White).consumeWindowInsets(parentPadding).consumeWindowInsets(consumed)) {
                            val state=rememberDrawerState(DrawerValue.Open)
                            val drawer:@Composable ()->Unit = {
                                var modifier=sheet.modifier().width(280.dp)
                                if(c.consumption=="self") modifier=modifier
                                    .consumeWindowInsets(PaddingValues(start=3.dp,top=5.dp,end=9.dp,bottom=7.dp))
                                    .consumeWindowInsets(WindowInsets(left=12.dp,top=8.dp,right=15.dp,bottom=10.dp))
                                val body:@Composable ColumnScope.()->Unit = {
                                    key(replaced) {
                                        val nestedInsets=if(replaced)WindowInsets(left=48.dp,top=38.dp,right=50.dp,bottom=42.dp) else original
                                        if(c.nested || replaced) Box(content.modifier().fillMaxWidth().windowInsetsPadding(nestedInsets)) {
                                            Box(marker.modifier().fillMaxWidth().height(56.dp).background(Color.Black))
                                        } else Box(content.modifier().then(marker.modifier()).fillMaxWidth().height(56.dp).background(Color.Black))
                                    }
                                }
                                when(c.kind) {
                                    "modal"->ModalDrawerSheet(state,modifier,drawerContainerColor=Color.Cyan,drawerTonalElevation=0.dp,windowInsets=requested,content=body)
                                    "dismissible"->DismissibleDrawerSheet(state,modifier,drawerContainerColor=Color.Cyan,drawerTonalElevation=0.dp,windowInsets=requested,content=body)
                                    else->PermanentDrawerSheet(modifier,drawerContainerColor=Color.Cyan,drawerTonalElevation=0.dp,windowInsets=requested,content=body)
                                }
                            }
                            val page:@Composable ()->Unit={Box(Modifier.fillMaxSize())}
                            when(c.kind) {
                                "modal"->ModalNavigationDrawer(drawer,drawerState=state,content=page)
                                "dismissible"->DismissibleNavigationDrawer(drawer,drawerState=state,content=page)
                                else->PermanentNavigationDrawer(drawer,content=page)
                            }
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) {
                fun point(name:String,probe:Probe)=ProbePoint(name,name,null,probe.geometry().filter{it.name=="box"||it.name=="size"})
                writer.submit(host.snapshot(id,id,listOf(point("sheet",sheet),point("content",content),point("marker",marker))))
            }
            sample("initial");if(!c.dynamic)return
            fun change(time:Long,id:String,action:()->Unit){host.advanceTo(time);action();host.draw();host.frame(time);sample(id)}
            change(1000,"fully-consumed"){consumed=WindowInsets(left=30.dp,top=30.dp,right=30.dp,bottom=30.dp)}
            change(17000,"larger-insets"){requested=WindowInsets(left=40.dp,top=35.dp,right=32.dp,bottom=31.dp)}
            change(33000,"clear-parent"){consumed=WindowInsets()}
            change(49000,"direction"){rtl=!c.rtl}
            change(65000,"replace-content"){replaced=true}
        }
    }
}

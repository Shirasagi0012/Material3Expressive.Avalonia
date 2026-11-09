@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class MenuArrangementConfig(val kind:String="selectable", val arrangement:String="center",
    val rtl:Boolean=false, val leading:Boolean=true, val trailing:Boolean=true, val selectedIcon:Boolean=false,
    val width:Double=200.0, val action:String="none", val dark:Boolean=false,val native:Boolean=false)

@ParitySuite("menu-arrangement")
class MenuArrangementCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<MenuArrangementConfig>()
        ComposeHost(writer,320,160).use { host ->
            var selected by mutableStateOf(false)
            var leading by mutableStateOf(config.leading)
            var trailing by mutableStateOf(config.trailing)
            var selectedIcon by mutableStateOf(config.selectedIcon)
            var arrangementName by mutableStateOf(config.arrangement)
            var clicks=0
            val itemProbe=Probe();val contentProbe=Probe()
            host.mount {
                MaterialTheme(colorScheme=if(config.dark)darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,
                        LocalContentColor provides MaterialTheme.colorScheme.onSurface) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            Box(Modifier.width(config.width.toFloat().dp)) {
                                val arrangement=when(arrangementName) {
                                    "start"->Arrangement.Start; "end"->Arrangement.End; "center"->Arrangement.Center
                                    "spacebetween"->Arrangement.SpaceBetween;"spacearound"->Arrangement.SpaceAround;"spaceevenly"->Arrangement.SpaceEvenly
                                    "spaced-center"->Arrangement.spacedBy(7.25.dp,Alignment.CenterHorizontally)
                                    "overlap"->Arrangement.spacedBy((-5.25).dp,Alignment.CenterHorizontally)
                                    "absolute.spaceevenly"->Arrangement.Absolute.SpaceEvenly
                                    "absolute-spaced"->Arrangement.Absolute.spacedBy(7.25.dp,Alignment.End)
                                    "custom"->MenuInsetArrangement
                                    else->MenuDefaults.DropdownMenuItemHorizontalArrangement
                                }
                                val text:@Composable ()->Unit={Box(contentProbe.modifier().size(48.dp,16.dp).background(LocalContentColor.current))}
                                val icon:@Composable (() -> Unit)?=if(leading) ({Box(Modifier.size(20.dp).background(LocalContentColor.current))}) else null
                                val check:@Composable (() -> Unit)?=if(selectedIcon) ({Box(Modifier.size(20.dp).background(LocalContentColor.current))}) else null
                                val tail:@Composable (() -> Unit)?=if(trailing) ({Box(Modifier.size(12.dp).background(LocalContentColor.current))}) else null
                                when(config.kind) {
                                    "ordinary"->DropdownMenuItem(onClick={clicks++},text=text,shape=MenuDefaults.itemShapes().shape,modifier=itemProbe.modifier(),
                                        leadingIcon=icon,trailingContent=tail,horizontalArrangement=arrangement)
                                    "checkable"->CheckableDropdownMenuItem(checked=selected,onCheckedChange={clicks++},text=text,shapes=MenuDefaults.itemShapes(),modifier=itemProbe.modifier(),
                                        leadingIcon=icon,checkedLeadingIcon=check,trailingContent=tail,horizontalArrangement=arrangement)
                                    else->SelectableDropdownMenuItem(selected=selected,onClick={clicks++},text=text,shapes=MenuDefaults.itemShapes(),modifier=itemProbe.modifier(),
                                        leadingIcon=icon,selectedLeadingIcon=check,trailingContent=tail,horizontalArrangement=arrangement)
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("content","content",null,contentProbe.geometry().filter{it.name=="box" || it.name=="size"}),
                ProbePoint("item","item",null,listOf(number("clicks",clicks.toDouble(),"count",definition="menu.click-count")))),true))
            sample("initial");if(!dynamic)return
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun times(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();host.draw();host.frame(time);sample(id)}
            when(config.action){
                "selection"->{
                    change(1000,"selected"){selected=true};times(16000,32000,64000)
                    change(65000,"reversed"){selected=false};times(80000,160000,400000)
                }
                "arrangement"->{
                    change(1000,"end"){arrangementName="end"};times(16000)
                    change(17000,"absolute"){arrangementName="absolute.spaceevenly"};times(32000)
                    change(33000,"default"){arrangementName="default"};times(48000)
                }
                "slots"->{
                    change(1000,"selected"){if(config.kind!="ordinary")selected=true};times(16000,64000)
                    change(65000,"leading"){leading=true};times(80000)
                    change(81000,"remove-trailing"){trailing=false};times(96000)
                    change(97000,"remove-leading"){leading=false;selectedIcon=false};times(112000)
                }
                "press"->{
                    val c=checkNotNull(itemProbe.coordinates)
                    val center=c.localToRoot(Offset(c.size.width/2f,c.size.height/2f))/ComposeHost.RenderScale.toFloat()
                    advance(1000);host.input("move",center);host.draw();host.frame(1000)
                    advance(2000);host.input("down",center);host.draw();host.frame(2000);sample("pressed");times(16000,64000)
                    advance(65000);host.input("up",center);host.draw();host.frame(65000);sample("released");times(80000,240000,400000)
                }
            }
        }
    }
}
private object MenuInsetArrangement : Arrangement.Horizontal {
    override val spacing=3.25.dp
    override fun Density.arrange(totalSize:Int,sizes:IntArray,layoutDirection:LayoutDirection,outPositions:IntArray){
        val inset=5.dp.roundToPx()
        with(Arrangement.spacedBy(spacing,Alignment.CenterHorizontally)){arrange(totalSize-2*inset,sizes,layoutDirection,outPositions)}
        for(i in outPositions.indices)outPositions[i]+=inset
    }
}

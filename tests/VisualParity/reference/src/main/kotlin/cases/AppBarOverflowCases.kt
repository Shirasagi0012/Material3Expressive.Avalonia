@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class AppBarOverflowConfig(val vertical:Boolean=false,val rtl:Boolean=false,val space:Int=128,
    val maximum:Int=Int.MAX_VALUE,val action:String="none",val dark:Boolean=false)

@ParitySuite("appbar-overflow")
class AppBarOverflowCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)

    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<AppBarOverflowConfig>()
        ComposeHost(writer,320,280).use {host ->
            var space by mutableStateOf(config.space)
            var clicks=0
            lateinit var menu:AppBarMenuState
            val bar=Probe();val indicator=Probe();val menuItems=List(3){Probe()}
            host.mount {
                MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft) {
                            val modifier=Modifier.absoluteOffset(48.dp,32.dp).then(
                                if(config.vertical)Modifier.heightIn(max=space.dp)else Modifier.widthIn(max=space.dp)).then(bar.modifier())
                            val overflow:@Composable (AppBarMenuState)->Unit={state ->
                                menu=state
                                Box(indicator.modifier().size(32.dp,40.dp).background(Color(0xff008000)).clickable(interactionSource=null,indication=null){
                                    if(state.isShowing)state.dismiss()else state.show()
                                })
                            }
                            val content:AppBarScope.()->Unit={
                                listOf(40 to 24,56 to 32,32 to 40).forEachIndexed {index,size ->
                                    customItem(appbarContent={Box(Modifier.size(size.first.dp,size.second.dp).background(Color.Blue))},menuContent={state ->
                                        DropdownMenuItem(modifier=menuItems[index].modifier(),text={Box(Modifier.size(48.dp,16.dp).background(LocalContentColor.current))},
                                            onClick={clicks++;state.dismiss()})
                                    })
                                }
                            }
                            if(config.vertical)AppBarColumn(modifier,overflow,config.maximum){content()}
                            else AppBarRow(modifier,overflow,config.maximum){content()}
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("bar","container",null,bar.geometry().filter{it.name=="box"||it.name=="size"}),
                ProbePoint("state","state",null,listOf(flag("expanded",menu.isShowing,definition="appbar.menu.showing"),
                    number("clicks",clicks.toDouble(),"count",definition="appbar.menu.clicks"))))))
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            fun times(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            fun click(probe:Probe){val coordinates=checkNotNull(probe.coordinates)
                val center=coordinates.localToWindow(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
                host.input("move",center);host.input("down",center);host.input("up",center)}
            sample("initial");if(!dynamic)return
            if(config.action=="resize"){
                change(1000,"all-visible"){space=160};change(2000,"none-visible"){space=20};change(3000,"restore"){space=config.space};return
            }
            change(1000,"open"){click(indicator)};times(16000,32000,64000)
            if(config.action=="reverse"){
                change(65000,"close"){menu.dismiss()};times(80000,96000)
                change(97000,"reopen"){menu.show()};times(112000,224000,800000)
            }else{
                times(224000,800000)
                if(config.action=="outside")change(801000,"outside"){
                    host.input("move",Offset(8f,8f));host.input("down",Offset(8f,8f));host.input("up",Offset(8f,8f))
                }else change(801000,"select"){click(menuItems.last())}
                times(816000,864000,960000,1200000)
            }
        }
    }
}

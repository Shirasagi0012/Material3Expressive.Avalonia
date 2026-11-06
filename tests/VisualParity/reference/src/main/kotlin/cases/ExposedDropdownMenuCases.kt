@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.focusable
import androidx.compose.foundation.text.input.rememberTextFieldState
import androidx.compose.foundation.text.input.TextFieldLineLimits
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class ExposedMenuConfig(val x:Int=80,val y:Int=64,val width:Int=160,val count:Int=3,
    val editable:Boolean=false,val rtl:Boolean=false,val match:Boolean=true,val dark:Boolean=false,val action:String="none",val menuWidth:Double?=null,val menuHeight:Double?=null,val minHeight:Double=0.0,val field:Boolean=false,val legacyField:Boolean=true)

@ParitySuite("exposed-dropdown-menu")
class ExposedDropdownMenuCases {
    @ParityScenario("menu") suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<ExposedMenuConfig>()
        ComposeHost(writer,400,320).use {host ->
            var expanded by mutableStateOf(config.action !in setOf("show","reverse","click"))
            var width by mutableIntStateOf(config.width)
            var y by mutableIntStateOf(config.y)
            var match by mutableStateOf(config.match)
            var requests=0
            val menu=Probe()
            var focused=false
            host.mount {
                MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                        Box(Modifier.absoluteOffset(config.x.dp,y.dp)) {
                            CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                                ExposedDropdownMenuBox(expanded=expanded,onExpandedChange={requests++;expanded=it},modifier=Modifier.size(width.dp,(if(config.field)96 else 56).dp)) {
                                    val anchorModifier=Modifier.fillMaxSize().menuAnchor(if(config.editable)ExposedDropdownMenuAnchorType.PrimaryEditable else ExposedDropdownMenuAnchorType.PrimaryNotEditable)
                                    if(config.field && !config.legacyField) TextField(state=rememberTextFieldState(),lineLimits=TextFieldLineLimits.SingleLine,readOnly=!config.editable,
                                        label={Box(Modifier.size(40.dp,12.dp).background(Color.Red))},colors=TextFieldDefaults.colors(cursorColor=Color.Transparent),
                                        modifier=anchorModifier.onFocusChanged{focused=it.isFocused})
                                    else if(config.field) TextField(value="",onValueChange={},singleLine=true,readOnly=!config.editable,
                                        label={Box(Modifier.size(40.dp,12.dp).background(Color.Red))},colors=TextFieldDefaults.colors(cursorColor=Color.Transparent),
                                        modifier=anchorModifier.onFocusChanged{focused=it.isFocused})
                                    else Box(anchorModifier.background(Color.Blue).focusable())
                                    var menuModifier=menu.modifier()
                                    config.menuWidth?.let{menuModifier=menuModifier.width(it.toFloat().dp)}
                                    config.menuHeight?.let{menuModifier=menuModifier.height(it.toFloat().dp)}
                                    menuModifier=menuModifier.heightIn(min=config.minHeight.toFloat().dp)
                                    ExposedDropdownMenu(expanded=expanded,onDismissRequest={requests++;expanded=false},modifier=menuModifier,matchAnchorWidth=match) {
                                        repeat(config.count) { DropdownMenuItem(text={Box(Modifier.size(48.dp,16.dp).background(Color.Red))},onClick={}) }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            var next=16000L
            fun advance(t:Long){while(next<=t){host.frame(next);next+=16000};host.advanceTo(t)}
            fun sample(id:String) {
                val presented=menu.coordinates?.isAttached==true
                val points=mutableListOf(ProbePoint("state","state",null,listOf(flag("expanded",expanded,definition="exposed.expanded"),
                    flag("presented",presented,definition="exposed.presented"),number("requests",requests.toDouble(),"count",definition="exposed.requests"))))
                if(config.field)points+=ProbePoint("field","field",null,listOf(flag("focused",focused,definition="exposed.field-focus")))
                if(presented) {
                    val size=checkNotNull(menu.coordinates).size
                    points+=ProbePoint("menu","popup",null,listOf(number("width",size.width.toDouble()/ComposeHost.RenderScale,"dp",category="geometry",definition="exposed.width"),
                        number("height",size.height.toDouble()/ComposeHost.RenderScale,"dp",category="geometry",definition="exposed.height")))
                }
                writer.submit(host.snapshot(id,id,points))
            }
            val origin=if(expanded)800000L else 0L
            advance(origin);host.draw();sample("initial")
            fun change(t:Long,id:String,action:()->Unit){advance(origin+t);action();writer.record(origin+t,"mutation",id);host.draw();host.frame(origin+t);sample(id)}
            fun times(vararg values:Long){values.forEach{advance(origin+it);host.draw();sample("time-$it")}}
            when(config.action) {
                "show","reverse"->{change(1000,"open"){expanded=true};times(16000,32000,64000)
                    if(config.action=="reverse"){change(65000,"close"){expanded=false};times(80000,96000);change(97000,"reopen"){expanded=true}}
                    times(112000,224000,800000);change(801000,"final-close"){expanded=false};times(816000,864000,960000,1200000)}
                "resize"->{change(1000,"resize"){width=240};times(16000,800000)}
                "move"->{change(1000,"move"){y=230};times(16000,800000)}
                "match"->{change(1000,"intrinsic"){match=false};times(16000,800000)}
                "outside"->{change(1000,"outside"){host.input("move",Offset(8f,8f));host.input("down",Offset(8f,8f))};change(2000,"release"){host.input("up",Offset(8f,8f))};times(16000,800000)}
                "click"->{val point=Offset(config.x+20f,config.y+20f);change(1000,"press"){host.input("move",point);host.input("down",point)}
                    change(2000,"release"){host.input("up",point)};times(16000,64000,800000)}
            }
        }
    }
}

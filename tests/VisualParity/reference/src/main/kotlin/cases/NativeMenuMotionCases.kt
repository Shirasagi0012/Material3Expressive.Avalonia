@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.AbsoluteAlignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class NativeMenuMotionConfig(val baseline:Boolean=false,val rtl:Boolean=false,val action:String="show")

@ParitySuite("native-menu-motion")
class NativeMenuMotionCases {
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<NativeMenuMotionConfig>()
        ComposeHost(writer,320,240).use { host ->
            var expanded by mutableStateOf(false)
            var attached by mutableStateOf(true)
            var anchorIndex by mutableStateOf(0)
            var calls=0
            val marker=Probe()
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft) {
                            if(attached) key(anchorIndex) {
                                val x=if(anchorIndex==1)48 else if(config.action=="edge")292 else 112
                                val y=if(anchorIndex==1)120 else if(config.action=="edge")208 else 64
                                Box(Modifier.absoluteOffset(x.dp,y.dp).size(24.dp).background(Color.Blue)) {
                                    val label:@Composable ()->Unit={val color=LocalContentColor.current;Canvas(marker.modifier().size(48.dp,16.dp)){drawRect(color)}}
                                    val click:()->Unit={calls++;expanded=false}
                                    if(config.baseline) DropdownMenu(expanded=expanded,onDismissRequest={expanded=false},shadowElevation=0.dp) {
                                        DropdownMenuItem(text=label,onClick=click)
                                    } else DropdownMenuPopup(expanded=expanded,onDismissRequest={expanded=false}) {
                                        DropdownMenuGroup(shapes=MenuDefaults.groupShapes(),shadowElevation=0.dp) {
                                            SelectableDropdownMenuItem(selected=false,text=label,onClick=click,shapes=MenuDefaults.itemShapes())
                                        }
                                    }
                                }
                            }
                            if(anchorIndex==1) Box(Modifier.absoluteOffset(112.dp,64.dp).size(24.dp).background(Color.Blue))
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            fun appearance():Pair<Float,Float> {
                var parent=marker.coordinates
                while(parent!=null) {
                    var coordinator:Any?=parent
                    while(coordinator!=null) {
                        val layer=field(coordinator,"layer")
                        if(layer!=null) {
                            val graphics=checkNotNull(field(layer,"graphicsLayer"))
                            fun value(name:String)=graphics.javaClass.methods.single{it.name==name&&it.parameterCount==0}.invoke(graphics) as Float
                            val scale=value("getScaleX");val alpha=value("getAlpha")
                            if(scale!=1f || alpha!=1f)return scale to alpha
                        }
                        coordinator=field(coordinator,"wrappedBy")
                    }
                    parent=parent.parentLayoutCoordinates
                }
                return 1f to 1f
            }
            fun sample(id:String) {
                val visible=marker.coordinates?.isAttached==true
                val points=mutableListOf(ProbePoint("state","state",null,listOf(
                    flag("expanded",expanded,definition="dropdown.expanded"),flag("presented",visible,definition="dropdown.content-presented"),
                    number("activations",calls.toDouble(),"count",definition="menu.activations"))))
                if(visible) {
                    points+=ProbePoint("content","content",null,marker.geometry().filter{it.name=="box"||it.name=="size"})
                    val(scale,alpha)=appearance()
                    points+=ProbePoint("appearance","popup",null,listOf(number("scale",scale.toDouble(),"none",category="motion",definition="dropdown.appearance.scale"),
                        number("alpha",alpha.toDouble(),"none",category="motion",definition="dropdown.appearance.alpha")))
                }
                writer.submit(host.snapshot(id,id,points))
            }
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.scheduler.runCurrent();host.draw()}
            fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            sample("initial")
            change(1000,"open"){expanded=true};at(16000,32000,64000)
            if(config.action=="reverse") {
                change(65000,"close"){expanded=false};at(80000,96000)
                change(97000,"reopen"){expanded=true};at(112000,160000,400000,800000)
            } else {
                at(160000,400000,800000)
                change(801000,config.action) {
                    when(config.action) {
                        "command"->{
                            val c=checkNotNull(marker.coordinates)
                            val point=c.localToWindow(Offset(c.size.width/2f,c.size.height/2f))/ComposeHost.RenderScale.toFloat()
                            host.input("move",point);host.input("down",point);host.input("up",point)
                        }
                        "escape"->{host.scene.sendKeyEvent(KeyEvent(Key.Escape,KeyEventType.KeyDown));host.scene.sendKeyEvent(KeyEvent(Key.Escape,KeyEventType.KeyUp))}
                        "outside"->{host.input("move",Offset(8f,8f));host.input("down",Offset(8f,8f));host.input("up",Offset(8f,8f))}
                        "unload"->attached=false
                        "new-anchor"->anchorIndex=1
                        else->expanded=false
                    }
                }
                at(816000,832000,864000,960000,1200000)
            }
        }
    }
}

@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
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

@Serializable data class DropdownMenuConfig(val grouped:Boolean=false,val rtl:Boolean=false,val dark:Boolean=false,
    val x:Int=112,val y:Int=64,val action:String="none",val noShadow:Boolean=false,val divider:Boolean=false)

@ParitySuite("dropdown-menu")
class DropdownMenuCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<DropdownMenuConfig>()
        ComposeHost(writer,320,240).use {host ->
            val scene=Scene(config)
            host.mount {scene.Content()};host.frame(-1000);host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            val origin=if(scene.expanded)800000L else 0L
            advance(origin);host.draw();sample("initial");if(!dynamic)return
            fun change(time:Long,id:String,action:()->Unit){advance(origin+time);action();writer.record(origin+time,"mutation",id);host.draw();host.frame(origin+time);sample(id)}
            fun times(vararg times:Long){times.forEach{advance(origin+it);host.draw();sample("time-$it")}}
            when(config.action){
                "show","reverse"->{
                    change(1000,"open"){scene.expanded=true};times(16000,32000,64000)
                    if(config.action=="reverse"){
                        change(65000,"close"){scene.expanded=false};times(80000,96000)
                        change(97000,"reopen"){scene.expanded=true};times(112000,224000,800000)
                    }else{
                        times(224000,800000);change(801000,"close"){scene.expanded=false}
                        times(816000,864000,960000,1200000)
                    }
                }
                "outside"->{change(1000,"outside-down"){host.input("move",Offset(8f,8f));host.input("down",Offset(8f,8f))}
                    change(2000,"outside-up"){host.input("up",Offset(8f,8f))};times(16000)}
                "selection"->{change(1000,"selected"){scene.selected=true};times(16000,32000,64000)
                    change(65000,"reversed"){scene.selected=false};times(80000,160000,400000,800000)}
                "hover"->{
                    val center=checkNotNull(scene.content.coordinates).localToWindow(Offset(24f,8f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
                    change(1000,"enter"){host.input("move",center)};times(16000,80000)
                    change(81000,"exit"){host.input("move",Offset(8f,8f))};times(96000,160000,400000)
                }
            }
        }
    }
    private class Scene(val config:DropdownMenuConfig){
        var expanded by mutableStateOf(config.action !in setOf("show","reverse"))
        var selected by mutableStateOf(false)
        var requests=0
        val content=Probe()
        val selectedIcon=Probe()
        val divider=Probe()
        @Composable fun Content(){
            MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()){
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr){
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft){
                        Box(Modifier.absoluteOffset(config.x.dp,config.y.dp).size(24.dp).background(Color.Blue)){
                            val label:@Composable ()->Unit={Box(content.modifier().size(48.dp,16.dp).background(LocalContentColor.current))}
                            val trailing:@Composable ()->Unit={Box(Modifier.size(16.dp).background(LocalContentColor.current))}
                            if(config.grouped)DropdownMenuPopup(expanded=expanded,onDismissRequest={requests++}){
                                DropdownMenuGroup(shapes=MenuDefaults.groupShapes(),shadowElevation=if(config.noShadow)0.dp else MenuDefaults.ShadowElevation){
                                    CheckableDropdownMenuItem(checked=selected,onCheckedChange={},text=label,shapes=MenuDefaults.itemShapes(),
                                        checkedLeadingIcon={Box(selectedIcon.modifier().size(20.dp).background(LocalContentColor.current))},
                                        supportingText={Box(Modifier.size(32.dp,12.dp).background(LocalContentColor.current))},trailingContent=trailing)
                                    if(config.divider)HorizontalDivider(divider.modifier())
                                }
                            }else DropdownMenu(expanded=expanded,onDismissRequest={requests++},shadowElevation=if(config.noShadow)0.dp else MenuDefaults.ShadowElevation){
                                DropdownMenuItem(text=label,onClick={},trailingIcon=trailing)
                                if(config.divider)HorizontalDivider(divider.modifier())
                            }
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint>{
            val presented=content.coordinates?.isAttached==true
            val points=mutableListOf(ProbePoint("state","state",null,listOf(
                flag("expanded",expanded,definition="dropdown.expanded"),flag("presented",presented,definition="dropdown.content-presented"),
                flag("selected",selected,definition="menu.selected"),number("requests",requests.toDouble(),"count",definition="dropdown.dismiss-requests"))))
            if(config.action=="selection")points+=ProbePoint("selected-icon","content",null,listOf(
                flag("presented",selectedIcon.coordinates?.isAttached==true,definition="menu.selected-icon-presented")))
            if(presented){
                if(config.divider)points+=ProbePoint("divider","divider",null,divider.geometry().filter{it.name=="box"||it.name=="size"})
                points+=ProbePoint("content","content",null,content.geometry().filter{it.name=="size"||config.action=="selection"&&it.name=="box"})
                val (scale,alpha)=appearance()
                points+=ProbePoint("appearance","popup",null,listOf(
                    number("scale",scale.toDouble(),"none",category="motion",definition="dropdown.appearance.scale"),
                    number("alpha",alpha.toDouble(),"none",category="motion",definition="dropdown.appearance.alpha")))
            }
            return points
        }
        // Read the actual graphics layer around the content, not an independently
        // calculated spring. Surface layers have identity scale/alpha; appearance owns
        // the nonidentity ancestor. This also observes interruption timing directly.
        private fun appearance():Pair<Float,Float>{
            fun field(owner:Any,name:String):Any? {
                var type:Class<*>?=owner.javaClass
                while(type!=null){
                    try{return type.getDeclaredField(name).apply{isAccessible=true}.get(owner)}
                    catch(_:NoSuchFieldException){type=type.superclass}
                }
                error("Missing $name on ${owner.javaClass.name}")
            }
            var parent=content.coordinates
            while(parent!=null){
                var coordinator:Any?=parent
                while(coordinator!=null){
                    val layer=field(coordinator,"layer")
                    if(layer!=null){
                        val graphics=checkNotNull(field(layer,"graphicsLayer"))
                        fun value(name:String)=graphics.javaClass.methods.single{it.name==name&&it.parameterCount==0}.invoke(graphics) as Float
                        val scale=value("getScaleX");val alpha=value("getAlpha")
                        if(scale!=1f||alpha!=1f)return scale to alpha
                    }
                    coordinator=field(coordinator,"wrappedBy")
                }
                parent=parent.parentLayoutCoordinates
            }
            return 1f to 1f
        }
    }
}

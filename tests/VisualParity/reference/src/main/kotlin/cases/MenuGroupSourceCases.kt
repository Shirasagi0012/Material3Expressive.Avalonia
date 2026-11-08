@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.*
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class MenuGroupSourceConfig(val action:String="overlap",val path:String="none",val rtl:Boolean=false)
@ParitySuite("menu-group-source")
class MenuGroupSourceCases {
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<MenuGroupSourceConfig>()
        ComposeHost(writer,240,160).use {host ->
            var source by mutableStateOf(MutableInteractionSource())
            var mounted by mutableStateOf(true)
            var path by mutableStateOf(c.path)
            var height by mutableStateOf(64)
            val probe=Probe()
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                  CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val defaults=MenuDefaults.groupShapes()
                        val shapes=if(path=="none")defaults else MenuGroupShapes(CutCornerShape(24.dp,4.dp,16.dp,2.dp),
                            when(path){"cut"->CutCornerShape(15);"generic"->NotchedShape;else->defaults.inactiveShape})
                        if(mounted) DropdownMenuGroup(shapes=shapes,modifier=probe.modifier(),shadowElevation=0.dp,interactionSource=source) {
                            Box(Modifier.size(180.dp,height.dp),contentAlignment=Alignment.Center) { Box(Modifier.size(32.dp,16.dp).background(Color.Blue)) }
                        }
                    }
                  }
                }
            }
            host.frame(-1000);host.frame(0)
            var next=16000L
            fun advance(t:Long){while(next<=t){host.frame(next);next+=16000};host.advanceTo(t)}
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("group","group",null,probe.geometry().filter{it.name in listOf("box","size")}))))
            fun times(vararg ts:Long){ts.forEach{advance(it);host.draw();sample("time-$it")}}
            fun mutate(t:Long,id:String,sampleNow:Boolean=true,action:()->Unit){advance(t);action();writer.record(t,"mutation",id);host.draw();host.frame(t);if(sampleNow)sample(id)}
            val first=HoverInteraction.Enter();val second=HoverInteraction.Enter()
            sample("initial")
            if(c.action=="native") {
                mutate(1000,"enter"){host.input("move",Offset(120f,80f))};times(16000,64000)
                mutate(65000,"exit"){host.input("move",Offset(8f,8f))};times(80000,160000,400000)
                mutate(401000,"enter-again"){host.input("move",Offset(120f,80f))};times(416000,480000,800000);return
            }
            mutate(1000,"hover"){source.tryEmit(first);if(c.action=="transient")source.tryEmit(HoverInteraction.Exit(first))};times(16000,64000)
            when(c.action) {
                "resize"->{
                    mutate(65000,"exit"){source.tryEmit(HoverInteraction.Exit(first))};times(80000,96000)
                    mutate(97000,"grow"){height=96};times(112000,160000)
                    mutate(161000,"shrink"){height=40}
                }
                "configure"->{
                    mutate(65000,"exit"){source.tryEmit(HoverInteraction.Exit(first))};times(80000,96000)
                    mutate(97000,"mixed"){path="mixed"};times(112000)
                    mutate(113000,"generic"){path="generic"};times(128000)
                    mutate(129000,"cut"){path="cut"};times(144000)
                    mutate(145000,"clear"){path="none"}
                }
                "replace"->{
                    val old=source;val replacement=MutableInteractionSource();replacement.tryEmit(second)
                    mutate(65000,"replace"){source=replacement};times(80000,160000)
                    mutate(161000,"old-exit"){old.tryEmit(HoverInteraction.Exit(first))};times(176000,240000)
                    mutate(241000,"new-nonhover"){source.tryEmit(DragInteraction.Start())}
                }
                "reattach"->{
                    mutate(65000,"detach",false){mounted=false};mutate(66000,"unmounted-hover",false){source.tryEmit(second)}
                    mutate(81000,"reattach"){mounted=true};times(96000,160000)
                    mutate(161000,"old-exit"){source.tryEmit(HoverInteraction.Exit(second))}
                }
                "overlap"->{
                    mutate(65000,"second"){source.tryEmit(second)};mutate(66000,"first-exit"){source.tryEmit(HoverInteraction.Exit(first))};times(80000,160000)
                    mutate(161000,"second-exit"){source.tryEmit(HoverInteraction.Exit(second))}
                }
                "reverse"->{
                    mutate(65000,"exit"){source.tryEmit(HoverInteraction.Exit(first))};times(80000,96000)
                    mutate(97000,"reverse"){source.tryEmit(second)};times(112000,160000)
                    mutate(161000,"exit-again"){source.tryEmit(HoverInteraction.Exit(second))}
                }
            }
            times(256000,320000,400000,800000)
        }
    }
    private object NotchedShape:Shape {
        override fun createOutline(size:Size,layoutDirection:LayoutDirection,density:Density):Outline=Outline.Generic(Path().apply {
            val inset=with(density){12.dp.toPx()};moveTo(0f,0f);lineTo(size.width,0f);lineTo(size.width-inset,size.height/2)
            lineTo(size.width,size.height);lineTo(0f,size.height);close()
        })
    }
}

@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class DragHandleConfig(val dark:Boolean=false,val rtl:Boolean=false,val appearance:String="default",
    val width:Double?=null,val height:Double?=null,val minimum:Double=48.0,val mutation:String="none")

@ParitySuite("drag-handle") class DragHandleCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<DragHandleConfig>()
        ComposeHost(writer,240,180).use {host ->
            val s=Scene(config);host.mount {s.Content()};host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure()))
            sample("initial");if(!dynamic)return
            var nextFrame=16000L
            fun advance(time:Long) {while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.frame(time);sample(id)}
            fun times(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            val c=checkNotNull(s.probe.coordinates)
            val center=c.localToWindow(Offset(c.size.width/2f,c.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val source=s.source
            when(config.mutation) {
                "press","cancel" -> {
                    change(1000,"down"){host.input("move",center);host.input("down",center)};times(16000,64000,160000)
                    if(config.mutation=="cancel") change(161000,"outside"){host.input("move",Offset(4f,4f))}
                    change(162000,"up"){host.input("up",if(config.mutation=="cancel") Offset(4f,4f) else center)};times(176000,240000,400000)
                }
                "drag" -> {
                    val first=DragInteraction.Start();val second=DragInteraction.Start()
                    change(1000,"start"){source.tryEmit(first)};times(16000,64000)
                    change(65000,"overlap"){source.tryEmit(second)}
                    change(66000,"stop-first"){source.tryEmit(DragInteraction.Stop(first))};times(80000)
                    change(81000,"cancel-last"){source.tryEmit(DragInteraction.Cancel(second))};times(96000,160000,320000)
                }
                "source" -> {
                    val drag=DragInteraction.Start()
                    change(1000,"start"){source.tryEmit(drag)};times(16000,64000)
                    val replacement=MutableInteractionSource()
                    change(65000,"replace"){s.source=replacement}
                    change(66000,"old-stop"){source.tryEmit(DragInteraction.Stop(drag))}
                    change(67000,"new-hover"){replacement.tryEmit(HoverInteraction.Enter())};times(80000,160000,320000)
                }
                "hover" -> {
                    change(1000,"enter"){host.input("move",center)};times(16000,64000,160000)
                    change(161000,"leave"){host.input("move",Offset(4f,4f))};times(176000,240000,400000)
                }
                "theme" -> {change(1000,"dark"){s.dark=true};change(2000,"light"){s.dark=false}}
                "configuration" -> {change(1000,"custom"){s.appearance="custom"};change(2000,"default"){s.appearance="default"}}
                "external-press" -> {
                    val press=PressInteraction.Press(Offset(2f,24f)*ComposeHost.RenderScale.toFloat())
                    change(1000,"source-down"){source.tryEmit(press)};times(16000,64000,160000)
                    change(161000,"source-up"){source.tryEmit(PressInteraction.Release(press))};times(176000,240000,400000)
                }
            }
        }
    }
    private class Scene(val config:DragHandleConfig) {
        var dark by mutableStateOf(config.dark);var appearance by mutableStateOf(config.appearance)
        var source by mutableStateOf(MutableInteractionSource())
        val probe=Probe();var presses=0
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark)darkColorScheme()else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,
                    LocalContentColor provides MaterialTheme.colorScheme.onSurface,
                    LocalMinimumInteractiveComponentSize provides config.minimum.toFloat().dp) {
                    LaunchedEffect(source) {source.interactions.collect {if(it is PressInteraction.Press) presses++}}
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=probe.modifier()
                        if(config.width!=null)modifier=modifier.width(config.width.toFloat().dp)
                        if(config.height!=null)modifier=modifier.height(config.height.toFloat().dp)
                        val sizes=when(appearance) {
                            "custom" -> DragHandleSizes(DpSize(8.dp,40.dp),DpSize(14.dp,56.dp),DpSize(20.dp,64.dp))
                            "fractional" -> DragHandleSizes(DpSize(4.125.dp,47.625.dp),DpSize(12.125.dp,51.625.dp),DpSize(12.125.dp,51.625.dp))
                            else -> VerticalDragHandleDefaults.sizes()
                        }
                        VerticalDragHandle(modifier=modifier,sizes=sizes,interactionSource=source,
                            colors=if(appearance=="custom") DragHandleColors(Color.Red,Color(0xFF008000),Color.Blue) else VerticalDragHandleDefaults.colors(),
                            shapes=if(appearance=="custom") DragHandleShapes(RoundedCornerShape(3.dp),RoundedCornerShape(4.dp),RoundedCornerShape(6.dp)) else VerticalDragHandleDefaults.shapes())
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            val c=checkNotNull(probe.coordinates);val d=ComposeHost.RenderScale.toFloat();val origin=c.localToWindow(Offset.Zero)/d
            return listOf(ProbePoint("handle","handle",null,listOf(
                vector("box","rect",listOf(origin.x.toDouble(),origin.y.toDouble(),c.size.width.toDouble()/d,c.size.height.toDouble()/d),"dip","scene","geometry","layout.box.aabb.unclipped","LayoutCoordinates.size + localToWindow / density"),
                vector("size","point",listOf(c.size.width.toDouble()/d,c.size.height.toDouble()/d),"dip","local","geometry","layout.box.local-size","LayoutCoordinates.size / density",listOf("width","height")),
                number("presses",presses.toDouble(),"count",definition="drag-handle.source.press-count"))))
        }
    }
}

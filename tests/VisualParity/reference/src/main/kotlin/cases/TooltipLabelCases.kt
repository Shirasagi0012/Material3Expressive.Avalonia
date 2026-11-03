@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class TooltipLabelConfig(val persistent:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,
    val x:Int=112,val y:Int=80,val contentWidth:Double=64.0,val contentHeight:Double=8.0,val action:String="none")

@ParitySuite("tooltip-label")
class TooltipLabelCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer)

    private suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<TooltipLabelConfig>()
        ComposeHost(writer,256,192).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() };host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            sample("initial")
            var time=0L
            fun change(id:String,action:()->Unit) {
                time+=16000;host.advanceTo(time);action();writer.record(time,"mutation",id);host.frame(time);sample(id)
            }
            val hover=HoverInteraction.Enter()
            val press=PressInteraction.Press(Offset.Zero)
            val drag=DragInteraction.Start()
            fun emit(id:String,interaction:Interaction)=change(id){check(scene.source.tryEmit(interaction))}
            when(config.action) {
                "overlap" -> {
                    emit("hover",hover);emit("press",press);emit("drag",drag)
                    emit("hover-exit",HoverInteraction.Exit(hover));emit("release",PressInteraction.Release(press))
                    emit("drag-stop",DragInteraction.Stop(drag))
                }
                "cancel" -> {
                    emit("press",press);emit("cancel-press",PressInteraction.Cancel(press))
                    emit("drag",drag);emit("cancel-drag",DragInteraction.Cancel(drag))
                }
                "identity" -> {
                    emit("press",press);emit("unmatched-release",PressInteraction.Release(PressInteraction.Press(Offset.Zero)))
                    emit("duplicate-start",press);emit("first-release",PressInteraction.Release(press))
                    emit("second-release",PressInteraction.Release(press))
                }
                "focus" -> {
                    val focus=FocusInteraction.Focus();emit("focus",focus);emit("hover",hover)
                    emit("unfocus",FocusInteraction.Unfocus(focus));emit("hover-exit",HoverInteraction.Exit(hover))
                }
                "source" -> {
                    emit("hover",hover);val old=scene.source
                    change("replace-source"){scene.source=MutableInteractionSource()}
                    change("old-source"){check(old.tryEmit(drag))};emit("new-source",drag)
                    emit("drag-stop",DragInteraction.Stop(drag))
                }
                "outside" -> {
                    emit("hover",hover)
                    change("outside-down"){host.input("move",Offset(8f,180f));host.input("down",Offset(8f,180f))}
                    change("outside-up"){host.input("up",Offset(8f,180f))}
                    emit("still-active",drag);emit("hover-exit",HoverInteraction.Exit(hover))
                    emit("drag-stop",DragInteraction.Stop(drag));emit("reenter",hover)
                }
                "persistent" -> {
                    emit("ignored-press",press)
                    change("outside-down"){host.input("move",Offset(8f,180f));host.input("down",Offset(8f,180f))}
                    change("outside-up"){host.input("up",Offset(8f,180f))}
                    change("nonpersistent"){scene.persistent=false};emit("hover",hover)
                    change("persistent"){scene.persistent=true}
                }
                "resize" -> {
                    change("move"){scene.x=4;scene.y=4}
                    change("grow"){scene.anchorWidth=40;scene.width=120.0}
                    change("bottom-right"){scene.x=212;scene.y=164}
                }
                "transform" -> {
                    change("scale"){scene.scaled=true}
                    change("translate"){scene.translated=true}
                    change("reset"){scene.scaled=false;scene.translated=false}
                }
                "detach" -> {
                    emit("hover",hover);change("detach"){scene.attached=false}
                    emit("detached-drag",drag);change("reattach"){scene.attached=true}
                    emit("press",press);emit("release",PressInteraction.Release(press))
                }
            }
        }
    }

    private class Scene(val config:TooltipLabelConfig) {
        var source by mutableStateOf(MutableInteractionSource())
        var persistent by mutableStateOf(config.persistent)
        var attached by mutableStateOf(true)
        var x by mutableStateOf(config.x);var y by mutableStateOf(config.y)
        var anchorWidth by mutableStateOf(24);var width by mutableStateOf(config.contentWidth)
        var scaled by mutableStateOf(false);var translated by mutableStateOf(false)
        val anchor=Probe();val tooltip=Probe();val content=Probe()
        var painted=Color.Transparent
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft) {
                        if(attached) Box(Modifier.absoluteOffset(x.dp,y.dp)) {
                            Label(modifier=if(config.action=="transform") Modifier.graphicsLayer {
                                scaleX=if(scaled)1.5f else 1f;scaleY=if(scaled)1.25f else 1f
                                translationX=if(translated)8f*density else 0f;translationY=if(translated)12f*density else 0f
                            } else Modifier, interactionSource=source,isPersistent=persistent,label={
                                PlainTooltip(modifier=tooltip.modifier()) {
                                    val color=LocalContentColor.current
                                    Canvas(content.modifier().size(width.toFloat().dp,config.contentHeight.toFloat().dp)) {
                                        painted=color;drawRect(color)
                                    }
                                }
                            }) { Canvas(anchor.modifier().size(anchorWidth.dp,24.dp)){drawRect(Color.Blue)} }
                        }
                    }
                }
            }
        }

        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe):List<Measurement> {
                val c=checkNotNull(probe.coordinates)
                val d=ComposeHost.RenderScale.toFloat()
                val corners=listOf(Offset.Zero,Offset(c.size.width.toFloat(),0f),Offset(0f,c.size.height.toFloat()),Offset(c.size.width.toFloat(),c.size.height.toFloat())).map { c.localToWindow(it)/d }
                val left=corners.minOf{it.x};val top=corners.minOf{it.y}
                return listOf(vector("box","rect",listOf(left.toDouble(),top.toDouble(),(corners.maxOf{it.x}-left).toDouble(),(corners.maxOf{it.y}-top).toDouble()),
                    "dip","scene","geometry","layout.box.aabb.unclipped","LayoutCoordinates four corners localToWindow / density"),
                    vector("size","point",listOf(c.size.width.toDouble()/d,c.size.height.toDouble()/d),"dip","local","geometry",
                        "layout.box.local-size","LayoutCoordinates.size / density",listOf("width","height")))
            }
            val visible=tooltip.coordinates?.isAttached==true
            val points=mutableListOf(ProbePoint("state","state",null,listOf(flag("visible",visible,definition="label.popup.attached"))))
            if(anchor.coordinates?.isAttached==true) points+=ProbePoint("anchor","anchor",null,geometry(anchor))
            if(visible) {
                points+=ProbePoint("tooltip","tooltip",null,geometry(tooltip))
                val argb=painted.toArgb()
                points+=ProbePoint("content","content","tooltip",geometry(content)+vector("paint","color",
                    listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","label.content.painted-solid-srgba8","Canvas inherited LocalContentColor.toArgb()"))
            }
            return points
        }
    }
}

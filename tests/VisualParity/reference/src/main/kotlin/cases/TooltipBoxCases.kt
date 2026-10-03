@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.MutatorMutex
import androidx.compose.foundation.MutatePriority
import androidx.compose.foundation.background
import androidx.compose.foundation.focusable
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.key.*
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class TooltipBoxConfig(val position:String="Above",val x:Int=112,val y:Int=80,val rtl:Boolean=false,
    val dark:Boolean=false,val spacing:Double=4.0,val contentWidth:Double=64.0,val contentHeight:Double=8.0,
    val action:String="none",val persistent:Boolean=false,val enabled:Boolean=true,val hasAction:Boolean=false,val elevation:Double=0.0,
    val anchorScale:Double=1.0)
@ParitySuite("tooltip-box") class TooltipBoxCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    private suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<TooltipBoxConfig>()
        ComposeHost(writer,256,192).use {host ->
            val scene=Scene(config,dynamic)
            host.mount {scene.Content()};host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            sample("initial");if(!dynamic)return
            var nextFrame=16000L
            fun advance(time:Long) {while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            fun times(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            fun show(){scene.scope.launch {scene.state.show(MutatePriority.UserInput)}}
            val center=checkNotNull(scene.anchor.coordinates).localToWindow(Offset(12f,12f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
            when(config.action) {
                "show" -> {change(1000,"show",::show);times(16000,32000,64000,112000,224000,400000);change(401000,"dismiss"){scene.state.dismiss()};times(416000,448000,512000,800000)}
                "interrupt" -> {change(1000,"show",::show);times(16000,32000,64000);change(65000,"dismiss"){scene.state.dismiss()};times(80000,96000);change(97000,"reopen",::show);times(112000,128000,224000,400000)}
                "hover" -> {change(1000,"enter"){host.input("move",center)};times(16000,64000,224000,400000);change(401000,"exit"){host.input("move",Offset(250f,185f))};times(416000,448000,512000,800000)}
                "focus","escape" -> {
                    change(1000,"focus"){scene.focus.requestFocus()};times(16000,64000,224000,400000)
                    if(config.hasAction) change(401000,"tab"){host.scene.sendKeyEvent(KeyEvent(Key.Tab,KeyEventType.KeyDown))}
                    else if(config.action=="escape") change(401000,"escape"){host.scene.sendKeyEvent(KeyEvent(Key.Escape,KeyEventType.KeyDown))}
                    else change(401000,"blur"){scene.outsideFocus.requestFocus()}
                    times(416000,448000,512000,800000)
                }
                "outside","request" -> {
                    val point=checkNotNull(scene.outside.coordinates).localToWindow(Offset(8f,8f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
                    change(1000,"down"){host.input("move",point);host.input("down",point)};change(2000,"up"){host.input("up",point)};times(16000,64000,224000,400000)
                }
                "resize" -> {change(1000,"move"){scene.x=4;scene.y=4};change(2000,"grow"){scene.anchorWidth=40;scene.contentWidth=160.0};change(3000,"direction"){scene.position="End"}}
            }
        }
    }
    private class Scene(val config:TooltipBoxConfig,val dynamic:Boolean) {
        val state=TooltipState(initialIsVisible=!dynamic || config.action in setOf("outside","request","resize"),isPersistent=config.persistent,mutatorMutex=MutatorMutex())
        lateinit var scope:CoroutineScope
        val anchor=Probe();val tooltip=Probe();val content=Probe();val outside=Probe()
        val focus=FocusRequester();val outsideFocus=FocusRequester()
        var x by mutableStateOf(config.x);var y by mutableStateOf(config.y)
        var anchorWidth by mutableStateOf(24);var contentWidth by mutableStateOf(config.contentWidth)
        var position by mutableStateOf(config.position)
        var requests=0;var presses=0;var painted=Color.Transparent
        @Composable fun Content() {
            scope=rememberCoroutineScope()
            MaterialTheme(colorScheme=if(config.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=AbsoluteAlignment.TopLeft) {
                        val anchorPosition=when(position){"Above"->TooltipAnchorPosition.Above;"Below"->TooltipAnchorPosition.Below;"Left"->TooltipAnchorPosition.Left;"Right"->TooltipAnchorPosition.Right;"Start"->TooltipAnchorPosition.Start;"End"->TooltipAnchorPosition.End;else->error(position)}
                        Box(Modifier.absoluteOffset(x.dp,y.dp).graphicsLayer(scaleX=config.anchorScale.toFloat(),scaleY=config.anchorScale.toFloat())) {
                        TooltipBox(positionProvider=TooltipDefaults.rememberTooltipPositionProvider(anchorPosition,config.spacing.toFloat().dp),state=state,
                            enableUserInput=dynamic&&config.enabled&&config.action !in setOf("outside","request","resize"),hasAction=config.hasAction,
                            onDismissRequest=if(config.action=="request")({requests++})else null,tooltip={
                                PlainTooltip(modifier=tooltip.modifier(),shadowElevation=config.elevation.toFloat().dp) {
                                    val color=LocalContentColor.current
                                    Canvas(content.modifier().size(contentWidth.toFloat().dp,config.contentHeight.toFloat().dp)){painted=color;drawRect(color)}
                                }
                            }) {Canvas(anchor.modifier().size(anchorWidth.dp,24.dp).focusRequester(focus).focusable()){drawRect(Color.Blue)}}
                        }
                        Box(Modifier.absoluteOffset(16.dp,72.dp)) {
                        Canvas(outside.modifier().size(16.dp).focusRequester(outsideFocus).focusable()
                            .pointerInput(Unit) { awaitPointerEventScope { while(true) {
                                if(awaitPointerEvent(PointerEventPass.Initial).type==PointerEventType.Press) presses++
                            } } }){drawRect(Color.Red)}
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(c:androidx.compose.ui.layout.LayoutCoordinates):List<Measurement> {
                val d=ComposeHost.RenderScale.toFloat()
                val corners=listOf(Offset.Zero,Offset(c.size.width.toFloat(),0f),Offset(c.size.width.toFloat(),c.size.height.toFloat()),Offset(0f,c.size.height.toFloat())).map{c.localToWindow(it)/d}
                val left=corners.minOf{it.x}.toDouble();val top=corners.minOf{it.y}.toDouble()
                return listOf(vector("box","rect",listOf(left,top,corners.maxOf{it.x}-left,corners.maxOf{it.y}-top),"dip","scene","geometry","layout.box.aabb.unclipped","LayoutCoordinates.size + localToWindow / density"),
                    vector("size","point",listOf(c.size.width.toDouble()/d,c.size.height.toDouble()/d),"dip","local","geometry","layout.box.local-size","LayoutCoordinates.size / density",listOf("width","height")))
            }
            val points=mutableListOf(ProbePoint("state","state",null,listOf(flag("visible",state.isVisible,definition="tooltip.state.is-visible"),flag("current",state.transition.currentState,definition="tooltip.transition.current-state"),
                flag("target",state.transition.targetState,definition="tooltip.transition.target-state"),number("requests",requests.toDouble(),"count",definition="tooltip.dismiss-request-count"),number("presses",presses.toDouble(),"count",definition="tooltip.underlying-press-count"))),ProbePoint("anchor","anchor",null,geometry(checkNotNull(anchor.coordinates))))
            if(tooltip.coordinates?.isAttached==true) {
                // The outer TooltipBox content Box is outside its graphicsLayer Box.
                // Compare popup layout and local sizes, and use pixels for animated appearance.
                val c=checkNotNull(tooltip.coordinates)
                if(dynamic) points+=ProbePoint("popup","popup",null,geometry(checkNotNull(c.parentLayoutCoordinates?.parentLayoutCoordinates)))
                points+=ProbePoint("tooltip","tooltip",if(dynamic)"popup"else null,geometry(c).filter{!dynamic||it.name=="size"})
                val argb=painted.toArgb()
                points+=ProbePoint("content","content","tooltip",geometry(checkNotNull(content.coordinates)).filter{!dynamic||it.name=="size"}+vector("paint","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),"srgba8","none","properties","tooltip.content.painted-solid-srgba8","Canvas inherited LocalContentColor.toArgb()"))
            }
            return points
        }
    }
}

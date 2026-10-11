@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.ui.ExperimentalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.*
import androidx.compose.ui.input.key.*
import androidx.compose.ui.navigationevent.BackNavigationEventInput
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import androidx.compose.ui.window.DialogProperties
import kotlinx.serialization.Serializable

@Serializable
data class PickerDialogConfig(val date:Boolean=false,val dismiss:Boolean=true,val toggle:Boolean=true,
    val dark:Boolean=false,val rtl:Boolean=false,val platformWidth:Boolean=false,
    val width:Double?=null,val height:Double?=null,val hostWidth:Int=640,val hostHeight:Int=640,
    val contentWidth:Double=256.0,val contentHeight:Double=300.0,val buttonWidth:Double=80.0,
    val colors:String="default",val action:String="none",val allowDismiss:Boolean=true)

@ParitySuite("date-picker-dialog")
class DatePickerDialogCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = capturePickerDialog(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) = capturePickerDialog(writer,true)
}
@ParitySuite("time-picker-dialog")
class TimePickerDialogCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = capturePickerDialog(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) = capturePickerDialog(writer,true)
}

private suspend fun capturePickerDialog(writer:CaptureWriter,dynamic:Boolean) {
    val config=writer.readConfig<PickerDialogConfig>()
    val empty:PlatformContext=PlatformContext.Empty()
    val context=object:PlatformContext by empty {
        override val windowInfo=object:WindowInfo by empty.windowInfo {
            override val containerSize=IntSize(config.hostWidth*ComposeHost.RenderScale,config.hostHeight*ComposeHost.RenderScale)
            override val isWindowFocused=true
        }
    }
    ComposeHost(writer,config.hostWidth,config.hostHeight,context).use { host ->
        // Desktop mediator routes unconsumed keys to this real upstream navigation input.
        // CanvasLayersComposeScene by itself only delivers them to the scene.
        val navigationInput=BackNavigationEventInput()
        val dispatcher=context.architectureComponentsOwner.navigationEventDispatcherOwner.navigationEventDispatcher
        dispatcher.addInput(navigationInput)
        try {
        val scene=PickerDialogScene(config)
        host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
        fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
        sample("initial")
        if(!dynamic) return
        var nextFrame=16000L
        fun advance(time:Long) {
            while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }
            host.advanceTo(time)
        }
        fun change(time:Long,id:String,action:()->Unit) {
            advance(time); action(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id)
        }
        fun times(vararg times:Long) {
            times.forEach { advance(it); host.draw(); sample("time-$it") }
        }
        when(config.action) {
            "theme" -> { change(1000,"dark") {scene.dark=true}; change(2000,"light") {scene.dark=false} }
            "slots" -> { change(1000,"remove-slots") {scene.dismiss=false;scene.toggle=false}; change(2000,"restore") {scene.dismiss=config.dismiss;scene.toggle=config.toggle} }
            "mode" -> { change(1000,"input-mode") {scene.contentHeight=100.0}; change(2000,"picker-mode") {scene.contentHeight=config.contentHeight} }
            "resize" -> { change(1000,"wide") {scene.buttonWidth=220.0}; change(2000,"restore") {scene.buttonWidth=config.buttonWidth} }
            "escape" -> {
                fun key(type:KeyEventType) {
                    val event=KeyEvent(Key.Escape,type)
                    if(!host.scene.sendKeyEvent(event)) navigationInput.onKeyEvent(event)
                    host.scheduler.runCurrent()
                }
                change(1000,"escape-down") {key(KeyEventType.KeyDown)}
                change(2000,"escape-up") {key(KeyEventType.KeyUp)}
            }
            "outside","confirm","dismiss","toggle" -> {
                val point=if(config.action=="outside") Offset(10f,10f) else
                    checkNotNull(scene.probes[config.action]?.coordinates).localToWindow(Offset(20f,20f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
                change(1000,"down") {host.input("move",point);host.input("down",point)}
                change(2000,"up") {host.input("up",point)}
            }
            "transition" -> {
                change(1000,"open") {scene.open=true}
                times(16000,32000,64000,112000,208000,224000)
                change(225000,"close") {scene.open=false}
                times(240000,272000,320000,336000)
                change(337000,"reopen") {scene.open=true}
                times(352000,400000,544000,560000)
            }
            "interrupt" -> {
                change(1000,"open") {scene.open=true}
                times(16000,32000,64000)
                change(65000,"close-during-entry") {scene.open=false}
                times(80000,96000)
                change(97000,"reopen-during-exit") {scene.open=true}
                times(112000,128000,160000,304000,320000)
                change(321000,"close") {scene.open=false}
                times(336000,352000,384000,448000)
            }
            "immediate" -> {
                change(1000,"open") {scene.open=true}
                change(2000,"close-before-animation-frame") {scene.open=false}
                times(16000,32000)
            }
        }
        host.advanceTo(host.timeUs+1000);sample("idle",false)
        } finally { dispatcher.removeInput(navigationInput) }
    }
}

private class PickerDialogScene(val config:PickerDialogConfig) {
    var open by mutableStateOf(config.action !in setOf("transition","interrupt","immediate"))
    var dark by mutableStateOf(config.dark)
    var dismiss by mutableStateOf(config.dismiss)
    var toggle by mutableStateOf(config.toggle)
    var contentHeight by mutableStateOf(config.contentHeight)
    var buttonWidth by mutableStateOf(config.buttonWidth)
    var requests=0;var confirms=0;var dismisses=0;var toggles=0
    val container=Probe()
    val probes=mutableMapOf<String,Probe>()
    val painted=mutableMapOf<String,Color>()
    @Composable fun Content() {
        MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(surface=Color(0xFFFFFBFE))) {
            CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                Box(Modifier.fillMaxSize().background(Color.White))
                if(open) {
                    var modifier=container.modifier()
                    if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                    if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                    val properties=DialogProperties(dismissOnBackPress=config.allowDismiss,dismissOnClickOutside=config.allowDismiss,
                        usePlatformDefaultWidth=config.platformWidth,animateTransition=config.action in setOf("transition","interrupt","immediate"))
                    val color=when(config.colors) {
                        "custom" -> Color(0xFFE0F0E8); "surface" -> MaterialTheme.colorScheme.surface;
                        "transparent" -> Color.Transparent; "primary" -> MaterialTheme.colorScheme.primary;
                        else -> MaterialTheme.colorScheme.surfaceContainerHigh
                    }
                    if(config.date) {
                        DatePickerDialog(onDismissRequest={requests++}, confirmButton={Marker("confirm",buttonWidth,40.0)},
                            dismissButton=if(dismiss) ({Marker("dismiss",config.buttonWidth,40.0)}) else null,
                            modifier=modifier, properties=properties,
                            colors=DatePickerDefaults.colors(containerColor=color),
                            shape=if(config.colors=="custom") RoundedCornerShape(12.dp) else DatePickerDefaults.shape,
                            tonalElevation=if(config.colors=="surface") 6.dp else DatePickerDefaults.TonalElevation) {
                            Marker("content",config.contentWidth,contentHeight)
                        }
                    } else {
                        TimePickerDialog(onDismissRequest={requests++}, confirmButton={Marker("confirm",buttonWidth,40.0)},
                            title={Marker("title",100.0,24.0)},
                            dismissButton=if(dismiss) ({Marker("dismiss",config.buttonWidth,40.0)}) else null,
                            modeToggleButton=if(toggle) ({Marker("toggle",40.0,48.0)}) else null,
                            modifier=modifier, properties=properties,containerColor=color,
                            shape=if(config.colors=="custom") RoundedCornerShape(12.dp) else TimePickerDialogDefaults.shape) {
                            Marker("content",config.contentWidth,contentHeight)
                        }
                    }
                }
            }
        }
    }
    @Composable fun Marker(id:String,width:Double,height:Double,fixedColor:Color?=null) {
        val color=fixedColor ?: LocalContentColor.current
        painted[id]=color
        val probe=probes.getOrPut(id) {Probe()}
        val click=if(id=="confirm" || id=="dismiss" || id=="toggle") Modifier.clickable(interactionSource=null,indication=null) {
            if(id=="confirm") confirms++ else if(id=="dismiss") dismisses++ else {toggles++;contentHeight=100.0}
        } else Modifier
        Canvas(probe.modifier().size(width.toFloat().dp,height.toFloat().dp).then(click)) {drawRect(color)}
    }
    fun measure():List<ProbePoint> {
        val points=mutableListOf(ProbePoint("state","dialog",null,listOf(
            flag("open",open,definition="dialog.application-open-state"),
            number("requests",requests.toDouble(),"count",definition="dialog.dismiss-request-count"),
            number("confirms",confirms.toDouble(),"count",definition="dialog.confirm-callback-count"),
            number("toggles",toggles.toDouble(),"count",definition="dialog.mode-toggle-callback-count"),
            number("dismisses",dismisses.toDouble(),"count",definition="dialog.dismiss-button-callback-count"))))
        if(!open) return points
        val coordinates=checkNotNull(container.coordinates)
        points+=ProbePoint("container","dialog",null,container.geometry().filter {it.name=="size"})
        probes.filter {it.value.coordinates?.isAttached==true}.forEach { (id,probe) ->
            val c=checkNotNull(probe.coordinates)
            val origin=coordinates.localPositionOf(c,Offset.Zero)/ComposeHost.RenderScale.toFloat()
            val argb=checkNotNull(painted[id]).toArgb()
            points+=ProbePoint(id,id,"container",listOf(
                vector("box","rect",listOf(origin.x.toDouble(),origin.y.toDouble(),c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),
                    "dip","local","geometry","dialog.slot-box.relative-container","LayoutCoordinates.localPositionOf relative to dialog content / density"),
                vector("color","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                    "srgba8","none","properties","dialog.slot-inherited-color","Canvas rendered LocalContentColor.toArgb()")))
        }
        return points
    }
}

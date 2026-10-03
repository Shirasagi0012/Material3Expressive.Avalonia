@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.input.key.*
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class IconButtonConfig(val style:String="filled", val size:String="small", val shape:String="round",
    val enabled:Boolean=true, val dark:Boolean=false, val rtl:Boolean=false, val width:Double?=null,
    val height:Double?=null, val pressedRadius:Double?=null, val action:String="none", val custom:String="none",
    val empty:Boolean=false, val ambientColor:String?=null)

@ParitySuite("icon-button")
class IconButtonCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<IconButtonConfig>()
        ComposeHost(writer,320,240).use { host ->
            val scene=IconScene(config)
            host.mount { scene.Content() }
            host.draw(); host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<IconButtonConfig>()
        ComposeHost(writer,320,240).use { host ->
            val scene=IconScene(config)
            host.mount { scene.Content() }
            host.draw(); host.frame(-1000); host.frame(0)
            fun sample(id:String,image:Boolean=true) = writer.submit(host.snapshot(id,id,scene.measure(),image))
            sample("initial")
            val center=Offset(160f,120f); val outside=Offset(8f,8f)
            var nextFrameUs=16000L
            suspend fun advanceTo(time:Long) {
                while(nextFrameUs<=time) { host.frame(nextFrameUs); nextFrameUs+=16000 }
                host.advanceTo(time)
            }
            suspend fun samplesAt(vararg times:Long) {
                for(time in times) { advanceTo(time); host.draw(); sample("time-"+time,time!=32000L) }
            }
            suspend fun event(time:Long,action:String,position:Offset) { advanceTo(time); host.input(action,position); host.draw() }
            suspend fun focus(time:Long,focused:Boolean) {
                advanceTo(time)
                if(focused) scene.requester.requestFocus() else scene.focusManager!!.clearFocus(force=true)
                writer.record(time,"input",if(focused) "FocusRequester.requestFocus" else "FocusManager.clearFocus"); host.draw()
            }
            suspend fun key(time:Long,down:Boolean) {
                advanceTo(time)
                host.scene.sendKeyEvent(KeyEvent(if(config.action=="space") Key.Spacebar else Key.Enter,if(down) KeyEventType.KeyDown else KeyEventType.KeyUp))
                host.scheduler.runCurrent()
                writer.record(time,"input","key ${config.action} ${if(down) "down" else "up"}"); host.draw()
            }
            when(config.action) {
                "hover" -> {
                    event(1000,"move",center); sample("hover-event"); samplesAt(16000,75000,180000)
                    event(181000,"move",outside); sample("exit-event"); samplesAt(196000,400000)
                }
                "focus" -> {
                    focus(1000,true); sample("focus-event"); samplesAt(16000,45000,180000)
                    focus(181000,false); sample("blur-event"); samplesAt(196000,400000)
                }
                "space", "enter" -> {
                    focus(1000,true); samplesAt(16000,121000)
                    key(122000,true); sample("key-down"); samplesAt(138000,197000,347000)
                    key(348000,false); sample("key-up"); samplesAt(364000,498000,700000)
                }
                "theme", "size" -> {
                    advanceTo(1000)
                    if(config.action=="theme") scene.dark=true else scene.size="large"
                    writer.record(1000,"mutation",config.action); host.draw(); sample("mutation"); samplesAt(16000,400000)
                    advanceTo(401000)
                    if(config.action=="theme") scene.dark=config.dark else scene.size=config.size
                    writer.record(401000,"mutation",config.action+" restored"); host.draw(); sample("restored"); samplesAt(416000,800000)
                }
                else -> {
                    event(1000,"move",center); sample("pointer-enter")
                    event(2000,"down",center); sample("down-event")
                    when(config.action) {
                        "quick", "disabled" -> {
                            event(5000,"up",center); sample("up-before-first-frame"); samplesAt(16000,75000,225000,600000)
                        }
                        "reverse" -> {
                            samplesAt(16000,32000); event(33000,"up",center); sample("reverse-event")
                            samplesAt(48000); event(49000,"down",center); sample("repress-event"); samplesAt(64000,96000)
                            event(97000,"up",center); sample("final-up"); samplesAt(113000,225000,600000)
                        }
                        else -> {
                            samplesAt(16000,32000,75000)
                            when(config.action) {
                                "cancel" -> {
                                    event(76000,"move",outside); sample("drag-out"); event(77000,"up",outside); sample("outside-release"); samplesAt(92000,225000,600000)
                                }
                                "drag-back" -> {
                                    event(76000,"move",outside); sample("drag-out"); samplesAt(92000,150000)
                                    event(151000,"move",center); sample("drag-back"); samplesAt(167000,225000)
                                    event(226000,"up",center); sample("inside-release"); samplesAt(242000,600000)
                                }
                                "disable" -> {
                                    advanceTo(76000); scene.enabled=false; writer.record(76000,"mutation","enabled=false"); host.draw(); sample("disabled-during-press"); samplesAt(80000,96000,225000,400000)
                                    event(401000,"up",center); sample("disabled-release"); advanceTo(402000); scene.enabled=true
                                    writer.record(402000,"mutation","enabled=true"); host.draw(); sample("reenabled"); samplesAt(418000,600000)
                                }
                                else -> {
                                    if(config.action in listOf("clear-shape","change-shape")) {
                                        advanceTo(76000); scene.pressedRadius=if(config.action=="clear-shape") null else 14.0
                                        writer.record(76000,"mutation",config.action); host.draw(); sample("shape-mutation")
                                    }
                                    samplesAt(120000,225000,400000); event(401000,"up",center); sample("release-event"); samplesAt(416000,551000,700000)
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private class IconScene(val config:IconButtonConfig) {
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var size by mutableStateOf(config.size)
        var pressedRadius by mutableStateOf(config.pressedRadius)
        val container=Probe(); val icon=Probe()
        val interactions=MutableInteractionSource()
        val requester=FocusRequester()
        var focusManager:FocusManager?=null
        var focused=false; var pressed=false; var hovered=false; var clicks=0
        var paintedColor:Color?=null

        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    @Composable fun Scene() { Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) { ButtonContent() } }
                    if(config.ambientColor!=null) {
                        val ambient=Color((0xff000000L or config.ambientColor.removePrefix("#").toLong(16)).toInt())
                        CompositionLocalProvider(LocalContentColor provides ambient) { Scene() }
                    } else Scene()
                }
            }
        }

        @Composable fun ButtonContent() {
            focusManager=LocalFocusManager.current
            LaunchedEffect(interactions) {
                val presses=mutableSetOf<PressInteraction.Press>(); val hovers=mutableSetOf<HoverInteraction.Enter>()
                interactions.interactions.collect {
                    when(it) {
                        is PressInteraction.Press -> presses.add(it)
                        is PressInteraction.Release -> presses.remove(it.press)
                        is PressInteraction.Cancel -> presses.remove(it.press)
                        is HoverInteraction.Enter -> hovers.add(it)
                        is HoverInteraction.Exit -> hovers.remove(it.enter)
                    }
                    pressed=presses.isNotEmpty(); hovered=hovers.isNotEmpty()
                }
            }
            val dimensions=when(size) {
                "xsmall" -> IconButtonDefaults.extraSmallContainerSize()
                "medium" -> IconButtonDefaults.mediumContainerSize()
                "large" -> IconButtonDefaults.largeContainerSize()
                "xlarge" -> IconButtonDefaults.extraLargeContainerSize()
                else -> IconButtonDefaults.smallContainerSize()
            }
            val iconSize=when(size) {
                "xsmall" -> IconButtonDefaults.extraSmallIconSize; "medium" -> IconButtonDefaults.mediumIconSize
                "large" -> IconButtonDefaults.largeIconSize; "xlarge" -> IconButtonDefaults.extraLargeIconSize
                else -> IconButtonDefaults.smallIconSize
            }
            val shape=when(config.shape) {
                "square" -> when(size) { "xsmall", "small" -> MaterialTheme.shapes.medium; "medium" -> MaterialTheme.shapes.large; else -> MaterialTheme.shapes.extraLarge }
                "asymmetric" -> RoundedCornerShape(topStart=4.dp,topEnd=16.dp,bottomEnd=10.dp,bottomStart=2.dp)
                else -> IconButtonDefaults.standardShape
            }
            val colors=when(config.style) {
                "standard" -> IconButtonDefaults.iconButtonColors(); "tonal" -> IconButtonDefaults.filledTonalIconButtonColors()
                "outlined" -> IconButtonDefaults.outlinedIconButtonColors(); else -> IconButtonDefaults.filledIconButtonColors()
            }
            val border=if(config.custom=="border") BorderStroke(3.dp,Color.Red) else IconButtonDefaults.outlinedIconButtonBorder(enabled)
            var modifier=container.modifier().focusRequester(requester).onFocusChanged { focused=it.isFocused }
            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
            // Small uses the actual composable default, preserving its 48dp outer touch allocation.
            // Other sizes use the explicit size helper as in upstream samples; do not constrain
            // default small to 40dp merely to match the current local allocation.
            if(size!="small") modifier=modifier.size(dimensions)
            val content:@Composable ()->Unit = {
                if(!config.empty) {
                    val paint=LocalContentColor.current
                    Canvas(icon.modifier().size(iconSize)) {
                        paintedColor=paint
                        withTransform({ scale(this.size.width/20f,this.size.height/20f,pivot=Offset.Zero) }) { drawPath(Arrow,paint) }
                    }
                }
            }
            val pressedShape=pressedRadius?.let { RoundedCornerShape(it.toFloat().dp) }
            // Keep the expressive call site when its target is cleared. Replacing the whole
            // composable overload would recreate clickable nodes and cancel the held gesture;
            // identical shapes express the fixed target without that fixture-induced reset.
            if(config.pressedRadius==null) when(config.style) {
                "standard" -> IconButton(onClick={clicks++},modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,shape=shape,content=content)
                "tonal" -> FilledTonalIconButton(onClick={clicks++},modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,shape=shape,content=content)
                "outlined" -> OutlinedIconButton(onClick={clicks++},modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,shape=shape,border=border,content=content)
                else -> FilledIconButton(onClick={clicks++},modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,shape=shape,content=content)
            } else {
                val shapes=IconButtonShapes(shape,pressedShape ?: shape)
                when(config.style) {
                    "standard" -> IconButton(onClick={clicks++},shapes=shapes,modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,content=content)
                    "tonal" -> FilledTonalIconButton(onClick={clicks++},shapes=shapes,modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,content=content)
                    "outlined" -> OutlinedIconButton(onClick={clicks++},shapes=shapes,modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,border=border,content=content)
                    else -> FilledIconButton(onClick={clicks++},shapes=shapes,modifier=modifier,enabled=enabled,colors=colors,interactionSource=interactions,content=content)
                }
            }
        }

        fun measure():List<ProbePoint> {
            val buttonFields=layoutBounds(container).toMutableList()
            if(config.action!="none") {
                val node=checkNotNull(field(checkNotNull(container.coordinates),"layoutNode"))
                val semantics=field(node,"_semanticsConfiguration") as SemanticsConfiguration
                buttonFields+=flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="icon-button.enabled",source="cached LayoutNode clickable SemanticsConfiguration Disabled key")
                buttonFields+=flag("pressed",pressed,definition="icon-button.pressed",source="actual MutableInteractionSource active PressInteraction set")
                buttonFields+=flag("hovered",hovered,definition="icon-button.hovered",source="actual MutableInteractionSource active HoverInteraction set")
                buttonFields+=flag("focused",focused,definition="icon-button.focused",source="Modifier.onFocusChanged")
                buttonFields+=number("clickCount",clicks.toDouble(),"count",definition="icon-button.on-click-count",source="real IconButton onClick callback")
            }
            val layers=mutableListOf<Pair<LayoutCoordinates,Any>>()
            var coordinator:Any?=checkNotNull(container.coordinates)
            while(coordinator!=null) { field(coordinator,"layer")?.let { layers+=(coordinator as LayoutCoordinates) to it }; coordinator=field(coordinator,"wrapped") }
            val clipped=layers.filter { (_,layer) ->
                val graphics=checkNotNull(field(layer,"graphicsLayer"))
                graphics.javaClass.methods.single { it.name=="getClip" && it.parameterCount==0 }.invoke(graphics)==true
            }.single()
            val rr=(field(clipped.second,"outline") as? Outline.Rounded ?: error("IconButton has no cached rounded clip outline")).roundRect
            val radii=listOf(rr.topLeftCornerRadius,rr.topRightCornerRadius,rr.bottomRightCornerRadius,rr.bottomLeftCornerRadius).flatMap { listOf(it.x.toDouble()/ComposeHost.RenderScale,it.y.toDouble()/ComposeHost.RenderScale) }
            val fields=layoutBounds(container,clipped.first)+vector("cornerRadii","quad",radii,"dip","container","motion","icon-button.container.normalized-visual-rounded-outline",
                "derived: cached GraphicsLayerOwnerLayer rounded outline / density; visible physical TL/TR/BR/BL",CornerComponents)
            val points=mutableListOf(ProbePoint("button","button",null,buttonFields),ProbePoint("container","container","button",fields))
            if(!config.empty) {
                val color=paintedColor
                val paint=if(color!=null) {
                    val argb=color.toArgb()
                    vector("color","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                        "srgba8","none","properties","icon-button.icon.painted-solid-srgba8","derived: actual inherited LocalContentColor.toArgb() observed in Canvas draw")
                } else unavailable("color","properties","color","srgba8","none","Vector painter has not drawn a solid brush.","icon-button.icon.painted-solid-srgba8")
                points+=ProbePoint("icon","icon","container",layoutBounds(icon)+paint)
            }
            return points
        }
        private fun layoutBounds(probe:Probe,coordinates:LayoutCoordinates=checkNotNull(probe.coordinates)) = probe.geometry(coordinates).filter { it.name=="box" || it.name=="size" }
    }
    companion object {
        private val CornerComponents=listOf("topLeft.x","topLeft.y","topRight.x","topRight.y","bottomRight.x","bottomRight.y","bottomLeft.x","bottomLeft.y")
        private val Arrow by lazy { Path().apply { moveTo(2f,3f); lineTo(11f,3f); lineTo(11f,1f); lineTo(18f,8f); lineTo(11f,15f); lineTo(11f,12f); lineTo(2f,12f); close() } }
    }
}

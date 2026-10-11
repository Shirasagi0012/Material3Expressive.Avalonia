@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.AnimationVector1D
import androidx.compose.animation.core.SpringSpec
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
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class ButtonConfig(val style:String="filled", val size:String="small", val shape:String="round",
    val enabled:Boolean=true, val dark:Boolean=false, val icon:String="none", val rtl:Boolean=false,
    val width:Double?=240.0, val custom:String="none", val baseline:Boolean=false,
    val action:String="none", val height:Double?=null, val empty:Boolean=false,
    val contentWidth:Double=32.0, val contentHeight:Double=16.0, val requiredContentSize:Boolean=false, val morph:Boolean=true)

@ParitySuite("button")
class ButtonCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<ButtonConfig>()
        ComposeHost(writer,320,240).use { host ->
            val state=ButtonScene(writer,config)
            host.mount { state.Content() }
            host.draw(); host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",state.measure()))
        }
    }

    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<ButtonConfig>()
        ComposeHost(writer,320,240).use { host ->
            val state=ButtonScene(writer,config)
            host.mount { state.Content() }
            host.draw(); host.frame(-1000); host.frame(0)
            fun sample(id:String,image:Boolean=true) = writer.submit(host.snapshot(id,id,state.measure(),image))
            // Same absolute 16ms frame grid as Avalonia; sampling does not add frames.
            var nextFrameUs=16000L
            fun advanceTo(time:Long) {
                while(nextFrameUs<=time) { host.frame(nextFrameUs); nextFrameUs+=16000L }
                host.advanceTo(time)
            }
            fun event(time:Long,action:String,position:Offset) { advanceTo(time); host.input(action,position); host.draw() }
            fun samplesAt(vararg times:Long) {
                times.forEach { advanceTo(it); host.draw(); sample("time-$it",it !in listOf(32000L,450000L,92000L)) }
            }
            fun focus(time:Long,focused:Boolean) {
                advanceTo(time)
                if(focused) state.requester.requestFocus() else checkNotNull(state.focusManager).clearFocus(force=true)
                writer.record(time,"input",if(focused) "FocusRequester.requestFocus" else "FocusManager.clearFocus")
                host.draw()
            }
            fun key(time:Long,key:String,down:Boolean) {
                advanceTo(time)
                host.scene.sendKeyEvent(KeyEvent(if(key=="space") Key.Spacebar else Key.Enter,if(down) KeyEventType.KeyDown else KeyEventType.KeyUp))
                host.scheduler.runCurrent(); writer.record(time,"input","key $key ${if(down) "down" else "up"}"); host.draw()
            }
            val center=Offset(160f,120f); val outside=Offset(8f,8f)
            sample("initial")
            when(config.action) {
                "hover" -> {
                    event(1000,"move",center); sample("hover-event"); samplesAt(16000,75000,121000,180000)
                    event(181000,"move",outside); sample("exit-event"); samplesAt(196000,301000,400000)
                }
                "focus" -> {
                    focus(1000,true); sample("focus-event"); samplesAt(16000,45000,121000,180000)
                    focus(181000,false); sample("blur-event"); samplesAt(196000,331000,400000)
                }
                "focus-hover", "hover-focus" -> {
                    if(config.action=="focus-hover") focus(1000,true) else event(1000,"move",center)
                    samplesAt(16000,121000)
                    if(config.action=="focus-hover") event(122000,"move",center) else focus(122000,true)
                    sample("second-interaction"); samplesAt(138000,242000)
                    if(config.action=="focus-hover") event(243000,"move",outside) else focus(243000,false)
                    sample("remove-last"); samplesAt(259000,400000)
                    if(config.action=="focus-hover") focus(401000,false) else event(401000,"move",outside)
                    sample("remove-first"); samplesAt(417000,560000)
                }
                "space", "enter" -> {
                    focus(1000,true); samplesAt(16000,121000)
                    key(122000,config.action,true); sample("key-down"); samplesAt(138000,197000,347000)
                    key(348000,config.action,false); sample("key-up"); samplesAt(364000,498000,700000)
                }
                "theme", "size", "content" -> {
                    advanceTo(1000)
                    when(config.action) { "theme" -> state.dark=true; "size" -> state.size="large"; "content" -> state.contentWidth=64.0 }
                    writer.record(1000,"mutation",config.action); host.draw(); sample("mutation"); samplesAt(16000,400000)
                    advanceTo(401000)
                    when(config.action) { "theme" -> state.dark=false; "size" -> state.size="small"; "content" -> state.contentWidth=32.0 }
                    writer.record(401000,"mutation",config.action+" restored"); host.draw(); sample("restored"); samplesAt(416000,800000)
                }
                else -> {
                    event(1000,"move",center); sample("pointer-enter")
                    event(2000,"down",center); sample("down-event")
                    when(config.action) {
                        "quick" -> {
                            event(5000,"up",center); sample("up-before-first-frame"); samplesAt(16000,75000,225000,375000,600000)
                        }
                        "disabled" -> {
                            event(5000,"up",center); sample("up-before-first-frame"); samplesAt(16000,75000)
                            event(76000,"move",outside); sample("disabled-pointer-exit"); samplesAt(92000,225000,375000,600000)
                        }
                        "reverse" -> {
                            samplesAt(16000,32000); event(33000,"up",center); sample("reverse-event")
                            samplesAt(48000); event(49000,"down",center); sample("repress-event")
                            samplesAt(64000,96000); event(97000,"up",center); sample("final-up"); samplesAt(113000,225000,375000,600000)
                        }
                        else -> {
                            samplesAt(16000,32000,75000)
                            when(config.action) {
                                "cancel" -> {
                                    event(76000,"move",outside); sample("drag-out"); event(77000,"up",outside); sample("outside-release")
                                    samplesAt(92000,225000,375000,600000)
                                }
                                "drag-back" -> {
                                    event(76000,"move",outside); sample("drag-out"); samplesAt(92000,150000)
                                    event(151000,"move",center); sample("drag-back"); samplesAt(167000,225000)
                                    event(226000,"up",center); sample("inside-release"); samplesAt(242000,376000,600000)
                                }
                                "disable" -> {
                                    advanceTo(76000); state.enabled=false; writer.record(76000,"mutation","enabled=false")
                                    host.draw(); sample("disabled-during-press"); samplesAt(80000,92000,96000,112000,128000,225000,400000)
                                    event(401000,"up",center); sample("disabled-release"); advanceTo(402000); state.enabled=true; writer.record(402000,"mutation","enabled=true")
                                    host.draw(); sample("reenabled"); samplesAt(418000,600000)
                                }
                                else -> {
                                    if(config.action=="shape-change") {
                                        advanceTo(76000); state.pressedRadius=14; writer.record(76000,"mutation","pressedShape=14dip")
                                        host.draw(); sample("third-shape")
                                    }
                                    samplesAt(120000,225000,400000); event(401000,"up",center); sample("release-event"); samplesAt(416000,450000,551000,700000)
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private class ButtonScene(val writer:CaptureWriter,val config:ButtonConfig) {
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var size by mutableStateOf(config.size)
        var contentWidth by mutableStateOf(config.contentWidth)
        var pressedRadius by mutableStateOf<Int?>(null)
        val density=ComposeHost.RenderScale.toFloat()
        val container=Probe(); val contentProbe=Probe(); val startIcon=Probe(); val endIcon=Probe()
        val paintedColors=mutableMapOf<String,Color>()
        val requester=FocusRequester()
        var focusManager:FocusManager?=null
        var focused=false
        var pressed=false
        var hovered=false
        var clicks=0
        val interactions=MutableInteractionSource()

        @Composable
        fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) { ButtonContent() }
                }
            }
        }

        @Composable
        fun ButtonContent() {
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
            val height=when(size) {
                "xsmall" -> ButtonDefaults.ExtraSmallContainerHeight; "medium" -> ButtonDefaults.MediumContainerHeight
                "large" -> ButtonDefaults.LargeContainerHeight; "xlarge" -> ButtonDefaults.ExtraLargeContainerHeight
                else -> ButtonDefaults.MinHeight
            }
            val colors=when(config.style) {
                "elevated" -> ButtonDefaults.elevatedButtonColors(); "tonal" -> ButtonDefaults.filledTonalButtonColors()
                "outlined" -> ButtonDefaults.outlinedButtonColors(); "text" -> ButtonDefaults.textButtonColors(); else -> ButtonDefaults.buttonColors()
            }.let { if(config.custom=="colors") it.copy(containerColor=Color(0xFF208070),contentColor=Color(0xFFFFEEDD),
                disabledContainerColor=Color(0xFF90A0B0),disabledContentColor=Color(0xFF203040)) else it }
            val elevation=when {
                config.custom=="elevation" -> ButtonDefaults.buttonElevation(8.dp,4.dp,10.dp,12.dp,0.dp)
                config.custom=="no-elevation" -> null
                config.style=="elevated" -> ButtonDefaults.elevatedButtonElevation()
                config.style=="tonal" -> ButtonDefaults.filledTonalButtonElevation()
                config.style in listOf("outlined","text") -> null
                else -> ButtonDefaults.buttonElevation()
            }
            val border=when {
                config.custom=="border" -> BorderStroke(3.dp,Color.Red)
                config.style=="outlined" -> ButtonDefaults.outlinedButtonBorder(enabled)
                else -> null
            }
            val baseShapes=ButtonDefaults.shapesFor(height)
            val shape=when(config.shape) {
                "square" -> when(size) { "xsmall", "small" -> MaterialTheme.shapes.medium; "medium" -> MaterialTheme.shapes.large; else -> MaterialTheme.shapes.extraLarge }
                "asymmetric" -> RoundedCornerShape(topStart=4.dp,topEnd=16.dp,bottomEnd=10.dp,bottomStart=2.dp)
                "large-asymmetric" -> RoundedCornerShape(topStart=80.dp,topEnd=40.dp,bottomEnd=0.dp,bottomStart=0.dp)
                "large" -> RoundedCornerShape(50.dp)
                else -> baseShapes.shape
            }
            val shapes=ButtonDefaults.shapes(shape=shape,pressedShape=pressedRadius?.let { RoundedCornerShape(it.dp) } ?: baseShapes.pressedShape)
            val padding=when {
                config.custom=="padding" -> PaddingValues(start=5.dp,top=3.dp,end=27.dp,bottom=9.dp)
                config.baseline -> if(config.style=="text") ButtonDefaults.TextButtonContentPadding else ButtonDefaults.ContentPadding
                else -> ButtonDefaults.contentPaddingFor(height,config.icon in listOf("start","both"),config.icon in listOf("end","both"))
            }
            var modifier=container.modifier().focusRequester(requester).onFocusChanged { focused=it.isFocused }
            if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
            if(!config.baseline) modifier=modifier.heightIn(min=height)
            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
            val content:@Composable RowScope.()->Unit = {
                @Composable fun Marker(probe:Probe,id:String,icon:Boolean=false) {
                    val paint=LocalContentColor.current
                    val dimensions=if(icon) Modifier.size(ButtonDefaults.iconSizeFor(height))
                        else if(config.requiredContentSize) Modifier.requiredSize(contentWidth.toFloat().dp,config.contentHeight.toFloat().dp)
                        else Modifier.size(contentWidth.toFloat().dp,config.contentHeight.toFloat().dp)
                    Canvas(probe.modifier().then(dimensions)) {
                        // A passive draw observation: this holder never invalidates composition.
                        paintedColors[id]=paint
                        if(icon) {
                            withTransform({ scale(size.width/20f,size.height/20f,pivot=Offset.Zero) }) { drawPath(Arrow,paint) }
                        } else drawRect(paint)
                    }
                }
                if(!config.empty) {
                    if(config.icon in listOf("start","both")) { Marker(startIcon,"start-icon",true); Spacer(Modifier.width(ButtonDefaults.iconSpacingFor(height))) }
                    Marker(contentProbe,"content")
                    if(config.icon in listOf("end","both")) { Spacer(Modifier.width(ButtonDefaults.iconSpacingFor(height))); Marker(endIcon,"end-icon",true) }
                }
            }
            // Call the actual family overload; no reimplementation of Surface/Row/interaction policy.
            if(config.baseline || !config.morph) when(config.style) {
                "elevated" -> ElevatedButton({clicks++},modifier,enabled,shape,colors,elevation,border,padding,interactions,content)
                "tonal" -> FilledTonalButton({clicks++},modifier,enabled,shape,colors,elevation,border,padding,interactions,content)
                "outlined" -> OutlinedButton({clicks++},modifier,enabled,shape,colors,elevation,border,padding,interactions,content)
                "text" -> TextButton({clicks++},modifier,enabled,shape,colors,elevation,border,padding,interactions,content)
                else -> Button({clicks++},modifier,enabled,shape,colors,elevation,border,padding,interactions,content)
            } else when(config.style) {
                "elevated" -> ElevatedButton({clicks++},shapes,modifier,enabled,colors,elevation,border,padding,interactions,content)
                "tonal" -> FilledTonalButton({clicks++},shapes,modifier,enabled,colors,elevation,border,padding,interactions,content)
                "outlined" -> OutlinedButton({clicks++},shapes,modifier,enabled,colors,elevation,border,padding,interactions,content)
                "text" -> TextButton({clicks++},shapes,modifier,enabled,colors,elevation,border,padding,interactions,content)
                else -> Button({clicks++},shapes,modifier,enabled,colors,elevation,border,padding,interactions,content)
            }
        }

        fun measure():List<ProbePoint> {
            val buttonFields=layoutBounds(container).toMutableList()
            if(config.action in listOf("disable","disabled") || !config.enabled && config.action!="none") {
                val layoutNode=checkNotNull(field(checkNotNull(container.coordinates),"layoutNode"))
                val semantics=checkNotNull(field(layoutNode,"_semanticsConfiguration")) as SemanticsConfiguration
                buttonFields+=flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="button.enabled",source="LayoutNode cached SemanticsConfiguration Disabled key from real clickable")
            }
            if(config.action in listOf("press","quick","cancel","drag-back","reverse","disable","disabled","shape-change","space","enter")) {
                buttonFields+=flag("pressed",pressed,definition="button.pressed",source="actual MutableInteractionSource active PressInteraction set")
                buttonFields+=number("clickCount",clicks.toDouble(),"count",definition="button.on-click-count",source="real Button onClick callback")
            }
            if(config.action in listOf("hover","focus-hover","hover-focus","press","quick","cancel","drag-back","reverse","disable","disabled","shape-change"))
                buttonFields+=flag("hovered",hovered,definition="button.hovered",source="actual MutableInteractionSource active HoverInteraction set")
            if(config.action in listOf("focus","focus-hover","hover-focus","space","enter"))
                buttonFields+=flag("focused",focused,definition="button.focused",source="Modifier.onFocusChanged")
            val points=mutableListOf(ProbePoint("button","button",null,buttonFields),containerPoint())
            val contentGeometry=config.width==null || config.width<64 || config.height!=null || config.icon!="none" || config.shape=="asymmetric" || config.custom=="padding"
            fun addContent(probe:Probe,id:String,role:String) {
                val color=paintedColors[id]
                val paint=if(color!=null) {
                    val argb=color.toArgb()
                    vector("color","color",listOf((argb ushr 16 and 255).toDouble(),(argb ushr 8 and 255).toDouble(),(argb and 255).toDouble(),(argb ushr 24).toDouble()),
                        "srgba8","none","properties","button.content.painted-solid-srgba8","derived: Canvas draw actual inherited LocalContentColor.toArgb(); 8-bit sRGBA paint, not screenshot pixels")
                } else unavailable("color","properties","color","srgba8","none","Content draw has not run (it can be fully constrained or clipped).","button.content.painted-solid-srgba8")
                points+=ProbePoint(id,role,"container",(if(contentGeometry) layoutBounds(probe) else emptyList())+listOf(paint))
            }
            if(!config.empty) {
                addContent(contentProbe,"content","content")
                if(config.icon in listOf("start","both")) addContent(startIcon,"start-icon","icon")
                if(config.icon in listOf("end","both")) addContent(endIcon,"end-icon","icon")
            }
            return points
        }

        fun containerPoint():ProbePoint {
            // Pin-specific passive reader: Surface owns a clip graphics layer on the same LayoutNode.
            // Traverse the known coordinator chain, not descendants chosen by text or guessed indices.
            val layers=mutableListOf<Pair<LayoutCoordinates,Any>>()
            var coordinator:Any?=checkNotNull(container.coordinates)
            while(coordinator!=null) {
                val layer=field(coordinator,"layer")
                if(layer!=null) layers+=(coordinator as LayoutCoordinates) to layer
                coordinator=field(coordinator,"wrapped")
            }
            val clipped=layers.filter { (_,layer) ->
                val graphics=checkNotNull(field(layer,"graphicsLayer"))
                graphics.javaClass.methods.single { it.name=="getClip" && it.parameterCount==0 }.invoke(graphics)==true
            }.single()
            val outline=field(clipped.second,"outline") as? Outline.Rounded ?: error("Surface clip has no cached rounded outline")
            val rr=outline.roundRect
            val radii=listOf(rr.topLeftCornerRadius,rr.topRightCornerRadius,rr.bottomRightCornerRadius,rr.bottomLeftCornerRadius)
                .flatMap { listOf(it.x.toDouble(),it.y.toDouble()) }
            val elevation=layers.maxOfOrNull { (_,layer) ->
                val graphics=checkNotNull(field(layer,"graphicsLayer"))
                (graphics.javaClass.methods.single { it.name=="getShadowElevation" && it.parameterCount==0 }.invoke(graphics) as Number).toDouble()
            } ?: 0.0
            val fields=layoutBounds(container,clipped.first).toMutableList()
            if(config.action!="none" || config.style=="elevated" || config.custom in listOf("elevation","no-elevation"))
                fields+=number("shadowElevation",elevation/density,category="motion",definition="button.shadow-elevation",source="derived: Surface GraphicsLayerOwnerLayer.graphicsLayer.shadowElevation / density")
            // Core's rounded outline uses the visible physical axes, including RTL shape resolution.
            fields+=vector("cornerRadii","quad",radii.map { it/density },"dip","container","motion","button.container.normalized-visual-rounded-outline","derived: GraphicsLayerOwnerLayer.outline cached by updateLayerProperties / density; visible physical TL/TR/BR/BL",CornerComponents)
            if(!config.baseline && config.morph && config.action in listOf("press","quick","cancel","drag-back","reverse","disable","shape-change","space","enter","size")) {
                // Fixed Core AnimatedShape.kt: read the already applied shape and its captured
                // state without invoking a layer block, creating an outline, or advancing a frame.
                val shape=checkNotNull(field(clipped.first,"lastShape"))
                check(shape.javaClass.name=="androidx.compose.material3.internal.AnimatedShapeKt\$rememberAnimatedShape\$1\$1") {
                    "Unexpected Button animated shape: ${shape.javaClass.name}"
                }
                val state=checkNotNull(field(shape,"\$state"))
                @Suppress("UNCHECKED_CAST")
                val progress=field(state,"progress") as Animatable<Float,AnimationVector1D>
                val spec=field(state,"spec") as SpringSpec<*>
                fields+=flag("shapeRunning",progress.isRunning,"motion","button.shape-animation.running","AnimatedShapeState.progress.isRunning")
                fields+=number("shapeProgress",progress.value.toDouble(),"fraction",category="motion",
                    definition="button.shape-animation.interpolation-progress",source="AnimatedShapeState.progress.value")
                fields+=number("shapeVelocity",progress.velocity.toDouble(),"fraction/s",category="motion",
                    definition="button.shape-animation.interpolation-velocity",source="AnimatedShapeState.progress.velocity")
                fields+=number("shapeDampingRatio",spec.dampingRatio.toDouble(),"ratio",category="motion",
                    definition="button.shape-animation.spring-damping-ratio",source="AnimatedShapeState.spec SpringSpec.dampingRatio captured for this morph")
                fields+=number("shapeStiffness",spec.stiffness.toDouble(),"stiffness",category="motion",
                    definition="button.shape-animation.spring-stiffness",source="AnimatedShapeState.spec SpringSpec.stiffness captured for this morph")
            }
            return ProbePoint("container","container","button",fields)
        }

        // No scene rotation: compare final position/size, not framework-local axis orientation.
        private fun layoutBounds(probe:Probe,coordinates:LayoutCoordinates=checkNotNull(probe.coordinates)) =
            probe.geometry(coordinates).filter { it.name=="box" || it.name=="size" }
    }

    companion object {
        private val CornerComponents=listOf("topLeft.x","topLeft.y","topRight.x","topRight.y","bottomRight.x","bottomRight.y","bottomLeft.x","bottomLeft.y")
        private val Arrow by lazy {
            Path().apply {
                moveTo(2f,3f); lineTo(11f,3f); lineTo(11f,1f); lineTo(18f,8f); lineTo(11f,15f); lineTo(11f,12f); lineTo(2f,12f); close()
            }
        }
    }
}

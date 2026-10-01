@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.BorderStroke
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
import androidx.compose.ui.graphics.vector.*
import androidx.compose.ui.input.key.*
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.*
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class ButtonConfig(val style:String="filled", val size:String="small", val shape:String="round",
    val enabled:Boolean=true, val dark:Boolean=false, val icon:String="none", val rtl:Boolean=false,
    val label:String="Save", val width:Double?=null, val custom:String="none", val baseline:Boolean=false,
    val action:String="none")

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
            fun event(time:Long,action:String,position:Offset) { host.advanceTo(time); host.input(action,position); host.draw() }
            fun frames(vararg times:Long) { times.forEach { host.frame(it); sample("frame-$it",it !in listOf(32000L,450000L,92000L)) } }
            fun focus(time:Long,focused:Boolean) {
                host.advanceTo(time)
                if(focused) state.requester.requestFocus() else checkNotNull(state.focusManager).clearFocus(force=true)
                writer.record(time,"input",if(focused) "FocusRequester.requestFocus" else "FocusManager.clearFocus")
                host.draw()
            }
            fun key(time:Long,key:String,down:Boolean) {
                host.advanceTo(time)
                host.scene.sendKeyEvent(KeyEvent(if(key=="space") Key.Spacebar else Key.Enter,if(down) KeyEventType.KeyDown else KeyEventType.KeyUp))
                host.scheduler.runCurrent(); writer.record(time,"input","key $key ${if(down) "down" else "up"}"); host.draw()
            }
            val center=Offset(160f,120f); val outside=Offset(8f,8f)
            sample("initial")
            when(config.action) {
                "hover" -> {
                    event(1000,"move",center); sample("hover-event"); frames(16000,75000,121000,180000)
                    event(181000,"move",outside); sample("exit-event"); frames(196000,301000,400000)
                }
                "focus" -> {
                    focus(1000,true); sample("focus-event"); frames(16000,45000,121000,180000)
                    focus(181000,false); sample("blur-event"); frames(196000,331000,400000)
                }
                "focus-hover", "hover-focus" -> {
                    if(config.action=="focus-hover") focus(1000,true) else event(1000,"move",center)
                    frames(16000,121000)
                    if(config.action=="focus-hover") event(122000,"move",center) else focus(122000,true)
                    sample("second-interaction"); frames(138000,242000)
                    if(config.action=="focus-hover") event(243000,"move",outside) else focus(243000,false)
                    sample("remove-last"); frames(259000,400000)
                }
                "space", "enter" -> {
                    focus(1000,true); frames(16000,121000)
                    key(122000,config.action,true); sample("key-down"); frames(138000,197000,347000)
                    key(348000,config.action,false); sample("key-up"); frames(364000,498000,700000)
                }
                "theme", "size", "content" -> {
                    host.advanceTo(1000)
                    when(config.action) { "theme" -> state.dark=true; "size" -> state.size="large"; "content" -> state.label="Save all changes" }
                    writer.record(1000,"mutation",config.action); host.draw(); sample("mutation"); frames(16000,400000)
                }
                else -> {
                    event(1000,"move",center); sample("pointer-enter")
                    event(2000,"down",center); sample("down-event")
                    when(config.action) {
                        "quick", "disabled" -> {
                            event(5000,"up",center); sample("up-before-first-frame"); frames(16000,75000,225000,375000,600000)
                        }
                        "reverse" -> {
                            frames(16000,32000); event(33000,"up",center); sample("reverse-event")
                            frames(48000); event(49000,"down",center); sample("repress-event")
                            frames(64000,96000); event(97000,"up",center); sample("final-up"); frames(113000,225000,375000,600000)
                        }
                        else -> {
                            frames(16000,32000,75000)
                            when(config.action) {
                                "cancel" -> {
                                    event(76000,"move",outside); sample("drag-out"); event(77000,"up",outside); sample("outside-release")
                                    frames(92000,225000,375000,600000)
                                }
                                "disable" -> {
                                    host.advanceTo(76000); state.enabled=false; writer.record(76000,"mutation","enabled=false")
                                    host.draw(); sample("disabled-during-press"); frames(92000,225000,400000)
                                    event(401000,"up",center); host.advanceTo(402000); state.enabled=true; writer.record(402000,"mutation","enabled=true")
                                    host.draw(); sample("reenabled"); frames(418000,600000)
                                }
                                else -> {
                                    if(config.action=="shape-change") {
                                        host.advanceTo(76000); state.pressedRadius=14; writer.record(76000,"mutation","pressedShape=14dip")
                                        host.draw(); sample("third-shape")
                                    }
                                    frames(120000,225000,400000); event(401000,"up",center); sample("release-event"); frames(416000,450000,551000,700000)
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
        var label by mutableStateOf(config.label)
        var pressedRadius by mutableStateOf<Int?>(null)
        val container=Probe(); val labelProbe=Probe(); val startIcon=Probe(); val endIcon=Probe()
        val requester=FocusRequester()
        var focusManager:FocusManager?=null
        var focused=false
        var pressed=false
        var hovered=false
        var clicks=0
        val interactions=MutableInteractionSource()

        @Composable
        fun Content() {
            val family=remember { CaseDefaults.font(writer) }
            val typography=remember(family) { Typography().let { it.copy(
                labelLarge=it.labelLarge.copy(fontFamily=family), titleMedium=it.titleMedium.copy(fontFamily=family),
                headlineSmall=it.headlineSmall.copy(fontFamily=family), headlineLarge=it.headlineLarge.copy(fontFamily=family)) } }
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),typography=typography) {
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
                else -> baseShapes.shape
            }
            val shapes=ButtonDefaults.shapes(shape=shape,pressedShape=pressedRadius?.let { RoundedCornerShape(it.dp) } ?: baseShapes.pressedShape)
            val padding=when {
                config.custom=="padding" -> PaddingValues(start=5.dp,top=3.dp,end=27.dp,bottom=9.dp)
                config.baseline -> if(config.style=="text") ButtonDefaults.TextButtonContentPadding else ButtonDefaults.ContentPadding
                else -> ButtonDefaults.contentPaddingFor(height,config.icon in listOf("start","both"),config.icon in listOf("end","both"))
            }
            var modifier=container.modifier().focusRequester(requester).onFocusChanged { focused=it.isFocused }
            if(!config.baseline) modifier=modifier.heightIn(min=height)
            if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
            if(config.custom=="padding") modifier=modifier.width(220.dp)
            val textStyle=if(config.baseline) MaterialTheme.typography.labelLarge else ButtonDefaults.textStyleFor(height)
            val content:@Composable RowScope.()->Unit = {
                @Composable fun IconContent(probe:Probe) {
                    Icon(Cross,contentDescription=null,modifier=probe.modifier().size(ButtonDefaults.iconSizeFor(height)))
                }
                if(config.icon in listOf("start","both")) { IconContent(startIcon); Spacer(Modifier.width(ButtonDefaults.iconSpacingFor(height))) }
                Text(label,modifier=labelProbe.modifier(),style=if(config.custom=="typography") textStyle.copy(fontSize=19.sp,fontWeight=FontWeight.Bold) else textStyle,
                    onTextLayout={ labelProbe.layout=it })
                if(config.icon in listOf("end","both")) { Spacer(Modifier.width(ButtonDefaults.iconSpacingFor(height))); IconContent(endIcon) }
            }
            // Call the actual family overload; no reimplementation of Surface/Row/interaction policy.
            if(config.baseline) when(config.style) {
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
            val layoutNode=checkNotNull(field(checkNotNull(container.coordinates),"layoutNode"))
            val semantics=checkNotNull(field(layoutNode,"_semanticsConfiguration")) as SemanticsConfiguration
            val button=ProbePoint("button","button",null,container.geometry()+listOf(
                flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="button.enabled",source="LayoutNode cached SemanticsConfiguration Disabled key from real clickable"),
                flag("pressed",pressed,definition="button.pressed",source="actual MutableInteractionSource active PressInteraction set"),
                flag("hovered",hovered,definition="button.hovered",source="actual MutableInteractionSource active HoverInteraction set"),
                flag("focused",focused,definition="button.focused",source="Modifier.onFocusChanged"),
                number("clickCount",clicks.toDouble(),"count",definition="button.on-click-count",source="real Button onClick callback")))
            val points=mutableListOf(button,containerPoint())
            val label=labelProbe.textPoint("label","container")
            points+=label.copy(measurements=label.measurements.filter { it.name !in listOf("resolvedFonts","drawOrigin","glyphInk") && !it.name.endsWith(".width") })
            if(config.icon in listOf("start","both")) points+=ProbePoint("start-icon","icon","container",startIcon.geometry())
            if(config.icon in listOf("end","both")) points+=ProbePoint("end-icon","icon","container",endIcon.geometry())
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
            return ProbePoint("container","container","button",container.geometry(clipped.first)+listOf(
                number("shadowElevation",elevation,category="motion",definition="button.shadow-elevation",source="Surface GraphicsLayerOwnerLayer.graphicsLayer.shadowElevation; density=1"),
                vector("cornerRadii","quad",radii,"dip","container","motion","button.container.normalized-rounded-outline","GraphicsLayerOwnerLayer.outline cached by updateLayerProperties",CornerComponents),
                vector("nativeCornerRadii","quad",radii,"dip","container","motion","compose.button.graphics-layer.cached-rounded-outline","GraphicsLayerOwnerLayer.outline; raw cached native outline",CornerComponents)))
        }
    }

    companion object {
        private val CornerComponents=listOf("topLeft.x","topLeft.y","topRight.x","topRight.y","bottomRight.x","bottomRight.y","bottomLeft.x","bottomLeft.y")
        private fun field(value:Any,name:String):Any? {
            var type:Class<*>?=value.javaClass
            while(type!=null) {
                val f=type.declaredFields.firstOrNull { it.name==name }
                if(f!=null) { f.isAccessible=true; return f.get(value) }
                type=type.superclass
            }
            error("Missing field $name on ${value.javaClass.name}")
        }
        private val Cross=ImageVector.Builder("Button content cross",20.dp,20.dp,20f,20f).apply {
            path(fill=SolidColor(Color.Black)) {
                moveTo(8f,2f); lineTo(12f,2f); lineTo(12f,8f); lineTo(18f,8f); lineTo(18f,12f); lineTo(12f,12f)
                lineTo(12f,18f); lineTo(8f,18f); lineTo(8f,12f); lineTo(2f,12f); lineTo(2f,8f); lineTo(8f,8f); close()
            }
        }.build()
    }
}

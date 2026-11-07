@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.AnimationVector1D
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CornerSize
import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.*
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class MaterialShapeConfig(val kind:String="surface",val shape:String="cut",val rtl:Boolean=false,
    val content:Boolean=false,val border:Double=1.1,val elevation:Double=0.0,val clickable:Boolean=false,val action:String="none",val pressedShape:String="none",val gradient:Boolean=false,val gradientType:String="linear")

@ParitySuite("material-shape")
class MaterialShapeCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = capture(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter) = capture(writer)

    private suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<MaterialShapeConfig>()
        var width by mutableStateOf(120f); var height by mutableStateOf(64f)
        var rtl by mutableStateOf(c.rtl); var changed by mutableStateOf(false)
        var selected by mutableStateOf(false)
        val source=MutableInteractionSource(); val requester=FocusRequester()
        var focusManager:FocusManager?=null; var clicks=0; val probe=Probe()
        ComposeHost(writer,240,160).use { host ->
            host.mount {
                focusManager=LocalFocusManager.current
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                        LocalRippleThemeConfiguration provides if(c.action=="focus") RippleDefaults.InsetFocusRingThemeConfiguration else RippleDefaults.OpacityFocusThemeConfiguration,
                        LocalRippleConfiguration provides RippleConfiguration(focus=RippleConfiguration.Focus.InsetRing(Color.Red,Color.Blue))) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            val shape:Shape=when(c.shape) {
                                "zero" -> CutCornerShape(0.dp)
                                "asymmetric" -> CutCornerShape(if(changed) CornerSize(75) else CornerSize(40.dp),CornerSize(8.dp),CornerSize(24.dp),CornerSize(48.dp))
                                "percent" -> CutCornerShape(50)
                                "concave","offset","curve","hole" -> object:Shape {
                                    override fun createOutline(size:androidx.compose.ui.geometry.Size,layoutDirection:LayoutDirection,density:Density):Outline {
                                        val path=Path().apply {
                                            when(c.shape) {
                                                "offset" -> { moveTo(8f,4f);lineTo(108f,4f);lineTo(108f,52f);lineTo(8f,52f);close() }
                                                "curve" -> { moveTo(0f,32f);cubicTo(0f,0f,120f,0f,120f,32f);cubicTo(120f,64f,0f,64f,0f,32f);close() }
                                                "hole" -> { fillType=PathFillType.EvenOdd;moveTo(0f,0f);lineTo(120f,0f);lineTo(120f,64f);lineTo(0f,64f);close()
                                                    moveTo(40f,20f);lineTo(80f,20f);lineTo(80f,44f);lineTo(40f,44f);close() }
                                                else -> { moveTo(0f,0f);lineTo(120f,0f);lineTo(120f,64f);lineTo(60f,40f);lineTo(0f,64f);close() }
                                            }
                                            transform(Matrix().apply { scale(density.density,density.density) })
                                        }
                                        return Outline.Generic(path)
                                    }
                                }
                                else -> CutCornerShape(20.dp)
                            }
                            val modifier=probe.modifier().focusRequester(requester).size(width.dp,height.dp)
                            val color=if(c.kind.startsWith("chip-")) Color(0xFFBBDDCC) else Color(0xFFEADDFF)
                            val density=androidx.compose.ui.platform.LocalDensity.current.density
                            val paint=if(!c.gradient) SolidColor(Color.Red) else when(c.gradientType) {
                                "radial" -> Brush.radialGradient(listOf(Color.Red,Color.Blue),center=Offset(16*density,12*density),radius=20*density)
                                "conic" -> Brush.sweepGradient(listOf(Color.Red,Color.Blue),center=Offset(16*density,12*density))
                                else -> Brush.horizontalGradient(listOf(Color.Red,Color.Blue))
                            }
                            val border=if(c.border>0) BorderStroke(c.border.toFloat().dp,paint) else null
                            val content:@Composable ()->Unit={ if(c.content) Box(Modifier.fillMaxSize().background(Color.Blue)) }
                            val e=c.elevation.toFloat().dp
                            val elevation=CardDefaults.cardElevation(e,e,e,e,e,e)
                            val colors=CardDefaults.cardColors(containerColor=color,contentColor=Color.Black)
                            if(c.kind=="surface") Surface(modifier=modifier,shape=shape,color=color,border=border,shadowElevation=e,content=content)
                            else if(c.kind=="click") Surface(onClick={clicks++},modifier=modifier,shape=shape,color=color,contentColor=Color.Black,
                                border=border,shadowElevation=e,interactionSource=source,content=content)
                            else if(c.kind.startsWith("toggle-")) {
                                val shapes=ToggleButtonShapes(shape,if(c.pressedShape=="none")shape else CutCornerShape(50),if(c.pressedShape=="none")shape else CutCornerShape(8.dp))
                                val colors=ToggleButtonColors(color,Color.Black,color,Color.Black,color,Color.Black)
                                val change:(Boolean)->Unit={selected=it;clicks++}
                                val buttonContent:@Composable RowScope.()->Unit={content()}
                                val elevation:ToggleButtonElevation?=null
                                when(c.kind) {
                                    "toggle-tonal" -> FilledTonalToggleButton(checked=selected,onCheckedChange=change,modifier=modifier,shapes=shapes,colors=colors,elevation=elevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                    "toggle-elevated" -> ElevatedToggleButton(checked=selected,onCheckedChange=change,modifier=modifier,shapes=shapes,colors=colors,elevation=elevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                    "toggle-outlined" -> OutlinedToggleButton(checked=selected,onCheckedChange=change,modifier=modifier,shapes=shapes,colors=colors,elevation=elevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                    else -> ToggleButton(checked=selected,onCheckedChange=change,modifier=modifier,shapes=shapes,colors=colors,elevation=elevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                }
                            }
                            else if(c.kind.startsWith("icon-")) {
                                val iconColors=IconButtonDefaults.iconButtonColors(containerColor=color,contentColor=Color.Black)
                                val pressedShape=if(c.pressedShape=="none") shape else CutCornerShape(50)
                                if(c.kind.contains("toggle")) {
                                    val shapes=IconToggleButtonShapes(shape,pressedShape,if(c.pressedShape=="none")shape else CutCornerShape(8.dp))
                                    val colors=IconToggleButtonColors(color,Color.Black,color,Color.Black,color,Color.Black)
                                    when(c.kind) {
                                        "icon-toggle-filled" -> FilledIconToggleButton(checked=selected,onCheckedChange={clicks++},shapes=shapes,modifier=modifier,colors=colors,interactionSource=source,content=content)
                                        "icon-toggle-tonal" -> FilledTonalIconToggleButton(checked=selected,onCheckedChange={clicks++},shapes=shapes,modifier=modifier,colors=colors,interactionSource=source,content=content)
                                        "icon-toggle-outlined" -> OutlinedIconToggleButton(checked=selected,onCheckedChange={clicks++},shapes=shapes,modifier=modifier,colors=colors,border=border,interactionSource=source,content=content)
                                        else -> IconToggleButton(checked=selected,onCheckedChange={clicks++},shapes=shapes,modifier=modifier,colors=colors,interactionSource=source,content=content)
                                    }
                                } else {
                                    val shapes=IconButtonShapes(shape,pressedShape)
                                    when(c.kind) {
                                        "icon-filled" -> FilledIconButton(onClick={clicks++},shapes=shapes,modifier=modifier,colors=iconColors,interactionSource=source,content=content)
                                        "icon-tonal" -> FilledTonalIconButton(onClick={clicks++},shapes=shapes,modifier=modifier,colors=iconColors,interactionSource=source,content=content)
                                        "icon-outlined","icon-outlined-vibrant" -> OutlinedIconButton(onClick={clicks++},shapes=shapes,modifier=modifier,colors=iconColors,border=border,interactionSource=source,content=content)
                                        else -> IconButton(onClick={clicks++},shapes=shapes,modifier=modifier,colors=iconColors,interactionSource=source,content=content)
                                    }
                                }
                            }
                            else if(c.kind.startsWith("chip-")) {
                                val basic=AssistChipDefaults.assistChipColors(containerColor=color,labelColor=Color.Black)
                                val selectable=FilterChipDefaults.filterChipColors(containerColor=color,labelColor=Color.Black,selectedContainerColor=color,selectedLabelColor=Color.Black)
                                val basicElevation=ChipElevation(e,e,e,e,e,e)
                                val selectableElevation=SelectableChipElevation(e,e,e,e,e,e)
                                when(c.kind) {
                                    "chip-suggestion","chip-elevated-suggestion" -> SuggestionChip(onClick={clicks++},label=content,modifier=modifier,shape=shape,colors=basic,elevation=basicElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source)
                                    "chip-filter","chip-elevated-filter" -> FilterChip(selected=selected,onClick={clicks++},label=content,modifier=modifier,shape=shape,colors=selectable,elevation=selectableElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source)
                                    "chip-input" -> InputChip(selected=selected,onClick={clicks++},label=content,modifier=modifier,shape=shape,colors=selectable,elevation=selectableElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source)
                                    else -> AssistChip(onClick={clicks++},label=content,modifier=modifier,shape=shape,colors=basic,elevation=basicElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source)
                                }
                            }
                            else if(c.kind.startsWith("button-")) {
                                val buttonColors=ButtonDefaults.buttonColors(containerColor=color,contentColor=Color.Black)
                                val buttonElevation=ButtonDefaults.buttonElevation(e,e,e,e,e)
                                val pressedShape:Shape=if(c.pressedShape=="concave") object:Shape {
                                    override fun createOutline(size:androidx.compose.ui.geometry.Size,layoutDirection:LayoutDirection,density:Density):Outline =
                                        Outline.Generic(Path().apply {moveTo(0f,0f);lineTo(120f,0f);lineTo(120f,64f);lineTo(60f,40f);lineTo(0f,64f);close();transform(Matrix().apply{scale(density.density,density.density)})})
                                } else CutCornerShape(50)
                                val shapes=ButtonShapes(shape,if(c.pressedShape=="none") shape else pressedShape)
                                val buttonContent:@Composable RowScope.()->Unit={content()}
                                when(c.kind) {
                                    "button-elevated" -> ElevatedButton(onClick={clicks++},modifier=modifier,shapes=shapes,colors=buttonColors,elevation=buttonElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                    "button-tonal" -> FilledTonalButton(onClick={clicks++},modifier=modifier,shapes=shapes,colors=buttonColors,elevation=buttonElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                    "button-outlined" -> OutlinedButton(onClick={clicks++},modifier=modifier,shapes=shapes,colors=buttonColors,elevation=buttonElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                    "button-text" -> TextButton(onClick={clicks++},modifier=modifier,shapes=shapes,colors=buttonColors,elevation=buttonElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                    else -> Button(onClick={clicks++},modifier=modifier,shapes=shapes,colors=buttonColors,elevation=buttonElevation,border=border,contentPadding=PaddingValues(0.dp),interactionSource=source,content=buttonContent)
                                }
                            }
                            else {
                                // ElevatedCard has no border parameter. An outer draw modifier
                                // supplies the caller's explicit border, in the same draw order.
                                val cardModifier=if(c.kind=="elevated" && border!=null) modifier.border(border,shape) else modifier
                                if(c.clickable) when(c.kind) {
                                    "elevated" -> ElevatedCard(onClick={clicks++},modifier=cardModifier,shape=shape,colors=colors,elevation=elevation,interactionSource=source) {content()}
                                    "outlined" -> OutlinedCard(onClick={clicks++},modifier=modifier,shape=shape,colors=colors,elevation=elevation,border=border!!,interactionSource=source) {content()}
                                    else -> Card(onClick={clicks++},modifier=modifier,shape=shape,colors=colors,elevation=elevation,border=border,interactionSource=source) {content()}
                                } else when(c.kind) {
                                    "elevated" -> ElevatedCard(modifier=cardModifier,shape=shape,colors=colors,elevation=elevation) {content()}
                                    "outlined" -> OutlinedCard(modifier=modifier,shape=shape,colors=colors,elevation=elevation,border=border!!) {content()}
                                    else -> Card(modifier=modifier,shape=shape,colors=colors,elevation=elevation,border=border) {content()}
                                }
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            var next=16000L
            fun advance(time:Long) {while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun sample(id:String) {
                val state=mutableListOf(number("clicks",clicks.toDouble(),"count",definition="shape.activation-count"))
                if(c.kind.startsWith("toggle-") || c.kind.contains("icon-toggle")) state+=flag("checked",selected,definition="shape.checked-state")
                if(c.pressedShape=="percent") {
                    var coordinator:Any?=checkNotNull(probe.coordinates); var shape:Any?=null
                    while(coordinator!=null) {
                        val candidate=field(coordinator,"lastShape")
                        if(candidate!=null && candidate.javaClass.name.contains("rememberAnimatedShape")) shape=candidate
                        coordinator=field(coordinator,"wrapped")
                    }
                    @Suppress("UNCHECKED_CAST")
                    val progress=field(checkNotNull(field(checkNotNull(shape),"\$state")),"progress") as Animatable<Float,AnimationVector1D>
                    state+=number("progress",progress.value.toDouble(),"fraction",category="motion",definition="shape.morph.progress")
                }
                writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("container","container",null,probe.geometry().filter{it.name in setOf("box","size")}),
                ProbePoint("state","state",null,state))))
            }
            fun times(vararg values:Long) {values.forEach{advance(it);host.draw();sample("time-$it")}}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            sample("initial")
            when(c.action) {
                "toggle-morph" -> {
                    change(1000,"checked"){selected=true};times(16000,32000)
                    advance(33000);host.input("move",Offset(120f,80f));host.input("down",Offset(120f,80f));host.draw();times(48000,64000)
                    advance(65000);host.input("up",Offset(120f,80f));host.input("move",Offset(8f,8f));host.draw();times(80000)
                    change(97000,"unchecked"){selected=false};times(112000,160000,640000)
                }
                "selection" -> {
                    change(1000,"select"){selected=true}; times(16000,160000)
                    change(161000,"deselect"){selected=false}; times(176000,640000)
                }
                "hit" -> {
                    advance(1000);host.input("move",Offset(61f,49f));host.input("down",Offset(61f,49f));host.input("up",Offset(61f,49f));host.draw()
                    times(16000,160000)
                    advance(161000);host.input("move",Offset(120f,80f));host.input("down",Offset(120f,80f));host.input("up",Offset(120f,80f));host.draw()
                    times(176000,640000)
                }
                "press","reverse","morph-resize" -> {
                    advance(1000);host.input("move",Offset(120f,80f));host.input("down",Offset(120f,80f));host.draw()
                    times(16000,32000)
                    if(c.action=="morph-resize") change(33000,"morph-resize"){width=88f;height=56f}
                    if(c.action=="reverse") {
                        advance(33000);host.input("up",Offset(120f,80f));host.draw();times(48000,64000)
                        advance(65000);host.input("down",Offset(120f,80f));host.draw()
                    }
                    times(80000,160000)
                    advance(161000);host.input("up",Offset(120f,80f));host.input("move",Offset(8f,8f));host.draw()
                    times(176000,240000,640000)
                }
                "focus" -> {
                    change(1000,"focus"){requester.requestFocus()};times(16000,80000,160000,320000)
                    change(321000,"blur"){focusManager!!.clearFocus(force=true)};times(336000,400000,640000)
                }
                "resize" -> {
                    change(1000,"resize"){width=80f;height=48f}
                    change(17000,"mutate"){changed=true}
                    change(33000,"direction"){rtl=!rtl}
                    change(49000,"restore"){width=120f;height=64f}
                    times(64000,160000)
                }
            }
        }
    }
}

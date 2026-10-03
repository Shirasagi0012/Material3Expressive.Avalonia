@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Outline
import androidx.compose.ui.input.key.*
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class SplitButtonConfig(val style:String="filled",val size:String="small",val checked:Boolean=false,
    val toggle:Boolean=true,val enabled:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,
    val width:Double?=null,val height:Double?=null,val spacing:Double=2.0,val contentHeight:Double=16.0,
    val action:String="none",val part:String="trailing")

@ParitySuite("split-button")
class SplitButtonCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val c=writer.readConfig<SplitButtonConfig>()
        ComposeHost(writer,420,208).use { host ->
            val s=Scene(c); host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",s.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<SplitButtonConfig>()
        ComposeHost(writer,420,208).use { host ->
            val s=Scene(c); host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure(),image=id!="time-32000"))
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time); change(); writer.record(time,"mutation",id); host.draw(); sample(id) }
            fun input(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw() }
            val coordinates=checkNotNull((if(c.part=="leading") s.leading else s.trailing).coordinates)
            val center=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(415f,203f); sample("initial")
            when(c.action) {
                "selection","reverse","shape" -> {
                    mutate(1000,"selected") { s.checked=true }; times(16000,32000,64000)
                    if(c.action=="reverse") mutate(65000,"reverse") { s.checked=false }
                    if(c.action=="shape") mutate(65000,"shape-change") { s.customRestShape=true }
                    times(80000,160000,400000)
                }
                "theme" -> { mutate(1000,"dark") { s.dark=true }; times(16000,400000) }
                "space","enter" -> {
                    advance(1000); (if(c.part=="leading") s.leadingFocus else s.trailingFocus).requestFocus(); host.scheduler.runCurrent(); host.draw(); sample("focus"); times(16000,80000)
                    val key=if(c.action=="space") Key.Spacebar else Key.Enter
                    advance(81000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); sample("key-down"); times(96000,160000)
                    advance(161000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); sample("key-up"); times(176000,400000)
                }
                "hover" -> { input(1000,"move",center); sample("hover"); times(16000,64000,160000); input(161000,"move",outside); sample("exit"); times(176000,400000) }
                else -> {
                    input(1000,"move",center); input(2000,"down",center); sample("press")
                    if(c.action=="quick" || c.action=="disabled-input") { input(5000,"up",center); sample("release"); times(16000,80000,240000,400000) }
                    else {
                        times(16000,32000,64000)
                        if(c.action=="cancel") input(65000,"move",outside)
                        if(c.action=="disable") mutate(65000,"disabled") { if(c.part=="leading") s.leadingEnabled=false else s.trailingEnabled=false }
                        times(80000,160000,240000); input(241000,"up",if(c.action=="cancel") outside else center); sample("release"); times(256000,400000,800000)
                    }
                }
            }
        }
    }
    private class Scene(val c:SplitButtonConfig) {
        var checked by mutableStateOf(c.checked)
        var leadingEnabled by mutableStateOf(c.enabled)
        var trailingEnabled by mutableStateOf(c.enabled)
        var dark by mutableStateOf(c.dark)
        var customRestShape by mutableStateOf(false)
        var leadingClicks=0; var trailingClicks=0
        val split=Probe(); val leading=Probe(); val trailing=Probe(); val leadingContent=Probe(); val trailingContent=Probe()
        val leadingFocus=FocusRequester(); val trailingFocus=FocusRequester()
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val height=when(c.size) { "xsmall"->SplitButtonDefaults.ExtraSmallContainerHeight; "medium"->SplitButtonDefaults.MediumContainerHeight; "large"->SplitButtonDefaults.LargeContainerHeight; "xlarge"->SplitButtonDefaults.ExtraLargeContainerHeight; else->SplitButtonDefaults.SmallContainerHeight }
                        var modifier=split.modifier()
                        if(c.width!=null) modifier=modifier.width(c.width.toFloat().dp)
                        if(c.height!=null) modifier=modifier.height(c.height.toFloat().dp)
                        SplitButtonLayout(modifier=modifier,spacing=c.spacing.toFloat().dp,
                            leadingButton={
                                val colors=when(c.style) { "tonal"->ButtonDefaults.filledTonalButtonColors(); "elevated"->ButtonDefaults.elevatedButtonColors(); "outlined"->ButtonDefaults.outlinedButtonColors(); else->ButtonDefaults.buttonColors() }
                                val elevation=when(c.style) { "tonal"->ButtonDefaults.filledTonalButtonElevation(); "elevated"->ButtonDefaults.elevatedButtonElevation(); "outlined"->null; else->ButtonDefaults.buttonElevation() }
                                SplitButtonDefaults.LeadingButton(onClick={ leadingClicks++ },modifier=leading.modifier().focusRequester(leadingFocus).heightIn(min=height),enabled=leadingEnabled,
                                    shapes=SplitButtonDefaults.leadingButtonShapesFor(height),colors=colors,elevation=elevation,
                                    border=if(c.style=="outlined") ButtonDefaults.outlinedButtonBorder(leadingEnabled) else null,
                                    contentPadding=SplitButtonDefaults.leadingButtonContentPaddingFor(height)) {
                                    Box(leadingContent.modifier().size(32.dp,c.contentHeight.toFloat().dp).background(LocalContentColor.current))
                                }
                            },trailingButton={
                                val colors=when(c.style) { "tonal"->ButtonDefaults.filledTonalButtonColors(); "elevated"->ButtonDefaults.elevatedButtonColors(); "outlined"->ButtonDefaults.outlinedButtonColors(); else->ButtonDefaults.buttonColors() }
                                val elevation=when(c.style) { "tonal"->ButtonDefaults.filledTonalButtonElevation(); "elevated"->ButtonDefaults.elevatedButtonElevation(); "outlined"->null; else->ButtonDefaults.buttonElevation() }
                                val shapes=SplitButtonDefaults.trailingButtonShapesFor(height).let { if(customRestShape) SplitButtonShapes(RoundedCornerShape(8.dp),it.pressedShape,it.checkedShape) else it }
                                val trailingModifier=trailing.modifier().focusRequester(trailingFocus).heightIn(min=height)
                                val border=if(c.style=="outlined") ButtonDefaults.outlinedButtonBorder(trailingEnabled) else null
                                val padding=SplitButtonDefaults.trailingButtonContentPaddingFor(height)
                                val body:@Composable RowScope.()->Unit={ val size=SplitButtonDefaults.trailingButtonIconSizeFor(height); Box(trailingContent.modifier().size(size).background(LocalContentColor.current)) }
                                if(c.toggle) SplitButtonDefaults.TrailingButton(checked=checked,onCheckedChange={ trailingClicks++; checked=it },modifier=trailingModifier,enabled=trailingEnabled,shapes=shapes,colors=colors,elevation=elevation,border=border,contentPadding=padding,content=body)
                                else SplitButtonDefaults.TrailingButton(onClick={ trailingClicks++ },modifier=trailingModifier,enabled=trailingEnabled,shapes=shapes,colors=colors,elevation=elevation,border=border,contentPadding=padding,content=body)
                            })
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun bounds(probe:Probe,c:LayoutCoordinates=checkNotNull(probe.coordinates))=probe.geometry(c).filter { it.name=="box" || it.name=="size" }
            fun button(probe:Probe,id:String,clicks:Int):ProbePoint {
                val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
                val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
                return ProbePoint(id,"button","split",bounds(probe)+listOf(flag("checked",semantics.contains(SemanticsProperties.ToggleableState) && semantics[SemanticsProperties.ToggleableState]==ToggleableState.On,definition="split-button.checked"),
                    flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="split-button.enabled"),number("clicks",clicks.toDouble(),"count",definition="split-button.click-count")))
            }
            fun surface(probe:Probe,id:String):ProbePoint {
                val layers=mutableListOf<Pair<LayoutCoordinates,Any>>()
                var coordinator:Any?=checkNotNull(probe.coordinates)
                while(coordinator!=null) { field(coordinator,"layer")?.let { layers+=(coordinator as LayoutCoordinates) to it }; coordinator=field(coordinator,"wrapped") }
                val clipped=layers.single { (_,layer) -> val graphics=checkNotNull(field(layer,"graphicsLayer")); graphics.javaClass.methods.single { it.name=="getClip" && it.parameterCount==0 }.invoke(graphics)==true }
                // A zero-width constrained leading area really paints Outline.Rectangle, not Rounded.
                val outline=field(clipped.second,"outline") as Outline
                val corners=when(outline) {
                    is Outline.Rounded->outline.roundRect.let { rr->listOf(rr.topLeftCornerRadius.x,rr.topRightCornerRadius.x,rr.bottomRightCornerRadius.x,rr.bottomLeftCornerRadius.x).map { (it/ComposeHost.RenderScale).toDouble() } }
                    is Outline.Rectangle->listOf(0.0,0.0,0.0,0.0)
                    else->error("Unexpected split button outline $outline")
                }
                val elevation=layers.maxOf { (_,layer) -> val graphics=checkNotNull(field(layer,"graphicsLayer")); (graphics.javaClass.methods.single { it.name=="getShadowElevation" && it.parameterCount==0 }.invoke(graphics) as Number).toDouble()/ComposeHost.RenderScale }
                return ProbePoint("$id-container","container",id,bounds(probe,clipped.first)+listOf(vector("radii","rect",corners,"dip","container","motion","split-button.visual-corner-radii",components=listOf("topLeft","topRight","bottomRight","bottomLeft")),number("shadowElevation",elevation,category="motion",definition="button.shadow-elevation")))
            }
            return listOf(ProbePoint("split","split-button",null,bounds(split)),button(leading,"leading",leadingClicks),button(trailing,"trailing",trailingClicks),surface(leading,"leading"),surface(trailing,"trailing"),
                ProbePoint("leading-content","content","leading-container",bounds(leadingContent)),ProbePoint("trailing-content","content","trailing-container",bounds(trailingContent)))
        }
    }
}

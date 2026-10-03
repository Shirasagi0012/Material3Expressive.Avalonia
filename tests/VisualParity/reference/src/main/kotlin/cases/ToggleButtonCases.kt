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
data class ToggleButtonConfig(val style:String="filled",val size:String="small",val checked:Boolean=false,
    val enabled:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,val icon:Boolean=false,val empty:Boolean=false,
    val width:Double?=null,val height:Double?=null,val custom:Boolean=false,val action:String="none")

@ParitySuite("toggle-button")
class ToggleButtonCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val c=writer.readConfig<ToggleButtonConfig>()
        ComposeHost(writer,420,208).use { host ->
            val s=Scene(c); host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",s.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<ToggleButtonConfig>()
        ComposeHost(writer,420,208).use { host ->
            val s=Scene(c); host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure(),image=id!="time-32000"))
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time); change(); writer.record(time,"mutation",id); host.draw(); sample(id) }
            fun input(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw() }
            val coordinates=checkNotNull(s.button.coordinates)
            val center=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(415f,203f); sample("initial")
            when(c.action) {
                "selection","reverse","third","shape" -> {
                    mutate(1000,"selected") { s.checked=true }; times(16000,32000,64000)
                    if(c.action=="reverse") mutate(65000,"reverse") { s.checked=false }
                    if(c.action=="third") { input(65000,"move",center); input(66000,"down",center); sample("third-target") }
                    if(c.action=="shape") mutate(65000,"shape-change") { s.customCheckedShape=true }
                    times(80000,160000,400000)
                    if(c.action=="third") { input(401000,"up",center); sample("release"); times(416000,800000) }
                }
                "theme" -> { mutate(1000,"dark") { s.dark=true }; times(16000,400000) }
                "space","enter" -> {
                    advance(1000); s.requester.requestFocus(); host.scheduler.runCurrent(); host.draw(); sample("focus"); times(16000,80000)
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
                        if(c.action=="disable") mutate(65000,"disabled") { s.enabled=false }
                        times(80000,160000,240000); input(241000,"up",if(c.action=="cancel") outside else center); sample("release"); times(256000,400000,800000)
                    }
                }
            }
        }
    }
    private class Scene(val c:ToggleButtonConfig) {
        var checked by mutableStateOf(c.checked)
        var enabled by mutableStateOf(c.enabled)
        var dark by mutableStateOf(c.dark)
        var customCheckedShape by mutableStateOf(false)
        var clicks=0
        val button=Probe(); val content=Probe(); val icon=Probe(); val requester=FocusRequester()
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val size=when(c.size) { "xsmall"->ToggleButtonSize.ExtraSmall; "medium"->ToggleButtonSize.Medium; "large"->ToggleButtonSize.Large; "xlarge"->ToggleButtonSize.ExtraLarge; else->ToggleButtonSize.Small }
                        var modifier=button.modifier().focusRequester(requester)
                        if(c.width!=null) modifier=modifier.width(c.width.toFloat().dp)
                        if(c.height!=null) modifier=modifier.height(c.height.toFloat().dp)
                        val shapes=ToggleButtonDefaults.shapesFor(size).let { if(customCheckedShape) it.copy(checkedShape=RoundedCornerShape(18.dp)) else it }
                        val colors=if(c.custom) ToggleButtonColors(Color(0xFF126E60),Color(0xFFF0E080),Color(0x70336699),Color(0x50556677),Color(0xFF335588),Color(0xFFFFEEDD))
                            else when(c.style) { "tonal"->FilledTonalToggleButtonDefaults.colors(); "elevated"->ElevatedToggleButtonDefaults.colors(); "outlined"->OutlinedToggleButtonDefaults.colors(); else->ToggleButtonDefaults.colors() }
                        val iconContent:(@Composable ()->Unit)?=if(c.icon) ({ Box(icon.modifier().fillMaxSize().background(LocalContentColor.current)) }) else null
                        val body:@Composable RowScope.()->Unit={ if(!c.empty) Box(content.modifier().size(32.dp,16.dp).background(LocalContentColor.current)) }
                        val change:(Boolean)->Unit={ clicks++; checked=it }
                        when(c.style) {
                            "tonal"->FilledTonalToggleButton(checked,change,modifier,buttonSize=size,enabled=enabled,icon=iconContent,shapes=shapes,colors=colors,content=body)
                            "elevated"->ElevatedToggleButton(checked,change,modifier,buttonSize=size,enabled=enabled,icon=iconContent,shapes=shapes,colors=colors,content=body)
                            "outlined"->OutlinedToggleButton(checked,change,modifier,buttonSize=size,enabled=enabled,icon=iconContent,shapes=shapes,colors=colors,content=body)
                            else->ToggleButton(checked,change,modifier,buttonSize=size,enabled=enabled,icon=iconContent,shapes=shapes,colors=colors,content=body)
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            val root=checkNotNull(button.coordinates)
            val node=checkNotNull(field(root,"layoutNode"))
            val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
            // Passive reads of the actual RippleNode Animatable; do not invoke draw or sample time.
            val visited=java.util.Collections.newSetFromMap(java.util.IdentityHashMap<Any,Boolean>())
            fun optionalField(current:Any,name:String):Any? = runCatching { field(current,name) }.getOrNull()
            fun rippleAlpha(current:Any?):Any? {
                if(current==null || !visited.add(current)) return null
                optionalField(current,"animatedAlpha")?.let { return it }
                return rippleAlpha(optionalField(current,"delegate")) ?: rippleAlpha(optionalField(current,"child"))
            }
            val alpha=checkNotNull(rippleAlpha(field(checkNotNull(field(node,"nodes")),"head")))
            val stateAlpha=(alpha.javaClass.methods.single { it.name=="getValue" && it.parameterCount==0 }.invoke(alpha) as Number).toDouble()
            val layers=mutableListOf<Pair<LayoutCoordinates,Any>>()
            var coordinator:Any?=root
            while(coordinator!=null) { field(coordinator,"layer")?.let { layers+=(coordinator as LayoutCoordinates) to it }; coordinator=field(coordinator,"wrapped") }
            val clipped=layers.single { (_,layer) -> val graphics=checkNotNull(field(layer,"graphicsLayer")); graphics.javaClass.methods.single { it.name=="getClip" && it.parameterCount==0 }.invoke(graphics)==true }
            val rr=(field(clipped.second,"outline") as Outline.Rounded).roundRect
            val corners=listOf(rr.topLeftCornerRadius.x,rr.topRightCornerRadius.x,rr.bottomRightCornerRadius.x,rr.bottomLeftCornerRadius.x).map { (it/ComposeHost.RenderScale).toDouble() }
            val elevation=layers.maxOf { (_,layer) -> val graphics=checkNotNull(field(layer,"graphicsLayer")); (graphics.javaClass.methods.single { it.name=="getShadowElevation" && it.parameterCount==0 }.invoke(graphics) as Number).toDouble()/ComposeHost.RenderScale }
            return listOf(ProbePoint("button","toggle-button",null,button.geometry().filter { it.name=="box" || it.name=="size" }+listOf(
                flag("checked",semantics[SemanticsProperties.ToggleableState]==ToggleableState.On,definition="toggle-button.checked"),flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="toggle-button.enabled"),flag("focused",semantics.getOrElse(SemanticsProperties.Focused) { false },definition="button.focused"),number("clicks",clicks.toDouble(),"count",definition="toggle-button.click-count"))),
                ProbePoint("container","container","button",button.geometry(clipped.first).filter { it.name=="box" || it.name=="size" }+listOf(vector("corners","rect",corners,"dip","container","motion","toggle-button.visual-corner-radii",components=listOf("topLeft","topRight","bottomRight","bottomLeft")),number("shadowElevation",elevation,category="motion",definition="button.shadow-elevation"),number("stateLayerAlpha",stateAlpha,"ratio",category="motion",definition="button.state-layer-alpha"))))+
                (if(c.empty) emptyList() else listOf(ProbePoint("content","content","container",content.geometry().filter { it.name=="box" || it.name=="size" })))+
                (if(c.icon) listOf(ProbePoint("icon","icon","container",icon.geometry().filter { it.name=="box" || it.name=="size" })) else emptyList())
        }
    }
}

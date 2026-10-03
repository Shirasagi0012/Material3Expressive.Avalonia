@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class CheckBoxConfig(val api:String="tri", val state:String="off", val enabled:Boolean=true,
    val dark:Boolean=false, val rtl:Boolean=false, val customColors:Boolean=false, val stroke:String="default",
    val width:Double?=null, val height:Double?=null, val action:String="none", val target:String="on", val standardMotion:Boolean=false)

@ParitySuite("checkbox")
class CheckBoxCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<CheckBoxConfig>()
        ComposeHost(writer,160,128).use { host ->
            val scene=CheckScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<CheckBoxConfig>()
        ComposeHost(writer,160,128).use { host ->
            val scene=CheckScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time); change(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            fun event(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw(); host.frame(time) }
            val c=checkNotNull(scene.probe.coordinates)
            val center=c.localToRoot(Offset(c.size.width/2f,c.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(155f,123f)
            sample("initial")
            when(config.action) {
                "transition","reverse","third" -> {
                    mutate(1000,"state-change") { scene.state=value(config.target) }; times(16000,32000,64000)
                    if(config.action=="reverse") mutate(65000,"state-reverse") { scene.state=value(config.state) }
                    if(config.action=="third") mutate(65000,"third-state") { scene.state=ToggleableState.Indeterminate }
                    times(96000,112000,160000,240000,400000,800000)
                }
                "enable" -> {
                    mutate(1000,"disabled") { scene.enabled=false }; times(16000,64000,160000)
                    mutate(161000,"enabled") { scene.enabled=true }; times(176000,240000,400000)
                }
                "theme" -> {
                    mutate(1000,"dark") { scene.dark=true }; times(16000,64000,160000,400000)
                    mutate(401000,"light") { scene.dark=false }; times(416000,480000,800000)
                }
                "hover" -> {
                    event(1000,"move",center); sample("hover"); times(16000,80000)
                    event(81000,"move",outside); sample("exit"); times(96000,240000,400000)
                }
                "space","enter" -> {
                    advance(1000); scene.requester.requestFocus(); host.draw(); sample("focus"); times(16000,80000)
                    val key=if(config.action=="space") Key.Spacebar else Key.Enter
                    advance(81000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); host.frame(81000); sample("key-down"); times(96000,160000)
                    advance(161000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); host.frame(161000); sample("key-up"); times(176000,400000,800000)
                }
                else -> {
                    event(1000,"move",center); event(2000,"down",center); sample("press")
                    if(config.action in listOf("quick","disabled-input")) {
                        event(5000,"up",center); sample("release"); times(16000,80000,240000,400000,800000)
                    } else {
                        times(16000,64000)
                        if(config.action=="disable-press") mutate(65000,"disabled") { scene.enabled=false }
                        if(config.action=="cancel") event(65000,"move",outside)
                        times(80000,160000,240000)
                        event(241000,"up",if(config.action=="cancel") outside else center); sample("release"); times(256000,320000,400000,800000)
                    }
                }
            }
        }
    }

    private class CheckScene(val config:CheckBoxConfig) {
        var state by mutableStateOf(value(config.state))
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var clicks=0
        val probe=Probe()
        val requester=FocusRequester()
        @Composable
        fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),
                motionScheme=if(config.standardMotion) MotionScheme.standard() else MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                    LocalContentColor provides MaterialTheme.colorScheme.onSurface) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=probe.modifier().focusRequester(requester)
                        if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                        if(config.height!=null) modifier=modifier.height(config.height.toFloat().dp)
                        val colors=if(config.customColors) CheckboxColors(
                            Color(0xFFF0E080),Color(0x40AABBCC),Color(0xFF126E60),Color(0x40FF0000),
                            Color(0x70336699),Color(0x20223344),Color(0x80553677),Color(0xFF335588),
                            Color(0xFF895210),Color(0x90556677),Color(0x50365A80),Color(0xA0775533),Color(0xFF008800))
                        else CheckboxDefaults.colors()
                        val stroke=with(LocalDensity.current) {
                            val width=(if(config.stroke=="thick") 5.dp else if(config.stroke=="round") 3.dp else 2.dp).toPx()
                            Stroke(width=width,cap=if(config.stroke=="round") StrokeCap.Round else StrokeCap.Square,
                                join=if(config.stroke=="round") StrokeJoin.Round else StrokeJoin.Miter,
                                pathEffect=if(config.stroke=="dash") PathEffect.dashPathEffect(floatArrayOf(3.dp.toPx(),2.dp.toPx())) else null)
                        }
                        if(config.api=="boolean") {
                            val onChange:(Boolean)->Unit={ clicks++; state=ToggleableState(it) }
                            if(config.stroke=="default") Checkbox(state==ToggleableState.On,onChange,modifier,enabled,colors)
                            else Checkbox(state==ToggleableState.On,onChange,stroke,stroke,modifier,enabled,colors)
                        } else {
                            // Real Compose callback ownership. Never reproduce native three-state cycling in the reference.
                            val click:()->Unit={ clicks++; state=if(state==ToggleableState.On) ToggleableState.Off else ToggleableState.On }
                            if(config.stroke=="default") TriStateCheckbox(state,click,modifier,enabled,colors)
                            else TriStateCheckbox(state,click,stroke,stroke,modifier,enabled,colors)
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
            val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
            val actual=semantics[SemanticsProperties.ToggleableState]
            return listOf(ProbePoint("checkbox","checkbox",null,
                probe.geometry().filter { it.name=="box" || it.name=="size" }+listOf(
                    number("state",when(actual) { ToggleableState.Off -> 0.0; ToggleableState.On -> 1.0; ToggleableState.Indeterminate -> 2.0 },
                        "enum",definition="checkbox.toggle-state",source="cached triStateToggleable semantics: Off=0, On=1, Indeterminate=2"),
                    flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="checkbox.enabled"),
                    number("clicks",clicks.toDouble(),"count",definition="checkbox.click-callback-count"))))
        }
    }
    companion object {
        fun value(state:String):ToggleableState=when(state) { "off" -> ToggleableState.Off; "on" -> ToggleableState.On; "indeterminate" -> ToggleableState.Indeterminate; else -> error(state) }
    }
}

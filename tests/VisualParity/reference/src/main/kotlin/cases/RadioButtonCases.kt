@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class RadioButtonConfig(val selected:Boolean=false, val enabled:Boolean=true, val dark:Boolean=false,
    val rtl:Boolean=false, val customColors:Boolean=false, val width:Double?=null, val height:Double?=null,
    val action:String="none", val standardMotion:Boolean=false)

@ParitySuite("radio-button")
class RadioButtonCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<RadioButtonConfig>()
        ComposeHost(writer,160,128).use { host ->
            val scene=RadioScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<RadioButtonConfig>()
        ComposeHost(writer,160,128).use { host ->
            val scene=RadioScene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=id !in listOf("time-32000","time-112000")))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time); change(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            fun event(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw(); host.frame(time) }
            fun center(probe:Probe):Offset {
                val c=checkNotNull(probe.coordinates)
                return c.localToRoot(Offset(c.size.width/2f,c.size.height/2f))/ComposeHost.RenderScale.toFloat()
            }
            var center=center(scene.probe)
            val outside=Offset(155f,123f)
            sample("initial")
            when(config.action) {
                "transition","reverse","disable-transition" -> {
                    mutate(1000,"selection-change") { scene.selected=!config.selected }; times(16000,32000,64000)
                    if(config.action=="reverse") mutate(65000,"selection-reverse") { scene.selected=config.selected }
                    if(config.action=="disable-transition") mutate(65000,"disabled") { scene.enabled=false }
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
                "colors" -> {
                    mutate(1000,"custom-colors") { scene.customColors=true }; times(16000,64000,160000,400000)
                    mutate(401000,"theme-colors") { scene.customColors=false }; times(416000,480000,800000)
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
                "group" -> {
                    val second=center(scene.secondProbe)
                    event(1000,"move",second); event(2000,"down",second); event(5000,"up",second); sample("second-selected"); times(16000,64000,400000)
                    event(401000,"move",center); event(402000,"down",center); event(405000,"up",center); sample("first-selected"); times(416000,480000,800000)
                }
                else -> {
                    if(config.action=="edge") center=checkNotNull(scene.probe.coordinates).localToRoot(Offset(2f,2f)*ComposeHost.RenderScale.toFloat())/ComposeHost.RenderScale.toFloat()
                    event(1000,"move",center); event(2000,"down",center); sample("press")
                    if(config.action in listOf("quick","disabled-input","request-only")) {
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

    private class RadioScene(val config:RadioButtonConfig) {
        var selected by mutableStateOf(config.action=="group" || config.selected)
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var customColors by mutableStateOf(config.customColors)
        var clicks=0
        val probe=Probe()
        val secondProbe=Probe()
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
                        val colors=if(customColors) RadioButtonColors(Color(0xFF126E60),Color(0xA0895210),Color(0x70336699),Color(0x50556677))
                            else RadioButtonDefaults.colors()
                        // Real Compose ownership: this callback can decline a selection request.
                        val click:()->Unit={ clicks++; if(config.action!="request-only") selected=true }
                        if(config.action=="group") Row {
                            RadioButton(selected,click,modifier,enabled,colors)
                            RadioButton(!selected,{ clicks++; selected=false },secondProbe.modifier(),enabled,colors)
                        } else RadioButton(selected,click,modifier,enabled,colors)
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun point(probe:Probe,id:String):ProbePoint {
                val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
                val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
                return ProbePoint(id,"radio-button",null,probe.geometry().filter { it.name=="box" || it.name=="size" }+listOf(
                    flag("selected",semantics[SemanticsProperties.Selected],definition="radio-button.selected",source="cached selectable semantics"),
                    flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="radio-button.enabled"),
                    number("clicks",clicks.toDouble(),"count",definition="radio-button.click-callback-count")))
            }
            return listOf(point(probe,"radio")) + if(config.action=="group") listOf(point(secondProbe,"second")) else emptyList()
        }
    }
}

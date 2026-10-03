@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
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
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class SwitchConfig(val state:String="off", val enabled:Boolean=true, val dark:Boolean=false,
    val rtl:Boolean=false, val thumb:String="none", val customColors:Boolean=false, val width:Double?=null,
    val height:Double?=null, val action:String="none", val target:String="on", val standardMotion:Boolean=false)

@ParitySuite("switch")
class SwitchCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val c=writer.readConfig<SwitchConfig>()
        ComposeHost(writer,160,128).use { host ->
            val s=SwitchScene(c)
            host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",s.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<SwitchConfig>()
        ComposeHost(writer,160,128).use { host ->
            val s=SwitchScene(c)
            host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure(),image=id!="time-32000"))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time); change(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            fun event(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw(); host.frame(time) }
            val coordinates=checkNotNull(s.probe.coordinates)
            val center=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(155f,123f)
            sample("initial")
            when(c.action) {
                "transition","reverse" -> {
                    mutate(1000,"state-change") { s.checked=c.target=="on" }; times(16000,32000,64000)
                    if(c.action=="reverse") mutate(65000,"state-reverse") { s.checked=c.state=="on" }
                    times(96000,112000,160000,240000,400000,800000)
                }
                "enable" -> {
                    mutate(1000,"disabled") { s.enabled=false }; times(16000,64000,160000)
                    mutate(161000,"enabled") { s.enabled=true }; times(176000,240000,400000)
                }
                "theme" -> {
                    mutate(1000,"dark") { s.dark=true }; times(16000,64000,160000,400000)
                    mutate(401000,"light") { s.dark=false }; times(416000,480000,800000)
                }
                "content" -> {
                    mutate(1000,"icon") { s.thumb="icon" }; times(16000,64000,400000)
                    mutate(401000,"empty") { s.thumb="empty" }; times(416000,480000,800000)
                }
                "hover" -> {
                    event(1000,"move",center); sample("hover"); times(16000,80000)
                    event(81000,"move",outside); sample("exit"); times(96000,240000,400000)
                }
                "space","enter" -> {
                    advance(1000); s.requester.requestFocus(); host.draw(); sample("focus"); times(16000,80000)
                    val key=if(c.action=="space") Key.Spacebar else Key.Enter
                    advance(81000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); host.frame(81000); sample("key-down"); times(96000,160000)
                    advance(161000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); host.frame(161000); sample("key-up"); times(176000,400000,800000)
                }
                "drag","drag-end","drag-return","drag-cancel","warm-drag" -> {
                    var dragTime=0L
                    if(c.action=="warm-drag") {
                        mutate(1000,"state-change") { s.checked=c.state!="on" }; times(16000,400000)
                        mutate(401000,"state-restore") { s.checked=c.state=="on" }; times(416000,800000)
                        dragTime=800000
                    }
                    val t=s.thumbCoordinates()
                    val start=t.localToRoot(Offset(t.size.width/2f,t.size.height/2f))/ComposeHost.RenderScale.toFloat()
                    val direction=(if(c.state=="off") 1 else -1)*(if(c.rtl) -1 else 1)
                    var end=start+Offset(direction*(if(c.action=="drag-end") 20f else 16f),0f)
                    event(dragTime+1000,"move",start); event(dragTime+2000,"down",start); sample("press")
                    event(dragTime+16000,"move",end); sample("drag")
                    if(c.action=="drag-return") {
                        end=start+Offset(direction*8f,0f)
                        event(dragTime+48000,"move",end); sample("drag-return")
                    }
                    if(c.action=="drag-cancel") {
                        event(dragTime+48000,"move",outside); sample("cancel")
                        times(dragTime+64000,dragTime+80000,dragTime+160000,dragTime+400000)
                        event(dragTime+401000,"up",outside); sample("release"); times(dragTime+416000,dragTime+800000)
                    } else {
                        advance(dragTime+64000); host.draw(); sample("pre-release")
                        host.input("up",end); host.draw(); sample("release-before-frame")
                        host.frame(dragTime+64000); sample("release")
                        times(dragTime+80000,dragTime+96000,dragTime+160000,dragTime+400000,dragTime+800000)
                    }
                }
                else -> {
                    var pressTime=2000L
                    if(c.action=="warm-press") {
                        mutate(1000,"state-change") { s.checked=c.target=="on" }; times(16000,400000)
                        pressTime=402000
                    }
                    event(pressTime-1000,"move",center); event(pressTime,"down",center); sample("press")
                    if(c.action in listOf("quick","disabled-input")) {
                        event(5000,"up",center); sample("release"); times(16000,80000,240000,400000,800000)
                    } else {
                        times(pressTime+14000,pressTime+62000)
                        if(c.action=="disable-press") mutate(65000,"disabled") { s.enabled=false }
                        if(c.action=="cancel") event(65000,"move",outside)
                        times(pressTime+78000,pressTime+158000,pressTime+238000)
                        event(pressTime+239000,"up",if(c.action=="cancel") outside else center); sample("release")
                        times(pressTime+254000,pressTime+318000,pressTime+398000,pressTime+798000)
                    }
                }
            }
        }
    }

    private class SwitchScene(val c:SwitchConfig) {
        var checked by mutableStateOf(c.state=="on")
        var enabled by mutableStateOf(c.enabled)
        var dark by mutableStateOf(c.dark)
        var thumb by mutableStateOf(c.thumb)
        var callbacks=0
        val probe=Probe()
        val requester=FocusRequester()
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),
                motionScheme=if(c.standardMotion) MotionScheme.standard() else MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                    LocalContentColor provides MaterialTheme.colorScheme.onSurface) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=probe.modifier().focusRequester(requester)
                        if(c.width!=null) modifier=modifier.width(c.width.toFloat().dp)
                        if(c.height!=null) modifier=modifier.height(c.height.toFloat().dp)
                        val colors=if(c.customColors) SwitchColors(
                            Color(0xFFF0E080),Color(0xFF126E60),Color(0x80335588),Color(0xFF895210),
                            Color(0xFFAABBCC),Color(0x40FF0000),Color(0xFF335588),Color(0xFF126E60),
                            Color(0x70336699),Color(0x20223344),Color(0x80553677),Color(0xFF008800),
                            Color(0x90556677),Color(0x50365A80),Color(0xA0775533),Color(0xFFF0E080))
                            else SwitchDefaults.colors()
                        Switch(checked=checked,onCheckedChange={ callbacks++; checked=it },modifier=modifier,
                            enabled=enabled,colors=colors,
                            thumbContent=if(thumb=="none") null else ({
                                val large=thumb=="large" || thumb=="required-large"
                                val width=if(thumb=="empty") 0.dp else if(large) 40.dp else 16.dp
                                val height=if(thumb=="empty" || thumb=="zero-height") 0.dp else if(large) 40.dp else 16.dp
                                val size=if(thumb=="required-large") Modifier.requiredSize(width,height) else Modifier.size(width,height)
                                Box(size.background(LocalContentColor.current))
                            }))
                    }
                }
            }
        }

        // Read the fixed component's actual layout node; no copied thumb algorithm.
        fun thumbCoordinates():LayoutCoordinates {
            val root=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
            fun find(node:Any):Any? {
                val nodes=checkNotNull(field(node,"nodes"))
                val head=checkNotNull(field(nodes,"head"))
                if(generateSequence(head) { field(it,"child") }.any { it.javaClass.name=="androidx.compose.material3.ThumbNode" })
                    return field(nodes,"innerCoordinator")
                val vector=checkNotNull(field(checkNotNull(field(node,"_foldedChildren")),"vector"))
                val content=field(vector,"content") as Array<*>
                val count=field(vector,"size") as Int
                for(i in 0 until count) find(checkNotNull(content[i]))?.let { return it }
                return null
            }
            return checkNotNull(find(root)) as LayoutCoordinates
        }

        fun measure():List<ProbePoint> {
            val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
            val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
            val actual=semantics[SemanticsProperties.ToggleableState]
            return listOf(ProbePoint("switch","switch",null,
                probe.geometry().filter { it.name=="box" || it.name=="size" }+listOf(
                    flag("checked",actual==ToggleableState.On,definition="switch.checked"),
                    flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="switch.enabled"),
                    number("callbacks",callbacks.toDouble(),"count",definition="switch.click-callback-count"))),
                ProbePoint("thumb","thumb","switch",probe.geometry(thumbCoordinates()).filter { it.name=="box" || it.name=="size" }))
        }
    }
}

@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
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
data class ChipConfig(val kind:String="assist", val morph:Boolean=false, val selected:Boolean=false,
    val enabled:Boolean=true, val dark:Boolean=false, val icons:String="none", val rtl:Boolean=false,
    val width:Double?=null, val height:Double?=null, val contentWidth:Double=32.0, val contentHeight:Double=16.0,
    val custom:String="none", val action:String="none")

@ParitySuite("chip")
class ChipCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<ChipConfig>()
        ComposeHost(writer,if(config.contentWidth>1000) 1400 else 320,160).use { host ->
            val scene=ChipScene(config)
            host.mount { scene.Content() }; host.draw(); host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }

    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<ChipConfig>()
        ComposeHost(writer,320,160).use { host ->
            val scene=ChipScene(config)
            host.mount { scene.Content() }; host.draw(); host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=id!="time-32000"))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            // Read after the requested property update has passed a composition frame.
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time); change(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            fun event(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw(); host.frame(time) }
            val coordinates=checkNotNull(scene.probe.coordinates)
            val center=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(8f,8f)
            sample("initial")
            when(config.action) {
                "resize" -> {
                    mutate(1000,"selected") { scene.selected=true }; times(16000,64000,800000)
                    mutate(801000,"taller") { scene.height=64.0 }; times(816000,960000)
                    mutate(961000,"shorter") { scene.height=24.0 }; times(976000,1200000)
                }
                "shapes" -> {
                    mutate(1000,"selected") { scene.selected=true }; times(16000,32000,64000)
                    mutate(65000,"shapes") { scene.customShapes=ChipShapes(RoundedCornerShape(6.dp),RoundedCornerShape(14.dp),RoundedCornerShape(4.dp)) }; times(80000,160000)
                    mutate(161000,"unselected") { scene.selected=false }; times(176000,240000)
                    mutate(241000,"equal-shapes") { scene.customShapes=ChipShapes(RoundedCornerShape(6.dp),RoundedCornerShape(14.dp),RoundedCornerShape(4.dp)) }; times(256000,400000)
                }
                "icons","reverse-icons" -> {
                    mutate(1000,"icons-enter") { scene.icons="both" }; times(16000,32000,64000)
                    mutate(if(config.action=="reverse-icons") 65000 else 401000,"icons-exit") { scene.icons="none" }
                    times(if(config.action=="reverse-icons") 80000 else 416000,480000,640000,1000000)
                }
                "avatar" -> {
                    mutate(1000,"avatar") { scene.icons="avatar" }; times(16000,64000,400000)
                    mutate(401000,"icon") { scene.icons="start" }; times(416000,480000,800000)
                }
                "selection","reverse-selection" -> {
                    mutate(1000,"selected") { scene.selected=true }; times(16000,32000,64000)
                    if(config.action=="reverse-selection") mutate(65000,"unselected") { scene.selected=false }
                    times(80000,160000,400000,800000)
                }
                "theme" -> { mutate(1000,"dark") { scene.dark=true }; times(16000,80000,400000) }
                "hover" -> {
                    event(1000,"move",center); sample("hover"); times(16000,64000,160000)
                    event(161000,"move",outside); sample("exit"); times(176000,240000,400000)
                }
                "space","enter" -> {
                    advance(1000); scene.requester.requestFocus(); host.draw(); sample("focus"); times(16000,80000)
                    val key=if(config.action=="space") Key.Spacebar else Key.Enter
                    advance(81000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); host.frame(81000); sample("key-down"); times(96000,160000)
                    advance(161000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); host.frame(161000); sample("key-up"); times(176000,400000)
                }
                else -> {
                    event(1000,"move",center); event(2000,"down",center); sample("press")
                    if(config.action in listOf("quick","disabled-input","request-only")) {
                        event(5000,"up",center); sample("release"); times(16000,80000,240000,400000)
                    } else {
                        times(16000,64000)
                        if(config.action=="cancel") event(65000,"move",outside)
                        if(config.action=="disable-press") mutate(65000,"disabled") { scene.enabled=false }
                        times(80000,160000)
                        event(161000,"up",if(config.action=="cancel") outside else center); sample("release"); times(176000,240000,400000,800000)
                    }
                }
            }
        }
    }

    private class ChipScene(val config:ChipConfig) {
        var selected by mutableStateOf(config.selected)
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var icons by mutableStateOf(config.icons)
        var height by mutableStateOf(config.height)
        var customShapes by mutableStateOf<ChipShapes?>(null)
        var clicks=0
        val probe=Probe()
        val labelProbe=Probe()
        val requester=FocusRequester()

        @Composable fun Marker(width:Double,height:Double,modifier:Modifier=Modifier) {
            val color=LocalContentColor.current
            Canvas(modifier.size(width.toFloat().dp,height.toFloat().dp)) { drawRect(color) }
        }

        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=probe.modifier().focusRequester(requester)
                        if(config.width!=null) modifier=modifier.width(config.width.toFloat().dp)
                        height?.let { modifier=modifier.height(it.toFloat().dp) }
                        val leading:(@Composable ()->Unit)?=if(icons in listOf("start","both","avatar-both")) { { Marker(18.0,18.0) } } else null
                        val trailing:(@Composable ()->Unit)?=if(icons in listOf("end","both","avatar-both")) { { Marker(18.0,18.0) } } else null
                        val avatar:(@Composable ()->Unit)?=if(icons.startsWith("avatar")) { { Marker(24.0,24.0) } } else null
                        val label:@Composable ()->Unit={ Marker(config.contentWidth,config.contentHeight,labelProbe.modifier()) }
                        val click:()->Unit={ clicks++; if(config.kind in listOf("filter","elevated-filter","input") && config.action!="request-only") selected=!selected }
                        val arrangement=if(config.custom=="spacing") AssistChipDefaults.horizontalArrangement(6.dp)
                            else if(config.morph) FilterChipDefaults.horizontalArrangement(leading!=null || avatar!=null,trailing!=null)
                            else AssistChipDefaults.horizontalArrangement()
                        val padding=if(config.custom=="spacing") PaddingValues(start=5.dp,top=2.dp,end=11.dp,bottom=4.dp)
                            else if(config.kind=="input") InputChipDefaults.contentPadding(avatar!=null,leading!=null,trailing!=null)
                            else AssistChipDefaults.ContentPadding
                        val elevated=config.kind.startsWith("elevated")
                        val basic=if(config.kind.contains("assist")) {
                            if(elevated) AssistChipDefaults.elevatedAssistChipColors() else AssistChipDefaults.assistChipColors()
                        } else {
                            if(elevated) SuggestionChipDefaults.elevatedSuggestionChipColors() else SuggestionChipDefaults.suggestionChipColors()
                        }
                        val selectable=if(config.kind=="input") {
                            if(config.morph) InputChipDefaults.tonalInputChipColors() else InputChipDefaults.inputChipColors()
                        } else if(elevated) {
                            if(config.morph) FilterChipDefaults.tonalElevatedFilterChipColors() else FilterChipDefaults.elevatedFilterChipColors()
                        } else {
                            if(config.morph) FilterChipDefaults.tonalFilterChipColors() else FilterChipDefaults.filterChipColors()
                        }
                        val basicColors=if(config.custom=="colors") ChipColors(Color(0xB0336699),Color(0xFF126E60),Color(0xFF925210),Color(0xFF643399),
                            Color(0x50336699),Color(0x70556677),Color(0x70925210),Color(0x70643399)) else basic
                        val selectableColors=if(config.custom=="colors") SelectableChipColors(Color(0xB0336699),Color(0xFF126E60),Color(0xFF925210),Color(0xFF643399),
                            Color(0x50336699),Color(0x70556677),Color(0x70925210),Color(0x70643399),Color(0xB0882255),Color(0x50882255),Color(0xFF336622),Color(0xFF992255),Color(0xFF225599)) else selectable
                        val border=if(config.custom=="border") BorderStroke(2.dp,Color.Red)
                            else when(config.kind) {
                                "assist"->AssistChipDefaults.assistChipBorder(enabled)
                                "suggestion"->SuggestionChipDefaults.suggestionChipBorder(enabled)
                                "filter"->FilterChipDefaults.filterChipBorder(enabled,selected)
                                "input"->InputChipDefaults.inputChipBorder(enabled,selected)
                                else->null
                            }
                        val basicElevation=if(config.custom=="elevation") ChipElevation(6.dp,6.dp,6.dp,6.dp,6.dp,6.dp)
                            else if(config.kind.contains("assist")) {
                                if(elevated) AssistChipDefaults.elevatedAssistChipElevation() else AssistChipDefaults.assistChipElevation()
                            } else {
                                if(elevated) SuggestionChipDefaults.elevatedSuggestionChipElevation() else SuggestionChipDefaults.suggestionChipElevation()
                            }
                        val selectableElevation=if(config.kind=="input") InputChipDefaults.inputChipElevation()
                            else if(elevated) FilterChipDefaults.elevatedFilterChipElevation() else FilterChipDefaults.filterChipElevation()
                        val shapes=customShapes ?: if(config.kind=="input") InputChipDefaults.shapes() else FilterChipDefaults.shapes()
                        when(config.kind) {
                            "assist","elevated-assist" -> AssistChip(click,label,modifier,enabled,leading,trailing,colors=basicColors,elevation=basicElevation,border=border,horizontalArrangement=arrangement,contentPadding=padding)
                            "suggestion","elevated-suggestion" -> SuggestionChip(click,label,modifier,enabled,leading,colors=basicColors,elevation=basicElevation,border=border,horizontalArrangement=arrangement,contentPadding=padding)
                            "filter","elevated-filter" -> if(config.morph) FilterChip(selected,click,label,shapes,modifier,enabled,leading,trailing,selectableColors,selectableElevation,border,arrangement,padding)
                                else FilterChip(selected,click,label,modifier,enabled,leading,trailing,colors=selectableColors,elevation=selectableElevation,border=border,horizontalArrangement=arrangement,contentPadding=padding)
                            "input" -> if(config.morph) InputChip(selected,click,label,shapes,modifier,enabled,leading,avatar,trailing,selectableColors,selectableElevation,border,arrangement,padding)
                                else InputChip(selected,click,label,modifier,enabled,leading,avatar,trailing,colors=selectableColors,elevation=selectableElevation,border=border,horizontalArrangement=arrangement,contentPadding=padding)
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
            val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
            val selectable=config.kind in listOf("filter","elevated-filter","input")
            var coordinator:Any?=probe.coordinates
            var elevation=0.0
            while(coordinator!=null) {
                val layer=field(coordinator,"layer")
                if(layer!=null) {
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    elevation=maxOf(elevation,(graphics.javaClass.methods.single { it.name=="getShadowElevation" && it.parameterCount==0 }.invoke(graphics) as Number).toDouble())
                }
                coordinator=field(coordinator,"wrapped")
            }
            return listOf(
                ProbePoint("chip","chip",null,probe.geometry().filter { it.name=="box" || it.name=="size" }+listOf(
                    flag("selected",if(selectable) semantics[SemanticsProperties.Selected] else false,definition="chip.selected"),
                    flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="chip.enabled"),
                    number("shadowElevation",elevation/ComposeHost.RenderScale,"dip",category="motion",definition="chip.shadow-elevation",source="Surface graphics layer shadowElevation / density"),
                    number("clicks",clicks.toDouble(),"count",definition="chip.click-callback-count"))),
                ProbePoint("label","content","chip",labelProbe.geometry().filter { it.name=="box" || it.name=="size" }))
        }
    }
}

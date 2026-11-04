@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.FocusInteraction
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.AbsoluteRoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class FocusRingConfig(val kind:String="button", val shape:String="round", val action:String="focus",
    val dark:Boolean=false, val custom:Boolean=false, val checked:Boolean=false, val morph:Boolean=false)

@ParitySuite("focus-ring")
class FocusRingCases {
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<FocusRingConfig>()
        val probe=Probe(); val requester=FocusRequester()
        var focusManager:FocusManager?=null
        var width by mutableStateOf(100f); var enabled by mutableStateOf(true)
        var dark by mutableStateOf(c.dark); var inset by mutableStateOf(c.action!="press-to-ring")
        val source=MutableInteractionSource()
        var cardSource by mutableStateOf(source)
        var clickable by mutableStateOf(true); var mounted by mutableStateOf(true)
        var currentSecondary=Color.Unspecified
        ComposeHost(writer,240,160).use { host ->
            host.mount {
                focusManager=LocalFocusManager.current
                MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(), motionScheme=MotionScheme.expressive()) {
                    currentSecondary=MaterialTheme.colorScheme.secondary
                    val theme=if(!inset) RippleDefaults.OpacityFocusThemeConfiguration
                        else if(c.custom) RippleThemeConfiguration(RippleThemeConfiguration.Focus.InsetRing(1.1.dp,1.3.dp,2.4.dp,2.7.dp))
                        else RippleDefaults.InsetFocusRingThemeConfiguration
                    val local=if(c.custom) RippleConfiguration(focus=RippleConfiguration.Focus.InsetRing(Color(0x806600CC),Color(0xC000CC66))) else RippleConfiguration()
                    // Match the native selection-control themes' OnSurface foreground, as in
                    // CheckBoxCases/RadioButtonCases. MaterialTheme alone leaves content color black.
                    CompositionLocalProvider(LocalRippleThemeConfiguration provides theme, LocalRippleConfiguration provides local,
                        LocalContentColor provides if(c.kind.endsWith("-chip")) Color.Black else MaterialTheme.colorScheme.onSurface) {
                        Box(Modifier.fillMaxSize().background(Color.White), contentAlignment=androidx.compose.ui.Alignment.Center) {
                            val modifier=probe.modifier().focusRequester(requester)
                            if(c.kind.endsWith("-surface")) {
                                if(mounted) {
                                    val surfaceModifier=modifier.size(width.dp,50.dp)
                                    val shape=AbsoluteRoundedCornerShape(16.dp)
                                    when(c.kind) {
                                        "select-surface" -> Surface(selected=true,onClick={},modifier=surfaceModifier,enabled=enabled,shape=shape,interactionSource=cardSource) {}
                                        "toggle-surface" -> Surface(checked=true,onCheckedChange={},modifier=surfaceModifier,enabled=enabled,shape=shape,interactionSource=cardSource) {}
                                        else -> Surface(onClick={},modifier=surfaceModifier,enabled=enabled,shape=shape,interactionSource=cardSource) {}
                                    }
                                }
                            }
                            else if(c.kind.endsWith("-chip")) {
                                if(mounted) {
                                    val label:@Composable ()->Unit = { Box(Modifier.size(32.dp,16.dp).background(Color.Blue)) }
                                    when(c.kind) {
                                        "assist-chip" -> AssistChip(onClick={},label=label,modifier=modifier,enabled=enabled,interactionSource=cardSource)
                                        "elevated-assist-chip" -> ElevatedAssistChip(onClick={},label=label,modifier=modifier,enabled=enabled,interactionSource=cardSource)
                                        "suggestion-chip" -> SuggestionChip(onClick={},label=label,modifier=modifier,enabled=enabled,interactionSource=cardSource)
                                        "elevated-suggestion-chip" -> ElevatedSuggestionChip(onClick={},label=label,modifier=modifier,enabled=enabled,interactionSource=cardSource)
                                        "input-chip" -> if(c.morph) InputChip(selected=c.checked,onClick={},label=label,shapes=InputChipDefaults.shapes(),modifier=modifier,enabled=enabled,
                                            colors=InputChipDefaults.inputChipColors(),interactionSource=cardSource)
                                            else InputChip(selected=c.checked,onClick={},label=label,modifier=modifier,enabled=enabled,interactionSource=cardSource)
                                        "elevated-filter-chip" -> if(c.morph) ElevatedFilterChip(selected=c.checked,onClick={},label=label,shapes=FilterChipDefaults.shapes(),modifier=modifier,enabled=enabled,
                                            colors=FilterChipDefaults.elevatedFilterChipColors(),interactionSource=cardSource)
                                            else ElevatedFilterChip(selected=c.checked,onClick={},label=label,modifier=modifier,enabled=enabled,interactionSource=cardSource)
                                        else -> if(c.morph) FilterChip(selected=c.checked,onClick={},label=label,shapes=FilterChipDefaults.shapes(),modifier=modifier,enabled=enabled,
                                            colors=FilterChipDefaults.filterChipColors(),interactionSource=cardSource)
                                            else FilterChip(selected=c.checked,onClick={},label=label,modifier=modifier,enabled=enabled,interactionSource=cardSource)
                                    }
                                }
                            }
                            else if(c.kind in listOf("card","elevated-card","outlined-card")) {
                                if(mounted) {
                                    val cardModifier=modifier.size(width.dp,50.dp)
                                    when(c.kind) {
                                        "elevated-card" -> if(clickable) ElevatedCard(onClick={},modifier=cardModifier,enabled=enabled,interactionSource=cardSource) {}
                                            else ElevatedCard(modifier=cardModifier) {}
                                        "outlined-card" -> if(clickable) OutlinedCard(onClick={},modifier=cardModifier,enabled=enabled,interactionSource=cardSource) {}
                                            else OutlinedCard(modifier=cardModifier) {}
                                        else -> if(clickable) Card(onClick={},modifier=cardModifier,enabled=enabled,interactionSource=cardSource) {}
                                            else Card(modifier=cardModifier) {}
                                    }
                                }
                            }
                            else if(c.kind=="switch") Switch(checked=c.checked,onCheckedChange={},modifier=modifier,enabled=enabled)
                            else if(c.kind=="checkbox") Checkbox(checked=false,onCheckedChange={},modifier=modifier,enabled=enabled)
                            else if(c.kind=="radio") RadioButton(selected=false,onClick={},modifier=modifier,enabled=enabled)
                            else if(c.kind=="list-item") ListItem(onClick={},modifier=modifier.width(180.dp),enabled=enabled) {
                                Box(Modifier.size(32.dp,16.dp).background(Color.Blue))
                            }
                            else if(c.kind=="segmented-list-item") SegmentedListItem(onClick={},modifier=modifier.width(180.dp),enabled=enabled,
                                shapes=ListItemDefaults.segmentedShapes(0,3)) {
                                Box(Modifier.size(32.dp,16.dp).background(Color.Blue))
                            }
                            else {
                                val shape=when(c.shape) {
                                    "asymmetric" -> AbsoluteRoundedCornerShape(24.dp,8.dp,0.dp,16.dp)
                                    "square" -> AbsoluteRoundedCornerShape(0.dp)
                                    "small-radius" -> AbsoluteRoundedCornerShape(0.5.dp)
                                    else -> AbsoluteRoundedCornerShape(16.dp)
                                }
                                OutlinedButton(onClick={},modifier=modifier.size(width.dp,50.dp),enabled=enabled,
                                    shape=shape,border=BorderStroke(1.1.dp,Color.Red),contentPadding=PaddingValues(0.dp),
                                    interactionSource=if(c.action=="source-old-end") source else null) {}
                            }
                        }
                    }
                }
            }
            host.frame(-1000);host.frame(0)
            var next=16000L
            fun advance(time:Long) { while(next<=time) { host.frame(next);next+=16000 }; host.advanceTo(time) }
            fun sample(id:String) {
                if(!mounted) { writer.submit(host.snapshot(id,id,emptyList()));return }
                val fields=probe.geometry().filter { it.name=="box" || it.name=="size" }.toMutableList()
                if(c.kind.endsWith("-chip") && c.action=="press-to-ring") {
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
                    fields+=number("shadowElevation",elevation/ComposeHost.RenderScale,"dip",category="motion",definition="chip.shadow-elevation",source="Surface graphics layer shadowElevation / density")
                }
                if(c.action=="theme") fields+=number("outerThemeColor",(currentSecondary.toArgb().toLong() and 0xffffffffL).toDouble(),"argb32",
                    definition="focus-ring.current-theme.secondary",source="MaterialTheme.colorScheme.secondary read during actual composition")
                writer.submit(host.snapshot(id,id,listOf(ProbePoint("control","control",null,fields))))
            }
            fun times(vararg times:Long) { times.forEach { advance(it);host.draw();sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) {
                advance(time);change();writer.record(time,"mutation",id);host.draw()
                if(id in listOf("resize","disable","opacity","inset","theme","replace","passive","clickable","detach","remount")) host.frame(time)
                sample(id)
            }
            sample("initial")
            if(c.action in listOf("press-to-ring","press-to-opacity")) {
                mutate(1000,"hover") { host.input("move",Offset(120f,80f)) };times(16000,80000,160000)
                mutate(161000,"press") { host.input("down",Offset(120f,80f)) };times(176000,208000)
                val toRing=c.action=="press-to-ring"
                mutate(209000,if(toRing) "inset" else "opacity") { inset=toRing };times(224000,256000,400000)
                mutate(401000,"cancel") { host.input("move",Offset(8f,8f));host.input("up",Offset(8f,8f)) }
                times(416000,480000,800000)
                return
            }
            if(c.action=="source-old-end") {
                val first=FocusInteraction.Focus()
                mutate(1000,"source-focus") { source.tryEmit(first) };times(16000,80000,800000)
                mutate(801000,"opacity") { inset=false }
                mutate(817000,"inset") { inset=true }
                mutate(833000,"old-unfocus") { source.tryEmit(FocusInteraction.Unfocus(first)) }
                mutate(849000,"new-focus") { source.tryEmit(FocusInteraction.Focus()) };times(864000,960000,1200000)
                mutate(1201000,"second-new-focus") { source.tryEmit(FocusInteraction.Focus()) };times(1216000,1328000,1600000)
                return
            }
            mutate(1000,"focus") { requester.requestFocus() }
            times(16000,32000,48000)
            if(c.action=="reverse") {
                mutate(49000,"blur") { focusManager!!.clearFocus(force=true) };times(64000)
                mutate(65000,"refocus") { requester.requestFocus() }
            }
            times(80000,112000,160000,240000,400000,800000)
            when(c.action) {
                "hover-blur" -> {
                    mutate(801000,"hover") { host.input("move",Offset(120f,80f)) };times(816000,960000)
                    mutate(961000,"blur-under-hover") { focusManager!!.clearFocus(force=true) };times(976000,1120000)
                    mutate(1121000,"hover-exit") { host.input("move",Offset(8f,8f)) };times(1136000,1500000)
                    return
                }
                "resize" -> mutate(801000,"resize") { width=140f }
                "disable" -> mutate(801000,"disable") { enabled=false }
                "mode" -> mutate(801000,"opacity") { inset=false }
                "focus-mode" -> {
                    mutate(801000,"opacity") { inset=false };times(816000,960000)
                    mutate(961000,"inset") { inset=true };times(976000,1200000)
                    mutate(1201000,"final-blur") { focusManager!!.clearFocus(force=true) }
                    times(1216000,1248000,1328000,1600000)
                    return
                }
                "source-replace" -> {
                    val replacement=MutableInteractionSource()
                    mutate(801000,"replace") { cardSource=replacement };times(816000,960000)
                    val focus=FocusInteraction.Focus()
                    mutate(961000,"source-focus") { replacement.tryEmit(focus) };times(976000,1200000)
                    mutate(1201000,"source-unfocus") { replacement.tryEmit(FocusInteraction.Unfocus(focus)) }
                    times(1216000,1248000,1328000,1600000);return
                }
                "passive" -> {
                    mutate(801000,"passive") { clickable=false };times(816000,960000)
                    mutate(961000,"clickable") { clickable=true };times(976000)
                    mutate(977000,"refocus") { requester.requestFocus() };times(992000,1200000,1600000);return
                }
                "detach" -> {
                    mutate(801000,"detach") { mounted=false };times(816000,960000)
                    mutate(961000,"remount") { mounted=true };times(976000)
                    mutate(977000,"refocus") { requester.requestFocus() };times(992000,1200000,1600000);return
                }
                "theme" -> mutate(801000,"theme") { dark=true }
            }
            times(816000,960000,1200000)
            mutate(1201000,"final-blur") { focusManager!!.clearFocus(force=true) }
            times(1216000,1248000,1328000,1600000)
        }
    }
}

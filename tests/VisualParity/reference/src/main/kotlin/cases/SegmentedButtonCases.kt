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
import androidx.compose.ui.input.key.*
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.SemanticsConfiguration
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class SegmentedButtonConfig(val multi:Boolean=false,val selected:Boolean=false,val enabled:Boolean=true,
    val icon:String="check",val dark:Boolean=false,val rtl:Boolean=false,val count:Int=3,val varied:Boolean=false,
    val width:Double?=null,val height:Double?=null,val labelHeight:Double=16.0,val iconWidth:Double=18.0,
    val padding:Double?=null,val border:Double=1.0,val radius:Double?=null,val customColors:Boolean=false,val action:String="none")

@ParitySuite("segmented-button")
class SegmentedButtonCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val c=writer.readConfig<SegmentedButtonConfig>()
        ComposeHost(writer,520,208).use { host ->
            val s=Scene(c); host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",s.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val c=writer.readConfig<SegmentedButtonConfig>()
        ComposeHost(writer,520,208).use { host ->
            val s=Scene(c); host.mount { s.Content() }; host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,s.measure(),image=id!="time-32000"))
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time); change(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id) }
            fun input(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.draw(); host.frame(time) }
            val coord=checkNotNull(s.items[0].coordinates)
            val center=coord.localToRoot(Offset(coord.size.width/2f,coord.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(515f,203f); sample("initial")
            when(c.action) {
                "selection","reverse" -> {
                    mutate(1000,"selected") { s.selected[0]=true }; times(16000,32000,64000)
                    if(c.action=="reverse") mutate(65000,"reverse") { s.selected[0]=false }
                    times(80000,160000,400000,800000)
                }
                "theme" -> { mutate(1000,"dark") { s.dark=true }; times(16000,400000) }
                "content" -> {
                    mutate(1000,"grow") { s.grown=true }; times(16000,64000)
                    mutate(65000,"shrink") { s.grown=false }; times(80000,400000)
                }
                "shape" -> { mutate(1000,"shape") { s.radius=8.0 }; times(16000,400000) }
                "icon" -> {
                    mutate(1000,"icon") { s.extraIcon=true }; times(16000,400000)
                    mutate(401000,"remove-icon") { s.extraIcon=false }; times(416000,800000)
                }
                "space","enter" -> {
                    advance(1000); s.focus.requestFocus(); host.scheduler.runCurrent(); host.draw(); sample("focus"); times(16000,80000)
                    val key=if(c.action=="space") Key.Spacebar else Key.Enter
                    advance(81000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown)); host.scheduler.runCurrent(); host.draw(); sample("key-down"); times(96000,160000)
                    advance(161000); host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp)); host.scheduler.runCurrent(); host.draw(); sample("key-up"); times(176000,400000)
                }
                "hover" -> { input(1000,"move",center); sample("hover"); times(16000,64000,160000); input(161000,"move",outside); sample("exit"); times(176000,400000) }
                else -> {
                    input(1000,"move",center); input(2000,"down",center); sample("press")
                    if(c.action in listOf("quick","click","disabled-input")) { input(5000,"up",center); sample("release"); times(16000,80000,240000,400000,800000) }
                    else {
                        times(16000,32000,64000)
                        if(c.action=="cancel") input(65000,"move",outside)
                        if(c.action=="disable") mutate(65000,"disabled") { s.enabled[0]=false }
                        times(80000,160000,240000); input(241000,"up",if(c.action=="cancel") outside else center); sample("release"); times(256000,400000,800000)
                    }
                }
            }
        }
    }
    private class Scene(val c:SegmentedButtonConfig) {
        val selected=mutableStateListOf<Boolean>().apply { repeat(c.count) { add(it==0 && c.selected) } }
        val enabled=mutableStateListOf<Boolean>().apply { repeat(c.count) { add(c.enabled) } }; var dark by mutableStateOf(c.dark)
        var grown by mutableStateOf(false); var radius by mutableStateOf(c.radius); var extraIcon by mutableStateOf(false)
        val clicks=IntArray(c.count); val row=Probe(); val items=List(c.count) { Probe() }; val labels=List(c.count) { Probe() }; val focus=FocusRequester()
        fun clicked(index:Int) {
            clicks[index]++
            if(c.action=="click") { if(c.multi) selected[index]=!selected[index] else { selected.indices.forEach { selected[it]=it==index } } }
        }
        @Composable fun Icon(index:Int) {
            val active:@Composable ()->Unit={ if(c.icon=="check") SegmentedButtonDefaults.ActiveIcon() else Box(Modifier.size(c.iconWidth.toFloat().dp,18.dp).background(LocalContentColor.current)) }
            val inactive:(@Composable ()->Unit)?=if(c.icon=="swap" || (index==0 && extraIcon))
                { { Box(Modifier.size((if(extraIcon) 24.0 else c.iconWidth).toFloat().dp,18.dp).background(LocalContentColor.current)) } } else null
            if(c.icon!="none") SegmentedButtonDefaults.Icon(selected[index],activeContent=active,inactiveContent=inactive)
        }
        @Composable fun Label(index:Int) {
            val width=if(index==0 && grown) 80 else 32+(if(c.varied) index*8 else 0)
            val height=if(index==0 && grown) 48.0 else c.labelHeight
            Box(labels[index].modifier().size(width.dp,height.toFloat().dp).background(LocalContentColor.current))
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=row.modifier()
                        if(c.width!=null) modifier=modifier.width(c.width.toFloat().dp)
                        if(c.height!=null) modifier=modifier.height(c.height.toFloat().dp)
                        val colors=if(c.customColors) SegmentedButtonDefaults.colors(
                            activeContainerColor=Color(0xFF126E60),activeContentColor=Color(0xFFF0E080),activeBorderColor=Color.Red,
                            inactiveContainerColor=Color(0xFF223366),inactiveContentColor=Color.Yellow,inactiveBorderColor=Color.Blue,
                            disabledActiveContainerColor=Color(0x70336699),disabledActiveContentColor=Color.Green,disabledActiveBorderColor=Color.Magenta,
                            disabledInactiveContainerColor=Color(0x50556677),disabledInactiveContentColor=Color.Cyan,disabledInactiveBorderColor=Color.Black)
                            else if(c.action=="press-content") SegmentedButtonDefaults.colors(activeContentColor=Color.Green,inactiveContentColor=Color.Yellow)
                            else SegmentedButtonDefaults.colors()
                        fun itemModifier(index:Int):Modifier=items[index].modifier().let { if(index==0) it.focusRequester(focus) else it }
                        val padding=c.padding?.let { PaddingValues(it.toFloat().dp) } ?: SegmentedButtonDefaults.ContentPadding
                        val shape=radius?.let { RoundedCornerShape(it.toFloat().dp) } ?: SegmentedButtonDefaults.baseShape
                        if(c.multi) MultiChoiceSegmentedButtonRow(modifier,c.border.toFloat().dp) {
                            repeat(c.count) { i -> SegmentedButton(checked=selected[i],onCheckedChange={ clicked(i) },shape=SegmentedButtonDefaults.itemShape(i,c.count,shape),
                                modifier=itemModifier(i),enabled=enabled[i],colors=colors,contentPadding=padding,
                                border=SegmentedButtonDefaults.borderStroke(if(!enabled[i]) { if(selected[i]) colors.disabledActiveBorderColor else colors.disabledInactiveBorderColor } else { if(selected[i]) colors.activeBorderColor else colors.inactiveBorderColor },c.border.toFloat().dp),
                                icon={ Icon(i) },label={ Label(i) }) }
                        } else SingleChoiceSegmentedButtonRow(modifier,c.border.toFloat().dp) {
                            repeat(c.count) { i -> SegmentedButton(selected=selected[i],onClick={ clicked(i) },shape=SegmentedButtonDefaults.itemShape(i,c.count,shape),
                                modifier=itemModifier(i),enabled=enabled[i],colors=colors,contentPadding=padding,
                                border=SegmentedButtonDefaults.borderStroke(if(!enabled[i]) { if(selected[i]) colors.disabledActiveBorderColor else colors.disabledInactiveBorderColor } else { if(selected[i]) colors.activeBorderColor else colors.inactiveBorderColor },c.border.toFloat().dp),
                                icon={ Icon(i) },label={ Label(i) }) }
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            fun bounds(probe:Probe,coordinate:LayoutCoordinates=checkNotNull(probe.coordinates))=probe.geometry(coordinate).filter { it.name=="box" || it.name=="size" }
            val result=mutableListOf(ProbePoint("row","row",null,bounds(row)))
            items.forEachIndexed { i,probe ->
                val node=checkNotNull(field(checkNotNull(probe.coordinates),"layoutNode"))
                val semantics=checkNotNull(field(node,"_semanticsConfiguration")) as SemanticsConfiguration
                val active=if(c.multi) semantics[SemanticsProperties.ToggleableState]==ToggleableState.On else semantics[SemanticsProperties.Selected]
                result+=ProbePoint("item-$i","button","row",bounds(probe)+listOf(flag("selected",active,definition="segmented-button.selected"),
                    flag("enabled",!semantics.contains(SemanticsProperties.Disabled),definition="segmented-button.enabled"),number("clicks",clicks[i].toDouble(),"count",definition="segmented-button.clicks")))
                var coordinator:Any?=probe.coordinates; var clipped:LayoutCoordinates?=null
                while(coordinator!=null) {
                    field(coordinator,"layer")?.let { layer ->
                        val graphics=checkNotNull(field(layer,"graphicsLayer"))
                        if(graphics.javaClass.methods.single { it.name=="getClip" && it.parameterCount==0 }.invoke(graphics)==true) clipped=coordinator as LayoutCoordinates
                    }
                    coordinator=field(coordinator,"wrapped")
                }
                result+=ProbePoint("container-$i","container","item-$i",bounds(probe,checkNotNull(clipped)))
                result+=ProbePoint("label-$i","content","container-$i",bounds(labels[i]))
            }
            return result
        }
    }
}

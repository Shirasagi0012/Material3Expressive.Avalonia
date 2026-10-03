@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
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
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class CardConfig(val kind:String="filled",val clickable:Boolean=false,val enabled:Boolean=true,val dark:Boolean=false,
    val rtl:Boolean=false,val width:Double?=160.0,val height:Double?=96.0,val contentWidth:Double=64.0,val contentHeight:Double=24.0,
    val twoChildren:Boolean=true,val custom:String="none",val action:String="none",val childPadding:Double=16.0)

@ParitySuite("card")
class CardCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        val config=writer.readConfig<CardConfig>()
        ComposeHost(writer,320,200).use { host ->
            val scene=CardScene(config)
            host.mount { scene.Content() }; host.draw(); host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<CardConfig>()
        ComposeHost(writer,320,200).use { host ->
            val scene=CardScene(config)
            host.mount { scene.Content() }; host.draw(); host.frame(-1000); host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=id!="time-32000"))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame);nextFrame+=16000 };host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it);host.draw();sample("time-$it") } }
            fun mutate(time:Long,id:String,change:()->Unit) { advance(time);change();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id) }
            fun event(time:Long,action:String,point:Offset) { advance(time);host.input(action,point);host.draw();host.frame(time) }
            val coordinates=checkNotNull(scene.probe.coordinates)
            val center=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(8f,8f);sample("initial")
            when(config.action) {
                "nested-pointer" -> {
                    val child=checkNotNull(scene.childProbe.coordinates)
                    val childCenter=child.localToRoot(Offset(child.size.width/2f,child.size.height/2f))/ComposeHost.RenderScale.toFloat()
                    event(1000,"move",childCenter);event(2000,"down",childCenter);sample("child-press");times(16000,64000)
                    event(65000,"up",childCenter);sample("child-release");times(80000,240000,400000)
                }
                "nested-space" -> {
                    advance(1000);scene.childRequester.requestFocus();host.draw();host.frame(1000);sample("child-focus");times(16000,160000)
                    advance(161000);host.scene.sendKeyEvent(KeyEvent(Key.Spacebar,KeyEventType.KeyDown));host.scheduler.runCurrent();host.draw();host.frame(161000);sample("child-key-down");times(176000,240000)
                    advance(241000);host.scene.sendKeyEvent(KeyEvent(Key.Spacebar,KeyEventType.KeyUp));host.scheduler.runCurrent();host.draw();host.frame(241000);sample("child-key-up");times(256000,400000)
                }
                "theme" -> { mutate(1000,"dark") {scene.dark=true};times(16000,80000,400000) }
                "colors" -> { mutate(1000,"custom") {scene.customColors=true};times(16000,64000);mutate(65000,"restore") {scene.customColors=false};times(80000,240000) }
                "elevation" -> {
                    event(1000,"move",center);times(16000,64000)
                    mutate(65000,"configuration") {scene.changedElevation=true};times(80000,160000,240000)
                }
                "mode" -> {
                    event(500,"move",center);event(1000,"down",center);times(16000,32000)
                    mutate(33000,"passive") {scene.clickable=false};event(34000,"up",center);sample("released-passive")
                    times(48000,160000);mutate(161000,"clickable") {scene.clickable=true}
                    event(161500,"move",center);event(162000,"down",center);event(163000,"up",center);sample("clicked");times(176000,400000)
                }
                "space","enter" -> {
                    advance(1000);scene.requester.requestFocus();host.draw();host.frame(1000);sample("focus");times(16000,64000,160000)
                    val key=if(config.action=="space") Key.Spacebar else Key.Enter
                    advance(161000);host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown));host.scheduler.runCurrent();host.draw();host.frame(161000);sample("key-down");times(176000,240000)
                    advance(241000);host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp));host.scheduler.runCurrent();host.draw();host.frame(241000);sample("key-up");times(256000,400000)
                }
                "hover","reverse-hover","focus-hover" -> {
                    if(config.action=="focus-hover") {advance(1000);scene.requester.requestFocus();host.draw();host.frame(1000);sample("focus");times(16000,160000)}
                    val start=if(config.action=="focus-hover") 161000L else 1000L
                    event(start,"move",center);sample("hover");times(start+15000,start+63000)
                    event(start+(if(config.action=="reverse-hover") 65000 else 161000),"move",outside);sample("exit")
                    times(start+175000,start+239000,start+399000)
                }
                else -> {
                    event(1000,"move",center);event(2000,"down",center);sample("press")
                    if(config.action in listOf("quick","disabled-input","passive-input")) {
                        event(5000,"up",center);sample("release");times(16000,80000,240000,400000)
                    } else {
                        times(16000,32000,64000)
                        if(config.action=="cancel") event(65000,"move",outside)
                        if(config.action=="disable-press") mutate(65000,"disabled") {scene.enabled=false}
                        times(80000,160000);event(161000,"up",if(config.action=="cancel") outside else center);sample("release");times(176000,240000,400000)
                        if(config.action=="disable-press") {mutate(401000,"enabled") {scene.enabled=true};times(416000,560000)}
                    }
                }
            }
        }
    }
    private class CardScene(val config:CardConfig) {
        var enabled by mutableStateOf(config.enabled)
        var clickable by mutableStateOf(config.clickable)
        var dark by mutableStateOf(config.dark)
        var customColors by mutableStateOf(config.custom=="colors")
        var changedElevation by mutableStateOf(false)
        var clicks=0
        var childClicks=0
        val probe=Probe()
        val childProbe=Probe()
        val childRequester=FocusRequester()
        val contentProbes=if(config.contentWidth>0 || config.contentHeight>0) List(if(config.twoChildren) 2 else 1) {Probe()} else emptyList()
        val requester=FocusRequester()
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var modifier=probe.modifier()
                        if(clickable) modifier=modifier.focusRequester(requester)
                        config.width?.let {modifier=modifier.width(it.toFloat().dp)}
                        config.height?.let {modifier=modifier.height(it.toFloat().dp)}
                        val defaults=when(config.kind) {"elevated"->CardDefaults.elevatedCardColors();"outlined"->CardDefaults.outlinedCardColors();else->CardDefaults.cardColors()}
                        val colors=when {
                            customColors->CardColors(Color(0xB0336699),Color(0xFF126E60),Color(0x50336699),Color(0x70556677))
                            config.custom in listOf("role","ambient","surface")-> {
                                val container=when(config.custom) {"role"->MaterialTheme.colorScheme.primary;"surface"->MaterialTheme.colorScheme.surface;else->Color(0xFF126E60)}
                                when(config.kind) {"elevated"->CardDefaults.elevatedCardColors(containerColor=container);"outlined"->CardDefaults.outlinedCardColors(containerColor=container);else->CardDefaults.cardColors(containerColor=container)}
                            }
                            else->defaults
                        }
                        val elevation=if(changedElevation) CardDefaults.cardElevation(1.dp,2.dp,3.dp,7.dp,9.dp,0.dp)
                            else if(config.custom in listOf("elevation","surface")) CardDefaults.cardElevation(2.dp,3.dp,4.dp,5.dp,9.dp,1.dp)
                            else when(config.kind) {"elevated"->CardDefaults.elevatedCardElevation();"outlined"->CardDefaults.outlinedCardElevation();else->CardDefaults.cardElevation()}
                        val shape=when(config.custom) {"square"->RoundedCornerShape(0.dp);"asymmetric"->RoundedCornerShape(topStart=(if(config.rtl) 9 else 3).dp,topEnd=(if(config.rtl) 3 else 9).dp,bottomEnd=(if(config.rtl) 5 else 17).dp,bottomStart=(if(config.rtl) 17 else 5).dp);else->when(config.kind) {"elevated"->CardDefaults.elevatedShape;"outlined"->CardDefaults.outlinedShape;else->CardDefaults.shape}}
                        val border=if(config.custom=="border") BorderStroke(2.25.dp,Color.Red) else null
                        val content:@Composable ColumnScope.()->Unit={
                            if(config.action.startsWith("nested-"))
                                Button({childClicks++},childProbe.modifier().focusRequester(childRequester).size(80.dp,48.dp),
                                    contentPadding=PaddingValues(horizontal=config.childPadding.toFloat().dp,vertical=8.dp)) {Markers()}
                            else Markers()
                        }
                        if(clickable) {
                            when(config.kind) {
                                "elevated"->ElevatedCard({clicks++},modifier,enabled,shape,colors,elevation,content=content)
                                "outlined"->OutlinedCard({clicks++},modifier,enabled,shape,colors,elevation,border=border ?: CardDefaults.outlinedCardBorder(enabled),content=content)
                                else->Card({clicks++},modifier,enabled,shape,colors,elevation,border=border,content=content)
                            }
                        } else {
                            when(config.kind) {
                                "elevated"->ElevatedCard(modifier,shape,colors,elevation,content=content)
                                "outlined"->OutlinedCard(modifier,shape,colors,elevation,border=border ?: CardDefaults.outlinedCardBorder(),content=content)
                                else->Card(modifier,shape,colors,elevation,border=border,content=content)
                            }
                        }
                    }
                }
            }
        }
        @Composable private fun Markers() {
            contentProbes.forEachIndexed {index,p ->
                val color=LocalContentColor.current
                Canvas(p.modifier().size((config.contentWidth/(if(index==0) 1 else 2)).toFloat().dp,(config.contentHeight/(if(index==0) 1 else 2)).toFloat().dp)) {drawRect(color)}
            }
        }
        fun measure():List<ProbePoint> {
            var coordinator:Any?=probe.coordinates
            var elevation=0.0
            while(coordinator!=null) {
                val layer=field(coordinator,"layer")
                if(layer!=null) {
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    elevation=maxOf(elevation,(graphics.javaClass.methods.single {it.name=="getShadowElevation" && it.parameterCount==0}.invoke(graphics) as Number).toDouble())
                }
                coordinator=field(coordinator,"wrapped")
            }
            return listOf(ProbePoint("card","card",null,probe.geometry().filter {it.name=="box" || it.name=="size"}+listOf(
                number("shadowElevation",elevation/ComposeHost.RenderScale,"dip",category="motion",definition="card.shadow-elevation",source="Surface graphics layer shadowElevation / density"),
                number("clicks",clicks.toDouble(),"count",definition="card.click-callback-count"))))+
                (if(config.action.startsWith("nested-")) listOf(ProbePoint("child","button","card",
                    childProbe.geometry().filter {it.name=="box" || it.name=="size"}+
                        number("clicks",childClicks.toDouble(),"count",definition="card.child-click-callback-count"))) else emptyList())+
                contentProbes.mapIndexed {index,p->ProbePoint("content-$index","content",if(config.action.startsWith("nested-")) "child" else "card",p.geometry().filter {it.name=="box" || it.name=="size"})}
        }
    }
}

@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Outline
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.focus.*
import androidx.compose.ui.input.key.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class ListItemConfig(val segmented:Boolean=false,val mode:String="none",val lines:Int=1,
    val selected:Boolean=false,val enabled:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,
    val index:Int=0,val count:Int=1,val width:Double=280.0,val height:Double?=null,
    val leading:Double=24.0,val sides:Boolean=true,val unbounded:Boolean=false,val alignment:String="auto",
    val customPadding:Boolean=false,val action:String="none",val path:String="none")

@ParitySuite("list-item")
class ListItemCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,320,200).use { host ->
            val scene=Scene(writer.readConfig<ListItemConfig>())
            host.mount {scene.Content()};host.draw();host.frame(-1000);host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<ListItemConfig>()
        ComposeHost(writer,320,200).use {host->
            val scene=Scene(config)
            host.mount {scene.Content()};host.draw();host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure(),image=id!="time-32000"))
            var next=16000L
            fun advance(time:Long) {while(next<=time) {host.frame(next);next+=16000};host.advanceTo(time)}
            fun times(vararg times:Long) {times.forEach {advance(it);host.draw();sample("time-$it")}}
            fun change(time:Long,id:String,action:()->Unit) {advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            fun input(time:Long,action:String,p:Offset) {advance(time);host.input(action,p);host.draw();host.frame(time)}
            val coordinates=checkNotNull(scene.probe.coordinates)
            val center=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
            sample("initial")
            when(config.action) {
                "path-change"->{
                    input(1000,"move",center);input(2000,"down",center);sample("pressed");times(16000,64000)
                    change(65000,"generic"){scene.path="generic"};times(80000,160000)
                    input(161000,"up",center);sample("released");times(176000)
                    change(177000,"cut"){scene.path="cut"};times(192000,400000)
                    change(401000,"restore-corners"){scene.path="none"};times(416000,800000)
                }
                "path-resize"->{
                    val hold=PressInteraction.Press(Offset(30f,30f)*ComposeHost.RenderScale.toFloat())
                    change(1000,"press"){scene.source.tryEmit(hold)};times(16000,32000)
                    change(33000,"grow"){scene.height=100.0};times(48000,64000)
                    change(65000,"release"){scene.source.tryEmit(PressInteraction.Release(hold))};times(80000,160000,400000)
                }
                "key-long"->{
                    fun key(key:Key,type:KeyEventType) {host.scene.sendKeyEvent(KeyEvent(key,type));host.scheduler.runCurrent()}
                    change(1000,"focus"){scene.requester.requestFocus()};times(16000,64000,160000)
                    change(161000,"enter-down"){key(Key.Enter,KeyEventType.KeyDown)}
                    change(162000,"space-down"){key(Key.Spacebar,KeyEventType.KeyDown)}
                    change(163000,"other-up"){key(Key.A,KeyEventType.KeyUp)}
                    times(176000,661000,662000)
                    change(663000,"enter-up"){key(Key.Enter,KeyEventType.KeyUp)}
                    change(664000,"space-up"){key(Key.Spacebar,KeyEventType.KeyUp)}
                    times(672000,800000)
                }
                "space","enter"->{
                    change(1000,"focus"){scene.requester.requestFocus()};times(16000,64000,160000)
                    val key=if(config.action=="space") Key.Spacebar else Key.Enter
                    change(161000,"key-down"){host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown));host.scheduler.runCurrent()};times(176000,240000)
                    change(241000,"key-up"){host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp));host.scheduler.runCurrent()};times(256000,400000,800000)
                }
                "nested"->{
                    val child=checkNotNull(scene.childProbe.coordinates)
                    val p=child.localToRoot(Offset(child.size.width/2f,child.size.height/2f))/ComposeHost.RenderScale.toFloat()
                    input(1000,"move",p);input(2000,"down",p);sample("child-pressed");times(16000,64000)
                    input(65000,"up",p);sample("child-released");times(80000,240000,400000)
                }
                "long"->{input(1000,"move",center);input(2000,"down",center);sample("pressed");times(16000,32000,501000,502000)
                    input(503000,"up",center);sample("released");times(512000,800000)}
                "shapes"->{input(1000,"move",center);times(16000,64000)
                    change(65000,"configuration"){scene.changedShapes=true};times(80000,160000)
                    input(161000,"down",center);times(176000,240000);input(241000,"up",center);sample("released");times(256000,800000)}
                "theme"->{change(1000,"dark"){scene.dark=true};times(16000,32000,80000,400000)}
                "select"->{change(1000,"selected"){scene.selected=true};times(16000,32000,80000);change(81000,"deselected"){scene.selected=false};times(96000,240000,400000)}
                "group"->{change(1000,"first"){scene.count=3};times(16000,32000);change(33000,"last"){scene.index=2};times(48000,160000)}
                "drag"->{
                    val first=DragInteraction.Start();val second=DragInteraction.Start()
                    change(1000,"drag"){scene.source.tryEmit(first)};times(16000,32000,80000)
                    change(81000,"second"){scene.source.tryEmit(second)};change(82000,"stop-first"){scene.source.tryEmit(DragInteraction.Stop(first))};times(96000,160000)
                    change(161000,"cancel-second"){scene.source.tryEmit(DragInteraction.Cancel(second))};times(176000,400000,800000)
                }
                else->{
                    input(1000,"move",center);input(2000,"down",center);sample("pressed")
                    if(config.action in listOf("quick","disabled-input","passive-input","single-click","multiple-click")) {input(5000,"up",center);sample("released");times(16000,32000,80000,400000)}
                    else {
                        times(16000,32000,64000)
                        if(config.action=="cancel") input(65000,"move",Offset(4f,4f))
                        if(config.action=="disable") change(65000,"disabled"){scene.enabled=false}
                        times(80000,160000);input(161000,"up",if(config.action=="cancel") Offset(4f,4f) else center);sample("released");times(176000,400000,800000)
                    }
                }
            }
        }
    }
    private class Scene(val config:ListItemConfig) {
        val probe=Probe()
        val slots=linkedMapOf<String,Probe>()
        val source=MutableInteractionSource()
        val requester=FocusRequester()
        val childProbe=Probe()
        var changedShapes by mutableStateOf(false)
        var path by mutableStateOf(config.path)
        var height by mutableStateOf(config.height)
        var selected by mutableStateOf(config.selected)
        var enabled by mutableStateOf(config.enabled)
        var dark by mutableStateOf(config.dark)
        var index by mutableStateOf(config.index)
        var count by mutableStateOf(config.count)
        var clicks=0
        var requests=0
        var longClicks=0
        var childClicks=0
        @Composable fun Marker(name:String,w:Double,h:Double) {
            val p=slots.getOrPut(name){Probe()};val color=LocalContentColor.current
            Canvas(p.modifier().size(w.toFloat().dp,h.toFloat().dp)){drawRect(color)}
        }
        @Composable fun Content() {
            // Product MotionTokenSource supplies Expressive springs. MaterialTheme alone defaults
            // to Standard; use the same public theme input, not per-animation compensation.
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                    LocalRippleThemeConfiguration provides if(config.path!="none" && config.action=="space") RippleDefaults.InsetFocusRingThemeConfiguration else RippleDefaults.OpacityFocusThemeConfiguration,
                    LocalRippleConfiguration provides RippleConfiguration(focus=RippleConfiguration.Focus.InsetRing(Color.Red,Color.Blue))) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        Box(Modifier.width(config.width.toFloat().dp)) {
                            var modifier:Modifier=Modifier
                            if(config.unbounded) modifier=modifier.wrapContentWidth(Alignment.Start,unbounded=true)
                            modifier=modifier.then(probe.modifier())
                            if(config.mode!="none") modifier=modifier.focusRequester(requester)
                            height?.let {modifier=modifier.height(it.toFloat().dp)}
                            val leading:(@Composable ()->Unit)?=if(config.sides) {{Marker("leading",config.leading,config.leading)}} else null
                            val trailing:(@Composable ()->Unit)?=if(config.sides) {{
                                if(config.action=="nested") Button({childClicks++},childProbe.modifier().size(64.dp,32.dp),contentPadding=PaddingValues(8.dp)) {Marker("trailing",16.0,16.0)}
                                else Marker("trailing",16.0,16.0)
                            }} else null
                            val supporting:(@Composable ()->Unit)?=if(config.lines>=2) {{Marker("supporting",80.0,20.0)}} else null
                            val overline:(@Composable ()->Unit)?=if(config.lines==3) {{Marker("overline",40.0,16.0)}} else null
                            val content:@Composable ()->Unit={Marker("main",64.0,24.0)}
                            val alignment=when(config.alignment) {"top"->Alignment.Top;"center"->Alignment.CenterVertically;"bottom"->Alignment.Bottom;else->ListItemDefaults.verticalAlignment()}
                            val padding=if(config.customPadding) PaddingValues(start=8.dp,top=4.dp,end=20.dp,bottom=14.dp) else ListItemDefaults.ContentPadding
                            val defaults=if(changedShapes) ListItemDefaults.shapes(shape=RoundedCornerShape(2.dp),selectedShape=RoundedCornerShape(8.dp),pressedShape=RoundedCornerShape(24.dp),focusedShape=RoundedCornerShape(8.dp),hoveredShape=RoundedCornerShape(6.dp),draggedShape=RoundedCornerShape(8.dp))
                                else if(config.segmented) ListItemDefaults.segmentedShapes(index,count) else ListItemDefaults.shapes()
                            val shapes=if(path=="none")defaults else ListItemShapes(
                                shape=if(path=="generic")NotchedShape else if(path=="rounded" || path=="round-cut") RoundedCornerShape(20.dp,4.dp,8.dp,2.dp) else CutCornerShape(20.dp,4.dp,8.dp,2.dp),
                                selectedShape=if(path=="rounded" || path=="round-cut") RoundedCornerShape(25) else if(path=="mixed")defaults.selectedShape else CutCornerShape(12.dp),
                                pressedShape=if(path=="rounded") RoundedCornerShape(10) else if(path=="mixed")defaults.pressedShape else CutCornerShape(10),
                                focusedShape=if(path=="rounded") RoundedCornerShape(8.dp) else CutCornerShape(8.dp),hoveredShape=if(path=="rounded") RoundedCornerShape(4.dp) else CutCornerShape(4.dp),
                                draggedShape=if(path=="rounded") RoundedCornerShape(25) else if(path=="generic")NotchedShape else CutCornerShape(25))
                            val longClick:(()->Unit)?=if(config.action in listOf("long","key-long")) {{longClicks++}} else null
                            if(!config.segmented) when(config.mode) {
                                "none"->ListItem(modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,content=content)
                                "click"->ListItem(onClick={clicks++},modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,interactionSource=source,onLongClick=longClick,content=content)
                                "single"->ListItem(selected=selected,onClick={clicks++},modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,interactionSource=source,onLongClick=longClick,content=content)
                                "multiple"->ListItem(checked=selected,onCheckedChange={requests++},modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,interactionSource=source,onLongClick=longClick,content=content)
                            }
                            else when(config.mode) {
                                "none"->SegmentedListItem(modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,content=content)
                                "click"->SegmentedListItem(onClick={clicks++},modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,interactionSource=source,onLongClick=longClick,content=content)
                                "single"->SegmentedListItem(selected=selected,onClick={clicks++},modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,interactionSource=source,onLongClick=longClick,content=content)
                                "multiple"->SegmentedListItem(checked=selected,onCheckedChange={requests++},modifier=modifier,enabled=enabled,leadingContent=leading,trailingContent=trailing,overlineContent=overline,supportingContent=supporting,verticalAlignment=alignment,shapes=shapes,contentPadding=padding,interactionSource=source,onLongClick=longClick,content=content)
                            }
                        }
                    }
                }
            }
        }
        fun measure():List<ProbePoint> {
            var coordinator:Any?=probe.coordinates
            var corner=0.0;var elevation=0.0;var outlineKind="rounded";var pathStartY=0.0
            while(coordinator!=null) {
                val layer=field(coordinator,"layer")
                if(layer!=null) {
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    // The clip layer has zero elevation; the separate shadow layer can
                    // carry a negative spring overshoot. Keep that signed runtime value.
                    val layerElevation=(graphics.javaClass.methods.single {it.name=="getShadowElevation" && it.parameterCount==0}.invoke(graphics) as Number).toDouble()
                    if(layerElevation!=0.0) elevation=layerElevation
                    if(graphics.javaClass.methods.single {it.name=="getClip" && it.parameterCount==0}.invoke(graphics)==true) {
                        when(val outline=field(layer,"outline")) {
                            is Outline.Rounded -> corner=outline.roundRect.topLeftCornerRadius.x.toDouble()
                            is Outline.Generic -> {outlineKind="generic";pathStartY=outline.path.iterator().next().points[1].toDouble()/ComposeHost.RenderScale}
                            is Outline.Rectangle -> outlineKind="rectangle"
                        }
                    }
                }
                coordinator=field(coordinator,"wrapped")
            }
            return listOf(ProbePoint("item","list-item",null,probe.geometry().filter {it.name=="box" || it.name=="size"}+listOf(
            number("corner",corner/ComposeHost.RenderScale,"dip",category="motion",definition="list-item.top-left-outline-radius"))+
            (if(config.path!="none")listOf(text("outlineKind",outlineKind,definition="list-item.rendered-outline-kind"),number("pathStartY",pathStartY,"dip",category="motion",definition="list-item.generic-outline-first-move-y-or-zero"))else emptyList())+listOf(
            number("shadow",elevation/ComposeHost.RenderScale,"dip",category="motion",definition="list-item.shadow-elevation"),
            number("longClicks",longClicks.toDouble(),"count",definition="list-item.long-clicks"),
            number("childClicks",childClicks.toDouble(),"count",definition="list-item.child-clicks"),
            number("clicks",clicks.toDouble(),"count",definition="list-item.clicks"),number("requests",requests.toDouble(),"count",definition="list-item.checked-requests"))))+
            slots.map {(name,p)->ProbePoint(name,"content","item",p.geometry().filter {it.name=="box" || it.name=="size"})}
        }
    }
    private object NotchedShape:Shape {
        override fun createOutline(size:Size,layoutDirection:LayoutDirection,density:Density):Outline=Outline.Generic(Path().apply {
            val inset=with(density){12.dp.toPx()};moveTo(0f,0f);lineTo(size.width,0f);lineTo(size.width-inset,size.height/2)
            lineTo(size.width,size.height);lineTo(0f,size.height);close()
        })
    }
}

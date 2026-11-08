@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.material3.*
import androidx.compose.material3.internal.Icons
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Outline
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class MenuConfig(val variant:String="standard", val selected:Boolean=false, val enabled:Boolean=true,
    val dark:Boolean=false, val rtl:Boolean=false, val icon:Boolean=true, val width:Double=200.0,
    val contentWidth:Double=48.0, val contentHeight:Double=16.0, val action:String="none", val selectedIcon:Boolean=false, val nativeCheck:Boolean=false, val autoWidth:Boolean=false, val comboItem:Boolean=false,
    val precision:Boolean=false,val trailing:Boolean=false,val path:String="none")

@ParitySuite("menu")
class MenuCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer,false)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer,true)
    @ParityScenario("native-selection") suspend fun nativeSelection(writer:CaptureWriter)=capture(writer,true)
    @ParityScenario("native-check") suspend fun nativeCheck(writer:CaptureWriter)=capture(writer,false)
    internal suspend fun capture(writer:CaptureWriter,dynamic:Boolean) {
        val config=writer.readConfig<MenuConfig>()
        @Suppress("UNCHECKED_CAST")
        val precision=Class.forName("androidx.compose.material3.PrecisionPointer").declaredMethods
            .single { it.name.startsWith("getShouldUsePrecisionPointerComponentSizing") }.invoke(null) as MutableState<Boolean>
        val previous=precision.value
        precision.value=config.precision
        try {
        ComposeHost(writer,320,160).use { host ->
            val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
            sample("initial"); if(!dynamic) return
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame);nextFrame+=16000 };host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it);host.draw();sample("time-$it",it!=32000L) } }
            fun change(time:Long,id:String,action:()->Unit) { advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id) }
            val coordinates=checkNotNull(scene.itemProbe.coordinates)
            val center=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
            val outside=Offset(8f,8f)
            fun event(time:Long,action:String,point:Offset) { advance(time);host.input(action,point);host.draw();host.frame(time) }
            when(config.action) {
                "precision" -> {
                    change(1000,"precision-on") {precision.value=true}
                    change(2000,"precision-off") {precision.value=false}
                }
                "icon-enter", "icon-exit" -> {
                    change(1000,"toggle-selection") {scene.selected=!config.selected};times(16000,32000,64000)
                    change(65000,"add-leading") {scene.hasIcon=true};times(80000,96000,160000,400000)
                    change(401000,"remove-leading") {scene.selected=false;scene.hasIcon=false};times(416000)
                    change(450000,"select-again") {scene.selected=true};times(464000,512000,800000)
                }
                "selection","reverse" -> {
                    change(1000,"selected") {scene.selected=true};times(16000,32000,64000)
                    if(config.action=="reverse") change(65000,"reversed") {scene.selected=false}
                    times(80000,160000,400000)
                }
                "theme" -> {
                    change(1000,"dark") {scene.dark=true};times(16000,64000,400000)
                    change(401000,"light") {scene.dark=false};times(416000,480000,800000)
                }
                "hover" -> {
                    event(1000,"move",center);sample("hover");times(16000,80000)
                    event(81000,"move",outside);sample("exit");times(96000,240000,400000)
                }
                else -> {
                    event(1000,"move",center);event(2000,"down",center);sample("press");times(16000,32000,64000,160000)
                    event(161000,"up",center);sample("release");times(176000,240000,400000)
                }
            }
        }
        } finally { precision.value=previous }
    }
    private object NotchedShape:Shape {
        override fun createOutline(size:Size,layoutDirection:LayoutDirection,density:Density):Outline=Outline.Generic(Path().apply {
            val inset=with(density){12.dp.toPx()};moveTo(0f,0f);lineTo(size.width,0f);lineTo(size.width-inset,size.height/2)
            lineTo(size.width,size.height);lineTo(0f,size.height);close()
        })
    }
    private class Scene(val config:MenuConfig) {
        var selected by mutableStateOf(config.selected)
        var hasIcon by mutableStateOf(config.icon)
        var dark by mutableStateOf(config.dark)
        var clicks=0
        val itemProbe=Probe()
        val contentProbe=Probe()
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark) darkColorScheme() else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        val item: @Composable () -> Unit = {
                            val content:@Composable ()->Unit={
                                Box(contentProbe.modifier().size(config.contentWidth.toFloat().dp,config.contentHeight.toFloat().dp).background(LocalContentColor.current))
                            }
                            val icon:@Composable (() -> Unit)?=if(config.nativeCheck && config.variant=="baseline" && selected) ({
                                Icon(Icons.Filled.Check,null,Modifier.size(24.dp))
                            }) else if(hasIcon) ({
                                val size=if(config.variant=="baseline") 24.dp else 20.dp
                                Box(Modifier.size(size).background(LocalContentColor.current))
                            }) else null
                            if(config.variant=="baseline") {
                                // DropdownMenuContent's outer Surface supplies contentColorFor(containerColor).
                                // The baseline ripple is outside its text/icon color providers and consumes
                                // that ambient color. Preserve the real menu context in this isolated item case.
                                CompositionLocalProvider(LocalContentColor provides contentColorFor(MenuDefaults.containerColor)) {
                                    DropdownMenuItem(text=content,onClick={clicks++},modifier=itemProbe.modifier(),leadingIcon=icon,enabled=config.enabled)
                                }
                            }
                            else SelectableDropdownMenuItem(selected=selected,onClick={clicks++},text=content,
                                trailingContent=if(config.trailing)({Box(Modifier.size(20.dp).background(LocalContentColor.current))}) else null,
                                shapes=if(config.path=="none")MenuDefaults.itemShapes() else MenuItemShapes(if(config.path=="rounded") RoundedCornerShape(20.dp,4.dp,12.dp,2.dp) else CutCornerShape(20.dp,4.dp,12.dp,2.dp),
                                    when(config.path){"rounded","round-cut"->RoundedCornerShape(35);"cut"->CutCornerShape(35);"generic"->NotchedShape;else->MenuDefaults.itemShapes().selectedShape}),
                                modifier=itemProbe.modifier(),leadingIcon=icon,enabled=config.enabled,
                                contentPadding=if(config.comboItem) ExposedDropdownMenuDefaults.ItemContentPadding else MenuDefaults.DropdownMenuSelectableItemContentPadding,
                                selectedLeadingIcon=if(config.nativeCheck) ({Icon(Icons.Filled.Check,null,Modifier.size(20.dp))}) else if(config.selectedIcon) ({Box(Modifier.size(20.dp).background(LocalContentColor.current))}) else null,
                                colors=if(config.variant=="vibrant") MenuDefaults.selectableItemVibrantColors() else MenuDefaults.selectableItemColors())
                        }
                        // Native menus intentionally have no shadow (the established native theme adaptation).
                        if(config.autoWidth) DropdownMenuGroup(shapes=MenuDefaults.groupShapes(),shadowElevation=0.dp,modifier=Modifier.width(IntrinsicSize.Max)) { item() }
                        else Box(Modifier.width(config.width.toFloat().dp)) { item() }
                    }
                }
            }
        }
        fun measure()=listOf(
            ProbePoint("content","content",null,contentProbe.geometry().filter {it.name=="box" || it.name=="size"}),
            ProbePoint("item","item",null,listOf(flag("selected",selected,definition="menu.selected"),
                number("clicks",clicks.toDouble(),"count",definition="menu.click-count"),
                number("cornerRadius",cornerRadius(),"dip",category="motion",definition="menu.item.visible-rounded-corner",source="cached Surface GraphicsLayerOwnerLayer.outline / density"))))

        private fun cornerRadius():Double {
            if(config.variant=="baseline") return 0.0
            fun field(owner:Any,name:String):Any? {
                var type:Class<*>?=owner.javaClass
                while(type!=null) {
                    try { return type.getDeclaredField(name).apply { isAccessible=true }.get(owner) }
                    catch(_:NoSuchFieldException) { type=type.superclass }
                }
                error("Missing $name on ${owner.javaClass.name}")
            }
            var coordinator:Any?=checkNotNull(itemProbe.coordinates)
            var radius:Double?=null
            while(coordinator!=null) {
                val layer=field(coordinator,"layer")
                if(layer!=null) {
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    if(graphics.javaClass.methods.single { it.name=="getClip" && it.parameterCount==0 }.invoke(graphics)==true) {
                        val outline=field(layer,"outline") as? Outline ?: error("Missing Surface outline")
                        if(outline !is Outline.Rounded)return 0.0
                        check(radius==null)
                        radius=outline.roundRect.topLeftCornerRadius.x.toDouble()/ComposeHost.RenderScale
                    }
                }
                coordinator=field(coordinator,"wrapped")
            }
            return checkNotNull(radius)
        }
    }
}


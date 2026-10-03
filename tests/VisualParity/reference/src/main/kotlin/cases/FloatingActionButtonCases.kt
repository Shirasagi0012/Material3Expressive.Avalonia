@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.WindowInfo
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class FloatingActionButtonConfig(val kind:String="icon",val size:String="default",val expanded:Boolean=true,
    val checked:Boolean=false,val dark:Boolean=false,val rtl:Boolean=false,val alignment:String="end",val count:Int=3,
    val iconWidth:Double=24.0,val iconHeight:Double=24.0,val labelWidth:Double=48.0,val labelHeight:Double=16.0,
    val width:Double?=null,val height:Double?=null,val color:String="default",val elevation:String="default",
    val visible:Boolean=true,val action:String="none",val animatedIcon:Boolean=false)

@ParitySuite("floating-action-button")
class FloatingActionButtonCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,360,400,desktopContext()).use { host ->
            val scene=FabScene(writer.readConfig<FloatingActionButtonConfig>())
            host.mount { scene.Content() }; host.frame(-1000);host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        val config=writer.readConfig<FloatingActionButtonConfig>()
        ComposeHost(writer,360,400,desktopContext()).use { host ->
            val scene=FabScene(config)
            host.mount { scene.Content() };host.frame(-1000);host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
            var next=16000L
            fun advance(time:Long) { while(next<=time) {host.frame(next);next+=16000};host.advanceTo(time) }
            fun at(time:Long) {advance(time);host.draw();sample("time-$time")}
            fun change(time:Long,id:String,action:()->Unit) {advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            val target=if(config.kind=="menu"&&config.action=="press") scene.itemProbe else scene.probe
            // State/visibility scenarios do not send pointer input; an initially hidden FAB
            // has no placed content coordinates, so it must not require a pointer target.
            val coordinates=target.coordinates
            val center=if(coordinates!=null)coordinates.localToRoot(androidx.compose.ui.geometry.Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat() else androidx.compose.ui.geometry.Offset.Zero
            fun input(time:Long,operation:String,point:androidx.compose.ui.geometry.Offset) {advance(time);host.input(operation,point);host.draw();host.frame(time);sample("$operation-$time")}
            sample("initial")
            when(config.action) {
                "expand","collapse","reverse","check","uncheck" -> {
                    change(1000,"state-change") {scene.toggle()};at(16000);at(64000)
                    if(config.action=="reverse")change(65000,"reverse") {scene.toggle()}
                    at(96000);at(160000);at(320000);at(800000)
                }
                "hide","show","visibility-reverse" -> {
                    change(1000,"visibility-change") {scene.visible=!scene.visible};at(16000);at(64000)
                    if(config.action=="visibility-reverse")change(65000,"visibility-reverse") {scene.visible=true}
                    at(160000);at(320000);at(800000)
                }
                "count" -> {
                    change(1000,"expand") {scene.toggle()};at(64000);change(65000,"append") {scene.count++};at(80000);at(96000);at(128000);at(160000);at(400000)
                }
                "theme" -> {change(1000,"dark") {scene.dark=true};at(16000);at(160000);change(161000,"light") {scene.dark=false};at(400000)}
                "elevation" -> {input(1000,"move",center);at(80000);change(81000,"elevation-replaced") {scene.replacedElevation=true};at(96000);at(240000)}
                else -> {
                    input(1000,"move",center)
                    if(config.action!="hover")input(2000,"down",center)
                    at(16000);at(if(config.action=="quick")32000 else 80000)
                    val outside=androidx.compose.ui.geometry.Offset(350f,390f)
                    if(config.action=="cancel"||config.action=="hover")input(81000,"move",outside)
                    if(config.action!="hover")input(if(config.action=="quick")33000 else 82000,"up",if(config.action=="cancel")outside else center)
                    at(160000);at(320000);at(800000)
                }
            }
            host.advanceTo(900000);sample("passive-idle",false)
        }
    }
    companion object {
        private fun desktopContext():PlatformContext {
            val empty:PlatformContext=PlatformContext.Empty()
            return object:PlatformContext by empty {
                override val windowInfo=object:WindowInfo by empty.windowInfo {
                    override val containerSize=IntSize(360*ComposeHost.RenderScale,400*ComposeHost.RenderScale)
                    override val isWindowFocused=true
                }
                override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                    override val touchSlop=18f*ComposeHost.RenderScale
                }
            }
        }
    }
    private class FabScene(val config:FloatingActionButtonConfig) {
        val probe=Probe();val itemProbe=Probe()
        val itemProbes=List(config.count+1) {Probe()}
        var expanded by mutableStateOf(config.expanded)
        var checked by mutableStateOf(config.checked)
        var visible by mutableStateOf(config.visible)
        var dark by mutableStateOf(config.dark)
        var count by mutableStateOf(config.count)
        var replacedElevation by mutableStateOf(false)
        var clicks=0
        var progressReader:()->Float={if(config.checked)1f else 0f}
        fun toggle() {if(config.kind=="toggle")checked=!checked else expanded=!expanded}
        @Composable fun glyph(width:Double,height:Double) {
            val color=LocalContentColor.current
            Canvas(Modifier.size(width.toFloat().dp,height.toFloat().dp)) {drawRect(color)}
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,LocalContentColor provides Color(0xFFEE7700)) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                        var parent:Modifier=Modifier
                        config.width?.let {parent=parent.width(it.toFloat().dp)}
                        config.height?.let {parent=parent.height(it.toFloat().dp)}
                        // Observe the component's complete layout allocation. Toggle's modifier
                        // belongs to its inner container; the wrapper includes its fixed outer Box.
                        Box(parent,contentAlignment=Alignment.Center) { Box(probe.modifier()) { ButtonContent() } }
                    }
                }
            }
        }
        @Composable fun ButtonContent() {
            var modifier:Modifier=Modifier
            if(config.kind !in listOf("toggle","menu"))modifier=modifier.animateFloatingActionButton(visible,Alignment.BottomEnd)
            val container=when(config.color) {"primary"->Color(0xFF6750A4);"surface"->Color(0xFFFEF7FF);"transparent"->Color.Transparent;"ambient"->Color(0xFF126E60);else->FloatingActionButtonDefaults.containerColor}
            val elevation=if(replacedElevation)FloatingActionButtonDefaults.elevation(2.dp,3.dp,4.dp,5.dp)else when(config.elevation) {
                "lowered"->FloatingActionButtonDefaults.loweredElevation();"flat"->FloatingActionButtonDefaults.bottomAppBarFabElevation();"custom"->FloatingActionButtonDefaults.elevation(2.dp,4.dp,6.dp,9.dp);else->FloatingActionButtonDefaults.elevation()
            }
            val icon:@Composable ()->Unit={glyph(config.iconWidth,config.iconHeight)}
            val text:@Composable ()->Unit={glyph(config.labelWidth,config.labelHeight)}
            when(config.kind) {
                "toggle" -> {
                    // The paired Size property keeps its mapping identity across state changes.
                    // Recreating radius lambdas here replaces Compose's remembered focus-ring
                    // shape and indication, which would test parameter replacement on click.
                    val size=remember(config.size) {when(config.size){"medium"->ToggleFloatingActionButtonDefaults.containerSizeMedium();"large"->ToggleFloatingActionButtonDefaults.containerSizeLarge();else->ToggleFloatingActionButtonDefaults.containerSize()}}
                    val radius=remember(config.size) {when(config.size){"medium"->ToggleFloatingActionButtonDefaults.containerCornerRadiusMedium();"large"->ToggleFloatingActionButtonDefaults.containerCornerRadiusLarge();else->ToggleFloatingActionButtonDefaults.containerCornerRadius()}}
                    ToggleFloatingActionButton(checked,{checked=it;clicks++},modifier,containerSize=size,containerCornerRadius=radius) {
                        progressReader={checkedProgress}
                        if(config.animatedIcon) {
                            val progress=checkedProgress
                            val iconSize=when(config.size){"medium"->ToggleFloatingActionButtonDefaults.iconSizeMedium();"large"->ToggleFloatingActionButtonDefaults.iconSizeLarge();else->ToggleFloatingActionButtonDefaults.iconSize()}
                            val iconModifier=with(ToggleFloatingActionButtonDefaults) {Modifier.animateIcon(checkedProgress={progress},size=iconSize)}
                            Canvas(iconModifier) {drawRect(Color.White)}
                        } else icon()
                    }
                }
                "menu" -> {
                    val align=when(config.alignment){"start"->Alignment.Start;"center"->Alignment.CenterHorizontally;else->Alignment.End}
                    FloatingActionButtonMenu(expanded,button={FloatingActionButton({clicks++}){glyph(24.0,24.0)}},modifier=modifier,horizontalAlignment=align) {
                        repeat(count) {i->FloatingActionButtonMenuItem({clicks++},text={glyph(config.labelWidth+i*8,config.labelHeight)},icon={glyph(24.0,24.0)},modifier=(if(i==0)itemProbe.modifier()else Modifier).then(itemProbes[i].modifier()))}
                    }
                }
                "extended" -> when(config.size) {
                    "small"->SmallExtendedFloatingActionButton(text,icon,{clicks++},modifier,expanded,containerColor=container,elevation=elevation)
                    "medium"->MediumExtendedFloatingActionButton(text,icon,{clicks++},modifier,expanded,containerColor=container,elevation=elevation)
                    "large"->LargeExtendedFloatingActionButton(text,icon,{clicks++},modifier,expanded,containerColor=container,elevation=elevation)
                    else->ExtendedFloatingActionButton(text,icon,{clicks++},modifier,expanded,containerColor=container,elevation=elevation)
                }
                "content" -> when(config.size) {
                    "small"->SmallExtendedFloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation){text()}
                    "medium"->MediumExtendedFloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation){text()}
                    "large"->LargeExtendedFloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation){text()}
                    else->ExtendedFloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation){text()}
                }
                else -> when(config.size) {
                    "small"->SmallFloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation,content=icon)
                    "medium"->MediumFloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation,content=icon)
                    "large"->LargeFloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation,content=icon)
                    else->FloatingActionButton({clicks++},modifier,containerColor=container,elevation=elevation,content=icon)
                }
            }
        }
        fun measure():List<ProbePoint> {
            val fields=probe.geometry().filter {it.name=="box"||it.name=="size"}.toMutableList()
            fields+=number("clicks",clicks.toDouble(),"count",definition="fab.click-count")
            if(config.kind=="toggle") {
                fields+=number("checkedProgress",progressReader().toDouble(),"ratio",category="motion",definition="fab.checked-progress")
            }
            val points=mutableListOf(ProbePoint("fab","fab",null,fields))
            if(config.action=="count") {
                // Alpha-zero items are not placed upstream. The ordinary holders only
                // observe placed slots; they do not invalidate composition or layout.
                itemProbes.take(count).forEachIndexed {i,p->
                    val coordinates=p.coordinates ?: return@forEachIndexed
                    // Fixed Core NodeCoordinator's item placement layer. Other layers
                    // in this chain have alpha=1, so the product is the drawn item alpha.
                    val alpha=generateSequence(coordinates as Any) {field(it,"wrapped")}
                        .filter {field(it,"layer")!=null}
                        .fold(1f) {a,c->a*(field(c,"lastLayerAlpha") as Float)}
                    points+=ProbePoint("item-$i","menu-item","fab",p.geometry().filter {it.name=="size"}+
                        number("alpha",alpha.toDouble(),"ratio",category="motion",definition="fab.item-layer-alpha",
                            source="NodeCoordinator.lastLayerAlpha on item placement layer"))
                }
            }
            return points
        }
    }
}

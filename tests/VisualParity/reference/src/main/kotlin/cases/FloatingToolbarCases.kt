@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.nestedscroll.NestedScrollSource
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable
data class FloatingToolbarConfig(val horizontal:Boolean=true,val fab:String="none",val expanded:Boolean=true,
    val vibrant:Boolean=false,val dark:Boolean=false,val rtl:Boolean=false,val leading:Boolean=true,val trailing:Boolean=true,
    val contentWidth:Double=48.0,val contentHeight:Double=48.0,val maxWidth:Double?=null,val maxHeight:Double?=null,
    val content:String="glyph",val custom:String="none",val action:String="none",val exit:String="bottom")

@ParitySuite("floating-toolbar")
class FloatingToolbarCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,400,440).use { host ->
            val scene=Scene(writer.readConfig<FloatingToolbarConfig>())
            host.mount {scene.Content()};host.frame(-1000);host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("dynamic")
    suspend fun dynamic(writer:CaptureWriter) {
        val config=writer.readConfig<FloatingToolbarConfig>()
        ComposeHost(writer,400,440).use {host ->
            val scene=Scene(config);host.mount {scene.Content()};host.frame(-1000);host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
            var next=16000L
            fun advance(time:Long) {while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun at(time:Long) {advance(time);host.draw();sample("time-$time")}
            fun change(time:Long,id:String,action:()->Unit) {advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id)}
            sample("initial")
            if(config.action=="theme") {
                change(1000,"dark"){scene.dark=true};at(64000)
                change(65000,"light"){scene.dark=false};at(160000)
            } else if(config.action in listOf("scroll","snap","fling")) {
                fun scroll(y:Float) {scene.behavior!!.onPostScroll(Offset(0f,y*ComposeHost.RenderScale),Offset.Zero,NestedScrollSource.UserInput)}
                change(1000,"scroll"){scroll(-40f)}
                if(config.action=="scroll")change(65000,"scroll-back"){scroll(40f)}
                else change(2000,"settle") {scene.settling=scene.scope!!.launch {scene.behavior!!.onPostFling(Velocity.Zero,Velocity(0f,(if(config.action=="fling")-400f else 0f)*ComposeHost.RenderScale))}}
                at(96000);at(160000);at(320000);at(800000)
                scene.settling?.join()
            } else {
                change(1000,"state-change"){scene.expanded=!scene.expanded};at(16000);at(64000)
                if(config.action=="reverse")change(65000,"reverse"){scene.expanded=!scene.expanded}
                at(96000);at(160000);at(320000);at(800000)
            }
            host.advanceTo(900000);sample("passive-idle",false)
        }
    }
    private class Scene(val config:FloatingToolbarConfig) {
        var expanded by mutableStateOf(config.expanded)
        var dark by mutableStateOf(config.dark)
        val toolbar=Probe();val content=Probe();val fab=Probe()
        var behavior:FloatingToolbarScrollBehavior?=null
        var scope:CoroutineScope?=null
        var settling:Job?=null
        @Composable fun glyph(width:Double,height:Double,probe:Probe?=null) {
            val color=LocalContentColor.current
            Canvas(Modifier.size(width.toFloat().dp,height.toFloat().dp).then(probe?.modifier() ?: Modifier)) {drawRect(color)}
        }
        @Composable fun Content() {
            MaterialTheme(colorScheme=if(dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {Toolbar()}
                }
            }
        }
        @Composable fun mainContent() {
            when(config.content) {
                "empty"->Box(content.modifier())
                "button"->Button({},content.modifier()) {}
                else->glyph(config.contentWidth,config.contentHeight,content)
            }
        }
        @Composable fun Toolbar() {
            var modifier:Modifier=toolbar.modifier()
            config.maxWidth?.let {modifier=modifier.widthIn(max=it.toFloat().dp)}
            config.maxHeight?.let {modifier=modifier.heightIn(max=it.toFloat().dp)}
            val colors=if(config.custom=="colors")FloatingToolbarDefaults.standardFloatingToolbarColors(toolbarContainerColor=Color(0xFF000080),toolbarContentColor=Color(0xFF00FF00))
                else if(config.vibrant)FloatingToolbarDefaults.vibrantFloatingToolbarColors() else FloatingToolbarDefaults.standardFloatingToolbarColors()
            val padding=if(config.custom=="padding")PaddingValues(start=4.dp,top=12.dp,end=16.dp,bottom=8.dp) else FloatingToolbarDefaults.ContentPadding
            val shape=if(config.custom=="shape")RoundedCornerShape(8.dp) else FloatingToolbarDefaults.ContainerShape
            val expandedElevation=if(config.custom=="elevation")3.dp else if(config.fab=="none")FloatingToolbarDefaults.ContainerExpandedElevation else FloatingToolbarDefaults.ContainerExpandedElevationWithFab
            val collapsedElevation=if(config.custom=="elevation")1.dp else 0.dp
            if(config.action in listOf("scroll","snap","fling")) {
                val direction=when(config.exit){"start"->FloatingToolbarExitDirection.Start;"end"->FloatingToolbarExitDirection.End;"top"->FloatingToolbarExitDirection.Top;else->FloatingToolbarExitDirection.Bottom}
                behavior=FloatingToolbarDefaults.exitAlwaysScrollBehavior(direction)
                scope=rememberCoroutineScope()
            }
            val button:@Composable ()->Unit={
                if(config.vibrant)FloatingToolbarDefaults.VibrantFloatingActionButton({},fab.modifier()){glyph(24.0,24.0)}
                else FloatingToolbarDefaults.StandardFloatingActionButton({},fab.modifier()){glyph(24.0,24.0)}
            }
            if(config.horizontal) {
                if(config.fab=="none")HorizontalFloatingToolbar(expanded,modifier,colors,padding,behavior,shape,
                    leadingContent=if(config.leading) ({glyph(24.0,24.0)}) else null,
                    trailingContent=if(config.trailing) ({glyph(24.0,24.0)}) else null,
                    expandedShadowElevation=expandedElevation,collapsedShadowElevation=collapsedElevation) {mainContent()}
                else HorizontalFloatingToolbar(expanded,button,modifier,colors,padding,behavior,shape,
                    floatingActionButtonPosition=if(config.fab=="start")FloatingToolbarHorizontalFabPosition.Start else FloatingToolbarHorizontalFabPosition.End,
                    expandedShadowElevation=expandedElevation,collapsedShadowElevation=collapsedElevation) {mainContent()}
            } else {
                if(config.fab=="none")VerticalFloatingToolbar(expanded,modifier,colors,padding,behavior,shape,
                    leadingContent=if(config.leading) ({glyph(24.0,24.0)}) else null,
                    trailingContent=if(config.trailing) ({glyph(24.0,24.0)}) else null,
                    expandedShadowElevation=expandedElevation,collapsedShadowElevation=collapsedElevation) {mainContent()}
                else VerticalFloatingToolbar(expanded,button,modifier,colors,padding,behavior,shape,
                    floatingActionButtonPosition=if(config.fab=="start")FloatingToolbarVerticalFabPosition.Top else FloatingToolbarVerticalFabPosition.Bottom,
                    expandedShadowElevation=expandedElevation,collapsedShadowElevation=collapsedElevation) {mainContent()}
            }
        }
        fun measure():List<ProbePoint> {
            fun geometry(probe:Probe)=probe.geometry().filter {it.name=="box" || it.name=="size"}
            val fields=geometry(toolbar).toMutableList()
            behavior?.let {
                fields+=number("offset",(it.state.offset/ComposeHost.RenderScale).toDouble(),"dip",category="motion",definition="floating-toolbar.scroll-offset")
                fields+=number("offsetLimit",(it.state.offsetLimit/ComposeHost.RenderScale).toDouble(),"dip",definition="floating-toolbar.scroll-limit")
                fields+=number("contentOffset",(it.state.contentOffset/ComposeHost.RenderScale).toDouble(),"dip",definition="floating-toolbar.content-offset")
            }
            val points=mutableListOf(ProbePoint("toolbar","toolbar",null,fields))
            if(config.action !in listOf("scroll","snap","fling"))points+=ProbePoint("content","content","toolbar",geometry(content))
            if(config.fab!="none")points+=ProbePoint("fab","fab","toolbar",geometry(fab))
            return points
        }
    }
}

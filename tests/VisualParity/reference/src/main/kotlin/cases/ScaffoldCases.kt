@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable
data class ScaffoldConfig(val position:String="end", val rtl:Boolean=false, val topHeight:Double?=40.0,
    val bottomHeight:Double?=32.0, val fabWidth:Double?=56.0, val fabHeight:Double=56.0, val snackbarWidth:Double?=120.0,
    val snackbarHeight:Double=24.0, val width:Double=240.0, val height:Double=200.0, val insets:List<Double>?=null,
    val consumed:List<Double>?=null, val dark:Boolean=false, val color:String?=null, val contentColor:String?=null,
    val applyPadding:Boolean=true, val nested:Boolean=false, val stacked:Boolean=false, val action:String="none")

@ParitySuite("scaffold")
class ScaffoldCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,320,280).use { host ->
            val scene=ScaffoldScene(writer.readConfig<ScaffoldConfig>())
            host.mount { scene.Content() }; host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("changes")
    suspend fun changes(writer:CaptureWriter) {
        val config=writer.readConfig<ScaffoldConfig>()
        ComposeHost(writer,320,280).use { host ->
            val scene=ScaffoldScene(config)
            host.mount { scene.Content() }; host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
            fun change(time:Long,id:String,action:()->Unit) {
                host.advanceTo(time); action(); writer.record(time,"mutation",id); host.draw(); host.frame(time); sample(id)
            }
            sample("initial")
            when(config.action) {
                "bars" -> {
                    change(1000,"removed") { scene.topHeight=null; scene.bottomHeight=null }
                    change(2000,"collapsed") { scene.topHeight=0.0; scene.bottomHeight=0.0 }
                    change(3000,"resized") { scene.topHeight=56.0; scene.bottomHeight=24.0 }
                }
                "fab-position" -> {
                    change(1000,"start") { scene.position="start" }; change(2000,"center") { scene.position="center" }
                    change(3000,"overlay") { scene.position="end-overlay" }
                }
                "fab-size" -> {
                    change(1000,"zero-width") { scene.fabWidth=0.0 }
                    change(2000,"wide") { scene.fabWidth=80.0; scene.fabHeight=40.0 }
                    change(3000,"removed") { scene.fabWidth=null }
                }
                "snackbar" -> {
                    change(1000,"removed") { scene.snackbarWidth=null }
                    change(2000,"replaced") { scene.snackbarWidth=160.0; scene.snackbarHeight=40.0 }
                    change(3000,"collapsed") { scene.snackbarHeight=0.0 }
                }
                "insets" -> {
                    change(1000,"changed") { scene.insets=listOf(16.0,30.0,24.0,40.0) }
                    change(2000,"consumed") { scene.consumed=listOf(4.0,12.0,8.0,16.0) }
                    change(3000,"cleared") { scene.consumed=null }
                }
                "direction" -> {
                    change(1000,"rtl") { scene.rtl=true }; change(2000,"ltr") { scene.rtl=false }
                }
                "theme" -> {
                    change(1000,"dark") { scene.dark=true }; change(2000,"light") { scene.dark=false }
                }
                "colors" -> {
                    change(1000,"primary") { scene.color="primary" }; change(2000,"ambient") { scene.color="#70336699" }
                    change(3000,"explicit") { scene.contentColor="#FF008877" }
                    change(4000,"default") { scene.color=null; scene.contentColor=null }
                }
                "resize" -> {
                    change(1000,"smaller") { scene.width=160.0; scene.height=140.0 }
                    change(2000,"restore") { scene.width=240.0; scene.height=200.0 }
                }
            }
            host.advanceTo(5000); sample("idle-layout",image=false)
        }
    }

    private class ScaffoldScene(val config:ScaffoldConfig) {
        val screen=Probe(); val top=Probe(); val bottom=Probe(); val fab=Probe(); val snackbar=Probe()
        val body=Probe(); val bodyContent=Probe(); val marker=Probe(); val nested=Probe()
        // Keep the actual PaddingValues object: Scaffold mutates its holder during measurement.
        // Reading/copying the values in composition would observe an earlier layout pass.
        var padding:PaddingValues?=null; var nestedPadding:PaddingValues?=null
        var position by mutableStateOf(config.position); var rtl by mutableStateOf(config.rtl)
        var topHeight by mutableStateOf(config.topHeight); var bottomHeight by mutableStateOf(config.bottomHeight)
        var fabWidth by mutableStateOf(config.fabWidth); var fabHeight by mutableStateOf(config.fabHeight)
        var snackbarWidth by mutableStateOf(config.snackbarWidth); var snackbarHeight by mutableStateOf(config.snackbarHeight)
        var width by mutableStateOf(config.width); var height by mutableStateOf(config.height)
        var insets by mutableStateOf(config.insets); var consumed by mutableStateOf(config.consumed)
        var dark by mutableStateOf(config.dark); var color by mutableStateOf(config.color); var contentColor by mutableStateOf(config.contentColor)

        @Composable fun Content() {
            val scheme=if(dark) darkColorScheme(background=Color(0xFF141218),onBackground=Color(0xFFE6E0E9))
                else lightColorScheme(background=Color(0xFFFFF7FF),onBackground=Color(0xFF1D1B20))
            MaterialTheme(colorScheme=scheme) {
                CompositionLocalProvider(LocalLayoutDirection provides if(rtl) LayoutDirection.Rtl else LayoutDirection.Ltr,
                    LocalContentColor provides Color(0xFF345678)) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        val consumedPadding=paddingValues(consumed)
                        Box(Modifier.consumeWindowInsets(consumedPadding).padding(consumedPadding)) {
                            Scaffold(modifier=screen.modifier().size(width.toFloat().dp,height.toFloat().dp),
                                topBar={ topHeight?.let { Box(top.modifier().fillMaxWidth().height(it.toFloat().dp).background(Color(0xFF336699))) } },
                                bottomBar={ bottomHeight?.let { Box(bottom.modifier().fillMaxWidth().height(it.toFloat().dp).background(Color(0xFF228866))) } },
                                snackbarHost={ snackbarWidth?.let { Box(snackbar.modifier().size(it.toFloat().dp,snackbarHeight.toFloat().dp).background(Color(0xFF8866AA))) } },
                                floatingActionButton={ fabWidth?.let { Box(fab.modifier().size(it.toFloat().dp,fabHeight.toFloat().dp).background(Color(0xFFDD7722))) } },
                                floatingActionButtonPosition=when(position) { "start"->FabPosition.Start; "center"->FabPosition.Center; "end-overlay"->FabPosition.EndOverlay; else->FabPosition.End },
                                contentWindowInsets=windowInsets(insets),
                                containerColor=color?.let { if(it=="primary") MaterialTheme.colorScheme.primary else parseColor(it) } ?: MaterialTheme.colorScheme.background,
                                contentColor=contentColor?.let { parseColor(it) } ?: contentColorFor(color?.let {
                                    if(it=="primary") MaterialTheme.colorScheme.primary else parseColor(it) } ?: MaterialTheme.colorScheme.background)) { values ->
                                padding=values
                                var modifier=body.modifier().fillMaxSize()
                                if(config.applyPadding) modifier=modifier.padding(values).consumeWindowInsets(values)
                                Box(modifier) {
                                    Box(bodyContent.modifier().fillMaxSize(),contentAlignment=Alignment.Center) {
                                        if(config.nested) Scaffold(modifier=nested.modifier().fillMaxSize(),contentWindowInsets=windowInsets(insets)) { inner ->
                                            nestedPadding=inner
                                            Box(Modifier.fillMaxSize(),contentAlignment=Alignment.Center) { Marker() }
                                        } else Marker()
                                        if(config.stacked) Box(Modifier.align(Alignment.TopStart).size(36.dp,16.dp).background(Color(0xFF2288AA)))
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        @Composable fun Marker() { Box(marker.modifier().size(24.dp).background(LocalContentColor.current)) }
        fun measure():List<ProbePoint> {
            val points=mutableListOf(point("scaffold",screen,null,padding(checkNotNull(padding))), point("body",body,"scaffold"),
                point("body-content",bodyContent,"body"), point("marker",marker,if(config.nested) "nested" else "body-content"))
            if(topHeight!=null) points+=point("top-bar",top,"scaffold")
            if(bottomHeight!=null) points+=point("bottom-bar",bottom,"scaffold")
            if(snackbarWidth!=null) points+=point("snackbar",snackbar,"scaffold")
            // Scaffold does not place a FAB having either zero dimension. Coordinates are
            // observed only when the actual placeable has nonzero size, not from configured size.
            if(fabWidth!=null && (fab.coordinates?.size?.width ?: 0)>0 && (fab.coordinates?.size?.height ?: 0)>0)
                points+=point("fab",fab,"scaffold")
            if(config.nested) points+=point("nested",nested,"body-content",padding(checkNotNull(nestedPadding)))
            return points
        }
        private fun point(id:String,probe:Probe,parent:String?,vararg extras:Measurement)=ProbePoint(id,id,parent,
            probe.geometry().filter { it.name=="box" || it.name=="size" }+extras)
        private fun padding(p:PaddingValues):Measurement {
            val direction=if(rtl) LayoutDirection.Rtl else LayoutDirection.Ltr
            return vector("contentPadding","rect",listOf(p.calculateStartPadding(direction).value.toDouble(),p.calculateTopPadding().value.toDouble(),
                p.calculateEndPadding(direction).value.toDouble(),p.calculateBottomPadding().value.toDouble()),"dip","logical","geometry",
                "scaffold.content-padding.logical","Scaffold content PaddingValues",listOf("start","top","end","bottom"))
        }
        private fun paddingValues(v:List<Double>?)=if(v==null) PaddingValues(0.dp) else PaddingValues(v[0].toFloat().dp,v[1].toFloat().dp,v[2].toFloat().dp,v[3].toFloat().dp)
        private fun windowInsets(v:List<Double>?)=if(v==null) WindowInsets(0,0,0,0) else WindowInsets(v[0].toFloat().dp,v[1].toFloat().dp,v[2].toFloat().dp,v[3].toFloat().dp)
        private fun parseColor(value:String):Color { val argb=value.removePrefix("#").toLong(16); return Color(if(value.length==7) argb or 0xFF000000 else argb) }
    }
}

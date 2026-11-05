@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.MutatorMutex
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.Alignment
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import androidx.compose.ui.window.PopupPositionProvider
import kotlinx.serialization.Serializable

@Serializable
data class BadgeBaselineConfig(val tooltip:Boolean=false, val rtl:Boolean=false, val nested:Boolean=false, val height:Double?=null)

@ParitySuite("badge-baseline")
class BadgeBaselineCases {
    @ParityScenario("baseline")
    suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<BadgeBaselineConfig>()
        ComposeHost(writer,400,240).use { host ->
            val root=Probe(); val box=Probe(); val anchor=Probe(); val badge=Probe(); val other=Probe()
            var first by mutableStateOf<Int?>(10)
            var last by mutableStateOf<Int?>(26)
            var badgeFirst by mutableStateOf(1)
            var actualFirst=AlignmentLine.Unspecified; var actualLast=AlignmentLine.Unspecified
            @Composable fun Marker(probe:Probe,width:Int,height:Int,color:Color,first:Int?=null,last:Int?=null) {
                Layout(content={ Canvas(Modifier.size(width.dp,height.dp)) { drawRect(color) } },modifier=probe.modifier()) { measurables,constraints ->
                    val child=measurables.single().measure(constraints)
                    val lines=buildMap<AlignmentLine,Int> {
                        first?.let { put(FirstBaseline,it.dp.roundToPx()) }
                        last?.let { put(LastBaseline,it.dp.roundToPx()) }
                    }
                    layout(child.width,child.height,lines) { child.place(0,0) }
                }
            }
            @Composable fun Anchor() { Marker(anchor,60,32,Color.Blue,first,last) }
            @Composable fun BadgeContent() { Marker(badge,8,16,Color.Red,badgeFirst,15) }
            @Composable fun BoxContent() {
                var modifier=box.modifier().layout { measurable,constraints ->
                    val child=measurable.measure(constraints)
                    actualFirst=child[FirstBaseline]; actualLast=child[LastBaseline]
                    layout(child.width,child.height) { child.place(0,0) }
                }
                config.height?.let { modifier=modifier.height(it.toFloat().dp) }
                if(config.nested) BadgedBox(badge={ Badge() },modifier=modifier) { BadgedBox(badge={ BadgeContent() }) { Anchor() } }
                else BadgedBox(badge={ BadgeContent() },modifier=modifier) { Anchor() }
            }
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) {
                            if(config.tooltip) TooltipBox(
                                positionProvider=object:PopupPositionProvider {
                                    override fun calculatePosition(anchorBounds:IntRect,windowSize:IntSize,layoutDirection:LayoutDirection,popupContentSize:IntSize)=
                                        IntOffset((windowSize.width-popupContentSize.width)/2,(windowSize.height-popupContentSize.height)/2)
                                },
                                state=rememberTooltipState(initialIsVisible=true,mutatorMutex=remember { MutatorMutex() }),
                                enableUserInput=false,
                                tooltip={ RichTooltip(title={ BoxContent() },modifier=root.modifier()) { Marker(other,24,24,Color(0xFF008000)) } }
                            ) { Box(Modifier.size(1.dp)) }
                            else Snackbar(modifier=root.modifier().width(320.dp),action={ Marker(other,24,24,Color(0xFF008000)) },actionOnNewLine=true) { BoxContent() }
                        }
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            fun geometry(probe:Probe):List<Measurement> {
                val c=checkNotNull(probe.coordinates)
                val origin=c.localToWindow(Offset.Zero)/ComposeHost.RenderScale.toFloat()
                return listOf(vector("box","rect",listOf(origin.x.toDouble(),origin.y.toDouble(),c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),
                    "dip","scene","geometry","layout.box.aabb.unclipped","LayoutCoordinates.size + localToWindow / density"),
                    vector("size","point",listOf(c.size.width.toDouble()/ComposeHost.RenderScale,c.size.height.toDouble()/ComposeHost.RenderScale),"dip","local","geometry","layout.box.local-size","LayoutCoordinates.size / density",listOf("width","height")))
            }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("host","host",null,geometry(root)),
                ProbePoint("box","box","host",geometry(box)+listOf(
                    number("first",actualFirst.toDouble(),"px",definition="badge.anchor-first-baseline",source="actual BadgedBox Placeable[FirstBaseline]"),
                    number("last",actualLast.toDouble(),"px",definition="badge.anchor-last-baseline",source="actual BadgedBox Placeable[LastBaseline]"))),
                ProbePoint("anchor","anchor","box",geometry(anchor)),
                ProbePoint("badge","badge","box",geometry(badge)),
                ProbePoint("other","other","host",geometry(other)))))
            fun change(time:Long,id:String,action:()->Unit) { host.advanceTo(time); action(); writer.record(time,"mutation",id); host.frame(time); sample(id) }
            sample("initial")
            change(1000,"first") { first=6 }
            change(2000,"last") { last=20 }
            change(3000,"badge-only") { badgeFirst=30 }
            change(4000,"no-anchor-lines") { first=null; last=null }
            change(5000,"restore") { first=10; last=26 }
        }
    }
}

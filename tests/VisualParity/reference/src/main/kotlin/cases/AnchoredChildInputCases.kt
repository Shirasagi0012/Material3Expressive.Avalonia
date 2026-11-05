@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import kotlinx.serialization.Serializable

@Serializable
data class AnchoredChildInputConfig(val kind:String,val rtl:Boolean,val nativeThumb:Boolean=false)

@ParitySuite("anchored-child-input")
class AnchoredChildInputCases {
    @ParityScenario("disable-parent")
    suspend fun disableParent(writer:CaptureWriter) {
        val c=writer.readConfig<AnchoredChildInputConfig>()
        ComposeHost(writer,320,240).use { host ->
            var gestures by mutableStateOf(true)
            var clicks=0
            lateinit var swipe:SwipeToDismissBoxState
            lateinit var sheet:SheetState
            val child=Probe()
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        val content:@Composable ()->Unit={
                            Box(Modifier.fillMaxWidth().height(128.dp).background(Color(0xFF336699))) {
                                Box(child.modifier().size(80.dp,48.dp).background(Color(0xFFFFD166))
                                    .then(if(c.nativeThumb) Modifier.draggable(rememberDraggableState {},
                                        if(c.kind=="sheet") Orientation.Vertical else Orientation.Horizontal)
                                        else Modifier.clickable(interactionSource=null,indication=null) { clicks++ }))
                            }
                        }
                        Box(Modifier.fillMaxSize().background(Color.White)) {
                            if(c.kind=="swipe") {
                                swipe=rememberSwipeToDismissBoxState()
                                SwipeToDismissBox(state=swipe,modifier=Modifier.size(240.dp,128.dp),
                                    gesturesEnabled=gestures,backgroundContent={}) { content() }
                            } else {
                                val density=LocalDensity.current
                                sheet=remember { SheetState(enabledValues=SheetValue.entries.toSet(),initialValue=SheetValue.Expanded,
                                    positionalThreshold={ with(density) { 56.dp.toPx() } },velocityThreshold={ with(density) { 125.dp.toPx() } }) }
                                BottomSheet(state=sheet,onDismissRequest={},gesturesEnabled=gestures,dragHandle=null,
                                    contentWindowInsets={ WindowInsets(0) }) { content() }
                            }
                        }
                    }
                }
            }
            host.frame(-1000); host.frame(0)
            var nextFrame=16000L
            fun advance(time:Long) {
                while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }
                host.advanceTo(time)
            }
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(
                ProbePoint("child","child",null,child.geometry().filter { it.name in listOf("box","size") }+listOf(
                    number("clicks",clicks.toDouble(),"count",definition="child.clicks"),
                    number("offset",(if(c.kind=="swipe")swipe.requireOffset() else sheet.requireOffset()).toDouble()/ComposeHost.RenderScale,
                        "dip","local","motion","parent.offset","Mounted parent state offset / density"))))))
            val box=checkNotNull(child.geometry().first { it.name=="box" }.numbers)
            val point=Offset(box[0].toFloat()+20f,box[1].toFloat()+20f)
            val end=if(c.nativeThumb) point+(if(c.kind=="sheet") Offset(0f,60f) else Offset(if(c.rtl)-60f else 60f,0f)) else point
            sample("initial")
            advance(1000); host.input("move",point); host.input("down",point); host.draw(); sample("pressed")
            advance(17000)
            if(c.nativeThumb) host.input("move",end)
            else { gestures=false; writer.record(17000,"mutation","disable-parent") }
            host.scheduler.runCurrent(); host.draw(); sample("changed")
            advance(32000); host.draw(); sample("committed")
            advance(49000); host.input("up",end); host.draw(); sample("released")
            advance(80000); host.draw(); sample("settled")
        }
    }
}

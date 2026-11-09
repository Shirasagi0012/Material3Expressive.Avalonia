@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.VectorConverter
import androidx.compose.animation.core.spring
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.FirstBaseline
import androidx.compose.ui.layout.LastBaseline
import androidx.compose.ui.layout.layout
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class TabIndicatorLayoutConfig(val native:Boolean=false,val scrollable:Boolean=false,val rtl:Boolean=false,
    val mode:String="elastic",val action:String="none")

@ParitySuite("tab-indicator-layout")
class TabIndicatorLayoutCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,384,180).use { host ->
            val scene=Scene(writer.readConfig<TabIndicatorLayoutConfig>())
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("changes")
    suspend fun changes(writer:CaptureWriter) {
        ComposeHost(writer,384,180).use { host ->
            val config=writer.readConfig<TabIndicatorLayoutConfig>(); val scene=Scene(config)
            host.mount { scene.Content() }; host.frame(-1000); host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(),image))
            var nextFrame=16000L
            fun advance(time:Long) { while(nextFrame<=time) { host.frame(nextFrame); nextFrame+=16000 }; host.advanceTo(time) }
            fun times(vararg times:Long) { times.forEach { advance(it); host.draw(); sample("time-$it") } }
            fun change(time:Long,id:String,action:()->Unit) { advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);sample(id) }
            sample("initial")
            if(config.action=="reverse") {
                change(1000,"last") { scene.selected=4 }; times(16000,32000,64000)
                change(65000,"first") { scene.selected=0 }; times(80000,96000,160000,400000,1000000)
            } else {
                change(1000,"select-second") { scene.selected=1 }; times(16000,64000)
                change(65000,"grow-first") { scene.firstWidth=112.0 }; times(80000,128000,400000)
                change(401000,"shrink-first") { scene.firstWidth=24.0 }; times(416000,480000,1000000)
            }
            advance(1100000);host.draw();sample("passive-late",false)
        }
    }
    private class Scene(val config:TabIndicatorLayoutConfig) {
        var selected by mutableStateOf(0)
        var firstWidth by mutableStateOf(32.0)
        val indicator=Probe()
        var minWidth=0.0; var maxWidth=0.0; var maxHeight=0.0
        var selectedStart=0.0; var selectedEnd=0.0
        @Composable
        fun Content() {
            MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(config.rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(start=16.dp,end=16.dp,top=16.dp)) {
                        val tabs:@Composable ()->Unit = {
                            repeat(5) { index ->
                                Tab(selected==index,{selected=index},text={
                                    val color=LocalContentColor.current
                                    Canvas(Modifier.size((if(index==0)firstWidth else 32.0+index*16).toFloat().dp,16.dp)
                                        .layout { measurable,constraints ->
                                            val p=measurable.measure(constraints)
                                            layout(p.width,p.height,mapOf(FirstBaseline to 12.dp.roundToPx(),LastBaseline to 12.dp.roundToPx())) { p.placeRelative(0,0) }
                                        }) { drawRect(color) }
                                })
                            }
                        }
                        val custom:@Composable TabIndicatorScope.()->Unit={ Indicator() }
                        if(config.scrollable) PrimaryScrollableTabRow(selected,Modifier.width(352.dp),indicator=custom,tabs=tabs)
                        else PrimaryTabRow(selected,Modifier.width(352.dp),indicator=custom,tabs=tabs)
                    }
                }
            }
        }
        @Composable
        fun TabIndicatorScope.Indicator() {
            val scope=rememberCoroutineScope()
            var start by remember { mutableStateOf<Animatable<Dp, androidx.compose.animation.core.AnimationVector1D>?>(null) }
            var end by remember { mutableStateOf<Animatable<Dp, androidx.compose.animation.core.AnimationVector1D>?>(null) }
            Box(Modifier.tabIndicatorLayout { measurable,constraints,positions ->
                minWidth=constraints.minWidth.toDouble()/density;maxWidth=constraints.maxWidth.toDouble()/density;maxHeight=constraints.maxHeight.toDouble()/density
                selectedStart=positions[selected].left.value.toDouble();selectedEnd=positions[selected].right.value.toDouble()
                if(config.mode!="elastic") {
                    val width=if(config.mode=="undersize")20.dp.roundToPx() else 315.dp.roundToPx()
                    val height=(if(config.mode=="undersize")12 else if(config.mode=="tall")72 else 48).dp.roundToPx()
                    val p=measurable.measure(Constraints.fixed(width,height))
                    layout(width,height) { p.placeRelative(0,0) }
                } else {
                    val newStart=positions[selected].left;val newEnd=positions[selected].right
                    val s=start ?: Animatable(newStart,Dp.VectorConverter).also { start=it }
                    val e=end ?: Animatable(newEnd,Dp.VectorConverter).also { end=it }
                    if(e.targetValue!=newEnd) scope.launch { e.animateTo(newEnd,spring(dampingRatio=1f,stiffness=if(e.targetValue<newEnd)1000f else 50f)) }
                    if(s.targetValue!=newStart) scope.launch { s.animateTo(newStart,spring(dampingRatio=1f,stiffness=if(s.targetValue<newStart)50f else 1000f)) }
                    val left=s.value.roundToPx();val right=e.value.roundToPx()
                    val p=measurable.measure(constraints.copy(minWidth=right-left,maxWidth=right-left))
                    layout(constraints.maxWidth,constraints.maxHeight) {
                        val correction=if(config.scrollable)(positions[selected].width.roundToPx()-positions[selected].contentWidth.roundToPx())/2 else 0
                        p.placeRelative(left-correction,0)
                    }
                }
            }.then(indicator.modifier()).fillMaxSize().background(Color(0x8055AADD)))
        }
        fun measure()=listOf(
            ProbePoint("indicator","indicator",null,indicator.geometry().filter { it.name=="box" || it.name=="size" }),
            ProbePoint("layout","state",null,listOf(
                number("selected",selected.toDouble(),"index",definition="tab-custom.selected-index"),
                number("minWidth",minWidth,"dip",definition="tab-custom.min-width"),
                number("maxWidth",maxWidth,"dip",definition="tab-custom.max-width"),
                number("maxHeight",maxHeight,"dip",definition="tab-custom.max-height"),
                number("selectedStart",selectedStart,"dip",definition="tab-custom.selected-start"),
                number("selectedEnd",selectedEnd,"dip",definition="tab-custom.selected-end")
            ))
        )
    }
}

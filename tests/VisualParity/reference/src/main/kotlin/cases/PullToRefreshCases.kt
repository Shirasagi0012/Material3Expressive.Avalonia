@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class,
    androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.animation.core.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.material3.pulltorefresh.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class PullToRefreshConfig(val kind:String="circular",val fraction:Double=1.0,val refreshing:Boolean=false,
    val dark:Boolean=false,val rtl:Boolean=false,val constraint:Double?=null,val customColors:Boolean=false,
    val maxDistance:Double=80.0,val action:String="none",val translucent:Boolean=false,val customMotion:Boolean=false)

@ParitySuite("pull-to-refresh")
class PullToRefreshCases {
    @ParityScenario("indicator") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<PullToRefreshConfig>()
        val state=if(c.customMotion) CustomPullMotionState() else PullToRefreshState()
        state.snapTo(c.fraction.toFloat())
        var refreshing by mutableStateOf(c.refreshing)
        var present by mutableStateOf(true)
        lateinit var scope:CoroutineScope
        val indicator=Probe()
        ComposeHost(writer,240,320).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(top=16.dp),contentAlignment=Alignment.TopCenter) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            Box(c.constraint?.let{Modifier.size(it.toFloat().dp)}?:Modifier,contentAlignment=Alignment.TopCenter) {
                                if(present) {
                                    scope=rememberCoroutineScope()
                                    when(c.kind) {
                                        "custom" -> PullToRefreshDefaults.IndicatorBox(state,refreshing,indicator.modifier(),
                                            maxDistance=c.maxDistance.toFloat().dp,containerColor=Color.Yellow) {
                                            Box(Modifier.size(12.dp).background(Color.Blue))
                                        }
                                        "loading" -> PullToRefreshDefaults.LoadingIndicator(state,refreshing,indicator.modifier(),
                                            containerColor=if(c.translucent)Color(0x8000CCAA) else if(c.customColors)Color.Yellow else PullToRefreshDefaults.loadingIndicatorContainerColor,
                                            color=if(c.translucent)Color(0x806750A4) else if(c.customColors)Color.Blue else PullToRefreshDefaults.loadingIndicatorColor,
                                            maxDistance=c.maxDistance.toFloat().dp)
                                        else -> PullToRefreshDefaults.Indicator(state,refreshing,indicator.modifier(),
                                            containerColor=if(c.translucent)Color(0x8000CCAA) else if(c.customColors)Color.Yellow else PullToRefreshDefaults.indicatorContainerColor,
                                            color=if(c.translucent)Color(0x806750A4) else if(c.customColors)Color.Blue else PullToRefreshDefaults.indicatorColor,
                                            maxDistance=c.maxDistance.toFloat().dp)
                                    }
                                }
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String) {
                val mounted=indicator.coordinates?.isAttached==true
                val points=mutableListOf(ProbePoint("state","pull-state",null,listOf(
                    number("distanceFraction",state.distanceFraction.toDouble(),"fraction",definition="pull.distance-fraction"),
                    flag("animating",state.isAnimating,definition="pull.animating"),
                    flag("refreshing",refreshing,definition="pull.refreshing"),
                    flag("present",mounted,definition="pull.present"))))
                if(mounted)points.add(ProbePoint("indicator","indicator-layout","state",indicator.geometry()))
                writer.submit(host.snapshot(id,id,points))
            }
            var nextFrame=16000L
            fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){
                advance(time);action();writer.record(time,"mutation",id);host.draw()
                // Match both hosts' composition and newly created waiter batches without advancing time.
                host.frame(time);host.frame(time);sample(id)
            }
            fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            sample("initial")
            when(c.action) {
                "bounce" -> {
                    change(1000,"threshold"){scope.launch{state.animateToThreshold()}}
                    at(16000,48000,96000,144000,240000,400000,800000,1600000)
                    change(1601000,"hide"){scope.launch{state.animateToHidden()}}
                    at(1616000,1680000,1760000,1856000)
                }
                "state-detach" -> {
                    change(1000,"threshold"){scope.launch{state.animateToThreshold()}};at(16000,48000,80000)
                    change(81000,"detach"){present=false};at(240000)
                    change(241000,"reattach"){present=true}
                    change(257000,"hide"){scope.launch{state.animateToHidden()}};at(272000,320000,400000,640000)
                }
                "conflation" -> {
                    listOf(1000L to 1.2f,2000L to .7f,3000L to 1.1f).forEach{(time,fraction)->
                        advance(time);state.snapTo(fraction);writer.record(time,"mutation","distance-before-frame")
                        host.draw();sample("input-$time")
                    }
                    at(16000,32000,64000,160000,640000)
                }
                "progress" -> {
                    change(1000,"below"){scope.launch{state.snapTo(.8f)}};at(16000,64000,160000)
                    change(161000,"above"){scope.launch{state.snapTo(1.2f)}};at(176000,208000)
                    change(209000,"below-again"){scope.launch{state.snapTo(.7f)}};at(224000,320000,640000)
                    change(641000,"tension"){scope.launch{state.snapTo(3f)}};at(656000,800000,1008000)
                }
                "state" -> {
                    change(1000,"threshold"){scope.launch{state.animateToThreshold()}};at(16000,48000,96000)
                    change(97000,"hide"){scope.launch{state.animateToHidden()}};at(112000,160000,240000)
                    change(241000,"snap-overshoot"){scope.launch{state.snapTo(1.4f)}}
                    change(257000,"threshold-again"){scope.launch{state.animateToThreshold()}};at(272000,400000,800000)
                }
                "refresh","reverse" -> {
                    change(1000,"refresh"){refreshing=true};at(16000,48000,80000)
                    val stop=if(c.action=="reverse")81000L else 737000L
                    if(c.action=="refresh")at(160000,320000,640000,736000)
                    change(stop,"stop"){refreshing=false};at(stop+16000,stop+64000,stop+160000,stop+640000)
                }
                "detach" -> {
                    at(64000,160000);change(161000,"detach"){present=false};at(320000)
                    change(321000,"reattach"){present=true};at(336000,400000,640000,1008000)
                }
                "indeterminate" -> at(16000,80000,160000,320000,640000,656000,736000,1300000,2600000,5200000)
            }
        }
    }
}

internal class CustomPullMotionState : PullToRefreshState {
    private val anim = Animatable(0f, Float.VectorConverter)
    override val distanceFraction get() = anim.value
    override val isAnimating get() = anim.isRunning
    override suspend fun animateToThreshold() { anim.animateTo(1f, spring(dampingRatio = Spring.DampingRatioHighBouncy)) }
    override suspend fun animateToHidden() { anim.animateTo(0f, tween(240)) }
    override suspend fun snapTo(targetValue: Float) { anim.snapTo(targetValue) }
}

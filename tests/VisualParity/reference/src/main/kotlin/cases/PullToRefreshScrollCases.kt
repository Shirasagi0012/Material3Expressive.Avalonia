@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class,androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.material3.pulltorefresh.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.nestedscroll.*
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class PullRefreshScrollConfig(val action:String="release",val drag:Float=161f,val velocity:Float=200f,val threshold:Double=80.0)

@ParitySuite("pull-refresh-scroll")
class PullToRefreshScrollCases {
    @ParityScenario("scroll") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<PullRefreshScrollConfig>()
        val state=PullToRefreshState();state.snapTo(2f)
        var refreshing by mutableStateOf(false);var enabled by mutableStateOf(true);var present by mutableStateOf(true)
        val dispatcher=NestedScrollDispatcher();val empty=object:NestedScrollConnection{}
        lateinit var scope:CoroutineScope
        var requests=0;var consumed=0f;var velocity=0f;var completed=false;var canceled=false
        ComposeHost(writer,240,320).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    Box(Modifier.fillMaxSize().background(Color.White)) {
                        if(present) {
                            scope=rememberCoroutineScope()
                            Box(Modifier.fillMaxSize().pullToRefresh(refreshing,state,enabled,c.threshold.toFloat().dp){requests++;if(c.action=="callback")refreshing=true}) {
                                Box(Modifier.fillMaxSize().nestedScroll(empty,dispatcher))
                                PullToRefreshDefaults.Indicator(state,refreshing,Modifier.align(Alignment.TopCenter).padding(top=16.dp))
                            }
                        }
                    }
                }
            };host.frame(0)
            fun post(delta:Float,user:Boolean=true):Float=dispatcher.dispatchPostScroll(Offset.Zero,Offset(0f,delta*ComposeHost.RenderScale),if(user)NestedScrollSource.UserInput else NestedScrollSource.SideEffect).y/ComposeHost.RenderScale
            fun release(){scope.launch{try{velocity=dispatcher.dispatchPreFling(Velocity(0f,c.velocity*ComposeHost.RenderScale)).y/ComposeHost.RenderScale;completed=true}catch(e:CancellationException){canceled=true}}}
            fun sample(id:String){writer.submit(host.snapshot(id,id,listOf(ProbePoint("state","scroll-state",null,listOf(
                number("fraction",state.distanceFraction.toDouble(),"fraction"),number("consumed",consumed.toDouble(),"dip"),
                number("velocity",velocity.toDouble(),"dip/s"),number("requests",requests.toDouble(),"count"),flag("animating",state.isAnimating),
                flag("completed",completed),flag("canceled",canceled),flag("refreshing",refreshing))))))}
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();host.frame(time);host.frame(time);sample(id)}
            fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            sample("initial")
            if(c.action=="programmatic"){
                change(1000,"start"){refreshing=true};at(16000,48000,160000,640000)
                change(641000,"stop"){refreshing=false};at(656000,720000,1008000);return
            }
            change(1000,"pull"){consumed=post(c.drag,c.action!="side-effect")};at(16000)
            if(c.action=="pre-post"){
                change(17000,"pre"){consumed=dispatcher.dispatchPreScroll(Offset(0f,-30f*ComposeHost.RenderScale),NestedScrollSource.UserInput).y/ComposeHost.RenderScale}
                change(33000,"post"){consumed=post(0f)}
                change(49000,"cap"){consumed=post(600f)};at(64000)
            }
            if(c.action=="disabled"){
                change(17000,"disable"){enabled=false}
                change(33000,"disabled-input"){consumed=post(20f)}
            }
            change(161000,"release"){release()};at(176000,208000)
            if(c.action=="detach")change(209000,"detach"){present=false}
            at(240000,400000,800000)
            if(c.action=="callback"){
                change(801000,"complete-refresh"){refreshing=false};at(816000,960000,1408000)
            }
        }
    }
}

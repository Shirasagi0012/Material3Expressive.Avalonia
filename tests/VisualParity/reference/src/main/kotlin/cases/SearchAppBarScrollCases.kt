@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.*
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.nestedscroll.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class SearchAppBarScrollConfig(val action:String,val rtl:Boolean=false,val reverse:Boolean=false,val stretch:Boolean=false)

@ParitySuite("search-app-bar-scroll")
class SearchAppBarScrollCases {
    @ParityScenario("scroll") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SearchAppBarScrollConfig>()
        var generation by mutableIntStateOf(0)
        var enabled by mutableStateOf(true)
        lateinit var behavior:SearchBarScrollBehavior
        lateinit var scroll:ScrollState
        lateinit var scope:CoroutineScope
        val dispatcher=NestedScrollDispatcher()
        val childConnection=object:NestedScrollConnection{}
        val bar=Probe();val viewport=Probe()
        ComposeHost(writer,400,300).use { host ->
            host.mount {
                MaterialTheme(colorScheme=lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        scope=rememberCoroutineScope()
                        scroll=rememberScrollState()
                        val scrollState=remember(generation){SearchBarScrollState(-Float.MAX_VALUE,0f,0f)}
                        behavior=SearchBarDefaults.enterAlwaysSearchBarScrollBehavior(scrollState=scrollState,canScroll={enabled},reverseLayout=c.reverse)
                        Scaffold(modifier=Modifier.nestedScroll(behavior.nestedScrollConnection),contentWindowInsets=WindowInsets(0),
                            topBar={AppBarWithSearch(state=rememberSearchBarState(),modifier=bar.modifier(),scrollBehavior=behavior,
                                windowInsets=WindowInsets(0),inputField={Box((if(c.stretch)Modifier.fillMaxWidth()else Modifier.width(240.dp)).height(56.dp).background(Color.Magenta))})}) { padding ->
                            Box(Modifier.fillMaxSize().padding(padding).consumeWindowInsets(padding)) {
                                Column(viewport.modifier().fillMaxSize().nestedScroll(childConnection,dispatcher).verticalScroll(scroll)) {
                                    repeat(20){i->Box(Modifier.fillMaxWidth().height(40.dp).background(if(i%2==0)Color.Cyan else Color.Yellow))}
                                }
                            }
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) { val state=behavior.scrollState
                writer.submit(host.snapshot(id,id,listOf(
                    ProbePoint("bar","search-app-bar",null,bar.geometry()),
                    ProbePoint("viewport","scroll-viewport",null,viewport.geometry()),
                    ProbePoint("scroll","scroll-state",null,listOf(number("offset",(state.scrollOffset/ComposeHost.RenderScale).toDouble(),"dip"),
                        number("limit",(state.scrollOffsetLimit/ComposeHost.RenderScale).toDouble(),"dip"),
                        number("content",(state.contentOffset/ComposeHost.RenderScale).toDouble(),"dip"),number("body",scroll.value.toDouble()/ComposeHost.RenderScale,"dip"))))))
            }
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,change:()->Unit){advance(time);change();writer.record(time,"scroll",id);host.scheduler.runCurrent();host.draw();sample(id)}
            fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            fun delta(value:Float){
                val available=Offset(0f,-value*ComposeHost.RenderScale)
                val pre=dispatcher.dispatchPreScroll(available,NestedScrollSource.UserInput)
                val afterPre=available-pre
                val used=Offset(0f,-scroll.dispatchRawDelta(-afterPre.y))
                dispatcher.dispatchPostScroll(used,afterPre-used,NestedScrollSource.UserInput)
            }
            sample("initial")
            change(1000,"up-20"){delta(20f)};at(16000)
            if(c.action=="snap"){
                change(17000,"settle"){scope.launch {behavior.nestedScrollConnection.onPostFling(Velocity.Zero,Velocity.Zero)}}
                at(32000,48000,64000,96000,160000,320000,640000);return
            }
            if(c.action=="disable")change(17000,"disable"){enabled=false}
            if(c.action=="replace")change(17000,"replace"){generation++}
            change(33000,"cross-limit"){delta(100f)};at(48000)
            change(65000,"body-scroll"){delta(40f)};at(80000)
            change(97000,"down-12"){delta(-12f)};at(112000)
            change(129000,"down-to-start"){delta(-180f)};at(144000,320000)
        }
    }
}

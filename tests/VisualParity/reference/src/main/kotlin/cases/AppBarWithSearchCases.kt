@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.nestedscroll.NestedScrollSource
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import androidx.compose.foundation.shape.RoundedCornerShape
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class SearchAppBarConfig(val navigation:Boolean=true,val actions:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,
    val width:Int=600,val inputWidth:Int=360,val inputHeight:Int=56,val transparent:Boolean=false,val insets:Boolean=false,val consumed:Boolean=false,
    val padding:Boolean=false,val scrolled:Boolean=false,val offset:Float=0f,val action:String="none",val reverse:Boolean=false,val enabled:Boolean=true,
    val elevation:Double=0.0,val custom:Boolean=false)

@ParitySuite("search-app-bar")
class AppBarWithSearchCases {
    @ParityScenario("expansion") suspend fun expansion(writer:CaptureWriter) = ExpandedSearchBarCases().capture(writer)

    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<SearchAppBarConfig>()
        val scrollState=SearchBarScrollState(-64f*ComposeHost.RenderScale,c.offset*ComposeHost.RenderScale,if(c.scrolled)-64f*ComposeHost.RenderScale else 0f)
        lateinit var state:SearchBarState
        lateinit var behavior:SearchBarScrollBehavior
        lateinit var scope:CoroutineScope
        var height by mutableStateOf(c.inputHeight)
        var enabled by mutableStateOf(c.enabled)
        var consumed=0f;var remaining=0f
        val bar=Probe();val input=Probe();val nav=Probe();val action=Probe()
        val empty=PlatformContext.Empty()
        // Empty's pixel slop does not scale with density. Supply the same 18 DIP platform
        // configuration as the existing AppBar/Swipe suites; mouse slop is its 1/144 ratio.
        val context=object:PlatformContext by empty {
            override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
        ComposeHost(writer,c.width,240,context).use { host ->
            host.mount {
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        state=rememberSearchBarState()
                        scope=rememberCoroutineScope()
                        behavior=SearchBarDefaults.enterAlwaysSearchBarScrollBehavior(scrollState=scrollState,canScroll={enabled},reverseLayout=c.reverse)
                        val colors=SearchBarDefaults.appBarWithSearchColors(
                            searchBarColors=if(c.custom)SearchBarDefaults.colors(containerColor=Color(0xFF008000))else SearchBarDefaults.colors(),
                            appBarContainerColor=if(c.custom)Color.Yellow else if(c.transparent)Color.Transparent else MaterialTheme.colorScheme.surface,
                            appBarNavigationIconColor=if(c.custom)Color.Red else MaterialTheme.colorScheme.onSurface,
                            appBarActionIconColor=if(c.custom)Color.Blue else MaterialTheme.colorScheme.onSurfaceVariant)
                        Box(Modifier.fillMaxSize().background(Color.White).then(if(c.consumed)Modifier.consumeWindowInsets(PaddingValues(start=4.dp,top=8.dp,end=6.dp,bottom=10.dp))else Modifier)) {
                            AppBarWithSearch(state=state,inputField={Marker(input,c.inputWidth,height,true)},modifier=bar.modifier(),
                                navigationIcon=if(c.navigation){{Marker(nav,48,48)}}else null,
                                actions=if(c.actions){{Marker(action,48,48)}}else null,
                                colors=colors,shape=if(c.custom)RoundedCornerShape(12.dp)else SearchBarDefaults.inputFieldShape,
                                tonalElevation=c.elevation.toFloat().dp,shadowElevation=c.elevation.toFloat().dp,
                                contentPadding=if(c.padding)PaddingValues(start=7.dp,top=3.dp,end=11.dp,bottom=5.dp)else SearchBarDefaults.AppBarContentPadding,
                                windowInsets=if(c.insets)WindowInsets(left=12.dp,top=20.dp,right=24.dp,bottom=16.dp)else WindowInsets(0),scrollBehavior=behavior)
                        }
                    }
                }
            };host.frame(-1000);host.frame(0)
            fun sample(id:String) {
                val coords=state.collapsedCoords
                val search=Probe().also { it.coordinates=coords }
                val points=mutableListOf(ProbePoint("app-bar","search-app-bar",null,bar.geometry()),
                    ProbePoint("search","collapsed-search-bar","app-bar",search.geometry()),
                    ProbePoint("input","input-slot","search",input.geometry()),
                    ProbePoint("scroll","scroll-state",null,listOf(number("offset",(scrollState.scrollOffset/ComposeHost.RenderScale).toDouble(),"dip"),
                        number("limit",(scrollState.scrollOffsetLimit/ComposeHost.RenderScale).toDouble(),"dip"),number("content",(scrollState.contentOffset/ComposeHost.RenderScale).toDouble(),"dip"),
                        number("consumed",consumed.toDouble(),"dip"),number("remaining",remaining.toDouble(),"dip/s"))))
                if(c.navigation)points.add(ProbePoint("navigation","navigation-slot","app-bar",nav.geometry()))
                if(c.actions)points.add(ProbePoint("action","action-slot","app-bar",action.geometry()))
                writer.submit(host.snapshot(id,id,points))
            }
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,change:()->Unit){advance(time);change();writer.record(time,"mutation",id);host.scheduler.runCurrent();host.draw();sample(id)}
            fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
            fun pre(y:Float):Float=behavior.nestedScrollConnection.onPreScroll(Offset(0f,y*ComposeHost.RenderScale),NestedScrollSource.UserInput).y/ComposeHost.RenderScale
            fun post(used:Float,y:Float):Float=behavior.nestedScrollConnection.onPostScroll(Offset(0f,used*ComposeHost.RenderScale),Offset(0f,y*ComposeHost.RenderScale),NestedScrollSource.UserInput).y/ComposeHost.RenderScale
            sample("initial")
            if(c.action=="none")return
            if(c.action=="scroll") {
                change(1000,"pre-up"){consumed=pre(-20f)};at(16000)
                change(17000,"post-up"){consumed=post(-15f,-5f)};at(32000,64000,128000,320000)
                change(321000,"pre-down"){consumed=pre(12f)};at(336000)
                change(337000,"post-down"){consumed=post(0f,80f)};at(352000,512000);return
            }
            if(c.action=="resize") {
                change(1000,"offset"){scrollState.scrollOffset=-32f*ComposeHost.RenderScale};at(16000)
                change(17000,"resize"){height=96};at(32000,64000);return
            }
            if(c.action=="color-reverse") {
                change(1000,"overlap"){scrollState.contentOffset=-64f*ComposeHost.RenderScale};at(16000,32000,64000)
                change(65000,"reverse-color"){scrollState.contentOffset=0f};at(80000,96000,128000,256000,512000);return
            }
            if(c.action in listOf("drag","disable-drag","disable-drag-settle")) {
                change(1000,"down"){host.input("move",Offset(300f,32f));host.input("down",Offset(300f,32f))}
                change(17000,"drag"){host.input("move",Offset(300f,12f))}
                if(c.action=="disable-drag")change(18000,"disable"){enabled=false}
                at(32000)
                change(33000,"up"){host.input("up",Offset(300f,12f))};at(48000)
                if(c.action=="disable-drag-settle")change(49000,"disable"){enabled=false}
                at(64000,96000,160000,320000,640000);return
            }
            change(1000,"offset"){scrollState.scrollOffset=-24f*ComposeHost.RenderScale};at(16000)
            change(17000,"settle"){scope.launch {
                val v=if(c.action=="fling-up")-600f else if(c.action=="fling-down")600f else 0f
                remaining=behavior.nestedScrollConnection.onPostFling(Velocity.Zero,Velocity(0f,v*ComposeHost.RenderScale)).y/ComposeHost.RenderScale
            }}
            if(c.action=="disable-settle") {
                at(32000,48000)
                change(49000,"disable"){enabled=false}
                at(64000,96000,160000,320000,640000,1024000)
                return
            }
            at(32000,48000,64000,96000,160000,320000,640000,1024000)
        }
    }
    @Composable private fun Marker(probe:Probe,width:Int,height:Int,input:Boolean=false) {
        Box(probe.modifier().width(width.dp).height(height.dp),contentAlignment=if(input)Alignment.CenterStart else Alignment.Center) {
            Box(Modifier.then(if(input)Modifier.padding(start=16.dp)else Modifier).size(24.dp).background(if(input)Color.Magenta else LocalContentColor.current))
        }
    }
}

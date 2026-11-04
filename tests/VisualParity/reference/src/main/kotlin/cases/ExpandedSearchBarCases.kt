@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.ui.ExperimentalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.animation.core.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.navigationevent.BackNavigationEventInput
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class ExpandedSearchBarConfig(val expanded:Boolean=true,val dark:Boolean=false,val rtl:Boolean=false,
    val hostWidth:Int=420,val hostHeight:Int=600,val contentHeight:Double=40.0,val inputWidth:Double=360.0,
    val inputHeight:Double=56.0,val custom:Boolean=false,val action:String="none",val motion:String="default",val anchor:Boolean=true,
    val tonalElevation:Double=0.0,val shadowElevation:Double=0.0)

@ParitySuite("expanded-search-bar")
class ExpandedSearchBarCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<ExpandedSearchBarConfig>()
        val empty=PlatformContext.Empty()
        val context=object:PlatformContext by empty {
            override val windowInfo=object:WindowInfo by empty.windowInfo {
                override val containerSize=IntSize(c.hostWidth*ComposeHost.RenderScale,c.hostHeight*ComposeHost.RenderScale)
                override val isWindowFocused=true
            }
        }
        lateinit var state:SearchBarState
        lateinit var scope:CoroutineScope
        var x by mutableStateOf(16); var y by mutableStateOf(24)
        var inputWidth by mutableStateOf(c.inputWidth)
        var contentHeight by mutableStateOf(c.contentHeight)
        val bar=Probe(); val popup=Probe(); val input=Probe(); val results=Probe()
        ComposeHost(writer,c.hostWidth,c.hostHeight,context).use { host ->
            val navigation=BackNavigationEventInput()
            val dispatcher=context.architectureComponentsOwner.navigationEventDispatcherOwner.navigationEventDispatcher
            dispatcher.addInput(navigation)
            try {
                host.mount {
                    MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme(),motionScheme=MotionScheme.expressive()) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            val initial=if(c.expanded)SearchBarValue.Expanded else SearchBarValue.Collapsed
                            val defaultState=rememberSearchBarState(initial)
                            state=remember { when(c.motion) {
                                "tween"->SearchBarState(initial,tween(400,50,LinearEasing),tween(300,easing=LinearEasing),tween(200,100,LinearEasing),tween(150,easing=LinearEasing))
                                "overshoot"->SearchBarState(initial,spring(.4f,120f),spring(.4f,120f))
                                "content"->SearchBarState(initial,snap(),snap(),tween(1000,easing=LinearEasing),tween(1000,easing=LinearEasing))
                                else->defaultState
                            } }
                            scope=rememberCoroutineScope()
                            val colors=if(c.custom)SearchBarDefaults.colors(containerColor=Color.Yellow,dividerColor=Color.Red)else SearchBarDefaults.colors()
                            Box(Modifier.fillMaxSize().background(Color.White)) {
                                if(c.anchor)Box(Modifier.padding(start=x.dp,top=y.dp)) {
                                    SearchBar(state=state,inputField={ Marker(Modifier,inputWidth,c.inputHeight) },modifier=bar.modifier(),
                                        shape=if(c.custom)RoundedCornerShape(12.dp)else SearchBarDefaults.inputFieldShape,colors=colors,
                                        tonalElevation=c.tonalElevation.toFloat().dp,shadowElevation=c.shadowElevation.toFloat().dp)
                                }
                                ExpandedDockedSearchBar(state=state,inputField={ Marker(input.modifier(),c.inputWidth,c.inputHeight) },modifier=popup.modifier(),
                                    shape=if(c.custom)RoundedCornerShape(12.dp)else SearchBarDefaults.dockedShape,colors=colors,
                                    tonalElevation=c.tonalElevation.toFloat().dp,shadowElevation=c.shadowElevation.toFloat().dp) {
                                    Box(results.modifier().fillMaxWidth().height(contentHeight.toFloat().dp).background(Color.Blue))
                                }
                            }
                        }
                    }
                }; host.frame(-1000); host.frame(0)
                fun sample(id:String) {
                    val presented=popup.coordinates?.isAttached==true
                    val points=mutableListOf(ProbePoint("state","search-state",null,listOf(
                        number("progress",state.progress.toDouble(),"fraction",definition="search-state.progress"),
                        number("content-progress",state.contentProgress.toDouble(),"fraction",definition="search-state.content-progress"),
                        text("current",state.currentValue.toString(),definition="search-state.current"),
                        text("target",state.targetValue.toString(),definition="search-state.target"),
                        flag("animating",state.isAnimating,definition="search-state.animating"),
                        flag("presented",presented,definition="search-state.presented"))),
                        ProbePoint("bar","collapsed-search-bar","state",if(c.anchor)bar.geometry()else emptyList()))
                    if(presented) {
                        points.add(ProbePoint("popup","expanded-search-bar","state",popup.geometry()))
                        points.add(ProbePoint("input","input-slot","popup",input.geometry()))
                        points.add(ProbePoint("results","results-slot","popup",results.geometry()))
                    }
                    writer.submit(host.snapshot(id,id,points))
                }
                var nextFrame=16000L
                fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
                fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.scheduler.runCurrent();host.draw();sample(id)}
                fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
                sample("initial")
                if(c.action=="none")return
                if(c.action=="anchor") {
                    change(1000,"move"){x=32;y=48};at(16000)
                    change(17000,"resize-input"){inputWidth=280.0};at(32000);return
                }
                if(c.action=="snap") {
                    listOf(1000L to .02f,17000L to .021f,33000L to .5f,49000L to 1.2f,65000L to -.1f).forEach{(time,value)->
                        change(time,"snap-$time"){scope.launch{state.snapTo(value)}};at(time+15000)
                    };return
                }
                if(c.action in setOf("escape","outside")) {
                    change(1000,c.action) {
                        if(c.action=="escape") {
                            val event=KeyEvent(Key.Escape,KeyEventType.KeyDown)
                            if(!host.scene.sendKeyEvent(event))navigation.onKeyEvent(event)
                        } else {
                            val point=Offset(c.hostWidth-4f,c.hostHeight-4f)
                            host.input("move",point);host.input("down",point)
                        }
                    };at(16000,32000,64000,128000,256000,512000,1024000);return
                }
                change(1000,"expand"){scope.launch{state.animateToExpanded()}}
                at(16000,32000,64000,96000)
                if(c.action=="reverse")change(97000,"reverse"){scope.launch{state.animateToCollapsed()}}
                if(c.action=="content")change(97000,"grow-results"){contentHeight=700.0}
                at(128000,192000,320000,512000,1024000)
                change(1025000,"collapse"){scope.launch{state.animateToCollapsed()}}
                at(1040000,1088000,1152000,1280000,1536000,2048000)
            } finally {dispatcher.removeInput(navigation)}
        }
    }
    @Composable private fun Marker(modifier:Modifier,width:Double,height:Double) {
        Box(modifier.width(width.toFloat().dp).height(height.toFloat().dp),contentAlignment=Alignment.CenterStart) {
            Box(Modifier.padding(horizontal=16.dp).size(24.dp).background(Color.Magenta))
        }
    }
}

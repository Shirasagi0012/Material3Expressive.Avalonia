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
    val tonalElevation:Double=0.0,val shadowElevation:Double=0.0,val gap:Boolean=false,val noScrim:Boolean=false,val fullScreen:Boolean=false,val insets:Boolean=false,val contained:Boolean=false,val filledInput:Boolean=false,val appBar:Boolean=false,val shape:String="none",val dropdownShape:String="none",val stableShapeDefaults:Boolean=false)

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
        var anchor by mutableStateOf(c.anchor)
        var shapeKind by mutableStateOf(c.shape)
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
                            val defaultState=if(c.motion=="gap-default")rememberSearchBarWithGapState(initial)else if(c.contained)rememberContainedSearchBarState(initial)else rememberSearchBarState(initial)
                            state=remember { when(c.motion) {
                                "tween"->SearchBarState(initial,tween(400,50,LinearEasing),tween(300,easing=LinearEasing),tween(200,100,LinearEasing),tween(150,easing=LinearEasing))
                                "overshoot"->SearchBarState(initial,spring(.4f,120f),spring(.4f,120f))
                                "content"->SearchBarState(initial,snap(),snap(),tween(1000,easing=LinearEasing),tween(1000,easing=LinearEasing))
                                else->defaultState
                            } }
                            scope=rememberCoroutineScope()
                            // Keep both producer forms as a diagnostic control. Pre-reading this
                            // composable getter did not remove the default-to-custom lifetime gap.
                            val defaultInputShape=if(c.stableShapeDefaults)SearchBarDefaults.inputFieldShape else null
                            val customShape=if(shapeKind!="none")carouselTestShape(shapeKind)else null
                            val colors=if(c.custom)SearchBarDefaults.colors(containerColor=Color.Yellow,dividerColor=Color.Red)else SearchBarDefaults.colors()
                            Box(Modifier.fillMaxSize().background(Color.White)) {
                                if(anchor)Box(Modifier.padding(start=x.dp,top=y.dp)) {
                                    if(c.appBar) AppBarWithSearch(
                                        state=state,inputField={ Marker(Modifier,inputWidth,c.inputHeight) },
                                        shape=customShape ?: (defaultInputShape ?: SearchBarDefaults.inputFieldShape),
                                        navigationIcon={Box(Modifier.size(48.dp).background(Color.Red))},
                                        actions={Box(Modifier.size(48.dp).background(Color.Red))},windowInsets=WindowInsets(0)
                                    ) else SearchBar(state=state,inputField={ Marker(Modifier,inputWidth,c.inputHeight) },modifier=bar.modifier(),
                                        shape=customShape ?: if(c.custom)RoundedCornerShape(12.dp)else (defaultInputShape ?: SearchBarDefaults.inputFieldShape),colors=colors,
                                        tonalElevation=c.tonalElevation.toFloat().dp,shadowElevation=c.shadowElevation.toFloat().dp)
                                }
                                if(c.contained) ExpandedFullScreenContainedSearchBar(state=state,inputField={ Marker(input.modifier(),c.inputWidth,c.inputHeight,c.filledInput) },modifier=popup.modifier(),
                                    collapsedShape=customShape ?: if(c.custom)RoundedCornerShape(12.dp)else (defaultInputShape ?: SearchBarDefaults.inputFieldShape),colors=if(c.custom)colors else SearchBarDefaults.containedColors(state),
                                    windowInsets={if(c.insets)WindowInsets(left=12.dp,top=20.dp,right=24.dp,bottom=16.dp)else WindowInsets(0)},
                                    tonalElevation=c.tonalElevation.toFloat().dp,shadowElevation=c.shadowElevation.toFloat().dp) {
                                    Box(results.modifier().fillMaxWidth().height(contentHeight.toFloat().dp).background(Color.Blue))
                                } else if(c.fullScreen) ExpandedFullScreenSearchBar(state=state,inputField={ Marker(input.modifier(),c.inputWidth,c.inputHeight,c.filledInput) },modifier=popup.modifier(),
                                    collapsedShape=customShape ?: if(c.custom)RoundedCornerShape(12.dp)else (defaultInputShape ?: SearchBarDefaults.inputFieldShape),colors=colors,
                                    windowInsets={if(c.insets)WindowInsets(left=12.dp,top=20.dp,right=24.dp,bottom=16.dp)else WindowInsets(0)},
                                    tonalElevation=c.tonalElevation.toFloat().dp,shadowElevation=c.shadowElevation.toFloat().dp) {
                                    Box(results.modifier().fillMaxWidth().height(contentHeight.toFloat().dp).background(Color.Blue))
                                } else if(c.gap) ExpandedDockedSearchBarWithGap(state=state,inputField={ Marker(input.modifier(),c.inputWidth,c.inputHeight,c.filledInput) },modifier=popup.modifier(),
                                    shape=customShape ?: if(c.custom)RoundedCornerShape(12.dp)else SearchBarDefaults.dockedShape,colors=colors,
                                    dropdownShape=if(c.dropdownShape!="none")carouselTestShape(c.dropdownShape)else if(c.custom)RoundedCornerShape(4.dp)else SearchBarDefaults.dockedDropdownShape,
                                    dropdownGapSize=if(c.custom)8.dp else SearchBarDefaults.dockedDropdownGapSize,
                                    dropdownScrimColor=if(c.custom)Color(0x400000FF)else if(c.noScrim)Color.Transparent else SearchBarDefaults.dockedDropdownScrimColor,
                                    tonalElevation=c.tonalElevation.toFloat().dp,shadowElevation=c.shadowElevation.toFloat().dp) {
                                    Box(results.modifier().fillMaxWidth().height(contentHeight.toFloat().dp).background(Color.Blue))
                                } else ExpandedDockedSearchBar(state=state,inputField={ Marker(input.modifier(),c.inputWidth,c.inputHeight,c.filledInput) },modifier=popup.modifier(),
                                    shape=customShape ?: if(c.custom)RoundedCornerShape(12.dp)else SearchBarDefaults.dockedShape,colors=colors,
                                    tonalElevation=c.tonalElevation.toFloat().dp,shadowElevation=c.shadowElevation.toFloat().dp) {
                                    Box(results.modifier().fillMaxWidth().height(contentHeight.toFloat().dp).background(Color.Blue))
                                }
                            }
                        }
                    }
                }; host.frame(-1000); host.frame(0)
                fun sample(id:String) {
                    if(c.appBar) bar.coordinates=state.collapsedCoords
                    val presented=popup.coordinates?.isAttached==true
                    val points=mutableListOf(ProbePoint("state","search-state",null,listOf(
                        number("progress",state.progress.toDouble(),"fraction",definition="search-state.progress"),
                        number("content-progress",state.contentProgress.toDouble(),"fraction",definition="search-state.content-progress"),
                        text("current",state.currentValue.toString(),definition="search-state.current"),
                        text("target",state.targetValue.toString(),definition="search-state.target"),
                        flag("animating",state.isAnimating,definition="search-state.animating"),
                        flag("presented",presented,definition="search-state.presented"))),
                        ProbePoint("bar","collapsed-search-bar","state",if(bar.coordinates?.isAttached==true)bar.geometry()else emptyList()))
                    if(c.appBar) points.add(ProbePoint("app-bar-state","search-app-bar-state","state",listOf(
                        flag("full-screen",state.expandsToFullScreen),
                        flag("search-visible",!state.expandsToFullScreen || state.currentValue==SearchBarValue.Collapsed || state.targetValue==SearchBarValue.Expanded,
                            definition="search.visibility.requested", source="AppBarWithSearch isVisible expression (not retained raster alpha)"))))
                    if(presented) {
                        points.add(ProbePoint("popup","expanded-search-bar","state",popup.geometry()))
                        points.add(ProbePoint("input","input-slot","popup",input.geometry()))
                        if(!c.gap || results.coordinates?.isAttached==true) points.add(ProbePoint("results","results-slot","popup",results.geometry()))
                    }
                    writer.submit(host.snapshot(id,id,points))
                }
                var nextFrame=16000L
                fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
                fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.scheduler.runCurrent();host.draw();sample(id)}
                fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
                sample("initial")
                if(c.action=="none")return
                if(c.action=="replace-shape") {
                    change(1000,"partial"){scope.launch{state.snapTo(.25f)}};at(16000)
                    change(17000,"replace-shape"){shapeKind="percent"};at(32000)
                    change(33000,"half"){scope.launch{state.snapTo(.5f)}};at(48000)
                    change(49000,"reverse"){scope.launch{state.snapTo(.25f)}};at(64000)
                    change(65000,"close"){scope.launch{state.snapTo(0f)}};at(80000)
                    change(81000,"reopen"){scope.launch{state.snapTo(.25f)}};at(96000)
                    return
                }

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
                    };at(16000)
                    if(c.gap && c.action=="outside")change(17000,"outside-release"){host.input("up",Offset(c.hostWidth-4f,c.hostHeight-4f))}
                    at(32000,64000,128000,256000,512000,1024000);return
                }
                change(1000,"expand"){scope.launch{state.animateToExpanded()}}
                at(16000,32000,64000,96000)
                if(c.action=="reverse")change(97000,"reverse"){scope.launch{state.animateToCollapsed()}}
                if(c.action=="remove-anchor")change(97000,"remove-anchor"){anchor=false}
                if(c.action=="content")change(97000,"grow-results"){contentHeight=700.0}
                at(128000,192000,320000,512000,1024000)
                change(1025000,"collapse"){scope.launch{state.animateToCollapsed()}}
                at(1040000,1088000,1152000,1280000,1536000,2048000)
            } finally {dispatcher.removeInput(navigation)}
        }
    }
    @Composable private fun Marker(modifier:Modifier,width:Double,height:Double,filled:Boolean=false) {
        Box(modifier.width(width.toFloat().dp).height(height.toFloat().dp).background(if(filled)Color(0xFF008000)else Color.Transparent),contentAlignment=Alignment.CenterStart) {
            Box(Modifier.padding(horizontal=16.dp).size(24.dp).background(Color.Magenta))
        }
    }
}

@ParitySuite("search-bar-gap")
class SearchBarGapCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) = ExpandedSearchBarCases().capture(writer)
}

@ParitySuite("full-screen-search-bar")
class FullScreenSearchBarCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) = ExpandedSearchBarCases().capture(writer)
}

@ParitySuite("contained-search-bar")
class ContainedSearchBarCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) = ExpandedSearchBarCases().capture(writer)
}

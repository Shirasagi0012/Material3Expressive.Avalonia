@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.ui.ExperimentalComposeUiApi::class)
@file:Suppress("INVISIBLE_MEMBER", "INVISIBLE_REFERENCE")
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.navigationevent.BackNavigationEventInput
import androidx.compose.ui.platform.*
import androidx.compose.ui.unit.*
import kotlinx.serialization.Serializable

@Serializable data class DockedSearchBarConfig(val expanded:Boolean=false,val dark:Boolean=false,val rtl:Boolean=false,
    val hostWidth:Int=420,val hostHeight:Int=600,val contentHeight:Double=40.0,val action:String="none")

@ParitySuite("docked-search-bar")
class DockedSearchBarCases {
    @ParityScenario("presentation") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<DockedSearchBarConfig>()
        val empty=PlatformContext.Empty()
        val context=object:PlatformContext by empty {
            override val windowInfo=object:WindowInfo by empty.windowInfo {
                override val containerSize=IntSize(c.hostWidth*ComposeHost.RenderScale,c.hostHeight*ComposeHost.RenderScale)
                override val isWindowFocused=true
            }
        }
        var expanded by mutableStateOf(c.expanded)
        var attached by mutableStateOf(true)
        var contentHeight by mutableStateOf(c.contentHeight)
        var requests=0
        val bar=Probe(); val input=Probe(); val content=Probe()
        ComposeHost(writer,c.hostWidth,c.hostHeight,context).use { host ->
            val navigation=BackNavigationEventInput()
            val dispatcher=context.architectureComponentsOwner.navigationEventDispatcherOwner.navigationEventDispatcher
            dispatcher.addInput(navigation)
            try {
                host.mount {
                    MaterialTheme(colorScheme=if(c.dark)darkColorScheme()else lightColorScheme()) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                            CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                                if(attached) DockedSearchBar(inputField={
                                    Box(input.modifier().fillMaxWidth().height(56.dp),contentAlignment=Alignment.CenterStart) {
                                        Box(Modifier.padding(horizontal=16.dp).size(24.dp).background(Color.Magenta))
                                    }
                                },expanded=expanded,onExpandedChange={requests++;expanded=it},modifier=bar.modifier()) {
                                    Box(content.modifier().fillMaxWidth().height(contentHeight.toFloat().dp).background(Color.Blue))
                                }
                            }
                        }
                    }
                }; host.frame(0)
                fun sample(id:String) {
                    val mounted=bar.coordinates?.isAttached==true
                    val present=content.coordinates?.isAttached==true
                    val points=mutableListOf(ProbePoint("state","docked-search-state",null,listOf(
                        flag("attached",mounted,definition="search-bar.attached"),
                        flag("expanded",expanded,definition="search-bar.expanded"),
                        flag("contentPresent",present,definition="search-bar.content-present"),
                        number("collapseRequests",requests.toDouble(),"count",definition="search-bar.collapse-requests"))))
                    if(mounted) {
                        points.add(ProbePoint("bar","docked-search-bar","state",bar.geometry()))
                        points.add(ProbePoint("input","input-slot","bar",input.geometry()))
                        if(present)points.add(ProbePoint("content","results-slot","bar",content.geometry()))
                    }
                    writer.submit(host.snapshot(id,id,points))
                }
                var nextFrame=16000L
                fun advance(time:Long){while(nextFrame<=time){host.frame(nextFrame);nextFrame+=16000};host.advanceTo(time)}
                fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"mutation",id);host.draw();sample(id)}
                fun at(vararg times:Long){times.forEach{advance(it);host.draw();sample("time-$it")}}
                sample("initial")
                if(c.action=="none")return
                if(c.action=="escape") {
                    change(1000,"escape-down") {
                        val event=KeyEvent(Key.Escape,KeyEventType.KeyDown)
                        if(!host.scene.sendKeyEvent(event))navigation.onKeyEvent(event)
                        host.scheduler.runCurrent()
                    }
                    at(16000,96000,112000,208000,416000,640000);return
                }
                change(1000,"expand"){expanded=true}
                at(16000,96000,112000,208000)
                if(c.action=="reverse")change(209000,"reverse"){expanded=false}
                if(c.action=="content")change(209000,"tall-results"){contentHeight=700.0}
                if(c.action=="detach") {
                    change(209000,"detach"){attached=false};at(224000)
                    change(225000,"reattach"){attached=true}
                }
                at(320000,640000,736000)
                change(737000,"collapse"){expanded=false}
                at(752000,832000,848000,944000,1152000,1280000)
            } finally {dispatcher.removeInput(navigation)}
        }
    }
}

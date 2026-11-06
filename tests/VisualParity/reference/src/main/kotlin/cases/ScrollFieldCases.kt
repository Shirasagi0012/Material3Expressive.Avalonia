@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.pager.PagerState
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.LocalViewConfiguration
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.PointerType
import androidx.compose.ui.input.key.*
import androidx.compose.ui.focus.*
import androidx.compose.ui.unit.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import kotlinx.serialization.Serializable

@Serializable
data class ScrollFieldConfig(val count:Int=7,val index:Int=3,val height:Double=200.0,val width:Double=120.0,
    val rtl:Boolean=false,val dark:Boolean=false,val action:String="none")

@ParitySuite("scroll-field")
class ScrollFieldCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer)
    suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<ScrollFieldConfig>()
        ComposeHost(writer,200,280).use { host ->
            val d=ComposeHost.RenderScale.toFloat()
            val desktopViewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration {
                override val touchSlop=18*d
            }
            var replace by mutableStateOf(false)
            lateinit var state:ScrollFieldState
            lateinit var scope:CoroutineScope
            val viewport=Probe()
            val selected=Probe()
            val requester=FocusRequester()
            host.mount {
                scope=rememberCoroutineScope()
                state=key(replace) { rememberScrollFieldState(if(replace)5 else c.count,if(replace)2 else c.index) }
                MaterialTheme(colorScheme=if(c.dark)darkColorScheme() else lightColorScheme()) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(24.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,
                            LocalContentColor provides MaterialTheme.colorScheme.onSurface,
                            LocalViewConfiguration provides desktopViewConfiguration) {
                            ScrollField(state,contentDescription="Options",modifier=Modifier.size(c.width.toFloat().dp,c.height.toFloat().dp).focusRequester(requester).then(viewport.modifier())) { index,isSelected ->
                                Canvas(Modifier.size(32.dp,24.dp).then(if(isSelected)selected.modifier() else Modifier)) {
                                    drawRect(if(isSelected)Color(0xFF000080) else Color(0xFF6495ED))
                                    drawRect(Color(0xFFFFD700),size=Size((4+index%7*4)*density,8*density))
                                }
                            }
                        }
                    }
                }
            }
            host.frame(0)
            fun sample(id:String) {
                val y=(checkNotNull(selected.coordinates).positionInRoot().y-checkNotNull(viewport.coordinates).positionInRoot().y)/ComposeHost.RenderScale.toFloat()
                writer.submit(host.snapshot(id,id,listOf(
                    ProbePoint("state","state",null,listOf(
                        number("selected",state.selectedOption.toDouble(),"index",definition="scrollField.selected"),
                        number("target",state.targetOption.toDouble(),"index",definition="scrollField.target"),
                        flag("scrolling",state.isScrollInProgress,definition="scrollField.scrolling"))),
                    ProbePoint("selected","item",null,listOf(number("y",y.toDouble(),"dip",definition="scrollField.selected.y"))))))
            }
            sample("initial")
            if(c.action=="none")return
            if(c.action.startsWith("key-") || c.action.startsWith("mouse-")) {
                for(time in 16000L..960000L step 16000L) {
                    host.advanceTo(time)
                    if(c.action.startsWith("key-") && (time==16000L || c.action=="key-repeat" && (time==32000L || time==48000L))) {
                        requester.requestFocus()
                        val key=if(c.action=="key-up")Key.DirectionUp else Key.DirectionDown
                        host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown));host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp))
                        host.scheduler.runCurrent();writer.record(time,"input","key $key")
                    }
                    if(c.action.startsWith("mouse-")) {
                        val y=if(c.action=="mouse-current")124f else 190f
                        if(time==16000L)host.input("move",Offset(84f,y))
                        if(time==32000L)host.input("down",Offset(84f,y))
                        if(time==80000L)host.input("up",Offset(84f,y))
                    }
                    host.frame(time)
                    if(time<=160000L || time%80000L==0L)sample("time-$time")
                }
                return
            }
            if(c.action.startsWith("wheel-")) {
                for(time in 16000L..960000L step 16000L) {
                    host.advanceTo(time)
                    if(time==16000L || c.action=="wheel-burst" && (time==32000L || time==48000L) || c.action=="wheel-reverse" && time==48000L) {
                        var delta=when(c.action) { "wheel-small"->.1f;"wheel-large"->12f;else->3f }
                        if(c.action=="wheel-reverse" && time==48000L)delta=-6f
                        host.scene.sendPointerEvent(PointerEventType.Scroll,Offset(84*d,124*d),scrollDelta=Offset(0f,delta),timeMillis=1000+time/1000)
                        host.scheduler.runCurrent();writer.record(time,"input","wheel $delta")
                    }
                    host.frame(time)
                    if(time<=240000L || time%80000L==0L)sample("time-$time")
                }
                return
            }
            if(c.action.startsWith("touch-")) {
                for(time in 16000L..1280000L step 16000L) {
                    host.advanceTo(time)
                    if(time==64000L && c.action=="touch-navigation") {
                        scope.launch { state.animateScrollToOption(0) }
                        writer.record(time,"mutation","default-priority navigation during user drag")
                    }
                    if(time==64000L && c.action=="touch-request") {
                        state.selectedOption=0
                        writer.record(time,"mutation","selection request during user drag")
                    }
                    var distance=when(time) { 16000L->0.0;32000L->10.0;48000L->36.0;64000L->60.0;80000L->88.0;else->-1.0 }
                    val release=if(c.action=="touch-slow")160000L else 96000L
                    if(distance>=0 || time==release) {
                        if(time==release)distance=88.0
                        if(c.action=="touch-small")distance*=.4
                        if(c.action=="touch-backward")distance=-distance
                        val point=Offset(84*d,((124-distance)*d).toFloat())
                        val type=if(time==16000L)PointerEventType.Press else if(time==release)PointerEventType.Release else PointerEventType.Move
                        host.scene.sendPointerEvent(type,point,timeMillis=1000+time/1000,type=PointerType.Touch)
                        host.scheduler.runCurrent();writer.record(time,"input","touch $type ($point)")
                    }
                    host.frame(time)
                    if(time<=240000L || time%80000L==0L)sample("time-$time")
                }
                return
            }
            host.advanceTo(16000)
            var job:Job?=null
            val pager=ScrollFieldState::class.java.getDeclaredField("pagerState").apply{isAccessible=true}.get(state) as PagerState
            if(c.action=="jump")state.selectedOption=4
            else job=scope.launch {
                when(c.action) {
                    "wrap" -> pager.animateScrollToPage(pager.targetPage+1)
                    "click-wrap" -> state.animateScrollToOption(0)
                    else -> state.animateScrollToOption(if(c.action=="far")50 else 4)
                }
            }
            host.draw();sample("requested")
            for(time in 16000L..960000L step 16000L) {
                if(time==80000L) {
                    host.advanceTo(time)
                    if(c.action=="reverse")job=scope.launch { state.animateScrollToOption(2) }
                    if(c.action=="cancel")state.selectedOption=1
                    if(c.action=="replace")replace=true
                }
                host.frame(time)
                if(time<=160000L || time%160000L==0L)sample("time-$time")
            }
        }
    }
}

@ParitySuite("scroll-field-touch")
class ScrollFieldTouchCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=ScrollFieldCases().capture(writer)
}

@ParitySuite("scroll-field-wheel")
class ScrollFieldWheelCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=ScrollFieldCases().capture(writer)
}

@ParitySuite("scroll-field-navigation")
class ScrollFieldNavigationCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=ScrollFieldCases().capture(writer)
}

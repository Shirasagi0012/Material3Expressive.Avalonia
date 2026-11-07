@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.focusable
import androidx.compose.ui.focus.*
import androidx.compose.ui.input.key.*
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.MutatePriority
import androidx.compose.foundation.pager.PagerState
import androidx.compose.foundation.gestures.TargetedFlingBehavior
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.carousel.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.LocalViewConfiguration
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.PointerType
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.*
import kotlinx.coroutines.launch
import kotlinx.coroutines.CoroutineScope
import kotlinx.serialization.Serializable

@Serializable
data class CarouselConfig(val variant:String="MultiBrowse",val rtl:Boolean=false,val count:Int=8,
    val item:Int=0,val width:Double=320.0,val itemWidth:Double=160.0,val spacing:Double=8.0,
    val before:Double=0.0,val after:Double=0.0,val radius:Double=16.0,val action:String="none",val border:Double=0.0,
    val mode:String="Default",val velocity:Double=0.0,val offset:Double=0.0)

@ParitySuite("carousel")
class CarouselCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter)=capture(writer)
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=capture(writer)
    suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<CarouselConfig>()
        val resize=c.action.startsWith("focus-shrink") || c.action=="focus-grow"
        ComposeHost(writer,if(resize)480 else 400,180).use { host ->
            val d=ComposeHost.RenderScale.toFloat()
            // Fixed Core ComposeSceneMediator.DesktopViewConfiguration converts 18.dp to px.
            // An offscreen scene otherwise inherits the density-independent empty context's 18px.
            val desktopViewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration {
                override val touchSlop=18*d
            }
            val state=CarouselState(currentItem=c.item) { c.count }
            val pager=CarouselState::class.java.getDeclaredField("pagerState").apply { isAccessible=true }.get(state)
            val scrollOffset=PagerState::class.java.getDeclaredMethod("calculateScrollOffset").apply { isAccessible=true }
            val infos=mutableMapOf<Int,CarouselItemDrawInfo>()
            val carouselProbe=Probe()
            val requesters=Array(c.count) { FocusRequester() }
            var focusAccepted=false
            lateinit var scope:CoroutineScope
            lateinit var fling:TargetedFlingBehavior
            var radius by mutableStateOf(c.radius)
            var viewportWidth by mutableStateOf(c.width)
            var enabled by mutableStateOf(c.action!="semantics-disabled" && c.action!="wheel-disabled" && c.action!="key-disabled" && c.action!="focus-disabled" && c.action!="focus-shrink-disabled")
            host.mount {
                scope=rememberCoroutineScope()
                MaterialTheme {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(24.dp)) {
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr,
                            LocalViewConfiguration provides desktopViewConfiguration) {
                            fling=when(c.mode) {
                                "SingleAdvance" -> CarouselDefaults.singleAdvanceFlingBehavior(state)
                                "MultiBrowse" -> CarouselDefaults.multiBrowseFlingBehavior(state)
                                "NoSnap" -> CarouselDefaults.noSnapFlingBehavior()
                                else -> if(c.variant=="Uncontained")CarouselDefaults.noSnapFlingBehavior() else CarouselDefaults.singleAdvanceFlingBehavior(state)
                            }
                            val content:@Composable CarouselItemScope.(Int)->Unit = { index ->
                                infos[index]=carouselItemDrawInfo
                                DisposableEffect(index) { onDispose { infos.remove(index) } }
                                val shape=RoundedCornerShape(radius.toFloat().dp)
                                var itemModifier=Modifier.fillMaxSize()
                                if(c.action.startsWith("key-") || c.action.startsWith("focus-"))itemModifier=itemModifier.focusRequester(requesters[index]).focusable()
                                if(c.border>0)itemModifier=itemModifier.maskBorder(BorderStroke(c.border.toFloat().dp,Color.Black),shape)
                                Canvas(itemModifier.maskClip(shape)) {
                                    drawRect(listOf(Color(0xFF6495ED),Color(0xFFFF7F50),Color(0xFF8FBC8F))[index%3])
                                    drawRect(Color(0xFF000080),size=Size(size.width/3,size.height/2))
                                    drawRect(Color(0xFFFFD700),topLeft=Offset(size.width*.6f,size.height*.5f),size=Size(size.width*.2f,size.height*.4f))
                                }
                            }
                            val modifier=Modifier.size(viewportWidth.toFloat().dp,120.dp).then(carouselProbe.modifier())
                            val padding=PaddingValues(start=c.before.toFloat().dp,end=c.after.toFloat().dp)
                            val itemWidth=if(c.itemWidth==0.0)c.width else c.itemWidth
                            when(c.variant) {
                                "MultiBrowse" -> HorizontalMultiBrowseCarousel(state,itemWidth.toFloat().dp,modifier,itemSpacing=c.spacing.toFloat().dp,contentPadding=padding,flingBehavior=fling,userScrollEnabled=enabled,content=content)
                                "Uncontained" -> HorizontalUncontainedCarousel(state,itemWidth.toFloat().dp,modifier,itemSpacing=c.spacing.toFloat().dp,contentPadding=padding,flingBehavior=fling,userScrollEnabled=enabled,content=content)
                                else -> HorizontalCenteredHeroCarousel(state,modifier,maxItemWidth=if(c.itemWidth==0.0)Dp.Unspecified else itemWidth.toFloat().dp,itemSpacing=c.spacing.toFloat().dp,contentPadding=padding,flingBehavior=fling,userScrollEnabled=enabled,content=content)
                            }
                        }
                    }
                }
            }
            host.frame(0)
            if(c.offset!=0.0) { state.dispatchRawDelta((c.offset*d).toFloat());host.draw() }
            fun semantics(node:Any):List<SemanticsConfiguration> {
                val own=field(node,"_semanticsConfiguration") as SemanticsConfiguration?
                val children=node.javaClass.methods.single { it.name.startsWith("getChildren$")&&it.parameterCount==0 }.invoke(node) as List<*>
                return listOfNotNull(own)+children.flatMap { semantics(checkNotNull(it)) }
            }
            fun paging()=semantics(checkNotNull(field(checkNotNull(carouselProbe.coordinates),"layoutNode")))
                .singleOrNull { it.contains(SemanticsActions.PageRight) }
            fun sample(id:String) {
                val points=mutableListOf(ProbePoint("viewport","viewport",null,listOf(
                    number("currentItem",state.currentItem.toDouble(),"index",definition="carousel.currentItem"),
                    number("scrollOffset",(scrollOffset.invoke(pager) as Int)/d.toDouble(),"dip",definition="carousel.scrollOffset"))))
                if(c.action.startsWith("semantics-"))points+=ProbePoint("actions","semantics","viewport",listOf(
                    flag("pagingAvailable",paging()!=null,definition="carousel.pagingAvailable")))
                if(c.action.startsWith("focus-"))points+=ProbePoint("focus","input","viewport",listOf(
                    flag("accepted",focusAccepted,definition="carousel.focusRequestAccepted")))
                if(c.action.startsWith("focus-shrink") || c.action=="focus-grow")points+=ProbePoint("size","viewport","viewport",listOf(
                    number("width",carouselProbe.coordinates!!.size.width/d.toDouble(),"dip",definition="carousel.viewportWidth")))
                infos[state.currentItem]?.let { info -> points+=ProbePoint("current","item","viewport",listOf(
                    number("size",(info.size/d).toDouble(),"dip",definition="carousel.mask.size"),
                    number("minSize",(info.minSize/d).toDouble(),"dip",definition="carousel.mask.minimum"),
                    number("maxSize",(info.maxSize/d).toDouble(),"dip",definition="carousel.mask.maximum"))) }
                writer.submit(host.snapshot(id,id,points))
            }
            sample("initial")
            if(c.action=="none")return
            if(c.action.startsWith("focus-")) {
                for(time in 16000L..640000L step 16000L) {
                    host.advanceTo(time)
                    if(time==16000L) {
                        val index=when(c.action) { "focus-previous"->c.item-1;"focus-current","focus-disabled"->c.item;else->c.item+1 }
                        focusAccepted=requesters[index].requestFocus();host.scheduler.runCurrent();host.draw()
                        writer.record(time,"input","focus item $index")
                    }
                    if(time==80000L && c.action=="focus-reverse") {
                        focusAccepted=requesters[c.item-1].requestFocus();host.scheduler.runCurrent();host.draw()
                        writer.record(time,"input","focus item ${c.item-1}")
                    }
                    if(time==80000L && c.action=="focus-navigation") { scope.launch { state.scrollToItem(0) };host.scheduler.runCurrent();host.draw() }
                    if(time==80000L && (c.action.startsWith("focus-shrink") || c.action=="focus-grow")) {
                        viewportWidth=if(c.action=="focus-grow")400.0 else 240.0
                        writer.record(time,"input","viewport width $viewportWidth")
                    }
                    host.frame(time)
                    if(time<=160000L || time%80000L==0L)sample("time-$time")
                }
                return
            }
            if(c.action.startsWith("key-")) {
                requesters[c.item].requestFocus();host.scheduler.runCurrent();host.draw()
                for(time in 16000L..320000L step 16000L) {
                    host.advanceTo(time)
                    if(time==16000L || c.action=="key-repeat" && (time==32000L || time==48000L)) {
                        val key=if(c.action=="key-up")Key.PageUp else Key.PageDown
                        host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyDown));host.scene.sendKeyEvent(KeyEvent(key,KeyEventType.KeyUp))
                        host.scheduler.runCurrent();writer.record(time,"input","key $key")
                    }
                    host.frame(time)
                    if(time<=80000L || time%80000L==0L)sample("time-$time")
                }
                return
            }
            if(c.action.startsWith("wheel-")) {
                for(time in 16000L..1600000L step 16000L) {
                    host.advanceTo(time)
                    if(time==16000L || c.action=="wheel-burst" && (time==32000L || time==48000L) || c.action=="wheel-reverse" && time==48000L) {
                        var delta=when(c.action) { "wheel-small"->.1f;"wheel-large"->12f;else->3f }
                        if(c.action=="wheel-reverse" && time==48000L)delta=-6f
                        if(c.action=="wheel-start-bound")delta=-3f
                        if(c.rtl)delta=-delta
                        val vector=if(c.action=="wheel-vertical")Offset(0f,3f) else Offset(delta,0f)
                        host.scene.sendPointerEvent(PointerEventType.Scroll,Offset(184*d,84*d),scrollDelta=vector,timeMillis=1000+time/1000)
                        host.scheduler.runCurrent();writer.record(time,"input","wheel $vector")
                    }
                    host.frame(time)
                    if(time<=240000L || time%80000L==0L)sample("time-$time")
                }
                return
            }
            if(c.action.startsWith("touch-")) {
                for(time in 16000L..1280000L step 16000L) {
                    host.advanceTo(time)
                    var distance=when(time) { 16000L->0;32000L->10;48000L->36;64000L->80;80000L->120;else->-1 }
                    val release=if(c.action=="touch-slow")160000L else 96000L
                    val disable=time==80000L && c.action=="touch-disable"
                    // Commit the enabled configuration before delivering the next input.
                    // draw() alone cannot recompose a changed composable parameter.
                    if(disable) { enabled=false;host.frame(time) }
                    if(distance>=0 || time==release) {
                        if(time==release)distance=120
                        val point=Offset((24+200+(if(c.rtl)distance else -distance))*d,84*d)
                        val type=if(time==16000L)PointerEventType.Press else if(time==release)PointerEventType.Release else PointerEventType.Move
                        host.scene.sendPointerEvent(type,point,timeMillis=1000+time/1000,type=PointerType.Touch)
                        host.scheduler.runCurrent();writer.record(time,"input","touch $type ($point)")
                    }
                    if(disable)host.draw() else host.frame(time)
                    if(time<=240000L || time%80000L==0L)sample("time-$time")
                }
                return
            }
            host.advanceTo(16000)
            if(c.action=="shape")radius=40.0
            else if(c.action=="fling")scope.launch { state.scroll(MutatePriority.Default) { with(fling) { performFling((c.velocity*d).toFloat()) } } }
            else if(c.action.startsWith("semantics-"))paging()?.let { config ->
                val action=if(c.action=="semantics-previous")SemanticsActions.PageLeft else SemanticsActions.PageRight
                check(checkNotNull(config[action].action).invoke())
            }
            else scope.launch { if(c.action=="jump")state.scrollToItem(3) else state.animateScrollToItem(if(c.action=="far")7 else 1) }
            val duration=if(c.action=="fling")1600000L else 640000L
            for(time in 16000L..duration step 16000L) {
                if(time==128000L && c.action=="reverse") {
                    host.advanceTo(time);scope.launch { state.animateScrollToItem(0) }
                }
                host.frame(time)
                if(time<=160000 || time%80000L==0L)sample("time-$time")
            }
        }
    }
}

@ParitySuite("carousel-fling")
class CarouselFlingCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=CarouselCases().capture(writer)
}

@ParitySuite("carousel-touch")
class CarouselTouchCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=CarouselCases().capture(writer)
}

@ParitySuite("carousel-semantics")
class CarouselSemanticsCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=CarouselCases().capture(writer)
}

@ParitySuite("carousel-wheel")
class CarouselWheelCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=CarouselCases().capture(writer)
}

@ParitySuite("carousel-keyboard")
class CarouselKeyboardCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=CarouselCases().capture(writer)
}

@ParitySuite("carousel-focus")
class CarouselFocusCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=CarouselCases().capture(writer)
}

@ParitySuite("carousel-resize")
class CarouselResizeCases {
    @ParityScenario("dynamic") suspend fun dynamic(writer:CaptureWriter)=CarouselCases().capture(writer)
}

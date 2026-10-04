@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.ScrollIndicatorState
import androidx.compose.foundation.gestures.ScrollableState
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.unit.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.input.nestedscroll.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable
data class AppBarConfig(val variant:String="small", val alignment:String="start", val subtitle:Boolean=false,
    val rtl:Boolean=false, val width:Double=360.0, val titleWidth:Double=100.0, val titleHeight:Double=20.0,
    val expandedHeight:Double?=null, val collapsedHeight:Double?=null, val insets:List<Double>?=null, val padding:List<Double>?=null,
    val consumed:List<Double>?=null, val fraction:Double=0.0, val actions:Int=2, val navigation:Boolean=true, val fab:Boolean=false,
    val custom:Boolean=false, val arrangement:String="start", val spacing:Double=8.0, val dark:Boolean=false,
    val elevation:Double=0.0, val behavior:String="exit", val action:String="none", val velocity:Double=0.0, val noSpecs:Boolean=false)

@ParitySuite("appbar")
class AppBarCases {
    @ParityScenario("static")
    suspend fun static(writer:CaptureWriter) {
        ComposeHost(writer,420,300,desktopContext()).use { host ->
            val scene=Scene(writer.readConfig<AppBarConfig>())
            host.mount { scene.Content() }; scene.setFraction(); host.frame(0)
            writer.submit(host.snapshot("initial","initial",scene.measure()))
        }
    }
    @ParityScenario("behavior")
    suspend fun behavior(writer:CaptureWriter) {
        ComposeHost(writer,420,300,desktopContext()).use { host ->
            val c=writer.readConfig<AppBarConfig>(); val scene=Scene(c)
            host.mount { scene.Content() }; scene.setFraction(); host.frame(0)
            fun sample(id:String,image:Boolean=true)=writer.submit(host.snapshot(id,id,scene.measure(true),image))
            var next=16000L
            fun advance(time:Long) { while(next<=time) { host.frame(next); next+=16000 }; host.advanceTo(time) }
            // Recompose the changed configuration before placing/drawing the frame,
            // matching Avalonia's property application at the same event time.
            fun change(time:Long,id:String,action:()->Unit) { advance(time); action(); writer.record(time,"mutation",id); host.frame(time); sample(id) }
            fun times(vararg values:Long) { for(time in values) { advance(time); host.draw(); sample("time-$time") } }
            sample("initial")
            when(c.action) {
                "scroll" -> {
                    change(1000,"up-pre") { scene.scroll(-25f,-25f) }; change(17000,"up-boundary") { scene.scroll(-50f,-50f) }
                    change(33000,"up-body") { scene.scroll(-40f,-32f) }; change(49000,"down-body") { scene.scroll(20f,20f) }
                    change(65000,"down-boundary") { scene.scroll(50f,10f) }
                    change(81000,"cannot-scroll") { scene.canScroll=false; scene.scroll(-25f,-25f) }
                    change(97000,"at-start") { scene.canScroll=true; scene.atStart=true; scene.scroll(30f,0f) }; times(160000,400000)
                }
                "fling", "interrupt" -> {
                    change(1000,"post-fling") { scene.fling(c.velocity.toFloat()) }; times(16000,32000,64000)
                    if(c.action=="interrupt") change(65000,"interrupted") { scene.flingJob?.cancel() }
                    times(96000,160000,240000,400000,800000)
                }
                "colors" -> {
                    change(1000,"overlap") { scene.scroll(-30f,-30f) }; times(16000,64000)
                    change(65000,"color-target") { scene.customColors=true }; times(96000,160000,240000,400000)
                    change(401000,"color-restore") { scene.customColors=false }; times(416000,480000,640000,800000)
                }
                "layout" -> {
                    change(1000,"half") { scene.setFraction(.5f) }; change(17000,"rtl") { scene.rtl=true }
                    change(33000,"insets") { scene.insets=listOf(16.0,30.0,24.0,0.0) }
                    change(49000,"height") { scene.height=160.0 }; change(65000,"restore") { scene.height=null; scene.setFraction(0f) }
                }
                "custom" -> {
                    change(1000,"replace") { scene.custom=true }; change(17000,"padding") { scene.contentPadding=listOf(12.0,8.0,16.0,4.0) }
                    change(33000,"restore") { scene.custom=false }
                }
                "drag", "drag-fling" -> {
                    val origin=Offset(300f,if(c.variant=="bottom") 70f else 100f)
                    fun input(time:Long,action:String,point:Offset) { advance(time); host.input(action,point); host.frame(time); sample("$action-$time") }
                    input(2000,"down",origin); input(18000,"move",origin+Offset(0f,-12f)); input(34000,"move",origin+Offset(0f,-28f))
                    if(c.action=="drag-fling") input(50000,"move",origin+Offset(0f,-44f))
                    input(if(c.action=="drag") 90000 else 51000,"up",origin+Offset(0f,if(c.action=="drag") -28f else -44f))
                    times(96000,160000,240000,400000,800000)
                }
            }
            advance(900000); sample("idle",false)
        }
    }
    private fun desktopContext():PlatformContext {
        val empty=PlatformContext.Empty()
        // ComposeSceneMediator.desktop supplies 18dp at the actual window density;
        // Empty uses 18 physical pixels and would misrepresent native desktop drag slop.
        return object:PlatformContext by empty {
            override val viewConfiguration=object:ViewConfiguration by empty.viewConfiguration {
                override val touchSlop=18f*ComposeHost.RenderScale
            }
        }
    }
    private class Scene(val c:AppBarConfig) {
        val bar=Probe(); val markers=linkedMapOf<String,Probe>(); val colors=mutableMapOf<String,Color>()
        val top=TopAppBarState(-Float.MAX_VALUE,0f,0f); val bottom=BottomAppBarState(-Float.MAX_VALUE,0f,0f)
        var rtl by mutableStateOf(c.rtl); var insets by mutableStateOf(c.insets); var contentPadding by mutableStateOf(c.padding)
        var height by mutableStateOf(c.expandedHeight); var custom by mutableStateOf(c.custom); var customColors by mutableStateOf(false)
        var canScroll=true; var atStart=false
        val dispatcher=NestedScrollDispatcher(); lateinit var scope:CoroutineScope; var flingJob:Job?=null
        var consumed=Offset.Zero; var available=Offset.Zero; var flingResult=Velocity.Zero
        @Composable fun Marker(id:String,width:Double,height:Double) {
            val twoRows=c.variant !in listOf("small","center","bottom","flexibleBottom")
            val effectiveId=remember { if(twoRows && id=="title" && !markers.containsKey("collapsed-title")) "collapsed-title"
                else if(twoRows && id=="subtitle" && !markers.containsKey("collapsed-subtitle")) "collapsed-subtitle" else id }
            val probe=remember { Probe().also { markers[effectiveId]=it } }; val color=LocalContentColor.current; colors[effectiveId]=color
            Box(probe.modifier().size(width.toFloat().dp,height.toFloat().dp).background(color))
        }
        @Composable fun Content() {
            scope=rememberCoroutineScope()
            val scheme=if(c.dark) darkColorScheme(surface=Color(0xFF141218),surfaceContainer=Color(0xFF211F26))
                else lightColorScheme(surface=Color(0xFFFFF7FF),surfaceContainer=Color(0xFFF3EDF7))
            MaterialTheme(colorScheme=scheme,motionScheme=MotionScheme.expressive()) {
                CompositionLocalProvider(LocalLayoutDirection provides if(rtl) LayoutDirection.Rtl else LayoutDirection.Ltr) {
                    Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                        val consumed=padding(c.consumed)
                        Box(Modifier.consumeWindowInsets(consumed).padding(consumed)) { Bar() }
                    }
                }
            }
        }
        @Composable fun Bar() {
            val scrollable=remember { object:ScrollableState by ScrollableState({ it }) {
                override val scrollIndicatorState=object:ScrollIndicatorState { override val scrollOffset:Int get()=if(atStart) 0 else 1; override val contentSize:Int get()=100; override val viewportSize:Int get()=10 }
            } }
            val topBehavior=when(c.behavior) { "pinned"->TopAppBarDefaults.pinnedScrollBehavior(scrollable,top,canScroll={canScroll})
                "enter"-> if(c.noSpecs) TopAppBarDefaults.enterAlwaysScrollBehavior(top,canScroll={canScroll},isScrollingContentAtStart={atStart},snapAnimationSpec=null,flingAnimationSpec=null)
                    else TopAppBarDefaults.enterAlwaysScrollBehavior(top,canScroll={canScroll},isScrollingContentAtStart={atStart})
                else -> if(c.noSpecs) TopAppBarDefaults.exitUntilCollapsedScrollBehavior(top,canScroll={canScroll},snapAnimationSpec=null,flingAnimationSpec=null)
                    else TopAppBarDefaults.exitUntilCollapsedScrollBehavior(top,canScroll={canScroll}) }
            val bottomBehavior=if(c.noSpecs) BottomAppBarDefaults.exitAlwaysScrollBehavior(bottom,canScroll={canScroll},snapAnimationSpec=null,flingAnimationSpec=null)
                else BottomAppBarDefaults.exitAlwaysScrollBehavior(bottom,canScroll={canScroll})
            val empty=remember { object:NestedScrollConnection {} }
            val connection=if(c.variant=="bottom" || c.variant=="flexibleBottom") bottomBehavior.nestedScrollConnection else topBehavior.nestedScrollConnection
            val modifier=bar.modifier().width(c.width.toFloat().dp).nestedScroll(connection).nestedScroll(empty,dispatcher)
            val insets=windowInsets(insets)
            val title:@Composable ()->Unit={ Marker("title",c.titleWidth,c.titleHeight) }
            val subtitle:(@Composable ()->Unit)?=if(c.subtitle) { { Marker("subtitle",70.0,12.0) } } else null
            val nav:@Composable ()->Unit={ if(c.navigation) Marker("navigation",48.0,48.0) }
            val actions:@Composable RowScope.()->Unit={ repeat(c.actions) { Marker("action-$it",48.0,48.0) } }
            val align=when(c.alignment) { "center"->Alignment.CenterHorizontally; "end"->Alignment.End; else->Alignment.Start }
            val expanded=height?.toFloat()?.dp
            val collapsed=c.collapsedHeight?.toFloat()?.dp ?: 64.dp
            val behavior=topBehavior
            val colors=if(customColors) TopAppBarDefaults.topAppBarColors(containerColor=Color(0xFF336699),scrolledContainerColor=Color(0xFF228866),navigationIconContentColor=Color(0xFFDD7722),titleContentColor=Color(0xFF8866AA),actionIconContentColor=Color(0xFF008877),subtitleContentColor=Color(0xFF223344)) else TopAppBarDefaults.topAppBarColors()
            when(c.variant) {
                "small"-> if(c.subtitle) TopAppBar(title=title,subtitle=checkNotNull(subtitle),modifier=modifier,navigationIcon=nav,actions=actions,
                    titleHorizontalAlignment=align,expandedHeight=expanded ?: 64.dp,windowInsets=insets,contentPadding=padding(contentPadding),colors=colors,scrollBehavior=behavior)
                    else TopAppBar(title=title,modifier=modifier,navigationIcon=nav,actions=actions,expandedHeight=expanded ?: 64.dp,
                        windowInsets=insets,contentPadding=padding(contentPadding),colors=colors,scrollBehavior=behavior)
                "center"->CenterAlignedTopAppBar(title=title,modifier=modifier,navigationIcon=nav,actions=actions,
                    expandedHeight=expanded ?: 64.dp,windowInsets=insets,contentPadding=padding(contentPadding),colors=colors,scrollBehavior=behavior)
                "medium"->MediumTopAppBar(title=title,modifier=modifier,navigationIcon=nav,actions=actions,collapsedHeight=collapsed,
                    expandedHeight=expanded ?: 112.dp,windowInsets=insets,colors=colors,scrollBehavior=behavior)
                "large"->LargeTopAppBar(title=title,modifier=modifier,navigationIcon=nav,actions=actions,collapsedHeight=collapsed,
                    expandedHeight=expanded ?: 152.dp,windowInsets=insets,colors=colors,scrollBehavior=behavior)
                "mediumFlexible"->MediumFlexibleTopAppBar(title=title,subtitle=subtitle,modifier=modifier,navigationIcon=nav,actions=actions,
                    titleHorizontalAlignment=align,collapsedHeight=collapsed,expandedHeight=expanded ?: if(c.subtitle) 136.dp else 112.dp,windowInsets=insets,colors=colors,scrollBehavior=behavior)
                "largeFlexible"->LargeFlexibleTopAppBar(title=title,subtitle=subtitle,modifier=modifier,navigationIcon=nav,actions=actions,
                    titleHorizontalAlignment=align,collapsedHeight=collapsed,expandedHeight=expanded ?: if(c.subtitle) 152.dp else 120.dp,windowInsets=insets,colors=colors,scrollBehavior=behavior)
                "twoRows"->TwoRowsTopAppBar(title={ expanded -> Marker(if(expanded) "title" else "collapsed-title",c.titleWidth,c.titleHeight) },
                    subtitle=if(c.subtitle) { { expanded -> Marker(if(expanded) "subtitle" else "collapsed-subtitle",70.0,12.0) } } else null,
                    modifier=modifier,navigationIcon=nav,actions=actions,titleHorizontalAlignment=align,collapsedHeight=collapsed,
                    expandedHeight=expanded ?: Dp.Unspecified,windowInsets=insets,colors=colors,scrollBehavior=behavior)
                "bottom"-> {
                    val scroll=bottomBehavior
                    val contentPadding=if(contentPadding==null) BottomAppBarDefaults.ContentPadding else padding(contentPadding)
                    if(custom) BottomAppBar(modifier=modifier,tonalElevation=c.elevation.toFloat().dp,windowInsets=insets,contentPadding=contentPadding,
                        scrollBehavior=scroll) { Box(Modifier.fillMaxSize()) { Marker("custom",80.0,24.0) } }
                    else BottomAppBar(actions=actions,floatingActionButton=if(c.fab) { { Marker("fab",56.0,56.0) } } else null,
                        modifier=modifier,tonalElevation=c.elevation.toFloat().dp,windowInsets=insets,contentPadding=contentPadding,scrollBehavior=scroll)
                }
                "flexibleBottom"-> {
                    val arrangement=when(c.arrangement) { "center"->Arrangement.Center; "end"->Arrangement.End;
                        "spaceBetween"->Arrangement.SpaceBetween; "spaceAround"->Arrangement.SpaceAround; "spaceEvenly"->Arrangement.SpaceEvenly;
                        "spacedBy"->Arrangement.spacedBy(c.spacing.toFloat().dp); else->Arrangement.Start }
                    FlexibleBottomAppBar(modifier=modifier,windowInsets=insets,
                        contentPadding=if(contentPadding==null) BottomAppBarDefaults.FlexibleContentPadding else padding(contentPadding),
                        horizontalArrangement=arrangement,expandedHeight=expanded ?: 64.dp,scrollBehavior=bottomBehavior) {
                        if(custom) Box(Modifier.fillMaxSize()) { Marker("custom",80.0,24.0) } else actions()
                    }
                }
            }
            // Standard medium/large/flexible wrappers call the same title lambda twice.
            // Distinguish their actual coordinate instances through independent probes below.
        }
        fun setFraction(value:Float=c.fraction.toFloat()) { if(c.variant=="bottom" || c.variant=="flexibleBottom") bottom.heightOffset=bottom.heightOffsetLimit*value
            else top.heightOffset=top.heightOffsetLimit*value }
        fun scroll(delta:Float,body:Float) {
            val input=Offset(0f,delta*ComposeHost.RenderScale); val pre=dispatcher.dispatchPreScroll(input,NestedScrollSource.UserInput)
            val left=input-pre; val content=Offset(0f,kotlin.math.sign(left.y)*kotlin.math.min(kotlin.math.abs(left.y),kotlin.math.abs(body*ComposeHost.RenderScale)))
            val post=dispatcher.dispatchPostScroll(content,left-content,NestedScrollSource.UserInput)
            consumed=pre+content+post; available=input-consumed
        }
        fun fling(velocity:Float) {
            flingJob=scope.launch {
                val input=Velocity(0f,velocity*ComposeHost.RenderScale); val pre=dispatcher.dispatchPreFling(input)
                flingResult=pre+dispatcher.dispatchPostFling(Velocity.Zero,input-pre)
            }
        }
        fun measure(state:Boolean=false):List<ProbePoint> {
            fun geometry(probe:Probe)=probe.geometry().filter { it.name=="box" || it.name=="size" }
            val points=mutableListOf(ProbePoint("bar","bar",null,geometry(bar)))
            if(state) {
                val values=if(c.variant=="bottom" || c.variant=="flexibleBottom") listOf(bottom.heightOffsetLimit,bottom.heightOffset,bottom.contentOffset,bottom.collapsedFraction)
                    else listOf(top.heightOffsetLimit,top.heightOffset,top.contentOffset,top.collapsedFraction)
                val names=listOf("limit","offset","content-offset","fraction","consumed","available","fling-complete","fling-cancelled","fling-consumed")
                val facts=values.map { it.toDouble() }+listOf(consumed.y.toDouble(),available.y.toDouble(),if(flingJob?.isCompleted==true && flingJob?.isCancelled==false) 1.0 else 0.0,if(flingJob?.isCancelled==true) 1.0 else 0.0,flingResult.y.toDouble())
                points+=ProbePoint("scroll-state","scroll-state","bar",names.mapIndexed { i,name -> number(name,facts[i],if(i==3 || i==6 || i==7) "none" else if(i==8) "px/s" else "px","none","motion","appbar.$name","AppBarState/NestedScrollDispatcher") })
            }
            for((id,probe) in markers) if(probe.coordinates?.isAttached==true) {
                val argb=checkNotNull(colors[id]).toArgb(); points+=ProbePoint(id,id,"bar",geometry(probe)+vector("color","color",
                    listOf((argb ushr 16) and 255,(argb ushr 8) and 255,argb and 255,(argb ushr 24) and 255).map { it.toDouble() },"srgba8","none","properties","appbar.inherited-content-color","LocalContentColor"))
            }
            return points
        }
        fun padding(v:List<Double>?)=if(v==null) PaddingValues(0.dp) else PaddingValues(v[0].toFloat().dp,v[1].toFloat().dp,v[2].toFloat().dp,v[3].toFloat().dp)
        fun windowInsets(v:List<Double>?)=if(v==null) WindowInsets(0,0,0,0) else WindowInsets(v[0].toFloat().dp,v[1].toFloat().dp,v[2].toFloat().dp,v[3].toFloat().dp)
    }
}

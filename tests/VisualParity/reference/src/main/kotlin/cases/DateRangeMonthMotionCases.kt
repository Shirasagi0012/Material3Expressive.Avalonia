@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class,androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.focusable
import androidx.compose.foundation.gestures.FlingBehavior
import androidx.compose.foundation.gestures.ScrollableDefaults
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.platform.PlatformContext
import androidx.compose.ui.platform.ViewConfiguration
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable
import java.time.LocalDate
import java.util.Locale

@Serializable data class DateRangeMonthMotionConfig(val action:String="fling",val realPicker:Boolean=false,val rtl:Boolean=false)

@ParitySuite("date-range-month-motion")
class DateRangeMonthMotionCases {
    @ParityScenario("motion") suspend fun motion(writer:CaptureWriter) {
        val c=writer.readConfig<DateRangeMonthMotionConfig>()
        val previousLocale=Locale.getDefault();Locale.setDefault(Locale.US)
        try {
            val state=LazyListState(13)
            val calendar=DateRangePickerState(locale=Locale.US,initialDisplayedMonthMillis=LocalDate.of(2024,2,1).toEpochDay()*86400000,yearRange=2024..2025)
            val probe=Probe()
            val focus=FocusRequester()
            var height by mutableStateOf(180)
            lateinit var scope:CoroutineScope
            lateinit var fling:FlingBehavior
            // Fixed Core ComposeSceneMediator.DesktopViewConfiguration scales touch slop
            // with density. PlatformContext.Empty uses 18 raw pixels and is not the desktop
            // input environment at this host's 4x capture density.
            val desktopContext=object:PlatformContext by PlatformContext.Empty() {
                override val viewConfiguration=object:ViewConfiguration by PlatformContext.DefaultViewConfiguration {
                    override val touchSlop=18f*ComposeHost.RenderScale
                }
            }
            ComposeHost(writer,440,680,desktopContext).use {host->
                host.mount {
                    MaterialTheme {
                        scope=rememberCoroutineScope();fling=ScrollableDefaults.flingBehavior()
                        CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                            Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp)) {
                                if(c.realPicker)DateRangePicker(calendar,Modifier.width(400.dp).height(620.dp))
                                else LazyColumn(probe.modifier().width(240.dp).height(height.dp),state=state,flingBehavior=fling) {
                                    items(36) { index->
                                        Box(Modifier.fillParentMaxWidth().height((when(index%3){0->80;1->140;else->100}).dp)
                                            .background(when(index%3){0->Color.Red;1->Color.Blue;else->Color(0xff008000)})) {
                                            if(c.action.startsWith("focus")) {
                                                val target=if(c.action=="focus")14 else 13
                                                val focusModifier=if(index==target)Modifier.focusRequester(focus) else Modifier
                                                Box(Modifier.align(Alignment.BottomStart).size(24.dp,20.dp).background(Color.Yellow).then(focusModifier).focusable())
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };host.frame(0)
                for(time in 16000L..512000L step 16000L)host.frame(time)
                fun sample(id:String) {
                    val facts=if(c.realPicker)listOf(
                        number("month",(calendar.displayedMonthMillis/86400000+719162).toDouble(),"day",definition="date-picker.displayed-month-day-number"),
                        flag("selected-start",calendar.selectedStartDateMillis!=null,definition="date-range.has-selected-start"),
                        flag("selected-end",calendar.selectedEndDateMillis!=null,definition="date-range.has-selected-end"))
                    else probe.geometry().filter{it.name=="box"||it.name=="size"}+listOf(
                        number("index",state.firstVisibleItemIndex.toDouble(),"item",definition="date-range.first-visible-index"),
                        number("offset",state.firstVisibleItemScrollOffset/ComposeHost.RenderScale.toDouble(),"dip",definition="date-range.first-visible-offset"))
                    writer.submit(host.snapshot(id,id,listOf(ProbePoint("months",if(c.realPicker)"date-range-calendar" else "date-range-month-list",null,facts)),true))
                }
                sample("initial")
                val d=ComposeHost.RenderScale.toFloat()
                val origin=if(c.realPicker)Offset(220f,450f) else Offset(84f,124f)
                for(elapsed in 16000L..1280000L step 16000L) {
                    val time=600000+elapsed;host.advanceTo(time)
                    if(elapsed==16000L && c.action.startsWith("focus")) {
                        focus.requestFocus();host.scheduler.runCurrent();writer.record(time,"action","focus day marker")
                    }
                    if(elapsed==80000L && c.action=="focus-resize") {
                        height=100;writer.record(time,"mutation","viewport height 180 to 100")
                    }
                    if(elapsed==16000L && c.action in listOf("fling","fling-back","boundary")) {
                        scope.launch {
                            if(c.action=="boundary")state.scrollToItem(35)
                            state.scroll { scrollBy(20*d);with(fling){performFling((if(c.action=="fling-back")-1500 else 1500)*d)} }
                        };host.scheduler.runCurrent();writer.record(time,"action","range spline fling ${c.action}")
                    }
                    if(c.action in listOf("touch","touch-slow","interrupt")) {
                        var distance=when(elapsed){16000L->0f;32000L->10f;48000L->36f;64000L->60f;80000L->88f;else->-1f}
                        val release=if(c.action=="touch-slow")160000L else 96000L
                        if(distance>=0 || elapsed==release) {
                            if(elapsed==release)distance=88f
                            val point=Offset(origin.x*d,(origin.y-distance)*d)
                            val type=if(elapsed==16000L)PointerEventType.Press else if(elapsed==release)PointerEventType.Release else PointerEventType.Move
                            host.scene.sendPointerEvent(type,point,timeMillis=1000+time/1000,type=PointerType.Touch)
                            host.scheduler.runCurrent();writer.record(time,"input","touch $type ($point)")
                        }
                        if(elapsed==160000L && c.action=="interrupt") {
                            if(c.realPicker)calendar.displayedMonthMillis=LocalDate.of(2024,7,1).toEpochDay()*86400000
                            else scope.launch {state.scrollToItem(18)}
                            host.scheduler.runCurrent();writer.record(time,"mutation","external July interrupts scrolling")
                        }
                    }
                    if(c.action=="wheel" && elapsed==16000L) {
                        host.scene.sendPointerEvent(PointerEventType.Scroll,origin*d,scrollDelta=Offset(0f,3f),timeMillis=1000+time/1000)
                        host.scheduler.runCurrent();writer.record(time,"input","vertical wheel 3")
                    }
                    host.frame(time)
                    if(elapsed<=240000L || elapsed%160000L==0L)sample("time-$elapsed")
                }
            }
        } finally { Locale.setDefault(previousLocale) }
    }
}

@file:Suppress("INVISIBLE_REFERENCE", "INVISIBLE_MEMBER")
@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class,androidx.compose.material3.ExperimentalMaterial3Api::class)
package parity

import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.FlingBehavior
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.serialization.Serializable

@Serializable data class DateMonthMotionConfig(val action:String="next",val rtl:Boolean=false,val width:Double=336.0)
@ParitySuite("date-month-motion")
class DateMonthMotionCases {
    @ParityScenario("motion") suspend fun motion(writer:CaptureWriter) {
        val c=writer.readConfig<DateMonthMotionConfig>()
        ComposeHost(writer,420,160).use {host->
            val state=LazyListState(13)
            val probe=Probe()
            lateinit var scope:CoroutineScope
            lateinit var fling:FlingBehavior
            host.mount {
                MaterialTheme {
                    scope=rememberCoroutineScope()
                    fling=DatePickerDefaults.rememberSnapFlingBehavior(state)
                    CompositionLocalProvider(LocalLayoutDirection provides if(c.rtl)LayoutDirection.Rtl else LayoutDirection.Ltr) {
                        Box(Modifier.fillMaxSize().background(Color.White).padding(16.dp),contentAlignment=Alignment.TopStart) {
                            // Keep the viewport's scene position fixed while its list direction changes.
                            Box(Modifier.fillMaxSize(),contentAlignment=if(c.rtl)Alignment.TopEnd else Alignment.TopStart) {
                                LazyRow(probe.modifier().width(c.width.toFloat().dp).height(112.dp),state=state,flingBehavior=fling) {
                                    items(36) { index-> Box(Modifier.fillParentMaxWidth().fillMaxHeight().background(if(index%2==1)Color.Red else Color.Blue)) }
                                }
                            }
                        }
                    }
                }
            };host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,listOf(ProbePoint("months","date-month-strip",null,
                probe.geometry().filter{it.name=="box"||it.name=="size"}+listOf(
                    number("index",state.firstVisibleItemIndex.toDouble(),"item",definition="date-month.first-visible-index"),
                    number("offset",state.firstVisibleItemScrollOffset/ComposeHost.RenderScale.toDouble(),"dip",definition="date-month.first-visible-offset")))),true))
            sample("initial");host.advanceTo(16000)
            scope.launch {
                if(c.action.startsWith("snap")) {
                    state.scrollToItem(13,kotlin.math.round(c.width*.3*ComposeHost.RenderScale).toInt())
                    state.scroll {with(fling){performFling((if(c.action=="snap-fast")600f else if(c.action=="snap-back")-600f else 0f)*ComposeHost.RenderScale)}}
                } else state.animateScrollToItem(if(c.action=="previous")12 else 14)
            }
            host.scheduler.runCurrent()
            for(t in 16000L..816000L step 16000L) {
                host.advanceTo(t)
                if(t==96000L && c.action=="reverse"){scope.launch {state.animateScrollToItem(12)};host.scheduler.runCurrent()}
                host.frame(t)
                if(t<=256000L || t==400000L || t==816000L)sample("time-$t")
            }
        }
    }
}

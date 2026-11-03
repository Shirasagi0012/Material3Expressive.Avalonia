@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.geometry.Offset
import kotlinx.serialization.Serializable

@Serializable
data class TabSourceConfig(val native:Boolean,val content:String,val action:String,val rtl:Boolean=false)

@ParitySuite("tab-source")
class TabSourceCases {
    @ParityScenario("source") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<TabSourceConfig>()
        ComposeHost(writer,420,208).use { host ->
            val scene=TabRowCases.TabScene(TabRowConfig(native=c.native,content=c.content,rtl=c.rtl))
            scene.interactions=MutableInteractionSource()
            val content:@Composable ()->Unit={scene.Content()};val measure=scene::measure;val item=scene.tabs[1]
            val getSource:()->MutableInteractionSource={checkNotNull(scene.interactions)}
            val setSource:(MutableInteractionSource)->Unit={scene.interactions=it}
            val setEnabled:(Boolean)->Unit={scene.enabled=it}
            host.mount(content);host.draw();host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,measure()))
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"source",id);host.draw();sample(id)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            sample("initial")
            val scale=ComposeHost.RenderScale.toFloat()
            val coordinates=checkNotNull(item.coordinates)
            val first=PressInteraction.Press(Offset(7f,9f)*scale)
            val second=PressInteraction.Press(Offset(coordinates.size.width-9f*scale,15f*scale))
            val hover=HoverInteraction.Enter();val focus=FocusInteraction.Focus()
            fun emit(interaction:Interaction){check(getSource().tryEmit(interaction))}
            if(c.action=="native-replace") {
                val pointer=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/scale
                fun input(time:Long,id:String,action:String){advance(time);host.input(action,pointer);writer.record(time,"source",id);host.draw();sample(id)}
                advance(1000);host.input("move",pointer);input(1000,"native-press","down");at(16000);at(64000)
                change(65000,"replace"){setSource(MutableInteractionSource())};at(80000);at(128000)
                input(129000,"native-release","up");at(160000);at(400000);at(800000);return
            }
            if(c.action=="hover-focus") {
                change(1000,"hover"){emit(hover)};at(16000);at(64000)
                change(65000,"focus"){emit(focus)};at(80000);at(128000)
                change(129000,"unfocus"){emit(FocusInteraction.Unfocus(focus))};at(160000)
                change(193000,"hover-end"){emit(HoverInteraction.Exit(hover))}
            } else {
                change(1000,"press"){emit(first)};at(16000);at(64000)
                when(c.action) {
                    "overlap" -> {
                        change(65000,"second-press"){emit(second)};at(80000);at(128000)
                        change(129000,"first-release"){emit(PressInteraction.Release(first))};at(160000)
                        change(193000,"last-cancel"){emit(PressInteraction.Cancel(second))}
                    }
                    "replace" -> {
                        val old=getSource()
                        change(65000,"replace"){setSource(MutableInteractionSource())};at(80000);at(128000)
                        change(129000,"old-release"){check(old.tryEmit(PressInteraction.Release(first)))};at(160000)
                        change(193000,"new-press"){emit(second)}
                        change(241000,"new-release"){emit(PressInteraction.Release(second))}
                    }
                    "disable" -> {
                        change(65000,"disable"){setEnabled(false)};at(80000);at(128000)
                        change(129000,"external-release"){emit(PressInteraction.Release(first))};at(160000)
                        change(193000,"enable"){setEnabled(true)}
                    }
                }
            }
            at(256000);at(400000);at(800000)
        }
    }
}

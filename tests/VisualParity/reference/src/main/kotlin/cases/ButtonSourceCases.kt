@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.LayoutCoordinates
import kotlinx.serialization.Serializable

@Serializable
data class ButtonSourceConfig(val kind:String,val style:String,val action:String,val checked:Boolean=false,val rtl:Boolean=false)

@ParitySuite("button-source")
class ButtonSourceCases {
    @ParityScenario("source") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<ButtonSourceConfig>()
        ComposeHost(writer,420,208).use { host ->
            val content:@Composable ()->Unit
            val measure:()->List<ProbePoint>
            val probe:Probe
            val getSource:()->MutableInteractionSource
            val setSource:(MutableInteractionSource)->Unit
            val setEnabled:(Boolean)->Unit
            when(c.kind) {
                "button" -> {
                    val s=ButtonCases.ButtonScene(writer,ButtonConfig(style=c.style,width=null,rtl=c.rtl,action="source"))
                    s.interactions=MutableInteractionSource()
                    content={s.Content()};measure=s::measure;probe=s.container
                    getSource={checkNotNull(s.interactions)};setSource={s.interactions=it};setEnabled={s.enabled=it}
                }
                "toggle" -> {
                    val s=ToggleButtonCases.Scene(ToggleButtonConfig(style=c.style,checked=c.checked,rtl=c.rtl))
                    s.interactions=MutableInteractionSource()
                    content={s.Content()};measure=s::measure;probe=s.button
                    getSource={checkNotNull(s.interactions)};setSource={s.interactions=it};setEnabled={s.enabled=it}
                }
                else -> {
                    val s=SplitButtonCases.Scene(SplitButtonConfig(style=c.style,checked=c.checked,toggle=c.kind!="trailing-click",rtl=c.rtl))
                    s.leadingSource=MutableInteractionSource();s.trailingSource=MutableInteractionSource()
                    content={s.Content()};measure=s::measure;probe=if(c.kind=="leading") s.leading else s.trailing
                    getSource={checkNotNull(if(c.kind=="leading") s.leadingSource else s.trailingSource)}
                    setSource={if(c.kind=="leading") s.leadingSource=it else s.trailingSource=it}
                    setEnabled={if(c.kind=="leading") s.leadingEnabled=it else s.trailingEnabled=it}
                }
            }
            host.mount(content);host.draw();host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,measure()))
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"source",id);host.draw();sample(id)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            sample("initial")
            // Interaction positions use the measured clickable container's physical pixels.
            var coordinator:Any?=checkNotNull(probe.coordinates)
            var center=Offset.Zero
            while(coordinator!=null) {
                val layer=field(coordinator,"layer")
                if(layer!=null) {
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    if(graphics.javaClass.methods.single{it.name=="getClip"&&it.parameterCount==0}.invoke(graphics)==true) {
                        val size=(coordinator as LayoutCoordinates).size
                        center=Offset(size.width/2f,size.height/2f)
                    }
                }
                coordinator=field(coordinator,"wrapped")
            }
            val first=PressInteraction.Press(center);val second=PressInteraction.Press(center);val hover=HoverInteraction.Enter()
            fun emit(interaction:Interaction){check(getSource().tryEmit(interaction))}
            if(c.action=="native-replace") {
                val coordinates=checkNotNull(probe.coordinates)
                val pointer=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
                fun input(time:Long,id:String,action:String){advance(time);host.input(action,pointer);writer.record(time,"source",id);host.draw();sample(id)}
                advance(1000);host.input("move",pointer);input(1000,"native-press","down");at(16000);at(64000)
                change(65000,"replace"){setSource(MutableInteractionSource())};at(80000);at(128000)
                input(129000,"native-release","up");at(160000);at(400000)
                input(401000,"new-press","down");at(480000)
                input(481000,"new-release","up");at(800000);return
            }
            if(c.action=="hover-replace") {
                change(1000,"hover"){emit(hover)};at(16000);at(64000)
                change(65000,"replace"){setSource(MutableInteractionSource())};at(80000);at(160000)
                change(193000,"old-end-on-new-source"){emit(HoverInteraction.Exit(hover))};at(208000);at(400000);return
            }
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
                    change(193000,"new-hover"){emit(hover)}
                }
                "disable" -> {
                    change(65000,"disable"){setEnabled(false)};at(80000);at(128000)
                    change(129000,"external-release"){emit(PressInteraction.Release(first))};at(160000)
                    change(193000,"enable"){setEnabled(true)}
                }
            }
            at(208000);at(240000);at(400000);at(800000)
        }
    }
}

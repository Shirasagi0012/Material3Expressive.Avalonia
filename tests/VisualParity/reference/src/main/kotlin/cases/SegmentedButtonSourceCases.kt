@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.LayoutCoordinates

@ParitySuite("segmented-button-source")
class SegmentedButtonSourceCases {
    @ParityScenario("source") suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<SegmentedButtonConfig>()
        ComposeHost(writer,520,208).use {host ->
            val scene=SegmentedButtonCases.Scene(config)
            host.mount{scene.Content()};host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            sample("initial");var time=0L;var next=16000L
            fun change(id:String,action:()->Unit) {
                time+=64000;while(next<=time){host.frame(next);next+=16000}
                host.advanceTo(time);action();writer.record(time,"source",id);host.draw();host.frame(time);sample(id)
            }
            var coordinator:Any?=checkNotNull(scene.items[0].coordinates);var center=Offset.Zero
            while(coordinator!=null) {
                field(coordinator,"layer")?.let{layer ->
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    if(graphics.javaClass.methods.single{it.name=="getClip"&&it.parameterCount==0}.invoke(graphics)==true) {
                        val size=(coordinator as LayoutCoordinates).size;center=Offset(size.width/2f,size.height/2f)
                    }
                }
                coordinator=field(coordinator,"wrapped")
            }
            val press=PressInteraction.Press(center);val second=PressInteraction.Press(center)
            val focus=FocusInteraction.Focus();val hover=HoverInteraction.Enter()
            fun emit(id:String,value:Interaction)=change(id){check(scene.sources[0].tryEmit(value))}
            when(config.action) {
                "source-count" -> {
                    emit("press",press);emit("second-press",second);emit("focus",focus)
                    emit("first-release",PressInteraction.Release(press));emit("cancel",PressInteraction.Cancel(second))
                    emit("unfocus",FocusInteraction.Unfocus(focus));emit("unmatched-release",PressInteraction.Release(press))
                }
                "source-replace" -> {
                    emit("press",press);val old=scene.sources[0]
                    change("replace"){scene.sources[0]=MutableInteractionSource()}
                    change("old-release"){check(old.tryEmit(PressInteraction.Release(press)))}
                    emit("new-release",PressInteraction.Release(press));emit("new-press",second);emit("cancel",PressInteraction.Cancel(second))
                }
                "source-hover" -> {
                    emit("hover",hover);val drag=DragInteraction.Start();emit("drag",drag)
                    emit("hover-exit",HoverInteraction.Exit(hover));emit("drag-stop",DragInteraction.Stop(drag))
                }
            }
            change("late"){}
        }
    }
}

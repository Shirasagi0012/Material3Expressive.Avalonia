@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.ui.geometry.Offset

@ParitySuite("switch-source")
class SwitchSourceCases {
    @ParityScenario("source") suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<SwitchConfig>()
        ComposeHost(writer,160,128).use { host ->
            val scene=SwitchCases.SwitchScene(config)
            host.mount { scene.Content() };host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            sample("initial");var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"source",id);host.draw();host.frame(time);sample(id)}
            // Unbounded Switch ripple ignores the source position and uses the thumb center.
            val first=PressInteraction.Press(Offset.Zero);val second=PressInteraction.Press(Offset.Zero)
            fun emit(value:Interaction){check(scene.source.tryEmit(value))}
            change(1000,"press"){emit(first)};at(16000);at(64000)
            when(config.action){
                "source-overlap" -> {
                    change(65000,"second-press"){emit(second)};at(80000)
                    change(129000,"first-release"){emit(PressInteraction.Release(first))};at(160000)
                    change(193000,"last-cancel"){emit(PressInteraction.Cancel(second))}
                }
                "source-unmatched" -> {
                    change(65000,"unmatched-release"){emit(PressInteraction.Release(second))};at(80000)
                    change(129000,"negative-count"){emit(PressInteraction.Cancel(first))};at(160000)
                    change(193000,"count-zero"){emit(second)};at(208000)
                    change(257000,"count-positive"){emit(first)}
                }
                "source-replace" -> {
                    val old=scene.source
                    change(65000,"replace"){scene.source=MutableInteractionSource()};at(80000)
                    change(129000,"old-release"){check(old.tryEmit(PressInteraction.Release(first)))};at(160000)
                    change(193000,"new-press"){emit(second)};at(208000)
                    change(257000,"old-press"){check(old.tryEmit(first))}
                }
                "source-disable" -> {
                    change(65000,"disable"){scene.enabled=false};at(80000)
                    change(129000,"release"){emit(PressInteraction.Release(first))};at(160000)
                    change(193000,"enable"){scene.enabled=true}
                }
            }
            at(272000);at(320000);at(400000);at(800000)
        }
    }
}

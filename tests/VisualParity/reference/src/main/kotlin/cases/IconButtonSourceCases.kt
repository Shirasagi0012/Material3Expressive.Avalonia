@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.ui.geometry.Offset

@ParitySuite("icon-button-source")
class IconButtonSourceCases {
    @ParityScenario("source") suspend fun capture(writer:CaptureWriter) {
        val config=writer.readConfig<IconButtonConfig>()
        ComposeHost(writer,320,240).use { host ->
            val scene=IconButtonCases.IconScene(config)
            host.mount { scene.Content() };host.draw();host.frame(-1000);host.frame(0)
            fun sample(id:String)=writer.submit(host.snapshot(id,id,scene.measure()))
            sample("initial")
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"source",id);host.draw();sample(id)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            // Source positions are clickable-local physical pixels in Compose. The default
            // small container is 40dp; use its measured center via the real clipped coordinator.
            var coordinator:Any?=checkNotNull(scene.container.coordinates)
            var center=Offset.Zero
            while(coordinator!=null) {
                val layer=field(coordinator,"layer")
                if(layer!=null) {
                    val graphics=checkNotNull(field(layer,"graphicsLayer"))
                    if(graphics.javaClass.methods.single{it.name=="getClip"&&it.parameterCount==0}.invoke(graphics)==true) {
                        val size=(coordinator as androidx.compose.ui.layout.LayoutCoordinates).size
                        center=Offset(size.width/2f,size.height/2f)
                    }
                }
                coordinator=field(coordinator,"wrapped")
            }
            val first=PressInteraction.Press(center);val second=PressInteraction.Press(center)
            val hover=HoverInteraction.Enter()
            fun emit(interaction:Interaction){check(scene.interactions.tryEmit(interaction))}
            if(config.action=="source-native-replace") {
                val coordinates=checkNotNull(scene.container.coordinates)
                val pointer=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
                suspend fun input(time:Long,id:String,action:String){advance(time);host.input(action,pointer);writer.record(time,"source",id);host.draw();sample(id)}
                advance(1000);host.input("move",pointer);input(1000,"native-press","down");at(16000);at(64000)
                change(65000,"replace"){scene.interactions=MutableInteractionSource()};at(80000);at(128000)
                input(129000,"native-release","up");at(160000);at(400000)
                input(401000,"new-native-press","down");at(480000)
                input(481000,"new-native-release","up");at(800000)
                return
            }
            if(config.action=="source-hover") {
                val other=HoverInteraction.Enter()
                change(1000,"hover"){emit(hover)};at(16000);at(64000)
                change(65000,"second-hover"){emit(other)};at(80000)
                change(81000,"first-exit"){emit(HoverInteraction.Exit(hover))};at(112000)
                change(113000,"last-exit"){emit(HoverInteraction.Exit(other))};at(160000);at(400000)
                return
            }
            change(1000,"press"){emit(first)};at(16000);at(64000)
            when(config.action) {
                "shape-cycle" -> {
                    change(65000,"static-shape"){scene.pressedRadius=null};at(80000);at(128000)
                    change(129000,"enable-morph"){scene.pressedRadius=8.0};at(160000)
                    change(193000,"new-press"){emit(second)}
                }
                "source-overlap" -> {
                    change(65000,"second-press"){emit(second)};at(80000);at(128000)
                    change(129000,"first-release"){emit(PressInteraction.Release(first))};at(160000)
                    change(193000,"last-cancel"){emit(PressInteraction.Cancel(second))}
                }
                "source-replace" -> {
                    val old=scene.interactions
                    change(65000,"replace"){scene.interactions=MutableInteractionSource()};at(80000);at(128000)
                    change(129000,"old-release"){check(old.tryEmit(PressInteraction.Release(first)))};at(160000)
                    change(193000,"new-hover"){emit(hover)}
                }
                "source-disable" -> {
                    change(65000,"disable"){scene.enabled=false};at(80000);at(128000)
                    change(129000,"external-release"){emit(PressInteraction.Release(first))};at(160000)
                    change(193000,"enable"){scene.enabled=true}
                }
            }
            at(208000);at(240000);at(400000);at(800000)
        }
    }
}

@file:OptIn(androidx.compose.ui.InternalComposeUiApi::class)
package parity

import androidx.compose.foundation.interaction.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.LayoutCoordinates
import kotlinx.serialization.Serializable

@Serializable
data class FabSourceConfig(val kind:String,val size:String,val action:String,val rtl:Boolean=false)

@ParitySuite("fab-source")
class FabSourceCases {
    @ParityScenario("source") suspend fun capture(writer:CaptureWriter) {
        val c=writer.readConfig<FabSourceConfig>()
        ComposeHost(writer,420,240).use { host ->
            val scene=FloatingActionButtonCases.FabScene(FloatingActionButtonConfig(kind=c.kind,size=c.size,rtl=c.rtl,elevation="custom"))
            scene.interactions=MutableInteractionSource()
            // Keep the elevation factory's composition call site stable while changing
            // its parameters; switching factory branches would test a different lifetime.
            if(c.action=="configuration")scene.sourceElevations=listOf(2f,4f,6f,9f)
            host.mount{scene.Content()};host.draw();host.frame(-1000);host.frame(0)
            fun sample(id:String) {
                // Observe the already-applied layer without executing its block or advancing time.
                val elevation=generateSequence(checkNotNull(scene.buttonProbe.coordinates) as Any) {field(it,"wrapped")}
                    .mapNotNull {field(it,"layer")}.maxOfOrNull { layer ->
                        val graphics=checkNotNull(field(layer,"graphicsLayer"))
                        (graphics.javaClass.methods.single {it.name=="getShadowElevation"&&it.parameterCount==0}.invoke(graphics) as Number).toDouble()
                    } ?: 0.0
                val points=scene.measure().map {point->if(point.id!="fab")point else point.copy(measurements=point.measurements+
                    number("shadowElevation",elevation/ComposeHost.RenderScale,category="motion",definition="fab.shadow-elevation",
                        source="derived: Surface GraphicsLayerOwnerLayer.graphicsLayer.shadowElevation / density"))}
                writer.submit(host.snapshot(id,id,points))
            }
            var next=16000L
            fun advance(time:Long){while(next<=time){host.frame(next);next+=16000};host.advanceTo(time)}
            fun change(time:Long,id:String,action:()->Unit){advance(time);action();writer.record(time,"source",id);host.draw();sample(id)}
            fun at(time:Long){advance(time);host.draw();sample("time-$time")}
            sample("initial")
            // Press coordinates are local to Surface's clickable node, inside minimum-interactive padding.
            var coordinator:Any?=checkNotNull(scene.buttonProbe.coordinates)
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
            val first=PressInteraction.Press(center);val second=PressInteraction.Press(center)
            val hover=HoverInteraction.Enter();val focus=FocusInteraction.Focus()
            fun emit(interaction:Interaction){check(checkNotNull(scene.interactions).tryEmit(interaction))}
            if(c.action=="native-replace") {
                val coordinates=checkNotNull(scene.probe.coordinates)
                val pointer=coordinates.localToRoot(Offset(coordinates.size.width/2f,coordinates.size.height/2f))/ComposeHost.RenderScale.toFloat()
                fun input(time:Long,id:String,action:String){advance(time);host.input(action,pointer);writer.record(time,"source",id);host.draw();sample(id)}
                advance(1000);host.input("move",pointer);input(1000,"native-press","down");at(16000);at(64000)
                change(65000,"replace"){scene.interactions=MutableInteractionSource()};at(80000);at(128000)
                input(129000,"native-release","up");at(160000);at(400000);at(800000);return
            }
            if(c.action=="hover-replace") {
                change(1000,"hover"){emit(hover)};at(16000);at(64000);at(160000)
                change(193000,"replace"){scene.interactions=MutableInteractionSource()};at(208000);at(240000);at(400000)
                change(401000,"new-focus"){emit(focus)};at(480000);at(640000)
                change(641000,"unfocus"){emit(FocusInteraction.Unfocus(focus))};at(800000);return
            }
            change(1000,"hover"){emit(hover)};at(16000);at(64000)
            if(c.action=="configuration") {
                change(65000,"unused-height"){scene.sourceElevations=listOf(2f,40f,60f,9f)};at(80000);at(128000)
                change(129000,"press"){emit(first)};at(160000);at(208000)
                change(209000,"active-height"){scene.sourceElevations=listOf(2f,5f,60f,9f)};at(224000);at(240000)
                change(241000,"cancel"){emit(PressInteraction.Cancel(first))};at(320000);at(400000);at(800000);return
            }
            change(65000,"press"){emit(first)};at(80000);at(128000)
            if(c.action=="overlap") {
                change(129000,"second-press"){emit(second)};at(160000)
                change(193000,"first-release"){emit(PressInteraction.Release(first))};at(208000);at(240000)
                change(241000,"last-cancel"){emit(PressInteraction.Cancel(second))};at(320000)
                change(321000,"hover-end"){emit(HoverInteraction.Exit(hover))}
            } else {
                val old=checkNotNull(scene.interactions)
                change(129000,"replace"){scene.interactions=MutableInteractionSource()};at(160000)
                change(193000,"old-release"){check(old.tryEmit(PressInteraction.Release(first)))};at(208000);at(240000)
                change(241000,"new-focus"){emit(focus)};at(320000)
                change(321000,"unfocus"){emit(FocusInteraction.Unfocus(focus))}
            }
            at(400000);at(800000)
        }
    }
}

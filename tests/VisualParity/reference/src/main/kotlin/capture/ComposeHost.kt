@file:OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class, androidx.compose.ui.InternalComposeUiApi::class)
package parity
import androidx.compose.runtime.*
import androidx.compose.runtime.snapshots.Snapshot
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.asComposeCanvas
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.platform.*
import androidx.compose.ui.scene.CanvasLayersComposeScene
import androidx.compose.ui.scene.ComposeScene
import androidx.compose.ui.unit.*
import kotlinx.coroutines.*
import kotlinx.coroutines.test.*
import org.jetbrains.skia.*
import java.io.File
import kotlinx.serialization.json.*
class ComposeHost(val writer:CaptureWriter, width:Int=240, height:Int=160, platformContext:PlatformContext=PlatformContext.Empty()):AutoCloseable {
    companion object { const val RenderScale=4 }
    val scheduler=TestCoroutineScheduler()
    val scope=CoroutineScope(UnconfinedTestDispatcher(scheduler) + CoroutineExceptionHandler { _,e -> writer.errors+=e.stackTraceToString() })
    val recomposer:FrameRecomposer
    val scene:ComposeScene
    val surface=Surface.makeRasterN32Premul(Math.multiplyExact(width,RenderScale),Math.multiplyExact(height,RenderScale))
    var timeUs:Long=-2000; private set
    var frameTimeUs:Long?=null; private set
    fun advanceTo(time:Long) {
        require(time>=timeUs); timeUs=time
        scheduler.advanceTimeBy(time.coerceAtLeast(0)/1000-scheduler.currentTime); scheduler.runCurrent()
        writer.record(timeUs,"advance","logical event time and coroutine scheduler (1ms); no animation frame")
    }
    var pressed=false
    init {
        registerSkikoComposeImplementation(); recomposer=FrameRecomposer(scope.coroutineContext)
        // Empty has a zero WindowInfo.containerSize. Scene size controls layout but does
        // not populate WindowInfo; Core uses it separately for desktop shadow lighting.
        // Preserve explicit caller window environments (including popup/dialog contexts).
        val context=if(platformContext.windowInfo.containerSize==IntSize.Zero) object:PlatformContext by platformContext {
            override val windowInfo=object:WindowInfo by platformContext.windowInfo {
                override val containerSize=IntSize(surface.width,surface.height)
            }
        } else platformContext
        scene=CanvasLayersComposeScene(recomposer,Density(RenderScale.toFloat()),size=IntSize(surface.width,surface.height),platformContext=context)
    }
    fun mount(content:@Composable ()->Unit) {
        scene.setContent(content=content); draw(); writer.record(timeUs,"mount","${surface.width/RenderScale}x${surface.height/RenderScale} DIP; density=$RenderScale; no animation frame")
    }
    fun frame(time:Long) {
        require(time>=timeUs); timeUs=time
        val target=(time.coerceAtLeast(0))/1000
        scheduler.advanceTimeBy(target-scheduler.currentTime); scheduler.runCurrent()
        recomposer.performFrame(Math.addExact(1_000_000_000L,Math.multiplyExact(time,1000L))); frameTimeUs=time
        writer.record(timeUs,"frame","origin=1000000us; one waiter batch",buildJsonObject { put("frameTimeUs",frameTimeUs);put("originUs",1_000_000) }); draw()
    }
    fun draw() {
        repeat(20) {
            Snapshot.sendApplyNotifications(); scheduler.runCurrent(); scene.measureAndLayout()
            surface.canvas.clear(Color.TRANSPARENT); scene.draw(surface.canvas.asComposeCanvas()); scheduler.runCurrent()
            // Popup.onPlaced writes its anchor bounds during layout. Notify the
            // snapshot observers before deciding whether that draw has settled;
            // the write can request another popup measure/layout in this turn.
            Snapshot.sendApplyNotifications(); scheduler.runCurrent()
            if(!scene.hasPendingMeasureOrLayout && !scene.hasPendingDraw) {
                writer.record(timeUs,"draw","snapshot/scheduler/layout/render settled; no animation frame")
                return
            }
        }
        error("Compose measure/layout/draw did not finish within 20 render turns.")
    }
    fun input(operation:String, position:Offset) {
        if(operation=="down") pressed=true
        if(operation=="up") pressed=false
        scene.sendPointerEvent(when(operation) { "down"->PointerEventType.Press; "up"->PointerEventType.Release; "move"->PointerEventType.Move; else->error(operation) },
            position*RenderScale.toFloat(), buttons=PointerButtons(isPrimaryPressed=pressed), timeMillis=1000+timeUs.coerceAtLeast(0)/1000,
            type=PointerType.Mouse, button=if(operation=="down" || operation=="up") PointerButton.Primary else null)
        scheduler.runCurrent(); writer.record(timeUs,"input","$operation (${position.x},${position.y})",buildJsonObject {
            put("action",operation);put("x",position.x);put("y",position.y);put("unit","dip");put("space","scene");put("pointerType","mouse");
            put("button",if(operation=="move") JsonNull else JsonPrimitive("primary")) })
    }
    fun snapshot(id:String, stage:String, points:List<ProbePoint>, image:Boolean=true):Sample {
        val rasters=mutableListOf<RasterObservation>()
        if(image || writer.extraImages) {
            val path="images/$id.png"; val file=File(writer.output,path); file.parentFile.mkdirs()
            surface.makeImageSnapshot().use { img -> img.encodeToData(EncodedImageFormat.PNG)?.use { file.writeBytes(it.bytes) } ?: error("PNG encode failed") }
            // Decode the saved PNG. Canonical bytes describe this artifact, not private native memory.
            rasters+=rasterFromPng(writer,"full",path)
            writer.record(timeUs,"capture","passive surface snapshot; no draw/frame")
        }
        return Sample(id,timeUs,stage,points,rasters,frameTimeUs,listOf(RasterCollection("full",if(rasters.isEmpty()) "notRequested" else "captured",
            if(rasters.isEmpty()) "Case requested a measurement-only sample." else null)))
    }
    override fun close() { scene.close(); recomposer.close(); surface.close(); scope.cancel(); scheduler.runCurrent(); writer.record(timeUs,"dispose","scene/recomposer/scope") }
}

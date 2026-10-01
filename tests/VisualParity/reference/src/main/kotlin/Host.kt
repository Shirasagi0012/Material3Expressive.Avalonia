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
class ComposeHost(val writer:CaptureWriter, width:Int=240, height:Int=160):AutoCloseable {
    val scheduler=TestCoroutineScheduler()
    val scope=CoroutineScope(UnconfinedTestDispatcher(scheduler) + CoroutineExceptionHandler { _,e -> writer.errors+=e.stackTraceToString() })
    val recomposer:FrameRecomposer
    val scene:ComposeScene
    val surface=Surface.makeRasterN32Premul(width,height)
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
        scene=CanvasLayersComposeScene(recomposer,Density(1f),size=IntSize(width,height))
    }
    fun mount(content:@Composable ()->Unit) {
        scene.setContent(content=content); draw(); writer.record(timeUs,"mount","240x160 DIP; density=1; no animation frame")
    }
    fun frame(time:Long) {
        require(time>=timeUs); timeUs=time
        val target=(time.coerceAtLeast(0))/1000
        scheduler.advanceTimeBy(target-scheduler.currentTime); scheduler.runCurrent()
        recomposer.performFrame(Math.addExact(1_000_000_000L,Math.multiplyExact(time,1000L))); frameTimeUs=time
        writer.record(timeUs,"frame","origin=1000000us; one waiter batch"); draw()
    }
    fun draw() {
        Snapshot.sendApplyNotifications(); scheduler.runCurrent(); scene.measureAndLayout()
        surface.canvas.clear(Color.TRANSPARENT); scene.draw(surface.canvas.asComposeCanvas()); scheduler.runCurrent()
        writer.record(timeUs,"draw","snapshot/scheduler/layout/render; no animation frame")
    }
    fun input(operation:String, position:Offset) {
        if(operation=="down") pressed=true
        if(operation=="up") pressed=false
        scene.sendPointerEvent(when(operation) { "down"->PointerEventType.Press; "up"->PointerEventType.Release; "move"->PointerEventType.Move; else->error(operation) },
            position, buttons=PointerButtons(isPrimaryPressed=pressed), timeMillis=1000+timeUs.coerceAtLeast(0)/1000,
            type=PointerType.Mouse, button=if(operation=="down" || operation=="up") PointerButton.Primary else null)
        scheduler.runCurrent(); writer.record(timeUs,"input","$operation (${position.x},${position.y})")
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
        return Sample(id,timeUs,stage,points,rasters,frameTimeUs)
    }
    override fun close() { scene.close(); recomposer.close(); surface.close(); scope.cancel(); scheduler.runCurrent(); writer.record(timeUs,"dispose","scene/recomposer/scope") }
}

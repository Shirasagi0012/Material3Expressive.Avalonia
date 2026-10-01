@file:OptIn(androidx.compose.ui.text.ExperimentalTextApi::class)
package parity
import androidx.compose.animation.core.*
import androidx.compose.foundation.interaction.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.*
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.*
import androidx.compose.ui.text.platform.Font
import androidx.compose.ui.unit.*
import kotlinx.coroutines.launch
import java.io.File
import kotlinx.serialization.json.*
object Cases {
    val entries:Map<String,suspend (CaptureWriter)->Unit> = mapOf(
        "surfaceStatic" to ::surfaceStatic,"buttonEnabled" to ::buttonEnabled,"buttonDisabled" to ::buttonDisabled,
        "buttonPressed" to ::buttonPressed,"textWrapped" to ::textWrapped,"textParagraphBreaks" to ::textParagraphBreaks,"motionInterrupted" to ::motionInterrupted,"directObservation" to ::directObservation)
    fun font(w:CaptureWriter)=FontFamily(listOf(400,500,700).map { weight -> Font(File(w.root,"modules/MaterialTypography.Roboto/Assets/Roboto.ttf"),
        weight=FontWeight(weight),variationSettings=FontVariation.Settings(FontVariation.weight(weight))) })
    @Composable fun Theme(writer:CaptureWriter, content:@Composable ()->Unit) {
        val family=remember { font(writer) }
        val type=Typography().let { it.copy(labelLarge=it.labelLarge.copy(fontFamily=family)) }
        MaterialTheme(colorScheme=lightColorScheme(),typography=type) {
            Box(Modifier.fillMaxSize().background(Color.White),contentAlignment=Alignment.Center) { content() }
        }
    }
    suspend fun surfaceStatic(w:CaptureWriter) {
        ComposeHost(w).use { h ->
            val probe=Probe()
            h.mount { Theme(w) { Surface(Modifier.size(100.dp,50.dp).then(probe.modifier()),shape=RoundedCornerShape(12.dp),color=Color(0xff6750a4)) {} } }
            h.frame(-2000); h.frame(-1000); h.frame(0)
            w.submit(h.snapshot("initial","mounted",listOf(ProbePoint("surface","container",null,probe.geometry()+
                vector("requestedColor","color",listOf(103.0,80.0,164.0,255.0),"srgba8","none","properties",source="declared")))))
        }
    }
    class ButtonProbe {
        val allocation=Probe(); val label=Probe(); var clicks=0
        val interactions=MutableInteractionSource(); val presses=mutableSetOf<PressInteraction.Press>(); val hovers=mutableSetOf<HoverInteraction.Enter>()
        fun points(enabled:Boolean)=listOf(ProbePoint("button","allocation",null,allocation.geometry()+listOf(flag("enabled",enabled,source="declared"),number("clicks",clicks.toDouble(),"count"))),
            label.textPoint("label","container"))+buttonMeasurements(allocation,presses.isNotEmpty(),hovers.isNotEmpty())
        @Composable fun Content(enabled:Boolean) {
            LaunchedEffect(interactions) {
                interactions.interactions.collect { event -> when(event) {
                    is PressInteraction.Press -> presses.add(event)
                    is PressInteraction.Release -> presses.remove(event.press)
                    is PressInteraction.Cancel -> presses.remove(event.press)
                    is HoverInteraction.Enter -> hovers.add(event)
                    is HoverInteraction.Exit -> hovers.remove(event.enter)
                } }
            }
            Button({clicks++},ButtonDefaults.shapes(),allocation.modifier(),enabled=enabled,interactionSource=interactions) {
                Text("Save",label.modifier(),onTextLayout={label.layout=it})
            }
        }
    }
    suspend fun buttonEnabled(w:CaptureWriter) {
        ComposeHost(w).use { h -> val b=ButtonProbe(); h.mount { Theme(w) { b.Content(true) } }
            h.frame(-2000); h.frame(-1000); h.frame(0); w.submit(h.snapshot("initial","enabled",b.points(true))) }
    }
    suspend fun buttonDisabled(w:CaptureWriter) {
        ComposeHost(w).use { h -> val b=ButtonProbe(); h.mount { Theme(w) { b.Content(false) } }
            h.frame(-2000); h.frame(-1000); h.frame(0); w.submit(h.snapshot("initial","disabled",b.points(false))) }
    }
    suspend fun buttonPressed(w:CaptureWriter) {
        ComposeHost(w).use { h -> val b=ButtonProbe(); h.mount { Theme(w) { b.Content(true) } }
            h.frame(-2000); h.frame(-1000); h.frame(0); w.submit(h.snapshot("initial","enabled",b.points(true)))
            val box=b.allocation.geometry()[0].numbers!!; val center=Offset((box[0]+box[2]/2).toFloat(),(box[1]+box[3]/2).toFloat())
            h.input("move",center); h.input("down",center)
            for(time in listOf(16000L,32000L,80000L)) { h.frame(time); w.submit(h.snapshot("pressed-$time","pressed",b.points(true),time!=32000L)) }
            h.advanceTo(88000); h.input("up",center); h.frame(96000); w.submit(h.snapshot("released","released",b.points(true)))
            h.frame(300000); w.submit(h.snapshot("late-300000","released",b.points(true)))
        }
    }
    suspend fun textWrapped(w:CaptureWriter)=textCase(w,"Compose parity\nWrap a longer line with spaces.","wrapped")
    suspend fun textParagraphBreaks(w:CaptureWriter)=textCase(w,"A \r\n\r\nB 😀  \nC\u2028D\u2029","paragraph-breaks")
    suspend fun textCase(w:CaptureWriter,content:String,stage:String) {
        ComposeHost(w).use { h -> val p=Probe(); val family=font(w)
            h.mount { Theme(w) { Text(content,Modifier.width(120.dp).then(p.modifier()),
                fontFamily=family,fontSize=14.sp,fontWeight=FontWeight.Normal,lineHeight=20.sp,letterSpacing=0.sp,color=Color(0xff6750a4),onTextLayout={p.layout=it}) } }
            h.frame(-2000); h.frame(-1000); h.frame(0); w.submit(h.snapshot("initial",stage,listOf(p.textPoint("label",null))))
        }
    }
    suspend fun motionInterrupted(w:CaptureWriter) {
        ComposeHost(w).use { h -> val p=Probe(); val value=Animatable(0f); var animationScope:kotlinx.coroutines.CoroutineScope?=null
            h.mount { animationScope=rememberCoroutineScope()
                Box(Modifier.fillMaxSize().background(Color.White)) {
                    Box(Modifier.offset(x=(24+value.value).dp,y=64.dp).size(18.dp).then(p.modifier()).background(Color(0xff6750a4)))
                } }
            h.frame(-2000); h.frame(-1000)
            fun target(valueTo:Float) { w.record(h.timeUs,"target",valueTo.toString(),buildJsonObject { put("value",valueTo);put("unit","dip") }); animationScope!!.launch { value.animateTo(valueTo,tween(100,easing=LinearEasing)) } }
            fun sample(id:String,image:Boolean) { w.submit(h.snapshot(id,"interrupted-tween",listOf(ProbePoint("marker","container",null,p.geometry()+listOf(
                number("value",value.value.toDouble(),"dip",category="motion"),number("velocity",value.velocity.toDouble(),"dip/s",category="motion"),flag("running",value.isRunning,"motion")))),image)) }
            target(100f)
            for(time in listOf(0L,16000L,40000L)) { h.frame(time); sample("forward-$time",time!=16000L) }
            target(-20f)
            for(time in listOf(56000L,80000L,140000L,160000L)) { h.frame(time); sample("reverse-$time",time!=80000L) }
        }
    }
    suspend fun directObservation(w:CaptureWriter) {
        val path="images/pixels.png"; val file=File(w.output,path); file.parentFile.mkdirs()
        val info=org.jetbrains.skia.ImageInfo(2,1,org.jetbrains.skia.ColorType.RGBA_8888,org.jetbrains.skia.ColorAlphaType.UNPREMUL)
        org.jetbrains.skia.Bitmap().use { bitmap ->
            check(bitmap.installPixels(info,byteArrayOf(80,120,200.toByte(),0,81,121,201.toByte(),128.toByte()),8))
            org.jetbrains.skia.Image.makeFromBitmap(bitmap).use { image ->
                checkNotNull(image.encodeToData(org.jetbrains.skia.EncodedImageFormat.PNG)).use { file.writeBytes(it.bytes) }
            }
        }
        val raster=rasterFromPng(w,"full",path).copy(contribution="declared-pixel-fixture")
        w.submit(Sample("initial",0,"author-built",listOf(ProbePoint("custom","measurement",null,listOf(number("answer",42.0,"count",source="declared")))),listOf(raster)))
    }
}

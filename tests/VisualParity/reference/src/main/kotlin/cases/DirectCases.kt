package parity

import java.io.File
import kotlinx.serialization.Serializable

@Serializable data class DirectConfig(val answer:Double)
@ParitySuite("protocol")
class DirectCases {
    @ParityScenario("direct")
    suspend fun directObservation(w:CaptureWriter) {
        val config=w.readConfig<DirectConfig>()
        val path="images/pixels.png"; val file=File(w.output,path); file.parentFile.mkdirs()
        val info=org.jetbrains.skia.ImageInfo(2,1,org.jetbrains.skia.ColorType.RGBA_8888,org.jetbrains.skia.ColorAlphaType.UNPREMUL)
        org.jetbrains.skia.Bitmap().use { bitmap ->
            check(bitmap.installPixels(info,byteArrayOf(80,120,200.toByte(),0,81,121,201.toByte(),128.toByte()),8))
            org.jetbrains.skia.Image.makeFromBitmap(bitmap).use { image ->
                checkNotNull(image.encodeToData(org.jetbrains.skia.EncodedImageFormat.PNG)).use { file.writeBytes(it.bytes) }
            }
        }
        val raster=rasterFromPng(w,"full",path).copy(contribution="declared-pixel-fixture")
        w.submit(Sample("initial",0,"author-built",listOf(ProbePoint("custom","measurement",null,listOf(number("answer",config.answer,"count",source="declared")))),listOf(raster)))
    }
}

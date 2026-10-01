package parity
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.text.TextLayoutResult
// Ordinary holders. Recording coordinates or text layouts must not invalidate composition.
class Probe {
    var coordinates:LayoutCoordinates?=null
    var layout:TextLayoutResult?=null
    fun modifier()=Modifier.onGloballyPositioned { coordinates=it }
    fun geometry():List<Measurement> {
        val c=checkNotNull(coordinates)
        val corners=listOf(Offset.Zero,Offset(c.size.width.toFloat(),0f),Offset(c.size.width.toFloat(),c.size.height.toFloat()),Offset(0f,c.size.height.toFloat()))
            .map { c.localToRoot(it) }
        val left=corners.minOf { it.x }.toDouble(); val top=corners.minOf { it.y }.toDouble()
        return listOf(vector("box","rect",listOf(left,top,corners.maxOf { it.x }-left,corners.maxOf { it.y }-top),"dip","scene","geometry","layout.box.aabb.unclipped"),
            vector("corners","quad",corners.flatMap { listOf(it.x.toDouble(),it.y.toDouble()) },"dip","scene","geometry","layout.box.corners.unclipped"),
            vector("size","point",listOf(c.size.width.toDouble(),c.size.height.toDouble()),"dip","local","geometry","layout.box.local-size"))
    }
    fun textPoint(id:String,parentId:String?):ProbePoint {
        val t=checkNotNull(layout); val style=t.layoutInput.style
        val fields=(geometry()+listOf(text("content",t.layoutInput.text.text,"text"),
            number("fontSize",style.fontSize.value.toDouble(),"sp",category="text"),
            number("fontWeight",(style.fontWeight?.weight ?: 400).toDouble(),"weight",category="text"),
            number("letterSpacing",style.letterSpacing.value.toDouble(),"sp",category="text"),
            number("lineCount",t.lineCount.toDouble(),"count",category="text"),
            unavailable("resolvedFonts","text","strings","none","none","TextLayoutResult does not expose resolved font families.","text.resolved-font-families"),
            unavailable("drawOrigin","text","point","dip","scene","No draw-origin reader installed."),
            unavailable("glyphInk","text","rect","dip","text","No glyph ink reader installed; caret boxes are not ink boxes."))).toMutableList()
        val color=style.color
        fields+=vector("color","color",listOf(color.red.toDouble(),color.green.toDouble(),color.blue.toDouble(),color.alpha.toDouble()),
            "srgb","none","text","text.foreground.solid-srgba","TextLayoutResult.layoutInput.style.color")
        for(i in 0 until t.lineCount) {
            val p="line.$i."
            fields+=listOf(vector(p+"nativeRange","range",listOf(t.getLineStart(i).toDouble(),t.getLineEnd(i,visibleEnd=false).toDouble()),
                "utf16","text","text","compose.line.native-range","TextLayoutResult.getLineStart/getLineEnd(visibleEnd=false)"),
                vector(p+"range","range",listOf(t.getLineStart(i).toDouble(),t.getLineEnd(i,visibleEnd=false).toDouble()),"utf16","text","text","text.line.source-range"),
                number(p+"top",t.getLineTop(i).toDouble(),"dip","text","text","text.line.top"),
                number(p+"height",(t.getLineBottom(i)-t.getLineTop(i)).toDouble(),"dip","text","text","text.line.height"),
                number(p+"baseline",t.getLineBaseline(i).toDouble(),"dip","text","text","text.line.baseline"),
                number(p+"width",(t.getLineRight(i)-t.getLineLeft(i)).toDouble(),"dip","text","text","compose.line.left-right-extent"),
                flag(p+"ellipsized",t.isLineEllipsized(i),"text","text.line.ellipsized"))
        }
        return ProbePoint(id,"text-block",parentId,fields)
    }
}

// Canonical conversion is usable without the scene host, including author-owned raster captures.
fun rasterFromPng(writer:CaptureWriter,id:String,path:String):RasterObservation {
    val file=java.io.File(writer.output,path); val pixels=path.removeSuffix(".png")+".rgba"
    org.jetbrains.skia.Data.makeFromBytes(file.readBytes()).use { png -> org.jetbrains.skia.Codec.makeFromData(png).use { codec ->
        val source=codec.imageInfo
        val info=org.jetbrains.skia.ImageInfo(source.width,source.height,org.jetbrains.skia.ColorType.RGBA_8888,
            org.jetbrains.skia.ColorAlphaType.UNPREMUL,org.jetbrains.skia.ColorSpace.sRGB)
        org.jetbrains.skia.Bitmap().use { bitmap ->
            check(bitmap.allocPixels(info)); codec.readPixels(bitmap)
            java.io.File(writer.output,pixels).writeBytes(checkNotNull(bitmap.readPixels(info)))
            return RasterObservation(id,path,pixels,bitmap.width,bitmap.height)
        }
    } }
}

package parity

import org.jetbrains.skia.*

// Independently identify the curve/fallback paths in the locked native artifact.
// Neither assertion uses Avalonia, the managed tessellator or stored expected images.
fun verifyNativeShadowCurves() {
    fun capture(draw: (Canvas) -> Unit): ByteArray = Surface.makeRasterN32Premul(640, 640).use { surface ->
        surface.canvas.clear(0xffffffff.toInt())
        draw(surface.canvas)
        surface.makeImageSnapshot().use { image -> image.encodeToData()!!.use { it.bytes } }
    }

    for (elevation in listOf(6f, 16f, 24f, 40f)) {
        fun curvedShadow(polyline: Boolean) = capture { canvas ->
            PathBuilder().use { builder ->
                builder.moveTo(0f, 160f).lineTo(0f, 64f)
                fun corner(cx: Float, cy: Float, x: Float, y: Float) {
                    if (polyline) builder.lineTo(cx, cy).lineTo(x, y)
                    else builder.quadTo(cx, cy, x, y)
                }
                corner(0f, 0f, 64f, 0f)
                builder.lineTo(160f, 0f)
                corner(224f, 0f, 224f, 64f)
                builder.lineTo(224f, 160f)
                corner(224f, 224f, 160f, 224f)
                builder.lineTo(64f, 224f)
                corner(0f, 224f, 0f, 160f)
                builder.closePath()
                builder.detach().use { path ->
                    canvas.translate(208f, 208f)
                    ShadowUtils.drawShadow(canvas, path, Point3(0f, 0f, elevation * 4),
                        Point3(320f, 0f, 1600f), 3200f, 0x09000000, 0x30000000,
                        transparentOccluder = true, geometricOnly = false)
                }
            }
        }
        check(curvedShadow(false).contentEquals(curvedShadow(true))) {
            "Locked native curve branch changed at elevation $elevation; recheck SK_GANESH and the managed port."
        }
    }

    for (physical in listOf(false, true)) {
        val native = capture { canvas ->
            val size = if (physical) 144f else 72f
            PathBuilder().use { builder ->
                builder.addRRect(RRect.makeLTRB(0f, 0f, size, size, size / 2), PathDirection.CLOCKWISE, 6)
                builder.detach().use { path ->
                    canvas.translate(64f, 64f)
                    if (!physical) canvas.scale(2f, 2f)
                    ShadowUtils.drawShadow(canvas, path, Point3(0f, 0f, 12f),
                        Point3(320f, 0f, 1000f), 1600f, 0x09000000, 0,
                        transparentOccluder = false, geometricOnly = false)
                }
            }
        }
        val fallback = capture { canvas ->
            // SkShadowUtils.cpp: ambient failure -> device-space stroke/fill + blur.
            val outset = 6f
            val blurRadius = .5f * outset * (1f + 12f / 128f)
            Paint().use { paint ->
                paint.color = 0x09000000
                paint.mode = PaintMode.STROKE_AND_FILL
                paint.strokeWidth = .5f * (outset - blurRadius)
                MaskFilter.makeBlur(FilterBlurMode.NORMAL, blurRadius * .57735f + .5f, false).use { filter ->
                    paint.maskFilter = filter
                    PathBuilder().use { builder ->
                        builder.addRRect(RRect.makeLTRB(64f, 64f, 208f, 208f, 72f), PathDirection.CLOCKWISE, 6)
                        builder.detach().use { canvas.drawPath(it, paint) }
                    }
                }
            }
        }
        check(native.contentEquals(fallback)) { "Locked native circle fallback changed; physical=$physical" }
    }
    println("Native shadow tests passed: four quad/control-point comparisons and two circle blur fallbacks, pixel-exact.")
}

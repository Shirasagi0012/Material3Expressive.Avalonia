package parity

import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.*
import androidx.compose.ui.unit.*

@ParitySuite("carousel-shape")
class CarouselShapeCases {
    @ParityScenario("classic") suspend fun classic(writer:CaptureWriter)=CarouselCases().capture(writer)
    @ParityScenario("multi") suspend fun multi(writer:CaptureWriter)=MultiAspectCarouselCases().capture(writer)
}

internal fun carouselTestShape(kind:String):Shape=when(kind) {
    "asymmetric" -> CutCornerShape(40.dp,8.dp,24.dp,48.dp)
    "percent" -> CutCornerShape(50)
    "rounded" -> RoundedCornerShape(40.dp,8.dp,24.dp,48.dp)
    "round-percent" -> RoundedCornerShape(50)
    "zero" -> CutCornerShape(0.dp)
    else -> object:Shape {
        override fun createOutline(size:Size,layoutDirection:LayoutDirection,density:Density):Outline {
            val path=Path().apply {
                when(kind) {
                    "offset" -> { moveTo(8f,4f);lineTo(108f,4f);lineTo(108f,52f);lineTo(8f,52f);close() }
                    "curve" -> { moveTo(0f,32f);cubicTo(0f,0f,120f,0f,120f,32f);cubicTo(120f,64f,0f,64f,0f,32f);close() }
                    "hole" -> { fillType=PathFillType.EvenOdd;moveTo(0f,0f);lineTo(120f,0f);lineTo(120f,64f);lineTo(0f,64f);close()
                        moveTo(40f,20f);lineTo(80f,20f);lineTo(80f,44f);lineTo(40f,44f);close() }
                }
                transform(Matrix().apply { scale(density.density,density.density) })
            }
            return Outline.Generic(path)
        }
    }
}

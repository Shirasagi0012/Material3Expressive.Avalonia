@file:OptIn(androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package parity

import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.ChipShapes
import androidx.compose.ui.unit.dp

@ParitySuite("chip-shape")
class ChipShapeCases {
    @ParityScenario("static") suspend fun static(writer:CaptureWriter) = ChipCases().static(writer)
    @ParityScenario("behavior") suspend fun behavior(writer:CaptureWriter) = ChipCases().behavior(writer)
}

internal fun chipTestShapes(set:String):ChipShapes = when(set) {
    "cut" -> ChipShapes(CutCornerShape(12.dp,4.dp,8.dp,2.dp),CutCornerShape(50),CutCornerShape(4.dp))
    "round-percent" -> ChipShapes(RoundedCornerShape(20,10,40,30),RoundedCornerShape(50),RoundedCornerShape(10))
    "mixed" -> ChipShapes(CutCornerShape(12.dp),RoundedCornerShape(50),CutCornerShape(4.dp))
    "generic" -> ChipShapes(carouselTestShape("curve"),CutCornerShape(12.dp),RoundedCornerShape(4.dp))
    else -> error(set)
}

package material3expressive.compose.reference.probes

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.text.TextLayoutResult

class ProbeRegistry {
    private val _geometries = linkedMapOf<String, BoundsPx>()
    private val _textLayouts = linkedMapOf<String, TextLayoutResult>()
    private val _focused = linkedMapOf<String, Boolean>()
    private val _shadows = linkedMapOf<String, ShadowProbeSpec>()
    private val _visualScales = linkedMapOf<String, VisualScale>()

    fun geometry(id: String): Modifier =
        Modifier.onGloballyPositioned { coordinates ->
            // boundsInRoot() calls localBoundingBoxOf() with clipping enabled. That truncates an
            // otherwise valid tab/text probe at a scroll viewport edge, while Avalonia reports
            // the visual's raw root-relative bounds. Capture the layout origin and measured size
            // instead so the cross-toolkit structural contract describes the same un-clipped box.
            val position = coordinates.positionInRoot()
            _geometries[id] = BoundsPx(
                position.x,
                position.y,
                position.x + coordinates.size.width,
                position.y + coordinates.size.height,
            )
            _visualScales[id] = VisualScale(1f, 1f)
        }

    fun text(id: String): Modifier = geometry(id)

    fun transformedGeometry(id: String): Modifier =
        Modifier.onGloballyPositioned { coordinates ->
            val width = coordinates.size.width.toFloat()
            val height = coordinates.size.height.toFloat()
            val corners =
                listOf(
                    coordinates.localToRoot(Offset.Zero),
                    coordinates.localToRoot(Offset(width, 0f)),
                    coordinates.localToRoot(Offset(0f, height)),
                    coordinates.localToRoot(Offset(width, height)),
                )
            val left = corners.minOf { it.x }
            val top = corners.minOf { it.y }
            val right = corners.maxOf { it.x }
            val bottom = corners.maxOf { it.y }
            _geometries[id] = BoundsPx(left, top, right, bottom)
            _visualScales[id] =
                VisualScale(
                    x = if (width == 0f) 1f else (right - left) / width,
                    y = if (height == 0f) 1f else (bottom - top) / height,
                )
        }

    fun onTextLayout(id: String): (TextLayoutResult) -> Unit = { result ->
        recordTextLayout(id, result)
    }

    fun geometryBounds(id: String): BoundsPx? {
        val bounds = _geometries[id] ?: return null
        return bounds.takeUnless { it.right <= it.left || it.bottom <= it.top }
    }

    fun textLayoutResult(id: String): TextLayoutResult? = _textLayouts[id]

    fun focused(id: String): Boolean? = _focused[id]

    fun shadow(id: String): ShadowProbeSpec? = _shadows[id]

    fun visualScale(id: String): VisualScale = _visualScales[id] ?: VisualScale(1f, 1f)

    fun recordTextLayout(id: String, result: TextLayoutResult) {
        _textLayouts[id] = result
    }

    fun recordFocused(id: String, focused: Boolean) {
        _focused[id] = focused
    }

    fun recordShadow(id: String, shadow: ShadowProbeSpec) {
        _shadows[id] = shadow
    }
}

data class ShadowProbeSpec(
    val shapeKind: String,
    val cornerRadiusDp: Float,
    val topLeftCornerRadiusDp: Float = cornerRadiusDp,
    val topRightCornerRadiusDp: Float = cornerRadiusDp,
    val bottomRightCornerRadiusDp: Float = cornerRadiusDp,
    val bottomLeftCornerRadiusDp: Float = cornerRadiusDp,
    val elevationDp: Float,
    val ambientColorArgb: String = "#FF000000",
    val spotColorArgb: String = "#FF000000",
    val opacity: Float = 1f,
    val clipProbeId: String? = null,
    val occluderDrawn: Boolean = true,
)

data class BoundsPx(
    val left: Float,
    val top: Float,
    val right: Float,
    val bottom: Float,
)

data class VisualScale(val x: Float, val y: Float)

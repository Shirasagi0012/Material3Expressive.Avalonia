package material3expressive.compose.reference.probes

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.text.TextLayoutResult

class ProbeRegistry {
    private val _geometries = linkedMapOf<String, BoundsPx>()
    private val _textLayouts = linkedMapOf<String, TextLayoutResult>()
    private val _focused = linkedMapOf<String, Boolean>()
    private val _shadows = linkedMapOf<String, ShadowProbeSpec>()

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
        }

    fun text(id: String): Modifier = geometry(id)

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

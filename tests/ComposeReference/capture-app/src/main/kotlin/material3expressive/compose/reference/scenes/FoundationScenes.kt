package material3expressive.compose.reference.scenes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.RectangleShape
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry
import material3expressive.compose.reference.probes.ShadowProbeSpec

private const val PARENT_CLIP_PROBE = "component-parent-clip"

fun ReferenceCaseRegistry.registerFoundationScenes() {
    register(
        CaseContract(
            id = "foundation/color/primary-roles/light",
            viewportWidthDp = 240,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            PrimaryRoleSwatches(probes)
        },
    )

    register(
        CaseContract(
            id = "foundation/color/primary-roles/dark",
            viewportWidthDp = 240,
            viewportHeightDp = 96,
            dark = true,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            PrimaryRoleSwatches(probes)
        },
    )

    listOf(1.dp, 3.dp, 6.dp).forEach { elevation ->
        registerElevationScene(
            id = "foundation/elevation/rounded-rectangle/level${elevation.value.toInt()}/light",
            elevation = elevation,
            shape = RoundedCornerShape(20.dp),
        )
    }
    registerElevationScene(
        id = "foundation/elevation/pill/level1/fractional-width/light",
        elevation = 1.dp,
        shape = CircleShape,
        cornerRadius = 28.dp,
        width = 90.5.dp,
        height = 56.dp,
        viewportHeightDp = 96,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/pill/level1/fractional-width/ambient-only/light",
        ambientColor = Color.Black,
        spotColor = Color.Transparent,
        elevation = 1.dp,
        shape = CircleShape,
        cornerRadius = 28.dp,
        width = 90.5.dp,
        height = 56.dp,
        viewportHeightDp = 96,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/pill/level1/fractional-width/spot-only/light",
        ambientColor = Color.Transparent,
        spotColor = Color.Black,
        elevation = 1.dp,
        shape = CircleShape,
        cornerRadius = 28.dp,
        width = 90.5.dp,
        height = 56.dp,
        viewportHeightDp = 96,
    )
    registerElevationScene(
        id = "foundation/elevation/rounded-rectangle/level6/dark",
        elevation = 6.dp,
        shape = RoundedCornerShape(20.dp),
        dark = true,
    )
    registerElevationScene(
        id = "foundation/elevation/rectangle/level12/light",
        elevation = 12.dp,
        shape = RectangleShape,
        cornerRadius = 0.dp,
    )
    registerElevationScene(
        id = "foundation/elevation/rounded-rectangle/level6/scale-1.5/light",
        elevation = 6.dp,
        shape = RoundedCornerShape(20.dp),
        renderScale = 1.5f,
    )
    registerOpacityElevationScene(
        id = "foundation/elevation/rounded-rectangle/level6/opacity-50/light",
        elevation = 6.dp,
        opacity = 0.5f,
        shape = RoundedCornerShape(20.dp),
    )
    registerElevationScene(
        id = "foundation/elevation/circle/level6/light",
        elevation = 6.dp,
        shape = CircleShape,
        cornerRadius = 36.dp,
        width = 72.dp,
        height = 72.dp,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/circle/level6/ambient-only/light",
        ambientColor = Color.Black,
        spotColor = Color.Transparent,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/circle/level6/spot-only/light",
        ambientColor = Color.Transparent,
        spotColor = Color.Black,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/circle/level6/ambient-only/shadow-only/light",
        ambientColor = Color.Black,
        spotColor = Color.Transparent,
        drawOccluder = false,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/circle/level6/spot-only/shadow-only/light",
        ambientColor = Color.Transparent,
        spotColor = Color.Black,
        drawOccluder = false,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/rounded-rectangle/level6/ambient-only/light",
        ambientColor = Color.Black,
        spotColor = Color.Transparent,
        shape = RoundedCornerShape(20.dp),
        cornerRadius = 20.dp,
        width = 96.dp,
        height = 64.dp,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/rounded-rectangle/level6/spot-only/light",
        ambientColor = Color.Transparent,
        spotColor = Color.Black,
        shape = RoundedCornerShape(20.dp),
        cornerRadius = 20.dp,
        width = 96.dp,
        height = 64.dp,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/rounded-rectangle/level6/ambient-only/shadow-only/light",
        ambientColor = Color.Black,
        spotColor = Color.Transparent,
        drawOccluder = false,
        shape = RoundedCornerShape(20.dp),
        cornerRadius = 20.dp,
        width = 96.dp,
        height = 64.dp,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/rounded-rectangle/level6/spot-only/shadow-only/light",
        ambientColor = Color.Transparent,
        spotColor = Color.Black,
        drawOccluder = false,
        shape = RoundedCornerShape(20.dp),
        cornerRadius = 20.dp,
        width = 96.dp,
        height = 64.dp,
    )
    registerElevationScene(
        id = "foundation/elevation/rounded-rectangle/level6/parent-clipped/light",
        elevation = 6.dp,
        shape = RoundedCornerShape(20.dp),
        parentClipped = true,
    )
}

private fun ReferenceCaseRegistry.registerOpacityElevationScene(
    id: String,
    elevation: Dp,
    opacity: Float,
    shape: Shape,
) {
    register(
        CaseContract(
            id = id,
            viewportWidthDp = 240,
            viewportHeightDp = 180,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "shadow")),
        ),
        ReferenceScene { probes ->
            OpacityElevationSurface(probes, elevation, opacity, shape, 20.dp)
        },
    )
}

private fun ReferenceCaseRegistry.registerShadowComponentScene(
    id: String,
    ambientColor: Color,
    spotColor: Color,
    drawOccluder: Boolean = true,
    shape: Shape = CircleShape,
    width: Dp = 72.dp,
    height: Dp = 72.dp,
    elevation: Dp = 6.dp,
    viewportHeightDp: Int = 180,
    cornerRadius: Dp = 36.dp,
) {
    register(
        CaseContract(
            id = id,
            viewportWidthDp = 240,
            viewportHeightDp = viewportHeightDp,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "shadow")),
        ),
        ReferenceScene { probes ->
            probes.recordShadow(
                "component",
                shadowSpec(
                    width,
                    height,
                    cornerRadius,
                    elevation,
                    ambientColor,
                    spotColor,
                    occluderDrawn = drawOccluder,
                ),
            )
            var modifier = Modifier
                .requiredSize(width, height)
                .shadow(
                    elevation = elevation,
                    shape = shape,
                    clip = false,
                    ambientColor = ambientColor,
                    spotColor = spotColor,
                )
            if (drawOccluder) {
                modifier = modifier.background(MaterialTheme.colorScheme.surface, shape)
            }
            Box(modifier = modifier.then(probes.geometry("component")))
        },
    )
}

private fun ReferenceCaseRegistry.registerElevationScene(
    id: String,
    elevation: Dp,
    shape: Shape,
    dark: Boolean = false,
    width: Dp = 96.dp,
    height: Dp = 64.dp,
    parentClipped: Boolean = false,
    viewportHeightDp: Int = 180,
    renderScale: Float = 2.0f,
    cornerRadius: Dp = 20.dp,
) {
    register(
        CaseContract(
            id = id,
            viewportWidthDp = 240,
            viewportHeightDp = viewportHeightDp,
            dark = dark,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "shadow")),
            renderScale = renderScale,
        ),
        ReferenceScene { probes ->
            if (parentClipped) {
                Box(
                    modifier = Modifier.requiredSize(112.dp, 80.dp)
                        .then(probes.geometry(PARENT_CLIP_PROBE))
                        .clip(RectangleShape),
                    contentAlignment = Alignment.Center,
                ) {
                    ElevationSurface(probes, elevation, shape, width, height, cornerRadius, PARENT_CLIP_PROBE)
                }
            } else {
                ElevationSurface(probes, elevation, shape, width, height, cornerRadius)
            }
        },
    )
}

@Composable
private fun ElevationSurface(
    probes: ProbeRegistry,
    elevation: Dp,
    shape: Shape,
    width: Dp,
    height: Dp,
    cornerRadius: Dp,
    clipProbeId: String? = null,
) {
    probes.recordShadow(
        "component",
        shadowSpec(width, height, cornerRadius, elevation, Color.Black, Color.Black, clipProbeId = clipProbeId),
    )
    Surface(
        modifier = Modifier.requiredSize(width, height).then(probes.geometry("component")),
        shape = shape,
        // Match the root background so the opaque occluder still hides the normally invisible
        // inner shadow mesh without adding a second, contrasting rounded-fill AA comparison.
        color = MaterialTheme.colorScheme.surface,
        shadowElevation = elevation,
) {}
}

@Composable
private fun OpacityElevationSurface(
    probes: ProbeRegistry,
    elevation: Dp,
    opacity: Float,
    shape: Shape,
    cornerRadius: Dp,
) {
    probes.recordShadow(
        "component",
        shadowSpec(96.dp, 64.dp, cornerRadius, elevation, Color.Black, Color.Black, opacity),
    )
    val density = LocalDensity.current
    Box(
        modifier =
            Modifier.requiredSize(96.dp, 64.dp)
                .graphicsLayer {
                    alpha = opacity
                    shadowElevation = with(density) { elevation.toPx() }
                    this.shape = shape
                    clip = false
                }
                .background(MaterialTheme.colorScheme.surface, shape)
                .then(probes.geometry("component"))
    )
}

private fun shadowSpec(
    width: Dp,
    height: Dp,
    cornerRadius: Dp,
    elevation: Dp,
    ambientColor: Color,
    spotColor: Color,
    opacity: Float = 1f,
    clipProbeId: String? = null,
    occluderDrawn: Boolean = true,
): ShadowProbeSpec {
    val radius = cornerRadius.value
    val kind = when {
        radius == 0f -> "rectangle"
        width == height && radius == width.value / 2f -> "circle"
        else -> "roundedRectangle"
    }
    return ShadowProbeSpec(
        shapeKind = kind,
        cornerRadiusDp = radius,
        elevationDp = elevation.value,
        ambientColorArgb = ambientColor.toArgbHex(),
        spotColorArgb = spotColor.toArgbHex(),
        opacity = opacity,
        clipProbeId = clipProbeId,
        occluderDrawn = occluderDrawn,
    )
}

private fun Color.toArgbHex(): String = "#%08X".format(toArgb())

@Composable
private fun PrimaryRoleSwatches(probes: ProbeRegistry) {
    val colors = listOf(
        MaterialTheme.colorScheme.primary,
        MaterialTheme.colorScheme.onPrimary,
        MaterialTheme.colorScheme.primaryContainer,
        MaterialTheme.colorScheme.onPrimaryContainer,
    )
    Row(Modifier.fillMaxSize().then(probes.geometry("component"))) {
        colors.forEach { color ->
            Box(Modifier.width(60.dp).fillMaxHeight().background(color))
        }
    }
}

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
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

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
        id = "foundation/elevation/rounded-rectangle/level6/dark",
        elevation = 6.dp,
        shape = RoundedCornerShape(20.dp),
        dark = true,
    )
    registerElevationScene(
        id = "foundation/elevation/circle/level6/light",
        elevation = 6.dp,
        shape = CircleShape,
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
        width = 96.dp,
        height = 64.dp,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/rounded-rectangle/level6/spot-only/light",
        ambientColor = Color.Transparent,
        spotColor = Color.Black,
        shape = RoundedCornerShape(20.dp),
        width = 96.dp,
        height = 64.dp,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/rounded-rectangle/level6/ambient-only/shadow-only/light",
        ambientColor = Color.Black,
        spotColor = Color.Transparent,
        drawOccluder = false,
        shape = RoundedCornerShape(20.dp),
        width = 96.dp,
        height = 64.dp,
    )
    registerShadowComponentScene(
        id = "foundation/elevation/rounded-rectangle/level6/spot-only/shadow-only/light",
        ambientColor = Color.Transparent,
        spotColor = Color.Black,
        drawOccluder = false,
        shape = RoundedCornerShape(20.dp),
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

private fun ReferenceCaseRegistry.registerShadowComponentScene(
    id: String,
    ambientColor: Color,
    spotColor: Color,
    drawOccluder: Boolean = true,
    shape: Shape = CircleShape,
    width: Dp = 72.dp,
    height: Dp = 72.dp,
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
            var modifier = Modifier
                .requiredSize(width, height)
                .shadow(
                    elevation = 6.dp,
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
) {
    register(
        CaseContract(
            id = id,
            viewportWidthDp = 240,
            viewportHeightDp = 180,
            dark = dark,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "shadow")),
        ),
        ReferenceScene { probes ->
            if (parentClipped) {
                Box(
                    modifier = Modifier.requiredSize(112.dp, 80.dp).clip(RectangleShape),
                    contentAlignment = Alignment.Center,
                ) {
                    ElevationSurface(probes, elevation, shape, width, height)
                }
            } else {
                ElevationSurface(probes, elevation, shape, width, height)
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
) {
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

// Implementation-source basis: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/{Divider.kt,
// tokens/DividerTokens.kt} (VERSION v0_117), DividerTest.kt, and DividerScreenshotTest.kt.
// These scenes use only HorizontalDivider/VerticalDivider APIs present in the pinned executable
// Material3 alpha22 reference. A Canvas stroke has no child layout node, so `component` reports
// Canvas layout bounds while same-coordinate raster evidence covers the actual line.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.width
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.VerticalDivider
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerDividerScenes() {
    register(
        horizontalDividerCase("divider/horizontal/default/light"),
        ReferenceScene { probes -> HorizontalDividerScene(probes) },
    )
    register(
        horizontalDividerCase("divider/horizontal/default/dark", dark = true),
        ReferenceScene { probes -> HorizontalDividerScene(probes) },
    )
    register(
        verticalDividerCase("divider/vertical/default/light"),
        ReferenceScene { probes -> VerticalDividerScene(probes) },
    )
    register(
        horizontalDividerCase("divider/horizontal/custom-thickness/light"),
        ReferenceScene { probes -> HorizontalDividerScene(probes, thickness = 3.dp) },
    )
    register(
        hairlineDividerCase(),
        ReferenceScene { probes -> HairlineDividerScene(probes) },
    )
    register(
        verticalHairlineDividerCase(),
        ReferenceScene { probes -> VerticalHairlineDividerScene(probes) },
    )
}

private fun horizontalDividerCase(id: String, dark: Boolean = false) = CaseContract(
    id = id,
    viewportWidthDp = 320,
    viewportHeightDp = 48,
    dark = dark,
    requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
)

private fun verticalDividerCase(id: String) = CaseContract(
    id = id,
    viewportWidthDp = 48,
    viewportHeightDp = 320,
    dark = false,
    requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
)

private fun hairlineDividerCase() = CaseContract(
    id = "divider/horizontal/hairline/light",
    viewportWidthDp = 320,
    viewportHeightDp = 48,
    dark = false,
    // Dp.Hairline gives Canvas a zero-height layout node, for which Compose does not deliver an
    // onGloballyPositioned callback. The transparent overlay is the painted one-physical-pixel
    // footprint only; it changes neither the upstream divider's layout nor its pixels.
    requiredNodes = listOf(RequiredNodeSpec(id = "stroke", kind = "container")),
)

private fun verticalHairlineDividerCase() = CaseContract(
    id = "divider/vertical/hairline/light",
    viewportWidthDp = 48,
    viewportHeightDp = 320,
    dark = false,
    // See hairlineDividerCase: Canvas has no non-zero layout node for Dp.Hairline, so this
    // transparent overlay records the deterministic one-physical-pixel render footprint.
    requiredNodes = listOf(RequiredNodeSpec(id = "stroke", kind = "container")),
)

@Composable
private fun HorizontalDividerScene(probes: ProbeRegistry, thickness: Dp = 1.dp) {
    Column(Modifier.requiredSize(320.dp, 48.dp)) {
        Spacer(Modifier.height(20.dp))
        HorizontalDivider(modifier = probes.geometry("component"), thickness = thickness)
    }
}

@Composable
private fun VerticalDividerScene(probes: ProbeRegistry) {
    Row(Modifier.requiredSize(48.dp, 320.dp)) {
        Spacer(Modifier.width(20.dp))
        VerticalDivider(modifier = probes.geometry("component"))
    }
}

@Composable
private fun HairlineDividerScene(probes: ProbeRegistry) {
    val physicalPixel = with(LocalDensity.current) { (1f / density).toDp() }
    Box(Modifier.requiredSize(320.dp, 48.dp)) {
        Column(Modifier.fillMaxSize()) {
            Spacer(Modifier.height(20.dp))
            HorizontalDivider(thickness = Dp.Hairline)
        }
        // See hairlineDividerCase: a named, transparent geometry probe for the painted footprint.
        Spacer(
            Modifier.fillMaxWidth()
                .height(physicalPixel)
                .offset(y = 20.dp)
                .then(probes.geometry("stroke")),
        )
    }
}

@Composable
private fun VerticalHairlineDividerScene(probes: ProbeRegistry) {
    val physicalPixel = with(LocalDensity.current) { (1f / density).toDp() }
    Box(Modifier.requiredSize(48.dp, 320.dp)) {
        Row(Modifier.fillMaxSize()) {
            Spacer(Modifier.width(20.dp))
            VerticalDivider(thickness = Dp.Hairline)
        }
        // See verticalHairlineDividerCase: a named, transparent geometry probe only.
        Spacer(
            Modifier.fillMaxHeight()
                .width(physicalPixel)
                .offset(x = 20.dp)
                .then(probes.geometry("stroke")),
        )
    }
}

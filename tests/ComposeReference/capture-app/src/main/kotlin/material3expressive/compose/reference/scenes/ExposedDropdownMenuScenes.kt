// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 /
// Jetpack Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// ExposedDropdownMenu.kt and Menu.kt.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuAnchorType
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuDefaults
import androidx.compose.material3.MotionScheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.MotionCaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry
import material3expressive.compose.reference.probes.ShadowProbeSpec

@OptIn(ExperimentalMaterial3Api::class)
fun ReferenceCaseRegistry.registerExposedDropdownMenuScenes() {
    registerCheckpoint("start", elapsedTicks = 0)
    registerCheckpoint("mid", elapsedTicks = 1_000_000)
    registerCheckpoint("settled", elapsedTicks = 5_000_000)
}

@OptIn(ExperimentalMaterial3Api::class)
private fun ReferenceCaseRegistry.registerCheckpoint(
    checkpointId: String,
    elapsedTicks: Long,
) {
    val expanded = mutableStateOf(false)
    register(
        CaseContract(
            id = "exposed-dropdown/filled/open/$checkpointId/light",
            viewportWidthDp = 360,
            viewportHeightDp = 320,
            dark = false,
            requiredNodes = buildList {
                add(RequiredNodeSpec("component", "container"))
                add(RequiredNodeSpec("menu", "container"))
                add(RequiredNodeSpec("menu-shadow", "container", parentId = "menu"))
                add(RequiredNodeSpec("red", "text", parentId = "menu", text = "Red"))
                add(RequiredNodeSpec("green", "text", parentId = "menu", text = "Green"))
                add(RequiredNodeSpec("blue", "text", parentId = "menu", text = "Blue"))
            },
            motion = MotionCaseContract(
                scenarioId = "exposed-dropdown/filled/open/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "expand-menu",
            ),
        ),
        ReferenceScene(trigger = { expanded.value = true }) { probes ->
            ExposedDropdownScene(probes, expanded.value, checkpointId != "mid")
        },
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ExposedDropdownScene(
    probes: ProbeRegistry,
    expanded: Boolean,
    withShadow: Boolean,
) {
    if (withShadow) {
        probes.recordShadow(
            "menu-shadow",
            ShadowProbeSpec(
                shapeKind = "roundedRectangle",
                cornerRadiusDp = 4f,
                elevationDp = 3f,
            ),
        )
    }
    MaterialTheme(
        colorScheme = MaterialTheme.colorScheme,
        typography = MaterialTheme.typography,
        shapes = MaterialTheme.shapes,
        motionScheme = MotionScheme.expressive(),
    ) {
        Box(
            modifier = Modifier.requiredSize(360.dp, 320.dp),
            contentAlignment = Alignment.TopCenter,
        ) {
            ExposedDropdownMenuBox(
                expanded = expanded,
                onExpandedChange = {},
                modifier = Modifier.padding(top = 32.dp),
            ) {
                TextField(
                    value = "",
                    onValueChange = {},
                    readOnly = true,
                    trailingIcon = {
                        ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded)
                    },
                    modifier = Modifier
                        .requiredSize(width = 240.dp, height = 56.dp)
                        .menuAnchor(ExposedDropdownMenuAnchorType.PrimaryNotEditable)
                        .then(probes.geometry("component")),
                )
                ExposedDropdownMenu(
                    expanded = expanded,
                    onDismissRequest = {},
                    modifier =
                        Modifier
                            .then(probes.geometry("menu"))
                            .then(probes.transformedGeometry("menu-shadow")),
                    shadowElevation = if (withShadow) MenuDefaults.ShadowElevation else 0.dp,
                ) {
                    listOf("Red", "Green", "Blue").forEach { option ->
                        DropdownMenuItem(
                            text = {
                                Text(
                                    text = option,
                                    modifier = probes.transformedGeometry(option.lowercase()),
                                    onTextLayout = probes.onTextLayout(option.lowercase()),
                                )
                            },
                            onClick = {},
                            modifier = Modifier.fillMaxWidth(),
                        )
                    }
                }
            }
        }
    }
}

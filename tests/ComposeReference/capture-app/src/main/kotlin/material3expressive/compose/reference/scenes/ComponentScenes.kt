package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularWavyProgressIndicator
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.RangeSlider
import androidx.compose.material3.TriStateCheckbox
import androidx.compose.material3.VerticalSlider
import androidx.compose.material3.rememberSliderState
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerComponentScenes() {
    register(
        CaseContract(
            id = "card/filled/fixed/enabled/light",
            viewportWidthDp = 240,
            viewportHeightDp = 128,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            Card(
                Modifier.requiredSize(160.dp, 80.dp).then(probes.geometry("component"))
            ) {
                Spacer(Modifier.fillMaxSize())
            }
        },
    )

    // ElevatedCard delegates to Card with ElevatedCardTokens' medium shape, SurfaceContainerLow
    // color, and Level1 shadow elevation. Keep the content empty so the raster contract isolates
    // the external shadow rather than font or icon rendering.
    register(
        CaseContract(
            id = "card/elevated/fixed/enabled/light",
            viewportWidthDp = 240,
            viewportHeightDp = 128,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "shadow")),
        ),
        ReferenceScene { probes ->
            ElevatedCard(
                Modifier.requiredSize(160.dp, 80.dp).then(probes.geometry("component"))
            ) {
                Spacer(Modifier.fillMaxSize())
            }
        },
    )

    register(
        CaseContract(
            id = "fab/primary/small/enabled/light",
            viewportWidthDp = 160,
            viewportHeightDp = 120,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "shadow")),
        ),
        ReferenceScene { probes ->
            FloatingActionButton(
                onClick = {},
                modifier = Modifier.then(probes.geometry("component")),
            ) {
                Spacer(Modifier.size(24.dp))
            }
        },
    )

    register(
        CaseContract(
            id = "selection/checkbox/selected/light",
            viewportWidthDp = 96,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "icon")),
        ),
        ReferenceScene { probes ->
            Checkbox(
                checked = true,
                onCheckedChange = {},
                modifier = Modifier.then(probes.geometry("component")),
            )
        },
    )

    // Checkbox.kt (alpha22 and alpha25) maps Indeterminate to the selected container and the
    // dedicated indeterminate glyph. This is static terminal state, so no host-clock advance is
    // involved in capture.
    register(
        CaseContract(
            id = "selection/checkbox/indeterminate/light",
            viewportWidthDp = 96,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "icon")),
        ),
        ReferenceScene { probes ->
            TriStateCheckbox(
                state = ToggleableState.Indeterminate,
                onClick = {},
                modifier = Modifier.then(probes.geometry("component")),
            )
        },
    )

    register(
        CaseContract(
            id = "selection/slider/vertical/default/light",
            viewportWidthDp = 112,
            viewportHeightDp = 240,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            VerticalSlider(
                state = rememberSliderState(value = 0.65f),
                modifier = Modifier.requiredSize(44.dp, 160.dp).then(probes.geometry("component")),
            )
        },
    )

    register(
        CaseContract(
            id = "selection/range-slider/default/light",
            viewportWidthDp = 288,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            RangeSlider(
                value = 0.25f..0.75f,
                onValueChange = {},
                modifier = Modifier.requiredSize(240.dp, 44.dp).then(probes.geometry("component")),
            )
        },
    )

    // Freeze wave travel with waveSpeed=0.dp. This preserves the alpha22 default morphology,
    // token values, and determinate semantics while making the raster contract clock-independent.
    register(
        CaseContract(
            id = "progress/circular-wavy/determinate/light",
            viewportWidthDp = 112,
            viewportHeightDp = 112,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            CircularWavyProgressIndicator(
                progress = { 0.5f },
                waveSpeed = 0.dp,
                modifier = Modifier.then(probes.geometry("component")),
            )
        },
    )
}

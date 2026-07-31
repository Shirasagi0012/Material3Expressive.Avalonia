package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.CircularWavyProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3ExpressiveApi
import androidx.compose.material3.LoadingIndicator
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.MotionCaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec

/** Material component checkpoints driven by the manifest-backed motion clock. */
@OptIn(ExperimentalMaterial3ExpressiveApi::class)
fun ReferenceCaseRegistry.registerComponentMotionScenes() {
    registerCircularWavyCheckpoint("start", elapsedTicks = 0)
    registerCircularWavyCheckpoint("half-wave", elapsedTicks = 5_000_000)
    registerCircularWavyCheckpoint("one-wave", elapsedTicks = 10_000_000)
    registerCircularWavyIndeterminateStart(dark = false)
    registerCircularWavyIndeterminateStart(dark = true)

    registerLoadingIndicatorCheckpoint("start", elapsedTicks = 0)
    registerLoadingIndicatorCheckpoint("spring-mid", elapsedTicks = 1_500_000)
    registerLoadingIndicatorCheckpoint("next-morph", elapsedTicks = 6_500_000)
}

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
private fun ReferenceCaseRegistry.registerCircularWavyIndeterminateStart(dark: Boolean) {
    val started = mutableStateOf(false)
    val theme = if (dark) "dark" else "light"
    register(
        CaseContract(
            id = "progress/circular-wavy/indeterminate/start/$theme",
            viewportWidthDp = 112,
            viewportHeightDp = 112,
            dark = dark,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
            motion = MotionCaseContract(
                scenarioId = "progress/circular-wavy/indeterminate/$theme",
                checkpointId = "start",
                elapsedTicks = 0,
                triggerAction = "start-indeterminate",
                usesComposeCoroutineClock = true,
            ),
        ),
        ReferenceScene(trigger = { started.value = true }) { probes ->
            Box(
                modifier = Modifier.requiredSize(48.dp).then(probes.geometry("component")),
                contentAlignment = Alignment.Center,
            ) {
                if (started.value) {
                    CircularWavyProgressIndicator(
                        waveSpeed = 0.dp,
                        modifier = Modifier.fillMaxSize(),
                    )
                }
            }
        },
    )
}

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
private fun ReferenceCaseRegistry.registerCircularWavyCheckpoint(
    checkpointId: String,
    elapsedTicks: Long,
) {
    val started = mutableStateOf(false)
    register(
        CaseContract(
            id = "progress/circular-wavy/motion/$checkpointId/light",
            viewportWidthDp = 112,
            viewportHeightDp = 112,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
            motion = MotionCaseContract(
                scenarioId = "progress/circular-wavy/determinate-motion/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "start-wave-travel",
            ),
        ),
        ReferenceScene(trigger = { started.value = true }) { probes ->
            // Mount the modifier node during the registration transaction. Updating waveSpeed on
            // the already drawn alpha22 node does not start its offset job reliably; a fresh mount
            // gives its draw cache the resolved vertex count before logical t=0 is delivered.
            Box(
                modifier = Modifier.requiredSize(48.dp).then(probes.geometry("component")),
                contentAlignment = Alignment.Center,
            ) {
                if (started.value) {
                    CircularWavyProgressIndicator(
                        progress = { 0.5f },
                        // The default 15dp wavelength and speed resolve to nine vertices.
                        modifier = Modifier.fillMaxSize(),
                    )
                }
            }
        },
    )
}

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
private fun ReferenceCaseRegistry.registerLoadingIndicatorCheckpoint(
    checkpointId: String,
    elapsedTicks: Long,
) {
    val started = mutableStateOf(false)
    register(
        CaseContract(
            id = "progress/loading-indicator/indeterminate/$checkpointId/light",
            viewportWidthDp = 112,
            viewportHeightDp = 112,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
            motion = MotionCaseContract(
                scenarioId = "progress/loading-indicator/indeterminate/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "start-indeterminate",
                usesComposeCoroutineClock = true,
            ),
        ),
        ReferenceScene(trigger = { started.value = true }) { probes ->
            // LoadingIndicator's morph is a custom-drawn path with no independent LayoutNode.
            // Contract its public 48dp component slot and retain the moving path as raster evidence.
            Box(
                modifier = Modifier.requiredSize(48.dp).then(probes.geometry("component")),
                contentAlignment = Alignment.Center,
            ) {
                if (started.value) LoadingIndicator(Modifier.fillMaxSize())
            }
        },
    )
}

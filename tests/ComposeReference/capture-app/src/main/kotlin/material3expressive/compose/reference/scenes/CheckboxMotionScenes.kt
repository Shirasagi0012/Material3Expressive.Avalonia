package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.TriStateCheckbox
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.MotionCaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec

/** Checkbox.kt's three distinct visual state transitions, replayed by the producer clock. */
fun ReferenceCaseRegistry.registerCheckboxMotionScenes() {
    registerCheckboxCheckpoint(
        scenarioId = "off-to-on",
        initial = ToggleableState.Off,
        target = ToggleableState.On,
        checkpointId = "spatial-settled",
        elapsedTicks = 5_000_000,
    )
    registerCheckboxCheckpoint(
        scenarioId = "on-to-indeterminate",
        initial = ToggleableState.On,
        target = ToggleableState.Indeterminate,
        checkpointId = "morph-mid",
        elapsedTicks = 1_500_000,
    )
    registerCheckboxCheckpoint(
        scenarioId = "indeterminate-to-off",
        initial = ToggleableState.Indeterminate,
        target = ToggleableState.Off,
        checkpointId = "after-clear",
        elapsedTicks = 1_500_000,
    )
}

private fun ReferenceCaseRegistry.registerCheckboxCheckpoint(
    scenarioId: String,
    initial: ToggleableState,
    target: ToggleableState,
    checkpointId: String,
    elapsedTicks: Long,
) {
    val state = mutableStateOf(initial)
    register(
        CaseContract(
            id = "selection/checkbox/motion/$scenarioId/$checkpointId/light",
            viewportWidthDp = 96,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
            motion = MotionCaseContract(
                scenarioId = "selection/checkbox/motion/$scenarioId/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "set-${target.name.lowercase()}",
            ),
        ),
        ReferenceScene(trigger = { state.value = target }) { probes ->
            Box(
                modifier = Modifier.requiredSize(48.dp).then(probes.geometry("component")),
                contentAlignment = Alignment.Center,
            ) {
                TriStateCheckbox(
                    state = state.value,
                    onClick = {},
                    modifier = Modifier.fillMaxSize(),
                )
            }
        },
    )
}

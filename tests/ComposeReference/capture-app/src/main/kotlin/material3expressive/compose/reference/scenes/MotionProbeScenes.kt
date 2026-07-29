package material3expressive.compose.reference.scenes

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.MotionCaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

/** Minimal renderer-neutral probe proving that exact intermediate motion frames are comparable. */
fun ReferenceCaseRegistry.registerMotionProbeScenes() {
    registerLinearTweenCheckpoint("start", elapsedTicks = 0)
    registerLinearTweenCheckpoint("mid", elapsedTicks = 1_000_000)
    registerLinearTweenCheckpoint("end", elapsedTicks = 2_000_000)
}

private fun ReferenceCaseRegistry.registerLinearTweenCheckpoint(
    checkpointId: String,
    elapsedTicks: Long,
) {
    val started = mutableStateOf(false)
    register(
        CaseContract(
            id = "foundation/motion/linear-tween/$checkpointId/light",
            viewportWidthDp = 112,
            viewportHeightDp = 64,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
            motion = MotionCaseContract(
                scenarioId = "foundation/motion/linear-tween/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "start-tween",
            ),
        ),
        ReferenceScene(trigger = { started.value = true }) { probes ->
            LinearTweenProbe(started.value, probes)
        },
    )
}

@Composable
private fun LinearTweenProbe(started: Boolean, probes: ProbeRegistry) {
    val progress by animateFloatAsState(
        targetValue = if (started) 1f else 0f,
        animationSpec = tween(durationMillis = 200, easing = LinearEasing),
        label = "linear-tween-probe",
    )
    Canvas(
        Modifier
            .requiredSize(60.dp, 20.dp)
            .then(probes.geometry("component"))
    ) {
        drawRect(
            color = Color.Black,
            topLeft = Offset(x = progress * 40.dp.toPx(), y = 0f),
            size = Size(width = 20.dp.toPx(), height = 20.dp.toPx()),
        )
    }
}

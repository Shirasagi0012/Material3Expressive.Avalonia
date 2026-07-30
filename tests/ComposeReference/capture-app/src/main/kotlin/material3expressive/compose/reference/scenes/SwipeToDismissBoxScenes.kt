// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 /
// Jetpack Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// SwipeToDismissBox.kt and foundation AnchoredDraggableDefaults.SnapAnimationSpec.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.SwipeToDismissBox
import androidx.compose.material3.SwipeToDismissBoxValue
import androidx.compose.material3.Text
import androidx.compose.material3.rememberSwipeToDismissBoxState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.launch
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.MotionCaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

@OptIn(ExperimentalMaterial3Api::class)
fun ReferenceCaseRegistry.registerSwipeToDismissBoxScenes() {
    registerCheckpoint("start", elapsedTicks = 0)
    registerCheckpoint("mid", elapsedTicks = 1_000_000)
    registerCheckpoint("settled", elapsedTicks = 4_000_000)
}

@OptIn(ExperimentalMaterial3Api::class)
private fun ReferenceCaseRegistry.registerCheckpoint(
    checkpointId: String,
    elapsedTicks: Long,
) {
    var dismissTrigger: (() -> Unit)? = null
    register(
        CaseContract(
            id = "swipe-to-dismiss/end-to-start/$checkpointId/light",
            viewportWidthDp = 360,
            viewportHeightDp = 120,
            dark = false,
            requiredNodes = listOf(
                RequiredNodeSpec("component", "container"),
                RequiredNodeSpec("background", "container", parentId = "component"),
                RequiredNodeSpec("content", "container", parentId = "component"),
                RequiredNodeSpec("delete", "text", parentId = "background", text = "Delete"),
                RequiredNodeSpec("message", "text", parentId = "content", text = "Message"),
            ),
            motion = MotionCaseContract(
                scenarioId = "swipe-to-dismiss/end-to-start/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "dismiss-end-to-start",
                usesComposeCoroutineClock = true,
            ),
        ),
        ReferenceScene(trigger = { checkNotNull(dismissTrigger).invoke() }) { probes ->
            SwipeToDismissBoxScene(probes) { dismissTrigger = it }
        },
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SwipeToDismissBoxScene(
    probes: ProbeRegistry,
    registerDismissTrigger: ((() -> Unit) -> Unit),
) {
    val state = rememberSwipeToDismissBoxState()
    val scope = rememberCoroutineScope()
    SideEffect {
        registerDismissTrigger {
            scope.launch { state.dismiss(SwipeToDismissBoxValue.EndToStart) }
        }
    }

    Box(
        modifier = Modifier
            .requiredSize(360.dp, 120.dp)
            .background(MaterialTheme.colorScheme.surface),
        contentAlignment = Alignment.Center,
    ) {
        SwipeToDismissBox(
            state = state,
            modifier = Modifier
                .requiredSize(280.dp, 64.dp)
                .then(probes.geometry("component")),
            backgroundContent = {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(MaterialTheme.colorScheme.errorContainer)
                        .then(probes.geometry("background"))
                        .padding(horizontal = 16.dp),
                    contentAlignment = Alignment.CenterEnd,
                ) {
                    Text(
                        text = "Delete",
                        modifier = probes.text("delete"),
                        color = MaterialTheme.colorScheme.onErrorContainer,
                        style = MaterialTheme.typography.bodyLarge,
                        onTextLayout = probes.onTextLayout("delete"),
                    )
                }
            },
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(MaterialTheme.colorScheme.surfaceContainer)
                    .then(probes.transformedGeometry("content"))
                    .padding(horizontal = 16.dp),
                contentAlignment = Alignment.CenterStart,
            ) {
                Text(
                    text = "Message",
                    modifier = probes.transformedGeometry("message"),
                    color = MaterialTheme.colorScheme.onSurface,
                    style = MaterialTheme.typography.bodyLarge,
                    onTextLayout = probes.onTextLayout("message"),
                )
            }
        }
    }
}

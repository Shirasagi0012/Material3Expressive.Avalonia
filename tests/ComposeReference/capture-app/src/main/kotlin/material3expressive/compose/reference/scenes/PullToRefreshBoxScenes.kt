// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 /
// Jetpack Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 PullToRefresh.kt.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.material3.pulltorefresh.PullToRefreshDefaults
import androidx.compose.material3.pulltorefresh.rememberPullToRefreshState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.launch
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.MotionCaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

@OptIn(ExperimentalMaterial3Api::class)
fun ReferenceCaseRegistry.registerPullToRefreshBoxScenes() {
    registerCheckpoint("start", elapsedTicks = 0)
    registerCheckpoint("mid", elapsedTicks = 1_000_000)
    registerCheckpoint("settled", elapsedTicks = 4_000_000)
}

@OptIn(ExperimentalMaterial3Api::class)
private fun ReferenceCaseRegistry.registerCheckpoint(
    checkpointId: String,
    elapsedTicks: Long,
) {
    var retractTrigger: (() -> Unit)? = null
    register(
        CaseContract(
            id = "pull-to-refresh/retract/$checkpointId/light",
            viewportWidthDp = 360,
            viewportHeightDp = 180,
            dark = false,
            requiredNodes = listOf(
                RequiredNodeSpec("component", "container"),
                RequiredNodeSpec("content", "container", parentId = "component"),
                RequiredNodeSpec("message", "text", parentId = "content", text = "Release to refresh"),
                RequiredNodeSpec("indicator", "container", parentId = "component"),
            ),
            motion = MotionCaseContract(
                scenarioId = "pull-to-refresh/retract/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "animate-to-hidden",
                usesComposeCoroutineClock = true,
            ),
        ),
        ReferenceScene(trigger = { checkNotNull(retractTrigger).invoke() }) { probes ->
            PullToRefreshBoxScene(probes) { retractTrigger = it }
        },
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun PullToRefreshBoxScene(
    probes: ProbeRegistry,
    registerRetractTrigger: ((() -> Unit) -> Unit),
) {
    val state = rememberPullToRefreshState()
    val scope = rememberCoroutineScope()
    SideEffect {
        registerRetractTrigger {
            scope.launch {
                state.snapTo(1.25f)
                state.animateToHidden()
            }
        }
    }

    Box(
        modifier = Modifier
            .requiredSize(360.dp, 180.dp)
            .background(MaterialTheme.colorScheme.surface),
        contentAlignment = Alignment.Center,
    ) {
        PullToRefreshBox(
            isRefreshing = false,
            onRefresh = {},
            state = state,
            modifier = Modifier
                .requiredSize(280.dp, 120.dp)
                .then(probes.geometry("component")),
            indicator = {
                Box(
                    modifier = Modifier
                        .requiredSize(40.dp)
                        .align(Alignment.TopCenter),
                ) {
                    PullToRefreshDefaults.IndicatorBox(
                        state = state,
                        isRefreshing = false,
                        containerColor = PullToRefreshDefaults.indicatorContainerColor,
                        elevation = 0.dp,
                    ) {}
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .graphicsLayer {
                                translationY =
                                    state.distanceFraction * 80.dp.toPx() - 40.dp.toPx()
                            }
                            .then(probes.transformedGeometry("indicator")),
                    )
                }
            },
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(MaterialTheme.colorScheme.surfaceContainer)
                    .then(probes.geometry("content")),
                contentAlignment = Alignment.Center,
            ) {
                Text(
                    text = "Release to refresh",
                    modifier = probes.text("message"),
                    color = MaterialTheme.colorScheme.onSurface,
                    style = MaterialTheme.typography.bodyLarge,
                    onTextLayout = probes.onTextLayout("message"),
                )
            }
        }
    }
}

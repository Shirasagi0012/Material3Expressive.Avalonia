// Implementation-source basis: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// BottomSheetScaffold.kt and SheetDefaults.kt (VERSION 29.0.0).
// The executable Compose reference is Material3 1.5.0-alpha22; these settled partial/expanded
// scenes use only the two standard anchors available in both pins.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.BottomSheetDefaults
import androidx.compose.material3.BottomSheetScaffold
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MotionScheme
import androidx.compose.material3.SheetValue
import androidx.compose.material3.Text
import androidx.compose.material3.rememberBottomSheetScaffoldState
import androidx.compose.material3.rememberStandardBottomSheetState
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
import material3expressive.compose.reference.probes.ShadowProbeSpec

@OptIn(ExperimentalMaterial3Api::class)
fun ReferenceCaseRegistry.registerBottomSheetScenes() {
    register(
        bottomSheetCase("bottomsheet/scaffold/partial/light", dark = false, expanded = false),
        ReferenceScene { probes -> BottomSheetScene(probes, expanded = false) },
    )
    register(
        bottomSheetCase("bottomsheet/scaffold/expanded/light", dark = false, expanded = true),
        ReferenceScene { probes -> BottomSheetScene(probes, expanded = true) },
    )
    register(
        bottomSheetCase("bottomsheet/scaffold/partial/dark", dark = true, expanded = false),
        ReferenceScene { probes -> BottomSheetScene(probes, expanded = false) },
    )
    registerBottomSheetMotionCheckpoint("start", elapsedTicks = 0)
    registerBottomSheetMotionCheckpoint("spring-mid", elapsedTicks = 1_500_000)
    registerBottomSheetMotionCheckpoint("settled", elapsedTicks = 10_000_000)
}

@OptIn(ExperimentalMaterial3Api::class)
private fun ReferenceCaseRegistry.registerBottomSheetMotionCheckpoint(
    checkpointId: String,
    elapsedTicks: Long,
) {
    var expandTrigger: (() -> Unit)? = null
    register(
        bottomSheetCase(
            id = "bottomsheet/scaffold/motion/$checkpointId/light",
            dark = false,
            expanded = false,
        ).copy(
            motion = MotionCaseContract(
                scenarioId = "bottomsheet/scaffold/partial-to-expanded/light",
                checkpointId = checkpointId,
                elapsedTicks = elapsedTicks,
                triggerAction = "expand-sheet",
                usesComposeCoroutineClock = true,
            ),
        ),
        ReferenceScene(trigger = { checkNotNull(expandTrigger).invoke() }) { probes ->
            BottomSheetScene(probes, expanded = false) { expandTrigger = it }
        },
    )
}

private fun bottomSheetCase(id: String, dark: Boolean, expanded: Boolean) = CaseContract(
    id = id,
    viewportWidthDp = 360,
    viewportHeightDp = 320,
    dark = dark,
    requiredNodes = listOf(
        RequiredNodeSpec("root", "container"),
        RequiredNodeSpec("body", "container", parentId = "root"),
        RequiredNodeSpec("PART_Sheet", "shadow", parentId = "root"),
        RequiredNodeSpec("sheet", "container", parentId = "root"),
        RequiredNodeSpec("title", "text", parentId = "root", text = "Inbox"),
        RequiredNodeSpec("body-text", "text", parentId = "body", text = "Body content"),
        RequiredNodeSpec("sheet-title", "text", parentId = "sheet", text = "Filters"),
        RequiredNodeSpec("sheet-support", "text", parentId = "sheet", text = "Persistent sheet"),
    ),
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun BottomSheetScene(
    probes: ProbeRegistry,
    expanded: Boolean,
    registerExpandTrigger: (((() -> Unit)) -> Unit)? = null,
) {
    // SheetBottomTokens v0_210: CornerExtraLargeTop (28dp top corners) and Level1 elevation.
    probes.recordShadow(
        "PART_Sheet",
        ShadowProbeSpec(
            shapeKind = "roundedRectangle",
            cornerRadiusDp = 0f,
            topLeftCornerRadiusDp = 28f,
            topRightCornerRadiusDp = 28f,
            bottomRightCornerRadiusDp = 0f,
            bottomLeftCornerRadiusDp = 0f,
            elevationDp = 1f,
            clipProbeId = "root",
        ),
    )
    // Material3Expressive.Avalonia treats the official expressive scheme as its default. The
    // producer's outer MaterialTheme intentionally remains standard for legacy cases, so scope
    // this component scene to the equivalent official Compose motion scheme.
    MaterialTheme(motionScheme = MotionScheme.expressive()) {
        val sheetState = rememberStandardBottomSheetState(
            initialValue = if (expanded) SheetValue.Expanded else SheetValue.PartiallyExpanded,
        )
        val scaffoldState = rememberBottomSheetScaffoldState(bottomSheetState = sheetState)
        val scope = rememberCoroutineScope()
        SideEffect {
            registerExpandTrigger?.invoke {
                scope.launch { sheetState.expand() }
            }
        }
        BottomSheetScaffold(
            modifier = Modifier.then(probes.geometry("root")),
            scaffoldState = scaffoldState,
            sheetPeekHeight = 56.dp,
            sheetMaxWidth = 640.dp,
            containerColor = MaterialTheme.colorScheme.surface,
            // Keep the official handle pixels, but place it inside the probed content so this node
            // measures the actual Surface bounds, including the 48dp handle row.
            sheetDragHandle = null,
            sheetContent = {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .then(probes.geometry("PART_Sheet")),
                ) {
                    Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
                        BottomSheetDefaults.DragHandle()
                    }
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .then(probes.geometry("sheet"))
                            .padding(horizontal = 16.dp, vertical = 22.dp),
                    ) {
                        Text(
                            "Filters",
                            modifier = probes.text("sheet-title"),
                            style = MaterialTheme.typography.titleMedium,
                            onTextLayout = probes.onTextLayout("sheet-title"),
                        )
                        Text(
                            "Persistent sheet",
                            modifier = probes.text("sheet-support"),
                            style = MaterialTheme.typography.bodyMedium,
                            onTextLayout = probes.onTextLayout("sheet-support"),
                        )
                    }
                }
            },
            topBar = {
                Text(
                    "Inbox",
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(56.dp)
                        .padding(16.dp)
                        .then(probes.text("title")),
                    style = MaterialTheme.typography.titleMedium,
                    onTextLayout = probes.onTextLayout("title"),
                )
            },
        ) { padding ->
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .then(probes.geometry("body"))
                    .padding(padding),
            ) {
                Text(
                    "Body content",
                    modifier = Modifier.padding(16.dp).then(probes.text("body-text")),
                    onTextLayout = probes.onTextLayout("body-text"),
                )
            }
        }
    }
}

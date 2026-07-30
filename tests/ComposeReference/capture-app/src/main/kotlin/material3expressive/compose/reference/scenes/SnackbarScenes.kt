// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// SnackbarHost.kt and Snackbar.kt. The pinned executable reference is Jetpack Material3
// 1.5.0-alpha22; its static current-item, default Snackbar, action, and layout contracts match.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.width
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Snackbar
import androidx.compose.material3.SnackbarDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry
import material3expressive.compose.reference.probes.ShadowProbeSpec

fun ReferenceCaseRegistry.registerSnackbarScenes() {
    register(
        CaseContract(
            id = "snackbar/host/message/light",
            viewportWidthDp = 360,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(
                RequiredNodeSpec("component", "container"),
                RequiredNodeSpec("snackbar", "shadow", parentId = "component"),
                RequiredNodeSpec("message", "text", parentId = "snackbar", text = "Message archived"),
            ),
        ),
        ReferenceScene { probes -> SnackbarHostScene(probes) },
    )
    register(
        CaseContract(
            id = "snackbar/host/action/light",
            viewportWidthDp = 360,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(
                RequiredNodeSpec("component", "container"),
                RequiredNodeSpec("snackbar", "shadow", parentId = "component"),
                RequiredNodeSpec("message", "text", parentId = "snackbar", text = "Message archived"),
                RequiredNodeSpec("action", "text", parentId = "snackbar", text = "Undo"),
            ),
        ),
        ReferenceScene { probes -> SnackbarHostScene(probes, withAction = true) },
    )
}

@Composable
private fun SnackbarHostScene(probes: ProbeRegistry, withAction: Boolean = false) {
    // Queue/suspension semantics are deterministic-test covered on Avalonia. This settled static
    // scene isolates the host's current-item visual; coroutine-driven appearance belongs in the
    // separately planned manifest-clock motion checkpoints.
    probes.recordShadow(
        "snackbar",
        ShadowProbeSpec(
            shapeKind = "roundedRectangle",
            cornerRadiusDp = 4f,
            elevationDp = 6f,
            clipProbeId = "component",
        ),
    )
    Box(
        modifier = Modifier.requiredSize(360.dp, 96.dp).then(probes.geometry("component")),
        contentAlignment = Alignment.BottomCenter,
    ) {
        // SnackbarHost gives its child loose width constraints. Putting required width directly on
        // Snackbar instead makes alpha22's legacy row measure the action with a 344dp minimum and
        // overlap it with the message, which is not the host contract. Constrain the parent so the
        // Snackbar itself receives the real 0..344dp range.
        Box(modifier = Modifier.width(344.dp).then(probes.geometry("snackbar"))) {
            Snackbar(
                action =
                    if (withAction) {
                        {
                            // This is the action used by Snackbar(snackbarData): keep the official
                            // TextButton and explicit Snackbar action color while exposing the
                            // otherwise-internal label to the structural text contract.
                            TextButton(
                                colors =
                                    ButtonDefaults.textButtonColors(
                                        contentColor = SnackbarDefaults.actionColor
                                    ),
                                onClick = {},
                            ) {
                                Text(
                                    "Undo",
                                    modifier = probes.text("action"),
                                    onTextLayout = probes.onTextLayout("action"),
                                )
                            }
                        }
                    } else {
                        null
                    },
            ) {
                Text(
                    "Message archived",
                    modifier = probes.text("message"),
                    onTextLayout = probes.onTextLayout("message"),
                )
            }
        }
    }
}

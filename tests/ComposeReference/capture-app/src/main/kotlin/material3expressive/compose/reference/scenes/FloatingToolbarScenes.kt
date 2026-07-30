// Executable reference: Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack Material3
// 1.5.0-alpha22. Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// FloatingToolbar.kt and FloatingToolbarTokens.kt VERSION 12_0_0.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.ExperimentalMaterial3ExpressiveApi
import androidx.compose.material3.HorizontalFloatingToolbar
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.VerticalFloatingToolbar
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
fun ReferenceCaseRegistry.registerFloatingToolbarScenes() {
    register(toolbarContract(horizontal = true), ReferenceScene { probes -> ToolbarScene(probes, true) })
    register(toolbarContract(horizontal = false), ReferenceScene { probes -> ToolbarScene(probes, false) })
}

private fun toolbarContract(horizontal: Boolean): CaseContract {
    val name = if (horizontal) "horizontal" else "vertical"
    return CaseContract(
        id = "floating-toolbar/$name/standard-expanded/light",
        viewportWidthDp = if (horizontal) 320 else 112,
        viewportHeightDp = if (horizontal) 112 else 320,
        dark = false,
        requiredNodes = listOf(
            RequiredNodeSpec("root", "container"),
            RequiredNodeSpec("toolbar", "container", parentId = "root"),
            RequiredNodeSpec("action-primary", "container", parentId = "toolbar"),
            RequiredNodeSpec("action-secondary", "container", parentId = "toolbar"),
            RequiredNodeSpec("action-tertiary", "container", parentId = "toolbar"),
        ),
    )
}

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
@Composable
private fun ToolbarScene(probes: ProbeRegistry, horizontal: Boolean) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(MaterialTheme.colorScheme.surface)
            .then(probes.geometry("root")),
        contentAlignment = Alignment.Center,
    ) {
        if (horizontal) {
            HorizontalFloatingToolbar(
                expanded = true,
                modifier = probes.geometry("toolbar"),
            ) {
                ToolbarAction(probes, "action-primary", MaterialTheme.colorScheme.primary)
                ToolbarAction(probes, "action-secondary", MaterialTheme.colorScheme.secondary)
                ToolbarAction(probes, "action-tertiary", MaterialTheme.colorScheme.tertiary)
            }
        } else {
            VerticalFloatingToolbar(
                expanded = true,
                modifier = probes.geometry("toolbar"),
            ) {
                ToolbarAction(probes, "action-primary", MaterialTheme.colorScheme.primary)
                ToolbarAction(probes, "action-secondary", MaterialTheme.colorScheme.secondary)
                ToolbarAction(probes, "action-tertiary", MaterialTheme.colorScheme.tertiary)
            }
        }
    }
}

@Composable
private fun ToolbarAction(probes: ProbeRegistry, id: String, color: Color) {
    Box(
        modifier = Modifier.requiredSize(48.dp).then(probes.geometry(id)),
        contentAlignment = Alignment.Center,
    ) {
        Box(Modifier.requiredSize(16.dp).background(color))
    }
}

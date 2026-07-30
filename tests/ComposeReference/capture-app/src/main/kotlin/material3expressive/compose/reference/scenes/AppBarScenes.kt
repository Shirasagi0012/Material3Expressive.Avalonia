// Executable reference: Compose Multiplatform Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 AppBar.kt.

package material3expressive.compose.reference.scenes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MediumTopAppBar
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.material3.rememberTopAppBarState
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

@OptIn(ExperimentalMaterial3Api::class)
fun ReferenceCaseRegistry.registerAppBarScenes() {
    register(
        appBarCase("appbar/medium/expanded/light"),
        ReferenceScene { probes -> AppBarScene(probes, collapsed = false) },
    )
}

private fun appBarCase(id: String) = CaseContract(
    id = id,
    viewportWidthDp = 360,
    viewportHeightDp = 160,
    dark = false,
    requiredNodes = listOf(
        RequiredNodeSpec("root", "container"),
        RequiredNodeSpec("title", "text", parentId = "root", text = "Inbox"),
    ),
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun AppBarScene(probes: ProbeRegistry, collapsed: Boolean) {
    Box(
        Modifier
            .fillMaxSize()
            .background(MaterialTheme.colorScheme.surface),
    ) {
        val state = rememberTopAppBarState(
            initialHeightOffsetLimit = -48f,
            initialHeightOffset = if (collapsed) -48f else 0f,
            initialContentOffset = if (collapsed) -1f else 0f,
        )
        MediumTopAppBar(
            title = {
                Text(
                    "Inbox",
                    modifier = probes.text("title"),
                    onTextLayout = probes.onTextLayout("title"),
                )
            },
            modifier = Modifier.then(probes.geometry("root")),
            collapsedHeight = 64.dp,
            expandedHeight = 112.dp,
            windowInsets = WindowInsets(0, 0, 0, 0),
            scrollBehavior = TopAppBarDefaults.exitUntilCollapsedScrollBehavior(state),
        )
    }
}

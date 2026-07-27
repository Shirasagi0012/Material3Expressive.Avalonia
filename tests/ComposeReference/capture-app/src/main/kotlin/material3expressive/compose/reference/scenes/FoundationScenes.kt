package material3expressive.compose.reference.scenes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.width
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerFoundationScenes() {
    register(
        CaseContract(
            id = "foundation/color/primary-roles/light",
            viewportWidthDp = 240,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            PrimaryRoleSwatches(probes)
        },
    )

    register(
        CaseContract(
            id = "foundation/color/primary-roles/dark",
            viewportWidthDp = 240,
            viewportHeightDp = 96,
            dark = true,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            PrimaryRoleSwatches(probes)
        },
    )
}

@Composable
private fun PrimaryRoleSwatches(probes: ProbeRegistry) {
    val colors = listOf(
        MaterialTheme.colorScheme.primary,
        MaterialTheme.colorScheme.onPrimary,
        MaterialTheme.colorScheme.primaryContainer,
        MaterialTheme.colorScheme.onPrimaryContainer,
    )
    Row(Modifier.fillMaxSize().then(probes.geometry("component"))) {
        colors.forEach { color ->
            Box(Modifier.width(60.dp).fillMaxHeight().background(color))
        }
    }
}

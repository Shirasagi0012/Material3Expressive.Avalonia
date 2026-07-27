package material3expressive.compose.reference

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.width
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp

internal val FoundationReferenceScenes =
    mapOf<String, ReferenceScene>(
        "foundation/color/primary-roles/light" to referenceScene { PrimaryRoleSwatches() },
        "foundation/color/primary-roles/dark" to referenceScene { PrimaryRoleSwatches() },
    )

@Composable
private fun PrimaryRoleSwatches() {
    val colors =
        listOf(
            MaterialTheme.colorScheme.primary,
            MaterialTheme.colorScheme.onPrimary,
            MaterialTheme.colorScheme.primaryContainer,
            MaterialTheme.colorScheme.onPrimaryContainer,
        )
    Row(Modifier.fillMaxSize().testTag(ComponentTag)) {
        colors.forEach { color ->
            Box(Modifier.width(60.dp).fillMaxHeight().background(color))
        }
    }
}

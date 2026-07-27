package material3expressive.compose.reference

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.FloatingActionButton
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp

internal val ComponentReferenceScenes =
    mapOf<String, ReferenceScene>(
        "card/filled/fixed/enabled/light" to referenceScene {
            Card(Modifier.requiredSize(160.dp, 80.dp).testTag(ComponentTag)) {
                Spacer(Modifier.fillMaxSize())
            }
        },
        "fab/primary/small/enabled/light" to referenceScene {
            FloatingActionButton(
                onClick = {},
                modifier = Modifier.testTag(ComponentTag),
            ) {
                Spacer(Modifier.size(24.dp))
            }
        },
        "selection/checkbox/selected/light" to referenceScene {
            Checkbox(
                checked = true,
                onCheckedChange = {},
                modifier = Modifier.testTag(ComponentTag),
            )
        },
    )

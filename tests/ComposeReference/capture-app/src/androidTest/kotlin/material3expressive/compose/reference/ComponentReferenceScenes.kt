package material3expressive.compose.reference

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp

internal val ComponentReferenceScenes =
    mapOf<String, ReferenceScene>(
        "card/filled/fixed/enabled/light" to {
            Card(Modifier.requiredSize(160.dp, 80.dp).testTag(ComponentTag)) {
                Spacer(Modifier.fillMaxSize())
            }
        },
        "fab/primary/small/enabled/light" to {
            FloatingActionButton(
                onClick = {},
                modifier = Modifier.testTag(ComponentTag),
            ) {
                Spacer(Modifier.size(24.dp))
            }
        },
        "selection/checkbox/selected/light" to {
            Checkbox(
                checked = true,
                onCheckedChange = {},
                modifier = Modifier.testTag(ComponentTag),
            )
        },
        "textfield/outlined/populated/light" to {
            OutlinedTextField(
                value = "Text",
                onValueChange = {},
                label = { Text("Label") },
                singleLine = true,
                modifier = Modifier.requiredSize(240.dp, 56.dp).testTag(ComponentTag),
            )
        },
        "navigation/bar/three-items/selected/light" to {
            NavigationBar(
                modifier = Modifier.requiredSize(360.dp, 80.dp).testTag(ComponentTag),
                windowInsets = WindowInsets(0, 0, 0, 0),
            ) {
                listOf("One", "Two", "Three").forEachIndexed { index, label ->
                    NavigationBarItem(
                        selected = index == 0,
                        onClick = {},
                        icon = { Spacer(Modifier.size(24.dp)) },
                        label = { Text(label) },
                    )
                }
            }
        },
    )


package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.requiredSize
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.FloatingActionButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerComponentScenes() {
    register(
        CaseContract(
            id = "card/filled/fixed/enabled/light",
            viewportWidthDp = 240,
            viewportHeightDp = 128,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "container")),
        ),
        ReferenceScene { probes ->
            Card(
                Modifier.requiredSize(160.dp, 80.dp).then(probes.geometry("component"))
            ) {
                Spacer(Modifier.fillMaxSize())
            }
        },
    )

    register(
        CaseContract(
            id = "fab/primary/small/enabled/light",
            viewportWidthDp = 160,
            viewportHeightDp = 120,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "shadow")),
        ),
        ReferenceScene { probes ->
            FloatingActionButton(
                onClick = {},
                modifier = Modifier.then(probes.geometry("component")),
            ) {
                Spacer(Modifier.size(24.dp))
            }
        },
    )

    register(
        CaseContract(
            id = "selection/checkbox/selected/light",
            viewportWidthDp = 96,
            viewportHeightDp = 96,
            dark = false,
            requiredNodes = listOf(RequiredNodeSpec(id = "component", kind = "icon")),
        ),
        ReferenceScene { probes ->
            Checkbox(
                checked = true,
                onCheckedChange = {},
                modifier = Modifier.then(probes.geometry("component")),
            )
        },
    )
}

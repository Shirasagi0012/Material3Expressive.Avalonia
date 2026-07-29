package material3expressive.compose.reference.scenes

// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt
// and tokens/OutlinedSegmentedButtonTokens.kt (VERSION v0_162). The pinned executable reference
// is Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack Material3 1.5.0-alpha22; its static
// row, item-shape, colour, icon-slot, and overlap contracts are equivalent.

import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MultiChoiceSegmentedButtonRow
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerSegmentedButtonScenes() {
    register(
        segmentedButtonContract(
            id = "segmentedbutton/single/selected-middle/light",
            dark = false,
        ),
        ReferenceScene { probes ->
            SingleChoiceRow(probes, selectedIndex = 1, enabled = true)
        },
    )
    register(
        segmentedButtonContract(
            id = "segmentedbutton/single/selected-middle/dark",
            dark = true,
        ),
        ReferenceScene { probes ->
            SingleChoiceRow(probes, selectedIndex = 1, enabled = true)
        },
    )
    register(
        segmentedButtonContract(
            id = "segmentedbutton/single/selected-middle/disabled/light",
            dark = false,
        ),
        ReferenceScene { probes ->
            SingleChoiceRow(probes, selectedIndex = 1, enabled = false)
        },
    )
    register(
        segmentedButtonContract(
            id = "segmentedbutton/multiple/first-last-selected/light",
            dark = false,
        ),
        ReferenceScene { probes ->
            MultipleChoiceRow(probes, selectedIndices = setOf(0, 2))
        },
    )
}

private fun segmentedButtonContract(id: String, dark: Boolean) = CaseContract(
    id = id,
    viewportWidthDp = 360,
    viewportHeightDp = 96,
    dark = dark,
    requiredNodes = buildList {
        add(RequiredNodeSpec(id = "component", kind = "container"))
        SegmentedLabels.forEachIndexed { index, label ->
            add(
                RequiredNodeSpec(
                    id = "item-$index",
                    kind = "container",
                    parentId = "component",
                )
            )
            add(
                RequiredNodeSpec(
                    id = "label-$index",
                    kind = "text",
                    parentId = "item-$index",
                    text = label,
                )
            )
        }
    },
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SingleChoiceRow(
    probes: ProbeRegistry,
    selectedIndex: Int,
    enabled: Boolean,
) {
    SingleChoiceSegmentedButtonRow(
        modifier =
            Modifier
                .width(300.dp)
                .height(40.dp)
                .then(probes.geometry("component"))
    ) {
        SegmentedLabels.forEachIndexed { index, label ->
            SegmentedButton(
                selected = index == selectedIndex,
                onClick = {},
                shape = SegmentedButtonDefaults.itemShape(index, SegmentedLabels.size),
                enabled = enabled,
                modifier = probes.geometry("item-$index"),
            ) {
                SegmentedLabel(probes, index, label)
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun MultipleChoiceRow(
    probes: ProbeRegistry,
    selectedIndices: Set<Int>,
) {
    MultiChoiceSegmentedButtonRow(
        modifier =
            Modifier
                .width(300.dp)
                .height(40.dp)
                .then(probes.geometry("component"))
    ) {
        SegmentedLabels.forEachIndexed { index, label ->
            SegmentedButton(
                checked = index in selectedIndices,
                onCheckedChange = {},
                shape = SegmentedButtonDefaults.itemShape(index, SegmentedLabels.size),
                modifier = probes.geometry("item-$index"),
            ) {
                SegmentedLabel(probes, index, label)
            }
        }
    }
}

@Composable
private fun SegmentedLabel(
    probes: ProbeRegistry,
    index: Int,
    label: String,
) {
    Text(
        text = label,
        modifier = probes.text("label-$index"),
        onTextLayout = probes.onTextLayout("label-$index"),
    )
}

private val SegmentedLabels = listOf("Day", "Month", "Week")

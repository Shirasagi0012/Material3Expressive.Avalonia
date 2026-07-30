package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerPickerInputScenes() {
    val contract = CaseContract(
        id = "picker-input/date/populated/light",
        viewportWidthDp = 280,
        viewportHeightDp = 96,
        dark = false,
        requiredNodes = listOf(
            RequiredNodeSpec(
                id = "component",
                kind = "container",
                expectedFocused = false,
            ),
            RequiredNodeSpec(
                id = "label",
                kind = "text",
                parentId = "component",
                text = "Date",
            ),
        ),
    )
    register(contract, ReferenceScene { probes -> PopulatedDateInputScene(probes) })
}

@Composable
private fun PopulatedDateInputScene(probes: ProbeRegistry) {
    val value = remember { mutableStateOf("07/04/2024") }
    OutlinedTextField(
        value = value.value,
        onValueChange = { value.value = it },
        label = {
            Text(
                text = "Date",
                modifier = probes.text("label"),
                onTextLayout = probes.onTextLayout("label"),
            )
        },
        singleLine = true,
        modifier = Modifier
            .requiredSize(width = 240.dp, height = 64.dp)
            .then(probes.geometry("component")),
    )
    SideEffect { probes.recordFocused("component", false) }
}

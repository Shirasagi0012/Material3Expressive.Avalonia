package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerTextFieldScenes() {
    val filledContracts: Map<String, TextFieldParams> = mapOf(
        "textfield/filled/empty/light" to TextFieldParams(variant = TextFieldVariant.Filled),
        "textfield/filled/populated/light" to TextFieldParams(TextFieldVariant.Filled, value = "Text"),
        "textfield/filled/populated/dark" to TextFieldParams(TextFieldVariant.Filled, value = "Text", dark = true),
        "textfield/filled/focused-empty/light" to TextFieldParams(TextFieldVariant.Filled, focused = true, placeholder = "Placeholder"),
        "textfield/filled/error/light" to TextFieldParams(TextFieldVariant.Filled, value = "Input", isError = true, supportingText = "Error message"),
        "textfield/filled/disabled/light" to TextFieldParams(TextFieldVariant.Filled, value = "Text", enabled = false),
    )

    val outlinedContracts: Map<String, TextFieldParams> = mapOf(
        "textfield/outlined/empty/light" to TextFieldParams(variant = TextFieldVariant.Outlined),
        "textfield/outlined/populated/light" to TextFieldParams(TextFieldVariant.Outlined, value = "Text"),
        "textfield/outlined/populated/dark" to TextFieldParams(TextFieldVariant.Outlined, value = "Text", dark = true),
        "textfield/outlined/focused-empty/light" to TextFieldParams(TextFieldVariant.Outlined, focused = true, placeholder = "Placeholder"),
        "textfield/outlined/error/light" to TextFieldParams(TextFieldVariant.Outlined, value = "Input", isError = true, supportingText = "Error message"),
        "textfield/outlined/disabled/light" to TextFieldParams(TextFieldVariant.Outlined, value = "Text", enabled = false),
    )

    (filledContracts + outlinedContracts).forEach { (id, params) ->
        register(textFieldContract(id, params), ReferenceScene { probes -> TextFieldScene(probes, params) })
    }
}

private data class TextFieldParams(
    val variant: TextFieldVariant,
    val value: String = "",
    val focused: Boolean = false,
    val placeholder: String? = null,
    val isError: Boolean = false,
    val supportingText: String? = null,
    val enabled: Boolean = true,
    val dark: Boolean = false,
)

private enum class TextFieldVariant { Filled, Outlined }

private fun textFieldContract(id: String, params: TextFieldParams): CaseContract {
    val nodes = mutableListOf(
        RequiredNodeSpec(id = "component", kind = "container"),
        RequiredNodeSpec(id = "label", kind = "text", parentId = "component", text = "Label"),
    )
    if (params.supportingText != null) {
        nodes.add(RequiredNodeSpec(id = "supporting", kind = "text", parentId = "component", text = params.supportingText))
    }
    val hasSupporting = params.supportingText != null
    return CaseContract(
        id = id,
        viewportWidthDp = 280,
        viewportHeightDp = if (hasSupporting) 112 else 96,
        dark = params.dark,
        requiredNodes = nodes,
    )
}

@Composable
private fun TextFieldScene(probes: ProbeRegistry, params: TextFieldParams) {
    var value by remember { mutableStateOf(params.value) }
    val focusRequester = remember { FocusRequester() }
    LaunchedEffect(params.focused) {
        if (params.focused) focusRequester.requestFocus()
    }
    val hasSupporting = params.supportingText != null
    val modifier = Modifier
        .requiredSize(width = 240.dp, height = if (hasSupporting) 80.dp else 56.dp)
        .focusRequester(focusRequester)
        .then(probes.geometry("component"))
    val label: @Composable () -> Unit = {
        Text(
            text = "Label",
            modifier = probes.text("label"),
            onTextLayout = probes.onTextLayout("label"),
        )
    }
    val placeholderContent: (@Composable () -> Unit)? = params.placeholder?.let { text -> { Text(text) } }
    val supportingContent: (@Composable () -> Unit)? = params.supportingText?.let { text ->
        {
            Text(
                text = text,
                modifier = probes.text("supporting"),
                onTextLayout = probes.onTextLayout("supporting"),
            )
        }
    }

    when (params.variant) {
        TextFieldVariant.Filled -> TextField(
            value = value,
            onValueChange = { value = it },
            label = label,
            placeholder = placeholderContent,
            supportingText = supportingContent,
            singleLine = true,
            isError = params.isError,
            enabled = params.enabled,
            modifier = modifier,
        )
        TextFieldVariant.Outlined -> OutlinedTextField(
            value = value,
            onValueChange = { value = it },
            label = label,
            placeholder = placeholderContent,
            supportingText = supportingContent,
            singleLine = true,
            isError = params.isError,
            enabled = params.enabled,
            modifier = modifier,
        )
    }
}

package material3expressive.compose.reference

import androidx.compose.foundation.layout.requiredSize
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.test.assertIsEnabled
import androidx.compose.ui.test.assertIsFocused
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.assertIsNotFocused
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performTouchInput
import androidx.compose.ui.test.junit4.ComposeContentTestRule
import androidx.compose.ui.unit.dp

internal val TextFieldReferenceScenes =
    mapOf<String, ReferenceScene>(
        "textfield/filled/empty/light" to textFieldScene(TextFieldReferenceVariant.Filled),
        "textfield/filled/populated/light" to
            textFieldScene(TextFieldReferenceVariant.Filled, value = "Text"),
        "textfield/filled/populated/dark" to
            textFieldScene(TextFieldReferenceVariant.Filled, value = "Text"),
        "textfield/filled/focused-empty/light" to
            textFieldScene(
                TextFieldReferenceVariant.Filled,
                focused = true,
                placeholder = "Placeholder",
            ),
        "textfield/filled/error/light" to
            textFieldScene(
                TextFieldReferenceVariant.Filled,
                value = "Input",
                isError = true,
                supportingText = "Error message",
            ),
        "textfield/filled/disabled/light" to
            textFieldScene(
                TextFieldReferenceVariant.Filled,
                value = "Text",
                enabled = false,
            ),
        "textfield/outlined/empty/light" to
            textFieldScene(TextFieldReferenceVariant.Outlined),
        "textfield/outlined/populated/light" to
            textFieldScene(TextFieldReferenceVariant.Outlined, value = "Text"),
        "textfield/outlined/populated/dark" to
            textFieldScene(TextFieldReferenceVariant.Outlined, value = "Text"),
        "textfield/outlined/focused-empty/light" to
            textFieldScene(
                TextFieldReferenceVariant.Outlined,
                focused = true,
                placeholder = "Placeholder",
            ),
        "textfield/outlined/error/light" to
            textFieldScene(
                TextFieldReferenceVariant.Outlined,
                value = "Input",
                isError = true,
                supportingText = "Error message",
            ),
        "textfield/outlined/disabled/light" to
            textFieldScene(
                TextFieldReferenceVariant.Outlined,
                value = "Text",
                enabled = false,
            ),
    )

private enum class TextFieldReferenceVariant {
    Filled,
    Outlined,
}

private fun textFieldScene(
    variant: TextFieldReferenceVariant,
    value: String = "",
    focused: Boolean = false,
    placeholder: String? = null,
    isError: Boolean = false,
    supportingText: String? = null,
    enabled: Boolean = true,
): ReferenceScene {
    val prepare: ComposeContentTestRule.() -> Unit =
        if (focused) {
            {
                onNodeWithTag(ComponentTag)
                    // Mirrors the upstream screenshot tests: split the gesture so focus and
                    // label animation receive an intervening composition.
                    .performTouchInput { down(center) }
                    .performTouchInput {
                        move()
                        up()
                    }
            }
        } else {
            {}
        }
    val validate: ComposeContentTestRule.() -> Unit = {
        val node = onNodeWithTag(ComponentTag)
        if (enabled) {
            node.assertIsEnabled()
            if (focused) node.assertIsFocused() else node.assertIsNotFocused()
        } else {
            // Disabled editable nodes omit the Focused semantics property entirely.
            node.assertIsNotEnabled()
        }
    }

    return referenceScene(
        prepare = prepare,
        validate = validate,
    ) {
        val modifier =
            Modifier.requiredSize(
                    width = 240.dp,
                    height = if (supportingText == null) 56.dp else 80.dp,
                )
                .testTag(ComponentTag)
        val label: @Composable () -> Unit = { Text("Label") }
        val placeholderContent: (@Composable () -> Unit)? =
            placeholder?.let { text ->
                { Text(text) }
            }
        val supportingContent: (@Composable () -> Unit)? =
            supportingText?.let { text ->
                { Text(text) }
            }

        when (variant) {
            TextFieldReferenceVariant.Filled ->
                TextField(
                    value = value,
                    onValueChange = {},
                    label = label,
                    placeholder = placeholderContent,
                    supportingText = supportingContent,
                    singleLine = true,
                    isError = isError,
                    enabled = enabled,
                    modifier = modifier,
                )
            TextFieldReferenceVariant.Outlined ->
                OutlinedTextField(
                    value = value,
                    onValueChange = {},
                    label = label,
                    placeholder = placeholderContent,
                    supportingText = supportingContent,
                    singleLine = true,
                    isError = isError,
                    enabled = enabled,
                    modifier = modifier,
                )
        }
    }
}

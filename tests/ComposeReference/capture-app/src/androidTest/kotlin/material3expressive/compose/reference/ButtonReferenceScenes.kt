package material3expressive.compose.reference

import androidx.compose.foundation.layout.height
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ElevatedButton
import androidx.compose.material3.ExperimentalMaterial3ExpressiveApi
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag

internal val ButtonReferenceScenes =
    mapOf<String, ReferenceScene>(
        "button/filled/medium/enabled/light" to {
            MediumButton(ButtonReferenceVariant.Filled, enabled = true)
        },
        "button/filled/medium/enabled/dark" to {
            MediumButton(ButtonReferenceVariant.Filled, enabled = true)
        },
        "button/filled/medium/disabled/light" to {
            MediumButton(ButtonReferenceVariant.Filled, enabled = false)
        },
        "button/filled/medium/disabled/dark" to {
            MediumButton(ButtonReferenceVariant.Filled, enabled = false)
        },
        "button/tonal/medium/enabled/light" to {
            MediumButton(ButtonReferenceVariant.Tonal, enabled = true)
        },
        "button/elevated/medium/enabled/light" to {
            MediumButton(ButtonReferenceVariant.Elevated, enabled = true)
        },
        "button/outlined/medium/enabled/light" to {
            MediumButton(ButtonReferenceVariant.Outlined, enabled = true)
        },
        "button/text/medium/enabled/light" to {
            MediumButton(ButtonReferenceVariant.Text, enabled = true)
        },
    )

private enum class ButtonReferenceVariant {
    Filled,
    Tonal,
    Elevated,
    Outlined,
    Text,
}

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
@Composable
private fun MediumButton(variant: ButtonReferenceVariant, enabled: Boolean) {
    val height = ButtonDefaults.MediumContainerHeight
    val modifier = Modifier.height(height).testTag(ComponentTag)
    val shapes = ButtonDefaults.shapesFor(height)
    val padding = ButtonDefaults.contentPaddingFor(height)
    val content: @Composable () -> Unit = { Text("Button") }

    when (variant) {
        ButtonReferenceVariant.Filled ->
            Button(
                onClick = {},
                shapes = shapes,
                modifier = modifier,
                enabled = enabled,
                contentPadding = padding,
            ) {
                content()
            }
        ButtonReferenceVariant.Tonal ->
            FilledTonalButton(
                onClick = {},
                shapes = shapes,
                modifier = modifier,
                enabled = enabled,
                contentPadding = padding,
            ) {
                content()
            }
        ButtonReferenceVariant.Elevated ->
            ElevatedButton(
                onClick = {},
                shapes = shapes,
                modifier = modifier,
                enabled = enabled,
                contentPadding = padding,
            ) {
                content()
            }
        ButtonReferenceVariant.Outlined ->
            OutlinedButton(
                onClick = {},
                shapes = shapes,
                modifier = modifier,
                enabled = enabled,
                contentPadding = padding,
            ) {
                content()
            }
        ButtonReferenceVariant.Text ->
            TextButton(
                onClick = {},
                shapes = shapes,
                modifier = modifier,
                enabled = enabled,
                contentPadding = padding,
            ) {
                content()
            }
    }
}

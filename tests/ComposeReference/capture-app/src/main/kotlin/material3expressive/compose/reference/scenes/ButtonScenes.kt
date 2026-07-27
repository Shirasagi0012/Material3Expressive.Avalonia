package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.requiredSize
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
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerButtonScenes() {
    register(
        buttonContract(id = "button/filled/medium/enabled/light", dark = false),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Filled, enabled = true) },
    )
    register(
        buttonContract(id = "button/filled/medium/enabled/dark", dark = true),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Filled, enabled = true) },
    )
    register(
        buttonContract(id = "button/filled/medium/disabled/light", dark = false),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Filled, enabled = false) },
    )
    register(
        buttonContract(id = "button/filled/medium/disabled/dark", dark = true),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Filled, enabled = false) },
    )
    register(
        buttonContract(id = "button/tonal/medium/enabled/light", dark = false),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Tonal, enabled = true) },
    )
    register(
        buttonContract(id = "button/elevated/medium/enabled/light", dark = false),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Elevated, enabled = true) },
    )
    register(
        buttonContract(id = "button/outlined/medium/enabled/light", dark = false),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Outlined, enabled = true) },
    )
    register(
        buttonContract(id = "button/text/medium/enabled/light", dark = false),
        ReferenceScene { probes -> MediumButton(probes, ButtonVariant.Text, enabled = true) },
    )
}

private fun buttonContract(id: String, dark: Boolean) = CaseContract(
    id = id,
    viewportWidthDp = 240,
    viewportHeightDp = 96,
    dark = dark,
    requiredNodes = listOf(
        RequiredNodeSpec(id = "component", kind = "container"),
        RequiredNodeSpec(id = "label", kind = "text", parentId = "component", text = "Button"),
    ),
)

private enum class ButtonVariant { Filled, Tonal, Elevated, Outlined, Text }

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
@Composable
private fun MediumButton(probes: ProbeRegistry, variant: ButtonVariant, enabled: Boolean) {
    val height = ButtonDefaults.MediumContainerHeight
    val modifier = Modifier.height(height).then(probes.geometry("component"))
    val shapes = ButtonDefaults.shapesFor(height)
    val padding = ButtonDefaults.contentPaddingFor(height)
    val content: @Composable () -> Unit = {
        Text(
            text = "Button",
            modifier = probes.text("label"),
            onTextLayout = probes.onTextLayout("label"),
        )
    }

    when (variant) {
        ButtonVariant.Filled -> Button(
            onClick = {}, shapes = shapes, modifier = modifier,
            enabled = enabled, contentPadding = padding,
        ) { content() }
        ButtonVariant.Tonal -> FilledTonalButton(
            onClick = {}, shapes = shapes, modifier = modifier,
            enabled = enabled, contentPadding = padding,
        ) { content() }
        ButtonVariant.Elevated -> ElevatedButton(
            onClick = {}, shapes = shapes, modifier = modifier,
            enabled = enabled, contentPadding = padding,
        ) { content() }
        ButtonVariant.Outlined -> OutlinedButton(
            onClick = {}, shapes = shapes, modifier = modifier,
            enabled = enabled, contentPadding = padding,
        ) { content() }
        ButtonVariant.Text -> TextButton(
            onClick = {}, shapes = shapes, modifier = modifier,
            enabled = enabled, contentPadding = padding,
        ) { content() }
    }
}

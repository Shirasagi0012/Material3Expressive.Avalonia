package material3expressive.compose.reference

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.unit.dp

internal val TypographyReferenceScenes =
    mapOf<String, ReferenceScene>(
        "foundation/typography/display-headline/light" to referenceScene {
            TypographyGroup(TypographyGroupKind.DisplayHeadline)
        },
        "foundation/typography/display-headline/dark" to referenceScene {
            TypographyGroup(TypographyGroupKind.DisplayHeadline)
        },
        "foundation/typography/title-body/light" to referenceScene {
            TypographyGroup(TypographyGroupKind.TitleBody)
        },
        "foundation/typography/title-body/dark" to referenceScene {
            TypographyGroup(TypographyGroupKind.TitleBody)
        },
        "foundation/typography/label/light" to referenceScene {
            TypographyGroup(TypographyGroupKind.Label)
        },
        "foundation/typography/label/dark" to referenceScene {
            TypographyGroup(TypographyGroupKind.Label)
        },
    )

private enum class TypographyGroupKind {
    DisplayHeadline,
    TitleBody,
    Label,
}

private data class TypographySample(
    val text: String,
    val style: TextStyle,
)

@Composable
private fun TypographyGroup(group: TypographyGroupKind) {
    val typography = MaterialTheme.typography
    val samples =
        when (group) {
            TypographyGroupKind.DisplayHeadline ->
                listOf(
                    TypographySample("Display large", typography.displayLarge),
                    TypographySample("Display medium", typography.displayMedium),
                    TypographySample("Display small", typography.displaySmall),
                    TypographySample("Headline large", typography.headlineLarge),
                    TypographySample("Headline medium", typography.headlineMedium),
                    TypographySample("Headline small", typography.headlineSmall),
                )
            TypographyGroupKind.TitleBody ->
                listOf(
                    TypographySample("Title large", typography.titleLarge),
                    TypographySample("Title medium", typography.titleMedium),
                    TypographySample("Title small", typography.titleSmall),
                    TypographySample("Body large", typography.bodyLarge),
                    TypographySample("Body medium", typography.bodyMedium),
                    TypographySample("Body small", typography.bodySmall),
                )
            TypographyGroupKind.Label ->
                listOf(
                    TypographySample("Label large", typography.labelLarge),
                    TypographySample("Label medium", typography.labelMedium),
                    TypographySample("Label small", typography.labelSmall),
                )
        }

    Column(
        modifier = Modifier.fillMaxSize().testTag(ComponentTag).padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        samples.forEach { sample ->
            Text(
                text = sample.text,
                style = sample.style,
                color = MaterialTheme.colorScheme.onSurface,
                maxLines = 1,
            )
        }
    }
}

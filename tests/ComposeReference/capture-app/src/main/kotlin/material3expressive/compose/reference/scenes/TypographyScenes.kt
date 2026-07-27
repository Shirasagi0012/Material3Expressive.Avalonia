package material3expressive.compose.reference.scenes

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.unit.dp
import material3expressive.compose.reference.CaseContract
import material3expressive.compose.reference.ReferenceCaseRegistry
import material3expressive.compose.reference.ReferenceScene
import material3expressive.compose.reference.RequiredNodeSpec
import material3expressive.compose.reference.probes.ProbeRegistry

fun ReferenceCaseRegistry.registerTypographyScenes() {
    register(
        CaseContract(
            id = "foundation/typography/display-headline/light",
            viewportWidthDp = 480,
            viewportHeightDp = 320,
            dark = false,
            requiredNodes = typographyNodes(TypographyGroupKind.DisplayHeadline),
        ),
        ReferenceScene { probes -> TypographyGroup(probes, TypographyGroupKind.DisplayHeadline) },
    )
    register(
        CaseContract(
            id = "foundation/typography/display-headline/dark",
            viewportWidthDp = 480,
            viewportHeightDp = 320,
            dark = true,
            requiredNodes = typographyNodes(TypographyGroupKind.DisplayHeadline),
        ),
        ReferenceScene { probes -> TypographyGroup(probes, TypographyGroupKind.DisplayHeadline) },
    )
    register(
        CaseContract(
            id = "foundation/typography/title-body/light",
            viewportWidthDp = 360,
            viewportHeightDp = 184,
            dark = false,
            requiredNodes = typographyNodes(TypographyGroupKind.TitleBody),
        ),
        ReferenceScene { probes -> TypographyGroup(probes, TypographyGroupKind.TitleBody) },
    )
    register(
        CaseContract(
            id = "foundation/typography/title-body/dark",
            viewportWidthDp = 360,
            viewportHeightDp = 184,
            dark = true,
            requiredNodes = typographyNodes(TypographyGroupKind.TitleBody),
        ),
        ReferenceScene { probes -> TypographyGroup(probes, TypographyGroupKind.TitleBody) },
    )
    register(
        CaseContract(
            id = "foundation/typography/label/light",
            viewportWidthDp = 320,
            viewportHeightDp = 92,
            dark = false,
            requiredNodes = typographyNodes(TypographyGroupKind.Label),
        ),
        ReferenceScene { probes -> TypographyGroup(probes, TypographyGroupKind.Label) },
    )
    register(
        CaseContract(
            id = "foundation/typography/label/dark",
            viewportWidthDp = 320,
            viewportHeightDp = 92,
            dark = true,
            requiredNodes = typographyNodes(TypographyGroupKind.Label),
        ),
        ReferenceScene { probes -> TypographyGroup(probes, TypographyGroupKind.Label) },
    )
}

private enum class TypographyGroupKind { DisplayHeadline, TitleBody, Label }

private data class TypographySampleSpec(val id: String, val text: String)
private data class TypographySample(val spec: TypographySampleSpec, val style: TextStyle)

private fun typographyNodes(group: TypographyGroupKind): List<RequiredNodeSpec> =
    listOf(RequiredNodeSpec(id = "component", kind = "container")) +
        typographySampleSpecs(group).map { spec ->
            RequiredNodeSpec(id = spec.id, kind = "text", parentId = "component", text = spec.text)
        }

private fun typographySampleSpecs(group: TypographyGroupKind): List<TypographySampleSpec> = when (group) {
    TypographyGroupKind.DisplayHeadline -> listOf(
        TypographySampleSpec("display-large", "Display large"),
        TypographySampleSpec("display-medium", "Display medium"),
        TypographySampleSpec("display-small", "Display small"),
        TypographySampleSpec("headline-large", "Headline large"),
        TypographySampleSpec("headline-medium", "Headline medium"),
        TypographySampleSpec("headline-small", "Headline small"),
    )
    TypographyGroupKind.TitleBody -> listOf(
        TypographySampleSpec("title-large", "Title large"),
        TypographySampleSpec("title-medium", "Title medium"),
        TypographySampleSpec("title-small", "Title small"),
        TypographySampleSpec("body-large", "Body large"),
        TypographySampleSpec("body-medium", "Body medium"),
        TypographySampleSpec("body-small", "Body small"),
    )
    TypographyGroupKind.Label -> listOf(
        TypographySampleSpec("label-large", "Label large"),
        TypographySampleSpec("label-medium", "Label medium"),
        TypographySampleSpec("label-small", "Label small"),
    )
}

@Composable
private fun TypographyGroup(probes: ProbeRegistry, group: TypographyGroupKind) {
    val typography = MaterialTheme.typography
    val specs = typographySampleSpecs(group)
    val samples = when (group) {
        TypographyGroupKind.DisplayHeadline -> listOf(
            TypographySample(specs[0], typography.displayLarge),
            TypographySample(specs[1], typography.displayMedium),
            TypographySample(specs[2], typography.displaySmall),
            TypographySample(specs[3], typography.headlineLarge),
            TypographySample(specs[4], typography.headlineMedium),
            TypographySample(specs[5], typography.headlineSmall),
        )
        TypographyGroupKind.TitleBody -> listOf(
            TypographySample(specs[0], typography.titleLarge),
            TypographySample(specs[1], typography.titleMedium),
            TypographySample(specs[2], typography.titleSmall),
            TypographySample(specs[3], typography.bodyLarge),
            TypographySample(specs[4], typography.bodyMedium),
            TypographySample(specs[5], typography.bodySmall),
        )
        TypographyGroupKind.Label -> listOf(
            TypographySample(specs[0], typography.labelLarge),
            TypographySample(specs[1], typography.labelMedium),
            TypographySample(specs[2], typography.labelSmall),
        )
    }

    Column(
        modifier = Modifier.fillMaxSize().padding(16.dp).then(probes.geometry("component")),
        verticalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        samples.forEach { sample ->
            Text(
                text = sample.spec.text,
                style = sample.style,
                color = MaterialTheme.colorScheme.onSurface,
                maxLines = 1,
                modifier = probes.text(sample.spec.id),
                onTextLayout = probes.onTextLayout(sample.spec.id),
            )
        }
    }
}

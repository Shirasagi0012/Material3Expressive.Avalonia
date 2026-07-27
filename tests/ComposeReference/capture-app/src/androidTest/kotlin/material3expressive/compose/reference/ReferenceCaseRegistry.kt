package material3expressive.compose.reference

import androidx.compose.runtime.Composable
import androidx.compose.ui.test.junit4.ComposeContentTestRule

internal typealias ReferenceContent = @Composable () -> Unit

internal data class ReferenceScene(
    val content: ReferenceContent,
    val prepare: ComposeContentTestRule.() -> Unit = {},
    val validate: ComposeContentTestRule.() -> Unit = {},
)

internal fun referenceScene(
    prepare: ComposeContentTestRule.() -> Unit = {},
    validate: ComposeContentTestRule.() -> Unit = {},
    content: ReferenceContent,
) = ReferenceScene(content, prepare, validate)

internal data class BoundReferenceCase(
    val contract: ReferenceCaseContract,
    val scene: ReferenceScene,
)

internal object ReferenceCaseRegistry {
    private val scenes =
        linkedMapOf<String, ReferenceScene>().apply {
            putAll(FoundationReferenceScenes)
            putAll(TypographyReferenceScenes)
            putAll(ComponentReferenceScenes)
            putAll(TextFieldReferenceScenes)
            putAll(NavigationReferenceScenes)
            putAll(ButtonReferenceScenes)
        }

    fun bind(contracts: List<ReferenceCaseContract>): List<BoundReferenceCase> {
        val manifestIds = contracts.map { it.id }.toSet()
        val registryIds = scenes.keys
        val missing = manifestIds - registryIds
        val extra = registryIds - manifestIds
        check(missing.isEmpty() && extra.isEmpty()) {
            "Reference registry and manifest differ. Missing renderers: " +
                "${missing.sorted()}; unplanned renderers: ${extra.sorted()}."
        }
        return contracts.map { contract ->
            BoundReferenceCase(
                contract,
                requireNotNull(scenes[contract.id]) {
                    "No Compose renderer is registered for '${contract.id}'."
                },
            )
        }
    }
}

internal const val CaptureTag = "material3-expressive-reference"
internal const val ComponentTag = "material3-expressive-reference-component"

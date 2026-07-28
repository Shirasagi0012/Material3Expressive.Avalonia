package material3expressive.compose.reference

import androidx.compose.runtime.Composable
import material3expressive.compose.reference.probes.ProbeRegistry

data class CaseContract(
    val id: String,
    val viewportWidthDp: Int,
    val viewportHeightDp: Int,
    val dark: Boolean,
    val requiredNodes: List<RequiredNodeSpec>,
)

data class RequiredNodeSpec(
    val id: String,
    val kind: String,
    val parentId: String? = null,
    val text: String? = null,
    /**
     * The semantic focus state required at capture time. It is deliberately nullable because
     * focus is only a contract dimension for scenes that explicitly exercise it.
     */
    val expectedFocused: Boolean? = null,
)

data class ReferenceScene(
    val content: @Composable (ProbeRegistry) -> Unit,
)

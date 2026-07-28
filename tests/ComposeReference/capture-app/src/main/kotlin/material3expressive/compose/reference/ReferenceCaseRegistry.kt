package material3expressive.compose.reference

import java.security.MessageDigest

class ReferenceCaseRegistry {
    private val scenes = linkedMapOf<String, ReferenceScene>()
    private val contracts = linkedMapOf<String, CaseContract>()

    fun register(contract: CaseContract, scene: ReferenceScene) {
        require(contract.id.isNotBlank()) { "Case ID must not be blank." }
        require(contract.viewportWidthDp > 0 && contract.viewportHeightDp > 0) {
            "Case '${contract.id}' has an invalid viewport."
        }
        require(contract.renderScale.isFinite() && contract.renderScale > 0f) {
            "Case '${contract.id}' has an invalid render scale."
        }
        require(contract.requiredNodes.map { it.id }.distinct().size == contract.requiredNodes.size) {
            "Case '${contract.id}' has duplicate required node IDs."
        }
        check(contract.id !in contracts) { "Case '${contract.id}' is registered more than once." }
        scenes[contract.id] = scene
        contracts[contract.id] = contract
    }

    fun get(caseId: String): ReferenceScene? = scenes[caseId]
    fun contract(caseId: String): CaseContract? = contracts[caseId]
    fun allIds(): List<String> = contracts.keys.toList()

    fun catalogSha256(): String {
        val canonical = buildString {
            contracts.values.forEach { contract ->
                appendField(contract.id)
                append(contract.viewportWidthDp).append(',')
                append(contract.viewportHeightDp).append(',')
                append(contract.dark).append(',')
                append(contract.renderScale).append(';')
                contract.requiredNodes.forEach { node ->
                    appendField(node.id)
                    appendField(node.kind)
                    appendField(node.parentId)
                    appendField(node.text)
                    appendField(node.expectedFocused?.toString())
                }
                append('\n')
            }
        }
        return MessageDigest.getInstance("SHA-256")
            .digest(canonical.toByteArray(Charsets.UTF_8))
            .joinToString("") { "%02X".format(it) }
    }

    private fun StringBuilder.appendField(value: String?) {
        append(value?.length ?: -1).append(':')
        if (value != null) append(value)
        append('|')
    }
}

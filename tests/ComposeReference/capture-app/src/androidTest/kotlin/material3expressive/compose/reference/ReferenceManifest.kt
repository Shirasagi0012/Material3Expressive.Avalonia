package material3expressive.compose.reference

import android.os.Build
import androidx.test.platform.app.InstrumentationRegistry
import java.security.MessageDigest
import java.util.Locale
import org.json.JSONObject

internal data class ReferenceCaseContract(
    val id: String,
    val image: String,
    val dark: Boolean,
    val width: Int,
    val height: Int,
)

internal class ReferenceManifestContract private constructor(
    private val document: JSONObject,
    val sha256: String,
    val cases: List<ReferenceCaseContract>,
) {
    val schemaVersion: Int
        get() = document.getInt("schemaVersion")

    val referenceSetId: String
        get() = document.getString("referenceSetId")

    val composeMaterial3Version: String
        get() = document.getString("composeMaterial3Version")

    val sourceArtifact: String
        get() = document.getString("sourceArtifact")

    val sourceArtifactSha256: String
        get() = document.getString("sourceArtifactSha256")

    fun colorContractMetadata(): JSONObject =
        JSONObject(document.getJSONObject("colorContract").toString())

    fun environmentMetadata(): JSONObject {
        val environment = JSONObject(document.getJSONObject("environment").toString())
        environment.put("androidApi", Build.VERSION.SDK_INT)
        return environment
    }

    private fun validate() {
        check(schemaVersion == 2) { "Expected reference manifest schema 2, got $schemaVersion." }
        check(composeMaterial3Version == BuildConfig.MATERIAL3_VERSION) {
            "Manifest Material3 version $composeMaterial3Version does not match the built " +
                "dependency ${BuildConfig.MATERIAL3_VERSION}."
        }
        check(sourceArtifact == BuildConfig.MATERIAL3_ARTIFACT) {
            "Manifest artifact $sourceArtifact does not match ${BuildConfig.MATERIAL3_ARTIFACT}."
        }
        check(sourceArtifactSha256 == BuildConfig.MATERIAL3_AAR_SHA256) {
            "Manifest AAR hash does not match the verified build artifact."
        }
        check(sha256 == BuildConfig.REFERENCE_MANIFEST_SHA256) {
            "Reference manifest hash mismatch. Expected " +
                "${BuildConfig.REFERENCE_MANIFEST_SHA256}, got $sha256."
        }

        val environment = document.getJSONObject("environment")
        check(environment.getInt("androidApi") == Build.VERSION.SDK_INT) {
            "Manifest requires Android API ${environment.getInt("androidApi")}; " +
                "device reports ${Build.VERSION.SDK_INT}."
        }
        check(environment.getInt("densityDpi") == 160)
        check(environment.getDouble("fontScale") == 1.0)
        check(environment.getString("locale") == "en-US")
        check(Locale.getDefault().toLanguageTag() == environment.getString("locale")) {
            "Manifest requires locale ${environment.getString("locale")}; " +
                "capture runtime reports ${Locale.getDefault().toLanguageTag()}."
        }
        check(environment.getString("layoutDirection") == "ltr")
        check(environment.getString("fontFamily") == "Roboto")
        check(environment.getString("fontWeightPolicy") == "preset-weights-only")
        check(environment.getString("fontFileSha256") == BuildConfig.REFERENCE_FONT_SHA256) {
            "Manifest font hash does not match the verified Roboto asset."
        }

        check(cases.isNotEmpty()) { "Reference manifest contains no cases." }
        val ids = HashSet<String>()
        val images = HashSet<String>()
        for (visualCase in cases) {
            check(visualCase.id.matches(Regex("[A-Za-z0-9._/-]+")) && ".." !in visualCase.id)
            check(ids.add(visualCase.id)) { "Duplicate manifest case '${visualCase.id}'." }
            check(images.add(visualCase.image)) { "Duplicate manifest image '${visualCase.image}'." }
            check(visualCase.image == "cases/${visualCase.id}.png") {
                "Case '${visualCase.id}' must use its stable ID as the image path."
            }
            check(visualCase.width > 0 && visualCase.height > 0)
        }
    }

    companion object {
        private const val AssetName = "reference-manifest.json"

        fun read(): ReferenceManifestContract {
            val context = InstrumentationRegistry.getInstrumentation().context
            val bytes = context.assets.open(AssetName).use { it.readBytes() }
            val document = JSONObject(bytes.toString(Charsets.UTF_8))
            val casesJson = document.getJSONArray("cases")
            val cases =
                buildList {
                    for (index in 0 until casesJson.length()) {
                        val item = casesJson.getJSONObject(index)
                        val theme = item.getString("theme")
                        check(theme == "light" || theme == "dark") {
                            "Case '${item.getString("id")}' has unsupported theme '$theme'."
                        }
                        add(
                            ReferenceCaseContract(
                                id = item.getString("id"),
                                image = item.getString("image"),
                                dark = theme == "dark",
                                width = item.getInt("width"),
                                height = item.getInt("height"),
                            )
                        )
                    }
                }
            return ReferenceManifestContract(document, bytes.sha256Hex(), cases).also {
                it.validate()
            }
        }
    }
}

internal fun ByteArray.sha256Hex(): String =
    MessageDigest.getInstance("SHA-256").digest(this).joinToString("") { "%02X".format(it) }

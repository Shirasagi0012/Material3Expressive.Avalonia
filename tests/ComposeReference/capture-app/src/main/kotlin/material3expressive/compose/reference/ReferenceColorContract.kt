package material3expressive.compose.reference

import androidx.compose.material3.ColorScheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.nio.file.Path
import java.security.MessageDigest
import kotlin.io.path.readText

class ReferenceColorContract(
    private val document: JsonObject,
    private val rawJson: String,
) {
    val manifestId: String = document["id"]!!.jsonPrimitive.content
    val manifestSha256: String = sha256(rawJson.toByteArray(Charsets.UTF_8))

    fun lightScheme(): ColorScheme = buildScheme(dark = false)
    fun darkScheme(): ColorScheme = buildScheme(dark = true)

    private fun buildScheme(dark: Boolean): ColorScheme {
        val roles = document["roles"]!!.jsonObject
        fun r(name: String) = parseColor(
            roles[name]!!.jsonObject[if (dark) "dark" else "light"]!!.jsonPrimitive.content
        )
        return if (dark) {
            darkColorScheme(
                primary = r("Primary"), onPrimary = r("OnPrimary"),
                primaryContainer = r("PrimaryContainer"), onPrimaryContainer = r("OnPrimaryContainer"),
                secondary = r("Secondary"), onSecondary = r("OnSecondary"),
                secondaryContainer = r("SecondaryContainer"), onSecondaryContainer = r("OnSecondaryContainer"),
                tertiary = r("Tertiary"), onTertiary = r("OnTertiary"),
                tertiaryContainer = r("TertiaryContainer"), onTertiaryContainer = r("OnTertiaryContainer"),
                background = r("Background"), onBackground = r("OnBackground"),
                surface = r("Surface"), onSurface = r("OnSurface"),
                surfaceVariant = r("SurfaceVariant"), onSurfaceVariant = r("OnSurfaceVariant"),
                outline = r("Outline"), outlineVariant = r("OutlineVariant"),
                inverseSurface = r("InverseSurface"), inverseOnSurface = r("InverseOnSurface"),
                inversePrimary = r("InversePrimary"), surfaceTint = r("SurfaceTint"),
                surfaceContainerLowest = r("SurfaceContainerLowest"),
                surfaceContainerLow = r("SurfaceContainerLow"),
                surfaceContainer = r("SurfaceContainer"),
                surfaceContainerHigh = r("SurfaceContainerHigh"),
                surfaceContainerHighest = r("SurfaceContainerHighest"),
                surfaceBright = r("SurfaceBright"), surfaceDim = r("SurfaceDim"),
            )
        } else {
            lightColorScheme(
                primary = r("Primary"), onPrimary = r("OnPrimary"),
                primaryContainer = r("PrimaryContainer"), onPrimaryContainer = r("OnPrimaryContainer"),
                secondary = r("Secondary"), onSecondary = r("OnSecondary"),
                secondaryContainer = r("SecondaryContainer"), onSecondaryContainer = r("OnSecondaryContainer"),
                tertiary = r("Tertiary"), onTertiary = r("OnTertiary"),
                tertiaryContainer = r("TertiaryContainer"), onTertiaryContainer = r("OnTertiaryContainer"),
                background = r("Background"), onBackground = r("OnBackground"),
                surface = r("Surface"), onSurface = r("OnSurface"),
                surfaceVariant = r("SurfaceVariant"), onSurfaceVariant = r("OnSurfaceVariant"),
                outline = r("Outline"), outlineVariant = r("OutlineVariant"),
                inverseSurface = r("InverseSurface"), inverseOnSurface = r("InverseOnSurface"),
                inversePrimary = r("InversePrimary"), surfaceTint = r("SurfaceTint"),
                surfaceContainerLowest = r("SurfaceContainerLowest"),
                surfaceContainerLow = r("SurfaceContainerLow"),
                surfaceContainer = r("SurfaceContainer"),
                surfaceContainerHigh = r("SurfaceContainerHigh"),
                surfaceContainerHighest = r("SurfaceContainerHighest"),
                surfaceBright = r("SurfaceBright"), surfaceDim = r("SurfaceDim"),
            )
        }
    }

    private fun parseColor(hex: String): Color {
        val digits = hex.removePrefix("#").removePrefix("0x")
        require(digits.length == 6 || digits.length == 8) {
            "Expected #RRGGBB or #AARRGGBB color, got '$hex'."
        }
        val value = digits.toLong(16)
        val alpha = if (digits.length == 8) ((value shr 24) and 0xFF) / 255f else 1f
        return Color(
            red = ((value shr 16) and 0xFF) / 255f,
            green = ((value shr 8) and 0xFF) / 255f,
            blue = (value and 0xFF) / 255f,
            alpha = alpha,
        )
    }

    companion object {
        private val json = Json { ignoreUnknownKeys = true }

        fun load(path: Path): ReferenceColorContract {
            val text = path.readText()
            val document = json.parseToJsonElement(text).jsonObject
            return ReferenceColorContract(document, text)
        }

        private fun sha256(bytes: ByteArray): String =
            MessageDigest.getInstance("SHA-256")
                .digest(bytes)
                .joinToString("") { "%02X".format(it) }
    }
}

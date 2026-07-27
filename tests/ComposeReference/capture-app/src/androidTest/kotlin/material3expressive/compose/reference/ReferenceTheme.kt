package material3expressive.compose.reference

import android.graphics.Color as AndroidColor
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.test.platform.app.InstrumentationRegistry
import org.json.JSONObject

internal object ReferenceTypographyContract {
    val Roboto =
        FontFamily(
            Font(R.font.roboto_reference, FontWeight.Normal),
            Font(R.font.roboto_reference, FontWeight.Medium),
            Font(R.font.roboto_reference, FontWeight.Bold),
        )

    val Typography =
        Typography().let { defaults ->
            Typography(
                displayLarge = defaults.displayLarge.withRoboto(),
                displayMedium = defaults.displayMedium.withRoboto(),
                displaySmall = defaults.displaySmall.withRoboto(),
                headlineLarge = defaults.headlineLarge.withRoboto(),
                headlineMedium = defaults.headlineMedium.withRoboto(),
                headlineSmall = defaults.headlineSmall.withRoboto(),
                titleLarge = defaults.titleLarge.withRoboto(),
                titleMedium = defaults.titleMedium.withRoboto(),
                titleSmall = defaults.titleSmall.withRoboto(),
                bodyLarge = defaults.bodyLarge.withRoboto(),
                bodyMedium = defaults.bodyMedium.withRoboto(),
                bodySmall = defaults.bodySmall.withRoboto(),
                labelLarge = defaults.labelLarge.withRoboto(),
                labelMedium = defaults.labelMedium.withRoboto(),
                labelSmall = defaults.labelSmall.withRoboto(),
            )
        }

    fun validate() {
        val styles =
            listOf(
                Typography.displayLarge,
                Typography.displayMedium,
                Typography.displaySmall,
                Typography.headlineLarge,
                Typography.headlineMedium,
                Typography.headlineSmall,
                Typography.titleLarge,
                Typography.titleMedium,
                Typography.titleSmall,
                Typography.bodyLarge,
                Typography.bodyMedium,
                Typography.bodySmall,
                Typography.labelLarge,
                Typography.labelMedium,
                Typography.labelSmall,
            )
        check(styles.all { it.fontFamily == Roboto }) {
            "Every Compose typography slot must use the verified Roboto family."
        }
    }

    private fun TextStyle.withRoboto() = copy(fontFamily = Roboto)
}

internal object ReferenceColorContract {
    private val metadata by lazy {
        ReferenceManifestContract.read().colorContractMetadata()
    }
    private val document by lazy {
        val assetName = metadata.getString("manifest").substringAfterLast('/')
        val context = InstrumentationRegistry.getInstrumentation().context
        val bytes = context.assets.open(assetName).use { it.readBytes() }
        val expected = metadata.getString("sha256")
        val actual = bytes.sha256Hex()
        check(actual == expected) {
            "Color contract hash mismatch. Expected $expected, got $actual."
        }
        JSONObject(bytes.toString(Charsets.UTF_8))
    }

    fun metadata(): JSONObject = JSONObject(metadata.toString())

    fun scheme(dark: Boolean) =
        if (dark) {
            darkColorScheme(
                primary = role("Primary", dark),
                onPrimary = role("OnPrimary", dark),
                primaryContainer = role("PrimaryContainer", dark),
                onPrimaryContainer = role("OnPrimaryContainer", dark),
                secondary = role("Secondary", dark),
                onSecondary = role("OnSecondary", dark),
                secondaryContainer = role("SecondaryContainer", dark),
                onSecondaryContainer = role("OnSecondaryContainer", dark),
                tertiary = role("Tertiary", dark),
                onTertiary = role("OnTertiary", dark),
                tertiaryContainer = role("TertiaryContainer", dark),
                onTertiaryContainer = role("OnTertiaryContainer", dark),
                background = role("Background", dark),
                onBackground = role("OnBackground", dark),
                surface = role("Surface", dark),
                onSurface = role("OnSurface", dark),
                surfaceVariant = role("SurfaceVariant", dark),
                onSurfaceVariant = role("OnSurfaceVariant", dark),
                outline = role("Outline", dark),
                outlineVariant = role("OutlineVariant", dark),
                inverseSurface = role("InverseSurface", dark),
                inverseOnSurface = role("InverseOnSurface", dark),
                inversePrimary = role("InversePrimary", dark),
                surfaceTint = role("SurfaceTint", dark),
                surfaceContainerLowest = role("SurfaceContainerLowest", dark),
                surfaceContainerLow = role("SurfaceContainerLow", dark),
                surfaceContainer = role("SurfaceContainer", dark),
                surfaceContainerHigh = role("SurfaceContainerHigh", dark),
                surfaceContainerHighest = role("SurfaceContainerHighest", dark),
                surfaceBright = role("SurfaceBright", dark),
                surfaceDim = role("SurfaceDim", dark),
            )
        } else {
            lightColorScheme(
                primary = role("Primary", dark),
                onPrimary = role("OnPrimary", dark),
                primaryContainer = role("PrimaryContainer", dark),
                onPrimaryContainer = role("OnPrimaryContainer", dark),
                secondary = role("Secondary", dark),
                onSecondary = role("OnSecondary", dark),
                secondaryContainer = role("SecondaryContainer", dark),
                onSecondaryContainer = role("OnSecondaryContainer", dark),
                tertiary = role("Tertiary", dark),
                onTertiary = role("OnTertiary", dark),
                tertiaryContainer = role("TertiaryContainer", dark),
                onTertiaryContainer = role("OnTertiaryContainer", dark),
                background = role("Background", dark),
                onBackground = role("OnBackground", dark),
                surface = role("Surface", dark),
                onSurface = role("OnSurface", dark),
                surfaceVariant = role("SurfaceVariant", dark),
                onSurfaceVariant = role("OnSurfaceVariant", dark),
                outline = role("Outline", dark),
                outlineVariant = role("OutlineVariant", dark),
                inverseSurface = role("InverseSurface", dark),
                inverseOnSurface = role("InverseOnSurface", dark),
                inversePrimary = role("InversePrimary", dark),
                surfaceTint = role("SurfaceTint", dark),
                surfaceContainerLowest = role("SurfaceContainerLowest", dark),
                surfaceContainerLow = role("SurfaceContainerLow", dark),
                surfaceContainer = role("SurfaceContainer", dark),
                surfaceContainerHigh = role("SurfaceContainerHigh", dark),
                surfaceContainerHighest = role("SurfaceContainerHighest", dark),
                surfaceBright = role("SurfaceBright", dark),
                surfaceDim = role("SurfaceDim", dark),
            )
        }

    private fun role(name: String, dark: Boolean): Color {
        val roles = document.getJSONObject("roles")
        val value = roles.getJSONObject(name).getString(if (dark) "dark" else "light")
        return Color(AndroidColor.parseColor(value))
    }
}

